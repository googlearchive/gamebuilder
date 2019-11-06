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
using System.Linq;

public class ControlsMenu : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Button backButton;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Button resetToDefaults;
  public UnityEngine.UI.Toggle invertMouseLookToggle;
  public UnityEngine.UI.Slider mouseLookSensitivitySlider;
  public UnityEngine.UI.Slider mouseWheelSensitivitySlider;

  public System.Action onBack;
  public System.Action onClose;
  public System.Action resetToDefaultAction;

  void Awake()
  {
    backButton.onClick.AddListener(() => onBack?.Invoke());
    closeButton.onClick.AddListener(() => onClose?.Invoke());
    resetToDefaults.onClick.AddListener(ResetToDefaults);
  }

  private void ResetToDefaults()
  {
    resetToDefaultAction?.Invoke();
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
}
