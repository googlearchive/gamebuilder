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

public abstract class Tool : MonoBehaviour
{
  [SerializeField] protected RectTransform reticleRect;

  [SerializeField] RectTransform subtoolbarRect;
  [SerializeField] protected BasicToolSubtoolbar subtoolbar;
  public static int DefaultLayer
  {
    get { return LayerMask.NameToLayer("Default"); }
  }

  public static int OffstageLayer
  {
    get { return LayerMask.NameToLayer("PrefabWorld"); }
  }

  protected EditMain editMain;
  protected ToolMemory toolMemory;

  // targets (which targets determined by those obols up there)
  protected VoosActor hoverActor;

  [HideInInspector] public Vector3 targetPosition;

  //anchor for mouselook/offset and anchor for emission effects like linerender
  [HideInInspector] public Transform emissionAnchor;
  [HideInInspector] public Transform mainAnchor;

  //max range of the effect
  //[HideInInspector] public float maxDistance = 5f;

  // primary and secondary actions
  [HideInInspector] public bool isTriggerDown = false;

  protected InputControl inputControl;

  bool active = false;

  protected int subtoolbarIndex = 0;

  int currentLayer;

  const float DEFAULT_MAX_SELECTION_DISTANCE = 1500f;
  const float DEFAULT_SPACE_DISTANCE = 15f;

  public virtual string GetInvalidActorReason(VoosActor actor)
  {
    if (actor == null) return "Actor does not exist anymore!";

    // True, this doesn't prevent the race condition if two players try to
    // request-lock the same actor. Basically one will win, and the other will
    // forever *think* they have it, but not really. And with copy-groups, this
    // problem becomes more likely. We'll ignore it for now, but in the future
    // possible solutions are: 1) make all tools poll for lock (and self-close
    // if they didn't actually get it), 2) have a centralized lock arbiter and
    // hopefully hide any edit latency, if possible.
    if (actor.IsLockedByAnother())
    {
      if (actor.IsCloneParentLockedByAnother())
      {
        VoosActor parent = actor.GetCloneParentActor();
        return $"LOCKED\nThis is a copy, and {parent.GetOwnerNickName()} is editing the original";
      }
      else
      {
        return $"LOCKED\n{actor.GetOwnerNickName()} is editing this";
      }
    }
    return null;
  }

  public virtual float GetMaxDistance()
  {
    return DEFAULT_SPACE_DISTANCE;
  }

  /*   public void UpdateLayer()
    {
      currentLayer = editMain.IsOffstageOpen() ? LayerMask.NameToLayer("PrefabWorld") : LayerMask.NameToLayer("Default");
      Util.SetLayerRecursively(gameObject, currentLayer);
    } */

  public virtual void ForceUpdateTargetActor() { }

  public virtual float GetMaxGroundDistance()
  {
    return DEFAULT_MAX_SELECTION_DISTANCE;
  }

  public virtual float GetMaxThingsDistance()
  {
    return DEFAULT_MAX_SELECTION_DISTANCE;
  }

  public virtual bool IsSelectionLocked()
  {
    return isTriggerDown;
  }

  public virtual bool TargetsActors()
  {
    return true;
  }

  public virtual bool ShowHoverTargetFeedback()
  {
    return true;
  }

  public virtual bool ShowSelectedTargetFeedback()
  {
    return true;
  }

  public virtual bool CanEditTargetActors()
  {
    return true;
  }

  public virtual bool TargetsGround()
  {
    return false;
  }

  public virtual bool TargetsSpace()
  {
    return false;
  }

  public virtual bool CursorActive()
  {
    return false;
  }

  public virtual string GetReticleText()
  {
    return "";
  }

  public virtual void SetHoverActor(VoosActor t)
  {
    hoverActor = t;
  }

  public virtual bool Trigger(bool on)
  {
    isTriggerDown = on;
    return true;
  }

  public virtual string GetName()
  {
    return "";
  }

  public virtual void ForceRelease()
  {
    if (isTriggerDown) Trigger(false);

  }

  public bool IsActive()
  {
    return active;
  }

  public virtual string GetDescription()
  {
    return "Tool";
  }

  public virtual void Launch(EditMain _editmain)
  {
    Util.FindIfNotSet(this, ref toolMemory);
    editMain = _editmain;
    mainAnchor = editMain.mainAnchor;
    emissionAnchor = editMain.emissionAnchor;
    inputControl = editMain.GetInputControl();

    active = true;


    if (reticleRect != null)
    {
      reticleRect.SetParent(editMain.GetReticleAnchor());
      reticleRect.anchoredPosition = Vector2.zero;
      reticleRect.gameObject.SetActive(true);
    }

    if (subtoolbarRect != null)
    {
      subtoolbarRect.SetParent(editMain.bottomToolbarAnchor);
      subtoolbarRect.SetAsFirstSibling();
      subtoolbarRect.anchoredPosition = Vector2.zero;
      subtoolbarRect.localScale = Vector3.one;
    }

    if (subtoolbar != null)
    {
      subtoolbar.Setup();
      subtoolbar.OnSelectIndex = (newindex) => subtoolbarIndex = newindex;
      subtoolbar.SelectIndex(toolMemory.RequestSubtoolbarIndex(GetType()));
    }
  }

  public virtual void Close()
  {
    ForceRelease();
    if (subtoolbar != null)
    {
      toolMemory.SetSubtoolbarIndex(GetType(), subtoolbarIndex);
    }
    if (subtoolbarRect != null)
    {
      Destroy(subtoolbarRect.gameObject);
    }

    if (reticleRect != null)
    {
      Destroy(reticleRect.gameObject);
    }
    toolMemory.lastTool = GetName();
    Destroy(gameObject);
  }

  public virtual bool OnEscape()
  {
    return false;
  }

  public virtual void UpdatePosition(Vector3 p)
  {
    targetPosition = p;
  }

  public virtual bool MouseLookActive()
  {
    return true;
  }

  public virtual bool KeyLock()
  {
    return false;
  }

  public virtual string GetToolEffectName()
  {
    // HACKY
    return $"ToolFX_{this.name}";
  }

  protected void PreviousSubtoolbarItem()
  {
    int newindex = subtoolbar.currentIndex - 1;
    if (newindex < 0) newindex = (subtoolbar.GetSize() - 1);
    subtoolbar.SelectIndex(newindex);
  }

  protected void NextSubtoolbarItem()
  {
    int newindex = (subtoolbar.currentIndex + 1) % subtoolbar.GetSize();
    subtoolbar.SelectIndex(newindex);
  }

  public virtual bool GetToolEffectActive() { return false; }

  public virtual int GetToolEffectTargetViewId() { return -1; }

  public virtual Vector3 GetToolEffectTargetPosition() { return targetPosition; }

  // YOLO
  public virtual string GetCreatePreviewSettingsJson() { return ""; }
}


