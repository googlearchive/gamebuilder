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
using Behaviors;
using System.IO;

// The "behaviors" system, built on top of the Voos JavaScript engine. Serves as
// a database of all JavaScript pieces: individual behaviors and global scripts
// (implementing the behavior API). Also provides controls for the system, such
// as triggering recompiles. This code is also responsible for networking
// itself, ie. making sure that it uses RPCs or whatever to provide an
// eventually consistent and locally-responsive (where possible) public
// interface.
public class BehaviorSystem : MonoBehaviour
{
  static string BuiltinBehaviorExtension = ".js.txt";
  public static string UserBehaviorExtension = ".js";

  public TextAsset[] sources;
  public Util.AbstractPath[] typings;

  // For the built-in behaviors. Should be relative to StreamingAssets. TODO
  // this library stuff really doesn't need to be in here..could be like
  // different behavior sources.
  public string behaviorLibraryDirectory;

  PhotonView photonView;
  Behaviors.Database db = new Behaviors.Database();

  Dictionary<string, Behaviors.Behavior> loadedBehaviorsByAbsPath = new Dictionary<string, Behaviors.Behavior>();
  Dictionary<string, string> builtinAbsPathByFilename = new Dictionary<string, string>();
  System.Collections.ObjectModel.ReadOnlyCollection<string> builtinUris;

  public VoosEngine voosEngine;

  // This only fires for put/delete *behaviors*, nothing else, like brains.
  public event System.Action<PutEvent> onBehaviorPut;
  public event System.Action<DeleteEvent> onBehaviorDelete;

  public event System.Action onBrainsHandlingCollisionsChanged;

  bool initialized = false;

  // Hack for legacy to not crash..
  public bool returnEmptyForMissingBuiltinBehaviors = false;

  Queue<PutRequest<Brain>> unackedPutBrains = new Queue<PutRequest<Brain>>();
  int putBrainSeqNum = 0;

  HashSet<string> brainIdsHandlingCollisions = new HashSet<string>();
  Dictionary<ulong, SavedCardPack> cardPacks = new Dictionary<ulong, SavedCardPack>();

  // Maybe cached.
  Behaviors.Behavior CachedLoadBehavior(string absPath, string label)
  {
    if (loadedBehaviorsByAbsPath.ContainsKey(absPath))
    {
      return loadedBehaviorsByAbsPath[absPath];
    }
    else
    {
      string js = File.ReadAllText(absPath);
      string metaJson = null;

      string metaPath = absPath + ".metaJson";
      if (File.Exists(metaPath))
      {
        metaJson = File.ReadAllText(metaPath);
      }

      Behaviors.Behavior beh = new Behaviors.Behavior
      {
        label = label,
        javascript = js,
        metadataJson = metaJson
      };
      loadedBehaviorsByAbsPath.Add(absPath, beh);
      return beh;
    }
  }

  public Behaviors.Behavior GetBehaviorData(System.Uri uri)
  {
    try
    {
      switch (uri.Scheme)
      {
        case BuiltinBehaviorUriScheme:
          {
            Debug.Assert(!uri.LocalPath.ContainsDirectorySeparators());
            string filename = uri.LocalPath + BuiltinBehaviorExtension;
            if (!builtinAbsPathByFilename.ContainsKey(filename)
              && returnEmptyForMissingBuiltinBehaviors)
            {
              return new Behavior { draftJavascript = "", javascript = "", label = "<Missing>", metadataJson = "{}" };
            }
            string absPath = builtinAbsPathByFilename[filename];
            return CachedLoadBehavior(absPath, filename);
          }

        case EmbeddedBehaviorUriScheme:
          return db.GetBehavior(uri.PathAndQuery);

        case UserLibraryBehaviorUriScheme:
          {
            throw new System.Exception("Unsupported as of 20190514");
            string file = uri.LocalPath;
            string absPath = Path.Combine(GetUserBehaviorsRoot(), file);
            var data = CachedLoadBehavior(absPath, file);
            data.userLibraryFile = file;
            return data;
          }

        default:
          throw new System.Exception($"Unknown behavior URI scheme: {uri.Scheme}. Full URI: {uri.ToString()}");
      }
    }
    catch (System.Exception e)
    {
      throw new System.Exception($"For behavior URI {uri.ToString()}", e);
    }
  }

  public Behaviors.Behavior GetBehaviorData(string behaviorUri)
  {
    return GetBehaviorData(new System.Uri(behaviorUri));
  }

  public bool IsBehaviorUriValid(string uri)
  {
    return IsBuiltinBehaviorUri(uri)
    || (IsEmbeddedBehaviorUri(uri) && EmbeddedBehaviorExists(GetIdOfBehaviorUri(uri)));
  }

