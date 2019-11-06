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

public class TabButton : MonoBehaviour
{
  [SerializeField] RectTransform tabRect;
  [SerializeField] UnityEngine.UI.Image icon;
  [SerializeField] Color mouseOffIconColor;
  [SerializeField] Color mouseOnIconColor;
  public System.Action onClick;

  bool opened = false;

  const float TAB_VERTICAL_OFFSET = 90;

  public void OnPointerEnter()
  {
    icon.color = mouseOnIconColor;
  }

  public void OnPointerExit()
  {
    icon.color = mouseOffIconColor;
  }

  public void OnClick()
  {
    onClick?.Invoke();
  }

  public void Open()
  {
    tabRect.anchoredPosition = new Vector2(0, TAB_VERTICAL_OFFSET);
    opened = true;
  }

  public void Close()
  {
    tabRect.anchoredPosition = Vector2.zero;
    opened = false;
  }

  public void SetVisible(bool on)
  {
    gameObject.SetActive(on);
  }

  public bool IsOpen()
  {
    return opened;
  }
}
