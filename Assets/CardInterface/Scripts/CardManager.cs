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
using System.Linq;

public class CardManager : MonoBehaviour
{
  [SerializeField] PanelManager panelManager;
  [SerializeField] ZoomableRect zoomableRect;
  [SerializeField] RectTransform resizingRect;
  [SerializeField] RectTransform canvasRect;
  [SerializeField] Canvas canvas;
  [SerializeField] CardDetail cardDetailPrefab;
  [SerializeField] UnityEngine.UI.Button addPanelButton;
  [SerializeField] RectTransform panelNoteParentRect;
  [SerializeField] RectTransform focusCardParent;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Button centerButton;
  [SerializeField] UnityEngine.UI.Button organizePanelsButton;
  public GameObject panelNotePrefab;

  public RectTransform trash;
  [SerializeField] UnityEngine.UI.Image trashImage;
  [SerializeField] Color trashColorOn;
  [SerializeField] Color trashColorOff;

  public Action<string, CardContainer, CardDeck> onCardLibraryRequest;
  public Action onCardLibraryCancelRequest;
  public Action onPanelLibraryRequest;
  public Action<string> onCodeRequest;
  public Action onCloseRequest;

  List<PanelNote> panelNotes = new List<PanelNote>();

  public static float BASE_CARD_SIZE = .65f;
  CursorManager cursorManager;
  CardDetail cardDetail;
  [HideInInspector] public RectTransform referenceRect;
  [HideInInspector] public Vector2 referenceScreenCornerMin;
  [HideInInspector] public Vector2 referenceScreenCornerMax;

  internal void SetReferenceRect(RectTransform referenceRect)
  {
    this.referenceRect = referenceRect;
    cardDetail.Setup(referenceRect);
  }

  const string SHOW_LIBRARY_TEXT = "Add\nPanels";
  const string HIDE_LIBRARY_TEXT = "Close\nLibrary";

  const float FROM_LIBRARY_DETAIL_ANIMATION_DURATION = .3f;
  const float FROM_CLICK_DETAIL_ANIMATION_DURATION = .1f;

  enum PointerState
  {
    MouseOver,
    PointerDown,
    Detail,
    Dragging,
    Disabled
  }

  public bool OnMenuRequest()
  {
    if (cardDetail.IsOpen())
    {
      cardDetail.OnEscape();
      return true;
    }
    return false;
  }

  ICardManagerModel model;

  PointerState pointerState = PointerState.MouseOver;

  Card focusCard;
  const float MOUSEOVER_COOLDOWN_CONST = .25f;
  const float CARD_CLICK_THRESHOLD = 0.15f;
  float mouseoverCooldownTimer;
  float clickThresholdTimer;

  List<CardDeck> cardSlots = new List<CardDeck>();
  CardDeck mouseOverCardSlot = null;
  CardContainer mouseOverContainer = null;
  CardDeck mouseOverSlot = null;

  PopupPanel[] popupPanels;

  // Purpsefully vague name..but it has to do with undo'ing notes changes.
  float notesUndoTimer = 0f;

  public ICardManagerModel GetModel() { return model; }

  void Awake()
  {

    addPanelButton.onClick.AddListener(OnAddPanelButtonClick);
    Util.FindIfNotSet(this, ref cursorManager);

    centerButton.onClick.AddListener(panelManager.ZoomOutToAllPanels);
    organizePanelsButton.onClick.AddListener(() => panelManager.SetOrganizedPanels(true));

    cardDetail = Instantiate(cardDetailPrefab, null);
    cardDetail.onTrashCard += (unassignedModel, assignedModel) =>
    {
      UnassignCard(assignedModel);
    };
    cardDetail.onCodeCard += (card, assignment, container) =>
    {
      if (!card.IsValid())
      {
        return;
      }
      if (card.IsBuiltin())
      {
        // Builtin card. Create a custom copy of it and change the assignment to
        // refer to the copy instead.
        ICardModel cardCustomCopy = card.MakeCopy();
        int index = container.deck.GetModel().GetIndexOf(assignment);
        container.deck.AcceptCard(cardCustomCopy, index, assignment.GetProperties());
        // Remove the old assignment
        assignment.Unassign();

        // Maybe open the code editor for the custom copy
        onCodeRequest?.Invoke(cardCustomCopy.GetUri());
      }
      else
      {
        onCodeRequest?.Invoke(card.GetUri());
      }
    };
    cardDetail.onPreviewCard += (card) =>
    {
      onCodeRequest?.Invoke(card.GetUri());
    };
    closeButton.onClick.AddListener(() =>
    {
      onCloseRequest?.Invoke();
    });
  }

