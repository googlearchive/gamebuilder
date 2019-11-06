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

public class BuildPlayToggleButton : MonoBehaviour
{
  [SerializeField] Color buildColorOn;
  [SerializeField] Color buildColorOff;
  // [SerializeField] Color buildColorOffMouseover;
  [SerializeField] Color playColorOff;
  [SerializeField] Color playColorOn;
  // [SerializeField] Color playColorOffMouseover;
  [SerializeField] Color iconOn;
  [SerializeField] Color iconOff;

  [SerializeField] UnityEngine.UI.Button buildButton;
  [SerializeField] UnityEngine.UI.Button playButton;
  [SerializeField] UnityEngine.UI.Image buildBackgroundImage;
  [SerializeField] UnityEngine.UI.Image buildIconImage;
  [SerializeField] TMPro.TextMeshProUGUI buildText;
  [SerializeField] UnityEngine.UI.Image playBackgroundImage;
  [SerializeField] UnityEngine.UI.Image playIconImage;
  [SerializeField] TMPro.TextMeshProUGUI playText;
  [SerializeField] UnityEngine.UI.Image playOutline;
  [SerializeField] UnityEngine.UI.Image buildOutline;
  [SerializeField] GameObject tabHintObject;
  [SerializeField] TMPro.TextMeshProUGUI tabHintText;

  UserMain userMain;

  void Start()
  {
    Util.FindIfNotSet(this, ref userMain);
    buildButton.onClick.AddListener(() =>
    {
      TrySetEditMode(true);
    });
    playButton.onClick.AddListener(() =>
    {
      TrySetEditMode(false);
    });

    buildButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Build Mode (Tab)");
    playButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Play Mode (Tab)");

  }

  void TrySetEditMode(bool on)
  {
    if (on != userMain.InEditMode())
    {
      userMain.SetEditMode(on);
    }
  }

  void Update()
  {
    bool buildMode = userMain.InEditMode();
    playOutline.enabled = !buildMode;
    buildOutline.enabled = buildMode;

    buildBackgroundImage.color = buildMode ? buildColorOn : buildColorOff;
    buildIconImage.color = buildMode ? iconOn : iconOff;
    buildText.color = buildMode ? iconOn : iconOff;

    playBackgroundImage.color = buildMode ? playColorOff : playColorOn;
    playIconImage.color = !buildMode ? iconOn : iconOff;
    playText.color = !buildMode ? iconOn : iconOff;

    if (userMain.InEditMode())
    {
      tabHintObject.SetActive(true);
      tabHintText.text = "[RMB] to move cam";
    }
    else
    {
      bool showTabHint = ShowTabHint();
      tabHintObject.SetActive(showTabHint);
      if (showTabHint)
      {
        tabHintText.text = "Press [ESC] for cursor";
      }
    }
  }

  private bool ShowTabHint()
  {
    if (!userMain.InEditMode())
    {
      return userMain.CameraCapturedCursor();
    }
    else
    {
      return userMain.CameraCapturedCursor();
    }
  }
}
