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

public class RotationWheel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{

  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform rotationLine;
  private CursorManager cursorManager;
  public delegate void RotationChanged(float rotation);
  public event RotationChanged OnRotationChanged;
  private float rotation;
  private bool isMouseOver = false;
  private bool isDragging = false;

  public void Awake()
  {
    Util.FindIfNotSet(this, ref cursorManager);
  }

  public void OnClose()
  {
    isMouseOver = false;
    isDragging = false;
  }

  public void SetRotation(float rotation)
  {
    if (isDragging)
    {
      return;
    }
    this.rotation = rotation;
    rotationLine.rotation = Quaternion.Euler(0, 0, -rotation);
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    isMouseOver = true;
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    isMouseOver = false;
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    isDragging = true;
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    isDragging = false;
    OnRotationChanged?.Invoke(rotation);
  }

  void Update()
  {
    if (isDragging)
    {
      Vector2 mousepos = Input.mousePosition;
      Vector2 dragPosition;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousepos, null, out dragPosition);
      float angle = Vector2.SignedAngle(dragPosition, Vector2.up);
      rotation = (angle % 360 + 360) % 360;
      rotationLine.rotation = Quaternion.Euler(0, 0, -rotation);
    }
  }
}
