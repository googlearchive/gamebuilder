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

public class MoveTool : Tool
{
  public struct UndoState
  {
    readonly Vector3 position;
    readonly Quaternion rotation;
    readonly Vector3 spawnPosition;
    readonly Quaternion spawnRotation;
    readonly bool enablePhysics;

    public UndoState(VoosActor actor)
    {
      position = actor.GetPosition();
      rotation = actor.GetRotation();
      spawnPosition = actor.GetSpawnPosition();
      spawnRotation = actor.GetSpawnRotation();
      enablePhysics = actor.GetEnablePhysics();
    }

    public void PushTo(VoosActor actor, bool autosetSpawn)
    {
      actor.RequestOwnership();
      actor.SweepTo(position);
      actor.SetRotation(rotation);
      actor.SetEnablePhysics(enablePhysics);

      if (autosetSpawn)
      {
        actor.SetSpawnPosition(spawnPosition);
        actor.SetSpawnRotation(spawnRotation);
      }
    }
  }

  struct ActorPreMoveData
  {
    public VoosActor actor;
    public bool hadPhysics;
    public Vector3 initialPosition;

    public UndoState undoState;

    internal void Update(Vector3 delta)
    {
      actor.SweepTo(delta + initialPosition);

      // Set both, for placing rolling physics objects.
      actor.SetSpawnPosition(actor.GetPosition());
      actor.SetSpawnRotation(actor.GetRotation());
    }
  }

  List<ActorPreMoveData> actorMoveDataList = new List<ActorPreMoveData>();

  bool moving = false;
  internal CoordinateSystem coordinateSystem = CoordinateSystem.Global;

  [SerializeField] ToolEffectController toolEffectPrefab;
  [SerializeField] HeightFeedback heightFeedback;
  [SerializeField] PositionSelectionFeedback gizmoSelectionFeedback;
  [SerializeField] GameObject gizmoParent;
  [SerializeField] MoveToolSettings moveToolSettingsPrefab;

  [SerializeField] Collider xCollider;
  [SerializeField] Collider yCollider;
  [SerializeField] Collider zCollider;
  [SerializeField] Collider centerCollider;

  float UNDO_THRESHOLD = .001f;

  UndoStack undoStack;
  VoosEngine engine;
  MoveToolSettings moveToolSettings;
  GameBuilderStage gbStage;

  Vector3 initialPosition = Vector3.zero;
  Quaternion initialRotation = Quaternion.identity;

  Rigidbody targetRigidbody;
  bool thingHadPhysics = false;

  Vector3 horizontalHitOffset;
  float horizontalRayDistance;
  Plane horizontalTargetPlane;

  Vector3 verticalHitOffset;
  float verticalRayDistance;
  Plane verticalTargetPlane;

  float grabRayDistanceOffset = 0;
  Vector3 grabRayTransformOffset;

  Vector3 horizontalActorPosition;

  Vector3 rayEffectOffset;

  ToolEffectController toolEffectController;

  Transform dummyTransform;

  bool waitingToStopMove;

  Ray xAxisRay;
  Ray yAxisRay;
  Ray zAxisRay;
  Vector3 xAxisRayOffset;
  Vector3 yAxisRayOffset;
  Vector3 zAxisRayOffset;

  public enum GizmoAxis
  {
    X = 0,
    Y = 1,
    Z = 2,
    Center = 3,
    None = 4
  }

  bool gizmoActive = false;
  GizmoAxis gizmoAxis;

