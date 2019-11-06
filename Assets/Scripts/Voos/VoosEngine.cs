/*
 * Copyright 2019 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SD = System.Diagnostics;
using NET = UnityEngine.Networking;

/**
 * The engine takes some input JavaScript and a Unity scene, serializes it, runs the JS on the serialized version, then deserializes the result to the Unity scene. This should be ran every frame.
 * 
 * The key value provided here is the serialization and merge (or "reconciliation") algorithms implemented.
 */
public partial class VoosEngine : MonoBehaviour, IPunObservable
{
  public static bool TerrainCollisionsEnabled = true;
  public static bool EnableProfilingFromScript = false;
  public static string MemCheckMode = "useOnly"; // See ModuleBehaviorsActor.js

  public static string DefaultBrainUid = "__DEFAULT_BRAIN__";

  // TODO better if the 1100 constant was shared between JS and this file...
  // NOTE this is actually limited by Photon's max view ID right now..
  public static int MaxActors = 1100;

  public static int WarningActorCount = 1100;

  public static bool EnableRpcDebug = false;

  public struct ModuleCompileError
  {
    public int lineNum; // base 1
    public string moduleKey;
    public string message;
  }

  // Used to communicate an instantaneous velocity change from script to engine.
  [System.Serializable]
  public struct VelocityChange
  {
    // The name of the entity to apply the change to.
    public string entityName;

    // The change quantity.
    public Vector3 delta;
  }

  [System.Serializable]
  public struct TorqueRequest
  {
    public string actorId;
    public Vector3 torque;
  }

  // Used to send messages from Unity to JS
  [System.Serializable]
  public struct ActorMessage
  {
    // The name of the message. By some pre-agreed protocol.
    public string name;

    // The name of the recipient actor.
    public string targetActor;

    // The arguments of the message as stringified JSON. The actual structure of
    // this should be agreed upon for a given message name.
    public string argsJson;

    // This is useful so...we don't RE-broadcast to other remotes!
    public bool fromRemote;
  }

  [System.Serializable]
  public struct PersistedState
  {
    public float gameTime;

    public VoosActor.PersistedState[] actors;
  }

  [System.Serializable]
  public struct PlayerInitPayload
  {
    public bool isRunning;
    public int[] actorViewIds;
    public VoosActor.PersistedState[] actorStates;
  }

  // For scripting, we need this as well.
  [System.Serializable]
  public struct RuntimeState
  {
    public float gameTime;
    public VoosActor.RuntimeState[] actors;
  }

  [System.Serializable]
  private struct TickRequest
  {
    public string operation;
    public float deltaSeconds;
    public string memCheckMode;
    public bool enableProfilingService;
    public RuntimeState runtimeState;
    public ActorMessage[] messagesFromUnity;
  }

  [System.Serializable]
  private struct TickResponse
  {
    public VelocityChange[] velocityChanges;
    public TorqueRequest[] torqueRequests;

    public ActorMessage[] messagesToRemoteActors;

    // May re-design later. Messages that Unity may be interested in from script.
    public ActorMessage[] messagesToUnity;

    // HACK. Maybe we could just leverage messagesToUnity instead?
    public PlayerToolTip[] playerToolTips;
  }

  [System.Serializable]
  public struct TransferPlayerControlRequest
  {
    public string fromActor;
    // If toActor is empty, control will be transfered to a default
    // actor, as if the player had just joined the game.
    public string toActor;
    // If true, also delete fromActor.
    public bool destroy;
  }

  public int memoryRPCsPerSecond = 5;
  public LinkedList<VoosActor> memoryUpdateQueue = new LinkedList<VoosActor>();
  private float lastMemoryRPCTime = 0;

  const int MEMORY_STATS_SECONDS = 5;
  static int currentMemoryStatSecond = 0;
  static float[] memoryStats = new float[MEMORY_STATS_SECONDS];
  static int memoryRPCQueueCount = 0;
  public static int MemoryRPCQueueCount
  {
    get
    {
      return memoryRPCQueueCount;
    }
  }
  public static float AverageMemoryBytesPerSecond
  {
    get
    {
      float total = 0;
      for (int i = 0; i < MEMORY_STATS_SECONDS; i++)
      {
        if (i != currentMemoryStatSecond)
        {
          total += memoryStats[i];
        }
      }
      return total / (MEMORY_STATS_SECONDS - 1);
    }
  }

  [SerializeField] GlobalUnreliableData globalUnreliableData;

  // Persist between scene reloads.
  string brainUid = System.Guid.NewGuid().ToString();
  string agentUid = System.Guid.NewGuid().ToString();

  [SerializeField] VoosActor actorPrefab;

  [SerializeField] PlayerBodyParts playerBodyParts;

  [SerializeField] AssetCache assetCache;
  [SerializeField] BehaviorSystem behaviorSystem;
  [SerializeField] PhotonViewIdTrash photonViewIdTrash;
  [SerializeField] DynamicPopup popups;
  [SerializeField] TerrainManager terrainSystem;
  [SerializeField] GameBuilderStage gbStage;
  [SerializeField] BuiltinPrefabLibrary builtinPrefabLibrary;

  V8InUnity.Services services = null;

  PhotonView photonView;

  // The active scene. Cloned from initScene.
  VoosScene scene;
  ParticleEffectSystem particleEffectSystem;
  SoundEffectSystem soundEffectSystem;
  GameBuilderSceneController sceneController;

  GameRulesState wizardState = new GameRulesState();

  float gameTime = 0f;

  bool isRunning = false;
  bool singleStepQueued = false;

  bool inEditMode = false;
  bool everUpdatedGlobalLights = false;
  GameBuilderStage.SceneLightingMode currentSceneLightingMode = GameBuilderStage.SceneLightingMode.Day;

  List<ActorMessage> messagesFromUnity = new List<ActorMessage>();

  struct QueuedCollision
  {
    public VoosActor receiver;
    public VoosActor other;
    public bool isEnter;
  }

  struct QueuedTerrainCollision
  {
    public VoosActor receiver;
    public byte style;
  }

  List<QueuedCollision> queuedCollisions = new List<QueuedCollision>();
  List<QueuedTerrainCollision> queuedTerrainCollisions = new List<QueuedTerrainCollision>();

  byte[] updateAgentByteBuffer = new byte[10 * 1024 * 1024];

  bool warnedAboutActorCount = false;

  public delegate void OnActorDestroyed(VoosActor actor);
  public delegate void OnActorCreated(VoosActor actor);
  public event OnActorDestroyed onBeforeActorDestroy;
  public event OnActorCreated onActorCreated;
  public event System.Action<VoosActor> onActorBecomingUncontrollable;

  // I guess longer term, we need a more general player-local 2D GUI API.

  [System.Serializable]
  public struct PlayerToolTip
  {
    public string playerName;
    public string keyCode;
    public string text;
  }

  [System.Serializable]
  public struct PlayerJoinedMessage
  {
    public string playerId;
  }

  [System.Serializable]
  public struct PlayerLeftMessage
  {
    public string playerId;
  }


  PlayerToolTip[] playerToolTips = new PlayerToolTip[0];

  public event System.Action<string> OnBeforeModuleCompile;
  public event System.Action<string> OnScriptRuntimeError; // TODO um, should this just crash? should we even have this, with exceptions?
  public event System.Action<ModuleCompileError> OnModuleCompileError;
  public event System.Action<BehaviorLogItem> onBehaviorException;
  public event System.Action OnResetGame;
  public event System.Action<BehaviorLogItem> onBehaviorLogMessage;

  public event System.Action onBeforeVoosUpdate;

  List<VoosActor> latestActorsInSerializedOrder = new List<VoosActor>();
  bool orderedActorsDirty = true;

  HashSet<VoosActor> actorsNeedScriptSync = new HashSet<VoosActor>();

  HashSet<VoosActor> actorsToDestroyLocal = new HashSet<VoosActor>();
  HashSet<VoosActor> actorsToDestroy = new HashSet<VoosActor>();
  Queue<VoosActor> actorsToReplicate = new Queue<VoosActor>();

  // Cache for others to use.
  private Dictionary<string, VoosActor> actorsByName = new Dictionary<string, VoosActor>();

  // If this is not null, this is the pending request to set the player actor.
  private TransferPlayerControlRequest? transferPlayerControlRequest = null;

  // Cached reference to UserMain, lazily assigned.
  private UserMain __userMain;
  private UserMain userMain
  {
    get
    {
      if (__userMain == null)
      {
        __userMain = GameObject.FindObjectOfType<UserMain>();
      }
      return __userMain;
    }
  }
  // Cached reference to NavigationControls, lazily assigned.
  private NavigationControls __navigationControls;
  private NavigationControls navigationControls
  {
    get
    {
      if (__navigationControls == null)
      {
        __navigationControls = GameObject.FindObjectOfType<NavigationControls>();
      }
      return __navigationControls;
    }
  }

  // A reference-counting approach to ownership locking.
  Dictionary<VoosActor, int> localLockCounts = new Dictionary<VoosActor, int>();

  private VirtualPlayerManager virtualPlayerManager;

  HashSet<string> compiledModules = new HashSet<string>();

  enum State { Uninit, Init };
  State state = State.Uninit;

  // Certain data is accessed so often by JS that it's worth sync'ing just once and cached. When that data changes, we need to update that JS-land cache.
  HashSet<VoosActor> actorsToSyncToScript = new HashSet<VoosActor>();

  public IEnumerable<PlayerToolTip> GetToolTipsForPlayer(string playerName)
  {
    foreach (PlayerToolTip tip in playerToolTips)
    {
      if (tip.playerName == playerName)
      {
        yield return tip;
      }
    }
  }

  public IEnumerable<VoosActor> EnumerateActors()
  {
    foreach (var pair in actorsByName)
    {
      yield return pair.Value;
    }
  }

  public IEnumerable<VoosActor> EnumerateActorsWhere(System.Predicate<VoosActor> predicate)
  {
    foreach (var pair in actorsByName)
    {
      if (predicate.Invoke(pair.Value))
      {
        yield return pair.Value;
      }
    }
  }

  public bool DoesActorExist(string actorName)
  {
    return GetActor(actorName) != null;
  }

  public VoosActor GetActor(string actorName)
  {
    if (actorName.IsNullOrEmpty())
    {
      // Can happen for fields, like parent name.
      return null;
    }

    VoosActor foundInTable = null;
    if (actorsByName.TryGetValue(actorName, out foundInTable))
    {
      return foundInTable;
    }
    else
    {
      return null;
    }
  }

  void RpcLog(string msg)
  {
#if USE_PUN
    if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
    {
      Util.Log(msg);
    }
#endif
  }

  void OnEnable()
  {
    Util.FindIfNotSet(this, ref sceneController);
    Application.wantsToQuit += OnWantsToQuit;
    Application.quitting += () => Util.Log($"VoosEngine Application.quitting handler CALLLLEEEEDDDD");
    sceneController.OnBeforeReloadMainScene += Teardown;
    sceneController.onBeforeQuitToSplash += Teardown;
  }

