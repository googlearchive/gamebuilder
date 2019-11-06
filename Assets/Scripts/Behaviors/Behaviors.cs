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
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.IO;

// Database structures for the VOOS Behavior system. NOTE: This does NOT
// guarantee anything about networked synchronization. That is the responsiblity
// of BehaviorSystem. These are simply local data structures, with facilities
// like serialization, acceleration and indexing, etc.
namespace Behaviors
{
  // Using struct for easy cloning.
  // But..kinda questioning that decision.
  [System.Serializable]
  public struct Behavior
  {
    // The human readable label. Not the GUID.
    public string label;

    public string javascript;

    // The code the player was editing, not yet committed.
    public string draftJavascript;

    // If this behavior was imported from the user's library, this stores the
    // file name. NOT a path. Just like, blah.js.
    public string userLibraryFile;

    // Any custom metadata users may want to store.
    public string metadataJson;

    public override bool Equals(object obj)
    {
      if (!(obj is Behavior))
      {
        return false;
      }

      var behavior = (Behavior)obj;
      return label == behavior.label &&
             javascript == behavior.javascript &&
             draftJavascript == behavior.draftJavascript &&
             userLibraryFile == behavior.userLibraryFile &&
             metadataJson == behavior.metadataJson;
    }

    public override int GetHashCode()
    {
      var hashCode = -284969125;
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(label);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(javascript);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(draftJavascript);
      hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(userLibraryFile);
      return hashCode;
    }

    public override string ToString()
    {
      return $"{label}, js={javascript.Substring(0, System.Math.Min(40, javascript.Length))}";
    }

