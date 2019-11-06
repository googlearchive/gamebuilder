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
using System.IO;
#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif
using URP = UnityEngine.Rendering.PostProcessing;

using UnityEngine;

using GameBuilder;
using System;

public class SaveMenu : MonoBehaviour
{
  UserMain userMain;
  EditMain editMain;
  SnapshotCamera snapshotCamera;
  GameBuilderSceneController sceneController;
  [SerializeField] SaveUI saveUI;

  GameBuilder.GameBundleLibrary gameBundleLibrary;
  GameResuming resuming;
  DynamicPopup popups;
  SaveLoadController saveLoad;
  URP.PostProcessVolume post;

  Texture2D currentSaveImage;
  ItemWithTooltipWithEventSystem workshopTooltip;

  bool quitAfterSave = false;

  bool uploading = false;
  bool uploaddesired = false;

  Camera thumbnailCam = null;

  bool CanSaveOrUpload()
  {
    return !uploading;
  }

  public void Setup()
  {
    Util.FindIfNotSet(this, ref gameBundleLibrary);
    Util.FindIfNotSet(this, ref sceneController);
    Util.FindIfNotSet(this, ref resuming);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref saveLoad);
    Util.FindIfNotSet(this, ref snapshotCamera);

    saveUI.saveButton.onClick.AddListener(SaveOverwrite);
    saveUI.newSaveButton.onClick.AddListener(SaveNew);
    saveUI.workshopButton.onClick.AddListener(RequestUploadToWorkshop);
    saveUI.screenshotButton.onClick.AddListener(() =>
    {
      Texture2D res = snapshotCamera.SnapshotGameView();
      SetAllInfo(res, saveUI.nameInput.text, saveUI.descriptionInput.text);
    });
    saveUI.closeButton.onClick.AddListener(Close);
    workshopTooltip = saveUI.workshopButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();

    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref editMain);
  }

  // Returns the prior enabled value.
  bool SetMotionBlurEnabled(bool enabled)
  {
    URP.MotionBlur blur = null;
    var oldEnabled = false;
    post.profile.TryGetSettings(out blur);
    if (blur != null)
    {
      oldEnabled = blur.enabled.value;
      blur.enabled.value = enabled;
    }
    return oldEnabled;
  }

  void UpdateWorkshopTooltext(string text)
  {
    workshopTooltip.SetDescription(text);
  }


  public void SetAllInfo(Texture2D texture, string nameText, string descriptionText)
  {
    if (texture != null)
    {
      currentSaveImage = texture;
      saveUI.screenshotImage.gameObject.SetActive(true);
      saveUI.screenshotImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
    }

    saveUI.nameInput.text = nameText;
    saveUI.descriptionInput.text = descriptionText;
  }

  string GetActiveBundleId()
  {
    return GameBuilderApplication.ActiveBundleId;
  }

  bool HasCurrentBeenSaved()
  {
    return GetActiveBundleId() != null;
  }

  public void SaveOverwrite()
  {
    if (!CanSaveOrUpload())
    {
      Debug.Log("Should not be able to save if saving or uploading");
      return;
    }


    string existingBundleId = GetActiveBundleId();
    if (existingBundleId == null)
    {
      saveUI.feedbackTextPrimary.gameObject.SetActive(true);
      saveUI.feedbackTextPrimaryButton.onClick.RemoveAllListeners();
      saveUI.feedbackTextPrimary.text = "Making new save...";

      bool wasRecovering = GameBuilderApplication.IsRecoveryMode;
      string autosaveId = GameBuilderApplication.CurrentGameOptions.bundleIdToLoad;
      string newBundleId = null;

      if (wasRecovering)
      {
      }

      newBundleId = gameBundleLibrary.SaveNew(saveLoad, GetMetadataFromUI(), currentSaveImage, () =>
      {
        if (wasRecovering)
        {
          // HACKY! Copy over the workshop file from the autosave, if any.
          // Ideally, SaveNew would kinda do this for us...
          string workshopXMLFile = Path.Combine(gameBundleLibrary.GetBundleDirectory(autosaveId), SteamUtil.WorkshopItemInfoFileName);
          if (File.Exists(workshopXMLFile))
          {
            string destPath = Path.Combine(gameBundleLibrary.GetBundleDirectory(newBundleId), SteamUtil.WorkshopItemInfoFileName);
            File.Copy(workshopXMLFile, destPath);
          }
        }

        OnSaveToFileCompleted();
      });
      GameBuilderApplication.CurrentGameOptions.mutable.lastManuallySavedBundleId = newBundleId;
      resuming.SetBundleForResuming(newBundleId);
    }
    else
    {
      bool differentName = gameBundleLibrary.GetBundle(existingBundleId).GetMetadata().name != GetMetadataFromUI().name;
      if (differentName)
      {
        WarnThatDifferentNameWillOverwrite(() => ConfirmSaveOverwrite(existingBundleId));
      }
      else
      {
        ConfirmSaveOverwrite(existingBundleId);
      }
    }
  }

  void WarnThatDifferentNameWillOverwrite(System.Action overwriteAction)
  {
    popups.Show(new DynamicPopup.Popup
    {
      fullWidthButtons = true,
      textWrapWidth = 800f,
      getMessage = () => $"You gave the project a different name since last save.\nSave as new file or overwrite?",
      buttons = new List<PopupButton.Params>() {
          new PopupButton.Params{ getLabel = () => "Save as new file", onClick =SaveNew },
          new PopupButton.Params { getLabel = () => "Overwrite last save", onClick = overwriteAction }
        }
    });
  }

  public void ConfirmSaveOverwrite(string bundleID)
  {
    gameBundleLibrary.SaveMaybeOverwrite(saveLoad, bundleID, GetMetadataFromUI(), currentSaveImage, OnSaveToFileCompleted);
    GameBuilderApplication.CurrentGameOptions.mutable.lastManuallySavedBundleId = bundleID;
    saveUI.feedbackTextPrimary.gameObject.SetActive(true);
    saveUI.feedbackTextPrimaryButton.onClick.RemoveAllListeners();
    saveUI.feedbackTextPrimary.text = "Overwriting save...";
    resuming.SetBundleForResuming(bundleID);
  }

  public void SaveNew()
  {
    if (!CanSaveOrUpload())
    {
      Debug.Log("Should not be able to save if saving or uploading");
      return;
    }
    saveUI.feedbackTextPrimary.gameObject.SetActive(true);
    saveUI.feedbackTextPrimary.text = "Making new save...";
    saveUI.feedbackTextPrimaryButton.onClick.RemoveAllListeners();


    string newBundleId = gameBundleLibrary.SaveNew(saveLoad, GetMetadataFromUI(), currentSaveImage, OnSaveToFileCompleted);
    resuming.SetBundleForResuming(newBundleId);
    GameBuilderApplication.CurrentGameOptions.mutable.lastManuallySavedBundleId = newBundleId;
  }

  GameBundle.Metadata GetMetadataFromUI()
  {
    GameBundle.Metadata metadata = new GameBundle.Metadata();

    if (saveUI.nameInput.text.Length > 0)
    {
      metadata.name = saveUI.nameInput.text;
    }
    else
    {
      metadata.name = "(unnamed scene)";
    }

    if (saveUI.descriptionInput.text.Length > 0)
    {
      metadata.description = saveUI.descriptionInput.text;
    }
    else
    {
      metadata.description = "(no description)";
    }

    return metadata;
  }

  void RequestUploadToWorkshop()
  {
    if (uploaddesired || uploading)
    {
      Debug.Log("Should not be able to request upload");
      return;
    }

    uploaddesired = true;
    SaveOverwrite();
  }