  void OnDisable()
  {
    this.Teardown();
    Application.wantsToQuit -= OnWantsToQuit;
    sceneController.OnBeforeReloadMainScene -= Teardown;
    sceneController.onBeforeQuitToSplash -= Teardown;
  }

  bool OnWantsToQuit()
  {
    Util.Log($"VOOSQUIT CALLED");
    return true;
  }

  void Teardown()
  {
    if (state == State.Uninit)
    {
      return;
    }
    state = State.Uninit;

    /// We cannot actually destroy the entire objects here because there are still active references.
    foreach (VoosActor actor in EnumerateActors().ToArray())
    {
      DestroyLocalActor(actor);
    }
    photonViewIdTrash.Clear();
  }

  void OnActorsByNameChanged()
  {
    orderedActorsDirty = true;
  }

  void DestroyLocalActor(VoosActor actor)
  {
    if (actor == null)
    {
      return;
    }

    Debug.Assert(actor.GetName() != null, "DestroyLocalActor: null actor name");

    // Util.Log($"destroying {actor.name}");

    // Util.Log($"Destroying {actor.name}. is a replicant? {actor.GetNetworking().IsRemoteReplicant()}");
    globalUnreliableData.RemoveActor(actor);
    actorsByName.Remove(actor.GetName());
    OnActorsByNameChanged();
    // Important to do this after RemoveActor, since it uses the viewID.
    if (actor.reliablePhotonView != null)
    {
      photonViewIdTrash.Put(actor.reliablePhotonView.viewID);
      DestroyImmediate(actor.reliablePhotonView);
    }

    // Any last words?
    actor.PrepareToBeDestroyed();
    GameObject.Destroy(actor.gameObject);
  }

  [PunRPC]
  void RequestDestroyActorRPC(int viewId)
  {
#if USE_PUN
    if (state != State.Init)
    {
      Util.Log($"Ignoring RequestDestroyActorRPC because not inited.");
      return;
    }
    VoosActor actor = GetActorByViewId(viewId);
    if (actor != null)
    {
      actorsToDestroy.Add(actor);
    }
    else
    {
      Util.LogWarning($"Got RequestDestroyActorRPC for bad viewId: {viewId}");
    }
#endif
  }

  public void DestroyActorByName(string name)
  {
    DestroyActor(GetActor(name));
  }

  public void DestroyActor(VoosActor actor)
  {
    if (actor == null)
    {
      Util.LogWarning("Tried to destroy a null VoosActor?");
      return;
    }
    if (actor.IsBuiltinActor())
    {
      Util.Log("Can't destroy built-in actor.");
      return;
    }
    if (actor.IsLockedByAnother())
    {
      Util.Log($"Cannot destroy an actor locked by another");
      return;
    }

    if (actor.reliablePhotonView == null)
    {
      // This was already destroy-requested this frame. Ignore.
      Util.Log($"double destroy? {actor.name}");
      return;
    }
    int viewId = actor.reliablePhotonView.viewID;
    Debug.Assert(viewId >= 0, "DestroyActor: Negative view ID?");

    // TODO we should rethink this a bit. Shouldn't we treat this like any other
    // edit, where we *need* ownership first? Rather than request it?

    if (actor.IsLocallyOwned())
    {
      // Immediately destroy locally, then tell everyone else to.
      DestroyLocalActor(actor);

      // We end up getting this back, since we send with AllViaServer, but we
      // really just want *ordered* Others. It's OK.
      photonView.RPC("DestroyReplicantRPC", PhotonTargets.AllViaServer, viewId);
    }
    else
    {
      // Ask the owner to destroy.
      PhotonPlayer owner = actor.GetPhotonOwner();
      Debug.Assert(owner != null, "DestroyActor: null from GetPhotonOwner");
      photonView.RPC("RequestDestroyActorRPC", owner, viewId);
    }
  }
#if USE_PUN
  VoosActor GetActorByViewId(int viewId)
  {
    // TODO does photon have a faster way of doing this maybe?
    PhotonView view = PhotonView.Find(viewId);
    return view?.GetComponent<VoosActor>();
  }
#endif

  // Tell others to destory by viewID. There's a short period of time where
  // replicants don't have the right name yet, but they'll always have the
  // right viewID.
  [PunRPC]
  void DestroyReplicantRPC(int viewId, PhotonMessageInfo info)
  {
#if USE_PUN
    if (state != State.Init)
    {
      Util.Log($"Ignoring DestroyReplicantRPC because not inited.");
      return;
    }

    try
    {
      if (info.sender == PhotonNetwork.player)
      {
        // Ignore this from ourself. We end up getting this, since we send with AllViaServer, but we really just want *ordered* Others.
        return;
      }
      RpcLog($"received RPC to destroy ({viewId})");
      VoosActor actor = GetActorByViewId(viewId);
      // Don't destroy immediately. Often what happens is, we'll get a collision
      // message RPC immediately followed by a destroy RPC. The message RPC is
      // effectively queued for the next update, but the destroy RPC is
      // immediately applied. This means the scripts don't have a chance to
      // respond to the collision, because the 'other' is already gone. This
      // results in an error usually, but it's also not great because really,
      // they should get a chance to respond (what did I get hit by? maybe I
      // respond differently depending on that!). So, effectively apply all RPCs
      // in the same order as they are received.
      actorsToDestroyLocal.Add(actor);
    }
    catch (System.Exception e)
    {
      Util.LogError($"Exception during DestroyActorRPC, view ID {viewId}. Exception:\n{e.ToString()}");
      throw e;
    }
#endif
  }

  void PushPlayerActorStateToSerialized(ref RuntimeState runtime, Dictionary<string, VoosActor> entsByName)
  {
    using (InGameProfiler.Section("PushUnityToSerialized"))
    {
      // TODO wasteful to allocate a new array each time...
      List<VoosActor> playerActors = new List<VoosActor>();
      foreach (VoosActor ent in entsByName.Values)
      {
        if (ent.GetIsPlayerControllable())
        {
          playerActors.Add(ent);
        }
      }

      runtime.actors = new VoosActor.RuntimeState[playerActors.Count];

      for (int j = 0; j < playerActors.Count; j++)
      {
        runtime.actors[j] = VoosActor.RuntimeState.NewFrom(playerActors[j]);
      }

      runtime.gameTime = gameTime;
    }
  }

  internal InstantiatePrefab.Response InstantiatePrefabForScript(InstantiatePrefab.Request args)
  {
    try
    {
      Debug.Assert(!args.prefabUri.IsNullOrEmpty(), $"InstantiatePrefabForScript: Script wants to instantiate a prefab, but it did not provide a prefab URI to use. Creator name: {args.creatorName}");
      Debug.Assert(actorsByName.ContainsKey(args.creatorName), "InstantiatePrefabForScript: Invalid creatorName given: " + args.creatorName);

      // TEMP TEMP TODO we should actually treat this like a URI, cuz we may have scene-embedded prefabs too.
      // NOTE: this may be weird in multiplayer, since it will probably get replicated in the wrong position first.
      var uri = new System.Uri(args.prefabUri);
      VoosActor actor = null;
      if (uri.Scheme == "builtin")
      {
        actor = builtinPrefabLibrary.Get(uri.LocalPath).Instantiate(this, behaviorSystem, args.position, args.rotation, setupActor =>
        {
          setupActor.SetSpawnPosition(args.position);
          setupActor.SetSpawnRotation(args.rotation);
          setupActor.SetDisplayName($"{actor.GetDisplayName()}-Script-Instance");
          setupActor.SetPreferOffstage(false);
          setupActor.SetWasClonedByScript(true);
        });
      }
      else
      {
        throw new System.Exception($"Could not find actor prefab for URI {args.prefabUri}");
      }

      latestActorsInSerializedOrder.Add(actor);
      actor.lastTempId = (ushort)(latestActorsInSerializedOrder.Count - 1);

      return new InstantiatePrefab.Response
      {
        name = actor.GetName(),
        brainName = actor.GetBrainName(),
        actorId = actor.lastTempId
      };
    }
    catch (System.Exception e)
    {
      Util.LogError($"Exception while trying to InstantePrefabForScript. Returning blank. Exception: {e}");
      return new InstantiatePrefab.Response { name = "" };
    }
  }

  public PersistedState GetPersistedState()
  {
    PersistedState rv;
    rv.gameTime = gameTime;
    List<VoosActor.PersistedState> persistedStates = new List<VoosActor.PersistedState>();

    foreach (var pair in actorsByName)
    {
      if (pair.Value != null && pair.Value.transform == null)
      {
        Util.LogError($"An actor in actorsByName was not null, but its transform was...? Actor GO name: {pair.Value.name}");
      }

      if (pair.Value.ShouldPersist())
      {
        persistedStates.Add(VoosActor.PersistedState.NewFrom(pair.Value));
      }
    }

    rv.actors = persistedStates.ToArray();
    return rv;
  }

  public void SetPersistedState(PersistedState state)
  {
    gameTime = state.gameTime;

    // Destroy all existing entities.
    foreach (VoosActor actor in EnumerateActors())
    {
      Util.LogError($"destroy on set persisted state.. {actor.name}");
      Destroy(actor.gameObject);
    }

    // Phase 1: Spawn all new actors and build a table (for transform parent lookups)
    foreach (VoosActor.PersistedState actorData in state.actors)
    {
      // Note: because of a bug in an earlier version, sometimes an actor with an empty
      // name may be on the file. We can ignore those.
      if (actorData.name.IsNullOrEmpty())
      {
        Debug.LogWarning("File has actor with empty name (written by known bug in previous version). Ignoring.");
        continue;
      }

      // HACK - work around bug that left some files with placeholders saved in them.
      if (actorData.name.StartsWith(VoosActor.PLAYER_PLACEHOLDER_NAME_PREFIX))
      {
        continue;
      }

      // Bypass the CreateActor flow here - do NOT spam huge replicate RPC
      // messages..or onActorCreated calls...heh. Especially since we are doing
      // our special 2-pass approach.
      VoosActor actor = CreateLocalActor(actorData.position, actorData.rotation, PhotonNetwork.AllocateViewID());
      actor.SetName(actorData.name);
    }

    // Phase 2: Initialize all actors.
    foreach (VoosActor.PersistedState actorData in state.actors)
    {
      if (actorData.name.IsNullOrEmpty())
      {
        continue;
      }
      // HACK - work around bug that left some files with placeholders saved in them.
      if (actorData.name.StartsWith(VoosActor.PLAYER_PLACEHOLDER_NAME_PREFIX))
      {
        continue;
      }
      VoosActor actor = actorsByName[actorData.name];
      actor.UpdateFrom(actorData);
    }

    // Phase 3: Broadcast a ResetGame message.
    ResetGame();

    CheckForUnusedBehaviorDatabaseItems(this);

    this.state = State.Init;
  }

