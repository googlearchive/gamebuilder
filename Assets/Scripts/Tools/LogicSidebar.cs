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
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LogicSidebar : Sidebar
{
  //  [SerializeField] RectTransform referenceRect;
  [SerializeField] LogicSidebarUI logicSidebarUI;
  [SerializeField] CardLibraryCanvasHelper libraryPrefab;
  [SerializeField] CardLogicTabController cardTab;
  [SerializeField] CodeTabController codeTab;
  [SerializeField] GameObject selectorToolFab;
  [SerializeField] GameObject cardDragLayerPrefab;

  VoosEngine voosEngine;
  EditMain editMain;
  VoosActor currActor;
  ObjectSelectorTool objectSelectorTool;

  private CardLibraryCanvasHelper libraryHelper;
  private CardLibrary cardLibrary;
  private PanelLibrary panelLibrary;
  private CardDragLayer cardDragLayer;

  bool usingCardView = true;

  public System.Action OnSwitchToCodeView;
  public System.Action OnSwitchToCardView;

  private float lastOwnershipRequest = Mathf.NegativeInfinity;
  const float OWNERSHIP_CHECK_DELAY_S = 0.5f;

  const float MIN_SELECTION_WIDTH = 480;
  const float MAX_SELECTION_WIDTH = 1500;
  const float DEFAULT_SELECTION_WIDTH = 1000;
  const float NO_SELECTION_WIDTH = 180;
  const string LogicSidebarWidthKey = "LogicSidebarWidth";

  private Vector2 startDragMousePosition;
  private bool isDragging;
  private CanvasScaler canvasScaler;
  public System.Action<VoosActor> onOpenActor;


  /* 
    [SerializeField] UnityEngine.UI.Button cardLibraryButton;
    [SerializeField] LogicTabButton logicTabButton;
    [SerializeField] LogicTabButton codeTabButton;
      [SerializeField] TMPro.TextMeshProUGUI actorTitle; */

  public override void Open()
  {
    base.Open();
    libraryHelper.Open();
  }


  public VoosActor GetActiveActor()
  {
    return currActor;
  }

  public bool KeyLock()
  {
    if (cardLibrary.IsOpen() || panelLibrary.IsOpen())
    {
      return false;
    }

    if (usingCardView)
    {
      return cardTab.KeyLock();
    }
    else
    {
      return codeTab.KeyLock();
    }
  }

  public bool OnMenuRequest()
  {
    if (cardLibrary.IsOpen() && cardLibrary.OnMenuRequest())
    {
      return true;
    }
    else if (panelLibrary.IsOpen())
    {
      panelLibrary.Close();
      return true;
    }
    else if (usingCardView)
    {
      return cardTab.OnMenuRequest();
    }
    else
    {
      if (codeTab.IsOpen())
      {
        if (!codeTab.OnMenuRequest())
        {
          SetToCardView();
        }
        return true;
      }
      return false;
    }
  }

  public void OpenWithParams(VoosActor actor)
  {
    RequestOpen();
    SetActor(actor);
  }

  private void SetActor(VoosActor actor)
  {
    SetCurrActor(actor);
    logicSidebarUI.label.text = actor?.GetDisplayName();
    if (usingCardView)
    {
      cardTab.Open(actor);
    }
  }

  public bool IsOpenedToFullWidth()
  {
    return IsOpenedOrOpening() && (currActor != null || IsCodeViewOpen() || cardLibrary.IsOpen());
  }

  void Update()
  {
    rectTransform.SetSizeWithCurrentAnchors(
      RectTransform.Axis.Horizontal, libraryHelper.IsOpen() || currActor != null || !usingCardView ? DEFAULT_SELECTION_WIDTH : NO_SELECTION_WIDTH);
    // libraryHelper.resizingRect.SetSizeWithCurrentAnchors(
    //   RectTransform.Axis.Horizontal, DEFAULT_SELECTION_WIDTH);
    // libraryHelper.resizingRect.SetSizeWithCurrentAnchors(
    //       RectTransform.Axis.Horizontal, canvasScaler.referenceResolution.x - rectTransform.sizeDelta.x);
    foreach (GameObject go in logicSidebarUI.hideWhenCollapsed) go.SetActive(currActor != null);

    //libraryHelper.backgroundObject.SetActive(libraryHelper.IsOpen());
    //update library
    UpdateLibraryButton(libraryHelper.IsOpen());
  }

  //request to hide
  public override void Close()
  {
    base.Close();
    cardTab.Close();
    codeTab.Close();
    libraryHelper.Close();
    if (currActor != null)
    {
      SetActor(null);
    }
    // offStageWorldController.SetHalfScreen(false);
  }

  public void SetToCodeView(string selectedCardUri = null, VoosEngine.BehaviorLogItem? error = null)
  {
    usingCardView = false;
    cardLibrary.Close();
    cardTab.Close();
    codeTab.Open(selectedCardUri, error);
    UpdateButtons();
    OnSwitchToCodeView?.Invoke();
  }

  public bool IsCodeViewOpen()
  {
    return codeTab.IsOpen();
  }

  public void SetToCardView()
  {
    usingCardView = true;
    codeTab.Close();
    cardTab.Open(currActor);
    UpdateButtons();
    OnSwitchToCardView?.Invoke();
  }

  private void UpdateButtons()
  {
    logicSidebarUI.cardButtonImage.color = usingCardView ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.cardButtonText.color = usingCardView ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.cardButtonBackground.color = usingCardView ? logicSidebarUI.primaryColor : logicSidebarUI.darkColor;

    logicSidebarUI.codeButtonImage.color = !usingCardView ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.codeButtonText.color = !usingCardView ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.codeButtonBackground.color = !usingCardView ? logicSidebarUI.primaryColor : logicSidebarUI.darkColor;
  }

  private void UpdateLibraryButton(bool on)
  {
    logicSidebarUI.libraryButtonImage.color = on ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.libraryButtonText.color = on ? logicSidebarUI.darkColor : logicSidebarUI.primaryColor;
    logicSidebarUI.libraryButtonBackground.color = on ? logicSidebarUI.primaryColor : logicSidebarUI.darkColor;
  }

  public override void Setup(SidebarManager _sidebarManager)
  {
    base.Setup(_sidebarManager);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref voosEngine);
    // Util.FindIfNotSet(this, ref offStageWorldController);
    cardTab.Setup();
    codeTab.Setup();
    /*     closeButton.onClick.AddListener(() =>
        {
          RequestClose();
        }); */

    logicSidebarUI.codeButton.onClick.AddListener(() => SetToCodeView());
    logicSidebarUI.cardButton.onClick.AddListener(() => SetToCardView());

    canvasScaler = GetComponentInParent<CanvasScaler>();

    libraryHelper = Instantiate(libraryPrefab, null);
    cardLibrary = libraryHelper.cardLibrary;
    panelLibrary = libraryHelper.panelLibrary;
    // focusCardParent = libraryHelper.focusRect;
    cardLibrary.Setup();
    // cardLibrary.onClose = OnLibraryClose;

    logicSidebarUI.libraryButton.onClick.AddListener(() =>
    {
      if (cardLibrary.IsOpen())
      {
        cardLibrary.Close();
      }
      else
      {
        cardTab.GetManager().CloseCardDetail();
        panelLibrary.Close();
        cardLibrary.Open();
        cardLibrary.SetAddCardToSlotListener(null);
      }
    });
    cardLibrary.onCodeRequest += (uri) =>
    {
      SetToCodeView(uri);
    };

    cardDragLayer = Instantiate(cardDragLayerPrefab).GetComponentInChildren<CardDragLayer>();
    cardDragLayer.Setup(cardLibrary, cardTab.GetManager());

    cardTab.GetManager().onCardLibraryRequest += (category, container, slot) =>
    {
      panelLibrary.Close();
      cardTab.GetManager().CloseCardDetail();
      cardLibrary.Open(category);
      cardLibrary.SetAddCardToSlotListener((card) =>
      {
        cardTab.GetManager().AcceptClickedLibraryCard(card, container, slot);
      }, (card) =>
      {
        return slot.SupportsCard(card);
      });
    };

    cardTab.GetManager().onCardLibraryCancelRequest += () =>
    {
      cardLibrary.SetAddCardToSlotListener(null);
    };

    cardTab.GetManager().onPanelLibraryRequest += () =>
    {
      cardLibrary.Close();
      panelLibrary.Open();
    };

    cardTab.GetManager().onCodeRequest += (uri) =>
    {
      SetToCodeView(uri);
    };

    cardTab.onActorChanged += (actor) =>
    {
      SetCurrActor(actor);
      logicSidebarUI.label.text = actor?.GetDisplayName();
    };

    panelLibrary.onRequestAddPanel += (panel, dragOn) =>
    {
      if (cardTab.IsOpen())
      {
        cardTab.GetManager().AddPanel(panel, dragOn);
      }
    };
  }

  void SetCurrActor(VoosActor actor)
  {
    currActor = actor;
    onOpenActor?.Invoke(actor);
  }
}

