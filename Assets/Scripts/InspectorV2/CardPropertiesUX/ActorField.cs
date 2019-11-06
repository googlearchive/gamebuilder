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
  public class ActorField : TypeField<string>
  {
    [SerializeField] TMPro.TextMeshProUGUI buttonLabel;
    [SerializeField] UnityEngine.UI.Button button;

    private VoosActor actor;
    private VoosEngine engine;
    private string pickerPrompt;
    private bool allowOffstageActors;
    private ActorPickerDialog currentlyOpenDialog;

    void Awake()
    {
      button.onClick.AddListener(OnButtonClick);
      Util.FindIfNotSet(this, ref engine);
    }

    public void Setup(string pickerPrompt, bool allowOffstageActors)
    {
      this.pickerPrompt = pickerPrompt;
      this.allowOffstageActors = allowOffstageActors;
    }

    public override void SetValue(string actorName)
    {
      actor = string.IsNullOrEmpty(actorName) ? null : engine.GetActor(actorName);
      buttonLabel.text = actor != null ? actor.GetDisplayName() : "[Click to set]";
    }

    void OnButtonClick()
    {
      currentlyOpenDialog = ActorPickerDialog.Launch(null, pickerPrompt, allowOffstageActors, (success, actorName) =>
      {
        currentlyOpenDialog = null;
        if (success)
        {
          onValueChanged?.Invoke(actorName);
        }
      });
    }

    public bool OnEscape()
    {
      if (currentlyOpenDialog != null)
      {
        currentlyOpenDialog.Close();
        return true;
      }
      return false;
    }
  }
}