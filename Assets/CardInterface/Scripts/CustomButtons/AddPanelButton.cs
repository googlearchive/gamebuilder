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
using UnityEngine.EventSystems;

public class AddPanelButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] UnityEngine.UI.Image icon;
  [SerializeField] TMPro.TextMeshProUGUI text;

  [SerializeField] Color highContrast;
  [SerializeField] Color mediumContrast;
  [SerializeField] Color lowContrast;

  public System.Action OnClick;

  public bool mouseOver = false;
  bool clicked = false;

  const float CLICK_FEEDBACK_DURATION = 0.1f;

  void Awake()
  {
    RefreshColors();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    mouseOver = true;
    RefreshColors();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    mouseOver = false;
    RefreshColors();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    OnClick?.Invoke();
    if (clickRoutine != null) StopCoroutine(clickRoutine);
    clickRoutine = StartCoroutine(ClickRoutine());
  }

  Coroutine clickRoutine;
  IEnumerator ClickRoutine()
  {
    clicked = true;
    RefreshColors();
    yield return new WaitForSeconds(CLICK_FEEDBACK_DURATION);
    clicked = false;
    RefreshColors();
  }

  void RefreshColors()
  {
    if (clicked)
    {
      icon.color = highContrast;
      text.color = highContrast;
    }
    else
    {
      icon.color = mouseOver ? mediumContrast : lowContrast;
      text.color = mouseOver ? mediumContrast : lowContrast;
    }
  }
}
