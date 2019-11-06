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
using UnityEngine;
using System;
public class TextPopup : MonoBehaviour, IPopup
{
  [SerializeField] TMPro.TMP_InputField inputField;
  [SerializeField] UnityEngine.UI.Button confirmButton;
  [SerializeField] UnityEngine.UI.Button cancelButton;
  public Action<string> confirmWithInputEvent;
  public Action cancelEvent;

  void Awake()
  {
    confirmButton.onClick.AddListener(() => confirmWithInputEvent?.Invoke(inputField.text));
    cancelButton.onClick.AddListener(() => cancelEvent?.Invoke());
    inputField.onEndEdit.AddListener(OnInputFieldEnd);
  }

  public void SetInputFieldText(string newtext)
  {
    inputField.text = newtext;
  }

  void OnInputFieldEnd(string currentText)
  {
    // bool isNewsearch = false;
    if (Input.GetButtonDown("Submit"))
    {
      if (currentText != "")
      {
        confirmWithInputEvent?.Invoke(currentText);
        // isNewsearch = true;
      }
    }

    // if (isNewsearch) sidebarManager.OnSubmitSoundEffect();
    // else sidebarManager.OnCancelSoundEffect();
  }

  public void Activate()
  {
    gameObject.SetActive(true);
    inputField.Select();
  }

  public bool IsActive()
  {
    return gameObject.activeSelf;
  }

  public void Deactivate()
  {
    gameObject.SetActive(false);
  }
}
