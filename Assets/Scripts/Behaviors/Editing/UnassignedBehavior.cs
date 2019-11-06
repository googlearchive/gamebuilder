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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using BehaviorProperties;

// Ie. a behavior in the library (that can be added to a brain/actor).
// TODO rename this to just.. Behavior
public class UnassignedBehavior
{
  public readonly BehaviorSystem behaviorSystem;
  private static string CLAIM_PREFIX = "Behavior:";

  // TODO This is actually modified when behaviors are copied. Could encapsulate
  // this better.
  public readonly string behaviorUri;

  // Constructor for unassigned
  public UnassignedBehavior(string behaviorUri, BehaviorSystem behaviorSystem)
  {
    this.behaviorSystem = behaviorSystem;
    this.behaviorUri = behaviorUri;
  }

  public Behaviors.Behavior GetBehavior()
  {
    Debug.Assert(IsValid(), "Function called on invalid UnassignedBehavior");
    return behaviorSystem.GetBehaviorData(behaviorUri);
  }

  public bool IsBehaviorReadOnly()
  {
    return BehaviorSystem.IsBuiltinBehaviorUri(behaviorUri);
  }

  public void SetMetadataJson(string metadataJson)
  {
    Debug.Assert(IsValid(), "Function called on invalid UnassignedBehavior");
    Behaviors.Behavior behavior = GetBehavior();
    behavior.metadataJson = metadataJson;
    behaviorSystem.PutBehavior(BehaviorSystem.GetIdOfBehaviorUri(behaviorUri), behavior);
  }

  public bool IsLegacyBuiltin()
  {
    return behaviorSystem.IsLegacyBuiltinBehavior(behaviorUri);
  }

  public string GetId()
  {
    if (BehaviorSystem.IsEmbeddedBehaviorUri(behaviorUri))
    {
      return BehaviorSystem.GetIdOfBehaviorUri(behaviorUri);
    }
    return null;
  }

  public static string GetClaimResourceId(string id)
  {
    return CLAIM_PREFIX + id;
  }

  public string GetDescription()
  {
    return GetBehavior().GetInlineCommentBody();
  }

  public string GetScriptInlineDocumentation()
  {
    return GetBehavior().GetInlineCommentLabel();
  }

  public virtual string GetName()
  {
    return GetScriptInlineDocumentation();
  }

  public string GetCode()
  {
    return GetBehavior().javascript;
  }

  // TODO rename this to like "CommitCode"
  public void SetCode(string s)
  {
    Debug.Assert(IsValid(), "Function called on invalid UnassignedBehavior");
    Behaviors.Behavior behavior = GetBehavior();

    if (behavior.javascript == s)
    {
      // Debug.Log("No actual change to behavior code. Doing nothing.");
      return;
    }

    behavior.javascript = s;
    behavior.draftJavascript = null;
    behaviorSystem.PutBehavior(BehaviorSystem.GetIdOfBehaviorUri(behaviorUri), behavior);
  }

  public PropEditor[] GetDefaultProperties()
  {
    return EnumeratePropDefs().Select(propDef => new PropEditor(propDef, null, null)).ToArray();
  }

  static System.Text.StringBuilder SearchableStringBuilder = new System.Text.StringBuilder();

  public string GetMetadataJson()
  {
    return this.GetBehavior().metadataJson;
  }

  public string GetBehaviorUri()
  {
    return behaviorUri;
  }

  // We will now figure out what properties the behavior declares. The
  // syntax for this depends on the API version. On V1, they used to be
  // declared in comments like this:
  //
  //     // property Number speed 12
  //     // property String nickname foo
  //     // property Boolean isHappy true
  //     // property Actor target
  //
  // In APIv2, they are now declared in a PROPS object like this:
  //
  //     export const PROPS = [
  //       propNumber("speed", 12),
  //       propString("nickname", "foo"),
  //       propBoolean("isHappy", true),
  //       propActor("target")
  //     ]
  //
  // For APIv2, we extract these by asking VOOS directly to give us the JSON
  // rep of the resulting structure. We will respect both methods for the
  // time being.
  public IEnumerable<PropDef> EnumeratePropDefs()
  {
    return ParsePropsFromJsV1().Concat(GetExportedPropDefs());
  }

  List<PropDef> ParsePropsFromJsV1()
  {
    string[] lines = GetCode().Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
    List<PropDef> result = new List<PropDef>();
    foreach (string line in lines)
    {
      if (line.StartsWith("// property "))
      {
        try
        {
          string[] parts = line.Split(' ');
          string initValueString = parts.Length > 4 ? parts[4] : null;
          PropDef prop = new PropDef
          {
            type = parts[2],
            variableName = parts[3],
            defaultValueString = initValueString
          };
          result.Add(prop);
        }
        catch (System.Exception e)
        {
          Util.LogError($"Error while trying to parse property comment - will ignore it. The error: {e.ToString()}. The comment line: {line}");
        }
      }
    }
    return result;
  }

  // Only cuz JsonUtiliy can't parse [1,2,3] directly.
  [System.Serializable]
  struct PropDefsWrapper
  {
    public PropDef[] props;
  }

  PropDef[] GetExportedPropDefs()
  {
    string json = behaviorSystem.GetBehaviorPropertiesJson(behaviorUri);
    if (json.IsNullOrEmpty())
    {
      return new PropDef[0];
    }
    return JsonUtility.FromJson<PropDefsWrapper>(json).props;
  }

  public string GetDraftCode()
  {
    return GetBehavior().draftJavascript;
  }

  public void SetDraftCode(string s)
  {
    Debug.Assert(IsValid(), "Function called on invalid UnassignedBehavior");
    Behaviors.Behavior behavior = GetBehavior();

    if (behavior.draftJavascript == s)
    {
      Debug.Log("No actual change to draft behavior code. Doing nothing.");
      return;
    }

    behavior.draftJavascript = s;
    behaviorSystem.PutBehavior(BehaviorSystem.GetIdOfBehaviorUri(behaviorUri), behavior);
  }

  public int GetLineNumberForError(string errorMessage)
  {
    return VoosEngine.ExtractFirstLineNumberForModuleError(this.behaviorUri, errorMessage);
  }

  public string CleanRuntimeError(string errorMessage)
  {
    // We really only need the lines up to the behavior URI.
    string[] lines = errorMessage.NormalizeLineEndings().Split('\n');
    string rv = "";
    foreach (string line in lines)
    {
      if (line.Contains($"{this.behaviorUri}:"))
      {
        break;
      }
      rv += line + "\n";
    }
    return rv;
  }

  public UnassignedBehavior MakeCopy()
  {
    Behaviors.Behavior behavior = GetBehavior();
    string behaviorId = behaviorSystem.GenerateUniqueId();
    Debug.Log(behavior.label + ", " + behavior.metadataJson + ", " + behavior.javascript);
    behaviorSystem.PutBehavior(behaviorId, new Behaviors.Behavior
    {
      label = behavior.label,
      metadataJson = behavior.metadataJson,
      javascript = behavior.javascript
    });
    return new UnassignedBehavior(BehaviorSystem.IdToEmbeddedBehaviorUri(behaviorId), behaviorSystem);
  }

  public bool IsValid()
  {
    return behaviorSystem.IsBehaviorUriValid(behaviorUri);
  }
}