  public PlayerInitPayload GetPlayerInitPayload()
  {
    PlayerInitPayload rv;
    rv.actorViewIds = new int[actorsByName.Count];
    rv.actorStates = new VoosActor.PersistedState[actorsByName.Count];
    int i = 0;
    foreach (var entry in actorsByName)
    {
      if (entry.Value.reliablePhotonView != null)
      {
        rv.actorViewIds[i] = entry.Value.reliablePhotonView.viewID;
        rv.actorStates[i] = SaveActor(entry.Value);
        i++;
      }
    }
    Util.AssertAllUnique(rv.actorViewIds);
    rv.isRunning = GetIsRunning();
    return rv;
  }

  public void SetPlayerInitPayload(PlayerInitPayload payload)
  {
#if USE_PUN
    Debug.Assert(state == State.Uninit, "SetPlayerInitPayload before init'd?");
    Debug.Assert(actorsByName.Count == 0, "There should be no actors before we're initialized!!");

    Util.AssertAllUnique(payload.actorViewIds);
    for (int i = 0; i < payload.actorViewIds.Length; i++)
    {
      int viewId = payload.actorViewIds[i];

      if (PhotonView.Find(viewId) != null)
      {
        Util.LogError($"View ID that was given in init payload was already taken locally by game object: {PhotonView.Find(viewId).gameObject.name}");
        continue;
      }

      var state = payload.actorStates[i];

      VoosActor actor = CreateLocalActor(state.position, state.rotation, viewId);
      actor.SetName(state.name);
      actor.UpdateFrom(state);
    }

    // Intentionally call the RPC directly. To avoid triggering unnecesary RPCs to other clients (who already have the right value).
    SetIsRunningRPC(payload.isRunning);

    state = State.Init;
#endif
  }

  TickRequest CreateTickRequest()
  {
    using (InGameProfiler.Section("CreateTickRequest"))
    {
      TickRequest rv;
      rv.memCheckMode = MemCheckMode;
      rv.enableProfilingService = EnableProfilingFromScript;
      rv.runtimeState = new RuntimeState();
      PushPlayerActorStateToSerialized(ref rv.runtimeState, actorsByName);
      rv.deltaSeconds = Time.deltaTime;
      rv.messagesFromUnity = messagesFromUnity.ToArray();
      rv.operation = "tickWorld";
      messagesFromUnity.Clear();
      return rv;
    }
  }

  public bool HasModuleCompiledOnce(string moduleKey)
  {
    return compiledModules.Contains(moduleKey);
  }

  // Returns error messages if any. Will never return null.
  public bool SetModule(string moduleKey, string javascript)
  {
    V8InUnity.Native.StringFunction handleCompileError = msg =>
    {
      int lineNum = ExtractFirstLineNumberForModuleError(moduleKey, msg);
      var args = new ModuleCompileError { message = msg, moduleKey = moduleKey, lineNum = lineNum };
      OnModuleCompileError?.Invoke(args);
    };

    // TODO OPT: we can avoid extra compiles here by keeping a simple hash of
    // JS. Now that the behavior system is doing synchronous syncs, redundant
    // calls are more likely.
    OnBeforeModuleCompile?.Invoke(moduleKey);
    bool ok = V8InUnity.Native.SetModule(brainUid, moduleKey, javascript, handleCompileError);
    if (ok)
    {
      compiledModules.Add(moduleKey);
    }
    else
    {
      // To be safe, we should always have some valid module for the key. Other
      // code may expect it. So, if we've never compiled something, compile a
      // dummy script.
      if (!compiledModules.Contains(moduleKey))
      {
        bool backupOk = V8InUnity.Native.SetModule(brainUid, moduleKey, "// Dummy", handleCompileError);
        if (!backupOk)
        {
          throw new System.Exception("Could not compile backup dummy module? Major problems..");
        }
      }
    }
    return ok;
  }

  public bool Recompile(string js)
  {
    return V8InUnity.Native.ResetBrain(brainUid, js);
  }

  void ApplyVelocityChanges(VelocityChange[] changes, TorqueRequest[] torques)
  {
    foreach (var change in changes)
    {
      VoosActor ent;
      if (actorsByName.TryGetValue(change.entityName, out ent))
      {
        if (ent != null)
        {
          ent.RequestVelocityChange(change.delta);
        }
      }
      else
      {
        Debug.LogError("Script requested velocity change to entity " + change.entityName + ", but no such entity exists.");
      }
    }

    foreach (var req in torques)
    {
      VoosActor actor;
      if (actorsByName.TryGetValue(req.actorId, out actor) && actor != null)
      {
        actor.RequestTorque(req.torque);
      }
    }
  }

  [System.Serializable]
  struct RemoteMessage
  {
    // Use view ID instead of actor name. It's smaller and has other benefits.
    public int viewId;
    public string messageName;
    public string argsJson;
  }

  [System.Serializable]
  struct RemoteMessages
  {
    public RemoteMessage[] messages;
  }

  [PunRPC]
  void HandleMessagesToMyActorsRPC(string remoteMessagesJson)
  {
#if USE_PUN
    if (state != State.Init)
    {
      Util.Log($"Ignoring HandleMessagesToMyActorsRPC because not inited.");
      return;
    }

    RemoteMessages args = JsonUtility.FromJson<RemoteMessages>(remoteMessagesJson);
    foreach (RemoteMessage msg in args.messages)
    {
      if (msg.viewId == -1)
      {
        // Broadcast.
        EnqueueMessage(new ActorMessage { fromRemote = true, name = msg.messageName, targetActor = null, argsJson = msg.argsJson });
      }
      else
      {
        VoosActor target = GetActorByViewId(msg.viewId);

        // If actor DNE, assume we destroyed it already and ignore the message.
        if (target == null)
        {
          continue;
        }

        // Don't worry about if the actor is no longer owned by us. The script
        // layer will take care of that.
        EnqueueMessage(new ActorMessage { fromRemote = true, name = msg.messageName, targetActor = target.GetName(), argsJson = msg.argsJson });
      }
    }
#endif
  }

  List<RemoteMessage> remoteMessagesTemp = new List<RemoteMessage>();

  void SendMessagesToRemotes(ActorMessage[] messages)
  {
    // Sort by owner ID
    Array.Sort(messages, Comparer<ActorMessage>.Create((x, y) =>
    {
      // If it's an untargeted broadcast, sort them as -1
      int xId = -1;
      int yId = -1;
      if (!x.targetActor.IsNullOrEmpty())
      {
        xId = actorsByName[x.targetActor].GetPhotonOwnerId();
      }
      if (!y.targetActor.IsNullOrEmpty())
      {
        yId = actorsByName[y.targetActor].GetPhotonOwnerId();
      }
      return xId.CompareTo(yId);
    }));

    // Now send out one RPC per unique owner.
    Util.ForSortedGroups(messages,
    msg => msg.targetActor.IsNullOrEmpty() ? -1 : actorsByName[msg.targetActor].GetPhotonOwnerId(),
    (ownerId, messagesForOwner) =>
    {
      if (ownerId == -1)
      {
        // Broadcast to others
        remoteMessagesTemp.Clear();
        foreach (ActorMessage msg in messagesForOwner)
        {
          Debug.Assert(msg.targetActor.IsNullOrEmpty(), "broadcasted messages should have no target actor");
          remoteMessagesTemp.Add(new RemoteMessage
          {
            viewId = -1,
            messageName = msg.name,
            argsJson = msg.argsJson
          });
#if USE_PUN
          if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
          {
            Util.Log($"Broadcasting message {msg.name}");
          }
#endif
        }
        RemoteMessages payload = new RemoteMessages { messages = remoteMessagesTemp.ToArray() };
        photonView.RPC("HandleMessagesToMyActorsRPC", PhotonTargets.Others, JsonUtility.ToJson(payload));
      }
      else
      {
        // Targetted to specific owner
        remoteMessagesTemp.Clear();
        PhotonPlayer owner = PhotonPlayer.Find(ownerId);

        foreach (ActorMessage msg in messagesForOwner)
        {
          Debug.Assert(msg.name != "ResetGame", "SendMessagesToRemotes: JS side should not be forwarding ResetGame messages..");
          VoosActor target = actorsByName[msg.targetActor];
          Debug.Assert(ownerId == target.GetPhotonOwnerId(), "SendMessagesToRemotes: ownerId did not match target's actual owner");
          Debug.Assert(owner == target.GetPhotonOwner(), "SendMessagesToRemotes: owner did not match target's actual owner");
          remoteMessagesTemp.Add(new RemoteMessage
          {
            viewId = target.GetPrimaryPhotonViewId(),
            messageName = msg.name,
            argsJson = msg.argsJson
          });
#if USE_PUN
          if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
          {
            Util.Log($"Sending voos message {msg.name} to {owner.NickName}");
          }
#endif
        }

        RemoteMessages payload = new RemoteMessages { messages = remoteMessagesTemp.ToArray() };
        photonView.RPC("HandleMessagesToMyActorsRPC", owner, JsonUtility.ToJson(payload));
      }
    });
  }

  void HandleV8RuntimeError(string message)
  {
    // Surface this to the user via other means.
    OnScriptRuntimeError?.Invoke(message);
  }

  void HandleV8SystemLog(string message)
  {
    // Native already logs this to Unity log, so ignore it.
  }

  V8InUnity.Native.UpdateCallbacks GetNativeUpdateCallbacks()
  {
    return new V8InUnity.Native.UpdateCallbacks
    {
      handleError = HandleV8RuntimeError,
      handleLog = HandleV8SystemLog,
      callService = services.CallService,
      getActorBoolean = GetActorBoolean,
      setActorBoolean = SetActorBoolean,
      getActorVector3 = GetActorVector3,
      setActorVector3 = SetActorVector3,
      getActorQuaternion = GetActorQuaternion,
      setActorQuaternion = SetActorQuaternion,
      setActorString = SetActorString,
      getActorString = GetActorString,
      setActorFloat = SetActorFloat,
      getActorFloat = GetActorFloat
    };
  }

  public Util.Maybe<TResponse> CommunicateWithAgent<TRequest, TResponse>(TRequest request)
  {
    return V8InUnity.Native.UpdateAgent<TRequest, TResponse>(brainUid, agentUid, request, GetNativeUpdateCallbacks());
  }

  void MaybeShowActorCountWarning(int currentNumActors)
  {
    if (warnedAboutActorCount)
    {
      return;
    }

    if (currentNumActors > WarningActorCount)
    {
      popups.Show($"Warning: There are currently {currentNumActors} actors, and the maximum allowed is {MaxActors}.\nYou may experience performance or connection issues, especially in multiplayer.\nWe're hard at work on optimizing and increasing these limits - apologies!", "OK", () => { });
      warnedAboutActorCount = true;
    }
  }

