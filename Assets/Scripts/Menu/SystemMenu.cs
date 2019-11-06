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

public class SystemMenu : MonoBehaviour
{
  UserMain userMain;
  DynamicPopup dynamicPopup;
  GameBuilderSceneController sceneController;
  [SerializeField] SystemMenuUI systemMenuUI;
  [SerializeField] ControlsMenu controlsMenu;
  [SerializeField] GraphicsMenu graphicsMenu;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref dynamicPopup);
    Util.FindIfNotSet(this, ref sceneController);

    systemMenuUI.showTooltipsToggle.onValueChanged.AddListener(userMain.ToggleTooltips);
    controlsMenu.invertMouseLookToggle.onValueChanged.AddListener(userMain.ToggleMouseInvert);
    systemMenuUI.autoPauseToggle.onValueChanged.AddListener(userMain.ToggleAutoPause);
    systemMenuUI.hideAvatarToggle.onValueChanged.AddListener(userMain.SetHideAvatarInTopDown);

    systemMenuUI.sfxSlider.onValueChanged.AddListener(userMain.SetSFXVolume);
    systemMenuUI.musicSlider.onValueChanged.AddListener(userMain.SetMusicVolume);
    controlsMenu.mouseWheelSensitivitySlider.onValueChanged.AddListener(userMain.SetMouseWheelSensitivity);
    controlsMenu.mouseLookSensitivitySlider.onValueChanged.AddListener(userMain.SetMouseSensitivity);

    systemMenuUI.closeButton.onClick.AddListener(Close);
    systemMenuUI.controlsButton.onClick.AddListener(ControlsMenuOpen);
    systemMenuUI.graphicsButton.onClick.AddListener(GraphicsMenuOpen);

    controlsMenu.resetToDefaultAction = ResetControlSettingsToDefaults;
    controlsMenu.onBack = ControlsMenuBack;
    controlsMenu.onClose = ControlsMenuClose;

    graphicsMenu.onBack = GraphicsMenuBack;
    graphicsMenu.onClose = GraphicsMenuClose;

    if (GameBuilderApplication.IsStandaloneExport)
    {
      systemMenuUI.exitButtonText.text = "Quit";
      systemMenuUI.exitToMainMenuButton.onClick.AddListener(() => Application.Quit());
    }
    else
    {
      systemMenuUI.exitToMainMenuButton.onClick.AddListener(() => userMain.ShowExitPopup("Exit to Main Menu?", "Exit", () => sceneController.LoadSplashScreen()));
    }
  }

  public GraphicsMenu GetGraphicsMenu()
  {
    return graphicsMenu;
  }

  void ResetControlSettingsToDefaults()
  {
    userMain.ResetMouseWheelSensitivity();
    userMain.SetMouseSensitivity(UserMain.DEFAULT_MOUSE_SENSITIVITY);
  }

  void ControlsMenuClose()
  {
    controlsMenu.Close();
    Close();
  }

  void ControlsMenuBack()
  {
    controlsMenu.Close();
  }

  void ControlsMenuOpen()
  {
    controlsMenu.Open();
  }

  void GraphicsMenuClose()
  {
    graphicsMenu.Close();
    Close();
  }

  void GraphicsMenuBack()
  {
    graphicsMenu.Close();
  }

  void GraphicsMenuOpen()
  {
    graphicsMenu.Open();
  }


  public void SetOpen(bool on)
  {
    if (on) Open();
    else Close();
  }

  public void Open()
  {
    gameObject.SetActive(true);
    Update();
  }

  public void Close()
  {
    gameObject.SetActive(false);
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

  void Update()
  {
    systemMenuUI.autoPauseToggle.gameObject.SetActive(!GameBuilderApplication.IsStandaloneExport);
    systemMenuUI.showTooltipsToggle.isOn = userMain.playerOptions.showTooltips;
    controlsMenu.invertMouseLookToggle.isOn = userMain.playerOptions.invertMouselook;
    systemMenuUI.autoPauseToggle.isOn = userMain.playerOptions.autoPause;
    systemMenuUI.hideAvatarToggle.isOn = userMain.playerOptions.hideAvatarInTopDown;

    systemMenuUI.sfxSlider.value = userMain.playerOptions.sfxVolume;
    systemMenuUI.musicSlider.value = userMain.playerOptions.musicVolume;
    controlsMenu.mouseLookSensitivitySlider.value = userMain.playerOptions.mouseLookSensitivity;

    systemMenuUI.canvasGroup.alpha = controlsMenu.IsOpen() ? 0 : 1;

    // need to convert to 0 to 1
    controlsMenu.mouseWheelSensitivitySlider.value = userMain.GetMouseWheelSensitivity();
  }

  internal void Back()
  {
    if (controlsMenu.IsOpen()) controlsMenu.Close();
    else if (graphicsMenu.IsOpen()) graphicsMenu.Close();
    else Close();
  }
}