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

namespace BehaviorUX
{
  public class ParticleField : PropertyField
  {
    [SerializeField] TMPro.TextMeshProUGUI label;
    [SerializeField] TMPro.TMP_Dropdown dropdown;

    ParticleEffectSystem pfxSystem;
    private List<string> pfxIdValues = new List<string>();

    public delegate void OnValueChanged(string value);
    public event OnValueChanged onValueChanged;

    protected override void Initialize()
    {
      label.text = editor.labelForDisplay;
      Util.FindIfNotSet(this, ref pfxSystem);
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
      pfxSystem.onParticleEffectChanged += (p) => RefreshValues();
      pfxSystem.onParticleEffectRemoved += (p) => RefreshValues();
      RefreshValues();
    }

    private void OnDropdownValueChanged(int i)
    {
      string newId = pfxIdValues[i];
      editor.SetData(newId);
      onValueChanged?.Invoke(newId);
    }

    private void RefreshValues()
    {
      dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
      dropdown.ClearOptions();
      pfxIdValues.Clear();

      List<TMPro.TMP_Dropdown.OptionData> data = new List<TMPro.TMP_Dropdown.OptionData>();

      data.Add(new TMPro.TMP_Dropdown.OptionData("(none)"));
      pfxIdValues.Add(null);

      foreach (ParticleEffectListing listing in pfxSystem.ListAll())
      {
        data.Add(new TMPro.TMP_Dropdown.OptionData(listing.name));
        pfxIdValues.Add(listing.id);
      }

      dropdown.options = data;
      string pfxId = (string)editor.data;
      int index = pfxIdValues.IndexOf(pfxId);
      if (index < 0)
      {
        dropdown.value = 0;
      }
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void Update()
    {
      string pfxId = (string)editor.data;
      if (!string.IsNullOrEmpty(pfxId) && pfxSystem.GetParticleEffect(pfxId) == null)
      {
        pfxId = null;
      }
      dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
      dropdown.value = pfxIdValues.IndexOf(pfxId);
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
  }
}