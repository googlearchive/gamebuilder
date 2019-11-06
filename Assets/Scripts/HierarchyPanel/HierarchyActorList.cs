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
using System.Collections.Generic;
using UnityEngine.UI;

public class HierarchyActorList : MonoBehaviour
{
  [SerializeField] RectTransform container;
  [SerializeField] GameObject dragHintSelfOutline;
  [SerializeField] GameObject entryPrefab;
  [SerializeField] Transform listEntryParent;
  [SerializeField] bool enableActionButtons;
  [SerializeField] bool disableLockedActors;

  List<HierarchyActorEntry> entries = new List<HierarchyActorEntry>();
  HierarchyActorEntry.OnActorEntryClicked onActorEntryClicked;
  EditMain editMain;

  public event System.Action<VoosActor> onActorDrag;

  private VoosActor draggingActor;
  private VoosActor dragHintTarget;
  private bool dragHintSelf;

  void Awake()
  {
    Debug.Assert(entryPrefab != null);
    Debug.Assert(listEntryParent != null);
  }

  // NOTE: If this list contains a null, it will render as "[None]" and will be selectable.
  // This is useful if you want a "No actor" option to be on the list (if this is an actor picker).
  public void SetActors(IEnumerable<VoosActor> actors, bool sortByDistance = false)
  {
    // Lazily get EditMain if we're going to need it.
    if (sortByDistance)
    {
      Util.FindIfNotSet(this, ref editMain);
    }

    List<ActorInfo> actorInfos = new List<ActorInfo>();
    foreach (VoosActor actor in actors)
    {
      if (actor != null && actor.IsSystemActor())
      {
        // System actors don't appear on the list.
        continue;
      }
      actorInfos.Add(new ActorInfo(actor, editMain));
    }
    actorInfos.Sort((a, b) =>
    {
      // No actor.
      if (a.hierarchyPath.Count == 0) return -1;
      if (b.hierarchyPath.Count == 0) return 1;

      // We want to sort by name / distance, but also make sure children are below parents.
      // Do this by comparing the children of the lowest common ancestor.

      // Go down the hierarchies until they are not the same.
      int i = 0;
      while (i + 1 < a.hierarchyPath.Count &&
             i + 1 < b.hierarchyPath.Count &&
             a.hierarchyPath[i].name == b.hierarchyPath[i].name)
      {
        i++;
      }

      if (a.hierarchyPath[i].name == b.hierarchyPath[i].name)
      {
        // One actor is the parent of another. Put the parent first.
        return a.hierarchyPath.Count.CompareTo(b.hierarchyPath.Count);
      }

      // The hierarchies have diverged. Now compare their names / distances.
      if (sortByDistance && editMain != null)
      {
        return a.hierarchyPath[i].distance.CompareTo(b.hierarchyPath[i].distance);
      }
      else
      {
        return a.hierarchyPath[i].displayName.CompareTo(b.hierarchyPath[i].displayName);
      }
    });

    for (int i = 0; i < actorInfos.Count; i++)
    {
      HierarchyActorEntry entry;
      if (i < entries.Count)
      {
        // Recycle entry
        entry = entries[i];
      }
      else
      {
        // Create new entry from template.
        GameObject clone = GameObject.Instantiate(entryPrefab);
        clone.SetActive(true);
        clone.transform.SetParent(listEntryParent.transform, false);
        entry = clone.GetComponent<HierarchyActorEntry>();
        entry.EnableActionButtons(enableActionButtons);
        entry.SetDisableLockedActors(disableLockedActors);
        entry.AddClickListener(OnActorEntryClicked);
        entries.Add(entry);
        entry.onDrag += onActorDrag;
      }
      entry.SetActor(actorInfos[i].actor, actorInfos[i].hierarchyPath.Count - 1);
      entry.SetHighlighted(false);
    }
    // Delete unused entries.
    while (entries.Count > actorInfos.Count)
    {
      HierarchyActorEntry entry = entries[entries.Count - 1];
      entry.RemoveClickListener(OnActorEntryClicked);
      entry.onDrag -= onActorDrag;
      GameObject.Destroy(entry.gameObject);
      entries.RemoveAt(entries.Count - 1);
    }

    UpdateDrag();
  }

  public void HighlightActors(List<VoosActor> actors)
  {
    foreach (HierarchyActorEntry entry in entries)
    {
      entry.SetHighlighted(actors.Contains(entry.GetActor()));
    }
  }

  private VoosActor GetActorUnderMouse()
  {
    foreach (HierarchyActorEntry entry in entries)
    {
      if (RectTransformUtility.RectangleContainsScreenPoint(entry.GetRect(), Input.mousePosition))
      {
        return entry.GetActor();
      }
    }
    return null;
  }

  public void SetDraggingActor(VoosActor draggingActor)
  {
    this.draggingActor = draggingActor;
    UpdateDrag();
  }

  public bool IsDragTarget(out VoosActor actorTarget)
  {
    actorTarget = dragHintTarget;
    return dragHintSelf;
  }

  public void Update()
  {
    UpdateDrag();
  }

  private void UpdateDrag()
  {
    Vector2 mousePosition = Input.mousePosition;

    if (draggingActor != null)
    {
      dragHintSelf = RectTransformUtility.RectangleContainsScreenPoint(container, Input.mousePosition);
      dragHintTarget = GetActorUnderMouse();
    }
    else
    {
      dragHintSelf = false;
      dragHintTarget = null;
    }

    foreach (HierarchyActorEntry entry in entries)
    {
      entry.SetDragging(entry.GetActor() != null && entry.GetActor().Equals(draggingActor));
      entry.SetDragInHint(entry.GetActor() != null && entry.GetActor().Equals(dragHintTarget));
    }

    dragHintSelfOutline.SetActive(dragHintTarget == null && dragHintSelf);
  }

  public void AddClickListener(HierarchyActorEntry.OnActorEntryClicked listener)
  {
    onActorEntryClicked += listener;
  }

  public void RemoveClickListener(HierarchyActorEntry.OnActorEntryClicked listener)
  {
    onActorEntryClicked -= listener;
  }

  private void OnActorEntryClicked(HierarchyActorEntry entry, HierarchyActorEntry.ActionType actionType)
  {
    if (onActorEntryClicked != null)
    {
      onActorEntryClicked(entry, actionType);
    }
  }

  // Used internally for list sorting.
  private struct ActorInfo
  {
    public VoosActor actor;
    public List<ActorHierarchyInfo> hierarchyPath;

    public ActorInfo(VoosActor actor, EditMain editMain)
    {
      this.actor = actor;
      hierarchyPath = new List<ActorHierarchyInfo>();
      for (VoosActor thisActor = actor; thisActor != null; thisActor = thisActor.GetParentActor())
      {
        ActorHierarchyInfo info = new ActorHierarchyInfo();
        info.name = thisActor.name;
        info.displayName = thisActor.GetDisplayName();
        if (editMain != null)
        {
          info.distance = Vector3.Distance(editMain.GetAvatarPosition(), thisActor.GetPosition());
        }
        hierarchyPath.Insert(0, info);
      }
    }
  }

  private struct ActorHierarchyInfo
  {
    public string name;
    public string displayName;
    public float distance;
  }
}