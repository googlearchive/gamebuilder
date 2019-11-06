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
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public interface ActorPrefab
{
  string GetId();
  string GetLabel();
  string GetDescription();
  string GetWorkshopId();
  Texture2D GetThumbnail();
  VoosActor Instantiate(VoosEngine engine, BehaviorSystem behaviorSystem, Vector3 position, Quaternion rotation, System.Action<VoosActor> setupActor);
  Vector3 GetLocalScale();
  Vector3 GetRenderableOffset();
  Quaternion GetRenderableRotation();
  string GetRenderableUri();
  string GetAssetPackName();
  string GetCategory();
}

[System.Serializable]
public class SavedActorPrefab
{
  public static int FirstVersion = 1;
  public static int FirstVersionWithHierarchies = 2;
  public static int CurrentVersion = 2;

  public int version;

  // Some human-readable label for this prefab.
  public string label;

  public VoosActor.PersistedState[] actors;

  // This database only has the data necessary for the actor brain(s).
  public Behaviors.Database.Jsonable brainDatabase;

  public Sojo.Saved[] sojos;

  public string thumbnailJZB64;

  public string category;
  public string workshopId;

  // LEGACY ONLY
  [SerializeField] VoosActor.PersistedState actorData;

  internal void PerformUpgrades()
  {
    // Kinda silly, but makes things consistent.
    if (version < FirstVersion)
    {
      version = FirstVersion;
    }

    if (version < FirstVersionWithHierarchies)
    {
      actors = new VoosActor.PersistedState[1] { this.actorData };
      version = FirstVersionWithHierarchies;
    }

    HashSet<string> usedBrainIds = new HashSet<string>();
    foreach (var actor in actors)
    {
      usedBrainIds.Add(actor.brainName);
    }

    brainDatabase.PerformUpgrades(usedBrainIds);

    AssertValid();
  }

  public void AssertValid()
  {
    Debug.Assert(version == CurrentVersion, "SavedActorPrefab bad version");
    brainDatabase.AssertValid();
  }
}

public class ActorPrefabImpl : ActorPrefab
{
  SavedActorPrefab saved;
  Texture2D thumbnail;
  string id;
  SojoSystem sojos;

  public ActorPrefabImpl(SavedActorPrefab saved, Texture2D thumbnail, string id, SojoSystem sojos)
  {
    saved.AssertValid();
    this.saved = saved;
    this.thumbnail = thumbnail;
    this.id = id;
    this.sojos = sojos;
  }

  public string GetDescription()
  {
    return saved.actors[0].description;
  }

  public string GetLabel()
  {
    return saved.label;
  }

  public string GetCategory()
  {
    return saved.category;
  }

  public string GetWorkshopId()
  {
    return saved.workshopId;
  }

  public Vector3 GetLocalScale()
  {
    return saved.actors[0].localScale;
  }

  public Vector3 GetRenderableOffset()
  {
    return saved.actors[0].renderableOffset;
  }

  public string GetRenderableUri()
  {
    return saved.actors[0].renderableUri;
  }

  public Quaternion GetRenderableRotation()
  {
    return saved.actors[0].renderableRotation;
  }

  public Texture2D GetThumbnail()
  {
    return thumbnail;
  }

  public string GetId()
  {
    return id;
  }

  public string GetAssetPackName()
  {
    if (id.Contains("/"))
    {
      // First component of URI is the asset pack name.
      // If the URI is "Space/RadarDish" then the asset pack name is "Space".
      return id.Substring(0, id.IndexOf('/'));
    }
    return "";
  }

