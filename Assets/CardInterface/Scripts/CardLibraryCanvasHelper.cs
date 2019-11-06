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

public class CardLibraryCanvasHelper : MonoBehaviour
{
  public CardLibrary cardLibrary;
  public PanelLibrary panelLibrary;
  public RectTransform focusRect;
  public RectTransform resizingRect;
  public GameObject backgroundObject;

  HudManager hudManager;

  void Awake()
  {
    Util.FindIfNotSet(this, ref hudManager);
  }

  internal void Open()
  {
    gameObject.SetActive(true);
  }

  public bool IsOpen()
  {
    return cardLibrary.IsOpen();
  }

  internal void Close()
  {
    cardLibrary.Close();
    panelLibrary.Close();
    gameObject.SetActive(false);
  }

  // This update loop is a hack to allow easy use of the command console (~) while the card library is open
  void Update()
  {
    if (cardLibrary.IsOpen() && resizingRect.anchorMax.y != hudManager.GetVerticalPercent())
    {
      resizingRect.anchorMax = new Vector2(0, hudManager.GetVerticalPercent());
    }
  }
}
