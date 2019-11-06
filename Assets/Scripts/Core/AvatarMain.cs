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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarMain : MonoBehaviour
{

  [SerializeField] protected NavigationControls navigationControls;
  protected UserMain userMain;
  [SerializeField] protected Transform avatarTransform;
  public RectTransform consoleAnchor;
  public Transform bodyParent;
  protected InputControl inputControl;

  public virtual void Setup(UserMain _usermain)
  {
    userMain = _usermain;
    inputControl = userMain.GetInputControl();
    navigationControls.Setup();
  }

  public void SetAvatarTransform(Transform t)
  {
    avatarTransform = t;
  }

  public void AddDebugMessage(string s)
  {
    userMain.AddDebugMessage(s);
  }

  public RectTransform GetMainRect()
  {
    return userMain.GetMainRect();
  }

  public virtual Vector3 GetAvatarPosition()
  {
    return avatarTransform != null ? avatarTransform.position : Vector3.zero;
  }

  public virtual Quaternion GetAim()
  {
    return navigationControls.GetAim();
  }

  public virtual void Teleport(Vector3 newPos, Quaternion newRot)
  {
    if (avatarTransform != null)
    {
      avatarTransform.position = newPos;
    }
    navigationControls.UpdateRotationValues(newRot);
  }

  public void SetUserBody(UserBody _userbody)
  {
    navigationControls.SetUserBody(_userbody);
  }

  public Camera GetCamera()
  {
    return navigationControls.targetCamera;
  }

  public InputControl GetInputControl()
  {
    return inputControl;
  }

  public virtual void Activate(bool on)
  {
    //targetCamera.enabled = on;

    gameObject.SetActive(on);
  }

  public bool IsActive()
  {
    return gameObject.activeSelf;
  }

  public virtual bool MouseLookActive()
  {
    return true;
  }

  public virtual void OnCameraViewUpdate(CameraView cv)
  {

  }

  public void SetMouseoverTooltipText(string newtext)
  {
    userMain.SetMouseoverTooltipText(newtext);
  }


  public virtual bool CursorActive()
  {
    return navigationControls.CursorActive();
  }

  public virtual bool KeyLock()
  {
    return userMain.ShouldAvatarKeyLock();
  }


  //returns if there's any menu action on escape
  //if nothing happens user main can do something with it
  public virtual bool OnEscape()
  {
    return false;
  }

  public bool CursorOverUI()
  {
    return userMain.CursorOverUI();
  }

  public Vector3 GetGroundPoint(float height, bool useCenter = false)
  {
    return navigationControls.GetHorizontalPlanePoint(height, useCenter);
  }

  public bool IsYLocked()
  {
    return navigationControls.GetCameraView() == CameraView.Isometric
      || navigationControls.GetCameraView() == CameraView.TopDown;
  }

}
