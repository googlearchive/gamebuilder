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
  public class SoundField : PropertyField
  {
    [SerializeField] TMPro.TextMeshProUGUI label;
    [SerializeField] TMPro.TMP_Dropdown dropdown;

    SoundEffectSystem sfxSystem;
    private List<string> sfxIdValues = new List<string>();

    public delegate void OnValueChanged(string value);
    public event OnValueChanged onValueChanged;

    protected override void Initialize()
    {
      label.text = editor.labelForDisplay;
      Util.FindIfNotSet(this, ref sfxSystem);
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
      sfxSystem.onSoundEffectChanged += (p) => RefreshValues();
      sfxSystem.onSoundEffectRemoved += (p) => RefreshValues();
      RefreshValues();
    }

    private void OnDropdownValueChanged(int i)
    {
      string newId = sfxIdValues[i];
      editor.SetData(newId);
      onValueChanged?.Invoke(newId);
    }

    private void RefreshValues()
    {
      dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
      dropdown.ClearOptions();
      sfxIdValues.Clear();

      List<TMPro.TMP_Dropdown.OptionData> data = new List<TMPro.TMP_Dropdown.OptionData>();

      data.Add(new TMPro.TMP_Dropdown.OptionData("(none)"));
      sfxIdValues.Add(null);

      foreach (SoundEffectListing listing in sfxSystem.ListAll())
      {
        data.Add(new TMPro.TMP_Dropdown.OptionData(listing.name));
        sfxIdValues.Add(listing.id);
      }

      dropdown.options = data;
      string pfxId = (string)editor.data;
      int index = sfxIdValues.IndexOf(pfxId);
      if (index < 0)
      {
        dropdown.value = 0;
      }
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void Update()
    {
      string sfxId = (string)editor.data;
      if (!string.IsNullOrEmpty(sfxId) && sfxSystem.GetSoundEffect(sfxId) == null)
      {
        sfxId = null;
      }
      dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
      dropdown.value = sfxIdValues.IndexOf(sfxId);
      dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }
  }
}