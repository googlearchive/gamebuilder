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

public class ThirdPersonCameraView : CameraViewController
{
  // Lerp factors for moving the camera closer/farther from the player. Must be in ]0,1]
  // 1 means "move it instantly", 0.1 means "move it slowly".
  const float LERP_FACTOR_MOVING_CLOSER = .5f;
  const float LERP_FACTOR_MOVING_FARTHER = .1f;

  public Vector2 lookRotation = Vector2.zero;

  float zoom = .75f;
  const float SPHERE_CAST_RADIUS_DEFAULT = .3f;
  const float SPHERE_CAST_RADIUS_LOOKING_UP = .55f;
  const float TRANSPARENT_HEAD_DISTANCE_THRESHOLD = .6f;

  Transform cameraPivot;
  Transform sphereCastOrigin;
  Transform cameraPivotAnchor; //the one on the avatar we try to match

  NavigationControls.Mode mode = NavigationControls.Mode.Fly;

  Vector3 defaultCameraOffset = new Vector3(0, .6f, 0f);
  Vector3 cameraOffsetMod = Vector3.zero;

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

  public override float GetZoom()
  {
    return zoom;
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

  public override void MoveCameraToActor(VoosActor actor)
  {
    Vector3 tempVec = actor.GetPosition() - mainTransform.position;
    // Prevent camera from spinning around when we are almost perfectly above the focus object.
    if (Vector3.Angle(tempVec, Vector3.up) <= 165)
    {
      rotation = Quaternion.LookRotation(tempVec);
      UpdateRotationValues(rotation);
      cameraPivot.rotation = rotation;
    }
  }

  public override void ControllerUpdate(int fixedUpdates)
  {
    Vector2 mouseDelta = navigationControls.inputControl.GetLookAxes();

    if (!navigationControls.GetCameraFollowingActor())
    {
      if (navigationControls.MouseLookActive())
      {
        lookRotation.x += mouseDelta.x;
        lookRotation.y = Mathf.Clamp(lookRotation.y + mouseDelta.y, -80, 80);
      }
      rotation = Quaternion.Euler(-lookRotation.y, lookRotation.x, 0);
      cameraPivot.rotation = Quaternion.Euler(-lookRotation.y, lookRotation.x, 0);
    }
    else
    {
      Vector3 tempVec = navigationControls.GetCameraLockActor().transform.position - mainTransform.position;
      // Prevent camera from spinning around when we are almost perfectly above the focus object.
      if (Vector3.Angle(tempVec, Vector3.up) <= 165)
      {
        rotation = Quaternion.LookRotation(tempVec);
        UpdateRotationValues(rotation);
        cameraPivot.rotation = rotation;
      }
    }


    CameraCollisionCheck();

    Vector3 controllerSpaceMove = navigationControls.inputControl.GetMoveAxes();
    if (mode == NavigationControls.Mode.Grounded)
    {
      controllerSpaceMove.y = 0f;
    }

    //making y world relative
    // float vertical = controllerSpaceMove.y;
    // controllerSpaceMove.y = 0;

    Vector3 worldSpaceMove = cameraPivot.rotation * controllerSpaceMove;
    if (mode == NavigationControls.Mode.Grounded)
    {
      worldSpaceMove.y = 0f;
    }
    // else
    // {
    //   worldSpaceMove.y = vertical;
    // }
    velocity = worldSpaceMove.normalized;
  }

  public float GetZOffset()
  {
    return -5;
  }

  public override void SetCamera()
  {
    navigationControls.ToggleCullingMask(false);
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

  public void SetupThirdPersonTransforms(Transform _pivotTransform, Transform _spherecastTransform)
  {
    cameraPivot = _pivotTransform;
    sphereCastOrigin = _spherecastTransform;

    lookRotation.x = cameraPivot.eulerAngles.y;
    lookRotation.y = cameraPivot.eulerAngles.x;
    lookRotation.y = Mathf.DeltaAngle(lookRotation.y, 0);
  }

  public void SetTargetPivotAnchor(Transform _newtarget)
  {
    cameraPivotAnchor = _newtarget;
  }

  public override void ControllerLateUpdate()
  {
    cameraPivot.position = cameraPivotAnchor.position;
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

  public override Vector3 GetAvatarMoveVector()
  {
    return navigationControls.inputControl.GetMoveAxes();
  }

  bool IsValidCameraCollision(RaycastHit _hit)
  {
    return _hit.transform.tag == EditMain.GROUND_TAG || _hit.transform.tag == EditMain.WALL_TAG || _hit.collider.transform.name == "WallChild";
  }

  void CameraCollisionCheck()
  {
    // If the camera is looking up, use a larger sphere cast radius to prevent us from going through the ground.
    float sphereCastRadius = cameraPivot.forward.y > 0 ? SPHERE_CAST_RADIUS_LOOKING_UP : SPHERE_CAST_RADIUS_DEFAULT;

    RaycastHit[] xCastHits;

    float spherecastOffsetRelativeX = 1f;
    float spherecastOffsetAbsoluteY = 1f;
    float spherecastOffsetRelativeZ = 5;

    // Initial origin for the sphere cast (right on top of the player's head).
    Vector3 castOrigin = cameraPivot.position + Vector3.up * spherecastOffsetAbsoluteY;

    // First, try "optimistic" casting, to check if the top of the player's head is already visible from the
    // desired camera offset. If it is, that's where the camera should be, and end of story.
    bool easySuccess = true;
    Vector3 idealCameraWorldPos = castOrigin + cameraPivot.right * spherecastOffsetRelativeX
        - spherecastOffsetRelativeZ * cameraPivot.forward;
    xCastHits = Physics.SphereCastAll(castOrigin, sphereCastRadius, idealCameraWorldPos - castOrigin,
        Vector3.Distance(castOrigin, idealCameraWorldPos), navigationControls.GetLayerMask(), QueryTriggerInteraction.Ignore);
    if (xCastHits.Length > 0)
    {
      for (int i = 0; i < xCastHits.Length; i++)
      {
        if (IsValidCameraCollision(xCastHits[i]))
        {
          // Something is blocking the camera's view of the player.
          easySuccess = false;
          break;
        }
      }
    }

    if (easySuccess)
    {
      cameraOffsetMod.x = spherecastOffsetRelativeX;
      cameraOffsetMod.z = -spherecastOffsetRelativeZ;
    }
    else
    {
      // We have to do it the hard way.
      // Cast a sphere to the right to find out where it hits an obstacle, then cast it back to find out how far back
      // we can go with the camera.
      float spherecastXdistance = spherecastOffsetRelativeX;
      xCastHits = Physics.SphereCastAll(castOrigin, SPHERE_CAST_RADIUS_DEFAULT, cameraPivot.right, spherecastOffsetRelativeX, navigationControls.GetLayerMask(), QueryTriggerInteraction.Ignore);
      if (xCastHits.Length > 0)
      {
        for (int i = 0; i < xCastHits.Length; i++)
        {
          if (IsValidCameraCollision(xCastHits[i]))
          {
            if (spherecastXdistance > xCastHits[i].distance - .1f)
            {
              spherecastXdistance = xCastHits[i].distance - .1f;
            }
          }
        }

      }

      castOrigin = cameraPivot.position + cameraPivot.right * spherecastXdistance + Vector3.up * spherecastOffsetAbsoluteY;

      //this is just for debug viz
      sphereCastOrigin.position = castOrigin;

      //do z-axis cast
      float spherecastZdistance = spherecastOffsetRelativeZ;
      RaycastHit[] zCastHits;
      zCastHits = Physics.SphereCastAll(castOrigin, sphereCastRadius, cameraPivot.forward * -1, spherecastOffsetRelativeZ, navigationControls.GetLayerMask(), QueryTriggerInteraction.Ignore);
      if (zCastHits.Length > 0)
      {
        for (int i = 0; i < zCastHits.Length; i++)
        {
          if (IsValidCameraCollision(zCastHits[i]))
          {
            if (spherecastZdistance > zCastHits[i].distance)
            {
              spherecastZdistance = zCastHits[i].distance;
            }
            break;
          }
        }
      }

      cameraOffsetMod.x = spherecastXdistance;
      cameraOffsetMod.z = -1 * spherecastZdistance;
    }

    float zDelta = mainTransform.localPosition.z - (defaultCameraOffset + cameraOffsetMod).z;
    // Calculate the lerp factor depending on whether we are moving the camera closer to the player
    // or farther from the player. For the closer case, we want it to lerp faster because
    // it will be clipping through walls.
    float lerpFactor = zDelta < 0 ? LERP_FACTOR_MOVING_CLOSER : LERP_FACTOR_MOVING_FARTHER;
    mainTransform.localPosition = Vector3.Lerp(mainTransform.localPosition, defaultCameraOffset + cameraOffsetMod, lerpFactor);

    if (-cameraOffsetMod.z < TRANSPARENT_HEAD_DISTANCE_THRESHOLD)
    {
      navigationControls.userBody.SetVariableTransparency(0);
    }
    else
    {
      navigationControls.userBody.SetVariableTransparency(1);
    }
  }

  public void SetMode(NavigationControls.Mode mode)
  {
    this.mode = mode;
  }

  public override Quaternion GetAimRotation()
  {
    return GetRotation();
  }
}