#if USE_STEAMWORKS
  WorkshopItemUpdate currentUploadItem;

  void UploadActiveSaveToWorkshop(bool newUpload = false)
  {
    var bundleId = GetActiveBundleId();
    Debug.Assert(bundleId != null);

    uploaddesired = false;
    uploading = true;

    string contentPath = gameBundleLibrary.GetBundleDirectory(bundleId);
    WorkshopItemUpdate uploadItem = null;
    if (File.Exists(Path.Combine(contentPath, SteamUtil.WorkshopItemInfoFileName)))
    {
      uploadItem = SteamWorkshopMain.Instance.GetItemUpdateFromFolder(contentPath);
    }

    if (uploadItem == null || newUpload)
    {
      uploadItem = new WorkshopItemUpdate();
    }

    GameBundle.Metadata metadata = GetMetadataFromUI();
    uploadItem.Name = metadata.name;
    uploadItem.Description = metadata.description;
    uploadItem.ContentPath = contentPath;
    if (gameBundleLibrary.GetBundle(bundleId).GetThumbnail() != null)
    {
      uploadItem.IconPath = gameBundleLibrary.GetBundle(bundleId).GetThumbnailPath();
    }
    uploadItem.Tags.Add(SteamUtil.GameBuilderTags.Project.ToString());
    SteamWorkshopMain.Instance.Upload(uploadItem, (args) => UploadCallback(args, uploadItem));
    currentUploadItem = uploadItem;
  }

  void UploadCallback(WorkshopItemUpdateEventArgs args, WorkshopItemUpdate uploadItem)
  {
    uploading = false;
    currentUploadItem = null;
    saveUI.feedbackTextSecondary.gameObject.SetActive(true);
    saveUI.feedbackTextPrimary.gameObject.SetActive(false);

    if (args.IsError)
    {
      if (args.ErrorMessage == "File was not found!")
      {
        saveUI.feedbackTextSecondary.SetText($"No workshop file found - attempting new upload...");
        saveUI.feedbackTextSecondaryButton.onClick.RemoveAllListeners();
        UploadActiveSaveToWorkshop(true /* forcing new upload */);
      }
    }
    else
    {
      string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={uploadItem.SteamNative.m_nPublishedFileId.ToString()}";
      saveUI.feedbackTextSecondary.SetText($"Workshop URL (must be logged in):\n{url}");
      saveUI.feedbackTextSecondaryButton.onClick.AddListener(() => Application.OpenURL(url));
      fileOnSteamWorkshop = true;

      PerformWorkshopVisibilityCheck(uploadItem.SteamNative.m_nPublishedFileId.m_PublishedFileId);
    }
  }
