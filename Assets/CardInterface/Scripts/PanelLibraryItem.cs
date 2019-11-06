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

public class PanelLibraryItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
  [SerializeField] UnityEngine.UI.Image image;
  [SerializeField] UnityEngine.UI.Image backgroundImage;
  [SerializeField] TMPro.TextMeshProUGUI titleField;
  [SerializeField] TMPro.TextMeshProUGUI descriptionField;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform displayRectTransform;

  bool dragging = false;

  // This only includes things necessary for seeing the panel in the library.
  public interface IModel : CardPanel.IPanelCommon
  {
    CardPanel.IAssignedPanel Assign();
  }

  public PanelLibraryItem.IModel model;

  PanelLibrary panelLibrary;

  const float FOCUS_SCALE = 1.05f;

  void Awake()
  {
    Util.FindIfNotSet(this, ref panelLibrary);
  }

  public void Setup(IModel model)
  {
    this.model = model;
    image.sprite = model.GetIcon();
    titleField.text = model.GetTitle();
    descriptionField.text = model.GetDescription();

    backgroundImage.color = model.GetColor();
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    panelLibrary.OnBeginDrag(this);
    displayRectTransform.SetParent(panelLibrary.focusPanelParent, true);
    dragging = true;
  }

  //have to include these for begin drag to trigger
  public void OnDrag(PointerEventData eventData)
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelLibrary.focusPanelParent, Input.mousePosition, null, out localPos);
    displayRectTransform.anchoredPosition = localPos;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    if (!dragging) return;
    dragging = false;
    panelLibrary.OnEndDrag(this);
    displayRectTransform.SetParent(rectTransform, true);
    displayRectTransform.anchoredPosition = Vector2.zero;
  }

  public void ForceEndDrag()
  {
    dragging = false;
    displayRectTransform.SetParent(rectTransform, true);
    displayRectTransform.anchoredPosition = Vector2.zero;
  }


  public void OnPointerEnter(PointerEventData eventData)
  {
    // panelLibrary.OnPointerEnterOnItem(this);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    // panelLibrary.OnPointerExitOnItem(this);
  }


  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(displayRectTransform, Input.mousePosition);
  }

  public void DragUpdate()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panelLibrary.focusPanelParent, Input.mousePosition, null, out localPos);
    displayRectTransform.anchoredPosition = localPos;
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (!eventData.dragging)
    {
      panelLibrary.OnClick(this);
    }
  }
}
