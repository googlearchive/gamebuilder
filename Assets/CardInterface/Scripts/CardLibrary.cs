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

#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
using Steamworks;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using System;

public class CardLibrary : MonoBehaviour, CardManager.PopupPanel
{
  [SerializeField] CardDetail cardDetailPrefab;
  [SerializeField] Card cardPrefab;
  [SerializeField] GameObject cardContainerPrefab;
  [SerializeField] CardLibraryUI cardLibraryUI;
  [SerializeField] NewCardUI newCardUI;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] GameObject darkBackground;

  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform cardRect;
  [SerializeField] RectTransform parentRect;
  [SerializeField] CardPackPicker cardPackPickerPrefab;
  [SerializeField] CardPackWorkshopMenu workshopMenuPrefab;
  [SerializeField] CardPackUploadDialog uploadDialogPrefab;


  private BehaviorSystem behaviorSystem;
  private UndoStack undoStack;
  private ClaimKeeper claimKeeper;
  private VoosEngine voosEngine;
  private List<TMPro.TMP_Dropdown.OptionData> categoryFilterOptions;
  private List<TMPro.TMP_Dropdown.OptionData> newCardCategoryOptions;
  private CardDetail cardDetail;
  private CardPackUploadDialog uploadDialog;
  private CardPackWorkshopMenu workshopMenu;
  private CardPackPicker cardPackPicker;
  private DynamicPopup popups;

  List<GameObject> cardContainerObjects = new List<GameObject>();
  System.Action onOpen;
  public System.Action onClose;

  public event System.Action<string> onCodeRequest;

  public event System.Action<Card> onBeginDragCard;
  public event System.Action<Card> onDragCard;
  public event System.Action<Card> onEndDragCard;
  public event System.Action<Card> onForceReleaseDragCard;
  public event System.Action<Card> onClickCard;

  private class Selection
  {
    public System.Action<IEnumerable<string>> selectionFinishedCallback;
    public HashSet<string> selectedCards;

    public Selection(System.Action<IEnumerable<string>> selectionFinishedCallback)
    {
      this.selectionFinishedCallback = selectionFinishedCallback;
      selectedCards = new HashSet<string>();
    }
  }

  private Selection selection;

  const float CARD_SCALE = .45f;
  const float ROW_HEIGHT = 330;
  const float GRID_DIMENSION_X = 180;
  const float GRID_DIMENSION_Y = 252;
  const float GRID_PADDING_X = 10;
  const float GRID_PADDING_EDGE = 200;
  const string ALL_CATEGORIES_OPTION = "<All>";

  const string IMPORT_FROM_DISK = "From disk";
  const string IMPORT_FROM_WORKSHOP = "From workshop";
  const string EXPORT_TO_DISK = "To disk";
  const string EXPORT_TO_WORKSHOP = "To workshop";
  const string UPDATE_WORKSHOP_PACK = "Update workshop pack";

  bool refreshQueued = false;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref claimKeeper);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref popups);
    newCardUI.newCardButton.onClick.AddListener(() =>
    {
      string category = newCardCategoryOptions[newCardUI.newCardCategoryDropdown.value].text;
      AddNewCard(category);
    });
    cardLibraryUI.closeButton.onClick.AddListener(Close);
    cardLibraryUI.inputField.onValueChanged.AddListener(OnInputFieldChanged);
    cardLibraryUI.clearSearchButton.onClick.AddListener(ClearSearch);
    cardLibraryUI.categoryDropdown.onValueChanged.AddListener((v) =>
    {
      MatchNewCardCategoryToFilter();
      UpdateCards();
    });
    cardDetail = Instantiate(cardDetailPrefab);
    cardDetail.Setup(parentRect);

    cardDetail.onCodeCard += (model, _, container) =>
    {
      if (!model.IsValid())
      {
        return;
      }
      if (model.IsBuiltin())
      {
        // Not embedded, need to copy first
        string newBehaviorUri = model.MakeCopy().GetUri();
        onCodeRequest?.Invoke(newBehaviorUri);
      }
      else
      {
        onCodeRequest?.Invoke(model.GetUri());
      }
    };
    cardDetail.onPreviewCard += (model) =>
    {
      onCodeRequest?.Invoke(model.GetUri());
    };
    cardDetail.onTrashCard += (model, _) =>
    {
      if (!model.IsValid())
      {
        return;
      }

      ClaimableUndoUtil.ClaimableUndoItem undoItem = new ClaimableUndoUtil.ClaimableUndoItem();
      Behaviors.Behavior behavior = model.GetUnassignedBehaviorItem().GetBehavior();
      string uri = model.GetUri();
      string id = model.GetId();
      undoItem.resourceId = UnassignedBehavior.GetClaimResourceId(id);
      undoItem.resourceName = model.GetTitle();
      undoItem.label = $"Delete {model.GetTitle()}";
      undoItem.doIt = () =>
      {
        behaviorSystem.DeleteBehavior(id);
      };
      undoItem.undo = () =>
      {
        behaviorSystem.PutBehavior(id, behavior);
      };
      undoItem.cannotDoReason = () =>
      {
        if (voosEngine.FindOneActorUsing(uri) != null)
        {
          return "One or more actors are using this card.";
        }
        return null;
      };
      ClaimableUndoUtil.PushUndoForResource(undoStack, claimKeeper, undoItem);
    };

    behaviorSystem.onBehaviorPut += OnBehaviorPut;
    behaviorSystem.onBehaviorDelete += OnBehaviorDelete;