#endif
  public void SetOpen(bool on)
  {
    if (on) Open();
    else Close();
  }
#if USE_STEAMWORKS
  void PerformWorkshopVisibilityCheck(ulong fileId)
  {
    GameBuilder.SteamUtil.QueryWorkShopItemVisibility(fileId, maybeVisible =>
    {
      if (maybeVisible.IsEmpty())
      {
        popups.Show("Sorry, something went wrong while trying to upload your item. Please restart Game Builder and try again.", "OK", () => { });
      }
      else
      {
        bool isPubliclyVisible = maybeVisible.Value;
        float wrapWidth = 600f;
        if (!isPubliclyVisible)
        {
          popups.Show("Sorry, your upload failed. Make sure you have recently accepted the Steam Subscriber Workshop agreement:",
          "Open Steam Subscriber Agreement", () =>
          {
            Application.OpenURL("https://steamcommunity.com/workshop/workshoplegalagreement/");
            popups.Show("After accepting, please restart Steam and Game Builder and try uploading again."
            + "<size=60%>\n\n(NOTE: If you have not spent at least $5 on your Steam account, you will not be able to upload. Sorry, this is Steam Policy.)\n\n",
            "OK", () => { }, wrapWidth);
          }, wrapWidth);
        }
        else
        {
          Util.Log($"OK - {fileId} is publicly visible.");
        }
      }
    });
  }
