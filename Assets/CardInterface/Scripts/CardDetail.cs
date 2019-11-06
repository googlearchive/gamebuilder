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

public class CardDetail : MonoBehaviour, CardManager.PopupPanel
{
  [SerializeField] UnityEngine.UI.Button addToSlotButton;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Button previewButton;
  [SerializeField] UnityEngine.UI.Button codeButton;
  [SerializeField] TMPro.TextMeshProUGUI codeText;
  [SerializeField] UnityEngine.UI.Button trashButton;
  [SerializeField] TMPro.TextMeshProUGUI trashText;
  // [SerializeField] UnityEngine.UI.Button replaceButton;
  [SerializeField] Card card;
  [SerializeField] RectTransform canvasRect;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] GameObject noPropertiesObject;
  [SerializeField] CanvasGroup canvasGroup;

  private RectTransform referenceParent;
  private CardContainer cardContainer;
  private VoosEngine voosEngine;
  private SceneActorLibrary actorLib;

  System.Action onOpen;
  public event System.Action<ICardModel, ICardAssignmentModel, CardContainer> onCodeCard;
  public event System.Action<ICardModel> onPreviewCard;
  public event System.Action<ICardModel, ICardAssignmentModel> onTrashCard;
  private System.Action<Card> onAddCardToSlotListener;

  public delegate bool CanAddCardToSlot(Card card);
  private CanAddCardToSlot canAddCardToSlot;


  void Awake()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref actorLib);
    closeButton.onClick.AddListener(Close);
    previewButton.onClick.AddListener(PreviewCard);
    codeButton.onClick.AddListener(CodeCard);
    trashButton.onClick.AddListener(OnTrashClicked);
    // replaceButton.onClick.AddListener(OnReplaceClicked);
    addToSlotButton.onClick.AddListener(OnAddCardToSlotClicked);
    card.onPropChanged += OnCardPropChanged;
  }

  void Update()
  {
    Vector2 cornerMin, cornerMax;
    Util.FindRectCornersFromDifferentCanvas(referenceParent, canvasRect, out cornerMin, out cornerMax);
    rectTransform.anchoredPosition = (cornerMax + cornerMin) / 2f;
  }

  public void Setup(RectTransform referenceParent)
  {
    this.referenceParent = referenceParent;
  }

  public bool IsOpen()
  {
    return rectTransform.gameObject.activeSelf;
  }

  public void Open(float openDuration)
  {
    Update();
    onOpen?.Invoke();
    showRoutine = StartCoroutine(ShowRoutine(openDuration));
  }

  Coroutine showRoutine;
  IEnumerator ShowRoutine(float openDuration)
  {
    canvasGroup.alpha = 0;
    rectTransform.gameObject.SetActive(true);
    float timer = 0;
    while (timer < openDuration)
    {
      timer += Time.unscaledDeltaTime;
      float percent = Mathf.Clamp01(timer / openDuration);
      canvasGroup.alpha = percent;
      rectTransform.localScale = Vector3.one * percent;
      yield return null;
    }
  }

  internal void OnEscape()
  {
    if (!card.OnEscape())
    {
      Close();
    }
  }

  /* 

 //TODO: THIS DOES NOT WORK
 IEnumerator HideRoutine()
 {
   float timer = 0;
   while (timer < HIDE_ANIMATION_DURATION)
   {
     timer += Time.unscaledDeltaTime;
     float percent = Mathf.Clamp01(timer / HIDE_ANIMATION_DURATION);
     canvasGroup.alpha = 1 - percent;
     yield return null;
   }

   gameObject.SetActive(false);
 }
*/
  public void Populate(ICardAssignmentModel assignedCard, CardContainer container)
  {
    if (assignedCard == null || !assignedCard.IsValid())
    {
      return;
    }

    this.cardContainer = container;
    card.Populate(assignedCard, true);
    if (assignedCard.GetCard().IsBuiltin())
    {
      codeText.SetText("Duplicate and edit javascript");
      previewButton.gameObject.SetActive(true);
    }
    else
    {
      codeText.SetText("Edit javascript");
      previewButton.gameObject.SetActive(false);
    }
    trashButton.gameObject.SetActive(true);
    noPropertiesObject.SetActive(!card.HasAnyProps());
    UpdateAddToSlotButton();
  }

  public void Populate(ICardModel unassignedCard)
  {
    if (unassignedCard == null || !unassignedCard.IsValid())
    {
      return;
    }

    cardContainer = null;
    card.Populate(unassignedCard, true);
    if (unassignedCard.IsBuiltin())
    {
      codeText.SetText("Duplicate and edit JavaScript");
      trashButton.gameObject.SetActive(false);
      previewButton.gameObject.SetActive(true);
    }
    else
    {
      codeText.SetText("Edit JavaScript");
      previewButton.gameObject.SetActive(false);

      string behaviorUri = unassignedCard.GetUnassignedBehaviorItem().behaviorUri;
      VoosActor user = voosEngine.FindOneActorUsing(behaviorUri);
      string fromActorLib = actorLib.FindOneActorUsingBehavior(behaviorUri);

      if (user != null)
      {
        trashText.SetText($"Cannot delete - used by actor '{user.GetDisplayName()}'");
        trashButton.interactable = false;
      }
      else if (fromActorLib != null)
      {
        trashText.SetText($"Cannot delete - used by creation library actor '{fromActorLib}'");
        trashButton.interactable = false;
      }
      else
      {
        trashText.SetText($"Remove card");
        trashButton.interactable = true;
      }
      trashButton.gameObject.SetActive(true);
    }
    noPropertiesObject.SetActive(!card.HasAnyProps());
    UpdateAddToSlotButton();
  }

  public ICardModel GetModel()
  {
    return card.GetModel();
  }

  public void Close()
  {
    if (showRoutine != null) StopCoroutine(showRoutine);
    rectTransform.gameObject.SetActive(false);
  }

  internal bool IsModelValid()
  {
    return card.IsCardValid();
  }

  public void SetAddCardToSlotListener(System.Action<Card> listener, CanAddCardToSlot condition = null)
  {
    canAddCardToSlot = condition;
    onAddCardToSlotListener = listener;
    UpdateAddToSlotButton();
  }

  private void UpdateAddToSlotButton()
  {
    addToSlotButton.gameObject.SetActive(
      onAddCardToSlotListener != null &&
      card.IsCardValid() &&
      (canAddCardToSlot == null || canAddCardToSlot(card)));
  }

  public void CodeCard()
  {
    Close();
    onCodeCard?.Invoke(card.GetModel(), card.GetCardAssignment(), cardContainer);
  }

  public void PreviewCard()
  {
    Close();
    onPreviewCard?.Invoke(card.GetModel());
  }

  void OnTrashClicked()
  {
    Close();
    onTrashCard?.Invoke(card.GetModel(), card.GetCardAssignment());
  }

  void OnReplaceClicked()
  {
    Debug.Log("This is not hooked up");
  }

  void OnAddCardToSlotClicked()
  {
    onAddCardToSlotListener(card);
  }

  void OnCardPropChanged(BehaviorProperties.PropType type)
  {
  }

  public void SetOpenAction(System.Action action)
  {
    onOpen = action;
  }
}
