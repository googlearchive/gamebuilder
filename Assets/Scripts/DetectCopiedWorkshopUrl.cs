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

using UnityEngine;

public class DetectCopiedWorkshopUrl : MonoBehaviour
{
#if USE_STEAMWORKS
  // Static so it sticks across scene loads..
  static string lastCopyBuffer = null;

  DynamicPopup popups;
  WorkshopAssetSource workshopSource;
  LoadingScreen loadingScreen;
  GameBuilderSceneController sceneController;

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref workshopSource);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref sceneController);
  }

  // Update is called once per frame
  void Update()
  {
    string url = GUIUtility.systemCopyBuffer;
    // Don't incur cost of checking, or bother the player, if buffer is unchanged.
    if (url == lastCopyBuffer) return;
    lastCopyBuffer = url;

    ulong workshopId = Util.ExtractIdFromWorkshopUrl(url);
    if (workshopId != 0)
    {
      popups.ShowTwoButtons($"You copied a Steam Workshop URL! Play it?\nDetected item ID: {workshopId}",
      $"Play", () =>
      {

        float progress = 0f;
        bool keepShowing = true;
        var downloadingPopup = new DynamicPopup.Popup();
        downloadingPopup.getMessage = () => $"Downloading.. {Mathf.Floor((progress * 100))} %";
        downloadingPopup.keepShowing = () => keepShowing;
        downloadingPopup.isCancellable = false;
        popups.Show(downloadingPopup);

        string displayName = $"Item {workshopId}";

        workshopSource.Get(workshopId, path =>
        {
          keepShowing = false;
          if (path.IsEmpty())
          {
            popups.ShowWithCancel($"Woops - could not download the workshop item. Maybe restart and try again?\nMessage: {path.GetErrorMessage()}", "OK", null, 800f);
          }
          else
          {
            loadingScreen.ShowAndDo(() =>
            sceneController.LoadWorkshopItem(
              new LoadableWorkshopItem
              {
                displayName = displayName,
                installedLocalFolder = path.Get(),
                steamId = workshopId
              },
              new GameBuilderApplication.PlayOptions(), null));
          }
        },
        prog => progress = prog,
        // TODO could also grab thumbnail async..
        item => displayName = item.Name
      );

      },
      "Cancel", () => { });
    }

  }
#endif
}
