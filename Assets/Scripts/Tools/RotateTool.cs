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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTool : Tool
{
  public struct UndoState
  {
    readonly Vector3 position;
    readonly Quaternion rotation;
    readonly Vector3 spawnPosition;
    readonly Quaternion spawnRotation;

    public UndoState(VoosActor actor)
    {
      position = actor.GetPosition();
      rotation = actor.GetRotation();
      spawnPosition = actor.GetSpawnPosition();
      spawnRotation = actor.GetSpawnRotation();
    }

    public void PushTo(VoosActor actor, bool autosetSpawn)
    {
      actor.RequestOwnership();
      actor.SetPosition(position);
      actor.SetRotation(rotation);

      if (autosetSpawn)
      {
        actor.SetSpawnPosition(spawnPosition);
        actor.SetSpawnRotation(spawnRotation);
      }
    }
  }

  Dictionary<VoosActor, UndoState> actorUndoStates = new Dictionary<VoosActor, UndoState>();

  bool rotating = false;
  const float ROTATE_MOD = 10f;

  [SerializeField] ToolEffectController toolEffectPrefab;
  ToolEffectController toolEffectController;
  [SerializeField] RotateSelectionFeedback rotateSelectionFeedback;
  [SerializeField] Collider yawCollider;
  [SerializeField] Collider pitchCollider;
  [SerializeField] Collider rollCollider;
  [SerializeField] RotateToolSettings rotateToolSettingsPrefab;
  [SerializeField] GameObject gizmoParent;

  RotateToolSettings rotateToolSettings;

  UndoStack undoStack;
  VoosEngine engine;
  DynamicPopup popups;

  Transform internalTransform;
  Rigidbody targetRigidbody;
  bool thingHadPhysics = false;

  float UNDO_THRESHOLD = .01f;

  Vector3 hitOffset;
  float cameraDist;
  Vector3 rayoffset = Vector3.zero;
  float rayDist;

  Vector2 normalLineSlope;

  Vector2 mouseMovementSinceStartRotate = Vector2.zero;

  float yawGizmoRadius = .75f;
  float pitchGizmoRadius = .625f;
  float rollGizmoRadius = .55f;
  float gizmoRadiusThreshold = .1f;

  Vector3 yawEdge = Vector3.up;
  Vector3 pitchEdge = Vector3.right;
  Vector3 rollEdge = Vector3.forward;

  RotationAxis currentAxis = RotationAxis.None;

  void Awake()
  {
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref popups);
    toolEffectController = GameObject.Instantiate(toolEffectPrefab, Vector3.zero, Quaternion.identity, transform);
  }

  VoosActor GetFocusActor()
  {
    return editMain.GetFocusedTargetActor();
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
      if (rotating) StopRotate();
    }
    return true;
  }

  bool OnTrigger()
  {
    if (GizmoCheck())
    {
      StartRotate();
      return true;
    }
    else if (hoverActor != null && !hoverActor.IsLockedByAnother())
    {
      bool addedOrPresent = editMain.AddSetOrRemoveTargetActor(hoverActor);

      if (addedOrPresent)
      {
        rotateSelectionFeedback.SetActor(GetFocusActor());
        StartRotate();
      }
      return true;
    }
    else
    {
      if (!Util.HoldingModiferKeys())
      {
        editMain.ClearTargetActors();
        rotateSelectionFeedback.SetActor(GetFocusActor());
      }
      return false;
    }
  }

  public override string GetName()
  {
    return "Rotate";
  }

  public override void SetHoverActor(VoosActor _actor)
  {
    base.SetHoverActor(_actor);

  }

  bool IsGizmoCollider(Collider collider)
  {
    return collider == yawCollider || collider == rollCollider || collider == pitchCollider;
  }

  private bool GizmoCheck()
  {
    if (editMain.GetTargetActorsCount() == 0) return false;
    RaycastHit[] hits;
    hits = Physics.RaycastAll(editMain.GetCursorRay());
    for (int i = 0; i < hits.Length; i++)
    {
      if (IsGizmoCollider(hits[i].collider))
      {
        return true;
      }
    }
    return false;
  }

  void StartRotate()
  {
    Debug.Assert(GetFocusActor() != null);

    rotating = true;
    mouseMovementSinceStartRotate = Vector2.zero;

    rotateSelectionFeedback.SetSelected(true);
    toolEffectController.ToolActivate(true);

    if (editMain.GetTargetActorsCount() == 1)
    {
      StartRotateSingleSelection();
    }
    else
    {
      StartRotateMultiselection();
    }
  }

  void StartRotateSingleSelection()
  {
    GetFocusActor().RequestOwnership();
    hitOffset = GetFocusActor().transform.InverseTransformPoint(targetPosition);
    targetRigidbody = GetFocusActor().GetComponent<Rigidbody>();
    internalTransform.SetPositionAndRotation(GetFocusActor().transform.position, GetFocusActor().transform.rotation);
    FindNormalLineSingleSelection();

    actorUndoStates.Clear();
    actorUndoStates[GetFocusActor()] = new UndoState(GetFocusActor());
  }

  void StartRotateMultiselection()
  {
    Vector3 averagePosition = Vector3.zero;
    int actorCount = 0;

    actorUndoStates.Clear();
    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        actor.RequestOwnership();
        averagePosition += actor.transform.position;
        actorCount++;

        actorUndoStates[actor] = new UndoState(actor);
      }
    }

    averagePosition /= (float)actorCount;
    hitOffset = GetFocusActor().transform.InverseTransformPoint(averagePosition);
    internalTransform.SetPositionAndRotation(averagePosition, Quaternion.identity);
    FindNormalLineMultiselection();
  }

  Vector3 GetAveragePosition()
  {
    Vector3 averagePosition = Vector3.zero;
    int actorCount = 0;

    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        averagePosition += actor.transform.position;
        actorCount++;
      }
    }
    averagePosition /= (float)actorCount;
    return averagePosition;
  }

  void StopRotate()
  {
    rotating = false;
    rotateSelectionFeedback.SetSelected(false);

    //this is because we're using something akin to "global axes" in multiselect (easier)
    if (editMain.GetTargetActorsCount() > 1)
    {
      internalTransform.SetPositionAndRotation(GetAveragePosition(), Quaternion.identity);
    }

    if (mouseMovementSinceStartRotate.sqrMagnitude > UNDO_THRESHOLD && editMain.GetTargetActorsCount() > 0)
    {
      bool autosetSpawn = rotateToolSettings.AutosetSpawn();

      // Save off map of name to state
      Dictionary<string, UndoState> name2undoState = actorUndoStates.ToDictionary(
        entry => entry.Key.GetName(),
        entry => entry.Value
      );
      Dictionary<string, UndoState> name2redoState = actorUndoStates.ToDictionary(
        entry => entry.Key.GetName(),
        entry => new UndoState(entry.Key)
      );
      undoStack.PushUndoForMany(engine,
        actorUndoStates.Keys,
        $"Rotate",
        redoActor => name2redoState[redoActor.GetName()].PushTo(redoActor, autosetSpawn),
        undoActor => name2undoState[undoActor.GetName()].PushTo(undoActor, autosetSpawn));

      // Propagate spawn positions to all children of all moved objects.
      if (autosetSpawn)
      {
        foreach (VoosActor actor in actorUndoStates.Keys)
        {
          actor.SetSpawnPositionRotationOfEntireFamily();
        }
      }

      actorUndoStates.Clear();
    }

    toolEffectController.ToolActivate(false);
  }


  public override bool MouseLookActive()
  {
    return !rotating;
  }

  void FindNormalLineMultiselection()
  {
    Vector3 v1 = internalTransform.position;
    Vector3 v2 = Vector3.up;
    if (GetAxis() == RotationAxis.Roll)
    {
      v2 = internalTransform.forward;
    }
    else if (GetAxis() == RotationAxis.Pitch)
    {
      v2 = internalTransform.right;
    }

    Camera cam = Camera.main;
    normalLineSlope = (cam.WorldToScreenPoint(v2 + v1) - cam.WorldToScreenPoint(v1)).normalized;
  }

  void FindNormalLineSingleSelection()
  {
    Vector3 v1 = GetFocusActor().GetPosition();
    Vector3 v2 = GetFocusActor().transform.up;
    if (GetAxis() == RotationAxis.Roll)
    {
      v2 = GetFocusActor().transform.forward;
    }
    else if (GetAxis() == RotationAxis.Pitch)
    {
      v2 = GetFocusActor().transform.right;
    }

    Camera cam = Camera.main;
    normalLineSlope = (cam.WorldToScreenPoint(v2 + v1) - cam.WorldToScreenPoint(v1)).normalized;
  }

  float GetRelativeToNormalLine(Vector2 dir)
  {
    float sign = Mathf.Sign(Vector3.SignedAngle(normalLineSlope, dir, Vector3.forward));
    return sign * Vector3.Cross(normalLineSlope, dir).magnitude;
  }


  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);

    rotateSelectionFeedback.Setup();

    rotateToolSettings = Instantiate(rotateToolSettingsPrefab, editMain.topLeftAnchor);
    rotateToolSettings.Setup();
    toolEffectController.SetTint(editMain.GetAvatarTint());
    toolEffectController.originTransform = emissionAnchor;
    internalTransform = new GameObject().transform;

    LoadToolMemory();

    ForceUpdateTargetActor();
  }

  public override void ForceUpdateTargetActor()
  {
    rotateSelectionFeedback.SetActor(editMain.GetFocusedTargetActor());
    if (editMain.GetTargetActorsCount() > 1)
    {
      internalTransform.SetPositionAndRotation(GetAveragePosition(), Quaternion.identity);
    }
  }

  public override void Close()
  {
    base.Close();
    SaveToolMemory();
    rotateToolSettings.RequestDestroy();
    Destroy(internalTransform.gameObject);
  }

  void Update()
  {
    gizmoParent.SetActive(rotateToolSettings.GetShowSettings());
    if (!rotating) UpdateAxis();
    rotateSelectionFeedback.UpdateAxis(GetAxis());
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return !(rotateToolSettings.GetShowSettings() && editMain.GetTargetActorsCount() == 1);
  }

  public enum RotationAxis
  {
    Yaw = 0,
    Pitch = 1,
    Roll = 2,
    None = 3
  };

  struct AxisCandidate
  {
    public RotationAxis axis;
    public float distance;
    public bool onEdge;
  }

  RotationAxis GetAxis()
  {
    return currentAxis;
  }

  void UpdateAxis()
  {
    if (!rotateToolSettings.GetShowSettings())
    {
      currentAxis = RotationAxis.Yaw;
      return;
    }


    RaycastHit[] hits;
    hits = Physics.RaycastAll(editMain.GetCursorRay());
    HashSet<AxisCandidate> candidates = new HashSet<AxisCandidate>();

    for (int i = 0; i < hits.Length; i++)
    {
      if (hits[i].collider == yawCollider)
      {
        bool onEdge = Vector3.Dot(hits[i].normal, yawEdge) == 0;
        float distanceFromCircle = Mathf.Abs(yawGizmoRadius - GetScaledHitDistanceFromCenter(hits[i]));
        if (distanceFromCircle < gizmoRadiusThreshold)
        {
          candidates.Add(new AxisCandidate() { axis = RotationAxis.Yaw, distance = distanceFromCircle, onEdge = onEdge });
        }
      }
      if (hits[i].collider == rollCollider)
      {
        bool onEdge = Vector3.Dot(hits[i].normal, rollEdge) == 0;
        float distanceFromCircle = Mathf.Abs(rollGizmoRadius - GetScaledHitDistanceFromCenter(hits[i]));
        if (distanceFromCircle < gizmoRadiusThreshold)
        {
          candidates.Add(new AxisCandidate() { axis = RotationAxis.Roll, distance = distanceFromCircle, onEdge = onEdge });
        }
      }
      if (hits[i].collider == pitchCollider)
      {
        bool onEdge = Vector3.Dot(hits[i].normal, pitchEdge) == 0;
        float distanceFromCircle = Mathf.Abs(pitchGizmoRadius - GetScaledHitDistanceFromCenter(hits[i]));
        if (distanceFromCircle < gizmoRadiusThreshold || onEdge)
        {
          candidates.Add(new AxisCandidate() { axis = RotationAxis.Pitch, distance = distanceFromCircle, onEdge = onEdge });
        }
      }
    }

    if (candidates.Count == 0)
    {
      currentAxis = RotationAxis.None;
      return;
    }

    if (candidates.Count == 0)
    {
      currentAxis = RotationAxis.None;
      return;
    }
    if (candidates.Where(x => x.onEdge).Count() != 0)
    {
      currentAxis = candidates.Where(x => x.onEdge).First().axis;
    }
    else
    {
      currentAxis = candidates.OrderBy(x => x.distance).First().axis;
    }

  }



  private float GetScaledHitDistanceFromCenter(RaycastHit raycastHit)
  {
    return Vector3.Distance(raycastHit.point, raycastHit.collider.transform.position) / raycastHit.collider.transform.lossyScale.x;
  }

  void SingleActorLateUpdate(bool snapping, Vector2 currentDelta, float amount)
  {
    internalTransform.position = GetFocusActor().GetPosition();

    switch (GetAxis())
    {
      case RotationAxis.Yaw:
        internalTransform.Rotate(0, amount * ROTATE_MOD, 0);
        break;
      case RotationAxis.Pitch:
        internalTransform.Rotate(amount * ROTATE_MOD, 0, 0);
        break;
      case RotationAxis.Roll:
        internalTransform.Rotate(0, 0, amount * ROTATE_MOD);
        break;
    }

    if (snapping)
    {
      GetFocusActor().SetRotation(SnapQuaternion(internalTransform.rotation), hackAdjustPlayerBodyLastAimRotation: true);
    }
    else
    {
      GetFocusActor().SetRotation(internalTransform.rotation, hackAdjustPlayerBodyLastAimRotation: true);
    }

    if (targetRigidbody != null)
    {
      targetRigidbody.angularVelocity = Vector3.zero;
    }

    if (rotateToolSettings.AutosetSpawn())
    {

      // Set both, for placing rolling physics objects.
      GetFocusActor().SetSpawnPosition(GetFocusActor().GetPosition());
      GetFocusActor().SetSpawnRotation(GetFocusActor().GetRotation());
    }
  }

  void MultiselectLateUpdate(bool snapping, Vector2 currentDelta, float amount)
  {
    internalTransform.position = GetAveragePosition();

    Vector3 rotationVector = Vector3.zero;

    switch (GetAxis())
    {
      case RotationAxis.Yaw:
        rotationVector = new Vector3(0, 1, 0);
        break;
      case RotationAxis.Pitch:
        rotationVector = new Vector3(1, 0, 0);
        break;
      case RotationAxis.Roll:
        rotationVector = new Vector3(0, 0, 1);
        break;
    }

    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) != null)
      {
        actorUndoStates.Remove(actor);
        continue;
      }

      actor.RotateAround(internalTransform.position, internalTransform.TransformDirection(rotationVector), amount * ROTATE_MOD, hackAdjustPlayerBodyLastAimRotation: true);
      SetFeedbackTransformToInternal();

      internalTransform.Rotate(internalTransform.TransformDirection(rotationVector), amount * ROTATE_MOD);

      Rigidbody rb = actor.GetComponent<Rigidbody>();
      if (rb != null)
      {
        rb.angularVelocity = Vector3.zero;
      }

      if (rotateToolSettings.AutosetSpawn())
      {
        // Set both, for placing rolling physics objects.
        actor.SetSpawnPosition(actor.GetPosition());
        actor.SetSpawnRotation(actor.GetRotation());
      }
    }
  }

  void LateUpdate()
  {

    if (rotating && GetFocusActor() == null)
    {
      StopRotate();
      return;
    }

    rotateSelectionFeedback.SetActor(GetFocusActor());

    if (rotating)
    {
      bool snapping = rotateToolSettings.GetSnapping();
      Vector2 currentDelta = inputControl.GetMouseAxes();
      float amount = GetRelativeToNormalLine(currentDelta);

      mouseMovementSinceStartRotate += currentDelta;

      if (editMain.GetTargetActorsCount() == 1)
      {
        SingleActorLateUpdate(snapping, currentDelta, amount);
      }
      else
      {
        MultiselectLateUpdate(snapping, currentDelta, amount);
      }
    }
    else
    {
      if (inputControl.GetButtonDown("Rotate"))
      {
        editMain.QuickRotateTargetActors();
      }

      if (editMain.GetTargetActorsCount() > 1)
      {
        internalTransform.position = GetAveragePosition();
        internalTransform.rotation = Quaternion.identity;
        SetFeedbackTransformToInternal();
      }
      else
      {
        rotateSelectionFeedback.UpdatePosition();
      }
    }



    if (rotating)
    {
      toolEffectController.UpdateTargetPosition(rotateSelectionFeedback.transform.position);
      toolEffectController.ExplicitLateUpdate();
    }
    rotateSelectionFeedback.UpdateScale(editMain.GetCamera().transform.position, editMain.GetCamera().fieldOfView);

  }

  void SetFeedbackTransformToInternal()
  {
    rotateSelectionFeedback.SetPosition(internalTransform.position);
    rotateSelectionFeedback.SetRotation(internalTransform.rotation);
  }

  public override bool GetToolEffectActive()
  {
    return rotating;
  }

  public override int GetToolEffectTargetViewId()
  {
    return GetFocusActor() != null ? GetFocusActor().GetPrimaryPhotonViewId() : -1;
  }

  Quaternion SnapQuaternion(Quaternion q)
  {
    Vector3 dir = q * Vector3.forward;
    Vector3 dirB = q * Vector3.up;
    dir.x = Mathf.Round(dir.x);
    dir.y = Mathf.Round(dir.y);
    dir.z = Mathf.Round(dir.z);
    dirB.x = Mathf.Round(dirB.x);
    dirB.y = Mathf.Round(dirB.y);
    dirB.z = Mathf.Round(dirB.z);
    return Quaternion.LookRotation(dir, dirB);
  }

  private void LoadToolMemory()
  {
    rotateToolSettings.SetShowSettings(PlayerPrefs.GetInt("rotateTool-showSettings", 0) == 1 ? true : false);
    rotateToolSettings.SetShowOffsets(PlayerPrefs.GetInt("rotateTool-showOffsets", 0) == 1 ? true : false);
    rotateToolSettings.SetSnapping(PlayerPrefs.GetInt("rotateTool-snapping", 0) == 1 ? true : false);
    rotateToolSettings.SetAutosetSpawn(PlayerPrefs.GetInt("rotateTool-autosetSpawn", 1) == 1 ? true : false);
  }

  private void SaveToolMemory()
  {
    PlayerPrefs.SetInt("rotateTool-showSettings", rotateToolSettings.GetShowSettings() ? 1 : 0);
    PlayerPrefs.SetInt("rotateTool-showOffsets", rotateToolSettings.GetShowOffsets() ? 1 : 0);
    PlayerPrefs.SetInt("rotateTool-snapping", rotateToolSettings.GetSnapping() ? 1 : 0);
    PlayerPrefs.SetInt("rotateTool-autosetSpawn", rotateToolSettings.AutosetSpawn() ? 1 : 0);

  }
}