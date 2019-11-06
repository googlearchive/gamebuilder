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

public class GradientStopUI : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] RectTransform rectTransform;
  [SerializeField] UnityEngine.UI.Image stopImage;
  [SerializeField] Color hoverColor;
  [SerializeField] Color selectedColor;
  [SerializeField] Color removingColor;
  public event System.Action onClick;
  public event System.Action onDragToPosition;
  private string id;
  private float position;
  private RectTransform parentContainer;
  private bool isSelected;
  private bool hasMouseOver;
  private bool inRemovingRange;
  private Color defaultColor;

  public event System.Action<float> onDragged;
  public event System.Action onRemoveGesture;

  const float DRAG_REMOVE_THRESHOLD = 100f;

  public void Setup()
  {
    parentContainer = transform.parent.GetComponentInParent<RectTransform>();
    defaultColor = stopImage.color;
  }

  public void SetId(string id)
  {
    this.id = id;
  }

  public string GetId()
  {
    return id;
  }

  public void SetSelected(bool selected)
  {
    this.isSelected = selected;
    UpdateUI();
  }

  public void SetPosition(float position)
  {
    this.position = position;
    float rectX = position * parentContainer.rect.width;
    MoveArrowTo(rectX);
  }

  public float GetPosition()
  {
    return position;
  }

  public void Destroy()
  {
    Destroy(gameObject);
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      parentContainer, Input.mousePosition, null, out localPos);
    float x = Mathf.Clamp(localPos.x + parentContainer.rect.width / 2, 0, parentContainer.rect.width);
    MoveArrowTo(x);
    inRemovingRange = Mathf.Abs(localPos.y) >= DRAG_REMOVE_THRESHOLD;
    UpdateUI();
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (eventData.dragging)
    {
      HandleDragEnd(eventData);
    }
    else
    {
      HandleClick(eventData);
    }
    inRemovingRange = false;
    UpdateUI();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    this.hasMouseOver = true;
    UpdateUI();
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    this.hasMouseOver = false;
    UpdateUI();
  }

  private void HandleClick(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Left)
    {
      onClick?.Invoke();
    }
    else if (eventData.button == PointerEventData.InputButton.Right)
    {
      onRemoveGesture?.Invoke();
    }
  }

  private void HandleDragEnd(PointerEventData eventData)
  {
    Vector2 rectPosition = rectTransform.anchoredPosition;

    MoveArrowTo(position);

    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      parentContainer, Input.mousePosition, null, out localPos);
    if (Mathf.Abs(localPos.y) >= DRAG_REMOVE_THRESHOLD)
    {
      onRemoveGesture?.Invoke();
    }
    else
    {
      float frac = rectPosition.x / parentContainer.rect.width;
      onDragged?.Invoke(frac);
    }
  }

  private void MoveArrowTo(float px)
  {
    Vector2 rectPosition = rectTransform.anchoredPosition;
    rectPosition.x = px;
    rectTransform.anchoredPosition = rectPosition;
  }

  private void UpdateUI()
  {
    if (isSelected)
    {
      stopImage.color = selectedColor;
    }
    else if (inRemovingRange)
    {
      stopImage.color = removingColor;
    }
    else if (hasMouseOver)
    {
      stopImage.color = hoverColor;
    }
    else
    {
      stopImage.color = defaultColor;
    }
  }

}
