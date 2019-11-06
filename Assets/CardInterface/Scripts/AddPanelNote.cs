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

public class AddPanelNote : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] CardManager cardManager;
  [SerializeField] RectTransform panelNoteParentRect;
  [SerializeField] GameObject pointerOverObject;
  [SerializeField] RectTransform defaultSpawnPoint;


  bool clickOnPointerUp;
  PanelNote draggedNote;

  public void OnBeginDrag(PointerEventData eventData)
  {
    clickOnPointerUp = false;
    draggedNote = cardManager.CreatePanelNote();

    //place note under cursor
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelNoteParentRect, Input.mousePosition, null, out localPos);
    draggedNote.rectTransform.anchoredPosition = localPos;

    //have to spoof the drage events for this first drag
    draggedNote.OnBeginDrag(eventData);
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (draggedNote != null)
    {
      draggedNote.OnDrag(eventData);
    }
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    if (draggedNote != null)
    {
      draggedNote.OnEndDrag(eventData);
      draggedNote.FocusTextInput();
      draggedNote = null;
    }
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    clickOnPointerUp = true;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    pointerOverObject.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    pointerOverObject.SetActive(false);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (clickOnPointerUp)
    {
      clickOnPointerUp = false;
      PanelNote newNote = cardManager.CreatePanelNote();
      AutoPlaceNote(newNote);
      newNote.FocusTextInput();
    }
  }

  private void AutoPlaceNote(PanelNote newNote)
  {
    //get screenpoint of default rect 
    Vector3[] referenceCorners = new Vector3[4];
    defaultSpawnPoint.GetWorldCorners(referenceCorners);
    Vector2 referenceScreenCornerMin = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[0]);

    //convert screen to relevant rect's space
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelNoteParentRect, referenceScreenCornerMin, null, out localPos);
    newNote.rectTransform.anchoredPosition = localPos;
  }


}
