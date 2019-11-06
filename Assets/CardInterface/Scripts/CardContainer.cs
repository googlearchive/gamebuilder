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

public class CardContainer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
{
  public RectTransform rectTransform;
  Card card;
  [SerializeField] RectTransform interiorScaledRect;
  [SerializeField] RectTransform frontRect;
  [SerializeField] AddCardEffect addCardEffectPrefab;
  [SerializeField] GameObject editFeedbackObject;
  [SerializeField] GameObject pulsingOutline;
  [SerializeField] GameObject staticOutline;
  [SerializeField] GameObject selectionOutline;
  [SerializeField] UnityEngine.UI.Button selectionOverlayButton;

  [HideInInspector] public CardDeck deck;

  public bool buttonsOnFocus = true;
  public bool offsetOnFocus = true;
  public bool scaleOnFocus = true;


  const float REPLACE_ALPHA = .7f;
  // const float CARD_WIDTH = 400;
  // const float CARD_HEIGHT = 560;

  bool replaceIndicating = false;
  bool isSelected = false;

  public event System.Action onBeginDrag;
  public event System.Action onDrag;
  public event System.Action onPointerEnter;
  public event System.Action onPointerDown;
  public event System.Action<bool> onPointerUp;

  private System.Action<bool> onSelectionChangedListener;

  void Awake()
  {
    /* if (replaceButton != null)
    {
      replaceButton.onClick.AddListener(() => cardManager.OpenCardLibrary(cardSlot, this));
    } */
    selectionOverlayButton.onClick.AddListener(() =>
    {
      isSelected = !isSelected;
      selectionOutline.SetActive(isSelected);
      onSelectionChangedListener?.Invoke(isSelected);
    });
  }

  public void SetSize(float size)
  {
    rectTransform.localScale = Vector3.one * size;
  }

  public Card GetCard()
  {
    return card;
  }

  public void TriggerAddEffect()
  {
    Instantiate(addCardEffectPrefab, rectTransform).transform.SetAsFirstSibling();
  }

  public void SetCard(Card card)
  {

    this.name = $"CardContainer for {card.name}";
    this.card = card;
    card.container = this;
    card.transform.SetParent(transform);
    card.transform.localScale = Vector3.one;
    card.rectTransform.anchoredPosition = Vector2.zero;

    CreateGhostCopy();

    if (frontRect != null) frontRect.SetAsLastSibling();
    selectionOverlayButton.transform.SetAsLastSibling();
    if (onSelectionChangedListener != null)
    {
      if (isSelected)
      {
        isSelected = false;
        onSelectionChangedListener?.Invoke(isSelected);
      }
      selectionOverlayButton.gameObject.SetActive(false);
    }
  }

  public void RequestDestruction()
  {
    if (onSelectionChangedListener != null && isSelected)
    {
      isSelected = false;
      onSelectionChangedListener?.Invoke(isSelected);
    }
    card.RequestDestroy();
    Destroy(rectTransform.gameObject);
  }

  public void ToggleReplaceIndicator(bool on)
  {
    replaceIndicating = on;
    if (pulsingOutline != null)
    {
      pulsingOutline.SetActive(on);
    }
  }

  public void StartFocus()
  {
    staticOutline.SetActive(true);
  }

  public void EndFocus()
  {
    staticOutline.SetActive(false);
  }

  public void StartSelectionMode(System.Action<bool> onSelectionChangedListener, bool startSelected = false)
  {
    this.onSelectionChangedListener = onSelectionChangedListener;
    selectionOverlayButton.gameObject.SetActive(true);
    if (startSelected)
    {
      isSelected = true;
      selectionOutline.SetActive(isSelected);
      onSelectionChangedListener?.Invoke(isSelected);
    }
  }

  public void EndSelectionMode()
  {
    onSelectionChangedListener = null;
    selectionOutline.gameObject.SetActive(false);
    selectionOverlayButton.gameObject.SetActive(false);
  }

  public Card ReplaceCard(ICardAssignmentModel assignedCard)
  {
    return deck.ReplaceCard(assignedCard, this);
  }

  public Card ReplaceCard(ICardModel unassignedCard)
  {
    return deck.ReplaceCard(unassignedCard, this);
  }

  GameObject ghostCopy;
  void CreateGhostCopy()
  {
    if (ghostCopy != null)
    {
      Destroy(ghostCopy);
    }

    ghostCopy = Instantiate(card.gameObject, transform);
    ghostCopy.GetComponent<Card>().ExitFocus();
    Destroy(ghostCopy.GetComponent<Card>());
    ghostCopy.transform.SetAsFirstSibling();
    CanvasGroup canvasGroup = ghostCopy.GetComponent<CanvasGroup>();
    canvasGroup.alpha = .5f;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    onPointerEnter?.Invoke();
    if (editFeedbackObject != null)
    {
      editFeedbackObject.SetActive(true);
    }

  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (editFeedbackObject != null)
    {
      editFeedbackObject.SetActive(false);
    }
  }

  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(frontRect, Input.mousePosition);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Left)
    {
      onPointerDown?.Invoke();
    }
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Left)
    {
      onPointerUp?.Invoke(eventData.dragging);
    }
  }

  public void OnEndDrag(PointerEventData eventData)
  { }

  public void OnBeginDrag(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Left && onSelectionChangedListener == null)
    {
      onBeginDrag?.Invoke();
    }
  }

  public void OnDrag(PointerEventData eventData)
  {
    if (eventData.button == PointerEventData.InputButton.Left && onSelectionChangedListener == null)
    {
      onDrag?.Invoke();
    }
  }

  public void OnScroll(PointerEventData eventData)
  {
    ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.scrollHandler);
  }


}