  private void OnLibraryClose()
  {
    if (pointerState == PointerState.Dragging)
    {
      ReleaseDraggedCard();
    }

    // if (currentLibraryTarget != null)
    // {
    //   currentLibraryTarget.ToggleLibraryTarget(false);
    // }
  }

  void OnAddPanelButtonClick()
  {
    onPanelLibraryRequest?.Invoke();
  }

  internal void UnassignCard(ICardAssignmentModel assignedModel)
  {
    if (!assignedModel.IsValid())
    {
      return;
    }
    //I think this was redundant
    // if (focusCard != null && focusCard.GetAssignedCard() == assignedModel)
    // {
    //   focusCard.RequestDestroy();
    // }

    foreach (var slot in cardSlots)
    {
      slot.DeleteCardsMatching(assignedModel);
    }

    assignedModel.Unassign();
  }

  public void RequestDestroy()
  {
    Destroy(cardDetail.gameObject);
    Destroy(gameObject);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Open(ICardManagerModel model)
  {
    gameObject.SetActive(true);
    SetModel(model);

    panelManager.ClearPanels();
    ClearPanelNotes();
    foreach (var item in model.GetAssignedPanels())
    {
      panelManager.AddPanel(item, false/*from library */, false/*dragged on */);
    }
    panelManager.RefreshPanelsLayout(model);

    Update();
  }

  void SetModel(ICardManagerModel newModel)
  {
    if (this.model == newModel) return;
    if (this.model != null)
    {
      this.model.Dispose();
    }
    this.model = newModel;
  }

  private void ClearPanelNotes()
  {
    for (int i = 0; i < panelNotes.Count; i++)
    {
      panelNotes[i].DeleteNote();
    }

    panelNotes.Clear();
  }

  public void Close()
  {
    if (!gameObject.activeInHierarchy) return;
    onCardLibraryCancelRequest?.Invoke();

    if (model != null && model.IsValid())
    {
      // We could've lost ownership or something, and thus should not try to
      // write.
      if (model.CanWrite())
      {
        model.SetViewState(panelManager.SaveViewState());
      }
    }

    if (pointerState == PointerState.Dragging)
    {
      ReleaseDraggedCard();
    }
    pointerState = PointerState.MouseOver;

    if (focusCard != null)
    {
      focusCard.ExitFocus();
      focusCard = null;
    }

    cardDetail.Close();
    cursorManager.ReturnToDefault();
    gameObject.SetActive(false);

    // Do this last, in case anything above depends on it.
    SetModel(null);
  }

  public bool IsCardDetailsOpen()
  {
    return cardDetail.IsOpen();
  }

  void CloseAllButOnePanel(PopupPanel panel)
  {
    for (int i = 0; i < popupPanels.Length; i++)
    {
      if (popupPanels[i] != panel) popupPanels[i].Close();
    }
  }

  //pinning the resize rect to the reference rect back on the card tab
  void RectPinUpdate()
  {
    Vector3[] referenceCorners = new Vector3[4];
    referenceRect.GetWorldCorners(referenceCorners);

    referenceScreenCornerMin = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[0]);
    referenceScreenCornerMax = RectTransformUtility.WorldToScreenPoint(null, referenceCorners[2]);

    Vector2 cornerMin, cornerMax;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, referenceScreenCornerMin, null, out cornerMin);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, referenceScreenCornerMax, null, out cornerMax);

    resizingRect.anchoredPosition = cornerMin;
    Vector2 size = cornerMax - cornerMin;
    resizingRect.sizeDelta = size;
  }

  bool OverAnyPanelNotes()
  {
    foreach (PanelNote note in panelNotes)
    {
      if (note.IsMouseOver()) return true;
    }
    return false;
  }


  bool DraggingAnyPanelNotes()
  {
    foreach (PanelNote note in panelNotes)
    {
      if (note.isBeingDragged) return true;
    }
    return false;
  }


  void CursorUpdate()
  {
    bool showHandCursor = zoomableRect.pointerEntered && !panelManager.OverAnyPanelsOrButtons() && !OverAnyPanelNotes();
    if (showHandCursor)
    {
      if (Input.GetMouseButton(0))
      {
        cursorManager.SetCursor(CursorManager.CursorType.HandClosed);
      }
      else if (Input.GetMouseButton(1))
      {
        cursorManager.SetCursor(CursorManager.CursorType.Zoom);
      }
      else
      {
        cursorManager.SetCursor(CursorManager.CursorType.HandOpen);
      }
    }
    else
    {
      cursorManager.SetCursor(CursorManager.CursorType.Pointer);
    }
  }

  void OnUserChangedPanelNotes()
  {
    // Set the timer.
    notesUndoTimer = 1f;
  }

  void UpdateNotesUndo()
  {
    if (notesUndoTimer <= 0f)
    {
      // Timer was already ran out. Nothing interesting happened.
      return;
    }

    notesUndoTimer -= Time.unscaledDeltaTime;
    if (notesUndoTimer <= 0f)
    {
      // Timer just ran out - take an undo snapshot!
      Util.Log($"Snapshot panel notes");
      model.SetNotes(SavePanelNotes());
    }
  }

  void UpdateTrash()
  {
    bool isOn = panelManager.IsPanelBeingDragged() || pointerState == PointerState.Dragging || DraggingAnyPanelNotes();
    trashImage.color = isOn ? trashColorOn : trashColorOff;
  }

  void Update()
  {
    RectPinUpdate();
    CursorUpdate();
    UpdateNotesUndo();

    UpdateTrash();
    // addPanelButtonText.text = panelLibrary.IsOpen() ? HIDE_LIBRARY_TEXT : SHOW_LIBRARY_TEXT;
    organizePanelsButton.gameObject.SetActive(!panelManager.GetOrganizedPanels());

    if (focusCard == null) return;

    switch (pointerState)
    {
      case PointerState.MouseOver:
        MouseOverUpdate();
        break;
      case PointerState.PointerDown:
        PointerDownUpdate();
        break;
      case PointerState.Detail:
        DetailUpdate();
        break;
      case PointerState.Dragging:
        DraggingUpdate();
        break;
    }

  }

  private void DetailUpdate()
  {
    if (!cardDetail.IsOpen())
    {
      pointerState = PointerState.MouseOver;
    }
  }

  CardDeck GetMouseoverSlot(Card card)
  {
    foreach (CardDeck slot in cardSlots)
    {
      if (slot.SupportsCard(card))
      {
        if (slot.IsMouseOver())
        {
          return slot;
        }
      }
    }

    return null;
  }

  CardContainer GetMouseoverCandidate(Card card)
  {
    foreach (CardDeck slot in cardSlots)
    {
      if (slot.SupportsCard(card))
      {
        foreach (CardContainer container in slot.containers)
        {
          if (container.IsMouseOver() && container.GetCard() != card)
          {
            return container;
          }
        }
      }
    }

    return null;
  }

  private bool IsCardOverTrash(Card card)
  {
    return RectTransformUtility.RectangleContainsScreenPoint(trash, Input.mousePosition) && card.GetCardAssignment() != null;
  }

  private void DraggingUpdate()
  {
    focusCard.DragUpdate(focusCardParent);
    if (IsCardOverTrash(focusCard))
    {
      focusCard.SetDeletionHintVisible(true);
    }
    else
    {
      focusCard.SetDeletionHintVisible(false);
      UpdateUIForDragCard(focusCard);
    }
  }

  public void BeginDrag()
  {
    focusCard.StartDrag(focusCardParent);
    focusCard.SetScale(GetCardScale());
    pointerState = PointerState.Dragging;

    UpdateUIForBeginDragCard(focusCard);
  }

  public float GetCardScale()
  {
    return zoomableRect.currentScale * BASE_CARD_SIZE;
  }

  public void UpdateUIForBeginDragCard(Card card)
  {
    cardDetail.Close();

    foreach (CardDeck slot in cardSlots)
    {
      bool supportsCard = slot.SupportsCard(card);
      slot.ToggleReplaceIndicator(supportsCard);

      foreach (CardContainer container in slot.containers)
      {
        if (container != null)
        {
          if (container.GetCard() != card)
          {
            container.ToggleReplaceIndicator(supportsCard);
          }
        }
      }
    }
  }

  public void UpdateUIForDragCard(Card card)
  {
    CardContainer candidateContainer = GetMouseoverCandidate(card);
    CardDeck candidateSlot = GetMouseoverSlot(card);

    if (candidateContainer != mouseOverContainer)
    {
      if (mouseOverContainer != null) mouseOverContainer.EndFocus();
      mouseOverContainer = candidateContainer;
      card.SetCandidateContainer(mouseOverContainer);
      if (mouseOverContainer != null) mouseOverContainer.StartFocus();
    }

    if (candidateSlot != mouseOverSlot)
    {
      if (mouseOverSlot != null) mouseOverSlot.EndFocus();
      mouseOverSlot = candidateSlot;
      card.SetCandidateSlot(mouseOverSlot);
    }

    if (mouseOverSlot != null)
    {
      if (mouseOverContainer == null)
      {
        mouseOverSlot.StartFocus();
      }
      else
      {
        mouseOverSlot.EndFocus();
      }
    }
  }

  private void PointerDownUpdate()
  {
    clickThresholdTimer += Time.unscaledDeltaTime;
    if (clickThresholdTimer > CARD_CLICK_THRESHOLD)
    {
      BeginDrag();
    }
  }

  void MouseOverUpdate()
  {
    if (focusCard.IsMouseOver())
    {
      mouseoverCooldownTimer = 0;
      focusCard.EnterFocus();
    }
    else
    {
      mouseoverCooldownTimer += Time.unscaledDeltaTime;
      if (mouseoverCooldownTimer > MOUSEOVER_COOLDOWN_CONST)
      {
        EndCardFocus();
      }
    }
  }

  void EndCardFocus()
  {
    focusCard?.ExitFocus();
    focusCard = null;
    mouseoverCooldownTimer = 0;
  }

  public void RegisterCardSlot(CardDeck newslot)
  {
    cardSlots.Add(newslot);
    newslot.onAddBehaviorButtonClicked += () =>
    {
      onCardLibraryRequest?.Invoke(newslot.GetModel().GetCardCategory(), null, newslot);
    };
    newslot.onCardClicked += (card) =>
    {
      if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
      {
        // Shortcut for copying card
        int index = newslot.GetModel().GetIndexOf(card.GetCardAssignment());
        newslot.AcceptCard(card.GetModel(), index, card.GetCardAssignment().GetProperties());
      }
      else
      {
        OpenCardDetail(card.GetCardAssignment(), card.container, FROM_CLICK_DETAIL_ANIMATION_DURATION);
      }
    };
  }

  public void AcceptClickedLibraryCard(Card card, CardContainer targetContainer, CardDeck targetSlot)
  {
    Card newCard = null;
    if (targetContainer != null)
    {
      newCard = targetContainer.ReplaceCard(card.GetModel());
    }
    else
    {
      newCard = targetSlot.AcceptCard(card.GetModel());

      string uri = newCard.GetCardAssignment().GetCard().GetUri();
      if (BehaviorSystem.IsBuiltinBehaviorUri(uri))
      {
      }
    }
    OnNewCardAdded(newCard);
  }

  public void CancelDraggedCard(Card card)
  {
    pointerState = PointerState.MouseOver;

    foreach (CardDeck slot in cardSlots)
    {
      slot.ToggleReplaceIndicator(false);
      foreach (CardContainer container in slot.containers)
      {
        if (container != null) container.ToggleReplaceIndicator(false);
      }
    }

    if (mouseOverSlot != null)
    {
      mouseOverSlot.EndFocus();
      mouseOverSlot = null;
    }
    if (mouseOverContainer != null)
    {
      mouseOverContainer.EndFocus();
      mouseOverContainer = null;
    }
  }

  public void MaybeAcceptDraggedCard(Card card)
  {
    pointerState = PointerState.MouseOver;

    bool isFromLibrary = card.GetCardAssignment() == null;
    Card newcard = null;
    foreach (CardDeck slot in cardSlots)
    {
      slot.ToggleReplaceIndicator(false);
      foreach (CardContainer container in slot.containers)
      {
        if (container != null) container.ToggleReplaceIndicator(false);
      }
    }

    bool placed = false;

    if (IsCardOverTrash(card))
    {
      UnassignCard(card.GetCardAssignment());
    }

    else if (mouseOverContainer != null)
    {
      if (mouseOverContainer.deck != card.GetCurrentSlot())
      {
        CardContainer targetContainer = mouseOverContainer;
        card.OnCardPlaced?.Invoke();
        if (isFromLibrary) newcard = targetContainer.ReplaceCard(card.GetModel());
        else newcard = targetContainer.ReplaceCard(card.GetCardAssignment());
        placed = true;
      }
    }
    else
    {
      CardDeck slot = GetMouseoverSlot(card);
      if (slot != null)
      {
        if (slot != card.GetCurrentSlot())
        {
          card.OnCardPlaced?.Invoke();
          if (isFromLibrary) newcard = slot.AcceptCard(card.GetModel());
          else newcard = slot.AcceptAssignedCard(card.GetCardAssignment());
          placed = true;
        }
      }
    }

    if (mouseOverSlot != null)
    {
      mouseOverSlot.EndFocus();
      mouseOverSlot = null;
    }
    if (mouseOverContainer != null)
    {
      mouseOverContainer.EndFocus();
      mouseOverContainer = null;
    }

    if (newcard != null)
    {
      focusCard = newcard;
      focusCard.EnterFocus();
    }

    if (placed && isFromLibrary)
    {
      OnNewCardAdded(newcard);
    }
  }

  void ReleaseDraggedCard()
  {
    focusCard.EndDrag();
    MaybeAcceptDraggedCard(focusCard);
  }

  public void OnNewCardAdded(Card newcard)
  {
    // cardLibrary.Close();
    //OpenCardDetail(newcard.GetAssignedCard(), newcard.container, FROM_LIBRARY_DETAIL_ANIMATION_DURATION);
  }

  public void OnPointerDownCard(Card card)
  {
    if (focusCard != card)
    {
      EndCardFocus();
    }

    focusCard = card;

    clickThresholdTimer = 0;
    pointerState = PointerState.PointerDown;

  }

  public void OnPointerUpCard(Card card)
  {
    if (pointerState == PointerState.Dragging)
    {
      ReleaseDraggedCard();
    }
    else
    {
      pointerState = PointerState.MouseOver;
    }
  }

  public void OpenCardDetail(ICardAssignmentModel assignedModel, CardContainer container, float openDuration)
  {
    cardDetail.Open(openDuration);
    cardDetail.Populate(assignedModel, container);
    pointerState = PointerState.Detail;
  }

  public void CloseCardDetail()
  {
    cardDetail.Close();
  }

  public void OnPointerEnterContainer(CardContainer container)
  {
    if (pointerState != PointerState.MouseOver) return;

    if (focusCard != null)
    {
      if (focusCard.container != container)
      {
        EndCardFocus();
      }
    }

    focusCard = container.GetCard();
    focusCard.EnterFocus();
  }

  public void EnablePointerState()
  {
    pointerState = PointerState.MouseOver;
  }

  public void DisablePointerState()
  {
    pointerState = PointerState.Disabled;
  }

  public void LoadPanelNotes(PanelNote.Data[] notes)
  {
    if (notes == null)
    {
      return;
    }
    for (int i = 0; i < notes.Length; i++)
    {
      CreatePanelNote(notes[i]);
    }
  }

  private void CreatePanelNote(PanelNote.Data note)
  {
    PanelNote newNote = CreatePanelNote();
    newNote.SetData(note);
  }

  public PanelNote CreatePanelNote()
  {
    PanelNote newNote = Instantiate(panelNotePrefab, panelNoteParentRect).GetComponentInChildren<PanelNote>();
    newNote.onUserChangedData += OnUserChangedPanelNotes;
    newNote.SetParentRect(panelNoteParentRect);
    panelNotes.Add(newNote);
    return newNote;
  }

  public void RemovePanelNoteFromList(PanelNote note)
  {
    if (panelNotes.Contains(note))
    {
      panelNotes.Remove(note);
    }
  }

  public PanelNote.Data[] SavePanelNotes()
  {
    return (from note in panelNotes select note.GetData()).ToArray();
  }

  public void AddPanel(PanelLibraryItem.IModel libPanel, bool dragOn)
  {
    Util.Log($"KEEP ME adding panel {libPanel.GetTitle()}");
    CardPanel.IAssignedPanel panel = libPanel.Assign();
    Util.Log($"KEEP ME added panel, ID = {panel.GetId()}");
    // Call this last, so it shows the default cards we added right above.
    panelManager.AddPanel(panel, true/*added from library*/, dragOn);
  }

  public void CopyPanel(CardPanel panel)
  {
    CardPanel.IAssignedPanel newPanel = panel.GetModel().Duplicate();
    panelManager.SetOrganizedPanels(false);
    panelManager.AddPanel(newPanel, false /*added from library*/, false /*draggedOn */);
    panelManager.SetupPanelPlacementAndNotes(model);
  }

  public interface PopupPanel
  {
    void SetOpenAction(System.Action action);
    //void Open(float openDuration);
    bool IsOpen();
    void Close();
  }
}

