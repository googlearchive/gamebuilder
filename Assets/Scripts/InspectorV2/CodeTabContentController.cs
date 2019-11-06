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

public class CodeTabContentController : MonoBehaviour
{

  const float CardContainerScale = 1f / .6f;

  [SerializeField] RectTransform codeContent;
  [SerializeField] EditableCard cardPrefab;
  [SerializeField] RectTransform cardContainerReferenceRect;
  [SerializeField] RectTransform cardContainer;
  [SerializeField] RectTransform browserSizeReference;

  [SerializeField] TMPro.TextMeshProUGUI saveButtonText;
  [SerializeField] UnityEngine.UI.Button saveButton;
  [SerializeField] UnityEngine.UI.Image saveButtonImage;
  [SerializeField] UnityEngine.UI.Button zoomInButton;
  [SerializeField] UnityEngine.UI.Button zoomOutButton;

  [SerializeField] Color activeButtonColor;
  [SerializeField] Color disabledButtonColor;
  [SerializeField] Color buttonTextColor;

  [SerializeField] GameObject browserPrefab;

  InputControl inputControl;

  // Full lifetime state
  private GameObject browserInstance;
  private CodeBrowserWrapper codeBrowser;
  private CodeEditorController codeEditor;
  private BehaviorSystem behaviorSystem;

  // Per population state
  private ICardModel card;
  private EditableCard editableCardUi;

  private bool __isUnsavedNewCard = false;
  private bool isUnsavedNewCard
  {
    get { return __isUnsavedNewCard; }
    set { __isUnsavedNewCard = value; }
  }

  private bool hasCardChanges = false;


  public void Setup()
  {
    Util.FindIfNotSet(this, ref inputControl);
    Util.FindIfNotSet(this, ref behaviorSystem);

    // This has a canvas on it, which is why it's not just a nested prefab.
    browserInstance = Instantiate(browserPrefab);
    codeBrowser = browserInstance.GetComponentInChildren<CodeBrowserWrapper>();
    Debug.Assert(codeBrowser != null, "CodeBrowser component not found in browser prefab");
    codeEditor = browserInstance.GetComponentInChildren<CodeEditorController>();
    codeEditor.onRecompiled += () => { editableCardUi?.OnCardCodeChanged(); };

    // Turn the browser off after it has had 5 seconds to "warm up"
    Invoke("DelayedCloseBrowser", 5f);

    saveButton.onClick.AddListener(SaveOrRun);

    zoomInButton.onClick.AddListener(codeEditor.ZoomIn);
    zoomOutButton.onClick.AddListener(codeEditor.ZoomOut);
    gameObject.SetActive(false);
  }

  void DelayedCloseBrowser()
  {
    if (!gameObject.activeSelf)
    {
      browserInstance.SetActive(false);
    }
  }

  void SaveOrRun()
  {
    editableCardUi.CommitChanges();
    codeEditor.RequestCodeCompile();
    hasCardChanges = false;
    isUnsavedNewCard = false;
  }

  void OnDestroy()
  {
    Destroy(browserInstance);
  }

  public void Close()
  {
    browserInstance.SetActive(false);
    gameObject.SetActive(false);
    Depopulate();
    codeBrowser.GetCodeBrowser().Show(false);
  }

  public void Open(string selectedCardUri, bool isNewCard, VoosEngine.BehaviorLogItem? error = null)
  {
    gameObject.SetActive(true);
    browserInstance.SetActive(true);
    UpdateBrowserSize();
    Populate(selectedCardUri, isNewCard, error);
    codeBrowser.GetCodeBrowser().Show(true);
  }

  public void RequestDestroy()
  {
    Destroy(browserInstance);
    Destroy(gameObject);
  }

  public bool OnMenuRequest()
  {
    return KeyLock();
  }

