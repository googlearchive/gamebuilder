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

public class CodeErrorNotification : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI errorMessage;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Button ignoreButton;
  [SerializeField] UnityEngine.UI.Button seeCodeButton;

  VoosEngine engine;
  EditMain editMain;
  UserMain userMain;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref userMain);

    engine.OnResetGame += Close;
    closeButton.onClick.AddListener(Close);
    ignoreButton.onClick.AddListener(Close);
  }

  public void Display(string errorMessageString, string uri, VoosEngine.BehaviorLogItem item)
  {
    errorMessage.text = errorMessageString;
    seeCodeButton.onClick.RemoveAllListeners();
    seeCodeButton.onClick.AddListener(() => userMain.ShowCodeEditor(uri, item));
    gameObject.SetActive(true);
  }

  void Update()
  {
    if (editMain.IsCodeViewOpen()) Close();
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }
}
