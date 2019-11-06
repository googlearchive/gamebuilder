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

public class CardPanelDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
  [SerializeField] CardPanel cardPanel;

  Vector2 mouseOffset = Vector2.zero;
  // bool clickOnPointerUp;
  public void OnBeginDrag(PointerEventData eventData)
  {
    Debug.Log("BEGIN DRAG");

    // clickOnPointerUp = false;
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(cardPanel.parentRect, Input.mousePosition, null, out localPos);
    mouseOffset = cardPanel.rectTransform.anchoredPosition - localPos;
    cardPanel.OnBeginDrag();
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(cardPanel.parentRect, Input.mousePosition, null, out localPos);
    cardPanel.rectTransform.anchoredPosition = localPos + mouseOffset;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    Debug.Log("END DRAG");

    cardPanel.OnEndDrag();
    // May have been deleted (dragged into trash)
    if (cardPanel.GetModel() != null)
    {
      cardPanel.SetUseMetadata($"Move {this.cardPanel.GetModel().GetTitle()} panel");
    }
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    // clickOnPointerUp = true;
    Debug.Log("POINTER CLICK");
    if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
    {
      cardPanel.OnRequestCopy();
    }
  }

  /* public void OnPointerDown(PointerEventData eventData)
  {
    // clickOnPointerUp = true;
    Debug.Log("POINTER DOWN");
  }
  
  public void OnPointerEnter(PointerEventData eventData)
  {
    Debug.Log("POINTER ENTER");

    // headerOutline.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    Debug.Log("POINTER EXIT");
    // headerOutline.SetActive(false);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    Debug.Log("POINTER UP");
    // if (clickOnPointerUp)
    // {
    //   clickOnPointerUp = false;
    //   cardPanel.HeaderClick();
    // }
  } */
}