#if USE_STEAMWORKS
    uploadDialog = Instantiate(uploadDialogPrefab);
    uploadDialog.Setup();

    workshopMenu = Instantiate(workshopMenuPrefab);
    workshopMenu.Setup();
#endif

    cardPackPicker = Instantiate(cardPackPickerPrefab);

    cardLibraryUI.selectionDoneButton.onClick.AddListener(() =>
    {
      Debug.Assert(selection != null);
      selection.selectionFinishedCallback(selection.selectedCards);
      EndSelection();
    });

    cardLibraryUI.selectionCancelButton.onClick.AddListener(() =>
    {
      EndSelection();
    });

#if USE_STEAMWORKS
    cardLibraryUI.exportDropdown.SetOptions(new List<string>() {
      EXPORT_TO_DISK, EXPORT_TO_WORKSHOP, UPDATE_WORKSHOP_PACK
    });
#else
    // cardLibraryUI.exportDropdown.SetOptions(new List<string>() {
    //   EXPORT_TO_DISK
    // });
    cardLibraryUI.exportButton.onClick.AddListener(() => StartSelection(OnFinishSelectionForLocal));
#endif

    cardLibraryUI.exportDropdown.onOptionClicked += (value) =>
    {
      if (value == EXPORT_TO_DISK) StartSelection(OnFinishSelectionForLocal);
#if USE_STEAMWORKS
      else if (value == UPDATE_WORKSHOP_PACK)
      {
        cardPackPicker.Open((res) =>
        {
          if (res.IsEmpty()) return;
          StartSelection((list) => OnFinishSelectionForWorkshop(list, res), res);
        });
      }
      else 
      {
        StartSelection(OnFinishSelectionForWorkshop, Util.Maybe<ulong>.CreateEmpty());
      }
#endif
    };

#if USE_STEAMWORKS
    cardLibraryUI.importDropdown.SetOptions(new List<string>() {
      IMPORT_FROM_DISK, IMPORT_FROM_WORKSHOP
    });
#else
    cardLibraryUI.importButton.onClick.AddListener(ImportLocal);

    // cardLibraryUI.importDropdown.SetOptions(new List<string>() {
    //   IMPORT_FROM_DISK
    // });
