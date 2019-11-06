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

// TODO: rename. No longer isometric.
public class IsometricCameraView : CameraViewController
{
  float zoom = .3f;

  float ZOOM_BASE_VALUE = 3;
  float ZOOM_MULTIPLIER = 30;

  float VERTICAL_OFFSET_BASE_VALUE = 5;
  float VERTICAL_OFFSET_MULTIPLIER = 50;

  float CURSOR_VELOCITY_LERP = .25f;
  float CURSOR_VELOCITY_DECELERATION = .9f;

  float cameraVerticalOffset = 30;

  const float ROTATE_AMOUNT = 45;
  const float CAMERA_SMOOTHING = .1f;
  const float FOCUS_ACTOR_PADDING_MAGNITUDE = 1;

  Quaternion DEFAULT_ROTATION = Quaternion.Euler(45, 0, 0);

  float CURSOR_MOD = .5f;

  int currentRotationIncrement;

  bool cursorControlledMovement = false;
  // bool usingCursor;
  Vector3 focusPoint = Vector3.zero;

  Vector3 cursorVelocity = Vector2.zero;

  enum Focus
  {
    Avatar,
    FocusActor,
    Cursor
  }

  Focus currentFocus = Focus.Avatar;

  public override bool CursorActive()
  {
    return true;
  }

  public override bool TryCaptureCursor()
  {
    return false;
  }

  public override bool IsCursorCaptured()
  {
    return false;
  }

  public override bool TryReleaseCursor()
  {
    return false;
  }


  public override Vector2 GetDefaultSelectionPoint()
  {
    return navigationControls.userMain.GetMousePos();
  }

  public override float GetZoom()
  {
    return zoom;
  }

  public override Vector3 GetAvatarMoveVector()
  {
    return rotation * velocity;
  }

  public override Vector3 GetAimOrigin()
  {
    return navigationControls.userBody.transform.position /* HAAACCCCKK */ + new Vector3(0, 0.5f, 0);
  }

  public override void ControllerUpdate(int fixedUpdatesSinceLastUpdate)
  {
    if (navigationControls.userBody == null) return;
    InputCheck();
    UpdateFocus();
    UpdateVelocity();
    UpdateAvatarRotationValue();

    lastFixedUpdates = fixedUpdatesSinceLastUpdate;
  }

  int lastFixedUpdates = 0;

  public override void ControllerLateUpdate()
  {
    UpdateCameraPosition(lastFixedUpdates);
  }

  void UpdateVelocity()
  {
    Vector3 newVec = Vector3.zero;
    Vector3 inputAxes = navigationControls.inputControl.GetMoveAxes();

    if (inputAxes != Vector3.zero && !navigationControls.inputControl.GetButton("MoveCamera"))
    {
      cursorControlledMovement = false;
    }

    if (currentFocus == Focus.Cursor)
    {
      velocity = focusPoint - navigationControls.userBody.transform.position;
      if (Vector3.SqrMagnitude(velocity) < .01f) velocity = Vector3.zero;
    }

    else if (currentFocus == Focus.FocusActor)
    {
      Bounds bounds = navigationControls.GetCameraLockActor().ComputeWorldRenderBounds();
      Vector3 closestPoint = bounds.ClosestPoint(navigationControls.userBody.transform.position);
      Vector3 closestPointPaddingVector = (closestPoint - bounds.center).normalized * FOCUS_ACTOR_PADDING_MAGNITUDE;
      velocity = closestPointPaddingVector + closestPoint - navigationControls.userBody.transform.position;
      if (Vector3.SqrMagnitude(velocity) < .01f) velocity = Vector3.zero;
    }

    else
    {
      Vector3 rightVec = mainTransform.right;
      rightVec.y = 0;

      Vector3 forwardVec = mainTransform.forward;
      forwardVec.y = 0;

      newVec += rightVec.normalized * inputAxes.x;
      newVec += forwardVec.normalized * inputAxes.z;
      newVec += Vector3.up * inputAxes.y;

      velocity = newVec.normalized;
    }
  }

  void UpdateFocus()
  {
    if (navigationControls.GetCameraFollowingActor())
    {
      currentFocus = Focus.FocusActor;
    }
    else
    {
      currentFocus = cursorControlledMovement ? Focus.Cursor : Focus.Avatar;
    }
  }

  bool CursorVelocityNearZero()
  {
    return cursorVelocity.magnitude < .01f;
  }

