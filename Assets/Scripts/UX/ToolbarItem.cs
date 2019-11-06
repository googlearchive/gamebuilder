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

public class ToolbarItem : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI itemTextField;
  [SerializeField] UnityEngine.UI.RawImage iconImage;
  // [SerializeField] UnityEngine.UI.LayoutElement layoutElement;
  [SerializeField] TMPro.TextMeshProUGUI numberTextField;

  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform movingRect;
  [SerializeField] UnityEngine.UI.Image primaryBackground;
  [SerializeField] UnityEngine.UI.Image secondaryBackground;
  [SerializeField] RectTransform stripeRect;
  [SerializeField] UnityEngine.UI.Image stripeImage;

  [SerializeField] float stripRectStart = -216;
  [SerializeField] float stripRectEnd = 216;
  float stripSpeed = 10f;

  const float TRANSITION_SPEED = 7f;
  Vector2 openPosition = Vector2.zero;
  Vector2 closePosition = new Vector2(0, -120);

  enum StripeState
  {
    Entering,
    Exiting,
    Entered,
    Exited
  }

  [SerializeField] float travel = 20f;
  protected bool selected = false;

  StripeState stripeState = StripeState.Exited;

  public virtual void SetSelect(bool on)
  {
    selected = on;
    movingRect.anchoredPosition = on ? Vector2.zero : new Vector2(0, travel);
  }

  public void Open(float timeDelay)
  {
    gameObject.SetActive(true);
    if (transitionRoutine != null) StopCoroutine(transitionRoutine);
    transitionRoutine = StartCoroutine(OpenRoutine(timeDelay));
  }

  public void Close(float timeDelay)
  {
    if (transitionRoutine != null) StopCoroutine(transitionRoutine);
    transitionRoutine = StartCoroutine(CloseRoutine(timeDelay));
  }

  Coroutine transitionRoutine;

  IEnumerator OpenRoutine(float timeDelay)
  {

    rectTransform.anchoredPosition = closePosition;
    yield return new WaitForSecondsRealtime(timeDelay);
    float lerpVal = 0;
    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * TRANSITION_SPEED);
      rectTransform.anchoredPosition = Vector2.Lerp(closePosition, openPosition, lerpVal);
      yield return null;
    }
    rectTransform.anchoredPosition = openPosition;
  }

  IEnumerator CloseRoutine(float timeDelay)
  {
    rectTransform.anchoredPosition = openPosition;
    yield return new WaitForSecondsRealtime(timeDelay);
    float lerpVal = 0;
    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * TRANSITION_SPEED);
      rectTransform.anchoredPosition = Vector2.Lerp(openPosition, closePosition, lerpVal);
      yield return null;
    }
    rectTransform.anchoredPosition = closePosition;
    gameObject.SetActive(false);
  }


  public bool IsSelected()
  {
    return selected;
  }

  public void SetNumber(string n)
  {
    numberTextField.text = n;
  }

  public void SetColors(Color primaryColor, Color secondaryColor)
  {
    primaryBackground.color = primaryColor;
    secondaryBackground.color = secondaryColor;
    if (stripeImage != null) stripeImage.color = Color.Lerp(primaryColor, secondaryColor, .35f);
  }

  public void SetTexture(Texture2D s)
  {
    iconImage.texture = s;
    // iconImage.enabled = true;
  }

  void Update()
  {
    if (stripeRect == null) return;

    if (selected)
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
        stripeImage.enabled = true;
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
          stripeImage.enabled = false;
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
          stripeImage.enabled = false;
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

