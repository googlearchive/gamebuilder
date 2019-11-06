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
#if USE_STEAMWORKS
using LapinerTools.Steam.Data;
using LapinerTools.Steam;
#endif
using UnityEngine;
using System;

public class WorkshopInfo : MonoBehaviour
{
  [SerializeField] WorkshopInfoUI infoUI;

#if USE_STEAMWORKS

  WWW thumbnailDownloading = null;
  WorkshopItem workshopItem;

  public System.Action<Texture2D, string, string> SaveLocalCopy;

  void Awake()
  {

    infoUI.upVoteButton.onClick.AddListener(VoteUp);
    infoUI.downVoteButton.onClick.AddListener(VoteDown);
    infoUI.openOnSteamButton.onClick.AddListener(OpenOnWorkshop);
    infoUI.saveButton.onClick.AddListener(() => SaveLocalCopy?.Invoke(infoUI.image.sprite.texture, workshopItem.Name, workshopItem.Description));
    infoUI.closeButton.onClick.AddListener(Close);
  }

  public void Open(WorkshopItem newItem)
  {
    workshopItem = newItem;
    infoUI.titleField.text = newItem.Name;
    infoUI.descriptionField.text = newItem.Description;
    SetThumbnailUrl(newItem.PreviewImageURL);

    gameObject.SetActive(true);
  }

  void SaveToLocal()
  {
    Debug.Log("TO DO");
  }

  void VoteUp()
  {
    Debug.Log(workshopItem.Name);
    SteamWorkshopMain.Instance.Vote(workshopItem, true, (args) => { }/*empty callback*/);
  }

  void VoteDown()
  {
    SteamWorkshopMain.Instance.Vote(workshopItem, false, (args) => { }/*empty callback*/);
  }

  void OpenOnWorkshop()
  {
    Application.OpenURL($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopItem.SteamNative.m_nPublishedFileId.ToString()}");
  }

  void Update()
  {
    // voteUpOutline.enabled = workshopItem.IsVotedUp;
    // voteDownOutline.enabled = workshopItem.IsVotedDown;


    if (thumbnailDownloading != null)
    {
      if (thumbnailDownloading.isDone)
      {
        SetThumbnail(thumbnailDownloading.texture);
        thumbnailDownloading = null;
      }
    }
  }

  public void Close()
  {
    gameObject.SetActive(false);
    // voteUpOutline.enabled = false;
    // voteDownOutline.enabled = false;
  }

  public void SetThumbnailUrl(string url)
  {
    thumbnailDownloading = new WWW(url);
  }

  public Sprite GetThumbnail()
  {
    return infoUI.image.sprite;
  }

  public void SetThumbnail(Texture2D texture)
  {
    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
    if (newSprite != null)
    {
      infoUI.image.sprite = newSprite;
    }
    infoUI.image.color = Color.white;
    //thumbnailLoadingObject.SetActive(false);
  }
#endif

  internal bool IsOpen()
  {
    return gameObject.activeSelf;
  }
}
