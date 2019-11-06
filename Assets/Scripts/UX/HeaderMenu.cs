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
using UnityEngine;
using GameBuilder;

public class HeaderMenu : MonoBehaviour
{
  [SerializeField] WindowHeaderUI windowHeaderUI;
  [SerializeField] WorldSettings worldVisualsPrefab;

  [SerializeField] MenuPanelManager menuPanelManagerPrefab;
  [SerializeField] WorkshopInfo workshopInfoPrefab;
  [SerializeField] SaveMenu saveMenuPrefab;
  [SerializeField] MultiplayerGameMenu multiplayerGameMenuPrefab;

  [SerializeField] TemplateSelectorMenu templateSelectorMenu;
  [SerializeField] Color enabledButtonColor;
  [SerializeField] Color disabledButtonColor;

  MenuPanelManager menuPanelManager;
  SaveMenu saveMenu;
  public MultiplayerGameMenu multiplayerGameMenu { get; private set; }
  LoadingScreen loadingScreen;

  WorkshopInfo workshopInfo;
  WorldSettings worldSettings;
  SidebarManager sidebarManager;

  UserMain userMain;
  EditMain editMain;
  DynamicPopup dynamicPopup;
  GameBuilderSceneController sceneController;

  HierarchyPanelController hierarchyPanelController;
  bool wasInEditMode;
  private Vector3[] tempWorldCorners = new Vector3[4];

  // Used to determine whether we need to update the left header layout
  private bool prevCanEdit = false;
  private bool prevActorsEditable = false;
  private float prevMaxWidth = 0;

  public void Awake()
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref dynamicPopup);
    Util.FindIfNotSet(this, ref sceneController);
    Util.FindIfNotSet(this, ref hierarchyPanelController);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref sidebarManager);

    windowHeaderUI.viewButton.onClick.AddListener(() =>
    {
      userMain.NextCameraView();
    });
    windowHeaderUI.rotateButton.onClick.AddListener(() =>
    {
      userMain.RotateCameraView();
    });

    windowHeaderUI.copyButton.onClick.AddListener(editMain.TryCopy);
    windowHeaderUI.undoButton.onClick.AddListener(userMain.TryUndo);
    windowHeaderUI.redoButton.onClick.AddListener(userMain.TryRedo);
    windowHeaderUI.deleteButton.onClick.AddListener(editMain.DeleteTargetActors);
    windowHeaderUI.cameraFocusToggle.onValueChanged.AddListener(editMain.SetCameraFollowingActor);

    windowHeaderUI.deleteButton.gameObject.SetActive(false);
    windowHeaderUI.cameraFocusToggle.gameObject.SetActive(false);
    windowHeaderUI.copyButton.gameObject.SetActive(false);

    windowHeaderUI.actorListToggle.onValueChanged.AddListener(hierarchyPanelController.SetExpanded);

    worldSettings = GameObject.Instantiate(worldVisualsPrefab, userMain.getEditRect());
    worldSettings.Setup();

    windowHeaderUI.worldVisualsToggle.onValueChanged.AddListener(worldSettings.SetIsOpen);

    SetupTooltips();
    SetupLeftSideMenu();
    templateSelectorMenu.Setup();
  }

  private void Start()
  {
    UpdateLeftHeader(true /* force update */);
  }

  public void OpenMultiplayerMenu()
  {
    multiplayerGameMenu.SetOpen(true);
  }

  public SaveMenu GetSaveMenu()
  {
    return saveMenu;
  }

  bool localCopyOfSteamWorkshopItem = false;
  ulong GetWorkshopFileID()
  {
    if (localCopyOfSteamWorkshopItem) return 0;
    return GameBuilderApplication.CurrentGameOptions.steamWorkShopFileId;
  }


  ItemWithTooltipWithEventSystem saveTooltip;
  const string SAVE_TOOLTIP = "Save Project";
  const string WORKSHOP_TOOLTIP = "Workshop Game Info";

  private void SetupTooltips()
  {
    windowHeaderUI.systemToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("System Menu");
    windowHeaderUI.newButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("New Project");

    saveTooltip = windowHeaderUI.saveToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    saveTooltip.SetDescription(SAVE_TOOLTIP);

    windowHeaderUI.openButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Game Library");
    windowHeaderUI.multiplayerToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Multiplayer");
    windowHeaderUI.pauseButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Play/Pause Game\n(CTRL+P)");
    windowHeaderUI.resetButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Restart Game\n(CTRL+R)");
    windowHeaderUI.viewButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Change View (V)");
    windowHeaderUI.rotateButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Rotate View");
    windowHeaderUI.actorListToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Actors\n(CTRL+L)");
    windowHeaderUI.cameraFocusToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Focus Camera (F)");
    windowHeaderUI.deleteButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Delete Selection\n(Delete)");
    windowHeaderUI.copyButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Copy Selection\n(CTRL+C)");
    windowHeaderUI.undoButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Undo\n(CTRL+Z)");
    windowHeaderUI.redoButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Redo\n(SHIFT+CTRL+Z)");
    windowHeaderUI.playOnlyObject.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Build mode disabled by host");

  }

  public void RequestSystemMenu(bool on)
  {
    userMain.systemMenu.SetOpen(on);
  }

  void SetupLeftSideMenu()
  {
#if USE_STEAMWORKS
    workshopInfo = GameObject.Instantiate(workshopInfoPrefab, userMain.getEditRect());
    workshopInfo.SaveLocalCopy = SaveLocalCopyOfWorkshopItem;
#endif



    windowHeaderUI.newButton.onClick.AddListener(OnNewButton);

    menuPanelManager = Instantiate(menuPanelManagerPrefab);
    menuPanelManager.Setup();

    saveMenu = GameObject.Instantiate(saveMenuPrefab, userMain.getEditRect());
    saveMenu.Setup();
    multiplayerGameMenu = GameObject.Instantiate(multiplayerGameMenuPrefab, userMain.getEditRect());
    multiplayerGameMenu.Setup();
    multiplayerGameMenu.GetJoinMultiplayerButton().onClick.AddListener(ShowMultiplayerPopup);

    windowHeaderUI.openButton.onClick.AddListener(OnOpenButton);
#if USE_PUN

    windowHeaderUI.multiplayerToggle.onValueChanged.AddListener(multiplayerGameMenu.SetOpen);
#else
    windowHeaderUI.multiplayerToggle.gameObject.SetActive(false);
    RectTransform playHeaderBGRect = windowHeaderUI.playHeaderBackground.GetComponent<RectTransform>();
    float bgRectX = playHeaderBGRect.sizeDelta.x;
    playHeaderBGRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgRectX - 50);
