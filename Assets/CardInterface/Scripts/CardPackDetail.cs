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

#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif

public class CardPackDetail : AssetDetail
{
#if USE_STEAMWORKS
  private BehaviorSystem behaviorSystem;
  private DynamicPopup popups;

  public override void Awake()
  {
    base.Awake();
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref popups);
  }

  protected override void DoImport(WorkshopItem item)
  {
    Debug.Log("DO IMPORT?");
    OnBehaviorsLoaded(item, BehaviorSystem.LoadEmbeddedBehaviorsFromDirectory(item.InstalledLocalFolder));
  }

  private void OnBehaviorsLoaded(WorkshopItem item, Dictionary<string, Behaviors.Behavior> behaviors)
  {
    bool containsOverrides = false;
    foreach (var entry in behaviors)
    {
      if (behaviorSystem.EmbeddedBehaviorExists(entry.Key))
      {
        containsOverrides = true;
      }
    }
    if (containsOverrides)
    {
      popups.ShowThreeButtons(
        "Some cards in this pack already exist in your library.",
        "Overwrite", () =>
        {
          behaviorSystem.PutBehaviors(behaviors, true);
          popups.Show(
            $"{item.Name} was successfully imported. Check your card library!",
            "Ok"
          );
        },
        "Duplicate", () =>
        {
          behaviorSystem.PutBehaviors(behaviors);
          popups.Show(
            $"{item.Name} was successfully imported. Check your card library!",
            "Ok"
          );
        },
        "Cancel", () => { });
    }
    else
    {
      behaviorSystem.PutBehaviors(behaviors);
      popups.Show(
        $"{item.Name} was successfully imported. Check your card library!",
        "Ok"
      );
    }
  }
#endif
}