  public virtual VoosActor Instantiate(VoosEngine engine, BehaviorSystem behaviorSystem, Vector3 position, Quaternion rotation, System.Action<VoosActor> setupActor)
  {
    // TODO TODO we should make a copy of 'saved' in case Instantiate is called again..
    if (this.saved.sojos != null)
    {
      foreach (var savedSojo in this.saved.sojos)
      {
        if (sojos.GetSojoById(savedSojo.id) == null)
        {
          sojos.PutSojo(Sojo.Load(savedSojo));
        }
      }
    }

    // Import behaviors. For now, create entirely new brain IDs.
    var brainIdMap = new Dictionary<string, string>();
    for (int i = 0; i < this.saved.brainDatabase.brainIds.Length; i++)
    {
      string brainId = this.saved.brainDatabase.brainIds[i];
      string newId = behaviorSystem.GenerateUniqueId();
      brainIdMap[brainId] = newId;
      this.saved.brainDatabase.brainIds[i] = newId;
    }

    // Use the new brain IDs..
    for (int i = 0; i < saved.actors.Length; i++)
    {
      if (brainIdMap.ContainsKey(saved.actors[i].brainName))
      {
        saved.actors[i].brainName = brainIdMap[saved.actors[i].brainName];
      }
      else
      {
        // Always default to default brain. Some prefabs have dangling brain
        // IDs, and bad stuff can happen if we just keep those.
        Util.LogWarning($"WARNING: Actor prefab '{saved.actors[i].displayName}' ({saved.actors[i].name}) had dangling brainName: {saved.actors[i].brainName}");
        saved.actors[i].brainName = VoosEngine.DefaultBrainUid;
      }
    }

    var expectedBrainIds = new HashSet<string>(from actor in this.saved.actors select actor.brainName);
    behaviorSystem.MergeNonOverwrite(saved.brainDatabase, expectedBrainIds);

    // Instantiate the actor hierarchy - very important we update the parent
    // references, as well as any property references in the use..

    // Create new names for all actors
    var actorNameMap = new Dictionary<string, string>();
    foreach (var savedActor in saved.actors)
    {
      actorNameMap[savedActor.name] = engine.GenerateUniqueId();
    }

    // Compute xform to go from saved to instance... then just apply to all.
    var savedRoot = saved.actors[0];

    Matrix4x4 savedRootLocalToWorld = new Matrix4x4();
    savedRootLocalToWorld.SetTRS(savedRoot.position, savedRoot.rotation, savedRoot.localScale);

    Matrix4x4 instRootLocalToWorld = new Matrix4x4();
    instRootLocalToWorld.SetTRS(position, rotation, savedRoot.localScale);

    Matrix4x4 savedToInstance = instRootLocalToWorld * savedRootLocalToWorld.inverse;

    // Instantiate the tree.
    List<VoosActor> instances = new List<VoosActor>();
    foreach (var savedActor in saved.actors)
    {
      string instanceName = actorNameMap[savedActor.name];

      Vector3 instPos = savedToInstance * savedActor.position.ToHomogeneousPosition();
      Quaternion instRot = savedToInstance.rotation * savedActor.rotation;

      System.Action<VoosActor> setupThisActor = actor =>
      {
        Debug.Assert(actor.GetName() == instanceName, "New instance did not have the new name we generated");
        // Push the saved data to this new actor
        var actorDataCopy = savedActor;
        actorDataCopy.position = instPos;
        actorDataCopy.rotation = instRot;
        actorDataCopy.name = instanceName;

        // Make sure we update the transform parent!
        if (!actorDataCopy.transformParent.IsNullOrEmpty()
          && actorNameMap.ContainsKey(actorDataCopy.transformParent))
        {
          actorDataCopy.transformParent = actorNameMap[actorDataCopy.transformParent];
        }

        if (!actorDataCopy.spawnTransformParent.IsNullOrEmpty()
          && actorNameMap.ContainsKey(actorDataCopy.spawnTransformParent))
        {
          actorDataCopy.spawnTransformParent = actorNameMap[actorDataCopy.spawnTransformParent];
        }

        actor.UpdateFrom(actorDataCopy);
        setupActor(actor);
      };

      VoosActor instance = engine.CreateActor(instPos, instRot, setupThisActor, instanceName);
      instances.Add(instance);
    }

    // FINALLY...fix up any actor refs!
    foreach (VoosActor inst in instances)
    {
      var brain = ActorBehaviorsEditor.FromActor(inst);
      foreach (var assigned in brain.GetAssignedBehaviors())
      {
        foreach (var prop in assigned.GetProperties())
        {
          if (prop.propType == BehaviorProperties.PropType.Actor)
          {
            string refActorName = (string)prop.data;
            if (!refActorName.IsNullOrEmpty() && actorNameMap.ContainsKey(refActorName))
            {
              prop.SetData(actorNameMap[refActorName]);
            }
          }
          else if (prop.propType == BehaviorProperties.PropType.ActorGroup)
          {
            ActorGroupSpec group = ActorGroupSpec.FromString((string)prop.data);
            if (group.mode == ActorGroupSpec.Mode.BY_NAME && actorNameMap.ContainsKey(group.tagOrName))
            {
              group = group.WithTagOrName(actorNameMap[group.tagOrName]);
              prop.SetData(group.ToString());
            }
          }
        }
      }
    }

    return instances[0];
  }
}

