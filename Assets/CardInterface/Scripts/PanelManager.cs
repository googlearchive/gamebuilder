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

public class PanelManager : MonoBehaviour
{
  [SerializeField] RectTransform panelParent;
  [SerializeField] CardPanel cardPanelPrefab;
  [SerializeField] ZoomableRect zoomableRect;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] RectTransform referenceRect;
  [SerializeField] CardManager cardManager;
  [SerializeField] UnityEngine.UI.ContentSizeFitter contentSizeFitter;
  [SerializeField] UnityEngine.UI.HorizontalLayoutGroup layoutGroup;
  private DynamicPopup popups;

  CardPanel draggedPanel;

  const float ZOOM_OUT_X_PADDING = 100;
  const float ZOOM_OUT_Y_PADDING = 200;
  const float ZOOM_OUT_Y_OFFSET = 60;

  List<CardPanel> panels = new List<CardPanel>();

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    zoomableRect.onPan += () =>
    {
      // Do not make pans/zooms undoable.
    };
    zoomableRect.onZoom += () =>
    {
      // Do not make pans/zooms undoable.
    };
  }

  void ShowPanelCanvas()
  {
    canvasGroup.alpha = 1;
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;
  }

  void HidePanelCanvas()
  {
    canvasGroup.alpha = 0;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
  }

  public void SetOrganizedPanels(bool value)
  {
    contentSizeFitter.enabled = value;
    layoutGroup.enabled = value;
  }

  public bool GetOrganizedPanels()
  {
    return contentSizeFitter.enabled;
  }

  public bool HasCustomLayout()
  {
    return !GetOrganizedPanels();
  }

  public void AddPanel(CardPanel.IAssignedPanel data, bool addedFromLibrary, bool draggedOn)
  {
    CardPanel newPanel = Instantiate(cardPanelPrefab, panelParent);
    newPanel.Setup(data, cardManager, this, popups);
    panels.Add(newPanel);
    if (addedFromLibrary)
    {
      if (draggedOn)
      {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(newPanel.parentRect, Input.mousePosition, null, out localPos);
        newPanel.SetPosition(localPos);

        // The user dragged this on, so remember the position. Note that we
        // don't give an undo label. We don't actually want this to be
        // undo-able, otherwise it would take *2* undos to undo the add.
        newPanel.SetUseMetadata(null);
      }
      else
      {
        StartCoroutine(PlaceNewlyAddedPanelRoutine(newPanel, ZoomOutToAllPanels));
      }
    }
  }

  public bool OverAnyPanelsOrButtons()
  {
    foreach (CardPanel panel in panels)
    {
      if (panel.IsMouseOver()) return true;
    }
    // return RectTransformUtility.RectangleContainsScreenPoint(addPanelRect, Input.mousePosition);
    return false;
  }

  public void ZoomOutToAllPanels()
  {
    Vector3 cornerMin, cornerMax;
    FindCornersOfAllPanels(out cornerMin, out cornerMax);
    referenceRect.anchoredPosition = Vector3.Lerp(cornerMax, cornerMin, .5f) + new Vector3(0, ZOOM_OUT_Y_OFFSET, 0);
    referenceRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cornerMax.x - cornerMin.x + ZOOM_OUT_X_PADDING);
    referenceRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cornerMax.y - cornerMin.y + ZOOM_OUT_Y_PADDING);
    zoomableRect.FocusOnRectTransform(referenceRect);
  }

  private void FindCornersOfAllPanels(out Vector3 cornerMin, out Vector3 cornerMax)
  {
    if (panels.Count == 0)
    {
      cornerMin = Vector3.zero;
      cornerMax = Vector3.zero;
      return;
    }

    cornerMin = panels[0].rectTransform.offsetMin;
    cornerMax = panels[0].rectTransform.offsetMax;

    foreach (CardPanel panel in panels)
    {
      cornerMin = Vector2.Min(cornerMin, panel.rectTransform.offsetMin);
      cornerMax = Vector2.Max(cornerMax, panel.rectTransform.offsetMax);
    }
  }

  public void ZoomToSpecificPanel(CardPanel panel)
  {
    zoomableRect.FocusOnRectTransform(panel.GetReferenceRectTransform());
  }

  void Update()
  {
    for (int i = panels.Count - 1; i >= 0; i--)
    {
      if (panels[i] == null) panels.RemoveAt(i);
    }

    if (draggedPanel != null)
    {
      if (GetOrganizedPanels())
      {
        SetOrganizedPanels(false);
      }
      draggedPanel.SetDeleteFeedback(IsPanelOverTrash());
    }
  }

  public void ClearPanels()
  {
    HidePanelCanvas();
    draggedPanel = null;
    foreach (var panel in panels)
    {
      if (panel != null)
      {
        // CardPanel may destroy itself..whatever
        GameObject.Destroy(panel.gameObject);
      }
    }
    panels.Clear();
  }


  IEnumerator PlaceNewlyAddedPanelRoutine(CardPanel panel, System.Action onComplete)
  {
    yield return null;
    PlacePanel(panel);
    yield return null;
    onComplete?.Invoke();
  }

  IEnumerator RefreshPanelsLayoutRoutine(ICardManagerModel model)
  {
    ViewState view = model.GetViewState();

    if (view.IsValid())
    {
      SetOrganizedPanels(!view.customLayout);
    }
    else
    {
      SetOrganizedPanels(true);
    }

    yield return PanelPlacementRoutine(model);

    if (view.IsValid())
    {
      zoomableRect.SetCanvasScale(view.scale);
      zoomableRect.SetAbsolutePosition(view.viewPosition);
    }
    else
    {
      ZoomOutToAllPanels();
    }

    ShowPanelCanvas();
  }

  IEnumerator PanelPlacementRoutine(ICardManagerModel model)
  {
    yield return null;

    foreach (CardPanel panel in panels)
    {
      PlacePanel(panel);
      if (!GetOrganizedPanels())
      {
        // To fix a bug where panels get 0 width + height
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel.rectTransform);
      }
    }
    yield return null;

    if (GetOrganizedPanels())
    {
      foreach (CardPanel panel in panels)
      {
        panel.SetUseMetadata(null);
      }
      // To fix a bug where horizontal layout group is not laid out properly
      UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panelParent);
    }

  }

  void PlacePanel(CardPanel panel, System.Action onComplete = null)
  {
    bool hadMetadata = panel.TrySetupFromMetadata();

    if (!hadMetadata)
    {
      float width = panel.GetReferenceRectTransform().rect.width;
      panel.SetPosition(Vector2.zero);
      /* if (panels.Count == 0)
      {
        panel.SetPosition(Vector2.zero);
      }
      else
      {
        panel.SetPosition(panels[panels.Count - 1].rectTransform.anchoredPosition + new Vector2(panels[panels.Count - 1].GetReferenceRectTransform().rect.width, 0) + new Vector2(width / 2f, 0));
      } */
      //addPanelRect.anchoredPosition += new Vector2(width, 0);

      // Save position metadata, but don't create an undo item. The user didn't
      // do this.
      panel.SetUseMetadata(null);
    }

    onComplete?.Invoke();
  }

  internal void RefreshPanelsLayout(ICardManagerModel model)
  {
    cardManager.LoadPanelNotes(model.GetNotes());
    StartCoroutine(RefreshPanelsLayoutRoutine(model));
  }

  internal void SetupPanelPlacementAndNotes(ICardManagerModel model)
  {
    // ViewState vs = model.GetViewState();
    /* if (vs.IsValid())
    {
      addPanelRect.anchoredPosition = vs.addPanelPosition;
    }
    else
    {
      addPanelRect.anchoredPosition = Vector2.zero;
    } */

    cardManager.LoadPanelNotes(model.GetNotes());
    StartCoroutine(PanelPlacementRoutine(model));
  }

  ///metadata

  [System.Serializable]
  public struct ViewState
  {
    static int CurrentVersion = 1;
    public int version;

    public float scale;
    public Vector2 viewPosition;
    public bool customLayout;

    public ViewState(float scale, Vector2 viewPosition, bool customLayout)
    {
      this.version = CurrentVersion;
      this.scale = scale;
      this.viewPosition = viewPosition;
      this.customLayout = customLayout;
    }

    public bool IsValid() { return version > 0; }
  }

  // NOTE: Does not actually save to the model - just saves to a struct and
  // returns it.
  public ViewState SaveViewState()
  {
    return new ViewState(
      zoomableRect.GetCanvasScale(),
      zoomableRect.GetAbsolutePosition(),
      HasCustomLayout());
  }

  internal void OnPanelBeginDrag(CardPanel cardPanel)
  {
    draggedPanel = cardPanel;
  }

  internal void OnPanelEndDrag(CardPanel cardPanel)
  {
    // Debug.Assert(cardPanel == draggedPanel);
    if (IsPanelOverTrash())
    {
      cardPanel.DeletePanel();
    }
    else
    {
      cardPanel.SetDeleteFeedback(false);
    }
    draggedPanel = null;
  }

  internal void OnRequestCopyPanel(CardPanel panel)
  {
    cardManager.CopyPanel(panel);
  }

  public bool IsPanelBeingDragged()
  {
    return draggedPanel != null;
  }

  private bool IsPanelOverTrash()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(cardManager.trash, Input.mousePosition)
    && draggedPanel != null
    && draggedPanel.GetModel().GetId() != BehaviorCards.GetMiscPanelId();
  }
}
