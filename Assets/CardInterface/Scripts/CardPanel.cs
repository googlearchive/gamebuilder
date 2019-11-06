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

public class CardPanel : MonoBehaviour
{
  public interface IPanelCommon
  {
    Sprite GetIcon();
    string GetTitle();
    string GetDescription();
    Color GetColor();
  }

  // This is assigned to an actor, so it has additional methods.
  public interface IAssignedPanel : IPanelCommon
  {
    IEnumerable<IDeckModel> GetDecks();
    AssignedBehavior GetBehavior();
    IAssignedPanel Duplicate();
    void Remove();
    string GetId();
    bool IsFake();

    PanelUse GetUse();

    // Pass null to undoLabel to avoid creating an undo step, ie. for changes that were not user-initiated.
    void SetUse(PanelUse data, string undoLabel);
  }


  [SerializeField] CardPanelUI cardPanelUI;
  [SerializeField] GameObject cardSlotPrefab;
  [SerializeField] GameObject propertyFieldsPrefab;
  [SerializeField] RectTransform referencRect;
  UnityEngine.UI.LayoutElement propsLayoutElement;

  public RectTransform rectTransform;
  public RectTransform parentRect;
  [SerializeField] DynamicPopup popups;
  [SerializeField] CanvasGroup canvasGroup;

  BehaviorUX.CardPropertiesUX cardPropertiesUX;

  CardManager cardManager;
  PanelManager panelManager;

  const float MIN_HEADER_WIDTH = 160;
  const float COLOR_SATURATION = .67f;
  const float BRIGHT_VALUE = .76f;
  const float DIM_VALUE = .36f;

  IAssignedPanel model;
  string customLabel;

  Dictionary<string, GameObject> cardSlotForProperty = new Dictionary<string, GameObject>();

  string initialTitle = "";

  public RectTransform GetReferenceRectTransform()
  {
    return referencRect;
  }


  public void Setup(IAssignedPanel model, CardManager cardManager, PanelManager panelManager, DynamicPopup popups)
  {
    this.model = model;
    this.cardManager = cardManager;
    this.panelManager = panelManager;
    this.popups = popups;

    cardPanelUI.headerIcon.sprite = model.GetIcon();
    initialTitle = model.GetTitle();
    cardPanelUI.SetHeaderText(model.GetTitle());
    cardPanelUI.closeButton.onClick.AddListener(DeletePanel);

    cardPanelUI.headerTextInput.onEndEdit.AddListener(OnEditTitle);

    foreach (var slot in model.GetDecks())
    {
      GameObject newSlotObj = Instantiate(cardSlotPrefab, cardPanelUI.deckParent);
      CardDeck newslot = newSlotObj.GetComponentInChildren<CardDeck>();
      newslot.Setup(slot);
      newslot.SetColor(model.GetColor());
      string propName = slot.GetPropertyName();
      if (propName != null)
      {
        cardSlotForProperty[propName] = newSlotObj;
      }
    }

    cardPanelUI.headerBackground.color = model.GetColor();

    GameObject propsObject = Instantiate(propertyFieldsPrefab, cardPanelUI.fieldsParent);
    propsObject.transform.SetAsFirstSibling();
    propsLayoutElement = cardPanelUI.fieldsParent.GetComponent<UnityEngine.UI.LayoutElement>();
    cardPropertiesUX = propsObject.GetComponent<BehaviorUX.CardPropertiesUX>();

    cardPropertiesUX.Setup(model.GetBehavior()?.GetProperties());
    cardPropertiesUX.onValueChanged += (type) =>
    {
      UpdateSlotsVisibility();
    };
    cardPropertiesUX.gameObject.SetActive(cardPropertiesUX.HasAnyProps());

    parentRect = transform.parent.GetComponent<RectTransform>();

    // I don't like this.
    if (GetModel().GetId() == BehaviorCards.GetMiscPanelId())
    {
      cardPanelUI.closeButton.gameObject.SetActive(false);
    }

    UpdateSlotsVisibility();
  }

