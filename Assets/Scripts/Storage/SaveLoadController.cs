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

using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;
using Unity.Collections;
using System.Text;
using System.Linq;
using UnityEngine.SceneManagement;
using System.IO;

public class SaveLoadController : MonoBehaviour
{

  static int FirstVersionWithScriptableControls = 2;
  static int FirstVersionWithDefaultBrain = 3;
  static int FirstVersionWithSojoDatabase = 4;
  static int FirstVersionWithDefaultBrainPanels = 5;

  // Specific for the scene-level card/behavior library feature. From this
  // version and above, we will *not* garbage collect behaviors on load. A
  // behavior/card could exist just in the library without any actors using it,
  // and that's totally valid.
  static int FirstVersionAllowingUnusedBehaviors = 6;
  static int FirstVersionWithLegacyWarning = 7;

  // Change this any time things become non-backwards compatible.
  static int CURRENT_SAVE_GAME_VERSION = 7;

  public class MoreRecentVersionNumberException : System.Exception
  {
  }

  [System.Serializable]
  public struct SaveGame
  {
    public int version;
    public VoosEngine.PersistedState voosEngineState;
    public TerrainManager.PersistedState gridState;
    public Behaviors.Database.Jsonable behaviorDatabase;
    public GameBuilderStage.Persisted stage;
    public SojoDatabase.Saved sojoDatabase;
    public BehaviorSystem.SavedCardPacks cardPacks;
    public SceneActorLibrary.SavedActorPacks actorPacks;
  }

  [SerializeField] VoosEngine engine;
  [SerializeField] TerrainManager terrain;
  [SerializeField] BehaviorSystem behaviors;
  [SerializeField] GameBuilderStage stage;
  [SerializeField] SojoSystem sojoSystem;
  [SerializeField] SceneActorLibrary sceneActorLibrary;

  DynamicPopup popups;

  class SaveRequest
  {
    public string voosPath;
    public System.Action onSaveComplete;
  }

  Queue<SaveRequest> saveJobQueue = new Queue<SaveRequest>();
  SaveRequest activeSaveRequest = null;
  JobHandle activeWriteJobHandle;

