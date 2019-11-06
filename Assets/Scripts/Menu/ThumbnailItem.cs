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

public class ThumbnailItem : MonoBehaviour
{

  [SerializeField] TMPro.TextMeshProUGUI titleField;
  [SerializeField] UnityEngine.UI.RawImage thumbnailImage;
  [SerializeField] GameObject thumbnailLoadingObject;
  [SerializeField] UnityEngine.UI.Button thumbnailButton;
  [SerializeField] UnityEngine.UI.Image outline;

  WWW thumbnailDownloading = null;

  public System.Action OnClick;
  public System.Func<System.DateTime> GetWriteTime;
  public System.Func<string> GetDescription;
  public System.Func<int> GetPlayerCount;
  private static CoroutineQueue setThumbnailQueue;
  private bool isQueued = false;

  // If > 0, this thumbnail should appear at the front of the list.
  // Amongst thumbnails with prio > 0, the higher the number, the more in front it appears.
  private int sortPriorityHint;

  void Awake()
  {
    if (thumbnailButton != null)
    {
      thumbnailButton.onClick.AddListener(() => OnClick?.Invoke());
    }
    thumbnailImage.color = Color.clear;
    if (thumbnailLoadingObject != null)
    {
      thumbnailLoadingObject.SetActive(true);
    }
  }

  public void SetName(string name)
  {
    titleField.text = name;
  }

  public void SetSortPriorityHint(int sortPriorityHint)
  {
    this.sortPriorityHint = sortPriorityHint;
  }

  public int GetSortPriorityHint()
  {
    return this.sortPriorityHint;
  }

  public string GetName()
  {
    return titleField.text;
  }

  public void SetVisibility(bool on)
  {
    gameObject.SetActive(on);
  }

  public bool GetVisibility()
  {
    return gameObject.activeSelf;
  }

  public void Destruct()
  {
    Destroy(gameObject);
  }

  public void SetThumbnail(Texture2D texture)
  {
    if (texture == null || thumbnailImage == null)
    {
      return;
    }
    thumbnailImage.texture = texture;
    thumbnailImage.color = Color.white;
    if (thumbnailLoadingObject)
    {
      thumbnailLoadingObject.SetActive(false);
    }
  }

  public void SetDownloadedThumbnail()
  {
    if (thumbnailDownloading != null)
    {
      SetThumbnail(thumbnailDownloading.texture);
      thumbnailDownloading = null;
    }
  }

  public Texture2D GetTexture()
  {
    if (thumbnailImage.texture == null)
    {
      return null;
    }
    return (Texture2D)thumbnailImage.texture;
  }

  public void SetThumbnailUrl(string url)
  {
    thumbnailDownloading = new WWW(url);
  }

  public void ToggleSelect(bool on)
  {
    outline.enabled = on;
  }

  protected virtual void Update()
  {
    if (thumbnailDownloading != null)
    {
      if (thumbnailDownloading.isDone && !isQueued)
      {
        if (setThumbnailQueue == null)
        {
          GameObject queueObject = new GameObject();
          setThumbnailQueue = queueObject.AddComponent<CoroutineQueue>();
        }
        setThumbnailQueue.Enqueue(() =>
        {
          this.SetDownloadedThumbnail();
          isQueued = false;
        });
        isQueued = true;
      }
    }
  }

}