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

using System.Linq;
using System.Collections.Generic;
using UnityEngine;

// Eh rename this to... DeckSlot just to be consistent
public class CardDeck : MonoBehaviour
{
  IDeckModel model;

  [SerializeField] CardManager cardManager;
  [SerializeField] TMPro.TextMeshProUGUI slotPrompt;
  [SerializeField] UnityEngine.UI.Image slotOutlineImage;
  [SerializeField] UnityEngine.UI.LayoutElement parentLayoutElement;
  [SerializeField] UnityEngine.UI.LayoutElement layoutElement;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] Color baseColor;
  [SerializeField] Color selectColor;
  [SerializeField] AddBehaviorButton addBehaviorButton;
  [SerializeField] RectTransform addBehaviorRect;
  [SerializeField] TMPro.TextMeshProUGUI addBehaviorButtonText;
  [SerializeField] GameObject cardContainerPrefab;
  [SerializeField] Card cardPrefab;
  [SerializeField] GameObject pulsingOutline;
  [SerializeField] GameObject staticOutline;

  public List<CardContainer> containers = new List<CardContainer>();

  private RectTransform addBehaviorButtonTransform;

  public delegate void OnAddBehaviorButtonClicked();
  public event OnAddBehaviorButtonClicked onAddBehaviorButtonClicked;

  public delegate void OnCardClicked(Card card);
  public event OnCardClicked onCardClicked;

  bool replaceIndicating = false;
  bool isLibraryTarget = false;

  public RectTransform GetAddRect()
  {
    return addBehaviorRect;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref cardManager);
    cardManager.RegisterCardSlot(this);
    addBehaviorButton.OnClick += () =>
    {
      onAddBehaviorButtonClicked?.Invoke();
    };
    addBehaviorButtonTransform = addBehaviorButton.GetComponent<RectTransform>();
  }

  internal void DeleteCardsMatching(ICardAssignmentModel assignedModel)
  {
    List<CardContainer> toRemove = new List<CardContainer>(
      from container in containers
      where container.GetCard().GetCardAssignment() == assignedModel
      select container);

    toRemove.ForEach(container => RemoveCardContainer(container));
  }

  public IDeckModel GetModel()
  {
    return model;
  }

  public void ToggleLibraryTarget(bool on)
  {
    isLibraryTarget = on;
    pulsingOutline.SetActive(isLibraryTarget || replaceIndicating);
  }

  public void ToggleReplaceIndicator(bool on)
  {
    replaceIndicating = on;
    if (pulsingOutline != null)
    {
      pulsingOutline.SetActive(isLibraryTarget || replaceIndicating);
    }
  }

  public void Setup(IDeckModel model)
  {
    this.model = model;
    slotPrompt.text = model.GetPrompt();
    if (slotPrompt.text == "") slotPrompt.gameObject.SetActive(false);
    addBehaviorButton.SetCardCategory(model.GetCardCategory());

    foreach (var assignment in model.GetAssignedCards())
    {
      AddCardFromModel(assignment);
    }
  }

  private Card AddCardFromModel(ICardAssignmentModel assignment)
  {
    Card card = Instantiate(cardPrefab);
    card.Populate(assignment);
    card.AddListener((eventCard, eventType, eventData) =>
    {
      switch (eventType)
      {
        case Card.EventType.POINTER_DOWN:
          cardManager.OnPointerDownCard(eventCard);
          break;
        case Card.EventType.POINTER_UP:
          cardManager.OnPointerUpCard(eventCard);
          break;
        case Card.EventType.BEGIN_DRAG:
          cardManager.BeginDrag();
          break;
        default:
          break;
      }
    });

    card.OnClick = () => onCardClicked?.Invoke(card);

    // Create container for the card
    CardContainer newContainer = Instantiate(cardContainerPrefab, transform).GetComponentInChildren<CardContainer>();
    int index = this.model.GetIndexOf(card.GetCardAssignment());
    newContainer.rectTransform.transform.SetSiblingIndex(index);
    newContainer.SetSize(CardManager.BASE_CARD_SIZE);
    newContainer.scaleOnFocus = false;
    newContainer.deck = this;

    newContainer.onPointerDown += () => cardManager.OnPointerDownCard(newContainer.GetCard());
    newContainer.onPointerUp += (dragging) => cardManager.OnPointerUpCard(newContainer.GetCard());
    newContainer.onPointerEnter += () => cardManager.OnPointerEnterContainer(newContainer);
    newContainer.onBeginDrag += cardManager.BeginDrag;

    containers.Add(newContainer);
    newContainer.SetCard(card);
    newContainer.TriggerAddEffect();
    ResortCards();

    return card;
  }

  public void SetColor(Color highlightColor)
  {
    baseColor = highlightColor;
    slotOutlineImage.color = baseColor;
    slotPrompt.color = highlightColor;
    addBehaviorButton.SetBaseColor(highlightColor);
  }

  // void RefreshAddBehaviorButton()
  // {
  //   addBehaviorButtonTransform.SetAsLastSibling();
  // }

  public Card ReplaceCard(ICardAssignmentModel assignment, CardContainer container)
  {
    int index = model.GetIndexOf(container.GetCard().GetCardAssignment());
    cardManager.UnassignCard(container.GetCard().GetCardAssignment());
    return AcceptAssignedCard(assignment, index);
  }

  public Card ReplaceCard(ICardModel unassignedCard, CardContainer container)
  {
    int index = model.GetIndexOf(container.GetCard().GetCardAssignment());
    cardManager.UnassignCard(container.GetCard().GetCardAssignment());
    return AcceptCard(unassignedCard, index);
  }

  public Card AcceptAssignedCard(ICardAssignmentModel assignedCard, int index = -1)
  {
    Debug.Assert(assignedCard != null, "AcceptAssignedCard called with null assignment");
    ICardModel unassignedCard = assignedCard.GetCard();
    // We need to unassign it. But make sure we keep the property assignments.
    PropEditor[] props = assignedCard.GetProperties();
    cardManager.UnassignCard(assignedCard);
    return AcceptCard(unassignedCard, index, props);
  }

  public Card AcceptCard(ICardModel unassignedCard, int index = -1, PropEditor[] initProps = null)
  {
    var assigned = this.model.OnAssignCard(unassignedCard, index);
    if (initProps != null)
    {
      assigned.SetProperties(initProps);
    }
    return AddCardFromModel(assigned);
  }

  private void ResortCards()
  {
    containers.Sort((CardContainer container1, CardContainer container2) =>
    {
      return this.model.GetIndexOf(container1.GetCard().GetCardAssignment())
          .CompareTo(this.model.GetIndexOf(container2.GetCard().GetCardAssignment()));
    });
    addBehaviorButtonTransform.SetAsLastSibling();

  }

  void RemoveCardContainer(CardContainer container)
  {
    containers.Remove(container);
    container.RequestDestruction();
  }

  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
  }

  public void StartFocus()
  {
    staticOutline.SetActive(true);
  }

  public void EndFocus()
  {
    staticOutline.SetActive(false);
  }

  internal bool SupportsCard(Card card)
  {
    return card.GetModel().GetCategories().Contains(this.model.GetCardCategory());
  }
}