  void InputCheck()
  {
    if (!navigationControls.userMain.InEditMode())
    {
      cursorControlledMovement = false;
      return;
    }

    if (navigationControls.inputControl.GetButtonDown("MoveCamera") && !navigationControls.userMain.CursorOverUI())
    {
      cursorControlledMovement = true;
    }


    if (cursorControlledMovement)
    {
      if (navigationControls.inputControl.GetButton("MoveCamera") && !navigationControls.userMain.CursorOverUI())
      {
        Vector3 instantVelocity = Vector3.zero;
        Vector2 mouseDelta = -cameraVerticalOffset * .05f * navigationControls.inputControl.GetMouseAxes();
        Vector3 rightVec = mainTransform.right;
        rightVec.y = 0;

        Vector3 forwardVec = mainTransform.forward;
        forwardVec.y = 0;

        instantVelocity = rightVec.normalized * mouseDelta.x + forwardVec.normalized * mouseDelta.y;
        focusPoint += instantVelocity;

        cursorVelocity = Vector3.Lerp(cursorVelocity, instantVelocity, CURSOR_VELOCITY_LERP);
        focusPoint += instantVelocity;
      }
      else
      {
        cursorVelocity = cursorVelocity * CURSOR_VELOCITY_DECELERATION;
        focusPoint += cursorVelocity;
      }
    }


    if (navigationControls.GetCameraFollowingActor())
    {
      if (navigationControls.HasMoveInput())
      {
        navigationControls.SetCameraFollowingActor(false);
      }
    }
  }

  float GetMaxEditHeight()
  {
    return Mathf.Infinity;
  }


  float GetMinEditHeight()
  {
    return Mathf.NegativeInfinity;
  }

  public override void MoveCameraToActor(VoosActor actor)
  {
    // Vector3 groundCenter = navigationControls.GetGroundPoint(actor.GetPosition().y, true);
    // focusPoint = groundCenter;

    // Vector3 delta = actor.GetPosition() - groundCenter;
    // delta.y = actor.GetPosition().y + cameraVerticalOffset - mainTransform.position.y;
    // Vector3 desiredCameraPosition = mainTransform.position + delta;
    // mainTransform.position = desiredCameraPosition;

    Bounds bounds = actor.ComputeWorldRenderBounds();
    Vector3 closestPoint = bounds.ClosestPoint(navigationControls.userBody.transform.position);
    Vector3 closestPointPaddingVector = (closestPoint - bounds.center).normalized * FOCUS_ACTOR_PADDING_MAGNITUDE;
    focusPoint = closestPointPaddingVector + closestPoint;
    //   if (Vector3.SqrMagnitude(velocity) < .01f) velocity = Vector3.zero;

    // focusPoint = actor.GetPosition();
    //cursorVelocity = Vector3.zero;
    cursorControlledMovement = true;
  }

  void UpdateCameraPosition(int fixedUpdates)
  {
    Vector3 delta;

    float targetDeltaTime = Time.unscaledDeltaTime;

    if (currentFocus == Focus.Avatar)
    {
      float userBodyY = Mathf.Clamp(navigationControls.userBody.transform.position.y, GetMinEditHeight(), GetMaxEditHeight());
      Vector3 groundCenter = navigationControls.GetHorizontalPlanePoint(userBodyY, true);
      focusPoint = groundCenter;
      delta = navigationControls.userBody.transform.position - groundCenter;
      delta.y = userBodyY + cameraVerticalOffset - mainTransform.position.y;

      if (navigationControls.userBody.GetComponentInParent<VoosActor>() != null)
      {
        // If we're parented to an actor, assume the actor is being moved during
        // FixedUpdate. So change our dampDeltaTime to make sure we don't
        // "overshoot" it (at least, to the extent that damped updates can
        // overshoot and look jittery).
        targetDeltaTime = fixedUpdates * Time.fixedUnscaledDeltaTime;
      }
    }
    else if (currentFocus == Focus.FocusActor && navigationControls.GetCameraLockActor() != null)
    {
      Vector3 groundCenter = navigationControls.GetHorizontalPlanePoint(navigationControls.GetCameraLockActor().transform.position.y, true);
      focusPoint = groundCenter;
      delta = navigationControls.GetCameraLockActor().transform.position - groundCenter;
      delta.y = navigationControls.GetCameraLockActor().transform.position.y + cameraVerticalOffset - mainTransform.position.y;
    }
    else
    {
      Vector3 groundCenter = navigationControls.GetHorizontalPlanePoint(focusPoint.y, true);
      delta = focusPoint - groundCenter;
      delta.y = focusPoint.y + cameraVerticalOffset - mainTransform.position.y;
    }

    if (currentFocus == Focus.Cursor)
    {
      if (deadZoneMode)
      {
        deadZoneMode = false;
        mainToDeadDampVel = Vector3.zero;
      }

      mainTransform.position = Vector3.SmoothDamp(
        mainTransform.position,
        mainTransform.position + delta,
        ref mainToDeadDampVel,
        0.05f,
        999f);
    }
    else
    {
      bool letRotationHandlePos = false;
      if (!deadZoneMode || justRotated || justZoomed)
      {
        letRotationHandlePos = justRotated;
        // Reset
        deadZoneMode = true;
        justRotated = false;
        justZoomed = false;
        mainToDeadDampVel = Vector3.zero;
        deadZonePos = mainTransform.position;
      }

      if (!letRotationHandlePos)
      {
        Vector3 lockPos = mainTransform.position + delta;
        float Dmax = 2f;
        Vector3 deadToLock = lockPos - deadZonePos;
        if (deadToLock.magnitude > Dmax)
        {
          float catchup = deadToLock.magnitude - Dmax;
          deadZonePos += deadToLock.normalized * catchup;
        }

        // Lerp to dead zone pos
        mainTransform.position = Vector3.SmoothDamp(mainTransform.position, deadZonePos, ref mainToDeadDampVel, 0.1f, 999f, targetDeltaTime);
      }
    }
  }

