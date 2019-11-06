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
using UnityEngine.UI;

public class ActorDropdown : TMPro.TMP_Dropdown
{
  private VoosEngine voosEngine;
  private VoosActor actor;
  private List<VoosActor> dropdownActors;

  public delegate void OnActorChanged(VoosActor newActor);
  private OnActorChanged onActorChanged;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    onValueChanged.AddListener((i) =>
    {
      onActorChanged(dropdownActors[i]);
    });
  }

  public void AddListener(OnActorChanged listener)
  {
    onActorChanged += listener;
  }

  public void SetActor(VoosActor actor)
  {
    this.actor = actor;
    RefreshDropdown();
  }

  private void RefreshDropdown()
  {
    dropdownActors = new List<VoosActor>(voosEngine.EnumerateActors());

    ClearOptions();
    // TODO wasteful to allocate a new array each time...    
    List<string> actorNames = new List<string>(dropdownActors.Count);
    foreach (VoosActor dropdownActor in dropdownActors)
    {
      actorNames.Add(dropdownActor.GetDisplayName());
    }
    AddOptions(actorNames);

    for (int i = 0; i < dropdownActors.Count; i++)
    {
      if (dropdownActors[i] == actor)
      {
        this.value = i;
        return;
      }
    }
  }

  protected override GameObject CreateDropdownList(GameObject template)
  {
    RefreshDropdown();
    return base.CreateDropdownList(template);
  }

}