  public static bool SuppressLegacyWarning = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref terrain);
    Util.FindIfNotSet(this, ref behaviors);
    Util.FindIfNotSet(this, ref stage);
    Util.FindIfNotSet(this, ref sojoSystem);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref sceneActorLibrary);
  }

  void Update()
  {
    // Developer help
    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
    {
#if USE_FILEBROWSER
      var filePath = Crosstales.FB.FileBrowser.SaveFile("Save scene", "", "", "voos");
      if (!filePath.IsNullOrEmpty())
      {
        this.RequestSave(filePath, () =>
        {
          Util.Log($"OK saved to {filePath}");
        });
      }
#endif
    }

    if (activeSaveRequest != null && activeWriteJobHandle.IsCompleted)
    {
      activeSaveRequest.onSaveComplete?.Invoke();
      activeSaveRequest = null;
    }

    if (activeSaveRequest == null && saveJobQueue.Count > 0)
    {
      activeSaveRequest = saveJobQueue.Dequeue();
      StartCoroutine(SaveRoutine(activeSaveRequest.voosPath));
    }
  }

  IEnumerator SaveRoutine(string filePath)
  {
    // TODO should probably get this from master controller, just as we Load by asking
    // the master controller to load. Also, master controller might have other stuff to save, outside of the voos stuff.
    SaveGame save;
    save.version = CURRENT_SAVE_GAME_VERSION;

    using (Util.Profile("engine.GetPersistedState()"))
    {
      save.voosEngineState = engine.GetPersistedState();
    }
    yield return null;

    save.gridState = new TerrainManager.PersistedState(); // Suppress uninit error
    yield return terrain.GetPersistedStateAsync(state => save.gridState = state);

    using (Util.Profile("behaviors.SaveDatabase()"))
    {
      save.behaviorDatabase = behaviors.SaveDatabase();
    }
    using (Util.Profile("GameBuilderStage.Save"))
    {
      save.stage = stage.Save();
    }
    using (Util.Profile("sojoSystem.Save()"))
    {
      save.sojoDatabase = sojoSystem.SaveDatabase();
    }
    using (Util.Profile("SavedCardPacks.Save()"))
    {
      save.cardPacks = behaviors.GetCardPacks();
    }
    using (Util.Profile("SavedActorPacks.Save()"))
    {
      save.actorPacks = sceneActorLibrary.GetActorPacks();
    }

    string jsonContents = null;
    using (Util.Profile("JsonUtility.ToJson"))
    {
      jsonContents = JsonUtility.ToJson(save, true);
    }

    using (Util.Profile("StartWriteJob"))
    {
      WriteJob jobData = new WriteJob();
      byte[] filePathBytes = Encoding.UTF8.GetBytes(filePath);
      jobData.filePathBytes = new NativeArray<byte>(filePathBytes.Length, Allocator.TempJob);
      jobData.filePathBytes.CopyFrom(filePathBytes);
      byte[] jsonContentsBytes = Encoding.UTF8.GetBytes(jsonContents);
      jobData.jsonContentsBytes = new NativeArray<byte>(jsonContentsBytes.Length, Allocator.TempJob);
      jobData.jsonContentsBytes.CopyFrom(jsonContentsBytes);
      activeWriteJobHandle = jobData.Schedule();
      JobHandle.ScheduleBatchedJobs();
      // Debug.Log($"OK scheduled save game write to {filePath}");
    }
  }

  public void RequestSave(string filePath, System.Action onSaveComplete)
  {
    SaveRequest req = new SaveRequest() { voosPath = filePath, onSaveComplete = onSaveComplete };
    saveJobQueue.Enqueue(req);
  }

  private void AddDefaultPlayerControlsToPlayerBrain()
  {
    string controlsUri = BehaviorSystem.IdToBuiltinBehaviorUri("Player Controls");
    Behaviors.PropertyAssignment speedProp = new Behaviors.PropertyAssignment { propertyName = "Speed", valueJson = "{\"value\":5}" };
    if (!behaviors.HasBrain("Player"))
    {
      behaviors.PutBrain("Player", new Behaviors.Brain());
    }
    var playerBrain = behaviors.GetBrain("Player");
    playerBrain.AddUse(new Behaviors.BehaviorUse
    {
      id = behaviors.GenerateUniqueId(),
      behaviorUri = controlsUri,
      propertyAssignments = new Behaviors.PropertyAssignment[] { speedProp }
    });

    if (!behaviors.HasBrain("e04de2718a594fc0afd2cb55bd4fa8dc"))
    {
      behaviors.PutBrain("e04de2718a594fc0afd2cb55bd4fa8dc", new Behaviors.Brain());
    }
    var playerInstBrain = behaviors.GetBrain("e04de2718a594fc0afd2cb55bd4fa8dc");
    playerInstBrain.AddUse(new Behaviors.BehaviorUse
    {
      id = behaviors.GenerateUniqueId(),
      behaviorUri = controlsUri,
      propertyAssignments = new Behaviors.PropertyAssignment[] { speedProp }
    });
  }

  private void SetupDefaultBrain()
  {
    if (!behaviors.HasBrain(VoosEngine.DefaultBrainUid))
    {
      behaviors.PutBrain(VoosEngine.DefaultBrainUid, new Behaviors.Brain());
    }
    var defaultBrain = behaviors.GetBrain(VoosEngine.DefaultBrainUid);
    defaultBrain.AddUse(new Behaviors.BehaviorUse
    {
      id = behaviors.GenerateUniqueId(),
      behaviorUri = "builtin:Default Behavior",
    });
    behaviors.PutBrain(VoosEngine.DefaultBrainUid, defaultBrain);
  }

  private void SetupDefaultBrainPanels()
  {
    // NOTE: As of 20190415, we don't actually want default brain panels
    // anymore. It's wasteful and unnecessary. So I removed this code. It seems
    // a bit dangerous to ret-con an upgrade function, but considering this only
    // performed a purely aesthetic upgrade (empty panels that do nothing), I'm
    // ok with it. We also had to modify minimal-scene to no longer contain
    // default brain panels.
  }

  IEnumerator PerformDeferredBehaviorUpgrades(int loadedVersion)
  {
    while (!behaviors.IsInitialized())
    {
      yield return null;
    }
    if (loadedVersion < FirstVersionWithScriptableControls)
    {
      AddDefaultPlayerControlsToPlayerBrain();
    }

    if (loadedVersion < FirstVersionWithDefaultBrain)
    {
      SetupDefaultBrain();
    }

    if (loadedVersion < FirstVersionWithDefaultBrainPanels)
    {
      SetupDefaultBrainPanels();
    }
  }

  const string OLD_SAVE_MESSAGE = "This project was created with a previous version of Game Builder and may not function properly.\n\n";

  public void Load(SaveGame saved, string voosFilePath = null)
  {
    StartCoroutine(PerformDeferredBehaviorUpgrades(saved.version));

    bool removeUnusedBehaviors = saved.version < FirstVersionAllowingUnusedBehaviors;
    var usedBrainIds = VoosEngine.GetUsedBrainIds(saved.voosEngineState.actors);

    if (saved.version < FirstVersionWithLegacyWarning)
    {
      behaviors.returnEmptyForMissingBuiltinBehaviors = true;
    }
    behaviors.LoadDatabase(saved.behaviorDatabase, removeUnusedBehaviors, usedBrainIds);

    engine.SetPersistedState(saved.voosEngineState);
    // Load stage before terrain..dependency there for world size. Not sure if good...shouldn't terrain just have its own size?
    stage.Load(saved.stage);
    terrain.SetPersistedState(stage.GetGroundSize(), saved.gridState);

    sojoSystem.Reset();
    if (saved.version >= FirstVersionWithSojoDatabase)
    {
      sojoSystem.LoadDatabase(saved.sojoDatabase);
    }
    if (saved.cardPacks != null)
    {
      behaviors.LoadCardPacks(saved.cardPacks);
    }
    if (saved.actorPacks != null)
    {
      sceneActorLibrary.LoadActorPacks(saved.actorPacks);
    }

    if (saved.version < FirstVersionWithLegacyWarning && !SuppressLegacyWarning)
    {
      popups.ShowTwoButtons(OLD_SAVE_MESSAGE,
      "Open Project Location", () => { Application.OpenURL("file://" + System.IO.Path.GetDirectoryName(voosFilePath)); },
      "OK", null, 800f);
    }
  }

  public static SaveGame ReadSaveGame(string filePath)
  {
    Debug.Log($"Loading world from {filePath}..");
    string jsonContents = File.ReadAllText(filePath);
    SaveGame save = JsonUtility.FromJson<SaveGame>(jsonContents);

    if (save.version > CURRENT_SAVE_GAME_VERSION)
    {
      throw new MoreRecentVersionNumberException();
    }

    return save;
  }

  public struct WriteJob : IJob
  {
    [DeallocateOnJobCompletionAttribute]
    public NativeArray<byte> filePathBytes;
    [DeallocateOnJobCompletionAttribute]
    public NativeArray<byte> jsonContentsBytes;

    public void Execute()
    {
      string filePath = Encoding.UTF8.GetString(filePathBytes.ToArray());
      string jsonContents = Encoding.UTF8.GetString(jsonContentsBytes.ToArray());
      Util.SetNormalFileAttributes(filePath);
      File.WriteAllText(filePath, jsonContents);
    }
  }
}
