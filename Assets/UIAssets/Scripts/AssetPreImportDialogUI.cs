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

public class AssetPreImportDialogUI : MonoBehaviour
{
  [SerializeField] Button okButton;
  [SerializeField] Button closeButton;
  [SerializeField] TMPro.TMP_InputField nameField;

  public delegate void OnClosed(bool proceed, string soundName);
  OnClosed callback;

  public void Setup()
  {
    gameObject.SetActive(false);
    okButton.onClick.AddListener(OnOkClicked);
    closeButton.onClick.AddListener(OnCancelClicked);
  }

  public void Open(string initialName, OnClosed callback)
  {
    this.callback = callback;
    nameField.text = initialName;
    gameObject.SetActive(true);
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  private void OnOkClicked()
  {
    Close();
    callback?.Invoke(true, nameField.text);
  }

  private void OnCancelClicked()
  {
    Close();
    callback?.Invoke(false, null);
  }
}
