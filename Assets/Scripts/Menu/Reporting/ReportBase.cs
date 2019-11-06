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

public class ReportBase : MonoBehaviour
{
  [SerializeField] protected GameObject backPanel;
  [SerializeField] protected GameObject mainPanel;
  [SerializeField] protected UnityEngine.UI.Button closeButton;
  protected PhotonPlayer reportee;

  protected virtual void Awake()
  {
    closeButton.onClick.AddListener(Close);
  }

  public bool IsOpen()
  {
    return mainPanel.activeSelf;
  }

  public virtual void Open(PhotonPlayer reportee, bool kicking = false)
  {
    this.reportee = reportee;
    backPanel.SetActive(true);
    mainPanel.SetActive(true);
  }

  public void Close()
  {
    backPanel.SetActive(false);
    mainPanel.SetActive(false);
  }
}
