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

public class ActorPrefabDetail : AssetDetail
{
#if USE_STEAMWORKS
  private DynamicPopup popups;
  private SceneActorLibrary sceneActorLibrary;

  public override void Awake()
  {
    base.Awake();
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref sceneActorLibrary);
  }

  protected override void DoImport(WorkshopItem item)
  {
    Dictionary<string, SavedActorPrefab> prefabs = SceneActorLibrary.ReadPrefabsFromDir(item.InstalledLocalFolder, item);
    bool containsOverrides = false;
    foreach (var entry in prefabs)
    {
      if (sceneActorLibrary.Exists(entry.Key))
      {
        containsOverrides = true;
      }
    }
    if (containsOverrides)
    {
      popups.ShowThreeButtons(
        "This actor already exists in your library.",
        "Overwrite", () =>
        {
          sceneActorLibrary.PutPrefabs(prefabs, true);
          popups.Show(
            $"{item.Name} was successfully imported. Check your custom actors!",
            "Ok"
          );
        },
        "Duplicate", () =>
        {
          sceneActorLibrary.PutPrefabs(prefabs);
          popups.Show(
            $"{item.Name} was successfully imported. Check your custom actors!",
            "Ok"
          );
        },
        "Cancel", () => { });
    }
    else
    {
      sceneActorLibrary.PutPrefabs(prefabs);
      popups.Show(
        $"{item.Name} was successfully imported. Check your custom actors!",
        "Ok"
      );
    }
  }
#endif

}