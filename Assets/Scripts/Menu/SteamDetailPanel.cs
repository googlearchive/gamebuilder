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

public class SteamDetailPanel : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Button voteUpButton;
  [SerializeField] UnityEngine.UI.Outline voteUpOutline;
  [SerializeField] UnityEngine.UI.Button voteDownButton;
  [SerializeField] UnityEngine.UI.Outline voteDownOutline;
  [SerializeField] UnityEngine.UI.Button openOnWorkshop;
  [SerializeField] TMPro.TextMeshProUGUI nameField;
  [SerializeField] UnityEngine.UI.Image thumbnailImage;
  [SerializeField] TMPro.TextMeshProUGUI descriptionField;
  [SerializeField] CustomMenuButton saveButton;

#if USE_STEAMWORKS

  WWW thumbnailDownloading = null;
  WorkshopItem workshopItem;

  public System.Action<Texture2D, string, string> SaveLocalCopy;

  void Awake()
  {

    voteUpButton.onClick.AddListener(VoteUp);
    voteDownButton.onClick.AddListener(VoteDown);
    openOnWorkshop.onClick.AddListener(OpenOnWorkshop);
    saveButton.ClickEvent = () => SaveLocalCopy?.Invoke(thumbnailImage.sprite.texture, workshopItem.Name, workshopItem.Description);
  }

  public void Open(WorkshopItem newItem)
  {
    workshopItem = newItem;
    nameField.text = newItem.Name;
    descriptionField.text = newItem.Description;
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
    voteUpOutline.enabled = workshopItem.IsVotedUp;
    voteDownOutline.enabled = workshopItem.IsVotedDown;


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
    voteUpOutline.enabled = false;
    voteDownOutline.enabled = false;
  }

  public void SetThumbnailUrl(string url)
  {
    thumbnailDownloading = new WWW(url);
  }

  public Sprite GetThumbnail()
  {
    return thumbnailImage.sprite;
  }

  public void SetThumbnail(Texture2D texture)
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
