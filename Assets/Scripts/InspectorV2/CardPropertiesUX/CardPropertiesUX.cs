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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using BehaviorProperties;

namespace BehaviorUX
{
  // NOTE: Also used for panel properties.
  public class CardPropertiesUX : MonoBehaviour
  {
    public RectTransform fieldParent;
    [SerializeField] NumberPropertyField numberPropertyFieldPrefab;
    [SerializeField] NumberArrayPropertyField numberArrayPropertyFieldPrefab;
    [SerializeField] DecimalField decimalFieldPrefab;
    [SerializeField] BooleanField booleanFieldPrefab;
    [SerializeField] StringPropertyField stringPropertyFieldPrefab;
    [SerializeField] StringArrayPropertyField stringArrayPropertyFieldPrefab;
    [SerializeField] ActorPropertyField actorPropertyFieldPrefab;
    [SerializeField] ActorArrayPropertyField actorArrayPropertyFieldPrefab;
    [SerializeField] SoundField soundFieldPrefab;
    [SerializeField] ParticleField particleFieldPrefab;
    [SerializeField] ActorGroupField actorGroupFieldPrefab;
    [SerializeField] ImageField imageFieldPrefab;
    [SerializeField] ColorProp colorFieldPrefab;
    [SerializeField] EnumPropertyField enumPropertyFieldPrefab;
    [SerializeField] EnumArrayPropertyField enumArrayPropertyFieldPrefab;
    [SerializeField] GameObject previewSpacer;
    [SerializeField] UnityEngine.UI.Image previewOverlay;

    List<PropertyField> fieldList = new List<PropertyField>();
    public delegate void OnValueChanged(PropType type);
    public event OnValueChanged onValueChanged;

    PropType[] implementedTypes = new PropType[] {
      PropType.Number,
      PropType.NumberArray,
      PropType.Decimal,
      PropType.Boolean,
      PropType.String,
      PropType.StringArray,
      PropType.Actor,
      PropType.ActorArray,
      PropType.Sound,
      PropType.ParticleEffect,
      PropType.ActorGroup,
      PropType.Image,
      PropType.Color,
      PropType.Enum,
      PropType.EnumArray,
    };

    bool IsRelevantField(PropEditor fieldData)
    {
      return implementedTypes.Contains(fieldData.propType);
    }

    public void Setup(IEnumerable<PropEditor> properties)
    {
      TryClearFieldList();

      if (properties == null)
      {
        return;
      }

      foreach (PropEditor prop in properties)
      {
        if (IsRelevantField(prop))
        {
          SpawnField(prop);
        }
      }
      SetPreviewSpacerAndOverlay(false);
    }

    void SetPreviewSpacerAndOverlay(bool value)
    {
      if (previewSpacer == null) return;
      previewSpacer.gameObject.SetActive(value);
      previewOverlay.gameObject.SetActive(value);
    }

    public void SetupPreview(UnassignedBehavior editor)
    {
      TryClearFieldList();

      if (editor == null)
      {
        return;
      }

      var defaultProps = editor.GetDefaultProperties();
      foreach (PropEditor propEditor in defaultProps)
      {
        if (IsRelevantField(propEditor))
        {
          SpawnField(propEditor);
        }
      }

      SetPreviewSpacerAndOverlay(HasAnyProps());
    }

    private void TryClearFieldList()
    {
      if (fieldList.Count == 0) return;

      for (int i = 0; i < fieldList.Count; i++)
      {
        fieldList[i].RequestDestruct();
      }

      fieldList.Clear();
    }

