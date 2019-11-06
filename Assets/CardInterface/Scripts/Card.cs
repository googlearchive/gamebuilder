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

using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IScrollHandler
{
  [SerializeField] protected CardUI cardUI;
  [SerializeField] RectTransform cardAssetRect;
  [SerializeField] RectTransform cardShadowRect;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] BehaviorUX.CardPropertiesUX props;
  [SerializeField] GameObject customFlag;

  [SerializeField] Card cardPrefab;
  [SerializeField] GameObject deletionHint;
  [SerializeField] UnityEngine.UI.Image flashImage;

  [SerializeField] GameObject debugPanel;
  [SerializeField] TMPro.TMP_Text debugText;
  [SerializeField] Sprite defaultSprite;
  [SerializeField] Sprite loadingSprite;

  public CardContainer container;
  public RectTransform rectTransform;
  private IconLoader iconLoader;

  // TODO: unified event listening
  public System.Action OnCardPlaced;

  public System.Action OnClick;

  const float DRAG_ROTATION_AMOUNT = 5;

  public enum EventType
  {
    POINTER_DOWN,
    POINTER_UP,
    BEGIN_DRAG,
  }

  public delegate void CardListener(Card card, EventType type, PointerEventData data);
  private CardListener cardListener = (c, t, d) => { };

  public delegate void OnPropChanged(BehaviorProperties.PropType type);
  public event OnPropChanged onPropChanged;

  const float FOCUS_X_OFFSET = 300;
  const float FOCUS_SCALE = 1.3f;
  const float OFFSET_DEFAULT = 0;
  const float OFFSET_DRAG = 5;
  const float STATUS_CHECK_PERIOD = 1f;

  private float flashAmt = 0;
  private float lastErrorCheck = 0;
  private float lastStatusCheck = 0;
  private RuntimeCardStatus lastCardStatus;

  [System.Serializable]
  struct RuntimeCardStatus
  {
    public string title;
    public string description;
    public string errorMessage;
    public string debugText;
  }

  // Data model for a card - NOT assigned.


  internal void RequestDestroy()
  {
    Destroy(gameObject);
  }

  protected ICardModel card;
  protected ICardAssignmentModel assignment;

  public bool IsCardValid()
  {
    return card != null && card.IsValid();
  }

  public bool IsAssignmentValid()
  {
    return assignment != null && assignment.IsValid();
  }

  public enum Side
  {
    Front,
    Back
  }

  internal bool OnEscape()
  {
    return props.OnEscape();
  }

  public bool HasAnyProps()
  {
    return props.HasAnyProps();
  }

  public void SetTransparency(float f)
  {
    canvasGroup.alpha = f;
  }

  public void SetDeletionHintVisible(bool visible)
  {
    deletionHint.SetActive(visible);
  }

  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition)
   || RectTransformUtility.RectangleContainsScreenPoint(container.rectTransform, Input.mousePosition);
  }

  public ICardModel GetModel()
  {
    return card;
  }

  public virtual void Populate(ICardModel card, bool withDetail = false)
  {
    if (card == null || !card.IsValid())
    {
      return;
    }

    // Debug
    this.name = $"Card '{card.GetTitle()}'";

    this.card = card;
    this.assignment = null;
    cardUI.nameField.text = card.GetTitle();
    cardUI.descriptionField.text = card.GetDescription();
    if (withDetail)
    {
      props.SetupPreview(card.GetUnassignedBehaviorItem());
    }
    customFlag.SetActive(!card.IsBuiltin());
    ReloadCardImage();
  }

  protected void ReloadCardImage()
  {
    LoadImageOnto(card.GetImagePath(), cardUI.imageField);
  }

  private void LoadImageOnto(string imagePath, UnityEngine.UI.Image targetImage)
  {
    targetImage.sprite = loadingSprite;
    if (imagePath == null)
    {
      targetImage.sprite = defaultSprite;
    }
    else if (imagePath.StartsWith("icon:"))
    {
      Util.FindIfNotSet(this, ref iconLoader);
      iconLoader.LoadIconSprite(imagePath.Substring("icon:".Length),
        (iconName, sprite) => targetImage.sprite = sprite,
        (iconName) => targetImage.sprite = defaultSprite);
    }
    else
    {
      targetImage.sprite = (Resources.Load(imagePath, typeof(Sprite)) as Sprite) ?? defaultSprite;
    }
  }

  // Needed to update properties so they reflect the code.
  public void OnCardCodeChanged()
  {
    if (IsAssignmentValid())
    {
      props.Setup(this.assignment.GetAssignedBehavior().GetProperties());
    }
    else if (IsCardValid())
    {
      props.SetupPreview(this.card.GetUnassignedBehaviorItem());
    }
  }

  public void Populate(ICardAssignmentModel assignment, bool withDetail = false)
  {
    if (assignment == null || !assignment.IsValid())
    {
      return;
    }

    this.Populate(assignment.GetCard());
    this.assignment = assignment;

    if (withDetail)
    {
      props.Setup(assignment.GetAssignedBehavior().GetProperties());
      props.onValueChanged += (fd) =>
      {
        onPropChanged?.Invoke(fd);
        MaybeUpdateRuntimeCardStatus(force: true);
      };
    }
  }

  public ICardAssignmentModel GetCardAssignment()
  {
    return assignment;
  }

  protected virtual void Awake()
  {
    Util.FindIfNotSet(this, ref iconLoader);
  }

  protected virtual void Update()
  {
    if (!IsCardValid())
    {
      cardUI.descriptionField.text = "(Card is missing)";
      return;
    }

    MaybeUpdateRuntimeCardStatus();

    Color flashColor = flashImage.color;
    flashColor.a = flashAmt;
    flashImage.color = flashColor;
    flashAmt = Mathf.Lerp(flashAmt, 0, 0.5f);
  }

  private void MaybeUpdateRuntimeCardStatus(bool force = false)
  {
    // Only assigned cards show a status panel.
    if (!IsAssignmentValid()) return;

    bool mustUpdate = force || (Time.unscaledTime - lastStatusCheck > STATUS_CHECK_PERIOD);
    if (debugPanel.activeSelf)
    {
      if (!Input.GetKey(KeyCode.LeftAlt))
      {
        debugPanel.SetActive(false);
      }
    }
    else
    {
      if (Input.GetKey(KeyCode.LeftAlt))
      {
        debugPanel.SetActive(true);
        mustUpdate = true;
      }
    }

    if (mustUpdate)
    {
      FetchCardStatus();
      string desc = lastCardStatus.description;
      if (!string.IsNullOrEmpty(lastCardStatus.errorMessage))
      {
        desc += $"\n<color={ (IsCardErrorBlinkOn() ? "red" : "yellow") }>{lastCardStatus.errorMessage}</color></b>";
      }
      cardUI.nameField.text = lastCardStatus.title;
      cardUI.descriptionField.text = desc;
      debugText.text = lastCardStatus.debugText;
      lastStatusCheck = Time.unscaledTime;
    }
  }

  bool IsCardErrorBlinkOn()
  {
    return Time.unscaledTime % 2 > 1;
  }

  private string GetCardErrorMessage_LEGACY()
  {
    if (!IsAssignmentValid())
    {
      return null;
    }
    else
    {
      return this.assignment.GetAssignedBehavior().
        CallScriptFunction<int, string>("getCardErrorMessage", 0).GetOr("");
    }
  }

  private void FetchCardStatus()
  {
    lastCardStatus = new RuntimeCardStatus();
    if (IsAssignmentValid())
    {
      Util.Maybe<RuntimeCardStatus> maybeStatus = this.assignment.GetAssignedBehavior().
        CallScriptFunction<int, RuntimeCardStatus>("getCardStatus", 0);
      if (!maybeStatus.IsEmpty())
      {
        lastCardStatus = maybeStatus.Get();
      }
    }
    else
    {
      lastCardStatus = new RuntimeCardStatus();
    }
    // Fill in with defaults for nulls or empty strings:
    lastCardStatus.title = string.IsNullOrEmpty(lastCardStatus.title) ? card.GetTitle() : lastCardStatus.title;
    lastCardStatus.description = string.IsNullOrEmpty(lastCardStatus.description) ? card.GetDescription() : lastCardStatus.description;
    lastCardStatus.debugText = string.IsNullOrEmpty(lastCardStatus.debugText) ? "(No debug text)" : lastCardStatus.debugText;
    // For the error message, if there is a legacy getCardErrorMessage function in the card, use it:
    if (string.IsNullOrEmpty(lastCardStatus.errorMessage))
    {
      lastCardStatus.errorMessage = GetCardErrorMessage_LEGACY() ?? "";
    }
  }

  public void DragUpdate(RectTransform dragBounds)
  {
    Vector2 localPos;

    // if (draggingOverContainer != null)
    // {
    //   RectTransformUtility.ScreenPointToLocalPointInRectangle(dragBounds,
    //   Util.FindRectTransformScreenPoint(draggingOverContainer.rectTransform),
    //   null, out localPos);
    // }
    // else if (draggingOverSlot != null)
    // {
    //   RectTransformUtility.ScreenPointToLocalPointInRectangle(dragBounds,
    //   Util.FindRectTransformScreenPoint(draggingOverSlot.GetAddRect()),
    //   null, out localPos);
    // }
    // else
    // {
    //   RectTransformUtility.ScreenPointToLocalPointInRectangle(dragBounds, Input.mousePosition, null, out localPos);
    // }
    RectTransformUtility.ScreenPointToLocalPointInRectangle(dragBounds, Input.mousePosition, null, out localPos);
    bool candidatePresent = draggingOverContainer != null || draggingOverSlot != null;

    if (candidatePresent)
    {
      cardAssetRect.rotation = Quaternion.identity;
      cardShadowRect.rotation = Quaternion.identity;
    }
    else
    {
      cardAssetRect.rotation = Quaternion.Euler(0, 0, DRAG_ROTATION_AMOUNT);
      cardShadowRect.rotation = Quaternion.Euler(0, 0, DRAG_ROTATION_AMOUNT);
    }

    rectTransform.anchoredPosition = localPos;
  }

  void SetDragFeedback(bool value)
  {
    if (value)
    {
      cardAssetRect.rotation = Quaternion.Euler(0, 0, DRAG_ROTATION_AMOUNT);
      cardShadowRect.rotation = Quaternion.Euler(0, 0, DRAG_ROTATION_AMOUNT);
      cardAssetRect.anchoredPosition = new Vector2(-OFFSET_DRAG, OFFSET_DRAG);
    }
    else
    {
      cardAssetRect.rotation = Quaternion.identity;
      cardShadowRect.rotation = Quaternion.identity;
      cardAssetRect.anchoredPosition = new Vector2(-OFFSET_DEFAULT, OFFSET_DEFAULT);
    }
  }

  CardContainer draggingOverContainer;
  public void SetCandidateContainer(CardContainer container)
  {
    draggingOverContainer = container;
  }

  CardDeck draggingOverSlot;
  public void SetCandidateSlot(CardDeck slot)
  {
    draggingOverSlot = slot;
  }

  public void StartDrag(RectTransform parent)
  {
    transform.SetParent(container.transform, true);
    if (container.offsetOnFocus)
    {
      rectTransform.anchoredPosition = new Vector2(FOCUS_X_OFFSET, 0);
    }
    else
    {
      rectTransform.anchoredPosition = Vector2.zero;
    }

    SetDragFeedback(true);
    transform.parent = parent;
  }

  public void EndDrag()
  {
    transform.SetParent(container.transform);
    transform.localScale = Vector3.one;
    rectTransform.anchoredPosition = Vector2.zero;
    draggingOverContainer = null;
    SetDragFeedback(false);
  }

  public void EnterFocus()
  {
    cardUI.outlineObject.SetActive(true);
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;
  }

  public void ExitFocus()
  {
    if (this == null || !this.isActiveAndEnabled)
    {
      return;
    }
    cardUI.outlineObject.SetActive(false);
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
  }

  public void OnScroll(PointerEventData eventData)
  {
    if (container != null)
    {
      GameObject receiver = ExecuteEvents.ExecuteHierarchy(container.gameObject, eventData, ExecuteEvents.scrollHandler);
    }
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    cardListener(this, EventType.POINTER_DOWN, eventData);
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    cardListener(this, EventType.POINTER_UP, eventData);
  }

  public void OnBeginDrag(PointerEventData eventData)
  {
    cardListener(this, EventType.BEGIN_DRAG, eventData);
  }

  public void OnPointerClick(PointerEventData eventData)
  {
    if (!eventData.dragging)
    {
      OnClick?.Invoke();
    }
  }

  public void OnDrag(PointerEventData eventData) { }

  public void OnEndDrag(PointerEventData eventData) { }

  public void AddListener(CardListener listener)
  {
    this.cardListener += listener;
  }

  public CardDeck GetCurrentSlot()
  {
    return this.container.deck;
  }

  internal void SetScale(float newScale)
  {
    transform.localScale = Vector3.one * newScale;
  }

  public void Flash()
  {
    flashAmt = 1;
  }

  void OnEnable()
  {
    //lastErrorCheck = Time.time + UnityEngine.Random.Range(0, ERROR_CHECK_PERIOD);
  }
}
