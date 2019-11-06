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

public class ActorDrivenCameraView : CameraViewController
{
  private float zoom = .75f;

  VoosActor cameraActor;
  float cooldownOwnershipRequestUntil;

  public void SetCameraActor(VoosActor cameraActor)
  {
    this.cameraActor = cameraActor;
    cameraActor.RequestOwnership();
  }

  public VoosActor GetCameraActor()
  {
    return cameraActor;
  }

  public override bool CursorActive()
  {
    // return cameraActor != null ? cameraActor.GetCameraSettings().cursorActive : false;
    return cameraActor != null ? cameraActor.GetCameraSettings().cursorActive || cursorActive : cursorActive;
  }

  public override bool IsCursorCaptured()
  {
    if (cameraActor != null)
    {
      return cameraActor.GetCameraSettings().cursorActive ? false : base.IsCursorCaptured();
    }
    else
    {
      return base.IsCursorCaptured();
    }
  }

  public override Vector2 GetDefaultSelectionPoint()
  {
    return new Vector2(0.5f, 0.5f);
  }

  public override float GetZoom()
  {
    return zoom;
  }

  public override Vector2 GetLookRotation()
  {
    return Vector2.zero;
  }

  public override void SetLookRotation(Vector2 newRotation)
  {

  }

  public override Vector3 GetAvatarMoveVector()
  {
    return Vector3.forward;
  }

  public override void ControllerUpdate(int fixedUpdates)
  {
    Vector2 mouseDelta = navigationControls.inputControl.GetLookAxes();
    Vector3 moveAxes = navigationControls.inputControl.GetMoveAxes();
    Quaternion rot = Quaternion.Euler(0, mainTransform.rotation.eulerAngles.y, 0);
    moveAxes = (rot * moveAxes).WithY(0);
    velocity = moveAxes.sqrMagnitude > 0.0001 ? moveAxes.normalized : Vector3.zero;

    if (cameraActor != null)
    {
      VoosActor.CameraSettings camSettings = cameraActor.GetCameraSettings();
      navigationControls.targetCamera.orthographic = false;
      navigationControls.targetCamera.fieldOfView = camSettings.fov > 0 ? camSettings.fov : 60;

      // If we don't own the camera actor, keep politely insisting on getting ownership...
      if (!cameraActor.IsLocallyOwned() && Time.unscaledTime > cooldownOwnershipRequestUntil)
      {
        cameraActor.RequestOwnership();
        cooldownOwnershipRequestUntil = Time.unscaledTime + 2;
      }
    }

    rotation = GetAimRotation();
  }

  public override void ControllerLateUpdate()
  {
    mainTransform.position = cameraActor != null ? cameraActor.GetPosition() : Vector3.zero;
    mainTransform.rotation = cameraActor != null ? cameraActor.GetRotation() : Quaternion.identity;
  }

  public override void SetCamera()
  {
    navigationControls.ToggleCullingMask(false);
    navigationControls.userMain.ToggleReticles(true);
    navigationControls.targetCamera.transform.SetParent(mainTransform);
    navigationControls.targetCamera.orthographic = false;
    navigationControls.targetCamera.transform.localPosition = Vector3.zero;
    navigationControls.targetCamera.transform.localRotation = Quaternion.identity;
    SetZoom(zoom);
  }

  public void UpdateRotationValues(Quaternion q)
  {
  }

  public override void Setup(NavigationControls _nav, Transform _mainT)
  {
    base.Setup(_nav, _mainT);
  }

  public override void SetZoom(float f)
  {
    navigationControls.targetCamera.fieldOfView = Mathf.Lerp(150, 30, f);
    zoom = f;
  }

  public void SetMode(NavigationControls.Mode mode)
  {
  }

  public override Quaternion GetAimRotation()
  {
    return cameraActor != null ? Quaternion.FromToRotation(Vector3.forward, cameraActor.GetCameraSettings().aimDir) : Quaternion.identity;
  }

  public override Vector3 GetAimOrigin()
  {
    return cameraActor != null ? cameraActor.GetCameraSettings().aimOrigin : Vector3.zero;
  }

  public override void MoveCameraToActor(VoosActor actor)
  {
    //
  }
}
