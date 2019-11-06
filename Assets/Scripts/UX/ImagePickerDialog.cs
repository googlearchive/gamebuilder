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

using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePickerDialog : MonoBehaviour
{
  private const string PREFAB_PATH = "ImagePicker";
  private const long IMAGE_FILE_SIZE_LIMIT = 1048576;

  [SerializeField] Button okButton;
  [SerializeField] Button cancelButton;
  [SerializeField] Image previewImage;
  [SerializeField] Button uploadButton;
  [SerializeField] GameObject pleaseWait;
  [SerializeField] TMPro.TMP_Text imageLoadingIndicator;
  [SerializeField] TMPro.TMP_Text steamWorkshopWarningText;
  ImageSystem imageSystem;
  DynamicPopup popups;
  ImageLoader imageLoader;
  WorkshopAssetSource workshopAssetSource;
  string currentImageId;

  // URL of the image we're currently loading
  string currentlyLoadingUrl;

  // Called when the image picker is closed.
  // success: True if user picked an image.
  // pickedImageId: the picked image ID, or null if canceled.
  public delegate void OnImagePickerResult(bool success, string pickedImageId);

  OnImagePickerResult callback;

  public static void Launch(string currentImageId, Canvas parentCanvas, OnImagePickerResult callback)
  {
    GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(PREFAB_PATH));
    if (parentCanvas != null) obj.transform.SetParent(parentCanvas.transform, false);
    obj.GetComponent<ImagePickerDialog>().Setup(currentImageId, callback);
  }

  public void Setup(string currentImageId, OnImagePickerResult callback)
  {
    this.currentImageId = currentImageId;
    this.callback = callback;
    pleaseWait.SetActive(false);
    okButton.onClick.AddListener(OnOkClicked);
    cancelButton.onClick.AddListener(() => CloseAndReturn(false, null));
    uploadButton.onClick.AddListener(() => OnUploadClicked());
    Util.FindIfNotSet(this, ref imageSystem);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref imageLoader);
    Util.FindIfNotSet(this, ref workshopAssetSource);

    imageLoadingIndicator.text = "";

    if (!string.IsNullOrEmpty(currentImageId))
    {
      string url = imageSystem.GetImageUrl(currentImageId);
      if (url != null)
      {
        LoadPreviewImage(url);
      }
    }
    else
    {
      imageLoadingIndicator.text = "(No image - click Upload to upload an image)";
    }

#if !USE_STEAMWORKS
    steamWorkshopWarningText.text = "Images are uploaded (copied) to the local cache.";
#endif
  }

  void LoadPreviewImage(string url)
  {
    imageLoadingIndicator.text = "Loading...";
    previewImage.sprite = null;
    previewImage.color = Color.gray;
    currentlyLoadingUrl = url;
    imageLoader.Request(url, previewImage, UpdateImageLoadingIndicator);
  }

  void UpdateImageLoadingIndicator(string loadedUrl, bool success)
  {
    // If this was called for anything that's not the image we're loading, ignore.
    if (loadedUrl != currentlyLoadingUrl) return;

    imageLoadingIndicator.text = success ? "" : "Error loading image";
  }

  void OnOkClicked()
  {
    CloseAndReturn(true, currentImageId);
  }

  void CloseAndReturn(bool success, string imageId)
  {
    GameObject.Destroy(gameObject);
    callback(success, imageId);
  }

  void Update()
  {
    previewImage.preserveAspect = true;
  }

  void OnUploadClicked()
  {
#if USE_FILEBROWSER
    Crosstales.FB.ExtensionFilter[] filters = new Crosstales.FB.ExtensionFilter[] {
      new Crosstales.FB.ExtensionFilter("PNG files", "png"),
      new Crosstales.FB.ExtensionFilter("JPG files", "jpg"),
    };
    var paths = Crosstales.FB.FileBrowser.OpenFiles("Upload image", "", filters);
    OnSelectedFileToUpload(paths);
#else
    popups.ShowTextInput("Enter full path of PNG or JPG image:", "", path => OnSelectedFileToUpload(new string[] { path }));
#endif
  }

  void OnSelectedFileToUpload(string[] selections)
  {
    if (selections == null || selections.Length == 0)
    {
      // Canceled.
      return;
    }
    string fullPath = selections[0];
    if (fullPath.IsNullOrEmpty()) return;
    if (!File.Exists(fullPath))
    {
      popups.Show("File does not exist: " + fullPath, "OK", () => { });
      return;
    }
    long size = new FileInfo(fullPath).Length;
    if (size > IMAGE_FILE_SIZE_LIMIT)
    {
      popups.Show("That file is too big (" + size + " bytes). The limit is " + IMAGE_FILE_SIZE_LIMIT + " bytes", "OK", () => { });
      return;
    }

    string name = Path.GetFileNameWithoutExtension(fullPath);
    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
    name = string.IsNullOrEmpty(name) ? "Untitled image" : name;
    pleaseWait.SetActive(true);
    string tempDir = Util.CreateTempDirectory();
    File.Copy(fullPath, Path.Combine(tempDir, "image" + ext));

    workshopAssetSource.Put(tempDir, name, name, GameBuilder.SteamUtil.GameBuilderTags.Image, null, null, OnWorkshopUploadComplete);
  }

  void OnWorkshopUploadComplete(Util.Maybe<ulong> result)
  {
    pleaseWait.SetActive(false);
    if (result.IsEmpty())
    {
      // Error.
      popups.Show("Failed to upload image to Steam Workshop. " + result.GetErrorMessage(), "OK", () => { });
      return;
    }
    popups.Show("Image uploaded successfully.", "OK", () => { });
    LoadPreviewImage("sw:" + result.Get());
    ulong steamWorkshopId = result.Get();
    // Import image into the Image System and get its ID.
    currentImageId = imageSystem.ImportImageFromUrl("sw:" + steamWorkshopId);
  }
}
