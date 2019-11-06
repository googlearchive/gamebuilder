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
using UnityEngine.UI;

public class HierarchyActorEntry : MonoBehaviour, IDragHandler
{
  [SerializeField] RectTransform rect;
  [SerializeField] TMPro.TextMeshProUGUI actorNameField;
  [SerializeField] GameObject disabledOverlay;
  [SerializeField] GameObject highlightBackground;
  [SerializeField] GameObject dragHint;
  [SerializeField] Button mainButton;

  public event System.Action<VoosActor> onDrag;
  private bool readyToDrag = false;

  public enum ActionType
  {
    SELECT,
    INFO,
    MOVE
  };

  // WARNING: this can be null! A null actor indicates that this is the "[ NONE ]"
  // entry that appears in a dialog box (that allows the user to specify that they
  // don't want an actor property to be filled in).
  private VoosActor __actor;
  private VoosActor actor
  {
    get { return __actor; }
    set
    {
      if (__actor != null)
      {
        __actor.onLockChanged -= UpdateActorLocked;
      }
      __actor = value;
      if (__actor != null)
      {
        __actor.onLockChanged += UpdateActorLocked;
      }
    }
  }

  private int indent;
  private bool actionButtonsEnabled;
  private bool disableLockedActors;

  public delegate void OnActorEntryClicked(HierarchyActorEntry entry, ActionType type);
  private OnActorEntryClicked onClick = (e, t) => { };

  void Awake()
  {
    mainButton.onClick.AddListener(() => onClick(this, ActionType.SELECT));
    SetHighlighted(false);
  }

  void OnDestroy()
  {
    Clear();
  }

  public void Clear()
  {
    actor = null;

    // Other state that should be cleared?
  }

  public void SetActor(VoosActor actor, int indent = 0)
  {
    Clear();
    this.actor = actor;
    this.indent = indent;
    UpdateUi();
    UpdateActorLocked();
  }

  public void SetHighlighted(bool isHighlighted)
  {
    highlightBackground.SetActive(isHighlighted);
  }

  public void SetDragging(bool dragging)
  {
    actorNameField.alpha = dragging ? 0.2f : 1;
  }

  public void SetDragInHint(bool hint)
  {
    dragHint.SetActive(hint);
  }

  public VoosActor GetActor()
  {
    return actor;
  }

  public string GetActorName()
  {
    return actor != null ? actor.GetName() : null;
  }

  public void AddClickListener(OnActorEntryClicked listener)
  {
    onClick += listener;
  }

  public void RemoveClickListener(OnActorEntryClicked listener)
  {
    onClick -= listener;
  }

  public void EnableActionButtons(bool enabled)
  {
    actionButtonsEnabled = enabled;
    UpdateUi();
  }

  public void SetDisableLockedActors(bool disableLockedActors)
  {
    this.disableLockedActors = disableLockedActors;
    UpdateUi();
  }

  private void UpdateUi()
  {
    bool isOffstage = actor != null && actor.GetIsOffstageEffective();
    string indentString = indent > 0 ? new string(' ', indent * 2) : "";  // weeeeird, right?

    if (actor != null && actor.GetCloneParentActor() != null)
    {
      indentString += "(copy) ";
      actorNameField.alpha = 0.7f;
    }
    else
    {
      actorNameField.alpha = 1f;
    }

    actorNameField.text = indentString + (actor != null ? actor.GetDisplayName() : "[ NONE ]");
    UpdateActorLocked();
  }

  private void UpdateActorLocked()
  {
    string message;
    bool shouldLock = ShouldLockActor(out message);
    if (shouldLock)
    {
      disabledOverlay.SetActive(true);
      ItemWithTooltip itemWithTooltip = disabledOverlay.GetComponent<ItemWithTooltip>();
      itemWithTooltip.SetDescription(message);
    }
    else
    {
      disabledOverlay.SetActive(false);
    }
  }

  private bool ShouldLockActor(out string message)
  {
    if (!disableLockedActors)
    {
      message = null;
      return false;
    }

    // True, this doesn't prevent the race condition if two players try to
    // request-lock the same actor. Basically one will win, and the other will
    // forever *think* they have it, but not really. And with copy-groups, this
    // problem becomes more likely. We'll ignore it for now, but in the future
    // possible solutions are: 1) make all tools poll for lock (and self-close
    // if they didn't actually get it), 2) have a centralized lock arbiter and
    // hopefully hide any edit latency, if possible.
    if (actor != null && actor.IsLockedByAnother())
    {
      if (actor.IsCloneParentLockedByAnother())
      {
        VoosActor parent = actor.GetCloneParentActor();
        message = $"LOCKED\nThis is a copy, and {parent.GetOwnerNickName()} is editing the original";
        return true;
      }
      else
      {
        message = $"LOCKED\n{actor.GetOwnerNickName()} is editing this";
        return true;
      }
    }
    message = null;
    return false;
  }

  public RectTransform GetRect()
  {
    return rect;
  }

  public void OnDrag(PointerEventData data)
  {
    if (RectTransformUtility.RectangleContainsScreenPoint(
      rect, RectTransformUtility.WorldToScreenPoint(null, data.pressPosition)))
    {
      onDrag?.Invoke(GetActor());
    }
  }

}