#endif

    windowHeaderUI.systemToggle.onValueChanged.AddListener(RequestSystemMenu);
    windowHeaderUI.saveToggle.onValueChanged.AddListener(SetOpenSaveOrWorkshop);

    windowHeaderUI.moreToggle.onValueChanged.AddListener(SetOpenOverflow);
  }

  public void SetEditCanvasAlpha(float f)
  {
    foreach (CanvasGroup cg in windowHeaderUI.editOnlyCanvasGroups)
    {
      cg.alpha = f;
    }
  }

  public void SetEditCanvasInteractable(bool value)
  {
    foreach (CanvasGroup cg in windowHeaderUI.editOnlyCanvasGroups)
    {
      cg.interactable = value;
      cg.blocksRaycasts = value;
    }
  }

  public void SetPlayBackground(bool value)
  {
    windowHeaderUI.playHeaderBackground.gameObject.SetActive(value);
  }


  void OnOpenButton()
  {
    menuPanelManager.SetOpen(true);
  }

  void OnNewButton()
  {
    ShowNewPopup();
  }

  void OnMultiplayerExitButton()
  {
    LoadMultiplayerSplashMenu();
  }

  private void SetOpenOverflow(bool value)
  {
    windowHeaderUI.leftMenuOverflow.gameObject.SetActive(value);
    StartCoroutine(AlignSettingsMenus());
  }

  public void SaveLocalCopyOfWorkshopItem(Texture2D thumbnailTexture, string name, string description)
  {
#if USE_STEAMWORKS
    workshopInfo.Close();
#endif
    localCopyOfSteamWorkshopItem = true;
    saveMenu.SetOpen(true);
    name = name + " (local copy)";
    saveMenu.SetAllInfo(thumbnailTexture, name, description);
    saveMenu.SaveNew();
  }

  public bool AreMenusActive()
  {
    return false;
  }

  public bool Back()
  {

    if (templateSelectorMenu.IsOpen())
    {
      templateSelectorMenu.Close();
      return true;
    }
    else if (menuPanelManager.Back())
    {
      return true;
    }
    else if (IsOpenSaveOrWorkshop())
    {
      SetOpenSaveOrWorkshop(false);
      return true;
    }
    else if (multiplayerGameMenu.IsOpen())
    {
      multiplayerGameMenu.SetOpen(false);
      return true;
    }
    else if (userMain.systemMenu.IsOpen())
    {
      userMain.systemMenu.Back();
      return true;
    }
    else
    {
      userMain.systemMenu.Open();
      return true;
    }
  }



  private void ShowNewPopup()
  {
    templateSelectorMenu.Show();
  }

  private void ShowMultiplayerPopup()
  {

    dynamicPopup.Show(new DynamicPopup.Popup
    {
      getMessage = () => "Exit current game and see multiplayer games?",
      isCancellable = true,
      buttons = new List<PopupButton.Params>()
          {
            new PopupButton.Params
            {
              getLabel = () => "Exit",
              onClick = OnMultiplayerExitButton
  },
            new PopupButton.Params
            {
              getLabel = () => "Cancel",

            }
          },
      fullWidthButtons = true
    });
  }

  void LoadMultiplayerSplashMenu()
  {
    SplashScreenController.debugShortcutToMpJoin = true;
    loadingScreen.ShowAndDo(() => sceneController.LoadSplashScreen());
  }

  public void SetOpenSaveOrWorkshop(bool on)
  {
    if (!on)
    {
#if USE_STEAMWORKS
      workshopInfo.Close();
#endif
      saveMenu.SetOpen(false);
      return;
    }

    if (!ShowWorkshopInfo())
    {

      saveMenu.SetOpen(true);
    }
    else
    {
#if USE_STEAMWORKS
      SteamUtil.GetWorkShopItem(GetWorkshopFileID(), maybeItem =>
      {
        if (!maybeItem.IsEmpty())
        {
          LapinerTools.Steam.Data.WorkshopItem item = maybeItem.Value;
          workshopInfo.Open(maybeItem.Value);
        }
      });
#endif

    }
  }

  bool IsOpenSaveOrWorkshop()
  {
    return saveMenu.IsOpen()
#if USE_STEAMWORKS
    || workshopInfo.IsOpen()
#endif
    ;
  }

  public bool ShowWorkshopInfo()
  {
    return GetWorkshopFileID() != 0;
  }

  internal void ToggleActorList()
  {
    hierarchyPanelController.SetExpanded(!hierarchyPanelController.IsExpanded());
  }

  // void CloseAllPanels()
  // {
  //   menuPanelManager.SetOpen(false);
  //   menuPanelManager.SetOpen(false);
  //   SetOpenHelp(false);
  //   SetOpenSaveOrWorkshop(false);
  //   hierarchyPanelController.SetExpanded(false);
  // }

  void Update()
  {
    UpdateLeftHeader();

    // right side
    bool logicOpen = sidebarManager.logicSidebar.IsOpenedToFullWidth();
    bool verySlimView = logicOpen && hierarchyPanelController.IsExpanded();

    windowHeaderUI.actorListToggle.isOn = hierarchyPanelController.IsExpanded();
    windowHeaderUI.actorListToggleText.gameObject.SetActive(!logicOpen);
    windowHeaderUI.rotateButton.gameObject.SetActive(userMain.GetCameraView() == CameraView.Isometric && !verySlimView);
    windowHeaderUI.viewButton.gameObject.SetActive(!verySlimView);

    bool isInEditMode = userMain.InEditMode();
    if (isInEditMode != wasInEditMode)
    {
      // UpdateOpenPanel();
      if (!isInEditMode)
      {
        worldSettings.SetIsOpen(false);
        hierarchyPanelController.SetExpanded(false);
      }
      // UpdateOpenMenuPanel();
      wasInEditMode = isInEditMode;
    }
  }

  void UpdateLeftHeader(bool forceUpdate = false)
  {
    bool needsLayoutUpdate = false;

    windowHeaderUI.undoImage.color = userMain.UndoAvailable() ? enabledButtonColor : disabledButtonColor;
    windowHeaderUI.redoImage.color = userMain.RedoAvailable() ? enabledButtonColor : disabledButtonColor;

    bool canEdit = userMain.CanEdit();
    if (canEdit != prevCanEdit || forceUpdate)
    {
      windowHeaderUI.buildButtonObject.SetActive(userMain.CanEdit());
      windowHeaderUI.playButtonObject.SetActive(userMain.CanEdit());
      windowHeaderUI.playOnlyObject.SetActive(!userMain.CanEdit() && !GameBuilderApplication.IsStandaloneExport);
      needsLayoutUpdate = true;
      prevCanEdit = canEdit;
    }

    bool actorsEditable = editMain.ActorsEditable();
    if (actorsEditable != prevActorsEditable || forceUpdate)
    {
      windowHeaderUI.deleteButton.gameObject.SetActive(actorsEditable);
      windowHeaderUI.cameraFocusToggle.gameObject.SetActive(actorsEditable);
      windowHeaderUI.copyButton.gameObject.SetActive(actorsEditable);
      needsLayoutUpdate = true;
      prevActorsEditable = actorsEditable;
    }

    windowHeaderUI.fileSubmenu.gameObject.SetActive(!GameBuilderApplication.IsStandaloneExport);

    windowHeaderUI.systemToggle.isOn = userMain.systemMenu.IsOpen();
    windowHeaderUI.saveToggle.isOn = saveMenu.IsOpen()
#if USE_STEAMWORKS
    || workshopInfo.IsOpen()
#endif
    ;

#if USE_PUN
    windowHeaderUI.multiplayerToggle.isOn = multiplayerGameMenu.IsOpen();
#endif
    if (editMain.ActorsEditable())
    {
      windowHeaderUI.cameraFocusToggle.isOn = editMain.GetCameraFollowingActor();
    }
    windowHeaderUI.worldVisualsToggle.isOn = worldSettings.GetIsOpen();

    bool showWorkshop = ShowWorkshopInfo();
    windowHeaderUI.saveButtonImage.enabled = !showWorkshop;
    windowHeaderUI.workshopButtonImage.enabled = showWorkshop;
    saveTooltip.SetDescription(showWorkshop ? WORKSHOP_TOOLTIP : SAVE_TOOLTIP);

    windowHeaderUI.leftMenuRight.GetComponent<RectTransform>().GetWorldCorners(tempWorldCorners);
    float maxWidth = tempWorldCorners[2].x;
    if (maxWidth != prevMaxWidth)
    {
      needsLayoutUpdate = true;
      prevMaxWidth = maxWidth;
    }

    if (needsLayoutUpdate)
    {
      UpdateSubmenuPositions();
      StartCoroutine(AlignSettingsMenus());
    }
  }

  void UpdateSubmenuPositions()
  {

    // Put everything back to fullscreen position for width calculations
    ResetSubmenuPositions();

    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(windowHeaderUI.leftMenuLeft);
    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(windowHeaderUI.leftMenuRight);

    windowHeaderUI.editSubmenu.rectTransform.GetWorldCorners(tempWorldCorners);
    float editSubmenuWidth = tempWorldCorners[2].x - tempWorldCorners[0].x;
    windowHeaderUI.moreSubmenu.rectTransform.GetWorldCorners(tempWorldCorners);
    float maxWidthWithEdit = tempWorldCorners[2].x - editSubmenuWidth;

    // One at a time, see if each of the submenus can fit into the main menu.
    // If not, put it into the overflow menu.
    // This logic is pretty brittle ...

    // Edit submenu
    windowHeaderUI.undoRedoSubmenu.rectTransform.GetWorldCorners(tempWorldCorners);
    float undoRedoSubmenuEnd = tempWorldCorners[2].x;
    if (undoRedoSubmenuEnd <= maxWidthWithEdit)
    {
      windowHeaderUI.moreToggle.isOn = false;
      windowHeaderUI.moreSubmenu.gameObject.SetActive(false);
      return;
    }
    windowHeaderUI.editSubmenu.transform.SetParent(windowHeaderUI.leftMenuOverflow);
    if (worldSettings.GetIsOpen())
    {
      windowHeaderUI.moreToggle.isOn = true;
    }

    // Undo redo submenu
    windowHeaderUI.moreSubmenu.rectTransform.GetWorldCorners(tempWorldCorners);
    float maxWidthWithMore = tempWorldCorners[0].x;
    if (undoRedoSubmenuEnd <= maxWidthWithMore)
    {
      return;
    }
    windowHeaderUI.undoRedoSubmenu.transform.SetParent(windowHeaderUI.leftMenuOverflow);
    windowHeaderUI.undoRedoSubmenu.transform.SetSiblingIndex(
      windowHeaderUI.editSubmenu.transform.GetSiblingIndex() - 1);
    windowHeaderUI.undoRedoSubmenu.divider.gameObject.SetActive(false);

    // File submenu
    windowHeaderUI.fileSubmenu.rectTransform.GetWorldCorners(tempWorldCorners);
    float fileSubmenuEnd = tempWorldCorners[2].x;
    if (fileSubmenuEnd <= maxWidthWithMore)
    {
      return;
    }
    windowHeaderUI.fileSubmenu.transform.SetParent(windowHeaderUI.leftMenuOverflow);
    windowHeaderUI.fileSubmenu.transform.SetSiblingIndex(
      windowHeaderUI.editSubmenu.transform.GetSiblingIndex() - 2);
    windowHeaderUI.fileSubmenu.divider.gameObject.SetActive(false);
    if (saveMenu.IsOpen() || multiplayerGameMenu.IsOpen())
    {
      windowHeaderUI.moreToggle.isOn = true;
    }
  }

  void ResetSubmenuPositions()
  {
    windowHeaderUI.systemSubmenu.divider.gameObject.SetActive(true);
    windowHeaderUI.fileSubmenu.divider.gameObject.SetActive(true);
    windowHeaderUI.undoRedoSubmenu.divider.gameObject.SetActive(true);
    windowHeaderUI.fileSubmenu.transform.SetParent(windowHeaderUI.leftMenuLeft);
    windowHeaderUI.undoRedoSubmenu.transform.SetParent(windowHeaderUI.leftMenuLeft);
    windowHeaderUI.editSubmenu.transform.SetParent(windowHeaderUI.leftMenuRight);
    windowHeaderUI.editSubmenu.transform.SetAsFirstSibling();
    windowHeaderUI.moreSubmenu.gameObject.SetActive(true);
  }

  private IEnumerator AlignSettingsMenus()
  {
    yield return null;

    // Note that the system / help menus don't need to be aligned since they never move.

    AlignSettingsMenu(
      saveMenu.GetComponent<RectTransform>(),
      windowHeaderUI.saveToggle.GetComponent<RectTransform>());

#if USE_STEAMWORKS
    AlignSettingsMenu(
      workshopInfo.GetComponent<RectTransform>(),
      windowHeaderUI.saveToggle.GetComponent<RectTransform>());
#endif

#if USE_PUN
    AlignSettingsMenu(
      multiplayerGameMenu.GetComponent<RectTransform>(),
      windowHeaderUI.multiplayerToggle.GetComponent<RectTransform>());
#endif

    AlignSettingsMenu(
      worldSettings.GetComponent<RectTransform>(),
      windowHeaderUI.worldVisualsToggle.GetComponent<RectTransform>());
  }

  // Puts the menu under the icon.
  private void AlignSettingsMenu(RectTransform menu, RectTransform icon)
  {
    // Get screen position of icon
    icon.GetWorldCorners(tempWorldCorners);
    Vector2 screen = tempWorldCorners[0];
    windowHeaderUI.leftMenuRight.GetWorldCorners(tempWorldCorners);

    // Set y position to the bottom of the header / overflow menu
    if (icon.IsChildOf(windowHeaderUI.leftMenuOverflow))
    {
      windowHeaderUI.leftMenuOverflow.GetWorldCorners(tempWorldCorners);
    }
    else
    {
      // Clamp x to the width of the left menu since we don't want the menu covering the build button
      float leftMenuXMax = tempWorldCorners[2].x;
      menu.GetWorldCorners(tempWorldCorners);
      float menuWidth = tempWorldCorners[2].x - tempWorldCorners[0].x;
      screen.x = Mathf.Min(screen.x, leftMenuXMax - menuWidth);

      windowHeaderUI.leftMenuLeft.GetWorldCorners(tempWorldCorners);
    }
    Vector2 screenY = RectTransformUtility.WorldToScreenPoint(null, tempWorldCorners[0]);
    screen.y = screenY.y;

    // Move menu to screen point
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      userMain.getEditRect(), screen, null, out Vector2 localPoint);
    menu.anchoredPosition =
      localPoint + new Vector2(userMain.getEditRect().rect.width / 2, -userMain.getEditRect().rect.height / 2);
  }
}
