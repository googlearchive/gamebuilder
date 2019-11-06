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

public class CustomTextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
  [SerializeField] TMPro.TextMeshProUGUI textField;

  public System.Action ClickEvent;

  bool mouseover = false;
  [SerializeField] string textContent = "";

  public void OnPointerDown(PointerEventData eventData)
  {
    ClickEvent?.Invoke();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    mouseover = true;
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    mouseover = false;
  }

  public void SetText(string text)
  {
    textContent = text;
  }

  void OnDisable()
  {
    mouseover = false;
  }

  void Update()
  {
    if (mouseover)
    {
      textField.text = $"<u>{textContent}</u>";
    }
    else
    {
      textField.text = textContent;
    }
  }

}