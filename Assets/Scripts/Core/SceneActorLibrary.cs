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
using System.Linq;
using System.IO;
using UnityEngine;

#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif

public class SceneActorLibrary : MonoBehaviour
{
  static string SojoIdPrefix = "c78581254ff845fc9383f170201cd4d8";

  static string ToSojoId(string id)
  {
    return $"{SojoIdPrefix}-{id}";
  }

  static string FromSojoId(string sojoId)
  {
    return sojoId.Substring(SojoIdPrefix.Length + 1);
  }

  [SerializeField] SojoSystem sojoSystem;

  // Called when, for example, another player adds a new actor to the scene lib.
  // Argument is the ID of the new prefab.
  public event System.Action<string> onActorPut;

  // Called when the prefab is deleted from the scene lib.
  // Argument is the ID of the deleted prefab.
  public event System.Action<string> onActorDelete;
  Dictionary<ulong, SavedActorPack> actorPacks = new Dictionary<ulong, SavedActorPack>();

  void Awake()
  {
    Util.FindIfNotSet(this, ref sojoSystem);
    sojoSystem.onSojoPut += OnSojoPut;
    sojoSystem.onSojoDelete += OnSojoDelete;
  }

  void OnSojoPut(Sojo newSojo)
  {
    if (newSojo.contentType != SojoType.ActorPrefab)
    {
      return;
    }

    string id = FromSojoId(newSojo.id);
    onActorPut?.Invoke(id);
  }

  void OnSojoDelete(Sojo newSojo)
  {
    if (newSojo.contentType != SojoType.ActorPrefab)
    {
      return;
    }

    string id = FromSojoId(newSojo.id);
    onActorDelete?.Invoke(id);
  }

  public ActorPrefab Get(string id)
  {
    var sojo = sojoSystem.GetSojoById(ToSojoId(id));
    if (sojo == null) return null;
    Debug.Assert(sojo.contentType == SojoType.ActorPrefab);
    return Deserialize(sojo);
  }

  ActorPrefabImpl Deserialize(Sojo sojo)
  {
    SavedActorPrefab saved = JsonUtility.FromJson<SavedActorPrefab>(sojo.content);
    saved.PerformUpgrades();
    Texture2D thumbnail = null;
    if (!saved.thumbnailJZB64.IsNullOrEmpty())
    {
      thumbnail = Util.ZippedJpegToTexture2D(System.Convert.FromBase64String(saved.thumbnailJZB64), false);
    }
    return new ActorPrefabImpl(saved, thumbnail, FromSojoId(sojo.id), sojoSystem);
  }

  public IEnumerable<ActorPrefab> GetAll()
  {
    foreach (var sojo in sojoSystem.GetAllSojosOfType(SojoType.ActorPrefab))
    {
      Debug.Assert(sojo.id.Contains(SojoIdPrefix));
      yield return Deserialize(sojo);
    }
  }

  public void Put(string id, VoosActor actor, Texture2D thumbnail)
  {
    string json = ExportToJson(actor, thumbnail);
    Sojo sojo = new Sojo(ToSojoId(id), actor.GetDisplayName(), SojoType.ActorPrefab, json);
    sojoSystem.PutSojo(sojo);
  }

  public void Delete(string id)
  {
    sojoSystem.DeleteSojo(ToSojoId(id));
  }

  SavedActorPrefab ToPrefab(VoosActor root, Texture2D thumbnail)
  {
    List<VoosActor.PersistedState> savedActors = new List<VoosActor.PersistedState>();
    Behaviors.Database brainDatabase = new Behaviors.Database();
    List<Sojo.Saved> sojos = new List<Sojo.Saved>();

    void ExportActorRecursive(VoosActor actor)
    {
      savedActors.Add(actor.Save());
      actor.GetBehaviorSystem().ExportBrain(actor.GetBrainName(), brainDatabase);

      foreach (string sojoId in GetUsedSojoIds(actor))
      {
        Sojo sojo = sojoSystem.GetSojoById(sojoId);
        if (sojo == null)
        {
          Util.LogError($"Could not find sojo of id {sojoId} for actor {actor.name}");
          continue;
        }
        sojos.Add(sojo.Save());
      }

      foreach (VoosActor child in actor.GetChildActors())
      {
        ExportActorRecursive(child);
      }
    }

    ExportActorRecursive(root);

    return new SavedActorPrefab
    {
      version = SavedActorPrefab.CurrentVersion,
      label = root.GetDisplayName(),
      thumbnailJZB64 = System.Convert.ToBase64String(Util.TextureToZippedJpeg(thumbnail)),
      brainDatabase = brainDatabase.Save(),
      sojos = sojos.ToArray(),
      actors = savedActors.ToArray()
    };
  }

  public string ExportToJson(VoosActor actor, Texture2D thumbnail)
  {
    return JsonUtility.ToJson(ToPrefab(actor, thumbnail), true);
  }

  public bool Exists(string id)
  {
    return sojoSystem.GetSojoById(ToSojoId(id)) != null;
  }

  public string FindOneActorUsingBehavior(string behaviorUri)
  {
    foreach (var sojo in sojoSystem.GetAllSojosOfType(SojoType.ActorPrefab))
    {
      Debug.Assert(sojo.id.Contains(SojoIdPrefix));
      SavedActorPrefab saved = JsonUtility.FromJson<SavedActorPrefab>(sojo.content);
      saved.PerformUpgrades();
      foreach (var brain in saved.brainDatabase.brains)
      {
        foreach (var use in brain.behaviorUses)
        {
          if (use.behaviorUri == behaviorUri)
          {
            // Too lazy to find the actual actor in the prefab..
            return $"Actor in prefab '{saved.label}'";
          }
        }
      }
    }
    return null;
  }

