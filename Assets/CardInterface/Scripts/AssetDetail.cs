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

public abstract class AssetDetail : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Image thumbnailImage;
  [SerializeField] TMPro.TextMeshProUGUI descriptionField;
  [SerializeField] UnityEngine.UI.Button importButton;
  [SerializeField] TMPro.TextMeshProUGUI importButtonText;
  [SerializeField] UnityEngine.UI.Button unsubscribeButton;

#if USE_STEAMWORKS
  private WorkshopItem workshopItem;
  private bool workshopItemInProgress;

  public virtual void Awake()
  {
    importButton.onClick.AddListener(OnImportButtonClicked);
    unsubscribeButton.onClick.AddListener(OnUnsubscribeButtonClicked);
  }

  public void FitTo(RectTransform parentRect)
  {
    transform.parent = parentRect.transform;
    transform.localScale = Vector3.one;
    RectTransform rect = GetComponent<RectTransform>();
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
  }

  public void Open(Texture2D texture, WorkshopItem item)
  {
    gameObject.SetActive(true);
    workshopItem = item;
    workshopItemInProgress = false;
    descriptionField.text = $"<b>{item.Name}</b> - {GetWorkshopHeader(item)}\n{item.Description}";
    if (texture != null)
    {
      thumbnailImage.sprite = Sprite.Create(
        texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), 100F, 0, SpriteMeshType.FullRect);
    }
    unsubscribeButton.gameObject.SetActive(workshopItem.IsSubscribed);
  }

  string GetWorkshopHeader(WorkshopItem item)
  {
    if (item.VotesUp + item.VotesDown > 5)
    {
      return $"{Mathf.RoundToInt(100 * item.VotesUp / (item.VotesUp + item.VotesDown))}% rating - {item.Subscriptions} subs";
    }
    else
    {
      return $"{item.Subscriptions} subscribers";
    }
  }

  private void OnImportButtonClicked()
  {
    if (workshopItemInProgress) return;
    if (!workshopItem.IsSubscribed)
    {
      workshopItemInProgress = true;
      SteamWorkshopMain.Instance.Subscribe(workshopItem, OnSubscribed);
    }
    else
    {
      ImportAsset(workshopItem);
    }
  }

  void Update()
  {
    importButtonText.text = "Import";
    if (workshopItem.IsSubscribed)
    {
      if (workshopItem.IsDownloading)
      {
        importButtonText.text = $"Downloading...{Mathf.FloorToInt(100 * SteamWorkshopMain.Instance.GetDownloadProgress(workshopItem))}%";
      }
      else if (!workshopItem.IsInstalled)
      {
        importButtonText.text = $"Installing...";
      }
    }
    if (workshopItemInProgress && workshopItem.IsInstalled)
    {
      workshopItemInProgress = false;
      ImportAsset(workshopItem);
    }
  }

  private void OnSubscribed(WorkshopItemEventArgs args)
  {
    if (args.Item.SteamNative.m_nPublishedFileId == workshopItem.SteamNative.m_nPublishedFileId)
    {
      workshopItem = args.Item;
      unsubscribeButton.gameObject.SetActive(true);
    }
  }

  private void ImportAsset(WorkshopItem item)
  {
    if (item.InstalledLocalFolder.IsNullOrEmpty())
    {
      Debug.LogError("Item was not installed");
      return;
    }
    DoImport(item);
  }

  protected abstract void DoImport(WorkshopItem item);

  private void WorkshopItemUpdate()
  {
    if (workshopItemInProgress && workshopItem.IsInstalled)
    {
      workshopItemInProgress = false;
      ImportAsset(workshopItem);
    }
  }

  private void OnUnsubscribeButtonClicked()
  {
    SteamWorkshopMain.Instance.Unsubscribe(workshopItem, (args) =>
      {
        if (args.Item.SteamNative.m_nPublishedFileId == workshopItem.SteamNative.m_nPublishedFileId)
        {
          workshopItem = args.Item;
          unsubscribeButton.gameObject.SetActive(false);
        }
      });
  }
#endif
}