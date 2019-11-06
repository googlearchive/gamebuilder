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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if USE_STEAMWORKS
using Steamworks;
#endif

public abstract class WorkshopUploadDialog : MonoBehaviour
{
  [SerializeField] protected TMPro.TMP_InputField nameField;
  [SerializeField] protected TMPro.TMP_InputField descField;
  [SerializeField] protected UnityEngine.UI.RawImage thumbnailImage;
  [SerializeField] protected UnityEngine.UI.Button closeButton;
  [SerializeField] protected UnityEngine.UI.Button uploadButton;
  [SerializeField] protected TMPro.TextMeshProUGUI nameRequiredText;
  [SerializeField] protected TMPro.TextMeshProUGUI descRequiredText;
  [SerializeField] protected RectTransform progressBar;
  [SerializeField] protected TMPro.TextMeshProUGUI finishedMessage;
  [SerializeField] protected UnityEngine.UI.Button visitButton;

#if USE_STEAMWORKS

  protected Util.Maybe<ulong> workshopId;
  protected WorkshopAssetSource workshopAssetSource;
  private WorkshopAssetSource.GetUploadProgress uploadProgressGetter;

  public virtual void Setup()
  {
    uploadButton.onClick.AddListener(StartUpload);
    Util.FindIfNotSet(this, ref workshopAssetSource);
    closeButton.onClick.AddListener(() => gameObject.SetActive(false));
  }

  protected void Open(Util.Maybe<ulong> workshopId)
  {
    this.workshopId = workshopId;
    nameField.text = "";
    descField.text = "";
    nameRequiredText.gameObject.SetActive(false);
    descRequiredText.gameObject.SetActive(false);
    finishedMessage.gameObject.SetActive(false);
    visitButton.gameObject.SetActive(false);
    uploadButton.gameObject.SetActive(true);
    gameObject.SetActive(true);
  }

  void StartUpload()
  {
    nameRequiredText.gameObject.SetActive(false);
    descRequiredText.gameObject.SetActive(false);
    string name = nameField.text;
    if (name.IsNullOrEmpty())
    {
      nameRequiredText.gameObject.SetActive(true);
      return;
    }
    string desc = descField.text;
    if (desc.IsNullOrEmpty())
    {
      descRequiredText.gameObject.SetActive(true);
      return;
    }
    uploadButton.gameObject.SetActive(false);
    DoUpload(result => OnUploadComplete(name, desc, result), onStatus => { uploadProgressGetter = onStatus; });
    finishedMessage.text = "Uploading: 0% complete";
    finishedMessage.gameObject.SetActive(true);
  }

  protected abstract void DoUpload(
    System.Action<Util.Maybe<ulong>> onComplete, System.Action<WorkshopAssetSource.GetUploadProgress> onStatus);

  void Update()
  {
    if (uploadProgressGetter == null) return;
    float progress = uploadProgressGetter();
    // temp
    finishedMessage.text = "Uploading: " + (progress * 100) + "% complete";
  }

  private void OnUploadComplete(string name, string desc, Util.Maybe<ulong> result)
  {
    uploadProgressGetter = null;
    if (result.GetErrorMessage() != null) finishedMessage.text = result.GetErrorMessage();
    else
    {
      string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={result.Get()}";
      finishedMessage.text = "Finished uploading! (Note: takes about 15 minutes to show up for other players)";
      visitButton.onClick.AddListener(() => Application.OpenURL(url));
      visitButton.gameObject.SetActive(true);
      OnUploadSuccess(result.Get());
    }
  }

  protected virtual void OnUploadSuccess(ulong result) { }
#endif
}