// TODO rename to BuiltinPrefabLibrary..
public class PrefabLibrary : MonoBehaviour
{
  static string ActorPrefabExtension = "actor-prefab.voos";

  static int MAX_PREFABS_PER_FRAME = 3;

  public Util.AbstractLocation rootLocation;
  public string subdirectory;

  SojoSystem sojos;

  List<ActorPrefab> actorPrefabs = new List<ActorPrefab>();

  event System.Action<ActorPrefab> prefabProcessor;

  string GetLibraryRoot()
  {
    return Path.Combine(rootLocation.GetConcretePath(), subdirectory);
  }

  string GetActorPrefabsRoot()
  {
    return Path.Combine(GetLibraryRoot(), "ActorPrefabs");
  }

  void Awake()
  {
    Util.UpgradeUserDataDir();

    Util.FindIfNotSet(this, ref sojos);

    Directory.CreateDirectory(GetLibraryRoot());
    Util.Log($"{this.name} creating lib root at: {GetLibraryRoot()}");

    Directory.CreateDirectory(GetActorPrefabsRoot());
    Util.Log($"{this.name} creating prefabs root at: {GetActorPrefabsRoot()}");

    StartCoroutine(ReloadActorPrefabs(false));
  }

  string GetActorThumbnailPath(string uri)
  {
    return Path.Combine(GetActorPrefabsRoot(), $"{uri}-thumbnail.png");
  }

  ActorPrefab LoadActorPrefab(string uri)
  {
    string voosFilePath = GetPathForUri(uri);
    string json = File.ReadAllText(voosFilePath);
    SavedActorPrefab actorPrefab = JsonUtility.FromJson<SavedActorPrefab>(json);
    actorPrefab.PerformUpgrades();

    // Load thumbnail
    string thumbnailPng = GetActorThumbnailPath(uri);
    Texture2D thumbnail = Util.ReadPngToTexture(thumbnailPng);
    if (thumbnail == null && !actorPrefab.thumbnailJZB64.IsNullOrEmpty())
    {
      thumbnail = Util.ZippedJpegToTexture2D(
        System.Convert.FromBase64String(actorPrefab.thumbnailJZB64));
    }
    return new ActorPrefabImpl(actorPrefab, thumbnail, uri, sojos);
  }

  [System.Serializable]
  struct LibraryMetadata
  {
    public string[] sortedFolders;
  }