  public bool HasBrain(string brainId)
  {
    return db.brains.Exists(brainId);
  }

  public Behaviors.Brain GetBrain(string brainId)
  {
    return db.GetBrain(brainId);
  }

  public IEnumerable<BehaviorUse> UsesForBehavior(string behaviorUri)
  {
    foreach (var entry in db.BehaviorUsesForBehavior(behaviorUri))
    {
      yield return entry;
    }
  }

  public IEnumerable<BehaviorUse> UseEntriesForBehavior(string behaviorUri)
  {
    foreach (var entry in db.BehaviorUsesForBehavior(behaviorUri))
    {
      yield return entry;
    }
  }

  public IEnumerable<Util.Table<Brain>.Entry> BrainsForBehavior(string behaviorUri)
  {
    foreach (var entry in db.brains.GetAll())
    {
      if (entry.value.behaviorUses.Any(use => use.behaviorUri == behaviorUri))
      {
        yield return entry;
      }
    }
  }

  // Remove any uses that correspond to deleted behaviors.
  Brain RemoveInvalidUses(Brain originalBrain)
  {
    Brain brain = originalBrain.DeepClone();
    foreach (BehaviorUse use in originalBrain.GetUses())
    {
      if (!IsBehaviorUriValid(use.behaviorUri))
      {
        brain.DeleteUse(use.id);
      }
    }
    return brain;
  }

  [System.Serializable]
  struct PutRequest<T>
  {
    public int seqNum;
    public string id;
    public T value;
  }

  private void PutBrainLocal(PutRequest<Brain> request)
  {
    // Util.Log($"KEEP ME: PutBrainLocal, req json: {JsonUtility.ToJson(request)}");
    db.brains.Set(request.id, RemoveInvalidUses(request.value));
    // Need to sync modules before we sync the database, in case any
    // prior-unused behaviors were used.
    foreach (var use in request.value.behaviorUses)
    {
      SyncBehaviorIfNeeded(use.behaviorUri);
    }
    SyncDatabase();
  }

  private void DeleteBrainLocal(string id)
  {
    db.brains.Delete(id);
    SyncDatabase();
  }

  private void PutBehaviorLocal(PutRequest<Behavior> request, bool fromThisUser)
  {
    Debug.Assert(IsGuid(request.id), $"It looks like you gave an invalid GUID: {request.id}. It should be just a string of hex digits.");
    if (db.behaviors.SetWouldChange(request.id, request.value))
    {
      // Figure out if it's a code change, before we update it.
      bool isNewBehavior = !db.behaviors.Exists(request.id);
      bool isNewCode = !isNewBehavior && db.GetBehavior(request.id).javascript != request.value.javascript;
      bool needsCodeSync = isNewBehavior || isNewCode;

      // Actually update it.
      db.behaviors.Set(request.id, request.value);

      if (needsCodeSync)
      {
        // Do this immediately...rather than waiting. This is more convenient
        // for live-editing, since it's good to be able to immediately query the
        // compiled code for things like exported PROPS, rather than waiting for
        // the next frame.
        SyncBehavior(IdToEmbeddedBehaviorUri(request.id));

        // For a database sync to refresh some things. Namely, the handler function caches. Otherwise, if we re-compile a module, the JS objects may be holding onto stale function refs.
        SyncDatabase();
      }
      onBehaviorPut?.Invoke(new PutEvent(request.id, isNewBehavior, fromThisUser));
    }
  }

  private void DeleteBehaviorLocal(string id, bool fromThisUser)
  {
    if (db.behaviors.Delete(id))
    {
      onBehaviorDelete?.Invoke(new DeleteEvent(id, fromThisUser));
    }
  }

  void NotifyCardRemovedLocal(string brainId, string useId)
  {
    if (!HasBrain(brainId))
    {
      return;
    }
    // Check the useId is still on the brain..
    var brain = GetBrain(brainId);
    if (!brain.HasUse(useId))
    {
      // Ignore it - could be a reflected duplicate call.
      return;
    }

    foreach (VoosActor actor in voosEngine.EnumerateActors().
      Where(actor => actor.GetBrainName() == brainId))
    {
      // Only call for my local actors (remote owners will take care of calling this for their actors).
      if (actor.IsLocallyOwned())
      {
        CallBehaviorUseMethod<object, object>(
          useId,
          actor.GetName(),
          "onCardRemoved",
          null);
      }
    }
  }

  [PunRPC]
  void NotifyCardRemovedRPC(string brainId, string useId)
  {
    NotifyCardRemovedLocal(brainId, useId);
  }

  public void NotifyCardRemoved(string brainId, string useId)
  {
    NotifyCardRemovedLocal(brainId, useId);
    photonView.RPC("NotifyCardRemovedRPC", PhotonTargets.AllViaServer, brainId, useId);
  }

