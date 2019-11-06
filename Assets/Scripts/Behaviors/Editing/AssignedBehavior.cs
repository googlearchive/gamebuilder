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
using Behaviors;

public class AssignedBehavior
{
  public readonly ActorBehaviorsEditor assignedBrain;

  public readonly string useId;

  public AssignedBehavior(string useId, ActorBehaviorsEditor assignedBrain)
  {
    this.useId = useId;
    this.assignedBrain = assignedBrain;
  }

  // Mostly so we can access some of the util functions
  public UnassignedBehavior GetUnassigned()
  {
    return new UnassignedBehavior(assignedBrain.GetUseBehaviorUri(this.useId), assignedBrain.GetBehaviorSystem());
  }

  public string GetName()
  {
    return GetDescription();
  }

  public string GetBehaviorMetadataJson()
  {
    return GetBehaviorSystem().GetBehaviorData(GetBehaviorUri()).metadataJson;
  }

  public string GetUseMetaJson()
  {
    try
    {
      return this.assignedBrain.GetUseMetaJson(useId);
    }
    catch (System.Exception e)
    {
      throw new System.Exception($"Failed to get use metajson for use Id {useId}", e);
    }
  }

  public string GetBehaviorUri()
  {
    return this.assignedBrain.GetUseBehaviorUri(this.useId);
  }

  public Behavior GetBehaviorData()
  {
    return GetBehaviorSystem().GetBehaviorData(GetBehaviorUri());
  }

  public void SetUseMetaJson(string json)
  {
    this.assignedBrain.SetUseMetaJson(useId, json);
  }

  public void RemoveSelfFromActor()
  {
    this.assignedBrain.RemoveBehavior(this);
  }

  public Util.Maybe<TReturn> CallScriptFunction<TArgs, TReturn>(string methodName, TArgs args)
  {
    return assignedBrain.GetBehaviorSystem().CallBehaviorUseMethod<TArgs, TReturn>(
      useId, assignedBrain.GetActorName(),
      methodName, args);
  }

  public BehaviorSystem GetBehaviorSystem()
  {
    return assignedBrain.GetBehaviorSystem();
  }

  public string GetDescription()
  {
    string callResult = CallScriptFunction<int, string>("getDescription", 0).GetOr("");
    if (callResult.IsNullOrEmpty())
    {
      throw new System.NotImplementedException();
      // return GetUnassigned().GetIn().GetInlineCommentLabel();
    }
    else
    {
      return callResult;
    }
  }

  BehaviorUse GetUse()
  {
    return assignedBrain.GetBrain().GetUse(this.useId);
  }

  public PropEditor[] GetProperties()
  {
    var assigns = GetUse().propertyAssignments.DeepClone();
    var assignmentByName = new Dictionary<string, PropertyAssignment?>();
    foreach (var assign in assigns)
    {
      assignmentByName[assign.propertyName] = assign;
    }

    return GetUnassigned().EnumeratePropDefs().Select(def =>
      new PropEditor(def,
        assignmentByName.GetOr(def.variableName, null),
        this)).ToArray();
  }

  public PropEditor GetPropEditorByName(string propertyName)
  {
    foreach (PropEditor editor in GetProperties())
    {
      if (editor.variableName == propertyName) return editor;
    }
    return null;
  }

  public void SetProperty(string propertyName, PropEditor value)
  {
    using (this.assignedBrain.StartUndo($"Set {propertyName}"))
    {
      var assigns = GetUse().propertyAssignments.DeepClone();
      int i = assigns.IndexOfWhere(pa => pa.propertyName == propertyName);
      if (i == -1)
      {
        // Add new prop
        i = assigns.Length;
        var assignList = assigns.ToList();
        assignList.Add(new PropertyAssignment());
        assigns = assignList.ToArray();
      }
      assigns[i] = PropUtil.ToAssignment(value);
      Debug.Assert(assigns[i].propertyName == propertyName);
      assignedBrain.SetProperties(this, assigns);
    }
  }

  public void SetProperties(PropEditor[] props)
  {
    var assigns = PropUtil.SerializeProps(props);
    assignedBrain.SetProperties(this, assigns);
  }

  public bool IsValid()
  {
    return this.assignedBrain.IsValid() && this.assignedBrain.GetBrain().HasUse(this.useId)
    && GetBehaviorSystem().IsBehaviorUriValid(GetBehaviorUri());
  }
}
