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

public class FPSCameraView : CameraViewController
{
  Vector2 lookRotation = Vector2.zero;

  float zoom = .75f;

  float wheelHeightOffset = 1.2f;
  float flyHeightOffset = 0f;

  NavigationControls.Mode mode = NavigationControls.Mode.Fly;

  Transform wheelTargetPositionTransform;
  Transform flyTargetPositionTransform;


  // public override Vector2 GetDefaultSelectionPoint()
  // {
  //   return new Vector2(0.5f, 0.5f);
  // }

  public override float GetZoom()
  {
    return zoom;
  }

  public void SetTargetPositionAnchors(Transform _wheeltarget, Transform _flytarget)
  {
    wheelTargetPositionTransform = _wheeltarget;
    flyTargetPositionTransform = _flytarget;
  }

  public override bool CursorActive()
  {
    return InEditMode() ? true : base.CursorActive();
  }

  public override bool IsCursorCaptured()
  {
    return InEditMode() ? true : base.IsCursorCaptured();
  }

  private bool InEditMode()
  {
    return navigationControls.userMain.InEditMode();

  }

  public override Vector2 GetDefaultSelectionPoint()
  {
    return navigationControls.MouseLookActive() ? new Vector2(0.5f, 0.5f) :
      navigationControls.userMain.GetMousePos();
  }


  public override Vector2 GetLookRotation()
  {
    return lookRotation;
  }

  public override void SetLookRotation(Vector2 newRotation)
  {
    lookRotation = newRotation;
  }

  public override Vector3 GetAimOrigin()
  {
    // Return the camera's origin, NOT the player body's.
    return mainTransform.position;
  }

  public override Vector3 GetAvatarMoveVector()
  {
    return navigationControls.inputControl.GetMoveAxes();
  }

  // bool MouseLookActive()
  // {
  //   return navigationControls.inputControl.GetButtonDown("MoveCamera")
  //   && !navigationControls.userMain.CursorOverUI();
  // }


  public override void ControllerUpdate(int fixedUpdates)
  {
    Vector2 mouseDelta = navigationControls.inputControl.GetLookAxes();
    // navigationControls.userMain.ToggleReticles(true);

    if (!navigationControls.GetCameraFollowingActor())
    {
      if (navigationControls.MouseLookActive())
      {
        lookRotation.x += mouseDelta.x;
        lookRotation.y = Mathf.Clamp(lookRotation.y + mouseDelta.y, -80, 80);
      }
      rotation = Quaternion.Euler(-lookRotation.y, lookRotation.x, 0);
      mainTransform.rotation = Quaternion.Euler(-lookRotation.y, lookRotation.x, 0);
    }
    else
    {
      Vector3 tempvec = navigationControls.GetCameraLockActor().transform.position - mainTransform.position;
      rotation = Quaternion.LookRotation(tempvec);
      UpdateRotationValues(rotation);
      mainTransform.rotation = rotation;
    }

    //keyboard
    Vector3 newVec = Vector3.zero;
    Vector3 tempAxes = navigationControls.inputControl.GetMoveAxes();

    if (mode == NavigationControls.Mode.Grounded)
    {
      tempAxes.y = 0f;
    }

    newVec += mainTransform.right * tempAxes.x;
    newVec += mainTransform.up * tempAxes.y;
    // newVec += Vector3.up * tempAxes.y;
    newVec += mainTransform.forward * tempAxes.z;

    velocity = newVec.normalized;
  }

  public override void ControllerLateUpdate()
  {
    if (mode == NavigationControls.Mode.Fly)
    {

      mainTransform.position = flyTargetPositionTransform.position + Vector3.up * flyHeightOffset;
    }
    else
    {
      mainTransform.position = wheelTargetPositionTransform.position + Vector3.up * wheelHeightOffset;

    }
  }


  public override void SetCamera()
  {
    navigationControls.ToggleCullingMask(true);
    navigationControls.targetCamera.transform.SetParent(mainTransform);
    navigationControls.targetCamera.orthographic = false;
    navigationControls.targetCamera.transform.localPosition = Vector3.zero;
    navigationControls.targetCamera.transform.localRotation = Quaternion.identity;
    SetZoom(zoom);
  }

  public void UpdateRotationValues(Quaternion q)
  {
    lookRotation.x = q.eulerAngles.y;
    lookRotation.y = q.eulerAngles.x;
    lookRotation.y = Mathf.DeltaAngle(lookRotation.y, 0);
  }

  public override void Setup(NavigationControls _nav, Transform _mainT)
  {
    base.Setup(_nav, _mainT);


    //setup rotation
    lookRotation.x = mainTransform.eulerAngles.y;
    lookRotation.y = mainTransform.eulerAngles.x;
    lookRotation.y = Mathf.DeltaAngle(lookRotation.y, 0);
  }

  public override void SetZoom(float f)
  {
    navigationControls.targetCamera.fieldOfView = Mathf.Lerp(150, 30, f);
    zoom = f;
  }

  public void SetMode(NavigationControls.Mode mode)
  {
    this.mode = mode;
  }

  public override Quaternion GetAimRotation()
  {
    return rotation;
  }

  public override void MoveCameraToActor(VoosActor actor)
  {
    Vector3 tempvec = actor.GetPosition() - mainTransform.position;
    rotation = Quaternion.LookRotation(tempvec);
    UpdateRotationValues(rotation);
    mainTransform.rotation = rotation;
  }
}