  bool justRotated = false;
  bool justZoomed = false;
  bool deadZoneMode = false;
  Vector3 deadZonePos = Vector3.zero;
  Vector3 mainToDeadDampVel = Vector3.zero;

  void UpdateAvatarRotationValue()
  {
    if (currentFocus == Focus.FocusActor)
    {
      rotation = Quaternion.LookRotation(navigationControls.GetCameraLockActor().transform.position - navigationControls.userBody.transform.position);
      return;
    }

    Vector3 targPoint = navigationControls.GetHorizontalPlanePoint(navigationControls.userBody.transform.position.y);
    rotation = Quaternion.LookRotation(targPoint - navigationControls.userBody.transform.position);

  }

  Vector3 GetDesiredCameraPosition()
  {
    float userBodyY = Mathf.Clamp(navigationControls.userBody.transform.position.y, GetMinEditHeight(), GetMaxEditHeight());
    Vector3 groundCenter = navigationControls.GetHorizontalPlanePoint(userBodyY, true);
    Vector3 delta = navigationControls.userBody.transform.position - groundCenter;

    delta.y = userBodyY + cameraVerticalOffset - mainTransform.position.y;
    return mainTransform.position + delta;
  }

  public void RotateViewByIncrement(int amount)
  {
    //find position relative ground center
    mainTransform.rotation = DEFAULT_ROTATION;
    mainTransform.position = GetDesiredCameraPosition();

    mainTransform.RotateAround(navigationControls.userBody.transform.position, Vector3.up, ROTATE_AMOUNT * (amount + 1)); //+1 is so 0 index has an angle to it
    mainTransform.position = GetDesiredCameraPosition();
    currentRotationIncrement = amount;
    justRotated = true;
  }

  int lastRotatingFrame = -1;

  public void RotateView(float amount)
  {
    mainTransform.RotateAround(navigationControls.userBody.transform.position, Vector3.up, amount); //+1 is so 0 index has an angle to it

    if (lastRotatingFrame != Time.frameCount - 1)
    {
      mainToDeadDampVel = Vector3.zero;
    }

    mainTransform.position = Vector3.SmoothDamp(mainTransform.position,
      GetDesiredCameraPosition(),
      ref mainToDeadDampVel, 0.05f, 999f);
    justRotated = true;
    lastRotatingFrame = Time.frameCount;
  }

  public override void SetCamera()
  {
    navigationControls.ToggleCullingMask(false);

    navigationControls.targetCamera.transform.SetParent(mainTransform);
    navigationControls.targetCamera.orthographic = false;
    navigationControls.targetCamera.transform.localPosition = Vector3.zero;
    navigationControls.targetCamera.transform.localRotation = Quaternion.identity;

    SetZoom(zoom);
    RotateViewByIncrement(currentRotationIncrement);
  }

  public override void SetZoom(float f)
  {

    float newVerticalOffset = VERTICAL_OFFSET_BASE_VALUE + VERTICAL_OFFSET_MULTIPLIER * f;

    float deltaOffset = cameraVerticalOffset - newVerticalOffset;
    Vector3 cameraForwardVec = mainTransform.forward;
    mainTransform.position -= cameraForwardVec * (deltaOffset / cameraForwardVec.y);

    cameraVerticalOffset = newVerticalOffset;

    zoom = f;

    justZoomed = true;
  }

  public override Quaternion GetAimRotation()
  {
    return navigationControls.userBody.transform.rotation;
  }
}
