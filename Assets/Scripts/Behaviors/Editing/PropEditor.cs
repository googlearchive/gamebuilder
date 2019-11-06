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

using BehaviorProperties;
using UnityEngine;

// TODO rename to PropInstance
public class PropEditor
{
  public readonly PropDef propDef;

  // The actual value object, concrete type dependent on propType.
  // Ideally this class would be a template, but let's just downcast for now.
  // TODO make this private
  object __data;
  // TODO MUTABLE STATE HERE!! Could get out of sync. Remove later - ie. just
  // retrieve it from the database every time we need it.

  private readonly AssignedBehavior assigned;

  public string labelForDisplay
  {
    get
    {
      return this.label.IsNullOrEmpty() ? this.propDef.variableName : this.label;
    }
  }

  public PropEditor(PropDef propOptions, Behaviors.PropertyAssignment? assignment, AssignedBehavior assigned)
  {
    this.propDef = propOptions;
    if (assignment != null)
    {
      this.__data = PropUtil.GetPropertyValue(assignment.Value, this.propType);
    }
    else
    {
      this.__data = PropUtil.ParsePropertyInitialValueOrDefault(this.propType, propOptions.defaultValueString);
    }
    this.assigned = assigned;
  }

  public string variableName { get { return propDef.variableName; } }
  public object data { get { return __data; } }
  public PropType propType { get { return Util.ParseEnum<PropType>(propDef.type); } }
  public string comment { get { return propDef.comment; } }
  public string label { get { return propDef.label; } }
  public bool allowOffstageActors { get { return this.propDef.allowOffstageActors; } }
  public string pickerPrompt { get { return this.propDef.pickerPrompt; } }
  public string cardCategory { get { return this.propDef.deckOptions.cardCategory; } }
  public DeckOptions deckOptions { get { return this.propDef.deckOptions; } }
  public PropDefRequirement[] requires { get { return this.propDef.requires; } }
  public EnumAllowedValue[] allowedValues { get { return this.propDef.allowedValues; } }

  public void AssertValid()
  {
    if (this.data == null)
    {
      return;
    }
    if (this.data.GetType() != PropUtil.GetExpectedType(this.propType))
    {
      throw new System.Exception($"For prop {this.labelForDisplay}, the type of the data ({this.data.GetType()}) did not match the expected ({PropUtil.GetDefaultValue(this.propType).GetType()})");
    }
  }

  // Ie. represents the same property, but maybe has a different value.
  public bool IsSameProperty(PropEditor other)
  {
    return this.propDef.variableName == other.propDef.variableName && this.propType == other.propType;
  }

  public void SetData(object newData)
  {
    Debug.Assert(newData == null || newData.GetType() == PropUtil.GetExpectedType(this.propType));
    if (System.Object.Equals(__data, newData)) // Object.equals supports nulls
    {
      return;
    }
    __data = newData;
    if (this.assigned == null)
    {
      // Util.LogWarning("This PropEditor is not meant to be changed! It did not come from an AssignedBehavior, but probably an UnassignedBehavior!");
    }
    else
    {
      this.assigned.SetProperty(this.propDef.variableName, this);
    }
  }
}