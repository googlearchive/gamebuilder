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
using System;

public class CustomMenuButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
  [SerializeField] UnityEngine.UI.Image buttonImage;
  [SerializeField] RectTransform stripeRect;


  [SerializeField] Color mainColor;
  [SerializeField] Color hoverColor;
  [SerializeField] Color clickColor;
  [SerializeField] Color disabledColor;

  public enum ButtonType
  {
    Trigger,
    Toggle
  }
  [SerializeField] ButtonType buttonType;

  public Action ClickEvent;
  public Action<bool> ToggleEvent;
  public Action<bool> HoverEvent;

  bool toggled = false;
  bool buttondown = false;
  bool hover = false;

  bool enabled = true;

  enum StripeState
  {
    Entering,
    Exiting,
    Entered,
    Exited
  }

  StripeState stripeState = StripeState.Exited;

  float stripRectStart = -340;
  float stripRectEnd = 350;
  float stripSpeed = 20f;

  public void OnPointerDown(PointerEventData eventData)
  {
    if (enabled)
    {
      StartCoroutine(TriggerRoutine());
      ClickEvent?.Invoke();
    }

  }

  public void Enable()
  {
    enabled = true;
    UpdateColors();
  }

  public void Disable()
  {
    enabled = false;
    UpdateColors();
  }

  public void UpdateColors()
  {
    if (!enabled)
    {
      buttonImage.color = disabledColor;
      return;
    }

    Color newcolor = mainColor;
    if (buttondown)
    {
      newcolor = clickColor;
    }
    else
    {
      if (hover)
      {
        newcolor = hoverColor;
      }
    }

    buttonImage.color = newcolor;
  }

  public bool IsToggle()
  {
    return buttonType == ButtonType.Toggle;
  }

  public void Reset()
  {
    StopAllCoroutines();
    SetButtonDown(false);
    UpdateColors();
  }

  void OnDisable()
  {
    hover = false;
    toggled = false;
    buttondown = false;
    UpdateColors();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    hover = true;
    UpdateColors();
    HoverEvent?.Invoke(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    hover = false;
    UpdateColors();
    HoverEvent?.Invoke(false);
  }

  public void SetToggle(bool on)
  {
    if (toggled == on)
    {
      return;
    }

    ToggleEvent?.Invoke(on);

    toggled = on;
  }

  void SetButtonDown(bool on)
  {
    buttondown = on;
    UpdateColors();
  }

  // Use this for initialization
  IEnumerator TriggerRoutine()
  {
    SetButtonDown(true);
    yield return new WaitForSecondsRealtime(.2f);
    SetButtonDown(false);

  }

  void Update()
  {
    if (stripeRect == null) return;

    if (toggled)
    {
      if (stripeState == StripeState.Entered)
      {
        return;
      }
      else if (stripeState == StripeState.Entering)
      {
        Vector2 curPosition = stripeRect.anchoredPosition;
        curPosition.x += stripSpeed;
        if (curPosition.x > 0)
        {
          curPosition.x = 0;
          stripeState = StripeState.Entered;
        }
        stripeRect.anchoredPosition = curPosition;
      }
      else if (stripeState == StripeState.Exiting)
      {
        Vector2 curPosition = stripeRect.anchoredPosition;
        curPosition.x -= stripSpeed;
        if (curPosition.x < 0)
        {
          curPosition.x = 0;
          stripeState = StripeState.Entered;
        }
        stripeRect.anchoredPosition = curPosition;
      }
      else if (stripeState == StripeState.Exited)
      {
        // stripeImage.enabled = true;
        stripeRect.gameObject.SetActive(true);
        Vector2 curPosition = stripeRect.anchoredPosition;
        curPosition.x = stripRectStart;
        stripeState = StripeState.Entering;
        stripeRect.anchoredPosition = curPosition;
      }
    }
    else
    {
      if (stripeState == StripeState.Exited)
      {
        return;
      }
      else if (stripeState == StripeState.Entering)
      {
        Vector2 curPosition = stripeRect.anchoredPosition;
        curPosition.x -= stripSpeed;
        if (curPosition.x < stripRectStart)
        {
          curPosition.x = stripRectStart;
          // stripeImage.enabled = false;
          stripeRect.gameObject.SetActive(false);
          stripeState = StripeState.Exited;
        }
        stripeRect.anchoredPosition = curPosition;
      }
      else if (stripeState == StripeState.Exiting)
      {
        Vector2 curPosition = stripeRect.anchoredPosition;
        // Debug.Log($"{curPosition},{stripSpeed},{stripRectEnd}");
        curPosition.x += stripSpeed;
        if (curPosition.x > stripRectEnd)
        {
          curPosition.x = stripRectEnd;
          // stripeImage.enabled = false;
          stripeRect.gameObject.SetActive(false);

          stripeState = StripeState.Exited;
        }
        stripeRect.anchoredPosition = curPosition;
      }
      else if (stripeState == StripeState.Entered)
      {
        stripeState = StripeState.Exiting;
      }
    }
  }
}