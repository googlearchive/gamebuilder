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

public class GraphicsMenu : MonoBehaviour
{

  [SerializeField] UnityEngine.UI.Button backButton;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Toggle motionBlurToggle;
  [SerializeField] UnityEngine.UI.Button fullscreenToggleButton;
  [SerializeField] TMPro.TextMeshProUGUI fullscreenToggleButtonText;
  [SerializeField] TMPro.TextMeshProUGUI currentResolution;
  [SerializeField] UnityEngine.UI.Toggle[] qualityToggles;
  [SerializeField] GraphicsResolutionMenuItem resolutionItemTemplate;

  // List<GraphicsResolutionMenuItem> resolutions = new List<GraphicsResolutionMenuItem>();

  public System.Action onBack;
  public System.Action onClose;

  DynamicPopup dynamicPopup;
  GameBuilderStage gbStage;

  bool motionBlur;
  // bool fullscreen;
  const string SET_TO_WINDOWED = "SET TO WINDOW";
  const string SET_TO_FULLSCREEN = "SET TO FULLSCREEN";


  public void Setup()
  {
    Util.FindIfNotSet(this, ref gbStage);
    Util.FindIfNotSet(this, ref dynamicPopup);

    backButton.onClick.AddListener(() => onBack?.Invoke());
    closeButton.onClick.AddListener(() => onClose?.Invoke());

    //full screen
    fullscreenToggleButton.onClick.AddListener(ToggleFullscreen);

    //quality
    for (int i = 0; i < qualityToggles.Length; i++)
    {
      int index = i;
      qualityToggles[i].onValueChanged.AddListener((on) => { if (on) UpdateQualitySetting(index); });
    }

    SetupResolutionList();
  }

  private void UpdateQualitySetting(int index)
  {
    if (QualitySettings.GetQualityLevel() == index) return;
    QualitySettings.SetQualityLevel(index);
    GameBuilderApplication.NotifyQualityLevelChanged();
  }

  private void SetupResolutionList()
  {
    foreach (Resolution resolution in Screen.resolutions)
    {
      GraphicsResolutionMenuItem newItem = Instantiate(resolutionItemTemplate, resolutionItemTemplate.transform.parent);
      newItem.SetResolution(resolution);
      newItem.gameObject.SetActive(true);
      Resolution thisResolution = resolution;
      newItem.toggle.isOn = ResolutionMatch(resolution);
      newItem.toggle.onValueChanged.AddListener((on) => { if (on) { UpdateResolution(thisResolution); } });
    }
  }

  bool ResolutionMatch(Resolution res)
  {
    return res.height == Screen.height &&
    res.width == Screen.width;
  }

  private void UpdateResolution(Resolution resolution)
  {
    Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode, resolution.refreshRate);
    GameBuilderApplication.SaveDisplaySettings();
  }

  void Update()
  {
    // motionBlurToggle.isOn = GetMotionBlur();
    fullscreenToggleButtonText.text = Screen.fullScreen ? SET_TO_WINDOWED : SET_TO_FULLSCREEN;

    // int qualityIndex = QualitySettings.GetQualityLevel();
    qualityToggles[QualitySettings.GetQualityLevel()].isOn = true;

    currentResolution.text = "Current: " + Screen.width + " x " + Screen.height;
  }

  public void Open()
  {
    gameObject.SetActive(true);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  public void ToggleFullscreen()
  {
    if (Screen.fullScreenMode == FullScreenMode.Windowed)
    {
      // Always try for exclusive - it's A LOT faster on laptops.
      Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    }
    else
    {
      Screen.fullScreenMode = FullScreenMode.Windowed;
    }
    GameBuilderApplication.SaveDisplaySettings();
  }

}
