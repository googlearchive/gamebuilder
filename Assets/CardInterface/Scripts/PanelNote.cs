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

public class PanelNote : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
  public RectTransform rectTransform;
  [SerializeField] RectTransform parentRect;
  [SerializeField] TMPro.TMP_InputField inputField;
  [SerializeField] GameObject outlineObject;
  [SerializeField] GameObject cornerObject;
  [SerializeField] GameObject deleteObject;

  Vector2 mouseOffset = Vector2.zero;
  bool clickOnPointerUp;
  CardManager cardManager;

  bool pointerOverMainRect = false;
  bool pointerOverCornerRect = false;

  public bool isBeingDragged = false;
  bool isCornerBeingDragged = false;

  // Needed for undo magic
  public event System.Action onUserChangedData;

  [System.Serializable]
  public struct Data
  {
    public Vector2 position;
    public Vector2 size;
    public string content;
  }

  Data? prevData;

  void Awake()
  {
    Util.FindIfNotSet(this, ref cardManager);
    parentRect = rectTransform.GetComponentInParent<RectTransform>();
  }

  public void SetParentRect(RectTransform parentRect)
  {
    this.parentRect = parentRect;
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    clickOnPointerUp = false;
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localPos);
    mouseOffset = rectTransform.anchoredPosition - localPos;
    isBeingDragged = true;
    // cardPanel.OnBeginDrag();
  }

  public void OnDrag(PointerEventData eventData)
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localPos);
    rectTransform.anchoredPosition = localPos + mouseOffset;
  }

  public void EditText()
  {
    inputField.ActivateInputField();
  }

  public Data GetData()
  {
    return new Data()
    {
      position = rectTransform.anchoredPosition,
      size = rectTransform.rect.size,
      content = inputField.text
    };
  }

  public void SetData(Data data)
  {
    // Util.Log($"note {metadata.content} as pos {metadata.position}");
    rectTransform.anchoredPosition = data.position;
    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, data.size.x);
    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, data.size.y);
    inputField.text = data.content;
  }

  public void OnEndDrag(PointerEventData eventData)
  {
    isBeingDragged = false;
    if (IsOverTrash())
    {
      cardManager.RemovePanelNoteFromList(this);
      DeleteNote();
    }
  }

  bool IsOverTrash()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(cardManager.trash, Input.mousePosition);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    clickOnPointerUp = true;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    pointerOverMainRect = true;
    outlineObject.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    pointerOverMainRect = false;

  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (clickOnPointerUp)
    {
      clickOnPointerUp = false;
      EditText();
    }
  }

  void CheckDataChangedByUser()
  {
    Data currData = GetData();
    if (prevData != null)
    {
      if (!prevData.Value.Equals(currData))
      {
        onUserChangedData?.Invoke();
      }
    }
    prevData = currData;
  }

  void Update()
  {
    if (isBeingDragged)
    {
      deleteObject.SetActive(IsOverTrash());
    }

    if (inputField.isFocused)
    {
      PreserveMinimumSize();
    }

    bool showPointerFeedback = pointerOverMainRect || pointerOverCornerRect || isCornerBeingDragged || isBeingDragged;

    outlineObject.SetActive(showPointerFeedback);
    cornerObject.SetActive(showPointerFeedback);

    CheckDataChangedByUser();
  }

  public bool IsMouseOver()
  {
    if (rectTransform == null) return false;
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
  }

  public void DeleteNote()
  {
    Destroy(rectTransform.gameObject);
  }

  Vector2 cornerDragOrigin;
  Vector2 cornerDragStartOffsetMax;
  Vector2 cornerDragStartOffsetMin;
  Vector2 minSize = new Vector2(50, 50);

  public void OnPointerEnterCorner()
  {
    pointerOverCornerRect = true;
  }

  public void OnPointerExitCorner()
  {
    pointerOverCornerRect = false;
  }

  public void OnBeginDragCorner()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localPos);
    cornerDragOrigin = localPos;

    cornerDragStartOffsetMax = rectTransform.offsetMax;
    cornerDragStartOffsetMin = rectTransform.offsetMin;
    isCornerBeingDragged = true;
  }

  public void OnDragCorner()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localPos);
    Vector2 delta = localPos - cornerDragOrigin;
    rectTransform.offsetMin = cornerDragStartOffsetMin + new Vector2(0, delta.y);
    rectTransform.offsetMax = cornerDragStartOffsetMax + new Vector2(delta.x, 0);
    PreserveMinimumSize();
  }

  public void OnEndDragCorner()
  {
    isCornerBeingDragged = false;
  }

  public void FocusTextInput()
  {
    inputField.ActivateInputField();
  }

  Vector2 minSizeThreshold = new Vector2(50, 50);
  Vector2 margin = new Vector2(50, 50);
  void PreserveMinimumSize()
  {
    minSize.y = Vector2.Max(inputField.textComponent.GetPreferredValues() + margin, minSizeThreshold).y;


    if (rectTransform.rect.width < minSize.x)
    {
      rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, minSize.x);
    }
    if (rectTransform.rect.height < minSize.y)
    {
      rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, minSize.y);
    }
  }
}