  IEnumerator ReloadActorPrefabs(bool immediate)
  {
    string actorPrefabsRoot = GetActorPrefabsRoot();

    actorPrefabs.Clear();

    var alreadyReadUris = new HashSet<string>();

    System.Action<string> readInFile = (string uri) =>
    {
      string filePath = GetPathForUri(uri);
      try
      {
        if (alreadyReadUris.Contains(uri))
        {
          return;
        }
        alreadyReadUris.Add(uri);
        actorPrefabs.Add(LoadActorPrefab(uri));
        prefabProcessor?.Invoke(actorPrefabs[actorPrefabs.Count - 1]);
      }
      catch (System.Exception e)
      {
        Debug.LogError($"Exception while trying to actor prefab URI '{uri}' from file {filePath}. Skipping it. The exception: {e.ToString()}");
      }
    };

    System.Func<string, string> ProcessURI = (string actorVoosFile) =>
    {
      string uri = actorVoosFile
               .Substring(actorPrefabsRoot.Length + 1) // + 1 is to to strip off / or \
               .Replace('\\', '/');
      // Remove extension. This is safe because of the asserts above.
      return uri.Substring(0, uri.Length - ActorPrefabExtension.Length - 1);
    };

    System.Func<string, List<string>> GenerateURIsFromFolder = (string folder) =>
     {
       List<string> uriList = new List<string>();
       foreach (string actorVoosFile in Directory.GetFiles(Path.Combine(actorPrefabsRoot, folder), $"*.{ActorPrefabExtension}", SearchOption.AllDirectories))
       {
         // Convert absolute file path into a prefab URI (which is the path relative to actorPrefabsRoot, without the extension).
         Debug.Assert(actorVoosFile.StartsWith(actorPrefabsRoot), "Full path does not begin with root path?? " + actorVoosFile);
         Debug.Assert(actorVoosFile.EndsWith("." + ActorPrefabExtension), "Full path does not end with extension?? " + actorVoosFile);
         uriList.Add(ProcessURI(actorVoosFile));
       }
       return uriList;
     };


    //read sorted folders 
    List<string> sortedFolders = new List<string>();
    string folderMetaJsonPath = Path.Combine(GetActorPrefabsRoot(), "folderMetadata.json");
    if (File.Exists(folderMetaJsonPath))
    {
      var metadata = JsonUtility.FromJson<LibraryMetadata>(File.ReadAllText(folderMetaJsonPath));
      sortedFolders.AddRange(metadata.sortedFolders);
    }

    int numProcessedThisFrame = 0;
    foreach (string folderpath in sortedFolders)
    {
      foreach (string uri in GenerateURIsFromFolder(folderpath))
      {
        readInFile(uri);
        if (!immediate)
        {
          numProcessedThisFrame++;
          if (numProcessedThisFrame >= MAX_PREFABS_PER_FRAME)
          {
            numProcessedThisFrame = 0;
            yield return null;
          }
        }
      }
    }

    // Search for other files that were not in the list of sorted ones.
    foreach (string actorVoosFile in Directory.GetFiles(actorPrefabsRoot, $"*.{ActorPrefabExtension}", SearchOption.AllDirectories))
    {
      // Convert absolute file path into a prefab URI (which is the path relative to actorPrefabsRoot, without the extension).
      Debug.Assert(actorVoosFile.StartsWith(actorPrefabsRoot), "Full path does not begin with root path?? " + actorVoosFile);
      Debug.Assert(actorVoosFile.EndsWith("." + ActorPrefabExtension), "Full path does not end with extension?? " + actorVoosFile);
      readInFile(ProcessURI(actorVoosFile));
      if (!immediate)
      {
        numProcessedThisFrame++;
        if (numProcessedThisFrame >= MAX_PREFABS_PER_FRAME)
        {
          numProcessedThisFrame = 0;
          yield return null;
        }
      }
    }
  }

  private string GetPathForUri(string uri)
  {
    return Path.Combine(GetActorPrefabsRoot(), $"{uri}.{ActorPrefabExtension}");
  }

  public ActorPrefab Get(string id)
  {
    return LoadActorPrefab(id);
  }

  public bool PrefabExists(string id)
  {
    return File.Exists(GetPathForUri(id));
  }

  public IEnumerable<ActorPrefab> GetAll()
  {
    return actorPrefabs;
  }

  public void AddPrefabsProcessor(System.Action<ActorPrefab> processor)
  {
    // Process ones we have already, and set it to process future ones.
    foreach (var prefab in actorPrefabs)
    {
      processor?.Invoke(prefab);
    }
    this.prefabProcessor += processor;
  }
}