  public void Import(string id, SavedActorPrefab prefab)
  {
    string json = JsonUtility.ToJson(prefab, true);
    Sojo sojo = new Sojo(ToSojoId(id), prefab.label, SojoType.ActorPrefab, json);
    sojoSystem.PutSojo(sojo);
  }

  public SavedActorPrefab Export(string id)
  {
    var sojo = sojoSystem.GetSojoById(ToSojoId(id));
    Debug.Assert(sojo != null, "Could not find scene actor prefab for given id");
    return JsonUtility.FromJson<SavedActorPrefab>(sojo.content);
  }

  public void WritePrefabToDir(string id, string path)
  {
    string json = JsonUtility.ToJson(Export(id));
    string subDir = Path.Combine(path, "actorPrefab_" + System.DateTime.Now.ToString("yyyyMMddTHHmm"));
    Directory.CreateDirectory(subDir);

    string filePath = Path.Combine(subDir, id.ToValidFileName() + ".json");
    File.WriteAllText(filePath, json);
  }

  public static Dictionary<string, SavedActorPrefab> ReadPrefabsFromDir(
    string path,
#if USE_STEAMWORKS
    WorkshopItem workshopItem = null,
#endif
    Dictionary<string, SavedActorPrefab> prefabs = null)
  {
    if (prefabs == null) prefabs = new Dictionary<string, SavedActorPrefab>();
    IEnumerable<string> files = Directory.EnumerateFiles(path, "*.json");
    foreach (string file in files)
    {
      string id = Path.GetFileNameWithoutExtension(file);
      try
      {
        string json = File.ReadAllText(file);
        SavedActorPrefab prefab = JsonUtility.FromJson<SavedActorPrefab>(json);
#if USE_STEAMWORKS
        if (workshopItem != null)
        {
          id += $" (from \"{workshopItem.Name}\" on Workshop)";
          prefab.label += $" (from \"{workshopItem.Name}\" on Workshop)";
          prefab.workshopId = workshopItem.SteamNative.m_nPublishedFileId.ToString();
        }
#endif
        prefabs[id] = prefab;
      }
      catch (System.IO.FileNotFoundException)
      {
        // Maybe user modified it badly? Ignore it.
      }
    }

    foreach (string subDir in Directory.GetDirectories(path))
    {
      ReadPrefabsFromDir(subDir,
#if USE_STEAMWORKS
      workshopItem, 
#endif
      prefabs);
    }

    return prefabs;
  }

  public void PutPrefabs(Dictionary<string, SavedActorPrefab> prefabs, bool overwrite = false)
  {
    foreach (var entry in prefabs)
    {
      string id = entry.Key;
      int i = 0;
      while (!overwrite && Exists(id))
      {
        i++;
        id = entry.Key + "-" + i;
        entry.Value.label = id;
      }
      Import(id, entry.Value);
    }
  }

  [System.Serializable]
  public class SavedActorPacks
  {
    public List<SavedActorPack> actorPacks = new List<SavedActorPack>();
  }

  [System.Serializable]
  public class SavedActorPack
  {
    public ulong workshopId;
    public string workshopName;
    public string workshopDesc;
    public List<string> ids;

    public SavedActorPack(ulong workshopId, string workshopName, string workshopDesc, List<string> ids)
    {
      this.workshopId = workshopId;
      this.workshopName = workshopName;
      this.workshopDesc = workshopDesc;
      this.ids = ids;
    }
  }

  public void PutActors(ulong workshopId, string workshopName, string workshopDesc, IEnumerable<string> ids)
  {
    actorPacks[workshopId] = new SavedActorPack(workshopId, workshopName, workshopDesc, ids.ToList());
  }

  public SavedActorPacks GetActorPacks()
  {
    SavedActorPacks packs = new SavedActorPacks();
    foreach (var entry in actorPacks)
    {
      packs.actorPacks.Add(entry.Value);
    }
    return packs;
  }

  public void LoadActorPacks(SavedActorPacks packs)
  {
    foreach (SavedActorPack actorPack in packs.actorPacks)
    {
      actorPacks[actorPack.workshopId] = actorPack;
    }
  }

  public SavedActorPack GetActorPack(ulong workshopId)
  {
    return actorPacks[workshopId];
  }

  static IEnumerable<string> GetUsedSojoIds(VoosActor actor)
  {
    string pfxId = actor.GetPfxId();
    string sfxId = actor.GetSfxId();
    if (!pfxId.IsNullOrEmpty()) yield return pfxId;
    if (!sfxId.IsNullOrEmpty()) yield return sfxId;

    var brain = new ActorBehaviorsEditor(actor.GetName(), actor.GetEngine(), null);
    foreach (var beh in brain.GetAssignedBehaviors())
    {
      foreach (var prop in beh.GetProperties())
      {
        if (prop.propType == BehaviorProperties.PropType.Image
        || prop.propType == BehaviorProperties.PropType.ParticleEffect
        || prop.propType == BehaviorProperties.PropType.Prefab
        || prop.propType == BehaviorProperties.PropType.Sound)
        {
          if (prop.propType == BehaviorProperties.PropType.Prefab)
          {
            throw new System.Exception("Sorry, exporting an actor with prefab ref is not supported yet.");
          }

          // Yes, we should crash if the string cast fails.
          string sojoId = (string)prop.data;
          if (!sojoId.IsNullOrEmpty())
          {
            yield return sojoId;
          }
        }
      }
    }
  }
}