  void GetActorBoolean(ushort actorId, ushort fieldId, out bool valueOut)
  {
    valueOut = false;
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    valueOut = latestActorsInSerializedOrder[actorId].GetBooleanField(fieldId);
  }

  void SetActorBoolean(ushort actorId, ushort fieldId, bool newValue)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetBooleanField(fieldId, newValue);
  }

  void GetActorFloat(ushort actorId, ushort fieldId, out float valueOut)
  {
    valueOut = 0f;
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    valueOut = latestActorsInSerializedOrder[actorId].GetFloatField(fieldId);
  }

  void SetActorFloat(ushort actorId, ushort fieldId, float newValue)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetFloatField(fieldId, newValue);
  }

  string GetActorString(ushort actorId, ushort fieldId)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return null;
    }

    // Never return null - we consider null and empty to be the same.
    return latestActorsInSerializedOrder[actorId].GetStringField(fieldId) ?? "";
  }

  void SetActorString(ushort actorId, ushort fieldId, string newValue)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetStringField(fieldId, newValue);
  }

  public Color GetActorColor(ushort actorId, ushort fieldId)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return Color.white;
    }

    return latestActorsInSerializedOrder[actorId].GetColorField(fieldId);
  }

  public void SetActorColor(ushort actorId, ushort fieldId, Color newValue)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetColorField(fieldId, newValue);
  }

  public delegate void ActorVector3Getter(ushort tempActorId, ushort fieldId, out float xOut, out float yOut, out float zOut);
  public delegate void ActorVector3Setter(ushort tempActorId, ushort fieldId, float newX, float newY, float newZ);


  void GetActorVector3(ushort actorId, ushort fieldId, out float xOut, out float yOut, out float zOut)
  {
    xOut = 0f;
    yOut = 0f;
    zOut = 0f;

    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    Vector3 rv = latestActorsInSerializedOrder[actorId].GetVector3Field(fieldId);
    xOut = rv.x;
    yOut = rv.y;
    zOut = rv.z;
  }

  void SetActorVector3(ushort actorId, ushort fieldId, float newX, float newY, float newZ)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetVector3Field(fieldId, new Vector3(newX, newY, newZ));
  }

  void GetActorQuaternion(ushort actorId, ushort fieldId, out float xOut, out float yOut, out float zOut, out float wOut)
  {
    xOut = 0f;
    yOut = 0f;
    zOut = 0f;
    wOut = 0f;

    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    Quaternion rv = latestActorsInSerializedOrder[actorId].GetQuaternionField(fieldId);
    xOut = rv.x;
    yOut = rv.y;
    zOut = rv.z;
    wOut = rv.w;
  }

  internal void NotifyNameChange(VoosActor voosActor, string oldName)
  {
    if (oldName != null && actorsByName.ContainsKey(oldName))
    {
      actorsByName.Remove(oldName);
    }
    if (voosActor.GetName() != null)
    {
      actorsByName[voosActor.GetName()] = voosActor;
    }
    OnActorsByNameChanged();
  }

  void SetActorQuaternion(ushort actorId, ushort fieldId, float newX, float newY, float newZ, float newW)
  {
    if (actorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {actorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return;
    }

    latestActorsInSerializedOrder[actorId].SetQuaternionField(fieldId, new Quaternion(newX, newY, newZ, newW));
  }

  void PumpQueuedCollisions(NetworkWriter writer)
  {
    writer.Write((int)queuedCollisions.Count);
    foreach (var msg in queuedCollisions)
    {
      int receiverId = msg.receiver.lastTempId;
      int otherId = msg.other.lastTempId;
      writer.Write((ushort)receiverId);
      writer.Write((ushort)otherId);
      writer.Write((bool)msg.isEnter);
    }
    // Magic number sanity check.
    writer.Write((int)536);
    queuedCollisions.Clear();

    // Terrain collisions
    writer.Write((int)queuedTerrainCollisions.Count);
    foreach (var msg in queuedTerrainCollisions)
    {
      int receiverId = msg.receiver.lastTempId;
      writer.Write((ushort)receiverId);
      writer.Write((ushort)msg.style);
    }
    // Magic number sanity check.
    writer.Write((int)451);
    queuedTerrainCollisions.Clear();
  }

  bool RunVoosUpdate()
  {
    MaybeShowActorCountWarning(actorsByName.Count);
    using (InGameProfiler.Section("Actors OnPreVoosUpdate"))
    {
      foreach (VoosActor actor in EnumerateActors())
      {
        actor.OnPreVoosUpdate();
      }
    }

    var writer = new NetworkWriter(updateAgentByteBuffer);
    SerializeOrderedActors(writer);
    SerializeActorStateSync(writer);

    using (InGameProfiler.Section("PumpQueuedCollisions"))
    {
      PumpQueuedCollisions(writer);
    }

    var callbacks = GetNativeUpdateCallbacks();
    Util.Maybe<TickResponse> maybeResponse = V8InUnity.Native.UpdateAgent<TickRequest, TickResponse>(
      brainUid, agentUid,
      CreateTickRequest(),
      updateAgentByteBuffer,
      callbacks);

    if (maybeResponse.IsEmpty())
    {
      return false;
    }

    TickResponse response = maybeResponse.Get();

    using (InGameProfiler.Section("handleResponse"))
    {
      try
      {
        playerToolTips = response.playerToolTips;
        var reader = new NetworkReader(updateAgentByteBuffer);
        DeserializeChangedMemoryActors(reader);
        ApplyVelocityChanges(response.velocityChanges, response.torqueRequests);
        ReplicateActors();
        SendMessagesToRemotes(response.messagesToRemoteActors);
        TryFulfilTransferPlayerControlRequest();

        // Send messages to Unity from script.
        foreach (ActorMessage msg in response.messagesToUnity)
        {
          if (msg.targetActor == null || msg.targetActor.Length == 0)
          {
            if (msg.name == "ResetTriggeredByHandler")
            {
              OnResetTriggeredByHandler();
            }
            else
            {
              Util.LogError($"Unknown system message from VOOS to Unity: ${msg.name}. Args: ${msg.argsJson}");
            }
          }
          else if (actorsByName.ContainsKey(msg.targetActor))
          {
            VoosActor target = actorsByName[msg.targetActor];
            target.HandleMessageFromScript(msg);
          }
          else
          {
            Debug.LogWarning($"Message from script targetted at {msg.targetActor}, but the actor no longer exists.");
          }
        }

        // Apply all destroy requests
        foreach (VoosActor actor in actorsToDestroy)
        {
          DestroyActor(actor);
        }
        actorsToDestroy.Clear();
        foreach (VoosActor actor in actorsToDestroyLocal)
        {
          DestroyLocalActor(actor);
        }
        actorsToDestroyLocal.Clear();
      }
      catch (System.Exception e)
      {
        Debug.LogError($"Got exception while handling V8 update response: {e.ToString()}. The response JSON: {JsonUtility.ToJson(response)}");
        throw e;
      }
    }
    return true;
  }

  public void MarkActorForScriptSync(VoosActor actor)
  {
    actorsNeedScriptSync.Add(actor);
  }

  private void SerializeOrderedActors(NetworkWriter writer)
  {
    using (InGameProfiler.Section("OrderActorsAndSerialize"))
    {
      writer.WriteVoosBoolean(orderedActorsDirty);
      if (orderedActorsDirty)
      {
        orderedActorsDirty = false;
        latestActorsInSerializedOrder.Clear();

        writer.Write((int)actorsByName.Count);
        foreach (var pair in actorsByName)
        {
          writer.WriteUtf16(pair.Key);
          latestActorsInSerializedOrder.Add(pair.Value);
          pair.Value.lastTempId = (ushort)(latestActorsInSerializedOrder.Count - 1);
        }

        // Sanity sentinel
        writer.Write((byte)42);
      }
    }
  }

  private void SerializeActorStateSync(NetworkWriter writer)
  {
    using (InGameProfiler.Section("SerializeActorStateSync"))
    {
      writer.Write((int)actorsNeedScriptSync.Count);
      foreach (var actor in actorsNeedScriptSync)
      {
        // It's possible this actor is no longer valid..ie. it was JUST destroyed.
        if (actor == null || !actorsByName.ContainsKey(actor.GetName()))
        {
          // Write -1, indicating to skip
          writer.Write(ushort.MaxValue);
          continue;
        }
        writer.Write(actor.lastTempId);
        actor.SerializeForScriptSync(writer);
      }
      actorsNeedScriptSync.Clear();

      // Sanity sentinel
      writer.Write((byte)43);
    }
  }

  string guid = null;

  public void Awake()
  {
    guid = Util.Generate32CharGuid();

    GameObject sceneObj = new GameObject("CurrentVoosScene");
    scene = sceneObj.AddComponent<VoosScene>();
    photonView = PhotonView.Get(this);
    Debug.Assert(photonView != null, "VoosEngine has no photon view?");

    Util.FindIfNotSet(this, ref assetCache);
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref terrainSystem);
    Util.FindIfNotSet(this, ref gbStage);
    Util.FindIfNotSet(this, ref builtinPrefabLibrary);

    GameUiMain gameUiMain = null;
    PlayerControlsManager playerControlsManager = null;
    Util.FindIfNotSet(this, ref gameUiMain);
    Util.FindIfNotSet(this, ref soundEffectSystem);
    Util.FindIfNotSet(this, ref particleEffectSystem);
    Util.FindIfNotSet(this, ref virtualPlayerManager);
    Util.FindIfNotSet(this, ref playerControlsManager);
    Util.FindIfNotSet(this, ref virtualPlayerManager);

    virtualPlayerManager.onVirtualPlayerJoined += OnVirtualPlayerJoined;
    virtualPlayerManager.onVirtualPlayerLeft += OnVirtualPlayerLeft;

    services = new V8InUnity.Services(terrainSystem, this, gameUiMain, soundEffectSystem, particleEffectSystem, virtualPlayerManager, playerControlsManager, gbStage);
#if USE_PUN
    Debug.Assert(PhotonNetwork.MAX_VIEW_IDS >= MaxActors * 2, "Not enough Photon view IDs for the max actors allowed. We need at least 2x, because destruction is deferred, so it's possible upon reset that we actually need 2x the outstanding IDs, which will immediately drop back down to 1x.");
#endif
    if (consoleInstance != null)
    {
      Debug.LogWarning($"There are multiple VoosEngine instances in the scene. Don't expect console commands to work predictably!");
    }
    consoleInstance = this;

    // This doesn't reset upon scene load - make sure it respects our wishes.
    Physics.autoSimulation = isRunning;

    behaviorSystem.onBrainsHandlingCollisionsChanged += onBrainsHandlingCollisionsChanged;
  }

  void onBrainsHandlingCollisionsChanged()
  {
    foreach (var actor in EnumerateActors())
    {
      actor.UpdateCollisionHandling();
    }
  }

  void DeserializeChangedMemoryActors(NetworkReader reader)
  {
    using (InGameProfiler.Section("DeserializeChangedMemoryActors"))
    {
      ushort numChanges = reader.ReadUInt16();
      for (int i = 0; i < numChanges; i++)
      {
        ushort actorTempId = reader.ReadUInt16();
        VoosActor actor = latestActorsInSerializedOrder[actorTempId];
        actor.MarkMemoryAsDirty();
      }
    }
  }

  void UpdateMemorySync()
  {
    if (Time.realtimeSinceStartup < (lastMemoryRPCTime + (1.0f / memoryRPCsPerSecond)))
    {
      return;
    }

    int sec = ((int)Time.realtimeSinceStartup) % MEMORY_STATS_SECONDS;
    if (currentMemoryStatSecond != sec)
    {
      memoryRPCQueueCount = memoryUpdateQueue.Count;
      currentMemoryStatSecond = sec;
      memoryStats[currentMemoryStatSecond] = 0;
    }

    while (memoryUpdateQueue.First != null)
    {
      var entry = memoryUpdateQueue.First;
      memoryUpdateQueue.RemoveFirst();

      if (entry.Value != null &&
        entry.Value.reliablePhotonView != null &&
        entry.Value.reliablePhotonView.isMine)
      {
        //Only increment this if we actually sent a message.
        lastMemoryRPCTime = Time.realtimeSinceStartup;
        //Debug.Log(Time.time + " : rpc " + entry.Value.gameObject.name);
        string memoryJson = entry.Value.GetMemoryJsonSlow();
        entry.Value.reliablePhotonView.RPC(
          "SetMemoryRPC", PhotonTargets.Others, memoryJson);
        memoryStats[currentMemoryStatSecond] += 2 + memoryJson.Length;

        // Only send one RPC per frame. So break out of the dequeue loop.
        break;
      }
    }
  }

  int numVoosUpdates = 0;
  SD.Stopwatch voosUpdateWatch = new SD.Stopwatch();

  // After every VoosUpdate, this will be called with the number of milliseconds
  // the update took.
  public event System.Action<float> onVoosUpdateTiming;

  void Update()
  {
    if ((isRunning || singleStepQueued)
      && behaviorSystem.IsInitialized())
    {
      singleStepQueued = false;
      gameTime += Time.deltaTime;
      onBeforeVoosUpdate?.Invoke();
      using (InGameProfiler.Section("VoosUpdate"))
      {
        long t0 = voosUpdateWatch.ElapsedTicks;
        voosUpdateWatch.Start();

        bool ok = RunVoosUpdate();
        if (!ok)
        {
          throw new Exception("Voos Engine runtime error - cannot recover!");
        }

        voosUpdateWatch.Stop();
        long t1 = voosUpdateWatch.ElapsedTicks;
        double TicksPerMilli = SD.Stopwatch.Frequency / 1000.0;
        float ms = (float)((t1 - t0) / TicksPerMilli);
        onVoosUpdateTiming?.Invoke(ms);
        numVoosUpdates++;
      }

      // TODO should we run mem sync even if we're paused?
      UpdateMemorySync();
    }

    UpdateGlobalLights();
  }

  public bool GetIsRunning()
  {
    return isRunning;
  }

  [PunRPC]
  void SetIsRunningRPC(bool running)
  {
    if (state != State.Init)
    {
      Util.Log($"Ignoring SetIsRunningRPC because not inited.");
      return;
    }

    Physics.autoSimulation = running;
    isRunning = running;
    gbStage.SetIsRunning(running);
    foreach (VoosActor actor in EnumerateActors())
    {
      actor.SetRunning(running);
    }
  }

  public void HackyForceSetIsRunning(bool running)
  {
    SetIsRunningRPC(running);
  }

  public void SetIsRunning(bool running)
  {
    if (running == isRunning)
    {
      return;
    }
#if USE_PUN
    photonView.RPC("SetIsRunningRPC", PhotonTargets.AllViaServer, running);
#else
    SetIsRunningRPC(running);
#endif
  }

  void OnResetTriggeredByHandler()
  {
    OnResetGame?.Invoke();

    // Optimization: ResetGame is not remote-forwarded, so do this via RPC.
    photonView.RPC("ResetGameRPC", PhotonTargets.Others);
  }

  void ResetGameLocal()
  {
    BroadcastMessageNoArgs("ResetGame");

    if (!isRunning)
    {
      // To see the effects of reset, we have to run at least one frame.
      singleStepQueued = true;
    }

    OnResetGame?.Invoke();
  }

  [PunRPC]
  void ResetGameRPC()
  {
    if (state != State.Init)
    {
      Util.Log($"Ignoring ResetGameRPC because not inited.");
      return;
    }
    ResetGameLocal();
  }

  // Triggered by GUI/keyboard.
  public void ResetGame()
  {
    ResetGameLocal();
    photonView.RPC("ResetGameRPC", PhotonTargets.Others);
  }

  public void EnqueueMessage(ActorMessage entry)
  {
    messagesFromUnity.Add(entry);
  }

  public void SendMessage<ArgsType>(string messageName, string targetEntityName, ArgsType args)
  {
    messagesFromUnity.Add(new ActorMessage { name = messageName, targetActor = targetEntityName, argsJson = JsonUtility.ToJson(args) });
  }

  // NOTE: broadcast includes offstage actors.
  public void BroadcastMessage<ArgsType>(string messageName, ArgsType args)
  {
    messagesFromUnity.Add(new ActorMessage { name = messageName, targetActor = null, argsJson = JsonUtility.ToJson(args) });
  }

  // NOTE: broadcast includes offstage actors.
  public void BroadcastMessageNoArgs(string messageName)
  {
    messagesFromUnity.Add(new ActorMessage { name = messageName, targetActor = null, argsJson = null });
  }

  // This must be unique, per session, per save, over all clients, etc. etc.
  public string GenerateUniqueId()
  {
    return System.Guid.NewGuid().ToString("N");
  }

  void ReplicateActors()
  {
    while (actorsToReplicate.Count > 0)
    {
      VoosActor actor = actorsToReplicate.Dequeue();
      if (actor == null) continue;

      // TODO TODO json is probably wasteful here. We should just binary serialize it.
      string serializedJson = JsonUtility.ToJson(SaveActor(actor));

      try
      {
        photonView.RPC(
          "CreateActorReplicateRPC",
          PhotonTargets.AllViaServer,
          actor.reliablePhotonView.viewID,
          serializedJson);
      }
      catch (System.Exception e)
      {
        throw new System.Exception($"While trying to RPC-replicate actor {actor.name}", e);
      }
    }
  }

  // 'setupActor' is required! It is called right before we send it to other
  // clients for replication, so the more accurate it is, the less likely there
  // will be sync issues. Otherwise, any other mutations you make to the new
  // actor will have to wait for the next reliabe replication, which is likely
  // to take many frames.
  public VoosActor CreateActor(Vector3 position, Quaternion rotation, System.Action<VoosActor> setupActor, string actorName = null)
  {
    // We can't use PhotonNetwork.Instantiate, because we need to make the
    // Initialize call. So we'll just replicate its logic here: Create a local
    // actor immediately, then just tell everyone else to create a replicate via
    // RPC.

    VoosActor actor = CreateLocalActor(position, rotation, PhotonNetwork.AllocateViewID());
    actor.SetName(actorName ?? GenerateUniqueId());
    actor.SetBrainName(DefaultBrainUid);
    // This actor is not immediately in the JS system yet, so we need to have
    // some valid memory in C# land for our SaveActor call later. setupActor may
    // or may not do that, so just do it by default.
    actor.SetMemoryJson("{}");
    setupActor(actor);

    // Defer replication until after the next VOOS update. This gives VOOS a
    // chance to do its own setup, so the replications get spawned more
    // accurately. For example, laser bolts spawn and immediately get a forward
    // velocity - if we replicated it before, the laser would sit still for a
    // bit on remotes!
    actorsToReplicate.Enqueue(actor);

    onActorCreated?.Invoke(actor);
    return actor;
  }

  // Why take position/rotation, when users can set it later? One good reason
  // is: Physics. If you create things at origin or something, there's a chance
  // that Unity's physics will register collisions, etc. etc. - better to not
  // worry about that.
  VoosActor CreateLocalActor(Vector3 position, Quaternion rotation, int viewId)
  {
    VoosActor actor = GameObject.Instantiate(actorPrefab, position, rotation).GetComponent<VoosActor>();
    Debug.Assert(behaviorSystem != null, "CreateLocalActor: behaviorSystem is null?");
    actor.Initialize(scene, this, assetCache, behaviorSystem);
    actor.reliablePhotonView.viewID = viewId;
    globalUnreliableData.AddActor(actor);
    return actor;
  }

  // NOTE: We do NOT include the actor's name/UID here. Because..that will get
  // sync'd over the network through regular replication. Of course, in the
  // future, we may not regularly sync that stuff, so we may need to change how
  // that's done.
  [PunRPC]
  void CreateActorReplicateRPC(int viewId, string serializedJson, PhotonMessageInfo info)
  {
    if (state != State.Init)
    {
      Util.Log($"Ignoring CreateActorReplicateRPC because not inited.");
      return;
    }

    try
    {
      if (info.sender == PhotonNetwork.player)
      {
        // Ignore this from ourself. We end up getting this, since we send with AllViaServer, but we really just want *ordered* Others.
        return;
      }
      RpcLog($"received CreateActorReplicateRPC, viewId {viewId}");
      VoosActor.PersistedState serialized = JsonUtility.FromJson<VoosActor.PersistedState>(serializedJson);
      VoosActor actor = CreateLocalActor(serialized.position, serialized.rotation, viewId);
      actor.SetName(serialized.name);
      actor.UpdateFrom(serialized);
      Debug.Assert(actor.GetNetworking().IsRemoteReplicant(), "CreateActorReplicateRPC: created actor is not a replicant?");
      Debug.Assert(!actor.GetName().IsNullOrEmpty(), "CreateActorReplicateRPC: created actor has no name");
    }
    catch (System.Exception e)
    {
      Util.LogError($"Exception while handling CreateActorReplicateRPC, viewID {viewId}");
      throw e;
    }
  }

  public PlayerBodyParts GetPlayerBodyPartsPrefab()
  {
    return playerBodyParts;
  }

  public struct CopyPasteActorRequest
  {
    public VoosActor source;
    public Vector3 pastedPosition;
    public Quaternion pastedRotation;
    public string pastedDisplayName;
  }

  public List<VoosActor> CopyPasteActors(IEnumerable<CopyPasteActorRequest> requests)
  {
    List<VoosActor> pastedActors = new List<VoosActor>();
    List<VoosActor> sourceActors = new List<VoosActor>();

    // First build the name dictionary mapping old names to new names.
    foreach (CopyPasteActorRequest request in requests)
    {
      sourceActors.Add(request.source);
    }
    Dictionary<string, string> nameDict = BuildClonedActorNameDict(sourceActors);

    // Now make the requested copies.
    foreach (CopyPasteActorRequest request in requests)
    {
      string pastedName = nameDict[request.source.GetName()];
      Debug.Assert(pastedName != null, "pastedName was null?");
      VoosActor pastedActor = CreateActor(request.pastedPosition, request.pastedRotation, clone =>
      {
        VoosActor.PersistedState state = VoosActor.PersistedState.NewFrom(request.source);
        // We want to use the new clone's newly generated name.
        state.name = pastedName;
        state.displayName = request.pastedDisplayName;
        state.position = request.pastedPosition;
        state.rotation = request.pastedRotation;

        // IMPORTANT: If the source is already part of a clone group, use that! Otherwise, start a new group.
        if (request.source.GetCloneParentActor() == null)
        {
          state.cloneParent = request.source.GetName();
        }
        else
        {
          state.cloneParent = request.source.GetCloneParent();
        }

        clone.UpdateFrom(state);
      }, pastedName);
      pastedActor.SetSpawnPosition(request.pastedPosition);
      pastedActor.SetSpawnRotation(request.pastedRotation);
      pastedActors.Add(pastedActor);
    }

    // Pass 2: update transform parents. This needs to happen after because actors may reference parents
    // that are also being copied, so the only way to be sure that everything actually exists is to do
    // this as a second step.
    foreach (VoosActor pastedActor in pastedActors)
    {
      string remappedParent;
      if (pastedActor.GetTransformParent() != null && nameDict.TryGetValue(pastedActor.GetTransformParent(), out remappedParent))
      {
        pastedActor.SetTransformParent(remappedParent);
      }
      if (pastedActor.GetSpawnTransformParent() != null && nameDict.TryGetValue(pastedActor.GetSpawnTransformParent(), out remappedParent))
      {
        pastedActor.SetSpawnTransformParent(remappedParent);
      }
    }

    return pastedActors;
  }

  public bool GetIsInPlayMode()
  {
    return !inEditMode;
  }

  public bool GetIsInEditMode()
  {
    return inEditMode;
  }

  public void NotifyEditModeToggled(bool on)
  {
    inEditMode = on;

    // If we don't do this here, in addition to the update agent loop, we get
    // flickering in certain cases (like the player ghost).
    foreach (VoosActor actor in EnumerateActors())
    {
      actor.NotifyEditModeToggled();
    }
  }

  void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