  public void PutBrain(string brainId, Behaviors.Brain brain)
  {
    // Follow predicted-last-writer-wins pattern.
    var req = new PutRequest<Behaviors.Brain> { id = brainId, value = brain, seqNum = putBrainSeqNum };
    putBrainSeqNum++;
    unackedPutBrains.Enqueue(req);
    Util.Log($"public PutBrain, numUses: {req.value.behaviorUses.Length}");
    PutBrainLocal(req);
    photonView.RPC("PutBrainRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(req));
  }

  public void DeleteBrain(string id)
  {
    // Follow predicted-last-writer-wins pattern.
    DeleteBrainLocal(id);
    photonView.RPC("DeleteBrainRPC", PhotonTargets.AllViaServer, id);
  }

  public bool EmbeddedBehaviorExists(string behaviorId)
  {
    return db.behaviors.Exists(behaviorId);
  }

  public void PutBehavior(string behaviorId, Behaviors.Behavior behavior)
  {
    Debug.Assert(!IsBehaviorUri(behaviorId), "Please provide an id, not an uri");

    // Follow predicted-last-writer-wins pattern.
    var req = new PutRequest<Behaviors.Behavior> { id = behaviorId, value = behavior };
    string json = JsonUtility.ToJson(req);
    // Make sure we put in the same deserialized version as the RPC would
    // (to avoid whitespace differences && make Behavior.Equals() comparisons consistent)
    req = JsonUtility.FromJson<PutRequest<Behavior>>(json);
    PutBehaviorLocal(req, true);
    photonView.RPC("PutBehaviorRPC", PhotonTargets.AllViaServer, json);
  }

  public void DeleteBehavior(string id)
  {
    Debug.Assert(!IsBehaviorUri(id), "Please provide an id, not an uri");

    // Follow predicted-last-writer-wins pattern.
    DeleteBehaviorLocal(id, true);
    photonView.RPC("DeleteBehaviorRPC", PhotonTargets.AllViaServer, id);
  }

  [PunRPC]
  void PutBrainRPC(string requestJson, PhotonMessageInfo info)
  {
    PutRequest<Brain> request = JsonUtility.FromJson<PutRequest<Brain>>(requestJson);
    if (info.sender == PhotonNetwork.player)
    {
      // Acked. It *must* be the first item in our queue.
      var justAcked = unackedPutBrains.Dequeue();
      Debug.Assert(request.seqNum == justAcked.seqNum);
    }
    // Util.Log($"PutBrainRPC, numUses: {request.value.behaviorUses.Length}");
    PutBrainLocal(request);

    // Replay all our unacked requests.
    if (unackedPutBrains.Count > 0)
    {
      // Util.Log($"Replaying {unackedPutBrains.Count} unacked PutBrain's..");
      foreach (var unacked in unackedPutBrains)
      {
        PutBrainLocal(unacked);
      }
    }
  }

  [PunRPC]
  void DeleteBrainRPC(string id)
  {
    DeleteBrainLocal(id);
  }

  [PunRPC]
  void PutBehaviorRPC(string requestJson)
  {
    PutRequest<Behavior> request = JsonUtility.FromJson<PutRequest<Behavior>>(requestJson);
    PutBehaviorLocal(request, false);
  }

  [PunRPC]
  void DeleteBehaviorRPC(string id)
  {
    DeleteBehaviorLocal(id, false);
  }

  // Hmm don't really like having this stuff in BehaviorSystem...should separate
  // this out into behaviors source or something.

  public IEnumerable<TextAsset> ForSystemSources()
  {
    foreach (TextAsset source in sources)
    {
      if (source != null)
      {
        yield return source;
      }
    }
  }

  private string BuildSystemSource()
  {
    Debug.Log("WHOAAH full system source rebuild triggered");
    System.Text.StringBuilder sourceBuilder = new System.Text.StringBuilder(1024 * 1024);
    foreach (TextAsset source in ForSystemSources())
    {
      sourceBuilder.Append(source.text.Replace("\r", ""));
      sourceBuilder.Append("\n");
    }
    return sourceBuilder.ToString();
  }

  string GetBuiltinBehaviorsRoot()
  {
    return Path.Combine(Application.streamingAssetsPath, behaviorLibraryDirectory);
  }

  string GetUserBehaviorsRoot()
  {
    return Path.Combine(Util.GetUserDataDir(), "Behaviors");
  }

  // Returns a list of URIs pointing to built-in and embedded behaviors.
  public List<string> LoadBehaviorLibrary()
  {
    List<string> uris = new List<string>(GetCachedBuiltinBehaviors());

    foreach (var behavior in db.behaviors.GetAll())
    {
      uris.Add(IdToEmbeddedBehaviorUri(behavior.id));
    }

    return uris;
  }

  public IEnumerable<string> GetEmbeddedBehaviorIds()
  {
    foreach (var entry in db.behaviors.GetAll())
    {
      yield return entry.id;
    }
  }

  private System.Collections.ObjectModel.ReadOnlyCollection<string> GetCachedBuiltinBehaviors()
  {
    if (builtinUris == null)
    {
      List<string> uris = new List<string>();
      HashSet<string> urisSeen = new HashSet<string>();

      foreach (string behaviorAbsPath in Directory.EnumerateFiles(GetBuiltinBehaviorsRoot(), $"*{BuiltinBehaviorExtension}", SearchOption.AllDirectories))
      {
        // Build URI.
        string uriPath =
        // Only use the file name, so we're free to move them around for internal organization
        Path.GetFileName(behaviorAbsPath)
        // Lastly, to give us flexibility on the extension...
        .Replace(BuiltinBehaviorExtension, "");

        // Ignore it if it's a spec file (that's only used for docs).
        if (uriPath.StartsWith("spec_"))
        {
          continue;
        }

        Debug.Assert(!uriPath.ContainsDirectorySeparators());

        string uriString = $"{BuiltinBehaviorUriScheme}:{uriPath}";
        uris.Add(uriString);
        builtinAbsPathByFilename[Path.GetFileName(behaviorAbsPath)] = behaviorAbsPath;

        Debug.Assert(!urisSeen.Contains(uriString), $"Duplicate file name found in builtin behaviors library. One of the files: {behaviorAbsPath}");
      }

      builtinUris = uris.AsReadOnly();
    }
    return builtinUris;
  }

  public bool IsLegacyBuiltinBehavior(string uri)
  {
    // Legacy builtins don't have metadata.
    return IsBuiltinBehaviorUri(uri) && GetBehaviorData(uri).metadataJson.IsNullOrEmpty();
  }

  public bool IsEmbeddedLegacyBehavior(string uri)
  {
    return IsEmbeddedBehaviorUri(uri) && GetBehaviorData(uri).metadataJson.IsNullOrEmpty();
  }

  // TODO should BehaviorSystem know about this?
  // For the card library.
  public IEnumerable<string> GetCards()
  {
    foreach (string uri in LoadBehaviorLibrary())
    {
      if (IsLegacyBuiltinBehavior(uri))
      {
        // Skip these.
        continue;
      }

      Behavior data = GetBehaviorData(uri);
      BehaviorCards.CardMetadata.Data meta = BehaviorCards.CardMetadata.GetMetaDataFor(data);
      if (!meta.isCard || meta.hidden) continue;
      yield return uri;
    }
  }

  // TODO should BehaviorSystem know about this?
  public IEnumerable<string> GetCategories()
  {
    HashSet<string> categories = new HashSet<string>();

    foreach (string uri in GetCards())
    {
      Behavior data = GetBehaviorData(uri);
      categories.AddRange(BehaviorCards.CardMetadata.GetEffectiveCardCategories(data));
    }
    return categories;
  }

  // TODO should BehaviorSystem know about this?
  public UnassignedBehavior CreateNewBehavior(string initCode)
  {
    return CreateNewBehavior(initCode, null);
  }

  // TODO should BehaviorSystem know about this?
  public UnassignedBehavior CreateNewBehavior(string initCode, string metadataJson)
  {
    string behaviorId = GenerateUniqueId();
    PutBehavior(behaviorId, new Behaviors.Behavior
    {
      label = "Custom",
      metadataJson = metadataJson,
      javascript = initCode
    });

    UnassignedBehavior newBehavior = new UnassignedBehavior(BehaviorSystem.IdToEmbeddedBehaviorUri(behaviorId), this);

    // NOTE: We don't need to add it here, per se. The caller should call AddBehavior on us with this instance.
    return newBehavior;
  }

  void Awake()
  {
    photonView = GetComponent<PhotonView>();
    Debug.Assert(photonView != null);
  }

  // Use this for initialization
  void Start()
  {
    // Load the builtins, since we need everyone to stay in sync, for RPCs.
    LoadBehaviorLibrary();

    bool ok = voosEngine.Recompile(BuildSystemSource());
    if (!ok)
    {
      throw new System.Exception("Failed to compile behavior system source - this is fatal!");
    }

    initialized = true;
  }

  // Call this if you know you'll *use* the behavior. Returns true if it's
  // ready, false if not.
  bool SyncBehaviorIfNeeded(string behaviorUri)
  {
    if (voosEngine.HasModuleCompiledOnce(behaviorUri))
    {
      return true;
    }

    return SyncBehavior(behaviorUri);
  }

  bool SyncBehavior(string behaviorUri)
  {
    var data = GetBehaviorData(behaviorUri);
    bool ok = voosEngine.SetModule(behaviorUri, data.javascript);
    if (!ok)
    {
      Util.Log($"Behavior compile error for URI {behaviorUri}");
    }

    return ok;
  }

  // This syncs everything except the behavior sources
  void SyncDatabase()
  {
    if (currentBatchName != null)
    {
      // Wait for end of batch before sync'ing.
      return;
    }

    using (Util.Profile("SyncDatabase"))
    {
      UpdateRequest request = new UpdateRequest();
      request.jsonObject = db.Save();
      Util.Maybe<UpdateResponse> maybeResponse = voosEngine.CommunicateWithAgent<UpdateRequest, UpdateResponse>(request);
      if (maybeResponse.IsEmpty())
      {
        throw new System.Exception("Failed to sync behavior database. Cannot proceed.");
      }

      brainIdsHandlingCollisions.Clear();
      brainIdsHandlingCollisions.AddRange(maybeResponse.Value.brainsHandlingCollisions);
      onBrainsHandlingCollisionsChanged?.Invoke();
    }
  }

  class CallBehaviorUseMethodRequest<T>
  {
    public string operation = "callBehaviorUseMethod";
    public string useId;
    public string actorId;
    public string methodName;
    public T args;
  }

  class CallBehaviorUseMethodResponse<T>
  {
    public T returnValue;
  }

  public Util.Maybe<TReturn> CallBehaviorUseMethod<TArgs, TReturn>(string useId, string actorId, string methodName, TArgs args)
  {
    using (Util.Profile("CallBehaviorUseMethod"))
    {
      var request = new CallBehaviorUseMethodRequest<TArgs>
      {
        actorId = actorId,
        useId = useId,
        methodName = methodName,
        args = args
      };

      Util.Maybe<CallBehaviorUseMethodResponse<TReturn>> response =
        voosEngine.CommunicateWithAgent<CallBehaviorUseMethodRequest<TArgs>, CallBehaviorUseMethodResponse<TReturn>>(request);

      if (response.IsEmpty())
      {
        return Util.Maybe<TReturn>.CreateEmpty();
      }
      else
      {
        return Util.Maybe<TReturn>.CreateWith(response.Get().returnValue);
      }
    }
  }

  class GetBehaviorPropertiesRequest
  {
    public string operation = "getBehaviorProperties";
    public string behaviorUri;
  }

  class GetBehaviorPropertiesResponse
  {
    public string propsJson;
  }

  public string GetBehaviorPropertiesJson(string behaviorUri)
  {
    var request = new GetBehaviorPropertiesRequest();
    request.behaviorUri = behaviorUri;

    if (!SyncBehaviorIfNeeded(behaviorUri))
    {
      Util.LogError($"GetBehaviorPropertiesJson failed for {behaviorUri} because it did not compile.");
      return null;
    }

    Util.Maybe<GetBehaviorPropertiesResponse> response = voosEngine.CommunicateWithAgent<GetBehaviorPropertiesRequest, GetBehaviorPropertiesResponse>(request);
    if (response.IsEmpty())
    {
      Util.LogError($"GetBehaviorPropertiesJson failed for behavior URI {behaviorUri}");
      return null;
    }
    else
    {
      return response.Get().propsJson;
    }
  }

  [System.Serializable]
  class UpdateRequest
  {
    public string operation = "updateBehaviorDatabase";
    public Behaviors.Database.Jsonable jsonObject;
  }

  [System.Serializable]
  class UpdateResponse
  {
    public string[] brainsHandlingCollisions;
  }

  void DebugLogDatabase()
  {
    db.DebugLog();
  }

  public int CountUsesOfBehavior(string behaviorUri, int shortCircuitAt = -1)
  {
    using (Util.WarnIfSlow("CountUsesOfBehavior", 10))
    {
      int count = 0;
      foreach (var entry in db.BehaviorUsesForBehavior(behaviorUri))
      {
        count++;

        if (shortCircuitAt > 0 && count >= shortCircuitAt)
        {
          return count;
        }
      }
      return count;
    }
  }

  public Behaviors.Database.Jsonable SaveDatabase()
  {
    return db.Save();
  }

  void SyncAllUsedBehaviors()
  {
    foreach (var entry in db.brains.GetAll())
    {
      foreach (var use in entry.value.behaviorUses)
      {
        SyncBehavior(use.behaviorUri);
      }
    }
  }

  public void LoadDatabaseForNetworkInit(Behaviors.Database.Jsonable saved)
  {
    db.LoadForNetworkInit(saved);
    SyncAllUsedBehaviors();
    SyncDatabase();
  }

  public void LoadDatabase(Behaviors.Database.Jsonable saved, bool removeUnusedBehaviors, HashSet<string> usedBrainIds)
  {
    db.Load(saved, removeUnusedBehaviors, usedBrainIds);
    SyncAllUsedBehaviors();
    SyncDatabase();
  }

  // TODO put this in some other class, like BehaviorUtil. I don't want it accessing private functions of system.
  // Returns the new brain ID, and fills out newUseIdsByOldOut with a use-ID map.
  public static string CloneBrain(BehaviorSystem system, string originalBrainId)
  {
    Brain cloneBrain = system.GetBrain(originalBrainId).DeepClone();
    string cloneBrainId = system.GenerateUniqueId();
    system.PutBrain(cloneBrainId, cloneBrain);
    return cloneBrainId;
  }

  public bool IsValidUserLibraryFile(string file)
  {
    return System.IO.File.Exists(Path.Combine(GetUserBehaviorsRoot(), file));
  }

  public string UserLibraryFileToUri(string file)
  {
    Debug.Assert(IsValidUserLibraryFile(file));
    return $"{UserLibraryBehaviorUriScheme}:{file}";
  }

  const string EmbeddedBehaviorUriScheme = "embedded";
  const string BuiltinBehaviorUriScheme = "builtin";
  const string BuiltinBehaviorUriPrefix = "builtin:";
  const string UserLibraryBehaviorUriScheme = "userlib";

  public string GenerateUniqueId()
  {
    return voosEngine.GenerateUniqueId();
  }

  public static bool IsGuid(string guid)
  {
    // Definitely could be more rigorous here. It should only contain hexadecimal digits.
    return guid.Length == 32 && !guid.Contains(":");
  }

  // Merges all data from the given export to the live database. Does NOT
  // overwrite if entries already exist.
  public void MergeNonOverwrite(Database.Jsonable exported, HashSet<string> expectedBrainIds)
  {
    exported.AssertValid();

    BeginBatchEdit("Merge");
    try
    {
      exported.PerformUpgrades(expectedBrainIds);
      exported.AssertValid();

      for (int i = 0; i < exported.behaviorIds.Length; i++)
      {
        string id = exported.behaviorIds[i];
        if (!db.behaviors.Exists(id))
        {
          PutBehavior(id, exported.behaviors[i]);
        }
      }

      for (int i = 0; i < exported.brainIds.Length; i++)
      {
        string id = exported.brainIds[i];
        if (!db.brains.Exists(id))
        {
          PutBrain(id, exported.brains[i]);
        }
      }
    }
    finally
    {
      EndBatchEdit("Merge");
    }
  }

  // Returns the brain ID of the imported brain in the active system. Ideally
  // wouldn't have this 'system' dependency..I think we need to move the
  // RPC/replication logic all into this class. Makes sense that the database
  // is responsible for keeping itself sync'd.
  public string ImportBrain(Database.Jsonable exported, string expectedBrainId)
  {
    exported.AssertValid();

    BeginBatchEdit("ImportBrain");
    try
    {
      exported.PerformUpgrades(new HashSet<string> { expectedBrainId });
      exported.AssertValid();

      Debug.Assert(exported.brainIds.Length == 1);
      Debug.Assert(exported.brains.Length == 1);
      Debug.Assert(exported.brainIds[0] == expectedBrainId);

      // Import all embedded behaviors, but give them new IDs
      Dictionary<string, string> exported2importedBehaviorId = new Dictionary<string, string>();
      for (int i = 0; i < exported.behaviorIds.Length; i++)
      {
        string expId = exported.behaviorIds[i];
        Behavior behavior = exported.behaviors[i];
        string impId = GenerateUniqueId();
        exported2importedBehaviorId[expId] = impId;
        PutBehavior(impId, behavior);
      }

      Brain brain = exported.brains[0];

      // Convert all embedded behavior URIs to the imported ones.
      for (int i = 0; i < brain.behaviorUses.Length; i++)
      {
        var use = brain.behaviorUses[i];

        if (IsEmbeddedBehaviorUri(use.behaviorUri))
        {
          if (!exported2importedBehaviorId.ContainsKey(use.behaviorUri))
          {
            // If the behavior URI doesn't exist, it was not exported. So just
            // assume this is meant to refer to some behavior that already
            // exists in the scene. Intended for scene actor library.
            // Check that it does exist.
            var existing = GetBehaviorData(use.behaviorUri);
            Debug.Assert(existing.javascript != null);
            continue;
          }

          // Freshly imported as a new behavior - update the URI.
          string expBehaviorId = BehaviorSystem.GetIdOfBehaviorUri(use.behaviorUri);
          string impBehaviorId = exported2importedBehaviorId[expBehaviorId];
          use.behaviorUri = BehaviorSystem.IdToEmbeddedBehaviorUri(impBehaviorId);
          brain.behaviorUses[i] = use;
        }
      }

      // Ok, ready to go in! We want to import into a different brain ID,
      // though.
      string importedBrainId = GenerateUniqueId();
      PutBrain(importedBrainId, brain);
      return importedBrainId;
    }
    catch (System.Exception e)
    {
      Util.LogError($"ImportBrain exception: {e}");
      throw e;
    }
    finally
    {
      EndBatchEdit("ImportBrain");
    }
  }

  public void ExportBrain(string brainId, Behaviors.Database destination)
  {
    // Export the brain itself.
    Brain brain = GetBrain(brainId).DeepClone();
    destination.brains.Set(brainId, brain);

    // Export the needed embedded behaviors. Don't need to export builtins -
    // they're always available.

    HashSet<string> exportedEmbeddedBehaviorIds = new HashSet<string>();
    foreach (var use in brain.behaviorUses)
    {
      if (BehaviorSystem.IsEmbeddedBehaviorUri(use.behaviorUri))
      {
        exportedEmbeddedBehaviorIds.Add(GetIdOfBehaviorUri(use.behaviorUri));
      }
    }

    foreach (var behaviorId in exportedEmbeddedBehaviorIds)
    {
      destination.behaviors.Set(behaviorId, db.GetBehavior(behaviorId));
    }
  }

  public static string IdToEmbeddedBehaviorUri(string behaviorId)
  {
    return $"{EmbeddedBehaviorUriScheme}:{behaviorId}";
  }

  public static string IdToBuiltinBehaviorUri(string behaviorId)
  {
    return $"{BuiltinBehaviorUriScheme}:{behaviorId}";
  }

  public static bool IsBuiltinBehaviorUri(string behaviorUri)
  {
    return behaviorUri.StartsWith(BuiltinBehaviorUriPrefix);
  }

  public static bool IsUserLibraryBehaviorUri(string behaviorUri)
  {
    System.Uri uri;
    if (!System.Uri.TryCreate(behaviorUri, System.UriKind.Absolute, out uri)) return false;
    return uri.Scheme == UserLibraryBehaviorUriScheme;
  }

  public static bool IsEmbeddedBehaviorUri(string behaviorUri)
  {
    System.Uri uri;
    if (!System.Uri.TryCreate(behaviorUri, System.UriKind.Absolute, out uri)) return false;
    return uri.Scheme == EmbeddedBehaviorUriScheme;
  }

  public bool IsBehaviorUri(string behaviorUri)
  {
    return IsEmbeddedBehaviorUri(behaviorUri) || IsBuiltinBehaviorUri(behaviorUri);
  }

  public static string GetIdOfBehaviorUri(string behaviorUri)
  {
    Debug.Assert(IsEmbeddedBehaviorUri(behaviorUri));
    System.Uri uri = new System.Uri(behaviorUri);
    return uri.PathAndQuery;
  }

  // This will happily overwrite.
  public void SaveToUserLibrary(string file, Behavior value)
  {
    Directory.CreateDirectory(GetUserBehaviorsRoot());

    string absPath = Path.Combine(GetUserBehaviorsRoot(), file);
    File.WriteAllText(absPath, value.javascript);

    if (!value.metadataJson.IsNullOrEmpty())
    {
      string metaAbsPath = absPath + ".metaJson";
      File.WriteAllText(metaAbsPath, value.metadataJson);
    }

    // Invalidate our cache, in case.
    loadedBehaviorsByAbsPath.Remove(absPath);
  }

  string currentBatchName = null;
  public void BeginBatchEdit(string name)
  {
    Debug.Assert(currentBatchName == null);
    currentBatchName = name;
  }

  public void EndBatchEdit(string name)
  {
    Debug.Assert(currentBatchName == name);
    currentBatchName = null;

    // Trigger actual sync.
    SyncDatabase();
  }

  public bool IsInitialized()
  {
    return initialized;
  }

  public struct PutEvent
  {
    public string id;
    public bool isNewBehavior;
    public bool fromThisUser;

    public PutEvent(string id, bool isNewBehavior, bool fromThisUser)
    {
      this.id = id;
      this.isNewBehavior = isNewBehavior;
      this.fromThisUser = fromThisUser;
    }
  }

  public struct DeleteEvent
  {
    public string id;
    public bool fromThisUser;

    public DeleteEvent(string id, bool fromThisUser)
    {
      this.id = id;
      this.fromThisUser = fromThisUser;
    }
  }

  public bool DoesBrainHandleCollisions(string brainId)
  {
    return brainIdsHandlingCollisions.Contains(brainId);
  }

  public int WriteAllEmbeddedBehaviorsToDirectory(string dir)
  {
    var allUris = from e in this.db.behaviors.GetAll()
                  select BehaviorSystem.IdToEmbeddedBehaviorUri(e.id);
    return WriteEmbeddedBehaviorsToDirectory(allUris, dir);
  }

  public int WriteEmbeddedBehaviorsToDirectory(IEnumerable<string> behaviorUris, string path)
  {
    int count = 0;
    foreach (string uri in behaviorUris)
    {
      string id = BehaviorSystem.GetIdOfBehaviorUri(uri);
      Behaviors.Behavior behavior = GetBehaviorData(uri);
      BehaviorCards.CardMetadata.Data meta = BehaviorCards.CardMetadata.GetMetaDataFor(behavior);
      string fileName = String.Join("_",
          meta.title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
      string baseBehaviorDir = Path.Combine(path, fileName);
      string behaviorDir = baseBehaviorDir;
      int i = 1;
      while (Directory.Exists(behaviorDir))
      {
        behaviorDir = baseBehaviorDir + " " + i;
        i++;
      }
      Directory.CreateDirectory(behaviorDir);
      Behaviors.Behavior.WriteToDirectory(behaviorDir, id, behavior);
      count++;
    }
    return count;
  }

  public static Dictionary<string, Behaviors.Behavior> LoadEmbeddedBehaviorsFromDirectory(string path)
  {
    Dictionary<string, Behaviors.Behavior> result = new Dictionary<string, Behaviors.Behavior>();
    foreach (string subDir in Directory.GetDirectories(path))
    {
      ReadDirectoryBehavior(subDir, result);
    }
    // In case the user picked a card folder
    ReadDirectoryBehavior(path, result);
    return result;
  }

  private static void ReadDirectoryBehavior(string subDir, Dictionary<string, Behaviors.Behavior> result)
  {
    if (Behaviors.Behavior.IsLegacyBehaviorDir(subDir))
    {
      try
      {
        string guid = null;
        Behavior beh = Behaviors.Behavior.ReadLegacyBehaviorDir(subDir, out guid);
        result[guid] = beh;
      }
      catch (System.IO.FileNotFoundException)
      {
        // Maybe user modified it badly? Ignore it.
      }
    }
    else
    {
      IEnumerable<string> files = Directory.EnumerateFiles(subDir, "*.js");
      if (files.Count() != 1)
      {
        // Maybe user modified it badly? Ignore it.
        return;
      }
      string guid = Path.GetFileNameWithoutExtension(files.First());
      if (guid.Length != 32)
      {
        // Skip - not a GUID.
        return;
      }
      try
      {
        Behavior beh = Behaviors.Behavior.ReadFromDirectory(subDir, guid);
        result[guid] = beh;
      }
      catch (System.IO.FileNotFoundException)
      {
        // Maybe user modified it badly? Ignore it.
      }
    }
  }

  public void PutBehaviors(Dictionary<string, Behaviors.Behavior> behaviors, bool overwrite = false)
  {
    foreach (var entry in behaviors)
    {
      if (!overwrite && EmbeddedBehaviorExists(entry.Key))
      {
        string newGuid = this.GenerateUniqueId();
        PutBehavior(newGuid, entry.Value);
      }
      else
      {
        PutBehavior(entry.Key, entry.Value);
      }
    }
  }

  [System.Serializable]
  public class SavedCardPacks
  {
    public List<SavedCardPack> cardPacks = new List<SavedCardPack>();
  }

  [System.Serializable]
  public class SavedCardPack
  {
    public ulong workshopId;
    public string workshopName;
    public string workshopDesc;
    public List<string> uris;

    public SavedCardPack(ulong workshopId, string workshopName, string workshopDesc, List<string> uris)
    {
      this.workshopId = workshopId;
      this.workshopName = workshopName;
      this.workshopDesc = workshopDesc;
      this.uris = uris;
    }
  }

  public void PutCardPack(ulong workshopId, string workshopName, string workshopDesc, IEnumerable<string> uris)
  {
    cardPacks[workshopId] = new SavedCardPack(workshopId, workshopName, workshopDesc, uris.ToList());
  }

  public SavedCardPacks GetCardPacks()
  {
    SavedCardPacks packs = new SavedCardPacks();
    foreach (var entry in cardPacks)
    {
      packs.cardPacks.Add(entry.Value);
    }
    return packs;
  }

  public void LoadCardPacks(SavedCardPacks packs)
  {
    foreach (SavedCardPack savedCardPack in packs.cardPacks)
    {
      cardPacks[savedCardPack.workshopId] = savedCardPack;
    }
  }

  public SavedCardPack GetCardPack(ulong workshopId)
  {
    return cardPacks[workshopId];
  }
}