#endif

    cardLibraryUI.importDropdown.onOptionClicked += (value) =>
    {
      if (value == IMPORT_FROM_DISK) ImportLocal();
      else workshopMenu.Open();
    };

    PopulateCards();
  }

  private void OnBehaviorPut(BehaviorSystem.PutEvent putEvent)
  {
    if (!IsOpen()) return;

    if (putEvent.isNewBehavior)
    {
      GameObject containerObj = CreateCardAndContainer();
      PopulateContainerWithUri(containerObj, BehaviorSystem.IdToEmbeddedBehaviorUri(putEvent.id));
      StartCoroutine(ScrollToAndFlash(containerObj));
      CardContainer container = containerObj.GetComponentInChildren<CardContainer>();
      containerObj.SetActive(DoesCardMatchSearch(container.GetCard(), cardLibraryUI.inputField.text));
      if (selection != null)
      {
        StartSelectionForContainerObj(containerObj);
      }
    }
    else
    {
      foreach (GameObject containerObj in cardContainerObjects)
      {
        CardContainer container = containerObj.GetComponentInChildren<CardContainer>();
        if (container.GetCard().GetModel().GetId() == putEvent.id)
        {
          PopulateContainerWithUri(containerObj, BehaviorSystem.IdToEmbeddedBehaviorUri(putEvent.id));
          break;
        }
      }
    }

    if (cardDetail.IsOpen() && cardDetail.IsModelValid() && cardDetail.GetModel().GetId() == putEvent.id)
    {
      cardDetail.Populate(
        new BehaviorCards.UnassignedCard(new UnassignedBehavior(
          BehaviorSystem.IdToEmbeddedBehaviorUri(putEvent.id), behaviorSystem)));
    }
  }


  private IEnumerator ScrollToAndFlash(GameObject containerObj)
  {
    yield return null;
    RectTransform rt = containerObj.GetComponent<RectTransform>();
    float scrollableHeight = cardLibraryUI.libraryContainer.rect.height - cardLibraryUI.libraryViewport.rect.height;
    // Where to scroll to so that the card would show in the middle of the viewport?
    float targetScrollPosition = -rt.anchoredPosition.y - (cardLibraryUI.libraryViewport.rect.height / 2);
    // For ScrollRects, 1 = top and 0 = bottom.
    float targetNormalizedPosition = 1 - Mathf.Clamp(targetScrollPosition / scrollableHeight, 0, 1);
    // For now, just instantly scroll
    cardLibraryUI.libraryScrollRect.verticalNormalizedPosition = targetNormalizedPosition;
    Card card = containerObj.GetComponentInChildren<CardContainer>().GetCard();
    card.Flash();
  }

  private void OnBehaviorDelete(BehaviorSystem.DeleteEvent deleteEvent)
  {
    if (!IsOpen()) return;
    if (cardDetail.IsOpen() && cardDetail.GetModel().GetId() == deleteEvent.id)
    {
      cardDetail.Close();
    }
    foreach (GameObject containerObj in cardContainerObjects)
    {
      CardContainer container = containerObj.GetComponentInChildren<CardContainer>();
      if (container.GetCard().GetModel().GetId() == deleteEvent.id)
      {
        Destroy(container.GetCard().gameObject);
        Destroy(containerObj);
        cardContainerObjects.Remove(containerObj);
        break;
      }
    }
  }

  void OpenCardDetailWithCard(Card card)
  {
    cardDetail.Populate(card.GetModel());
    cardDetail.Open(.1f);
  }

  public void SetAddCardToSlotListener(System.Action<Card> listener, CardDetail.CanAddCardToSlot condition = null)
  {
    if (listener == null)
    {
      cardDetail.SetAddCardToSlotListener(null);
      onClickCard = OpenCardDetailWithCard;
    }
    else
    {
      cardDetail.SetAddCardToSlotListener((card) =>
      {
        cardDetail.Close();
        Close();
        listener.Invoke(card);
      }, condition);

      onClickCard = (card) =>
      {
        if (condition(card))
        {
          Close();
          listener.Invoke(card);
        }
        else
        {
          OpenCardDetailWithCard(card);
        }
      };
    }
  }

  CardContainer currentDraggingContainer = null;
  void OnBeginDrag(CardContainer container)
  {
    onBeginDragCard(container.GetCard());
    currentDraggingContainer = container;
    Hide();
    //Close();
  }

  void OnEndDrag(CardContainer container)
  {
    onEndDragCard(container.GetCard());
    currentDraggingContainer = null;
    // Show();
    Close();
  }

  GameObject CreateCardAndContainer()
  {
    Card card = Instantiate(cardPrefab);

    GameObject containerObj = Instantiate(cardContainerPrefab, cardLibraryUI.libraryContainer.transform);
    cardContainerObjects.Add(containerObj);
    CardContainer container = cardContainerObjects.Last().GetComponentInChildren<CardContainer>();
    container.SetSize(CARD_SCALE);
    container.buttonsOnFocus = false;
    container.offsetOnFocus = false;
    container.scaleOnFocus = true;
    container.onBeginDrag += () => OnBeginDrag(container);
    container.onPointerUp += (dragging) =>
    {
      if (dragging)
      {
        OnEndDrag(container);
      }
      else
      {
        onClickCard?.Invoke(card);
      }
    };
    container.onDrag += () => onDragCard(container.GetCard());

    container.SetCard(card);
    card.OnCardPlaced = Close;

    return containerObj;
  }

  void AddNewCard(string category)
  {
    // Close();
    BehaviorCards.CardMetadata md = BehaviorCards.CardMetadata.DefaultCardMetadata;
    md.cardSystemCardData.categories = new string[] { category };
    string metadataJson = JsonUtility.ToJson(md);
    UnassignedBehavior newBehavior = behaviorSystem.CreateNewBehavior(
      BehaviorCards.GetDefaultCodeForCategory(category), metadataJson);
    // onCodeRequest(newBehavior.GetId());
  }

  public bool IsSearchActive()
  {
    return cardLibraryUI.inputField.isFocused;
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public bool OnMenuRequest()
  {
    if (workshopMenu.IsOpen())
    {
      workshopMenu.Close();
      return true;
    }
    else if (IsOpen())
    {
      Close();
      return true;
    }
    return false;
  }

  public void Open(string categoryFilter = null)
  {
    PopulateCards();

    darkBackground.SetActive(true);
    gameObject.SetActive(true);
    Show();
    onOpen?.Invoke();

    // HideAllCards();

    PopulateCategories(categoryFilter);
    ClearSearch();

    refreshQueued = false;
  }

  // Populate cards, reusing old card containers where possible.
  private void PopulateCards()
  {
    IEnumerable<string> cards = behaviorSystem.GetCards();
    int cardCount = cards.Count();

    // If we have more cards in the model than the UI, create the extra # of objects we need.
    for (int i = cardContainerObjects.Count; i < cardCount; i++)
    {
      CreateCardAndContainer();
    }

    // If we have more cards in the UI than the model, destroy the # of objects we don't need.
    for (int i = cardContainerObjects.Count - 1; i >= cardCount; i--)
    {
      GameObject containerObj = cardContainerObjects[i];
      Card card = containerObj.GetComponentInChildren<CardContainer>().GetCard();
      if (card != null)
      {
        Destroy(card.gameObject);
      }
      Destroy(containerObj);
      cardContainerObjects.RemoveAt(i);
    }

    // Populate the objects with the models.
    int j = 0;
    foreach (string cardUri in cards)
    {
      GameObject containerObj = cardContainerObjects[j];
      PopulateContainerWithUri(containerObj, cardUri);
      j++;
    }
  }

  private void PopulateContainerWithUri(GameObject containerObj, string cardUri)
  {
    UnassignedBehavior unassignedBehavior = new UnassignedBehavior(cardUri, behaviorSystem);
    ICardModel cardModel = new BehaviorCards.UnassignedCard(unassignedBehavior);
    if (!cardModel.IsValid())
    {
      return;
    }
    Card card = containerObj.GetComponentInChildren<CardContainer>().GetCard();
    card.Populate(cardModel);
  }

  private void PopulateCategories(string categoryFilter = null)
  {
    newCardCategoryOptions = new List<TMPro.TMP_Dropdown.OptionData>();
    categoryFilterOptions = new List<TMPro.TMP_Dropdown.OptionData>();

    cardLibraryUI.categoryDropdown.ClearOptions();
    newCardUI.newCardCategoryDropdown.ClearOptions();

    bool needsCustomCategory = true;
    foreach (string category in behaviorSystem.GetCategories())
    {
      if (category == BehaviorCards.CUSTOM_CATEGORY) needsCustomCategory = false;
      newCardCategoryOptions.Add(new TMPro.TMP_Dropdown.OptionData(category));
      categoryFilterOptions.Add(new TMPro.TMP_Dropdown.OptionData(category));
    }

    if (needsCustomCategory)
    {
      newCardCategoryOptions.Add(new TMPro.TMP_Dropdown.OptionData(BehaviorCards.CUSTOM_CATEGORY));
      categoryFilterOptions.Add(new TMPro.TMP_Dropdown.OptionData(BehaviorCards.CUSTOM_CATEGORY));
    }

    newCardUI.newCardCategoryDropdown.AddOptions(newCardCategoryOptions);

    categoryFilterOptions.Insert(0, new TMPro.TMP_Dropdown.OptionData(ALL_CATEGORIES_OPTION));
    cardLibraryUI.categoryDropdown.AddOptions(categoryFilterOptions);
    if (categoryFilter == null) cardLibraryUI.categoryDropdown.value = 0;
    else cardLibraryUI.categoryDropdown.value = categoryFilterOptions.FindIndex((option) => option.text == categoryFilter);

    MatchNewCardCategoryToFilter();
  }

  private void MatchNewCardCategoryToFilter()
  {
    string category = categoryFilterOptions[cardLibraryUI.categoryDropdown.value].text;
    int newCardCategoryIndex = newCardCategoryOptions.FindIndex((option) => option.text == category);
    if (newCardCategoryIndex >= 0) newCardUI.newCardCategoryDropdown.value = newCardCategoryIndex;
  }

  private float FindThreshold()
  {
    return Mathf.FloorToInt((parentRect.sizeDelta.x - GRID_PADDING_EDGE) / (GRID_DIMENSION_X + GRID_PADDING_X));
  }

  void Update()
  {
    newCardUI.newButtonRect.SetAsLastSibling();
    cardLibraryUI.clearSearchButton.gameObject.SetActive(cardLibraryUI.inputField.text != "");

    if (refreshQueued)
    {
      refreshQueued = false;
      string category = categoryFilterOptions[cardLibraryUI.categoryDropdown.value].text;
      if (category == ALL_CATEGORIES_OPTION)
      {
        Open();
      }
      else
      {
        Open(category);
      }
    }
  }

  float GetFullScreenHeight()
  {
    return Mathf.Min(parentRect.sizeDelta.y, cardRect.sizeDelta.y);
  }

  public void Close()
  {
    EndSelection();
    if (currentDraggingContainer != null)
    {
      onForceReleaseDragCard(currentDraggingContainer.GetCard());
    }
    cardDetail.SetAddCardToSlotListener(null);
    cardDetail.Close();

    onClose?.Invoke();
    gameObject.SetActive(false);
    darkBackground.SetActive(false);
    // Show();
  }

  public void SetOpenAction(System.Action action)
  {
    onOpen = action;
  }

  bool DoesCardMatchSearch(Card card, string searchString)
  {
    ICardModel model = card.GetModel();
    if (!model.IsValid())
    {
      return false;
    }

    if (searchString.IsNullOrEmpty()) return true;
    string stringForSearch = model.GetTitle().ToLower() + " " + model.GetDescription().ToLower();
    return stringForSearch.Contains(searchString.ToLower());
  }

  void UpdateCards()
  {
    string searchString = cardLibraryUI.inputField.text;
    string category = categoryFilterOptions[cardLibraryUI.categoryDropdown.value].text;
    foreach (var containerObj in cardContainerObjects)
    {
      Card card = containerObj.GetComponentInChildren<CardContainer>().GetCard();
      bool active = false;
      if (category == ALL_CATEGORIES_OPTION || (card != null && card.GetModel().GetCategories().Contains(category)))
      {
        if (card != null && DoesCardMatchSearch(card, searchString))
        {
          if (!(selection != null && card.GetModel().IsBuiltin()))
          {
            active = true;
          }
        }
      }
      containerObj.SetActive(active);
    }
  }

  void OnInputFieldChanged(string s)
  {
    UpdateCards();
  }

  void ClearSearch()
  {
    cardLibraryUI.inputField.text = "";
    UpdateCards();
  }

  Coroutine showRoutine;
  IEnumerator ShowRoutine()
  {
    float percent = 0;
    while (percent < 1)
    {
      percent = Mathf.Clamp01(percent + Time.unscaledDeltaTime * 3f);
      canvasGroup.alpha = percent;
      yield return null;
    }
    CompleteShow();
  }

  void Show()
  {
    CompleteShow();
    // canvasGroup.alpha = 0;
    // if (showRoutine != null) StopCoroutine(showRoutine);
    // showRoutine = StartCoroutine(ShowRoutine());
  }

  void CompleteShow()
  {
    canvasGroup.alpha = 1;
    canvasGroup.interactable = true;
    darkBackground.SetActive(true);

  }

  void Hide()
  {
    if (showRoutine != null) StopCoroutine(showRoutine);
    canvasGroup.alpha = 0;
    canvasGroup.interactable = false;
    darkBackground.SetActive(false);
  }

  void OnDisable()
  {
    StopAllCoroutines();
  }

  void HideAllCards()
  {
    foreach (var containerObj in cardContainerObjects)
    {
      containerObj.SetActive(false);
    }
  }

  void StartSelection(System.Action<IEnumerable<string>> selectionFinishedCallback)
  {
    StartSelection(selectionFinishedCallback, Util.Maybe<ulong>.CreateEmpty());
  }

  void StartSelection(System.Action<IEnumerable<string>> selectionFinishedCallback, Util.Maybe<ulong> workshopId)
  {
    BehaviorSystem.SavedCardPack pack = null;
    if (!workshopId.IsEmpty())
    {
      pack = behaviorSystem.GetCardPack(workshopId.Get());
    }
    this.selection = new Selection(selectionFinishedCallback);
    cardLibraryUI.selectionModePrompt.SetActive(true);
    foreach (GameObject containerObj in cardContainerObjects)
    {
      StartSelectionForContainerObj(containerObj, pack);
    }
  }

  private void StartSelectionForContainerObj(GameObject containerObj, BehaviorSystem.SavedCardPack existingPack = null)
  {
    CardContainer container = containerObj.GetComponentInChildren<CardContainer>();
    if (container.GetCard().GetModel().IsBuiltin())
    {
      containerObj.SetActive(false);
    }
    else
    {
      bool startSelected = existingPack != null && existingPack.uris.Contains(container.GetCard().GetModel().GetUri());
      container.StartSelectionMode(
        (selected) =>
        {
          if (selection == null) return;
          if (selected)
          {
            selection.selectedCards.Add(container.GetCard().GetModel().GetUri());
          }
          else
          {
            selection.selectedCards.Remove(container.GetCard().GetModel().GetUri());
          }
        }, startSelected);
    }
  }

  private void EndSelection()
  {
    if (selection == null) return;
    foreach (GameObject containerObj in cardContainerObjects)
    {
      CardContainer container = containerObj.GetComponentInChildren<CardContainer>();
      container.EndSelectionMode();
    }
    cardLibraryUI.selectionModePrompt.SetActive(false);
    selection = null;
    UpdateCards();
  }

#if USE_STEAMWORKS
  private void OnFinishSelectionForWorkshop(IEnumerable<string> result)
  {
    OnFinishSelectionForWorkshop(result, Util.Maybe<ulong>.CreateEmpty());
  }

  private void OnFinishSelectionForWorkshop(IEnumerable<string> result, Util.Maybe<ulong> workshopId)
  {
    if (result.Count() == 0) return;
    uploadDialog.Open(result, workshopId);
  }
#endif

  private void OnFinishSelectionForLocal(IEnumerable<string> result)
  {
#if USE_FILEBROWSER
    var paths = Crosstales.FB.FileBrowser.OpenFolders("Select save location", null);
    if (paths.Count() == 0) return;
    string path = paths.First();
    if (path.IsNullOrEmpty()) return;
    behaviorSystem.WriteEmbeddedBehaviorsToDirectory(result, Path.Combine(path, "cardPack_" + System.DateTime.Now.ToString("yyyyMMddTHHmm")));
    popups.Show("Cards exported!", "Ok");
#else
    behaviorSystem.WriteEmbeddedBehaviorsToDirectory(result, Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GBCards", "cardPack_" + System.DateTime.Now.ToString("yyyyMMddTHHmm")));
    popups.Show("Cards exported to Documents/GBCards folder.\nFor more control over export location, build with the free CrossTales FileBrowser plugin.", "Ok");
#endif
  }

  [CommandTerminal.RegisterCommand(Help = "Import cards by path")]
  static void ImportCards(CommandTerminal.CommandArg[] args)
  {
    CardLibrary inst = GameObject.FindObjectOfType<CardLibrary>();
    if (inst == null)
    {
      GameBuilderConsoleCommands.Log($"Please open the card library (via Logic Tool) before using this command.");
      return;
    }
    string path = GameBuilderConsoleCommands.JoinTailToPath(args);
    if (path != null)
    {
      inst.ImportLocalByPath(new string[] { path });
    }
  }

  public void ImportLocalByPath(string[] paths)
  {
    string path = paths.First();
    if (path.IsNullOrEmpty()) return;
    OnBehaviorsLoaded(BehaviorSystem.LoadEmbeddedBehaviorsFromDirectory(path));
  }

  private void ImportLocal()
  {
#if USE_FILEBROWSER
    var paths = Crosstales.FB.FileBrowser.OpenFolders("Open folder", null);
    if (paths.Count() == 0) return;
    string path = paths.First();
    if (path.IsNullOrEmpty()) return;
    OnBehaviorsLoaded(BehaviorSystem.LoadEmbeddedBehaviorsFromDirectory(path));
#else
    popups.Show("Use the console command 'importcards C:\\path\\to\\cardfolder' instead. Or, build with the free CrossTales FileBrowser plugin.", "OK");
#endif
  }

  private void OnBehaviorsLoaded(Dictionary<string, Behaviors.Behavior> behaviors)
  {
    if (behaviors.Count == 0)
    {
      popups.Show("No cards found.", "Ok");
      return;
    }
    bool containsOverrides = false;
    foreach (var entry in behaviors)
    {
      if (behaviorSystem.EmbeddedBehaviorExists(entry.Key))
      {
        containsOverrides = true;
      }
    }
    if (containsOverrides)
    {
      popups.ShowThreeButtons(
        "Some cards already exist in the library.",
        "Overwrite", () =>
        {
          behaviorSystem.PutBehaviors(behaviors, true);
          popups.Show($"{behaviors.Count} cards imported!", "Ok");
        },
        "Duplicate", () =>
        {
          behaviorSystem.PutBehaviors(behaviors);
          popups.Show($"{behaviors.Count} cards imported!", "Ok");
        },
        "Cancel", () => { });
    }
    else
    {
      behaviorSystem.PutBehaviors(behaviors);
      popups.Show($"{behaviors.Count} cards imported!", "Ok");
    }
  }
}
