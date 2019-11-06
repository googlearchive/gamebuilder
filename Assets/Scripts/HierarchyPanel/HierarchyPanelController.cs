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
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

// This is the Hierarchy Panel, the panel that shows the list of actors
// that are on stage and off stage and lets the player select actors.
// (NOT TO BE CONFUSED with the Actor Picker Dialog!)
public class HierarchyPanelController : MonoBehaviour
{
  const int COLLAPSED_WIDTH = 35;
  const string ShowCopiesPrefKey = "SHOW_COPIES";
  const string SortByDistancePrefKey = "SORT_BY_DISTANCE";

  // Container of all the panels. This is what gets activated/deactivated to
  // hide/show everything when we enter/leave edit mode.
  [SerializeField] RectTransform container;
  // The panel to show when we're in expanded state.
  [SerializeField] RectTransform expandedPanel;
  // The widget that shows the list of actors on stage.
  [SerializeField] HierarchyActorList onStageList;
  // The widget that shows the list of actors off stage.
  [SerializeField] HierarchyActorList offStageList;
  // Checkbox for "show copies".
  [SerializeField] Toggle showCopiesToggle;
  [SerializeField] TMPro.TMP_InputField searchInput;
  [SerializeField] Button clearSearchButton;
  // Sort by button.
  [SerializeField] Button sortByButton;
  // Close button.
  [SerializeField] Button closeButton;
  [SerializeField] ActorListItemUI draggingGhost;

  // Are we in expanded mode right now?
  bool expanded;
  bool monitoringActors;

  // References to other modules/services:
  UserMain userMain;
  EditMain editMain;
  ToolMemory toolMemory;
  VoosEngine engine;
  HudManager hudManager;
  UndoStack undoStack;

  // If true, we need to refresh the list.
  bool listNeedsRefresh = false;

  // If true, sort by distance; else, by name.
  bool sortByDistance = false;

  List<VoosActor> selectedActors = new List<VoosActor>();
  // VoosActor selectedActor;

  float timeSinceLastRefresh;

  private System.Action<VoosActor> selectCallback;

  private VoosActor draggingActor;