    void SpawnField(PropEditor propEditor)
    {
      PropertyField newField = null;

      switch (propEditor.propType)
      {
        case PropType.Number:
          NumberPropertyField numberPropertyField = Instantiate(numberPropertyFieldPrefab, fieldParent);
          numberPropertyField.onValueChanged += (n) => HandleValueChange(PropType.Number);
          newField = numberPropertyField;
          break;
        case PropType.Decimal:
          DecimalField decimalField = Instantiate(decimalFieldPrefab, fieldParent);
          decimalField.onValueChanged += (n) => HandleValueChange(PropType.Decimal);
          newField = decimalField;
          break;
        case PropType.Boolean:
          BooleanField booleanField = Instantiate(booleanFieldPrefab, fieldParent);
          booleanField.onValueChanged += (b) => HandleValueChange(PropType.Boolean);
          newField = booleanField;
          break;
        case PropType.String:
          StringPropertyField stringPropertyField = Instantiate(stringPropertyFieldPrefab, fieldParent);
          stringPropertyField.onValueChanged += (s) => HandleValueChange(PropType.String);
          newField = stringPropertyField;
          break;
        case PropType.Actor:
          ActorPropertyField actorPropertyField = Instantiate(actorPropertyFieldPrefab, fieldParent);
          actorPropertyField.onValueChanged += (a) => HandleValueChange(PropType.Actor);
          newField = actorPropertyField;
          break;
        case PropType.Sound:
          SoundField soundField = Instantiate(soundFieldPrefab, fieldParent);
          soundField.onValueChanged += (a) => HandleValueChange(PropType.Sound);
          newField = soundField;
          break;
        case PropType.ParticleEffect:
          ParticleField particleField = Instantiate(particleFieldPrefab, fieldParent);
          particleField.onValueChanged += (a) => HandleValueChange(PropType.ParticleEffect);
          newField = particleField;
          break;
        case PropType.ActorGroup:
          ActorGroupField actorGroupField = Instantiate(actorGroupFieldPrefab, fieldParent);
          actorGroupField.onValueChanged += (a) => HandleValueChange(PropType.ActorGroup);
          newField = actorGroupField;
          break;
        case PropType.Image:
          ImageField imageField = Instantiate(imageFieldPrefab, fieldParent);
          imageField.onValueChanged += (a) => HandleValueChange(PropType.Image);
          newField = imageField;
          break;
        case PropType.Color:
          ColorProp colorField = Instantiate(colorFieldPrefab, fieldParent);
          colorField.onValueChanged += (a) => HandleValueChange(PropType.Color);
          newField = colorField;
          break;
        case PropType.Enum:
          EnumPropertyField enumPropertyField = Instantiate(enumPropertyFieldPrefab, fieldParent);
          enumPropertyField.onValueChanged += (a) => HandleValueChange(PropType.Enum);
          newField = enumPropertyField;
          break;
        case PropType.NumberArray:
          NumberArrayPropertyField numberArrayPropertyField = Instantiate(numberArrayPropertyFieldPrefab, fieldParent);
          numberArrayPropertyField.onValueChanged += (n) => HandleValueChange(PropType.NumberArray);
          newField = numberArrayPropertyField;
          break;
        case PropType.StringArray:
          StringArrayPropertyField stringArrayPropertyField = Instantiate(stringArrayPropertyFieldPrefab, fieldParent);
          stringArrayPropertyField.onValueChanged += (s) => HandleValueChange(PropType.StringArray);
          newField = stringArrayPropertyField;
          break;
        case PropType.EnumArray:
          EnumArrayPropertyField enumArrayPropertyField = Instantiate(enumArrayPropertyFieldPrefab, fieldParent);
          enumArrayPropertyField.onValueChanged += (s) => HandleValueChange(PropType.EnumArray);
          newField = enumArrayPropertyField;
          break;
        case PropType.ActorArray:
          ActorArrayPropertyField actorArrayPropertyField = Instantiate(actorArrayPropertyFieldPrefab, fieldParent);
          actorArrayPropertyField.onValueChanged += (s) => HandleValueChange(PropType.ActorArray);
          newField = actorArrayPropertyField;
          break;
      }

      // if(newField == null)
      fieldList.Add(newField);
      newField?.Setup(propEditor);
      if (previewOverlay != null)
      {
        previewOverlay.transform.SetAsLastSibling();
      }
      UpdateFieldVisibility();
    }

    internal bool OnEscape()
    {
      foreach (PropertyField field in fieldList)
      {
        if (field.OnEscape())
        {
          return true;
        }
      }

      return false;
    }

    private void HandleValueChange(PropType propType)
    {
      onValueChanged?.Invoke(propType);
      // TODO: hide/unhide fields based on field requirements.

      UpdateFieldVisibility();
    }

    public bool HasAnyProps()
    {
      return fieldList != null && fieldList.Count > 0;
    }

    private void UpdateFieldVisibility()
    {
      foreach (PropertyField field in fieldList)
      {
        field.gameObject.SetActive(ShouldShowField(field.GetEditor()));
      }
    }

    // HACK: I don't like making this public. This is used from CardPanel.
    // The right way to do this would be for this logic to be implemented at a lower level,
    // not at the UI level.    
    public bool ShouldShowField(PropEditor editor)
    {
      PropDefRequirement[] requires = editor.requires;
      return AreRequirementsFulfilled(requires);
    }

    private bool AreRequirementsFulfilled(PropDefRequirement[] requires)
    {
      if (requires == null || requires.Length == 0)
      {
        return true;
      }
      foreach (PropDefRequirement require in requires)
      {
        if (!IsRequirementFulfilled(require))
        {
          return false;
        }
      }
      return true;
    }

    private bool IsRequirementFulfilled(PropDefRequirement requirement)
    {
      object valueObj;
      if (!TryGetFieldValue(requirement.key, out valueObj))
      {
        Debug.LogWarning("Field mentioned in property requirement does not exist: " + requirement.key);
        return false;
      }
      string valueString = ObjToString(valueObj);
      string requiredValueString = ObjToString(requirement.value);

      switch (requirement.op)
      {
        case "=":
          return valueString == requiredValueString;
        case "!=":
          return valueString != requiredValueString;
        default:
          {
            Debug.LogError("Property requirement has invalid operator: " + requirement.op);
            return false;
          }
      }
    }

    private bool TryGetFieldValue(string fieldName, out object value)
    {
      // TODO: make this a hashtable, if we're picky (but it probably doesn't matter; the # of fields is really small).
      foreach (PropertyField field in fieldList)
      {
        if (field != null && field.GetEditor().variableName == fieldName)
        {
          value = field.GetEditor().data;
          return true;
        }
      }
      value = null;
      return false;
    }

    private string ObjToString(object valueObj)
    {
      if (valueObj == null)
      {
        return null;
      }
      if (valueObj is bool)
      {
        // We have to do this explicitly because implicit C# conversion uses
        // "True" / "False" (capitalized) and JS uses lowercase.
        return ((bool)valueObj) ? "true" : "false";
      }
      return valueObj.ToString();
    }
  }
}