  void Awake()
  {
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref gbStage);
    toolEffectController = GameObject.Instantiate(toolEffectPrefab, Vector3.zero, Quaternion.identity, transform);
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
      if (moving) StopMove();
    }

    return true;

  }

  public override bool ShowSelectedTargetFeedback()
  {
    return !(moveToolSettings.GetShowSettings() && editMain.GetTargetActorsCount() == 1);
  }

  bool OnTrigger()
  {
    if (waitingToStopMove) return false;
    if (GizmoCheck())
    {
      StartMove();
      return true;
    }
    else if (hoverActor != null && !hoverActor.IsLockedByAnother())
    {
      bool addedOrPresent = editMain.AddSetOrRemoveTargetActor(hoverActor);

      if (addedOrPresent)
      {
        gizmoAxis = GizmoAxis.Center;
        gizmoSelectionFeedback.SetActor(GetFocusActor());
        StartMove();
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

  VoosActor GetFocusActor()
  {
    return editMain.GetFocusedTargetActor();
  }


  public override string GetName()
  {
    return "Move";
  }

  public override void SetHoverActor(VoosActor _actor)
  {
    base.SetHoverActor(_actor);
  }


  void StartMove()
  {
    Debug.Assert(editMain.GetFocusedTargetActor() != null);

    gizmoSelectionFeedback.SetSelected(true);

    CalculateHortizontalInitialValues();
    CalculateVerticalInitialValues();
    CalculateAxisRays();
    CalculateAxisRayOffsets();
    CalculateGrabRayOffset();

    moving = true;

    rayEffectOffset = GetFocusActor().transform.InverseTransformPoint(targetPosition);
    initialPosition = GetFocusActor().GetPosition();
    initialRotation = GetFocusActor().GetRotation();

    actorMoveDataList.Clear();

    foreach (VoosActor actor in editMain.GetTargetActors())
    {
      if (GetInvalidActorReason(actor) == null)
      {
        actor.RequestOwnership();
        actorMoveDataList.Add(new ActorPreMoveData
        {
          actor = actor,
          hadPhysics = actor.GetEnablePhysics(),
          initialPosition = actor.GetPosition(),
          undoState = new UndoState(actor)
        });
      }
    }

    UpdateToolEffectController();
  }

  private void UpdateToolEffectController()
  {
    if (toolEffectController.IsActive() != GetToolEffectActive())
    {
      toolEffectController.ToolActivate(GetToolEffectActive());
    }
  }

  bool IsActorMoveAboveUndoThreshold(ActorPreMoveData actorPreMoveData)
  {
    return (actorPreMoveData.actor.GetPosition() - actorPreMoveData.initialPosition).sqrMagnitude > UNDO_THRESHOLD;
  }

  void StopMove()
  {
    moving = false;
    if (!waitingToStopMove)
    {
      waitingToStopMove = true;
      // We have to do this in a coroutine because we have to wait for the next fixedupdate.
      // See comment in StopMoveCoroutine.
      StartCoroutine(StopMoveCoroutine());
    }
  }

  private IEnumerator StopMoveCoroutine()
  {
    // Wait for FixedUpdate so that all of our move requests are carried out (via actor.SweepTo),
    // so that things are where they should be. This is needed because we want to have the final
    // positions so we can set those as spawn positions for all moved actors and their descendants.
    yield return new WaitForFixedUpdate();
    waitingToStopMove = false;
    gizmoSelectionFeedback.SetSelected(false);

    for (int i = actorMoveDataList.Count - 1; i >= 0; i--)
    {
      if (actorMoveDataList[i].actor == null)
      {
        actorMoveDataList.RemoveAt(i);
        continue;
      }
      actorMoveDataList[i].actor.SetEnablePhysics(actorMoveDataList[i].hadPhysics);
    }

    if (actorMoveDataList.Count > 0 && IsActorMoveAboveUndoThreshold(actorMoveDataList[0]))
    {
      bool autosetSpawn = moveToolSettings.AutosetSpawn();

      // Save off map of name to state
      var name2undoState = actorMoveDataList.ToDictionary(
        data => data.actor.GetName(),
        data => data.undoState
      );
      var name2redoState = actorMoveDataList.ToDictionary(
        data => data.actor.GetName(),
        data => new UndoState(data.actor)
      );

      // Propagate spawn positions to all children of all moved objects.
      if (autosetSpawn)
      {
        foreach (ActorPreMoveData info in actorMoveDataList)
        {
          info.actor.SetSpawnPositionRotationOfEntireFamily();
        }
      }

      undoStack.PushUndoForMany(engine,
        actorMoveDataList.Select(a => a.actor),
        $"Move",
        redoActor => name2redoState[redoActor.GetName()].PushTo(redoActor, autosetSpawn),
        undoActor => name2undoState[undoActor.GetName()].PushTo(undoActor, autosetSpawn));
    }

    actorMoveDataList.Clear();

    UpdateToolEffectController();
  }

  private bool GizmoCheck()
  {
    if (editMain.GetTargetActorsCount() == 0) return false;
    RaycastHit[] hits;
    hits = Physics.RaycastAll(editMain.GetCursorRay());
    for (int i = 0; i < hits.Length; i++)
    {
      if (hits[i].collider == xCollider)
      {
        gizmoAxis = GizmoAxis.X;
        return true;
      }
      if (hits[i].collider == yCollider)
      {
        gizmoAxis = GizmoAxis.Y;
        return true;
      }
      if (hits[i].collider == zCollider)
      {
        gizmoAxis = GizmoAxis.Z;
        return true;
      }
      if (hits[i].collider == centerCollider)
      {
        gizmoAxis = GizmoAxis.Center;
        return true;
      }
    }



    gizmoAxis = GizmoAxis.None;
    return false;

  }

  void CalculateVerticalInitialValues()
  {
    Ray ray = editMain.GetCursorRay();
    Vector3 verticalNormal = editMain.GetCamera().transform.position - GetFocusActor().GetPosition();
    verticalNormal.y = 0;
    verticalTargetPlane = new Plane(verticalNormal.normalized, GetFocusActor().GetPosition());

    verticalTargetPlane.Raycast(ray, out verticalRayDistance);
    Vector3 secondIntersect = ray.GetPoint(verticalRayDistance);
    verticalHitOffset = GetFocusActor().GetPosition() - secondIntersect;
    horizontalActorPosition = GetFocusActor().GetPosition();

  }

  internal enum CoordinateSystem
  {
    Global,
    Local
  };

  void SetCoordinateSystem(CoordinateSystem coordinateSystem)
  {
    if (this.coordinateSystem == coordinateSystem) return;
    this.coordinateSystem = coordinateSystem;
    ForceRelease();
    // gizmoSelectionFeedback.SetGlobalCoordinateSystem(system);
  }


  void CalculateAxisRays()
  {
    if (coordinateSystem == CoordinateSystem.Global)
    {
      xAxisRay = new Ray(GetFocusActor().GetPosition(), Vector3.right);
      yAxisRay = new Ray(GetFocusActor().GetPosition(), Vector3.up);
      zAxisRay = new Ray(GetFocusActor().GetPosition(), Vector3.forward);
    }
    else
    {
      xAxisRay = new Ray(GetFocusActor().GetPosition(), GetFocusActor().transform.right);
      yAxisRay = new Ray(GetFocusActor().GetPosition(), GetFocusActor().transform.up);
      zAxisRay = new Ray(GetFocusActor().GetPosition(), GetFocusActor().transform.forward);
    }
  }

  void CalculateAxisRayOffsets()
  {
    Ray ray = editMain.GetCursorRay();

    xAxisRayOffset = Util.GetClosestPointOnRayFromRay(xAxisRay, ray) - GetFocusActor().GetPosition();
    yAxisRayOffset = Util.GetClosestPointOnRayFromRay(yAxisRay, ray) - GetFocusActor().GetPosition();
    zAxisRayOffset = Util.GetClosestPointOnRayFromRay(zAxisRay, ray) - GetFocusActor().GetPosition();
  }

  void CalculateGrabRayOffset()
  {
    // float ballparkDistance = Vector3.Distance(editMain.GetCamera().transform.position, GetFocusActor().GetPosition());
    Ray ray = editMain.GetCursorRay();
    float distance = Vector3.Dot(ray.direction, GetFocusActor().GetPosition() - editMain.GetCamera().transform.position);

    dummyTransform.position = ray.GetPoint(distance);
    dummyTransform.rotation = Quaternion.identity;

    grabRayDistanceOffset = distance;//Vector3.Distance(editMain.GetCamera().transform.position, GetFocusActor().GetPosition());
    grabRayTransformOffset = dummyTransform.InverseTransformPoint(GetFocusActor().GetPosition());
  }

  void CalculateHortizontalInitialValues()
  {
    Ray ray = editMain.GetCursorRay();

    horizontalTargetPlane = new Plane(Vector3.up, GetFocusActor().GetPosition());

    horizontalRayDistance = 0;
    horizontalTargetPlane.Raycast(ray, out horizontalRayDistance);
    Vector3 rayPlaneIntersect = ray.GetPoint(horizontalRayDistance);
    horizontalHitOffset = GetFocusActor().GetPosition() - rayPlaneIntersect;
  }

  public override bool MouseLookActive()
  {
    return true;
  }

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);

    gizmoSelectionFeedback.Setup();

    toolEffectController.originTransform = emissionAnchor;
    toolEffectController.SetTint(editMain.GetAvatarTint());
    moveToolSettings = Instantiate(moveToolSettingsPrefab, editMain.topLeftAnchor);
    moveToolSettings.Setup();
    dummyTransform = new GameObject("MoveDummyTransform").transform;
    dummyTransform.SetParent(transform);
    LoadToolMemory();

    ForceUpdateTargetActor();
  }

  private void LoadToolMemory()
  {
    moveToolSettings.SetShowSettings(PlayerPrefs.GetInt("moveTool-showSettings", 0) == 1 ? true : false);
    moveToolSettings.SetShowOffsets(PlayerPrefs.GetInt("moveTool-showOffsets", 0) == 1 ? true : false);
    moveToolSettings.SetSnappingSetting(PlayerPrefs.GetInt("moveTool-snapping", 0) == 1 ? true : false);
    moveToolSettings.SetAutosetSpawn(PlayerPrefs.GetInt("moveTool-autosetSpawn", 1) == 1 ? true : false);
    moveToolSettings.SetLocalSpace(PlayerPrefs.GetInt("moveTool-localSpace", 0) == 1 ? true : false);
  }

  private void SaveToolMemory()
  {
    PlayerPrefs.SetInt("moveTool-showSettings", moveToolSettings.GetShowSettings() ? 1 : 0);
    PlayerPrefs.SetInt("moveTool-showOffsets", moveToolSettings.GetShowOffsets() ? 1 : 0);
    PlayerPrefs.SetInt("moveTool-snapping", moveToolSettings.GetSnappingSetting() ? 1 : 0);
    PlayerPrefs.SetInt("moveTool-autosetSpawn", moveToolSettings.AutosetSpawn() ? 1 : 0);
    PlayerPrefs.SetInt("moveTool-localSpace", moveToolSettings.GetLocalSpace() ? 1 : 0);
  }

  public override void ForceUpdateTargetActor()
  {
    gizmoSelectionFeedback.SetActor(editMain.GetFocusedTargetActor());
  }

  public override void Close()
  {
    base.Close();
    SaveToolMemory();
    moveToolSettings.RequestDestroy();
    Destroy(dummyTransform.gameObject);
  }

  void Update()
  {
    if (!moving) GizmoCheck();
    gizmoParent.SetActive(moveToolSettings.GetShowSettings());
    gizmoSelectionFeedback.UpdateAxis(gizmoAxis, editMain.Using3DCamera());
    bool shouldUseLocalSpace = moveToolSettings.GetLocalSpace() && editMain.GetTargetActorsCount() == 1;
    if (shouldUseLocalSpace != (coordinateSystem == CoordinateSystem.Local))
    {
      SetCoordinateSystem(shouldUseLocalSpace ? CoordinateSystem.Local : CoordinateSystem.Global);
    }
  }

  void LateUpdate()
  {
    if (moving)
    {
      if (GetFocusActor() == null)
      {
        StopMove();
        return;
      }

      Vector3 moveDelta = MoveFocusedActor();

      for (int i = actorMoveDataList.Count - 1; i >= 0; i--)
      {
        if (GetInvalidActorReason(actorMoveDataList[i].actor) != null)
        {
          // Hmm..there's a chance its physics will be left disabled here. Eep.
          actorMoveDataList.RemoveAt(i);
          continue;
        }

        if (actorMoveDataList[i].actor != null)
        {
          // Physics makes moving weird, especially when networked. Do this
          // every frame in case some script enables it.
          actorMoveDataList[i].actor.SetEnablePhysics(false);
        }

        if (actorMoveDataList[i].actor == null || actorMoveDataList[i].actor == GetFocusActor()) continue;
        actorMoveDataList[i].Update(moveDelta);
      }

      toolEffectController.UpdateTargetPosition(GetFocusActor().GetWorldRenderBoundsCenter());
      toolEffectController.ExplicitLateUpdate();
    }
    else
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

    gizmoSelectionFeedback.UpdatePosition();
    gizmoSelectionFeedback.UpdateScale(editMain.GetCamera().transform.position, editMain.GetCamera().fieldOfView);
    heightFeedback.SetActor(GetFocusActor());
    heightFeedback.ExplicitUpdate();


  }

  void MoveFocusedActorAllAxes()
  {
    Ray ray = editMain.GetCursorRay();
    dummyTransform.position = editMain.GetCursorRay().GetPoint(grabRayDistanceOffset);
    SetFocusedActorPositionWithChecks(dummyTransform.TransformPoint(grabRayTransformOffset), SnapAxis.Both);
  }

  void MoveFocusedActorOnAxis(GizmoAxis axis)
  {
    Ray ray = editMain.GetCursorRay();
    Ray axisRay = zAxisRay;
    Vector3 offset = zAxisRayOffset;

    SnapAxis snapAxis = SnapAxis.Horizontal;
    if (axis == GizmoAxis.Y)
    {
      snapAxis = SnapAxis.Vertical;
      axisRay = yAxisRay;
      offset = yAxisRayOffset;
    }
    else if (axis == GizmoAxis.X)
    {
      axisRay = xAxisRay;
      offset = xAxisRayOffset;
    }

    Vector3 point = Util.GetClosestPointOnRayFromRay(axisRay, ray) - offset;
    SetFocusedActorPositionWithChecks(point, snapAxis);
  }


  // void MoveFocusedActorInIsometricY()
  // {
  //   Ray ray = editMain.GetCursorRay();
  //   Vector3 verticalNormal = GetFocusActor().GetPosition() - editMain.GetCamera().transform.position;
  //   verticalNormal.y = 0;
  //   verticalTargetPlane = new Plane(verticalNormal.normalized, GetFocusActor().GetPosition());

  //   float rayPlaneDist;
  //   if (verticalTargetPlane.Raycast(ray, out rayPlaneDist))
  //   {
  //     Vector3 rayPlaneIntersect = ray.GetPoint(rayPlaneDist);
  //     Vector3 newVerticalPosition = horizontalActorPosition;
  //     newVerticalPosition.y = (rayPlaneIntersect + verticalHitOffset).y;
  //     SetFocusedActorPositionWithChecks(newVerticalPosition, SnapAxis.Vertical);
  //   }
  // }

  void MoveFocusedActorInIsometricXZ()
  {
    Ray ray = editMain.GetCursorRay();
    float rayPlaneDist;
    if (horizontalTargetPlane.Raycast(ray, out rayPlaneDist))
    {
      Vector3 rayPlaneIntersect = ray.GetPoint(rayPlaneDist);
      Vector3 newPosition = horizontalActorPosition;
      // if (gizmoAxis == GizmoAxis.X)
      // {
      //   newPosition.x = (rayPlaneIntersect + horizontalHitOffset).x;
      // }
      // else if (gizmoAxis == GizmoAxis.Z)
      // {
      //   newPosition.z = (rayPlaneIntersect + horizontalHitOffset).z;
      // }
      // else
      // {
      newPosition = rayPlaneIntersect + horizontalHitOffset;
      // }
      SetFocusedActorPositionWithChecks(newPosition, SnapAxis.Horizontal);
    }
  }

  enum SnapAxis
  {
    Horizontal,
    Vertical,
    Both
  };

  Vector3 SnapOnAxis(SnapAxis axis, Vector3 vec)
  {
    if (axis == SnapAxis.Horizontal)
    {
      return TerrainManager.SnapHorizontalPosition(vec);
    }
    else if (axis == SnapAxis.Vertical)
    {
      return TerrainManager.SnapVerticalPosition(vec);

    }
    else
    {
      return TerrainManager.SnapPosition(vec);

    }
  }

  void SetFocusedActorPositionWithChecks(Vector3 vec, SnapAxis axis)
  {
    GetFocusActor().SetEnablePhysics(false);
    GetFocusActor().SweepTo(
      CheckDistanceAndLimits(moveToolSettings.ShouldSnap() ? SnapOnAxis(axis, vec) : vec)
       );
  }

  Vector3 MoveFocusedActor()
  {
    if (coordinateSystem == CoordinateSystem.Local)
    {
      // do we need this? it causes lag when moving player 1
      // GetFocusActor().SetRotation(initialRotation);
      CalculateAxisRays();
    }

    if (gizmoAxis == GizmoAxis.X || gizmoAxis == GizmoAxis.Y || gizmoAxis == GizmoAxis.Z)
    {
      MoveFocusedActorOnAxis(gizmoAxis);
    }
    else
    {
      if (editMain.Using3DCamera())
      {
        MoveFocusedActorAllAxes();
      }
      else
      {

        MoveFocusedActorInIsometricXZ();
      }
    }



    // Set both, for placing rolling physics objects.
    if (moveToolSettings.AutosetSpawn())
    {
      GetFocusActor().SetSpawnPosition(GetFocusActor().GetPosition());
      GetFocusActor().SetSpawnRotation(GetFocusActor().GetRotation());
    }


    Vector3 deltaPosition = GetFocusActor().GetPosition() - initialPosition;
    return deltaPosition;
  }

  private Vector3 CheckDistanceAndLimits(Vector3 newpos)
  {
    Vector3 worldMin = gbStage.GetWorldMin();
    Vector3 worldMax = gbStage.GetWorldMax();

    return new Vector3(
      Mathf.Clamp(newpos.x, worldMin.x, worldMax.x),
      Mathf.Clamp(newpos.y, worldMin.y, worldMax.y),
      Mathf.Clamp(newpos.z, worldMin.z, worldMax.z)
    );
  }

  public override bool GetToolEffectActive()
  {
    return moving;
  }

  public override int GetToolEffectTargetViewId()
  {
    return GetFocusActor() != null ? GetFocusActor().GetPrimaryPhotonViewId() : -1;
  }


}