    public string GetInlineCommentLabel()
    {
      // A bit hacky..
      string s = javascript;
      string firstLine = s.Split(new string[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None)[0];
      if (firstLine.Length > 3)
      {
        // Total hack. Eventually, we should call an exported function for this, like GetDescription(properties);
        return firstLine.Remove(0, 3).Replace("\\n", "\n");
      }
      else
      {
        return "";
      }
    }

    public string GetInlineCommentBody()
    {
      string s = javascript;
      string firstLine = s.Split(new string[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None)[0];
      if (firstLine.Length > 3)
      {
        // Total hack. Eventually, we should call an exported function for this, like GetDescription(properties);
        string[] splits = firstLine.Split(new string[] { "\\n" }, System.StringSplitOptions.None);
        if (splits.Length > 1)
        {
          return splits[1];
        }
      }
      return "";
    }

    public static void WriteToDirectory(string dirPath, string id, Behavior behavior)
    {
      Directory.CreateDirectory(dirPath);

      string jsPath = Path.Combine(dirPath, id + ".js");
      File.WriteAllText(jsPath, behavior.javascript);

      if (!behavior.metadataJson.IsNullOrEmpty())
      {
        string metaPath = Path.Combine(dirPath, id + ".json");
        File.WriteAllText(metaPath, behavior.metadataJson);
      }
    }

    public static Behavior ReadFromDirectory(string dirPath, string id)
    {
      string jsPath = Path.Combine(dirPath, id + ".js");
      string metaPath = Path.Combine(dirPath, id + ".json");

      string js = File.ReadAllText(jsPath);
      string metaJson = null;

      if (File.Exists(metaPath))
      {
        metaJson = File.ReadAllText(metaPath);
      }

      return new Behaviors.Behavior
      {
        javascript = js,
        metadataJson = metaJson
      };
    }

    public static void WriteToDirectoryConsole(string dirPath, Behavior behavior)
    {
      Directory.CreateDirectory(dirPath);

      string jsPath = Path.Combine(dirPath, "source.js");
      File.WriteAllText(jsPath, behavior.javascript);

      if (!behavior.metadataJson.IsNullOrEmpty())
      {
        string metaPath = Path.Combine(dirPath, "metadata.json");
        File.WriteAllText(metaPath, behavior.metadataJson);
      }
    }

    public static bool IsLegacyBehaviorDir(string dirPath)
    {
      string guid = Path.GetFileName(dirPath);
      string jsPath = Path.Combine(dirPath, "source.js");
      return guid.Length == 32 && File.Exists(jsPath);
    }

    public static Behavior ReadLegacyBehaviorDir(string dirPath, out string guid)
    {
      string jsPath = Path.Combine(dirPath, "source.js");
      string metaPath = Path.Combine(dirPath, "metadata.json");

      string js = File.ReadAllText(jsPath);
      string metaJson = null;

      if (File.Exists(metaPath))
      {
        metaJson = File.ReadAllText(metaPath);
      }

      guid = Path.GetFileName(dirPath);

      return new Behaviors.Behavior
      {
        javascript = js,
        metadataJson = metaJson
      };
    }

  }

  [System.Serializable]
  public struct PropertyAssignment : Util.IDeepCloneable<PropertyAssignment>
  {
    public string propertyName;
    public string valueJson;

    // Need this because JsonUtility doesn't work with simple types (int, float, etc.) directly.
    // NOTE: Do NOT use this for actual members. They won't JSON-ize. This is ONLY for the Set/GetValue
    // helpers below!
    private struct Wrapper<T>
    {
      public T value;
    }

    // Up to the user to provide the right type.
    public void SetValue<T>(T value)
    {
      valueJson = JsonUtility.ToJson(new Wrapper<T> { value = value });
    }

    // Up to the user to provide the right type.
    public T GetValue<T>()
    {
      return JsonUtility.FromJson<Wrapper<T>>(valueJson).value;
    }

    // Suitable for passing into behaviors.
    // The output will look like: {"health":{"value":123},"owner":{"value":"alice"},}
    // ..so each value is wrapped in a "value", but this is easily unwrapped on the JS side.
    public static string BuildPropertyBlockJson(IEnumerable<PropertyAssignment> assignments)
    {
      var builder = BlockJsonBuilder;
      builder.Clear();
      builder.Append("{");
      bool first = true;
      foreach (PropertyAssignment assignment in assignments)
      {
        if (!first)
        {
          builder.Append(",");
        }
        first = false;

        builder.Append("\"");
        builder.Append(assignment.propertyName);
        builder.Append("\":");
        builder.Append(assignment.valueJson);

      }
      builder.Append("}");
      return builder.ToString();
    }

    public PropertyAssignment DeepClone()
    {
      return this;
    }

    private static StringBuilder BlockJsonBuilder = new StringBuilder();
  }

  [System.Serializable]
  public class BehaviorUse : Util.IDeepCloneable<BehaviorUse>
  {
    // Only needs to be unique within a brain.
    public string id;
    public string behaviorUri;
    public string metadataJson;

    // TODO make this private, and add getter which DeepClone's it?
    public PropertyAssignment[] propertyAssignments;

    public BehaviorUse()
    {
      this.propertyAssignments = new PropertyAssignment[0];
    }

    // A bit dangerous...technically, no one should use = operator, but we
    // can't really prevent that..maybe we SHOULD really just use a class?
    public BehaviorUse DeepClone()
    {
      return new BehaviorUse
      {
        id = this.id,
        behaviorUri = this.behaviorUri,
        metadataJson = this.metadataJson,
        propertyAssignments = this.propertyAssignments.DeepClone()
      };
    }

    public T GetPropertyValue<T>(string propName, T defaultValue = default(T))
    {
      foreach (PropertyAssignment assignment in propertyAssignments)
      {
        if (assignment.propertyName == propName)
        {
          return assignment.GetValue<T>();
        }
      }
      return defaultValue;
    }

    public void SetPropertyValue<T>(string propName, T newValue)
    {
      for (int i = 0; i < propertyAssignments.Length; i++)
      {
        if (propertyAssignments[i].propertyName == propName)
        {
          propertyAssignments[i].SetValue(newValue);
          return;
        }
      }
      System.Array.Resize(ref propertyAssignments, propertyAssignments.Length + 1);
      propertyAssignments[propertyAssignments.Length - 1] = new PropertyAssignment();
      propertyAssignments[propertyAssignments.Length - 1].propertyName = propName;
      propertyAssignments[propertyAssignments.Length - 1].SetValue(newValue);
    }
  }

  // Many actors can share a brain. They each have their own memories though.
  [System.Serializable]
  public class Brain : Util.IDeepCloneable<Brain>
  {

    // Completely opaque, controlled by the user.
    public string metadataJson;

    public BehaviorUse[] behaviorUses;

    public Brain()
    {
      this.behaviorUses = new BehaviorUse[0];
    }

    public Brain DeepClone()
    {
      return new Brain
      {
        metadataJson = this.metadataJson,
        behaviorUses = this.behaviorUses.DeepClone()
      };
    }

    public void SetUse(BehaviorUse newValue)
    {
      behaviorUses[IndexOfUse(newValue.id)] = newValue;
    }

    public void AddUse(BehaviorUse newValue)
    {
      Debug.Assert(!newValue.id.IsNullOrEmpty());
      List<BehaviorUse> useList = new List<BehaviorUse>(behaviorUses);
      useList.Add(newValue);
      behaviorUses = useList.ToArray();
    }

    public void DeleteUse(string useId)
    {
      int index = IndexOfUse(useId);
      List<BehaviorUse> useList = new List<BehaviorUse>(behaviorUses);
      useList.RemoveAt(index);
      behaviorUses = useList.ToArray();
    }

    public BehaviorUse GetUse(string useId)
    {
      return behaviorUses[IndexOfUse(useId)].DeepClone();
    }

    public bool HasUse(string useId)
    {
      return IndexOfUse(useId) != -1;
    }

    public int IndexOfUse(string useId)
    {
      Debug.Assert(!useId.IsNullOrEmpty());

      for (int i = 0; i < behaviorUses.Length; i++)
      {
        if (behaviorUses[i].id == useId)
        {
          return i;
        }
      }
      return -1;
    }

    public IEnumerable<BehaviorUse> GetUses()
    {
      return behaviorUses;
    }
  }

  public class Database
  {
    public static string NewUID()
    {
      return Util.Generate32CharGuid();
    }

    [System.Serializable]
    struct LegacyBehaviorUse
    {
      public string brainId;
      public string behaviorUri;
      public string metadataJson;
      public PropertyAssignment[] propertyAssignments;
    }

    // TODO should probably make these private in order to maintain acceleration structures.
    public Util.Table<Behavior> behaviors = new Util.Table<Behavior>();
    public Util.Table<Brain> brains = new Util.Table<Brain>();

    [System.Serializable]
    public class Jsonable
    {
      public static int FirstVersionWithBrains = 1;
      public static int FirstVersionWithDefaultBehavior = 2;
      // Before, it was OK for a use/actor to refer to a brain that was not
      // actually in the "brains" table. This was because..we didn't store
      // anything of value per-brain. But now we have metadata - store it!
      public static int FirstVersionWithRequiredBrains = 3;
      // Two brains never refer to the same use, so this was an unnecessary
      // level of flexibility. It also added extra complexity, since making
      // copies of a brain meant entirely new use IDs, leading to very
      // error-prone code (like forgetting to update use IDs inside deck
      // properties..). So, just forgo all of that and put use info directly in
      // the brain itself. If two brains have the same use IDs, it's ok, since
      // they're only meant to be locally unique. 
      public static int FirstVersionWithoutUseTable = 4;
      public static int CurrentVersionNumber = 4;

      public int version;
      public string[] behaviorIds;
      public Behavior[] behaviors;
      public string[] brainIds;
      public Brain[] brains;

      // Legacy
      [SerializeField] string[] behaviorUseIds;
      [SerializeField] LegacyBehaviorUse[] behaviorUses;

      public void PerformUpgrades(HashSet<string> brainIdsUsedByActors)
      {
        if (version < FirstVersionWithBrains)
        {
          Debug.Assert(version == FirstVersionWithBrains - 1);
          version = FirstVersionWithBrains;

          Debug.Assert(brainIds.Length == 0);
          Debug.Assert(brains.Length == 0);

          HashSet<string> brainIdsToAdd = new HashSet<string>();
          foreach (LegacyBehaviorUse use in behaviorUses)
          {
            brainIdsToAdd.Add(use.brainId);
          }

          brainIds = new string[brainIdsToAdd.Count];
          brains = new Brain[brainIdsToAdd.Count];
          for (int i = 0; i < brains.Length; i++)
          {
            brains[i] = new Brain();
          }
          brainIdsToAdd.CopyTo(brainIds);
        }

        if (version < FirstVersionWithDefaultBehavior)
        {
          Debug.Assert(version == FirstVersionWithDefaultBehavior - 1);
          version = FirstVersionWithDefaultBehavior;

          // Add Default Behavior to every brain.

          HashSet<string> brainIds = new HashSet<string>();
          foreach (var use in behaviorUses)
          {
            brainIds.Add(use.brainId);
          }

          List<string> useIds = new List<string>();
          useIds.AddRange(behaviorUseIds);
          List<LegacyBehaviorUse> uses = new List<LegacyBehaviorUse>();
          uses.AddRange(behaviorUses);

          foreach (string brainId in brainIds)
          {
            useIds.Add(Behaviors.Database.NewUID());
            uses.Add(new LegacyBehaviorUse { brainId = brainId, behaviorUri = "builtin:Default Behavior", propertyAssignments = new Behaviors.PropertyAssignment[0] });
          }

          behaviorUseIds = useIds.ToArray();
          behaviorUses = uses.ToArray();
        }

        if (version < FirstVersionWithRequiredBrains)
        {
          HashSet<string> existingBrains = new HashSet<string>(brainIds);
          HashSet<string> referencedBrainIds = new HashSet<string>(behaviorUses.Select(use => use.brainId));
          List<string> brainIdsList = new List<string>(brainIds);
          List<Brain> brainsList = new List<Brain>(brains);
          // Add empty brain for every non-existant brain.
          foreach (string brainId in referencedBrainIds.Except(existingBrains))
          {
            brainIdsList.Add(brainId);
            brainsList.Add(new Brain());
          }
          brainIds = brainIdsList.ToArray();
          brains = brainsList.ToArray();

          // Upgrade done
          version = FirstVersionWithRequiredBrains;
        }

        if (version < FirstVersionWithoutUseTable)
        {
          Debug.Assert(behaviorUses != null, "Before FirstVersionWithoutUseTable, should have non-null behavior uses");

          // We now also require brains to exists, even if they're not
          // referenced by a use (ie. only referenced by an actor).
          if (brainIdsUsedByActors.Count > 0)
          {
            HashSet<string> brainIdsSet = new HashSet<string>(brainIds);
            List<string> brainIdsList = new List<string>(brainIds);
            List<Brain> brainsList = new List<Brain>(brains);
            foreach (string brainId in brainIdsUsedByActors.Except(brainIdsSet))
            {
              brainIdsList.Add(brainId);
              brainsList.Add(new Brain());
            }
            brainIds = brainIdsList.ToArray();
            brains = brainsList.ToArray();
          }

          // Now convert everything to "uses-in-brains" form.
          Dictionary<string, int> brainIdToIndex = new Dictionary<string, int>();
          for (int i = 0; i < brainIds.Length; i++)
          {
            string brainId = brainIds[i];
            Brain brain = brains[i];

            Debug.Assert(behaviorUses != null);

            List<int> useIndexes = new List<int>(
              from useIndex in Enumerable.Range(0, behaviorUses.Length)
              where behaviorUses[useIndex].brainId == brainId
              select useIndex);

            brain.behaviorUses = useIndexes.Select(index =>
            {
              LegacyBehaviorUse use = behaviorUses[index];
              var newUse = new BehaviorUse
              {
                id = behaviorUseIds[index],
                behaviorUri = use.behaviorUri,
                metadataJson = use.metadataJson,
                propertyAssignments = use.propertyAssignments.DeepClone()
              };
              return newUse;
            }).ToArray();
          }
          this.behaviorUseIds = new string[0];
          this.behaviorUses = new LegacyBehaviorUse[0];
          // Upgrade done
          version = FirstVersionWithoutUseTable;
        }

        AssertValid();
      }

      internal void AssertValid()
      {
        Debug.Assert(version == CurrentVersionNumber, "BDB version wrong");
        Debug.Assert(behaviorIds.Length == behaviors.Length, "BDB behaviorIDs length wrong");
        Debug.Assert(brainIds.Length == brains.Length, "BDB brainIDs length wrong");

        // Legacy arrays empty?
        Debug.Assert(behaviorUseIds.Length == 0, "BDB use IDs not empty");
        Debug.Assert(behaviorUses.Length == 0, "BDB uses not empty");
      }
    }

    public Jsonable Save()
    {
      Jsonable rv = new Jsonable();
      rv.version = Jsonable.CurrentVersionNumber;
      behaviors.GetJsonables(ref rv.behaviorIds, ref rv.behaviors);
      brains.GetJsonables(ref rv.brainIds, ref rv.brains);

      return rv;
    }

    public void LoadForNetworkInit(Jsonable saved)
    {
      Debug.Assert(saved.version == Jsonable.CurrentVersionNumber, "LoadForNetworkInit: Received wrong version number!");
      saved.AssertValid();
      behaviors.LoadJsonables(saved.behaviorIds, saved.behaviors);
      brains.LoadJsonables(saved.brainIds, saved.brains);
      // Assume the host already GC'd, so no need to call it here.
    }

    public void Load(Jsonable saved, bool removeUnusedBehaviors, HashSet<string> usedBrainIds)
    {
      saved.PerformUpgrades(usedBrainIds);
      saved.AssertValid();
      behaviors.LoadJsonables(saved.behaviorIds, saved.behaviors);
      brains.LoadJsonables(saved.brainIds, saved.brains);
      GarbageCollect(removeUnusedBehaviors, usedBrainIds);
    }

    public void GarbageCollect(bool removeUnusedBehaviors, HashSet<string> usedBrainIds)
    {
      brains.DeleteAll(brains.ComplementOf(usedBrainIds).ToList());

      if (removeUnusedBehaviors)
      {
        HashSet<string> usedEmbeddedBehaviors = new HashSet<string>(
          usedBrainIds.SelectMany(brainId => GetBrain(brainId).behaviorUses.Select(use => use.behaviorUri)).
          Where(uri => BehaviorSystem.IsEmbeddedBehaviorUri(uri)).
          Select(uri => BehaviorSystem.GetIdOfBehaviorUri(uri)));

        behaviors.DeleteAll(behaviors.ComplementOf(usedEmbeddedBehaviors).ToList());
      }
    }

    public IEnumerable<BehaviorUse> BehaviorUsesForBrain(string brainId)
    {
      Brain brain = GetBrain(brainId);
      foreach (var use in brain.behaviorUses)
      {
        yield return use;
      }
    }

    public IEnumerable<BehaviorUse> BehaviorUsesForBehavior(string behaviorUri)
    {
      foreach (var brain in brains.GetAll())
      {
        foreach (var use in brain.value.behaviorUses)
        {
          if (use.behaviorUri == behaviorUri)
          {
            yield return use;
          }
        }
      }
    }

    public Behavior GetBehavior(string id)
    {
      return behaviors.Get(id);
    }

    public BehaviorUse GetBehaviorUse(string brainId, string useId)
    {
      var brain = brains.Get(brainId);
      foreach (var use in brain.behaviorUses)
      {
        if (use.id == useId)
        {
          return use;
        }
      }
      throw new System.Exception($"Could not find use with useid={useId} for brainId={brainId}");
    }

    public Brain GetBrain(string id)
    {
      // Becareful..must clone!
      return brains.Get(id).DeepClone();
    }

    public void DebugLog()
    {
      behaviors.DebugLog("behavior");
      brains.DebugLog("brain");
    }
  }
}
