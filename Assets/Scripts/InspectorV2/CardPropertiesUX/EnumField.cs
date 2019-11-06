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
using UnityEngine;
using System.Linq;

namespace BehaviorUX
{
  public class EnumField : TypeField<string>
  {
    [SerializeField] TMPro.TMP_Dropdown dropdown;
    [SerializeField] GameObject errorIndicator;

    bool expandedLastFrame = false;

    private BehaviorProperties.EnumAllowedValue[] allowedValues;

    void Awake()
    {
      dropdown.onValueChanged.AddListener(UpdateValue);
    }

    public void SetAllowedValues(BehaviorProperties.EnumAllowedValue[] allowedValues)
    {
      this.allowedValues = allowedValues;
      dropdown.ClearOptions();
      dropdown.AddOptions((from v in allowedValues select v.label).ToList());
    }

    public override void SetValue(string value)
    {
      int index = LookUpValue(value);
      dropdown.onValueChanged.RemoveListener(UpdateValue);
      dropdown.value = index;
      dropdown.onValueChanged.AddListener(UpdateValue);
      errorIndicator.SetActive(index < 0);
      if (index < 0)
      {
        errorIndicator.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = value;
      }
    }

    private int LookUpValue(string value)
    {
      for (int i = 0; i < allowedValues.Length; i++)
      {
        if (allowedValues[i].value == value)
        {
          return i;
        }
      }
      return -1;
    }

    void UpdateCanvasShaderChannels()
    {
      Canvas canvas = dropdown.GetComponentInChildren<Canvas>();
      if (canvas == null) return;
      canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
      canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
      canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord3;
    }

    void UpdateValue(int index)
    {
      onValueChanged?.Invoke(allowedValues[index].value);
      errorIndicator.SetActive(false);
    }

    void Update()
    {
      if (expandedLastFrame != dropdown.IsExpanded)
      {
        if (dropdown.IsExpanded) UpdateCanvasShaderChannels();
        expandedLastFrame = dropdown.IsExpanded;
      }
    }

  }

}
