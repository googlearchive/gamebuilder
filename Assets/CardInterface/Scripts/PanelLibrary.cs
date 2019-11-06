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

public class PanelLibrary : MonoBehaviour, CardManager.PopupPanel
{
  [SerializeField] CardLibraryUI libraryUI;
  [SerializeField] PanelLibraryItem libraryItemPrefab;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] GameObject darkBackground;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform parentRect;
  public RectTransform focusPanelParent;
  public event System.Action<PanelLibraryItem.IModel, bool> onRequestAddPanel;


  PanelManager panelManager;
  CardManager cardManager;
  System.Action onOpen;
  public System.Action onClose;
  BehaviorSystem behaviorSystem;

  float lastPanelAddTime = 0f;

  enum PointerState
  {
    MouseOver,
    PointerDown,
    Dragging
  }


  List<PanelLibraryItem> items = new List<PanelLibraryItem>();

  const float ROW_HEIGHT = 240;
  const float GRID_DIMENSION_X = 245;
  const float GRID_DIMENSION_Y = 175;
  const float GRID_PADDING_X = 10;
  const float GRID_PADDING_EDGE = 265;

  void Awake()
  {
    Util.FindIfNotSet(this, ref panelManager);
    Util.FindIfNotSet(this, ref cardManager);
    Util.FindIfNotSet(this, ref behaviorSystem);
    libraryUI.closeButton.onClick.AddListener(Close);
  }

  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
  }

  void Populate()
  {
    // Setup library
    ClearPanelItems();
    foreach (var panel in cardManager.GetModel().GetPanelLibrary())
    {
      AddPanelItem(panel);
    }
  }

  public void ClearPanelItems()
  {
    foreach (var item in items)
    {
      GameObject.Destroy(item.gameObject);
    }
    items.Clear();
  }

  public void AddPanelItem(PanelLibraryItem.IModel item)
  {
    PanelLibraryItem newItem = Instantiate(libraryItemPrefab, libraryUI.libraryContainer);
    newItem.Setup(item);
    items.Add(newItem);
  }

  internal void OnEndDrag(PanelLibraryItem panelLibraryItem)
  {
    if (!IsOpen()) return;
    lastItemDragged = null;
    AddPanel(panelLibraryItem.model, true);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  internal void OnClick(PanelLibraryItem panelLibraryItem)
  {
    AddPanel(panelLibraryItem.model);
  }

  public void Open()
  {
    gameObject.SetActive(true);
    Show();
    Populate();
    onOpen?.Invoke();
    darkBackground.SetActive(true);
    cardManager.DisablePointerState();
  }

  public void SetOpenAction(System.Action action)
  {
    onOpen = action;
  }

  PanelLibraryItem lastItemDragged = null;
  public void Close()
  {
    if (!IsOpen()) return;
    if (lastItemDragged != null)
    {
      lastItemDragged.ForceEndDrag();
      lastItemDragged = null;
    }

    cardManager.EnablePointerState();

    gameObject.SetActive(false);
    darkBackground.SetActive(false);
  }

  public void OnBeginDrag(PanelLibraryItem item)
  {
    lastItemDragged = item;
    Hide();
  }

  void AddPanel(PanelLibraryItem.IModel libPanel, bool dragOn = false)
  {
    if (Time.unscaledTime - lastPanelAddTime < 0.5f)
    {
      // Weird race conditions can happen in multiplayer, even with no other
      // players, so prevent this from happening too fast.
      return;
    }
    lastPanelAddTime = Time.unscaledTime;

    onRequestAddPanel?.Invoke(libPanel, dragOn);
    Close();
  }

  void Show()
  {
    canvasGroup.alpha = 1;
    canvasGroup.interactable = true;
    darkBackground.SetActive(true);
  }

  void Hide()
  {
    canvasGroup.alpha = 0;
    canvasGroup.interactable = false;
    darkBackground.SetActive(false);
  }


}