#endif

  void OnSaveToFileCompleted()
  {
    if (quitAfterSave) quitCallback();
#if USE_STEAMWORKS
    if (uploaddesired) UploadActiveSaveToWorkshop();
#endif

    // Effectively force a refresh of everything, such as the fileOnSteamWorkshop variable.
    if (IsOpen()) Open();
    saveUI.feedbackTextPrimary.text = $"Saved! Click here to see the file.";
    string url = gameBundleLibrary.GetBundleDirectory(GameBuilderApplication.CurrentGameOptions.mutable.lastManuallySavedBundleId);
    saveUI.feedbackTextPrimaryButton.onClick.AddListener(() => Application.OpenURL(url));
    saveUI.feedbackTextPrimary.gameObject.SetActive(true);
  }

  bool fileOnSteamWorkshop = false;

  System.Action quitCallback;

  public void SaveBeforeExit(System.Action quitCallback)
  {
    this.quitAfterSave = true;
    this.quitCallback = quitCallback;
    if (HasCurrentBeenSaved())
    {
      if (!IsOpen())
      {

        string existingBundleId = GetActiveBundleId();
        if (existingBundleId != null)
        {
          GameBundle bundle = gameBundleLibrary.GetBundle(existingBundleId);
          saveUI.nameInput.text = bundle.GetMetadata().name;
          saveUI.descriptionInput.text = bundle.GetMetadata().description;
          currentSaveImage = bundle.GetThumbnail();
        }
      }
      SaveOverwrite();
    }
    else
    {
      Open();
    }
  }

  public void SaveFromShortcut()
  {
    if (HasCurrentBeenSaved())
    {
      userMain.AddDebugMessage("Project saved!");
      if (!IsOpen())
      {
        string existingBundleId = GetActiveBundleId();
        if (existingBundleId != null)
        {
          GameBundle bundle = gameBundleLibrary.GetBundle(existingBundleId);
          saveUI.nameInput.text = bundle.GetMetadata().name;
          saveUI.descriptionInput.text = bundle.GetMetadata().description;
          currentSaveImage = bundle.GetThumbnail();
        }
      }
      SaveOverwrite();
    }
    else
    {
      Open();
    }
  }

  public void Open()
  {
    if (IsOpen()) return;
    UpdateSaveButtonTextAndVisibility();

    saveUI.feedbackTextPrimary.gameObject.SetActive(false);

    saveUI.feedbackTextSecondary.gameObject.SetActive(false);
    gameObject.SetActive(true);
    Update();

    string existingBundleId = GetActiveBundleId();
    if (existingBundleId != null)
    {
      GameBundle bundle = gameBundleLibrary.GetBundle(existingBundleId);
      saveUI.nameInput.text = bundle.GetMetadata().name;
      saveUI.descriptionInput.text = bundle.GetMetadata().description;
      currentSaveImage = bundle.GetThumbnail();

#if USE_STEAMWORKS
      WorkshopItemUpdate uploadItem = null;

      //see if the xml file is there
      if (File.Exists(Path.Combine(gameBundleLibrary.GetBundleDirectory(existingBundleId), SteamUtil.WorkshopItemInfoFileName)))
      {
        uploadItem = SteamWorkshopMain.Instance.GetItemUpdateFromFolder(gameBundleLibrary.GetBundleDirectory(existingBundleId));
      }

      if (uploadItem != null)
      {
        string url = $"https://steamcommunity.com/sharedfiles/filedetails/?id={uploadItem.SteamNative.m_nPublishedFileId.ToString()}";
        saveUI.feedbackTextSecondary.SetText($"Workshop URL (must be logged in):\n{url}");
        saveUI.feedbackTextSecondaryButton.onClick.AddListener(() => Application.OpenURL(url));
        saveUI.feedbackTextSecondary.gameObject.SetActive(true);
        fileOnSteamWorkshop = true;
      }
      else
#endif
      {
        saveUI.feedbackTextSecondary.gameObject.SetActive(false);
        fileOnSteamWorkshop = false;
      }
    }
    else if (GameBuilderApplication.IsRecoveryMode)
    {
      saveUI.feedbackTextPrimary.gameObject.SetActive(true);
      saveUI.feedbackTextPrimary.text = "Recovering will create a new save, and you can continue building normally from it. Your old project will still exist in your game library in case you need to go back to it.";
      saveUI.feedbackTextPrimaryButton.onClick.RemoveAllListeners();

      // Fill in these fields for convenience
      string autosaveBundleId = GameBuilderApplication.CurrentGameOptions.bundleIdToLoad;
      GameBundle bundle = gameBundleLibrary.GetBundle(autosaveBundleId);
      saveUI.nameInput.text = bundle.GetMetadata().name;
      saveUI.descriptionInput.text = bundle.GetMetadata().description;
      currentSaveImage = bundle.GetThumbnail();
    }

    if (currentSaveImage != null)
    {
      saveUI.screenshotImage.gameObject.SetActive(true);
      saveUI.screenshotImage.sprite = Sprite.Create(currentSaveImage, new Rect(0, 0, currentSaveImage.width, currentSaveImage.height), new Vector2(.5f, .5f), 100);
    }
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Toggle()
  {
    if (IsOpen()) Close();
    else Open();
  }

#if USE_STEAMWORKS
  string GetUploadText()
  {
    if (currentUploadItem != null)
    {
      return $"uploading...{Mathf.FloorToInt(100 * SteamWorkshopMain.Instance.GetUploadProgress(currentUploadItem))}%";
    }
    else
    {
      return "uploading..";
    }
  }
#endif

  public void Close()
  {
    gameObject.SetActive(false);
    userMain.SetMouseoverTooltipText("");
  }

  bool AllFieldsComplete()
  {
    bool pictureFieldComplete = currentSaveImage != null;
    bool nameFieldComplete = saveUI.nameInput.text.Length > 0 && saveUI.nameInput.text != "(unnamed scene)";
    bool descriptionInputComplete = saveUI.descriptionInput.text.Length > 0 && saveUI.descriptionInput.text != "(no description)";


    if (pictureFieldComplete && nameFieldComplete && descriptionInputComplete)
    {
      UpdateWorkshopTooltext("");
      return true;
    }
    else
    {
      string workshopTooltipText = "Requires:";
      if (!nameFieldComplete) workshopTooltipText += "\n-Name";
      if (!descriptionInputComplete) workshopTooltipText += "\n-Description";
      if (!pictureFieldComplete) workshopTooltipText += "\n-Picture";
      UpdateWorkshopTooltext(workshopTooltipText);
      return false;
    }

  }

  void UpdateSaveButtonTextAndVisibility()
  {
    bool hasSaved = HasCurrentBeenSaved();
    saveUI.newSaveButton.gameObject.SetActive(hasSaved && !quitAfterSave);
    #if USE_STEAMWORKS
    saveUI.workshopButton.gameObject.SetActive(hasSaved && !quitAfterSave);
    #else
    saveUI.workshopButton.gameObject.SetActive(false);
    #endif
    if (GameBuilderApplication.IsRecoveryMode)
    {
      saveUI.saveButtonText.text = "Recover & Save";
    }
    else
    {
      if (quitAfterSave)
      {
        saveUI.saveButtonText.text = "Save & Exit";
      }
      else
      {
        saveUI.saveButtonText.text = "Save";
      }
    }
  }

  // string workshopTooltipText = "";
  void Update()
  {
    UpdateSaveButtonTextAndVisibility();

    if (AllFieldsComplete())
    {
      saveUI.workshopButton.interactable = true;
    }
    else
    {
      saveUI.workshopButton.interactable = false;
    }

    if (uploaddesired)
    {
      saveUI.workshopButtonText.text = "(uploading...)";
    }
    else if (uploading)
    {
#if USE_STEAMWORKS
      saveUI.workshopButtonText.text = GetUploadText();
#endif
    }
    else
    {
      saveUI.workshopButtonText.text = fileOnSteamWorkshop ? "Update on Steam Workshop" : "Share on Steam Workshop";
    }
  }
}