  void Awake()
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref toolMemory);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref hudManager);
    Util.FindIfNotSet(this, ref undoStack);
    SetExpanded(false);
    editMain.targetActorsChanged += TargetActorsUpdatedExternally;
    onStageList.AddClickListener(OnActorEntryClicked);
    onStageList.onActorDrag += OnActorDrag;
    offStageList.AddClickListener(OnActorEntryClicked);
    offStageList.onActorDrag += OnActorDrag;
    sortByDistance = PlayerPrefs.GetInt(SortByDistancePrefKey, 0) != 0;
    sortByButton.GetComponentInChildren<TMPro.TMP_Text>().text = sortByDistance ? "Sort: distance" : "Sort: name";
    sortByButton.onClick.AddListener(OnSortButtonClicked);
    showCopiesToggle.isOn = PlayerPrefs.GetInt(ShowCopiesPrefKey, 1) != 0;
    showCopiesToggle.onValueChanged.AddListener(v =>
    {
      PlayerPrefs.SetInt(ShowCopiesPrefKey, v ? 1 : 0);
      RefreshActorList();
    });
    searchInput.onValueChanged.AddListener(s =>
    {
      RefreshActorList();
      clearSearchButton.gameObject.SetActive(!s.IsNullOrEmpty());
    });
    clearSearchButton.onClick.AddListener(() =>
    {
      searchInput.text = "";
    });
    closeButton.onClick.AddListener(OnCloseButtonClicked);
  }
  public bool IsExpanded()
  {
    return expanded;
  }

  public void SetExpanded(bool expanded)
  {
    this.expanded = expanded;
    if (!expanded)
    {
      SetDraggingActor(null);
    }
    UpdateUi();
  }

  void UpdateUi()
  {
    hudManager.UpdateHorizontalRightOffset(expanded ? expandedPanel.rect.width : 0);
    if (expanded)
    {
      // Start monitoring for actor changes that are interesting to us.
      RefreshActorList();
      StartMonitoringActors();
    }
    else
    {
      // Stop monitoring for actor changes.
      StopMonitoringActors();
    }
    expandedPanel.gameObject.SetActive(expanded);
  }

  private void StartMonitoringActors()
  {
    if (monitoringActors) return;
    monitoringActors = true;
    engine.onActorCreated += OnActorCreated;
    engine.onBeforeActorDestroy += OnActorDestroyed;
    foreach (VoosActor actor in engine.EnumerateActors())
    {
      StartMonitoringActor(actor);
    }
  }

  private void StartMonitoringActor(VoosActor actor)
  {
    actor.AddDisplayNameChangedListener(OnSomeActorChanged);
    actor.offstageChanged += OnSomeActorChanged;
  }

  private void StopMonitoringActors()
  {
    if (!monitoringActors) return;
    engine.onActorCreated -= OnActorCreated;
    engine.onBeforeActorDestroy -= OnActorDestroyed;
    foreach (VoosActor actor in engine.EnumerateActors())
    {
      StopMonitoringActor(actor);
    }
    monitoringActors = false;
  }

  private void StopMonitoringActor(VoosActor actor)
  {
    actor.RemoveDisplayNameChangedListener(OnSomeActorChanged);
    actor.offstageChanged -= OnSomeActorChanged;
  }

  /*  bool ActorListsAreDifferent()
   {
     return true;
     ////int deltaA = selectedActors.Except(editMain.GetTargetActors()).Count();
     //int deltaB = editMain.GetTargetActors().Except(selectedActors).Count();
     //return deltaA + deltaB != 0;
   } */

  void TargetActorsUpdatedExternally(IEnumerable<VoosActor> actors)
  {
    selectedActors.Clear();
    selectedActors.AddRange(actors);
    listNeedsRefresh = true;
  }

  void Update()
  {
    timeSinceLastRefresh += Time.deltaTime;

    if (!expanded) return;


    if (listNeedsRefresh)
    {
      RefreshActorList();
    }

    if (sortByDistance)
    {
      // Actors move, the player moves, everything changes, so refresh the list
      // every 2 seconds or so to keep it up to date.
      listNeedsRefresh = timeSinceLastRefresh > 2;
    }

    if (draggingActor != null)
    {
      UpdateDrag();
    }
  }

  void UpdateDrag()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      container, Input.mousePosition, null, out localPos);
    draggingGhost.container.anchoredPosition = localPos;

    VoosActor onstageHint;
    bool onstageUnderMouse = onStageList.IsDragTarget(out onstageHint);
    if (onstageHint == draggingActor) onstageHint = null;

    VoosActor offstageHint;
    bool offstageUnderMouse = offStageList.IsDragTarget(out offstageHint);
    if (offstageHint == draggingActor) offstageHint = null;

    if (Input.GetMouseButtonUp(0))
    {
      if (onstageUnderMouse)
      {
        if (onstageHint != null)
        {
          if (VoosActor.IsValidParent(draggingActor, onstageHint))
          {
            draggingActor.SetTransformParent(onstageHint.GetName());
            draggingActor.SetSpawnTransformParent(onstageHint.GetName());
          }
        }
        else
        {
          draggingActor.SetPreferOffstage(false);
          draggingActor.SetTransformParent(null);
          draggingActor.SetSpawnTransformParent(null);
        }
      }
      else if (offstageUnderMouse)
      {
        if (offstageHint != null)
        {
          if (VoosActor.IsValidParent(draggingActor, offstageHint))
          {
            draggingActor.SetTransformParent(offstageHint.GetName());
            draggingActor.SetSpawnTransformParent(offstageHint.GetName());
          }
        }
        else
        {
          draggingActor.SetPreferOffstage(true);
          draggingActor.SetTransformParent(null);
          draggingActor.SetSpawnTransformParent(null);
        }
      }
      SetDraggingActor(null);
    }
  }

  void OnSomeActorChanged()
  {
    // We can't just update the actor that changed, because changes to on/off stage
    // and display name require us to re-sort the lists, etc.
    listNeedsRefresh = true;
  }

  void OnActorCreated(VoosActor actor)
  {
    if (!monitoringActors)
    {
      Debug.LogError("Shouldn't get OnActorCreated when not monitoring actors...");
      return;
    }
    StartMonitoringActor(actor);
    listNeedsRefresh = true;
  }

  void OnActorDestroyed(VoosActor actor)
  {
    if (!monitoringActors)
    {
      Debug.LogError("Shouldn't get OnActorDestroyed when not monitoring actors...");
      return;
    }
    StopMonitoringActor(actor);
    listNeedsRefresh = true;
    if (actor == draggingActor)
    {
      SetDraggingActor(null);
    }
  }

  void OnSortButtonClicked()
  {
    sortByDistance = !sortByDistance;
    PlayerPrefs.SetInt(SortByDistancePrefKey, sortByDistance ? 1 : 0);
    sortByButton.GetComponentInChildren<TMPro.TMP_Text>().text = sortByDistance ? "Sort: distance" : "Sort: name";
    listNeedsRefresh = true;
  }

  void OnCloseButtonClicked()
  {
    SetExpanded(false);
  }

  void RefreshActorList()
  {
    onStageList.SetActors(engine.EnumerateActors().Where(actor => ShouldActorBeListed(false, actor)), sortByDistance);
    onStageList.HighlightActors(selectedActors);
    offStageList.SetActors(engine.EnumerateActors().Where(actor => ShouldActorBeListed(true, actor)));
    offStageList.HighlightActors(selectedActors);
    listNeedsRefresh = false;
    timeSinceLastRefresh = 0;
  }

  private bool ShouldActorBeListed(bool isOffstageList, VoosActor actor)
  {
    return (actor.GetIsOffstageEffective() == isOffstageList) &&
      !actor.GetWasClonedByScript() &&
      (showCopiesToggle.isOn || actor.GetCloneParentActor() == null) &&
      actor.GetDisplayName().ToLower().Contains(searchInput.text.ToLower());
  }

  public void SetSelectCallback(System.Action<VoosActor> callback)
  {
    this.selectCallback = callback;
  }

  void OnActorEntryClicked(HierarchyActorEntry entry, HierarchyActorEntry.ActionType actionType)
  {
    // The user has clicked on an actor in the list.
    VoosActor actor = engine.GetActor(entry.GetActorName());
    if (actor == null)
    {
      // Shouldn't happen.
      Debug.LogError("Clicked on actor that doesn't exist " + entry.GetActorName() + ". Bug?");
      return;
    }
    if (selectCallback != null)
    {
      selectCallback(actor);
      selectCallback = null;
      return;
    }
    switch (actionType)
    {
      case HierarchyActorEntry.ActionType.SELECT:
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
          AddActorToSelection(actor);
        }
        else
        {
          SelectActor(actor);
        }
        break;
      case HierarchyActorEntry.ActionType.INFO:
        SelectActor(actor, true);
        break;
      case HierarchyActorEntry.ActionType.MOVE:
        ToggleActorOffstage(actor);
        break;
    }
  }

  private void AddActorToSelection(VoosActor actor)
  {
    listNeedsRefresh = true;
    selectedActors.Add(actor);
    editMain.TryForceAddingActor(actor);
  }

  private void SelectActor(VoosActor actor, bool forceShowInfo = false)
  {
    if (forceShowInfo)
    {
      editMain.SelectInfoTool();
    }
    listNeedsRefresh = true;
    selectedActors.Clear();
    selectedActors.Add(actor);
    editMain.TryForceSelectingActor(actor);
  }

  private void ToggleActorOffstage(VoosActor toggleActor)
  {
    bool currentOffstage = toggleActor.GetPreferOffstage();
    if (!toggleActor.IsLockedByAnother() && !toggleActor.IsParentedToAnotherActor())
    {
      undoStack.PushUndoForActor(
        toggleActor,
        $"Toggle offstage for {toggleActor.GetDisplayName()}",
        actor =>
        {
          if (!actor.IsParentedToAnotherActor())
          {
            actor.SetPreferOffstage(!currentOffstage);
          }
        },
        actor =>
        {
          if (!actor.IsParentedToAnotherActor())
          {
            actor.SetPreferOffstage(currentOffstage);
          }
        });
      RefreshActorList();
    }
  }

  private void OnActorDrag(VoosActor actor)
  {
    if (draggingActor != null) return;
    SetDraggingActor(actor);
  }

  private void SetDraggingActor(VoosActor actor)
  {
    if (actor == draggingActor) return;
    draggingActor = actor;

    onStageList.SetDraggingActor(draggingActor);
    offStageList.SetDraggingActor(draggingActor);
    draggingGhost.gameObject.SetActive(draggingActor != null);
    if (actor != null)
    {
      draggingGhost.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = actor.GetDisplayName();
    }
  }

}