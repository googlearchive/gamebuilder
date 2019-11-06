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

public class MultiplayerWarning : MonoBehaviour
{

  [SerializeField] GameObject container;
  [SerializeField] UnityEngine.UI.Button acceptButton;
  [SerializeField] UnityEngine.UI.Button cancelButton;
  [SerializeField] UnityEngine.UI.Button guidelinesButton;

  public System.Action onAccept;
  public System.Action OnClose;

  void Awake()
  {
    acceptButton.onClick.AddListener(() => onAccept?.Invoke());
    cancelButton.onClick.AddListener(Close);

    guidelinesButton.onClick.AddListener(() =>
    {
      Application.OpenURL("https://support.steampowered.com/kb_article.php?ref=4045-USHJ-3810");
    });
  }

  public void Open(System.Action onContinue)
  {
    onAccept = () =>
    {
      Close();
      onContinue();
    };
    container.SetActive(true);
  }

  public void Close()
  {
    container.SetActive(false);
    OnClose?.Invoke();
  }

  public void SetCloseEvent(System.Action action)
  {
    OnClose = action;
  }
}
