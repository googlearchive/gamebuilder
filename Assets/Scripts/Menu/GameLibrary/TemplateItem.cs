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
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Events;

public class TemplateItem : MonoBehaviour
{
  [SerializeField] TMPro.TMP_Text title;
  [SerializeField] Image thumbnail;

  UnityWebRequest thumbnailWebRequest;

  public void SetTitle(string title)
  {
    this.title.text = title;
  }

  public void SetThumbnailPath(string path)
  {
    // MacOSX requires file://
    if (!path.StartsWith("file://"))
    {
      path = "file://" + path;
    }
    thumbnailWebRequest = UnityWebRequestTexture.GetTexture(path);
    thumbnailWebRequest.SendWebRequest();
  }

  public void AddClickListener(UnityAction listener)
  {
    GetComponent<Button>().onClick.AddListener(listener);
  }

  void Update()
  {
    if (thumbnailWebRequest != null && thumbnailWebRequest.isDone)
    {
      Texture2D texture = ((DownloadHandlerTexture)thumbnailWebRequest.downloadHandler).texture;
      thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
  }
}
