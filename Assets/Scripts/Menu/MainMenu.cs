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
using UnityEngine;
#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif
using ExitGames.UtilityScripts;
using GameBuilder;

public class MainMenu : MonoBehaviour
{
  [SerializeField] CustomMenuButton resumeButton;
  [SerializeField] CustomMenuButton newButton;
  [SerializeField] CustomMenuButton libraryButton;
  [SerializeField] CustomMenuButton steamWorkshopButton;

  // [SerializeField] GameObject menuPanelManagerPrefab;
  // [SerializeField] RectTransform menuPanelManagerParent;
  // MenuPanelManager menuPanelManager;

  [SerializeField] GameObject multiplayerCode;
  [SerializeField] GameObject popupObject;
  [SerializeField] QuitPopup quitPopup;
  [SerializeField] ThreeWayPopup saveConfirmPopup;

  IPopup[] popupArray;

  [SerializeField] SteamDetailPanel steamDetailPanel;

  [SerializeField] GameObject feedbackPrefab;
  [SerializeField] UnityEngine.UI.Button feedbackButton;

  // [SerializeField] UnityEngine.UI.Button reportButton;
  // [SerializeField] UnityEngine.UI.Button kickButton;
  // [SerializeField] KickPlayer kickPlayer;
  // [SerializeField] ReportPlayer reportPlayer;

  CustomMenuButton[] buttonArray;

  FeedbackForm feedbackForm;

  NetworkingController networkingController;
  GameBuilderSceneController sceneController;
#if USE_STEAMWORKS
  WorkshopItem currentItem;
#endif

  public void Setup()
  {
    Util.FindIfNotSet(this, ref networkingController);
    Util.FindIfNotSet(this, ref sceneController);

    // menuPanelManager = Instantiate(menuPanelManagerPrefab, menuPanelManagerParent).GetComponent<MenuPanelManager>();
    // menuPanelManager.Setup();

    buttonArray = new CustomMenuButton[]{
              resumeButton,
              libraryButton,
              steamWorkshopButton,
            };

    popupArray = new IPopup[] {
      quitPopup,
       saveConfirmPopup
      };

    feedbackButton.onClick.AddListener(GiveFeedback);

    quitPopup.confirmEvent = QuitToMainMenu;
    quitPopup.cancelEvent = () => CancelPopups();

    // saveConfirmPopup.saveNewEvent = () => { saveMenu.SaveNew(); CancelPopups(); };
    saveConfirmPopup.cancelEvent = () => CancelPopups();

    //separated initial click from the toggle event
    //since toggle sometimes programmatically triggerd
    resumeButton.ClickEvent = Resume;
    // libraryButton.ClickEvent = OpenLibrary;
    // steamWorkshopButton.ClickEvent = OpenWorkshop;

    // reportButton.onClick.AddListener(() => reportPlayer.Open());
    // kickButton.onClick.AddListener(() => kickPlayer.Open());

    newButton.ClickEvent = LaunchNew;

    //TO DO HERE
    //steamDetailPanel.SaveLocalCopy = SaveLocalCopyOfCurrentWorkshopItem;
    steamLoadedItemId = GameBuilderApplication.CurrentGameOptions.steamWorkShopFileId;
    // saveMenu.Setup();
  }

  ulong steamLoadedItemId;

  void LaunchTutorial()
  {
    sceneController.RestartAndLoadTutorial();
  }

  void LaunchNew()
  {
    sceneController.RestartAndLoadMinimalScene();
  }

  void GiveFeedback()
  {
    feedbackForm = Instantiate(feedbackPrefab, GetComponent<RectTransform>()).GetComponentInChildren<FeedbackForm>();
  }

  void Update()
  {
    if (networkingController.GetIsInMultiplayer())
    {
      multiplayerCode.SetActive(true);
    }
    else
    {
      multiplayerCode.SetActive(false);
    }
  }


  // public void SaveConfirmPopup(string bundleID)
  // {
  //   saveConfirmPopup.Activate();
  //   popupObject.SetActive(true);
  //   saveConfirmPopup.saveOverwriteEvent = () => { saveMenu.ConfirmSaveOverwrite(bundleID); CancelPopups(); };
  // }

  void QuitPopup()
  {
    quitPopup.Activate();
    popupObject.SetActive(true);
  }

  void CloseToggles()
  {
    foreach (CustomMenuButton button in buttonArray)
    {
      if (button.IsToggle())
      {
        button.SetToggle(false);
      }
    }
  }

  bool CancelPopups()
  {
    bool anyPopups = false;
    for (int i = 0; i < popupArray.Length; i++)
    {
      if (popupArray[i].IsActive())
      {
        anyPopups = true;
        popupArray[i].Deactivate();
      }
    }

    popupObject.SetActive(false);

    return anyPopups;
  }

  void CloseTogglesExceptForOne(CustomMenuButton exceptionButton)
  {
    foreach (CustomMenuButton button in buttonArray)
    {
      if (button.IsToggle() && button != exceptionButton)
      {
        button.SetToggle(false);
      }
    }
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;

  }
  public void Close()
  {
    CloseToggles();
    foreach (CustomMenuButton button in buttonArray)
    {
      button.Reset();
    }


    // saveMenu.Close();
#if USE_STEAMWORKS
    steamDetailPanel.Close();
#endif

    gameObject.SetActive(false);
  }

  public void Back()
  {
    if (CancelPopups())
    {
      return;
    }

    if (feedbackForm != null)
    {
      feedbackForm.Close();
      return;
    }

    // if (menuPanelManager.Back())
    // {
    //   return;
    // }

    // if (kickPlayer.IsOpen())
    // {
    //   kickPlayer.Close();
    //   return;
    // }

    // if (reportPlayer.IsOpen())
    // {
    //   reportPlayer.Close();
    //   return;
    // }

    Close();
  }

  void UpdateMultiplayerAdminButtonsVisibility()
  {
    //   bool visible = networkingController.GetIsInMultiplayer();
    //   reportButton.gameObject.SetActive(visible);
    //   kickButton.gameObject.SetActive(visible && PhotonNetwork.isMasterClient);
  }


  //TO DO: Steam panel here...

  public void Open()
  {
    gameObject.SetActive(true);

#if USE_STEAMWORKS
    if (steamLoadedItemId == 0)
    {
      // saveMenu.Open();
    }
    else
    {
      SteamUtil.GetWorkShopItem(steamLoadedItemId, maybeItem =>
      {
        if (!maybeItem.IsEmpty())
        {
          currentItem = maybeItem.Value;
          steamDetailPanel.Open(maybeItem.Value);
        }
      });
    }
#endif

    UpdateMultiplayerAdminButtonsVisibility();

  }

  void Resume()
  {
    CloseToggles();
    Close();
  }

  void QuitToMainMenu()
  {
    CancelPopups();
    CloseToggles();
    sceneController.LoadSplashScreen();
  }

  // public void SaveLocalCopyOfCurrentWorkshopItem(Texture2D thumbnailTexture, string name, string description)
  // {
  //   steamDetailPanel.Close();
  //   saveMenu.Open();
  //   name = name + " (local copy)";
  //   saveMenu.SetAllInfo(thumbnailTexture, name, description);
  //   steamLoadedItemId = 0;
  //   saveMenu.SaveNew();
  // }
}