  void Depopulate()
  {
    codeEditor.Clear();

    if (this.card != null)
    {
      if (isUnsavedNewCard)
      {
        // Not saved - discard it for now.
        behaviorSystem.DeleteBehavior(this.card.GetId());
      }
      this.card = null;
    }

    isUnsavedNewCard = false;
    hasCardChanges = false;
  }

  private void Populate(
    string selectedCardUri, bool isUnsavedNewCard, VoosEngine.BehaviorLogItem? error = null)
  {
    Depopulate();

    this.isUnsavedNewCard = isUnsavedNewCard;
    this.hasCardChanges = false;

    this.card = new BehaviorCards.UnassignedCard(new UnassignedBehavior(
        selectedCardUri, behaviorSystem));

    CreateCardUI();
    editableCardUi.Populate(this.card);
    codeEditor.Set(this.card.GetUnassignedBehaviorItem(), error);
  }

  void CreateCardUI()
  {
    if (editableCardUi == null)
    {
      editableCardUi = Instantiate(cardPrefab, cardContainer);
      editableCardUi.Setup();

      editableCardUi.onChangesToCommit += () =>
      {
        hasCardChanges = true;
      };
    }
  }

  private void UpdateBrowserSize()
  {
    if (!browserInstance.activeSelf)
    {
      return;
    }
    codeEditor.FitRect(browserSizeReference);
  }

  void Update()
  {
    if (inputControl.GetButtonDown("Save"))
    {
      SaveOrRun();
    }

    UpdateButtons();
    UpdateBrowserSize();
    UpdateCardWindowSize();
  }

  void UpdateCardWindowSize()
  {
    cardContainer.sizeDelta = cardContainerReferenceRect.rect.size * CardContainerScale;
  }

  public bool KeyLock()
  {
    if (codeBrowser.GetCodeBrowser().KeyboardHasFocus()) return true;
    GameObject selected = EventSystem.current?.currentSelectedGameObject;
    // The less hacky way would be for each tab to implement this function individually. But YAGNI.
    // Equals comparison is needed for when object is destroyed.
    return selected != null && !selected.Equals(null) && selected.GetComponent<TMPro.TMP_InputField>() != null;
  }

  void UpdateSaveButton()
  {
    if ((isUnsavedNewCard && !hasCardChanges && !codeEditor.HasUnsavedChanges())
        || card.GetUnassignedBehaviorItem().IsBehaviorReadOnly())
    {
      saveButton.gameObject.SetActive(false);
      return;
    }

    saveButton.gameObject.SetActive(true);
    var saveUtil = new ButtonUtil()
    {
      button = saveButton,
      buttonImage = saveButtonImage,
      label = saveButtonText,
      controller = this
    };

    if (isUnsavedNewCard)
    {
      saveUtil.Enable("SAVE NEW (CTRL-S)");
    }
    else
    {
      // We're really kinda overloading the meaning of this button, but oh well.
      if (codeEditor.HasUnsavedChanges())
      {
        saveUtil.Enable("SAVE & RUN (CTRL-S)");

        // Flash...
        saveButtonText.color = Util.BoolWave(Time.unscaledTime, 0.35f) ? Color.Lerp(buttonTextColor, Color.clear, .4f) : buttonTextColor;
      }
      else if (hasCardChanges)
      {
        saveUtil.Enable("SAVE (CTRL-S)");
      }
      else
      {
        saveUtil.Disable("(No changes)");
      }
    }
  }


  void UpdateButtons()
  {
    UpdateSaveButton();
  }

  struct ButtonUtil
  {
    public UnityEngine.UI.Button button;
    public UnityEngine.UI.Image buttonImage;
    public TMPro.TextMeshProUGUI label;
    public CodeTabContentController controller;

    public void Enable(string text)
    {
      button.interactable = true;
      buttonImage.color = controller.activeButtonColor;
      label.text = text;
    }

    public void Disable(string text)
    {
      button.interactable = false;
      buttonImage.color = controller.disabledButtonColor;
      label.text = text;
    }
  }
}