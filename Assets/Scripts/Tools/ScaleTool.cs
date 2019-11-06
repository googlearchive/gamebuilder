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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ScaleTool : Tool
{
  bool scaling = false;
  float scaleScaler = .25f;
  Vector3 hitOffset;

  [SerializeField] ToolEffectController toolEffectPrefab;
  ToolEffectController toolEffectController;
  [SerializeField] ScaleSelectionFeedback gizmoSelectionFeedback;
  [SerializeField] GameObject gizmoParent;
  [SerializeField] GameObject selectionParent;
  [SerializeField] Collider gizmoCollider;
  UndoStack undoStack;
  VoosEngine engine;
  DynamicPopup popups;

  const float MIN_SCALE = 0.1f;
  const float DRAG_SCALE_MOD = .3f;
  const float DRAG_SCALE_MOD_FAST = .7f;
  float UNDO_THRESHOLD = .001f;

  Dictionary<VoosActor, Vector3> actorStartScales = new Dictionary<VoosActor, Vector3>();

  enum ScaleType
  {
    Drag = 0,
    Grow = 1,
    Shrink = 2,
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref popups);

    toolEffectController = GameObject.Instantiate(toolEffectPrefab, Vector3.zero, Quaternion.identity, transform);
  }

  ScaleType GetScaleType()
  {
    return ScaleType.Drag;
  }

  public override bool Trigger(bool on)
  {
    base.Trigger(on);

    if (on)
    {
      return OnTrigger();
    }
    else
    {
      if (scaling) StopScale();
    }
    return true;
  }

  bool OnTrigger()
  {
    {
      if (GizmoCheck())
      {
        StartScale();
        return true;
      }
      else if (hoverActor != null && !hoverActor.IsLockedByAnother())
      {
        bool addedOrPresent = editMain.AddSetOrRemoveTargetActor(hoverActor);

        if (addedOrPresent)
        {
          gizmoSelectionFeedback.SetActor(GetFocusActor());
          StartScale();
        }

        return true;
      }
      else
      {
        if (!Util.HoldingModiferKeys())
        {
          editMain.ClearTargetActors();
          gizmoSelectionFeedback.SetActor(GetFocusActor());
        }
        return false;
      }
    }
  }

  /*  public override bool ShowSelectedTargetFeedback()
   {
     return false;
   }
  */
  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);
    toolEffectController.SetTint(editMain.GetAvatarTint());
    toolEffectController.originTransform = emissionAnchor;
    ForceUpdateTargetActor();
  }

  public override void ForceUpdateTargetActor()
  {
    gizmoSelectionFeedback.SetActor(editMain.GetFocusedTargetActor());
  }

  public override string GetName()
  {
    return "Size";
  }

  private bool GizmoCheck()
  {
    if (editMain.GetTargetActorsCount() == 0) return false;
    RaycastHit[] hits;
    hits = Physics.RaycastAll(editMain.GetCursorRay());
    for (int i = 0; i < hits.Length; i++)
    {
      if (hits[i].collider == gizmoCollider)
      {
        return true;
      }
    }
    return false;
  }

  void StartScale()
  {
    Debug.Assert(GetFocusActor() != null);

    bool wasScaling = scaling;
    scaling = true;

    if (GetScaleType() == ScaleType.Drag)
    {
      gizmoSelectionFeedback.SetSelected(true);
      hitOffset = gizmoSelectionFeedback.transform.position;
    }
    else
    {
      hitOffset = GetFocusActor().transform.InverseTransformPoint(targetPosition);
    }

    actorStartScales.Clear();

    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        actorStartScales[actor] = actor.GetLocalScale();
        actor.RequestOwnership();
        if (!wasScaling)
          Debug.Assert(actor.GetLocalScale().magnitude != 0f, "ToggleScale: actor has zero scale??");
      }
    }

    toolEffectController.ToolActivate(true);
  }

  void StopScale()
  {
    bool wasScaling = scaling;
    scaling = false;
    gizmoSelectionFeedback.SetSelected(false);

    // Filter out deleted actors
    var nonNullStartScales = actorStartScales.
      // OK kinda gross we need this extra GetActor check..
      Where(p => p.Key != null && p.Key.GetEngine().GetActor(p.Key.GetName()) != null).
      ToDictionary(p => p.Key, p => p.Value);

    actorStartScales.Clear();

    if (wasScaling && nonNullStartScales.Count > 0 && IsAboveUndoThreshold(nonNullStartScales.First()))
    {
      // Save off map of name to state
      Dictionary<string, Vector3> name2undoState = nonNullStartScales.ToDictionary(
        entry => entry.Key.GetName(),
        entry => entry.Value
      );
      Dictionary<string, Vector3> name2redoState = nonNullStartScales.ToDictionary(
        entry => entry.Key.GetName(),
        entry => entry.Key.GetLocalScale()
      );
      undoStack.PushUndoForMany(engine,
        nonNullStartScales.Keys,
        $"Scale",
        redoActor => redoActor.SetLocalScale(name2redoState[redoActor.GetName()]),
        undoActor => undoActor.SetLocalScale(name2undoState[undoActor.GetName()]));

      foreach (VoosActor actor in nonNullStartScales.Keys)
      {
        actor.SetSpawnPositionRotationOfEntireFamily();
      }
    }

    toolEffectController.ToolActivate(false);
  }

  private bool IsAboveUndoThreshold(KeyValuePair<VoosActor, Vector3> entry)
  {
    return Vector3.Distance(entry.Key.GetLocalScale(), entry.Value) > UNDO_THRESHOLD;
  }

  void GrowShrinkUpdate()
  {
    float scaleMod = scaleScaler * (editMain.GetInputControl().GetButton("Snap") ? 3 : 1);
    float value = scaleMod * ((GetScaleType() == ScaleType.Grow ? 1 : 0) - (GetScaleType() == ScaleType.Shrink ? 1 : 0))
          * Time.unscaledDeltaTime;
    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        AddScale(actor, value);
      }
    }
  }

  void DragScaleUpdate()
  {
    Vector2 currentDelta = inputControl.GetMouseAxes();
    float scaleMod = scaleScaler * (editMain.GetInputControl().GetButton("Snap") ? DRAG_SCALE_MOD_FAST : DRAG_SCALE_MOD);
    float value = 1 + (currentDelta.y + currentDelta.x) * scaleMod;
    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        MultiplyScale(actor, value);
      }
    }
  }

  void UpdateScale(VoosActor actor, Vector3 newscale)
  {
    actor.SetLocalScale(newscale);
    if (actor.GetLocalScale().x < MIN_SCALE)
    {
      actor.SetLocalScale(Vector3.one * MIN_SCALE);
    }
  }

  void MultiplyScale(VoosActor actor, float value)
  {
    UpdateScale(actor, actor.GetLocalScale() * value);
  }

  void AddScale(VoosActor actor, float value)
  {
    UpdateScale(actor, actor.GetLocalScale() + Vector3.one * value);
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return editMain.GetTargetActorsCount() != 1;
  }

  void Update()
  {

    if (scaling && GetFocusActor() == null)
    {
      StopScale();
      return;
    }

    if (!scaling)
    {
      if (inputControl.GetButtonDown("Rotate"))
      {
        editMain.QuickRotateTargetActors();
      }
    }


    if (gizmoSelectionFeedback.GetActor() != GetFocusActor())
    {
      gizmoSelectionFeedback.SetActor(GetFocusActor());
    }

    if (GetScaleType() == ScaleType.Drag)
    {
      selectionParent.SetActive(false);
      gizmoParent.SetActive(true);
      if (scaling)
      {
        DragScaleUpdate();
      }
    }
    else
    {
      if (scaling)
      {
        GrowShrinkUpdate();
      }
      selectionParent.SetActive(true);
      gizmoParent.SetActive(false);
    }

  }


  private void LateUpdate()
  {
    gizmoSelectionFeedback.UpdatePosition();
    gizmoSelectionFeedback.UpdateScale(editMain.GetCamera().transform.position, editMain.GetCamera().fieldOfView);

    if (GetFocusActor() != null)
    {
      if (GetScaleType() == ScaleType.Drag)
      {
        toolEffectController.UpdateTargetPosition(gizmoSelectionFeedback.transform.position);
      }
      else
      {
        toolEffectController.UpdateTargetPosition(GetFocusActor().transform.TransformPoint(hitOffset));
      }
    }
    toolEffectController.ExplicitLateUpdate();
  }

  public override bool GetToolEffectActive()
  {
    return scaling;
  }

  VoosActor GetFocusActor()
  {
    return editMain.GetFocusedTargetActor();
  }

  public override int GetToolEffectTargetViewId()
  {
    return GetFocusActor() != null ? GetFocusActor().GetPrimaryPhotonViewId() : -1;
  }

}
