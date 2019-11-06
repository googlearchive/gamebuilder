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
public class ThreeWayPopup : MonoBehaviour, IPopup
{
  [SerializeField] UnityEngine.UI.Button saveOverwriteButton;
  [SerializeField] UnityEngine.UI.Button saveNewButton;
  [SerializeField] UnityEngine.UI.Button cancelButton;
  public Action saveOverwriteEvent;
  public Action saveNewEvent;
  public Action cancelEvent;

  void Awake()
  {
    saveOverwriteButton.onClick.AddListener(() => saveOverwriteEvent?.Invoke());
    saveNewButton.onClick.AddListener(() => saveNewEvent?.Invoke());
    cancelButton.onClick.AddListener(() => cancelEvent?.Invoke());
  }

  public void Activate()
  {
    gameObject.SetActive(true);
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

