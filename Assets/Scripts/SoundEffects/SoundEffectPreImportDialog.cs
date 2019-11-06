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
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SoundEffectPreImportDialog : MonoBehaviour
{
  [SerializeField] Button okButton;
  [SerializeField] Button cancelButton;
  [SerializeField] Button closeButton;
  [SerializeField] TMPro.TMP_InputField soundNameField;

  public delegate void OnClosed(bool proceed, string soundName);
  OnClosed callback;

  public void Setup()
  {
    gameObject.SetActive(false);
    okButton.onClick.AddListener(OnOkClicked);
    cancelButton.onClick.AddListener(OnCancelClicked);
    closeButton.onClick.AddListener(OnCancelClicked);
  }

  public void Open(string initialSoundName, OnClosed callback)
  {
    this.callback = callback;
    soundNameField.text = initialSoundName;
    gameObject.SetActive(true);
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  private void OnOkClicked()
  {
    Close();
    callback?.Invoke(true, soundNameField.text);
  }

  private void OnCancelClicked()
  {
    Close();
    callback?.Invoke(false, null);
  }
}
