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
  public class ActorGroupField : PropertyField
  {
    [SerializeField] TMPro.TextMeshProUGUI label;
    [SerializeField] TMPro.TextMeshProUGUI buttonLabel;
    [SerializeField] UnityEngine.UI.Button button;

    VoosEngine engine;

    public delegate void OnValueChanged(string value);
    public event OnValueChanged onValueChanged;

    ActorGroupPickerDialog currentlyOpenGroupPicker;

    protected override void Initialize()
    {
      button.onClick.AddListener(OnButtonClick);
      label.text = editor.labelForDisplay;
      Util.FindIfNotSet(this, ref engine);
    }

    private void Update()
    {
      ActorGroupSpec groupSpec = ActorGroupSpec.FromString((string)editor.data);
      buttonLabel.text = groupSpec.ToUserFriendlyString(engine);
    }

    void OnButtonClick()
    {
      currentlyOpenGroupPicker = ActorGroupPickerDialog.Launch(
        null, editor.pickerPrompt, editor.allowOffstageActors, (success, spec) =>
      {
        currentlyOpenGroupPicker = null;
        if (success)
        {
          editor.SetData(spec.ToString());
          onValueChanged?.Invoke(spec.ToString());
        }
      });
    }

    public override bool OnEscape()
    {
      if (currentlyOpenGroupPicker != null)
      {
        return currentlyOpenGroupPicker.OnEscape();
      }
      return false;
    }
  }
}