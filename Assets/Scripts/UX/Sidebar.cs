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

public abstract class Sidebar : MonoBehaviour
{
  protected SidebarManager sidebarManager;
  [SerializeField] protected RectTransform rectTransform;

  public System.Action OnOpen;

  float DEFAULT_SPEED = 3000;
  public float currentWidth = 0;
  public enum DisplayState
  {
    Entering,
    Exiting,
    Entered,
    Exited
  };

  [SerializeField] DisplayState displayState = DisplayState.Exited;
  [SerializeField] DisplayState desiredDisplayState = DisplayState.Exited;

  //request to display
  public virtual void Open()
  {
    gameObject.SetActive(true);
    displayState = DisplayState.Entering;
    desiredDisplayState = DisplayState.Entered;
  }

  public virtual void Setup(SidebarManager _sidebarManager)
  {
    sidebarManager = _sidebarManager;
  }

  // Err is this speed?
  public float GetSpeed()
  {
    return DEFAULT_SPEED * Time.unscaledDeltaTime;
  }

  public float GetTargetWidth()
  {
    return rectTransform.sizeDelta.x;
  }

  //request to hide
  public virtual void Close()
  {
    displayState = DisplayState.Exiting;
    desiredDisplayState = DisplayState.Exited;
  }

  public RectTransform GetRectTransform()
  {
    return rectTransform;
  }

  public void MoveTowards(float targetX)
  {
    if (rectTransform.anchoredPosition.x == targetX)
    {
      if (desiredDisplayState == DisplayState.Exited)
      {
        OnExited();
      }
      return;
    }

    Vector2 curPosition = rectTransform.anchoredPosition;
    curPosition.x = targetX;
    if (desiredDisplayState == DisplayState.Exited)
    {
      OnExited();
    }

    rectTransform.anchoredPosition = curPosition;


    /*  Vector2 curPosition = rectTransform.anchoredPosition;
     float delta = targetX - curPosition.x;
     if (Mathf.Abs(delta) < GetSpeed())
     {
       curPosition.x = targetX;
       if (desiredDisplayState == DisplayState.Exited)
       {
         OnExited();
       }
     }
     else
     {
       curPosition.x += Mathf.Sign(delta) * GetSpeed();
     }
     rectTransform.anchoredPosition = curPosition; */
  }


  public bool IsOpenedOrOpening()
  {
    return displayState == DisplayState.Entered || displayState == DisplayState.Entering;
  }

  public bool IsVisible()
  {
    return displayState != DisplayState.Exited;
  }


  public void OnExited()
  {
    displayState = DisplayState.Exited;
    gameObject.SetActive(false);
  }



  public virtual void RequestOpen()
  {
    if (!IsOpenedOrOpening()) OnOpen?.Invoke();
    sidebarManager.RequestOpen(this);
  }

  public virtual void RequestClose()
  {
    sidebarManager.RequestClose(this);
  }

}
