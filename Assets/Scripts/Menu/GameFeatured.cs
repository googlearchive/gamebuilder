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

using System.IO;

public class GameFeatured : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI nameField;
  [SerializeField] TMPro.TextMeshProUGUI downloadText;
  [SerializeField] UnityEngine.UI.Image thumbnailImage;
  [SerializeField] UnityEngine.UI.Button button;
  [SerializeField] GameObject thumbnailLoadingObject;
  [SerializeField] GameObject subscribeObject;
  [SerializeField] GameObject playObject;
  [SerializeField] GameObject downloadTextObject;

#if USE_STEAMWORKS
  DynamicPopup popups;
  LoadingScreen loadingScreen;

  WWW thumbnailDownloading = null;

  // If this is not null, this is the local file that is featured.
  // (full path).
  string localVoosFile;

  // The featured game can be either a workshop item or a local file.
  // If workshopItem == null, it's a local file.
  WorkshopItem workshopItem;

  SplashScreenController splashScreenController;
  GameBuilderSceneController sceneController;

  public void Setup()
  {
    button.onClick.AddListener(OnButton);
    Util.FindIfNotSet(this, ref sceneController);
    Util.FindIfNotSet(this, ref splashScreenController);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref loadingScreen);
  }

  void OnButton()
  {
    if (localVoosFile != null || workshopItem.IsInstalled)
    {
      // Local file or already installed workshop item.
      popups.AskHowToPlay(LoadGame);
    }
    else if (!workshopItem.IsSubscribed)
    {
      // Not yet installed workshop item.
      SteamWorkshopMain.Instance.Subscribe(workshopItem, (args) => { }) /* empty callback */;
    }
  }

  void LoadGame(GameBuilderApplication.PlayOptions playOpts)
  {
    if (localVoosFile != null)
    {
      // Two action logs intentional.
    }
    else
    {
      ulong itemId = workshopItem.SteamNative.m_nPublishedFileId.m_PublishedFileId;
      // Two action logs intentional.
    }
    loadingScreen.ShowAndDo(() =>
    {
      if (localVoosFile != null)
      {
        var gameOpts = new GameBuilderApplication.GameOptions { playOptions = playOpts };
        loadingScreen.ShowAndDo(() => sceneController.RestartAndLoad(localVoosFile, gameOpts));
      }
      else
      {
        sceneController.LoadWorkshopItem(
          new LoadableWorkshopItem(workshopItem),
          playOpts,
          thumbnailImage.sprite.texture);
      }
    });
  }

  public void SetWorkshopItem(WorkshopItem item)
  {
    gameObject.SetActive(true);
    workshopItem = item;
    localVoosFile = null;
    SetThumbnailUrl(item.PreviewImageURL);
    nameField.text = $"<b>{item.Name}</b>";//\n{item.Description}";
  }

  public void SetLocalVoosFile(string localFileFullPath, string localThumbnailFullPath, string title, string description)
  {
    gameObject.SetActive(true);
    workshopItem = null;
    localVoosFile = localFileFullPath;
    // The file:// prefix is necessary on OSX.
    SetThumbnailUrl("file://" + localThumbnailFullPath);
    nameField.text = $"<b>{title}</b>\n{description}";
  }

  void SetThumbnailUrl(string url)
  {
    thumbnailDownloading = new WWW(url);
  }

  void Update()
  {
    if (thumbnailDownloading != null)
    {
      if (thumbnailDownloading.isDone)
      {
        SetThumbnail(thumbnailDownloading.texture);
        thumbnailDownloading = null;
      }
    }

    if (workshopItem != null)
    {
      WorkshopItemUpdate();
    }
  }

  void WorkshopItemUpdate()
  {
    if (workshopItem.IsSubscribed)
    {
      if (workshopItem.IsInstalled)
      {
        downloadTextObject.SetActive(false);
        playObject.SetActive(true);
        subscribeObject.SetActive(false);
      }
      else
      {
        playObject.SetActive(false);
        subscribeObject.SetActive(false);
        downloadTextObject.SetActive(true);
        if (workshopItem.IsDownloading)
        {
          downloadText.text = $"{Mathf.FloorToInt(100 * SteamWorkshopMain.Instance.GetDownloadProgress(workshopItem))}%";
        }
        else
        {
          downloadText.text = $"...";
        }
      }
    }
    else
    {
      downloadTextObject.SetActive(false);
      playObject.SetActive(false);
      subscribeObject.SetActive(true);
    }
  }

  void SetThumbnail(Texture2D texture)
  {
    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
    if (newSprite != null)
    {
      thumbnailImage.sprite = newSprite;
    }
    thumbnailImage.color = Color.white;
    //thumbnailLoadingObject.SetActive(false);
  }

#endif
}
