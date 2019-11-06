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
using UnityEngine.EventSystems;

public class AddBehaviorButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
  [SerializeField] TMPro.TextMeshProUGUI textField;
  [SerializeField] UnityEngine.UI.Image[] imageFields;

  [SerializeField] Color baseColor;
  [SerializeField] Color mouseoverColor;
  [SerializeField] Color clickColor;

  public System.Action OnClick;

  bool mouseover = false;
  bool clicked = false;

  string cardType = "";

  void Awake()
  {
    SetColor(baseColor);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    OnClick?.Invoke();
    StopAllCoroutines();
    StartCoroutine(ClickRoutine());
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    mouseover = true;
    RefreshColor();

  }

  public void OnPointerExit(PointerEventData eventData)
  {
    mouseover = false;
    RefreshColor();
  }

  IEnumerator ClickRoutine()
  {
    clicked = true;
    RefreshColor();
    yield return new WaitForSeconds(0.2f);
    clicked = false;
    RefreshColor();
  }

  void SetColor(Color color)
  {
    textField.color = color;
    foreach (UnityEngine.UI.Image image in imageFields)
    {
      image.color = color;
    }
    textField.text = $"Add {cardType} card";
  }

  public void SetBaseColor(Color color)
  {
    baseColor = color;
    RefreshColor();
  }

  void RefreshColor()
  {
    if (clicked)
    {
      SetColor(clickColor);
    }
    else
    {
      if (mouseover)
      {
        SetColor(mouseoverColor);
      }
      else
      {
        SetColor(baseColor);
      }
    }
  }

  internal void SetCardCategory(string cardType)
  {
    this.cardType = cardType;
    RefreshColor();
  }
}