  private void UpdateSlotsVisibility()
  {
    foreach (KeyValuePair<string, GameObject> pair in cardSlotForProperty)
    {
      PropEditor editor = model.GetBehavior().GetPropEditorByName(pair.Key);
      if (editor != null)
      {
        pair.Value.SetActive(cardPropertiesUX.ShouldShowField(editor));
      }
    }
  }

  public void HeaderClick()
  {
    panelManager.ZoomToSpecificPanel(this);
  }

  public Vector3[] GetLocalCorners()
  {
    Vector3[] vectors = new Vector3[4];
    rectTransform.GetLocalCorners(vectors);
    return vectors;
  }

  public float GetWidth()
  {
    return UnityEngine.UI.LayoutUtility.GetPreferredWidth(rectTransform);
  }

  public Vector2 GetPosition()
  {
    return rectTransform.anchoredPosition;
  }

  internal void SetPosition(Vector2 newPos)
  {
    rectTransform.anchoredPosition = newPos;
  }

  void Update()
  {
    //   UpdateReferenceRect();
    UpdatePropsRect();

  }

  private void UpdatePropsRect()
  {
    if (!propsLayoutElement.gameObject.activeSelf) return;
    propsLayoutElement.preferredHeight = Mathf.Max(100, cardPropertiesUX.fieldParent.rect.height);
  }

  const float REF_RECT_X_PADDING = 80;
  const float REF_RECT_Y_PADDING = 80;
  const float REF_RECT_HEADER_PADDING = 150;
  private void UpdateReferenceRect()
  {
    referencRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.sizeDelta.x + REF_RECT_X_PADDING);
    referencRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.sizeDelta.y + REF_RECT_HEADER_PADDING + REF_RECT_Y_PADDING);
    referencRect.anchoredPosition = new Vector2(0, -REF_RECT_Y_PADDING / 2f);
  }

  public bool IsMouseOver()
  {
    if (rectTransform == null) return false;
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
  }

  public void DeletePanel()
  {
    // TEMP
    // if (Input.GetKey(KeyCode.LeftShift))
    // {
    //   this.model.Duplicate();
    //   return;
    // }
    Destroy(gameObject);
    this.model.Remove();
    this.model = null;
  }

  public IAssignedPanel GetModel()
  {
    return model;
  }

  public void SetDeleteFeedback(bool on)
  {
    canvasGroup.alpha = on ? .5f : 1;
  }

  internal bool TrySetupFromMetadata()
  {
    // Util.Log($"Panel {model.GetTitle()} has meta? {HasMetadata()}");
    if (HasMetadata())
    {
      PanelUse metadata = GetUseMetadata();
      SetPosition(metadata.position);
      if (metadata.customLabel != null && metadata.customLabel != "")
      {
        cardPanelUI.SetHeaderText(metadata.customLabel);
      }
      return true;
    }
    else
    {
      return false;
    }
  }

  public void OnBeginDrag()
  {
    panelManager.OnPanelBeginDrag(this);
  }

  public void OnEndDrag()
  {
    panelManager.OnPanelEndDrag(this);
  }

  public void OnRequestCopy()
  {
    if (model.GetId() == BehaviorCards.GetMiscPanelId()) return;
    panelManager.OnRequestCopyPanel(this);
  }

  [System.Serializable]
  public struct PanelUse
  {
    static int CurrentVersion = 1;
    public int version;

    public Vector2 position;
    public string customLabel;

    public PanelUse(Vector2 position, string customLabel)
    {
      this.version = CurrentVersion;
      this.position = position;
      this.customLabel = customLabel;
    }
  }

  bool HasMetadata()
  {
    return model.GetUse().version > 0;
  }

  PanelUse GetUseMetadata()
  {
    return model.GetUse();
  }

  internal void SetUseMetadata(string undoLabel)
  {
    model.SetUse(new PanelUse(GetPosition(), customLabel), undoLabel);
  }


  void OnEditTitle(string newtitle)
  {
    if (newtitle == "")
    {
      cardPanelUI.SetHeaderText(initialTitle);
    }
    else
    {
      customLabel = newtitle;
    }
    SetUseMetadata("Edit panel label");
  }
}
