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

public class ZoomableRect : MonoBehaviour
{
  [SerializeField] RectTransform canvasRect;
  [SerializeField] RectTransform objectRect;
  [SerializeField] RectTransform windowRect;
  [SerializeField] RectTransform referenceRect;
  [SerializeField] RectTransform offsetRect;
  [SerializeField] RectTransform pinningRect;

  public bool pointerEntered;
  bool pointerDown;
  public float currentScale;

  const float INIT_SCALE = .6f;
  // const float MIN_SCALE = .25f;
  const float MAX_SCALE = 1f;

  //you probably want this to exp not linear, but it's a start..
  const float MOD_SCALE = 1.5f;
  const float MOUSE_MOD_SCALE = .1f;


  Vector2 mouseStart;
  Vector2 mouseStartInScreenCoordinates;
  Vector2 zoomOrigin;

  bool isRightMouseButton = false;

  public event System.Action onZoom;
  public event System.Action onPan;

  InputControl inputControl;

  void Start()
  {
    Util.FindIfNotSet(this, ref inputControl);
    SetCanvasScale(INIT_SCALE);
  }

  void InstantOffset(Vector2 offset)
  {
    objectRect.SetParent(offsetRect);
    offsetRect.anchoredPosition = offset;
    objectRect.SetParent(windowRect);
    offsetRect.anchoredPosition = Vector2.zero;
  }

  public void SetAbsolutePosition(Vector2 pos)
  {
    objectRect.anchoredPosition = pos;
  }

  public Vector2 GetAbsolutePosition()
  {
    return objectRect.anchoredPosition;
  }

  public Vector2 GetCenterPosition()
  {
    return referenceRect.anchoredPosition;
  }

  public void OnPointerEnter()
  {
    pointerEntered = true;
  }

  public void OnPointerExit()
  {
    pointerEntered = false;
  }

  public void OnPointerDown()
  {
    //this is a bit of a hack //
    isRightMouseButton = Input.GetMouseButton(1);
    //
    pointerDown = true;
    objectRect.SetParent(offsetRect);
    mouseStart = GetMouseAsRectPos();
    mouseStartInScreenCoordinates = Input.mousePosition;
  }

  public void OnPointerUp()
  {
    pointerDown = false;
    objectRect.SetParent(windowRect);
    offsetRect.anchoredPosition = Vector2.zero;
  }

  public Vector2 GetMouseAsRectPos()
  {
    Vector2 newpos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, Input.mousePosition, null, out newpos);
    return newpos;
  }

  public Vector2 GetStartingMouseAsRectPos()
  {
    Vector2 newpos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, mouseStartInScreenCoordinates, null, out newpos);
    return newpos;
  }


  void Update()
  {
    if (pointerDown)
    {
      if (isRightMouseButton)
      {
        float mod = Input.GetAxis("Mouse X") + Input.GetAxis("Mouse Y");
        UpdateScaleWithMouse(mod * MOUSE_MOD_SCALE);
      }
      else
      {
        Vector2 newPosition = GetMouseAsRectPos() - mouseStart;
        if (offsetRect.anchoredPosition != newPosition)
        {
          onPan?.Invoke();
          offsetRect.anchoredPosition = newPosition;
        }
      }
    }

    if (!pointerEntered) return;

    float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
    if (mouseWheel != 0)
    {
      UpdateScaleWithMouse(mouseWheel * MOD_SCALE);
      onZoom?.Invoke();
    }
  }

  void UpdateScaleWithMouse(float mod)
  {
    Vector2 initpos = isRightMouseButton ? GetStartingMouseAsRectPos() : GetMouseAsRectPos();
    float newScale = currentScale * (1 + Mathf.Clamp(mod, -.5f, .5f));
    SetCanvasScale(newScale);

    Vector2 newpos = isRightMouseButton ? GetStartingMouseAsRectPos() : GetMouseAsRectPos();
    objectRect.anchoredPosition += (newpos - initpos);
  }

  public void SetCanvasScale(float newScale)
  {
    if (currentScale == newScale) return;
    if (newScale > MAX_SCALE) newScale = MAX_SCALE;
    currentScale = newScale;// Mathf.Clamp(newScale, Mathf.NegativeInfinity, MAX_SCALE);
    windowRect.localScale = Vector2.one * currentScale;
  }

  public float GetCanvasScale()
  {
    return currentScale;
  }


  public void FocusOnRectTransform(RectTransform rectTransform)
  {
    float scaleYdelta = pinningRect.sizeDelta.y / rectTransform.sizeDelta.y;
    float scaleXdelta = pinningRect.sizeDelta.x / rectTransform.sizeDelta.x;

    SetCanvasScale(Mathf.Min(scaleXdelta, scaleYdelta));
    bool useYforScale = scaleYdelta < scaleXdelta;

    //finds offset
    Vector2 cornerMin, cornerMax;
    Util.FindRectCornersFromDifferentCanvas(rectTransform, canvasRect, out cornerMin, out cornerMax);
    Vector2 panelCenter = (cornerMin + cornerMax) / 2f;
    if (!useYforScale) panelCenter.x = cornerMax.x;

    Vector2 windowCornerMin, windowCornerMax;
    Util.FindRectCornersFromDifferentCanvas(pinningRect, canvasRect, out windowCornerMin, out windowCornerMax);
    Vector2 windowCenter = (windowCornerMin + windowCornerMax) / 2f;
    if (!useYforScale) windowCenter.x = windowCornerMax.x;

    Vector2 offset = windowCenter - panelCenter;
    offset /= currentScale;
    InstantOffset(offset);
  }



}