#if USE_PUN
    if (stream.isWriting)
    {
      stream.SendNext(gameTime);
    }
    else
    {
      gameTime = (float)stream.ReceiveNext();
    }
#endif
  }

  public void HandleTouches(VoosActor unityEventReceiver, Collision collision, bool isEnterMessage)
  {
    // This does often result in multiple "HandleTouch" calls, since you can
    // have more than one contact per collider. That's OK - collisions should
    // always be post-filtered - but I honestly don't remember why I go through
    // contacts instead of just going with collision.collider. Maybe it was
    // related to parent-child hierarchies?
    for (int i = 0; i < collision.contactCount; i++)
    {
      ContactPoint cp = collision.GetContact(i);
      HandleTouch(unityEventReceiver, cp.otherCollider, isEnterMessage);
    }
  }

  public void HandleTerrainCollision(VoosActor unityEventReceiver, TerrainManager terrain, ContactPoint contact, bool isEnterMessage)
  {
    bool isReceiverOffstageEffective = unityEventReceiver.GetIsOffstageEffective();
    if (isReceiverOffstageEffective)
    {
      return;
    }

    if (!unityEventReceiver.IsLocallyOwned())
    {
      // The chances that we simulate a remote-terrain collision and the owner does NOT seems pretty low.
      // Terrain does not move or anything. So, let the owner worry about this and **spare the network traffic**.
      return;
    }

    var style = terrain != null
      ? terrain.GetCellValue(TerrainManager.GetCellForRayHit(contact.point, contact.normal)).style
      // TEMP If it is the ground, consider it a grass block.
      : TerrainManager.BlockStyle.Grass;
    queuedTerrainCollisions.Add(new QueuedTerrainCollision { receiver = unityEventReceiver, style = (byte)style });
  }

  // Temporary solution because we don't get precise collision info from triggers..
  public void HandleUnknownTerrainCollision(VoosActor unityEventReceiver, bool isEnter)
  {
    bool isReceiverOffstageEffective = unityEventReceiver.GetIsOffstageEffective();
    if (isReceiverOffstageEffective)
    {
      return;
    }

    if (!unityEventReceiver.IsLocallyOwned())
    {
      // The chances that we simulate a remote-terrain collision and the owner does NOT seems pretty low.
      // Terrain does not move or anything. So, let the owner worry about this and **spare the network traffic**.
      return;
    }

    byte unknownStyle = 255;
    queuedTerrainCollisions.Add(new QueuedTerrainCollision { receiver = unityEventReceiver, style = unknownStyle });
  }

  public void HandleTouch(VoosActor unityEventReceiver, Collider otherCollider, bool isEnterMessage)
  {
    bool isReceiverOffstageEffective = unityEventReceiver.GetIsOffstageEffective();
    if (isReceiverOffstageEffective)
    {
      return;
    }

    VoosActor other = otherCollider.GetComponentInParent<VoosActor>();
    if (other == null)
    {
      return;
    }

    // DO NOT send any messages if either or both are off-stage.
    if (isReceiverOffstageEffective || other.GetIsOffstageEffective())
    {
      return;
    }

    // DO NOT send any messages if both are remote replicants.
    if (unityEventReceiver.GetNetworking().IsRemoteReplicant() && other.GetNetworking().IsRemoteReplicant())
    {
      return;
    }

    // Subtle optimization logic here..
    if (isEnterMessage)
    {
      // For enters, we always send them, no matter if the actor is remote or
      // local. This is because often times, a collision can happen on one
      // client but not happen on another. We want to favor *happening* and not
      // miss any. This can result in double collisions, but that's OK (scripts
      // should be robust to that anyway, using cooldowns to deal with it).
      queuedCollisions.Add(new QueuedCollision { receiver = unityEventReceiver, other = other, isEnter = isEnterMessage });

      // TODO we could optimize this further with a specialized RPC, if it's a
      // remote actor. Instead of waiting for VoosEngine to report it out as a
      // generic message, etc.
    }
    else
    {
      // But for stays, we only want to send them to **local actors**. This is
      // an optimization. Otherwise, if we're touching another player actor for
      // example, we would spam RPCs to the player. BUT unlike for enters, if
      // they are just continuously contacting for a while, it's pretty likely
      // that the owning client is simulating these stays as well. The
      // simulation has likely settled into a steady state, so we're likely to
      // converge to the same stay state. In that case, I'll send stays to my
      // actor and the other client can do it for theirs. No extra RPCs
      // required!
      if (unityEventReceiver.IsLocallyOwned())
      {
        queuedCollisions.Add(new QueuedCollision { receiver = unityEventReceiver, other = other, isEnter = isEnterMessage });
      }
    }
  }

  public enum OwnRequestReason : byte
  {
    Default = 0,
    Collision = 1
  }

  public void RequestOwnership(VoosActor actor, OwnRequestReason reason)
  {
    if (actor == null) { return; }
    if (actor.IsLocallyOwned()) { return; }
    if (actor.IsLockedByAnother()) { return; }

    photonView.RPC("RequestOwnershipRPC",
      actor.GetPhotonOwner(),
      actor.GetPrimaryPhotonViewId(),
      (byte)reason);
  }

  [PunRPC]
  void RequestOwnershipRPC(int viewId, byte reasonByte, PhotonMessageInfo info)
  {
#if USE_PUN
    if (state != State.Init)
    {
      Util.Log($"Ignoring RequestOwnershipRPC because not inited.");
      return;
    }
    PhotonView view = PhotonView.Find(viewId);
    if (view == null)
    {
      // We destroyed this locally already - just ignore.
      return;
    }

    var reason = (OwnRequestReason)reasonByte;
    PhotonPlayer requestingPlayer = info.sender;
    VoosActor actor = view.GetComponent<VoosActor>();

    if (actor == null)
    {
      Util.Log($"OnOwnershipRequest called for non-actor object: {view.name}. Assuming we will NOT transfer ownership.");
      return;
    }

    if (userMain != null && userMain.GetPlayerActor() == actor && reason == OwnRequestReason.Collision)
    {
      // Don't relinquish ownership of our player actor just because somebody bumped into it.
      Util.Log($"Denying collision-based ownership request from player {requestingPlayer} for actor {actor.GetDisplayName()} because it's our player actor.");
      return;
    }

    if (navigationControls != null && navigationControls.GetCameraView() == CameraView.ActorDriven && navigationControls.GetActorDrivenCameraActor() == actor && reason == OwnRequestReason.Collision)
    {
      // Don't relinquish ownership of our camera just because somebody bumped into it.
      Util.Log($"Denying collision-based ownership request from player {requestingPlayer} for actor {actor.GetDisplayName()} because it's our camera.");
      return;
    }

    if (IsLockWantedForActor(actor))
    {
      Util.Log($"Denying ownership request from player {requestingPlayer} for actor {actor.GetDisplayName()} because we want it locked.");
      return;
    }

    if (VoosAssetUtil.IsLocalAsset(actor.GetRenderableUri()))
    {
      Util.Log($"Denying ownership because actor {actor.GetDisplayName()} is still using a local renderable, {actor.GetRenderableUri()}");
      return;
    }

    if (reason == OwnRequestReason.Collision
      && Time.realtimeSinceStartup - actor.realTimeOfLastCollisionWithLocalPlayer < 1f
    )
    {
      // Util.Log($"Denying collision-ownership request for {actor.GetDisplayName()}, because we just collided with it, and this is likely to result in flickering.");
      return;
    }

    // OK - all checks passed..granted!

    // Subtle: Memory is sync'd in a rate-limited way in
    // VoosEngine.UpdateMemorySync, but of course we want to make sure that if
    // we're transfering ownership, we DEFINITELY transfer memory now.
    if (actor.IsMemoryDirty && actor.reliablePhotonView != null)
    {
      actor.reliablePhotonView.RPC(
          "SetMemoryRPC", PhotonTargets.Others, actor.GetMemoryJsonSlow());
    }

    view.TransferOwnership(requestingPlayer.ID);
    actor.OnOwnershipTransfered();
#endif
  }

  public IEnumerable<VoosActor> EnumerateCopiesOf(VoosActor actor)
  {
    string cloneParent = actor.GetCloneParent();
    if (cloneParent.IsNullOrEmpty())
    {
      // This is an original - not a clone.
      cloneParent = actor.GetName();
    }

    foreach (var entry in actorsByName)
    {
      VoosActor other = entry.Value;

      if (other == actor)
      {
        continue;
      }

      // We want to apply this to other clones of the original, but also the
      // original itself.
      bool isCopy = other.GetCloneParent() == cloneParent || other.GetName() == cloneParent;

      if (isCopy)
      {
        yield return other;
      }
    }
  }

  public int CountCopiesOf(VoosActor actor)
  {
    // Ideally this would be continuously maintained, not refreshed each time.
    int count = 0;
    foreach (VoosActor copy in EnumerateCopiesOf(actor))
    {
      count++;
    }
    return count;
  }

  public VoosActor GetPlayerActor()
  {
    return userMain?.GetPlayerActor();
  }

  public void RequestTransferPlayerControl(TransferPlayerControlRequest request)
  {
    // Is the request for us, or for a different player? We can check this by checking
    // who we are in UserMain, versus the "fromActor" in the request.
    // If they differ, it's because this request is meant for another player in a
    // multiplayer game, so we can ignore it.
    VoosActor playerActor = GetPlayerActor();
    if (playerActor == null || playerActor.GetName() != request.fromActor)
    {
      // We can safely ignore the request.
      return;
    }
    // Request will be handled on the next frame.
    transferPlayerControlRequest = request;
  }

  private void TryFulfilTransferPlayerControlRequest()
  {
    if (transferPlayerControlRequest == null) return;
    if (userMain == null) return;

    // Consume the request.
    TransferPlayerControlRequest request = transferPlayerControlRequest.Value;
    transferPlayerControlRequest = null;

    Debug.Log("Trying to set player actor from " + request.fromActor +
        " to " + (request.toActor == "" ? "(default)" : request.toActor));
    VoosActor newActor = null;
    if (request.toActor != "")
    {
      newActor = GetActor(request.toActor);
    }
    VoosActor oldActor = GetPlayerActor();

    if (newActor == oldActor)
    {
      Debug.LogWarning("Can't set player actor. Same as current.");
      return;
    }

    if (oldActor != null && oldActor.GetName() != request.fromActor)
    {
      // Shouldn't happen, as we gate this earlier, but...
      Debug.LogWarning("Can't set player actor. Current actor is not set correctly.");
      return;
    }

    userMain.MigrateUserTo(newActor);
    Debug.Log("Successfully migrated player to actor " + (newActor != null ? newActor.GetName() : "(default)"));
    // If requested, destroy the original actor.
    if (request.destroy)
    {
      DestroyActor(oldActor);
    }
  }

  internal void NotifyActorDestroyed(VoosActor voosActor)
  {
    onBeforeActorDestroy?.Invoke(voosActor);
    actorsByName.Remove(voosActor.GetName());
    OnActorsByNameChanged();
  }

  private VoosActor FindNewPlayerPrefabActor()
  {
    foreach (VoosActor actor in EnumerateActors())
    {
      // HACK HACK HACK
      // For now we just search by display name. Later on we will make this a property
      // of a Game Rules behavior.
      if (actor.GetDisplayName() == "NEW_PLAYER_PREFAB")
      {
        return actor;
      }
    }
    return null;
  }

  [System.Serializable]
  public struct CloneActorRequest
  {
    public string baseActorName;
    public string creatorName;
    public Vector3 position;
    public Quaternion rotation;
  }

  [System.Serializable]
  public struct CloneActorResponse
  {
    public string error;
    public string[] names;
    public ushort[] tempIds;
    public string[] baseActorNames;
  }

  public static class InstantiatePrefab
  {
    [System.Serializable]
    public struct Request
    {
      public string prefabUri;
      public string creatorName;
      public Vector3 position;
      public Quaternion rotation;
    }

    [System.Serializable]
    public struct Response
    {
      public string name;
      public string brainName;
      public ushort actorId;
    }
  }

  public CloneActorResponse CloneActorForScript(CloneActorRequest args)
  {
    Debug.Assert(!args.baseActorName.IsNullOrEmpty(), $"CloneActorForScript: Script wanted to clone something, but did not tell us the actor to clone from.");
    Debug.Assert(actorsByName.ContainsKey(args.baseActorName), "CloneActorForScript: Clone-parent does not exist.");

    VoosActor rootActor = actorsByName[args.baseActorName];
    VoosActor rootClone = CloneActorAndDescendantsForScript(rootActor, rootActor.GetParentActor(), args.position, args.rotation);

    string[] actorNames = (from actor in rootClone.DepthFirstSearch() select actor.GetName()).ToArray();

    ushort[] tempIds = new ushort[actorNames.Length];
    string[] baseActorNames = new string[actorNames.Length];
    for (int i = 0; i < actorNames.Length; i++)
    {
      VoosActor actor = GetActor(actorNames[i]);
      tempIds[i] = actor.lastTempId;
      baseActorNames[i] = actor.GetCloneParent();
    }

    return new CloneActorResponse
    {
      names = actorNames,
      tempIds = tempIds,
      baseActorNames = baseActorNames
    };
  }

  private VoosActor CloneActorAndDescendantsForScript(VoosActor rootActor, VoosActor parentActor, Vector3 worldPosition, Quaternion worldRotation)
  {
    // Clone the root first, then children.
    VoosActor rootClone = CloneSingleActorForScript(rootActor, parentActor, worldPosition, worldRotation);

    for (int i = 0; i < rootActor.transform.childCount; i++)
    {
      VoosActor child = rootActor.transform.GetChild(i).GetComponent<VoosActor>();
      if (child == null) continue;

      // The child's world transform is determined by transporting its local transform to the root's clone.
      Vector3 childsWorldPosition = rootClone.transform.TransformPoint(child.transform.localPosition);
      Quaternion childsWorldRotation = child.transform.localRotation * rootClone.transform.rotation;

      VoosActor childClone = CloneSingleActorForScript(child, rootClone, childsWorldPosition, childsWorldRotation);
      childClone.transform.SetParent(rootClone.transform, worldPositionStays: true);
    }

    return rootClone;
  }

  // Clones a single actor actorToClone. The clone will be parented to parentActor.
  // If parentActor == null, it will be parented to the scene.
  private VoosActor CloneSingleActorForScript(VoosActor actorToClone, VoosActor parentActor, Vector3 worldPosition, Quaternion worldRotation)
  {
    VoosActor.PersistedState state = VoosActor.PersistedState.NewFrom(actorToClone, true /* HACKY BUT IMPORTANT! Otherwise we crash. */);
    state.displayName = $"{actorToClone.GetDisplayName()}-Script-Clone";

    VoosActor clone = CreateActor(worldPosition, worldRotation, cloneToSetup =>
    {
      // Do NOT copy over everything...only a subset of fields.
      state.name = cloneToSetup.GetName();
      state.cloneParent = actorToClone.GetName();
      state.position = worldPosition;
      state.rotation = worldRotation;
      state.preferOffstage = false;
      state.wasClonedByScript = true;
      state.transformParent = parentActor != null ? parentActor.GetName() : null;
      state.spawnTransformParent = parentActor != null ? parentActor.GetName() : null;
      cloneToSetup.UpdateFrom(state);

      // Note: when the clone goes from off-stage to on-stage, we are resetting its position/rotation to
      // its spawn position/rotation, so we have to set it again here, which is kind of lame... but works.
      cloneToSetup.SetPosition(worldPosition);
      cloneToSetup.SetRotation(worldRotation);
    });

    latestActorsInSerializedOrder.Add(clone);
    clone.lastTempId = (ushort)(latestActorsInSerializedOrder.Count - 1);

    // Bit of a hack..JS will take full control of a script-clone's memories, so
    // don't overwrite. Test caser: firing laser bolt sends laser bolt flying?
    clone.SetMemoryJson(null);
    return clone;
  }

  // Builds a look-up dictionary from original actor names to cloned actor names in preparation
  // for cloning or copying the given list of actors.
  private Dictionary<string, string> BuildClonedActorNameDict(IEnumerable<VoosActor> actorsToClone)
  {
    Dictionary<string, string> dict = new Dictionary<string, string>();
    foreach (VoosActor actor in actorsToClone)
    {
      dict[actor.GetName()] = GenerateUniqueId();
      foreach (VoosActor descendant in actor.gameObject.GetComponentsInChildren<VoosActor>())
      {
        dict[actor.GetName()] = GenerateUniqueId();
      }
    }
    return dict;
  }

  [System.Serializable]
  public struct DestroyActorsRequest
  {
    public string[] actorNames;
  }

  public void DestroyActorsForScript(DestroyActorsRequest request)
  {
    foreach (string actorName in request.actorNames)
    {
      VoosActor actor = GetActor(actorName);
      if (actor != null && actor.IsLocallyOwned())
      {
        actorsToDestroy.Add(actor);
      }
    }
  }

  public void WantLockForActor(VoosActor actor)
  {
    int count = localLockCounts.GetOrSet(actor, 0);
    count++;
    localLockCounts[actor] = count;
  }

  public void UnwantLockForActor(VoosActor actor)
  {
    int count = localLockCounts.GetOrSet(actor, 0);
    if (count == 0)
    {
      Util.LogError($"Unmatched UnlockActor call! Actor: {actor.name}");
      return;
    }
    count--;

    if (count == 0)
    {
      localLockCounts.Remove(actor);
    }
    else
    {
      localLockCounts[actor] = count;
    }
  }

  public bool IsLockWantedForActor(VoosActor actor)
  {
#if UNITY_EDITOR
    foreach (int count in localLockCounts.Values)
    {
      Debug.Assert(count > 0, "IsLockWantedForActor: Count was negative some how?");
    }
#endif

    return localLockCounts.ContainsKey(actor);
  }

  // Returns a list of all tags in use in the game, sorted alphabetically.
  public List<string> GetAllTagsInUse()
  {
    HashSet<string> tags = new HashSet<string>();
    foreach (var actor in EnumerateActors())
    {
      tags.AddRange(actor.GetTags());
    }
    List<string> tagsList = new List<string>(tags);
    tagsList.Sort();
    return tagsList;
  }

  public VoosActor GetActorByTempId(ushort tempActorId)
  {
    if (tempActorId >= latestActorsInSerializedOrder.Count)
    {
      Util.LogError($"Bad actorId given: {tempActorId}. # actors = {latestActorsInSerializedOrder.Count}");
      return null;
    }

    return latestActorsInSerializedOrder[tempActorId];
  }

  public IEnumerator<VoosActor> GetActorsWithLocalResources()
  {
    foreach (VoosActor actor in EnumerateActors())
    {
      if (VoosAssetUtil.IsLocalAsset(actor.GetRenderableUri()))
      {
        yield return actor;
      }
    }
  }

  public IEnumerator<VoosActor> GetActorsWithUnavailableResources()
  {
    foreach (VoosActor actor in EnumerateActors())
    {
      if (actor.GetRenderableUri() == VoosActor.NOT_AVAILABLE_URI)
      {
        yield return actor;
      }
    }
  }

  public VoosActor.PersistedState[] SaveActorHierarchy(VoosActor actor)
  {
    VoosActor[] actors = actor.GetComponentsInChildren<VoosActor>(actor);
    VoosActor.PersistedState[] saved = new VoosActor.PersistedState[actors.Length];
    for (int i = 0; i < actors.Length; i++)
    {
      saved[i] = SaveActor(actors[i]);
    }
    return saved;
  }

  public VoosActor.PersistedState[] SaveActorHierarchy(IEnumerable<VoosActor> rootActors)
  {
    HashSet<VoosActor> allActors = new HashSet<VoosActor>();
    foreach (var root in rootActors)
    {
      allActors.AddRange(root.GetComponentsInChildren<VoosActor>());
    }
    return allActors.Select(a => SaveActor(a)).ToArray();
  }

  public void RestoreActorHierarchy(VoosActor.PersistedState[] saved)
  {
    foreach (var save in saved)
    {
      RestoreActor(save);
    }
  }

  public VoosActor.PersistedState SaveActor(VoosActor actor)
  {
    return VoosActor.PersistedState.NewFrom(actor);
  }

  public VoosActor RestoreActor(VoosActor.PersistedState savedState)
  {
    return CreateActor(savedState.position, savedState.rotation, restored =>
    {
      restored.SetName(savedState.name);
      restored.UpdateFrom(savedState);
    });
  }

  public static void CheckForUnusedBehaviorDatabaseItems(VoosEngine engine)
  {
#if UNITY_EDITOR

    // Are all brains in the DB being used?
    var db = engine.behaviorSystem.SaveDatabase();
    HashSet<string> usedBrainIds = new HashSet<string>(
      from a in engine.EnumerateActors()
      select a.GetBrainName());

    HashSet<string> usedBehaviorIds = new HashSet<string>();

    for (int i = 0; i < db.brainIds.Length; i++)
    {
      if (usedBrainIds.Contains(db.brainIds[i]))
      {
        var brain = db.brains[i];

        foreach (var use in brain.behaviorUses)
        {
          if (BehaviorSystem.IsEmbeddedBehaviorUri(use.behaviorUri))
          {
            usedBehaviorIds.Add(BehaviorSystem.GetIdOfBehaviorUri(use.behaviorUri));
          }
        }
      }

    }

    Util.Log($"{db.brainIds.Where(id => id != VoosEngine.DefaultBrainUid && !usedBrainIds.Contains(id)).Count()} unused brains of {db.brainIds.Length}");
    Util.Log($"{db.behaviorIds.Where(id => !usedBehaviorIds.Contains(id)).Count()} unused behaviors of {db.behaviorIds.Length}");

#endif
  }

  public void GetActorsControlledByVirtualPlayerId(string playerId, List<VoosActor> outResult)
  {
    outResult.Clear();
    foreach (VoosActor actor in EnumerateActors())
    {
      if (actor.GetControllingVirtualPlayerId() == playerId)
      {
        outResult.Add(actor);
      }
    }
  }

  public VirtualPlayerManager GetVirtualPlayerManager()
  {
    return virtualPlayerManager;
  }

  [System.Serializable]
  public struct BehaviorLogItem
  {
    public string actorId;
    public string senderId;
    public string useId;

    // The name of the actor-message being handled
    public string messageName;

    public int lineNum;

    // The payload of the item
    public string message;
  }

  internal void HandleBehaviorException(BehaviorLogItem e)
  {
    onBehaviorException?.Invoke(e);
  }

  internal void HandleBehaviorLogMessage(BehaviorLogItem msg)
  {
    onBehaviorLogMessage?.Invoke(msg);
  }

  public static HashSet<string> GetUsedBrainIds(VoosActor.PersistedState[] actors)
  {
    var rv = new HashSet<string>(actors.Select(actor => actor.brainName));
    rv.Add(DefaultBrainUid);
    return rv;
  }

  public static int ExtractFirstLineNumberForModuleError(string moduleKey, string errorMessage)
  {
    string needle = $"{moduleKey}:";
    int startIdx = errorMessage.IndexOf(needle);
    if (startIdx == -1)
    {
      return -1;
    }

    int afterColon = startIdx + needle.Length;
    int nextColon = errorMessage.IndexOf(':', afterColon);
    if (nextColon == -1)
    {
      return -1;
    }

    string lineNumberString = errorMessage.Substring(afterColon, nextColon - afterColon);
    int lineNumber = -1;
    if (!Int32.TryParse(lineNumberString, out lineNumber))
    {
      Debug.LogError("Failed to parse line number from error message: " + errorMessage);
      return -1;
    }
    return lineNumber;
  }

  public VoosActor FindOneActorUsing(string behaviorUri)
  {
    foreach (var actor in this.EnumerateActors())
    {
      var brain = behaviorSystem.GetBrain(actor.GetBrainName());
      foreach (var use in brain.behaviorUses)
      {
        if (use.behaviorUri == behaviorUri)
        {
          return actor;
        }
      }
    }
    return null;
  }

  void UpdateGlobalLights()
  {
    if (everUpdatedGlobalLights && currentSceneLightingMode == gbStage.GetSceneLightingMode())
    {
      return;
    }
    everUpdatedGlobalLights = true;
    currentSceneLightingMode = gbStage.GetSceneLightingMode();

    // Dim directional lights.
    foreach (LightingSetup lightingSetup in GameObject.FindObjectsOfType<LightingSetup>())
    {
      Light light = lightingSetup.GetComponent<Light>();
      Debug.Assert(light != null, "LightingSetup component on something that's not a Light.");
      light.intensity = currentSceneLightingMode == GameBuilderStage.SceneLightingMode.Day ? lightingSetup.intensityDay :
          currentSceneLightingMode == GameBuilderStage.SceneLightingMode.Night ? lightingSetup.intensityNight : 0;
    }
    RenderSettings.ambientLight = currentSceneLightingMode == GameBuilderStage.SceneLightingMode.Day ? new Color(0.73f, 0.73f, 0.73f) :
          currentSceneLightingMode == GameBuilderStage.SceneLightingMode.Night ? new Color(0.05f, 0.05f, 0.2f) : Color.black;
    RenderSettings.ambientMode = currentSceneLightingMode == GameBuilderStage.SceneLightingMode.Dark ?
          UnityEngine.Rendering.AmbientMode.Flat : UnityEngine.Rendering.AmbientMode.Trilight;
  }

  // Returns false if we (the engine) know of any reason to NOT show the actor renderable.
  // Returns true otherwise.
  public bool ShouldRenderActor(VoosActor actor)
  {
    // We have to do this lazily because NavigationControls is created after VoosEngine.
    if (navigationControls == null)
    {
      // NavigationControls isn't available yet.
      return true;
    }
    if (navigationControls.GetCameraView() == CameraView.FirstPerson &&
        userMain != null && !userMain.InEditMode() &&
        actor == userMain.GetPlayerActor())
    {
      // If using the default first-person camera, hide the local player actor.
      return false;
    }
    if (navigationControls.GetCameraView() != CameraView.ActorDriven)
    {
      return true;
    }
    VoosActor cameraActor = navigationControls.GetActorDrivenCameraActor();
    if (cameraActor == null)
    {
      return true;
    }
    string[] dontRender = cameraActor.GetCameraSettings().dontRenderActors;

    if (dontRender != null && Array.IndexOf(dontRender, actor.GetName()) >= 0)
    {
      // Actor is in the camera's "do not render" list.
      return false;
    }
    return true;
  }

  public ParticleEffectSystem GetParticleEffectSystem()
  {
    return particleEffectSystem;
  }

  public SoundEffectSystem GetSoundEffectSystem()
  {
    return soundEffectSystem;
  }

  public void OnQuailtySettingsChanged()
  {
    foreach (var actor in EnumerateActors())
    {
      actor.OnQualitySettingsChanged();
    }
  }

  public int GetNumActors()
  {
    return actorsByName.Count;
  }

  byte PINIT_END_SENTINEL = 79;

  public void SerializePlayerInitPayloadV2(NET.NetworkWriter writer)
  {
    writer.Write(GetNumActors());

    foreach (VoosActor actor in EnumerateActors())
    {
      int viewId = actor.reliablePhotonView.viewID;
      writer.Write(viewId);
      SaveActor(actor).Serialize(writer);
    }

    writer.WriteVoosBoolean(isRunning);
    writer.Write(PINIT_END_SENTINEL);
  }

  public void DeserializePlayerInitV2(NET.NetworkReader reader)
  {
#if USE_PUN
    Debug.Assert(state == State.Uninit, "DeserializePlayerInitV2 called before init'd?");
    Debug.Assert(actorsByName.Count == 0, "There should be no actors before we're initialized!!");

    int numActors = reader.ReadInt32();

    for (int i = 0; i < numActors; i++)
    {
      int viewId = reader.ReadInt32();
      Debug.AssertFormat(PhotonView.Find(viewId) == null, "View ID {0} that was given in init payload was already taken locally by game object: {1}", viewId, PhotonView.Find(viewId)?.gameObject?.name);
      var state = new VoosActor.PersistedState();
      state.Deserialize(reader);
      VoosActor actor = CreateLocalActor(state.position, state.rotation, viewId);
      actor.SetName(state.name);
      actor.UpdateFrom(state);
    }

    bool startRunning = reader.ReadVoosBoolean();
    byte endSent = reader.ReadByte();
    Debug.Assert(endSent == PINIT_END_SENTINEL, $"Expected sentinel to be {PINIT_END_SENTINEL}, but it was {endSent}");

    state = State.Init;

    // Intentionally call the RPC directly. To avoid triggering unnecesary RPCs to other clients (who already have the right value).
    SetIsRunningRPC(startRunning);
#endif
  }

  void OnApplicationQuit()
  {
    Util.Log($"VoosEngine.OnApplicationQuit");
  }

  void OnVirtualPlayerJoined(string virtualId)
  {
    BroadcastMessage<PlayerJoinedMessage>("PlayerJoined", new PlayerJoinedMessage { playerId = virtualId });
  }

  void OnVirtualPlayerLeft(string virtualId)
  {
    BroadcastMessage<PlayerLeftMessage>("PlayerLeft", new PlayerLeftMessage { playerId = virtualId });
  }

  public void NotifyActorBecomingUncontrollable(VoosActor actor)
  {
    this.onActorBecomingUncontrollable?.Invoke(actor);
  }

  public IEnumerable<VoosActor> GetActorsAndDescendants(IEnumerable<VoosActor> actors)
  {
    HashSet<VoosActor> result = new HashSet<VoosActor>();
    foreach (VoosActor actor in actors)
    {
      foreach (VoosActor descendant in actor.gameObject.GetComponentsInChildren<VoosActor>())
      {
        result.Add(descendant);
      }
    }
    return result;
  }
}
