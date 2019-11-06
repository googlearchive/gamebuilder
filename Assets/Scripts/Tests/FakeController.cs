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

public class FakeController : PlayerBody.Controller, PlayerBody.EventHandler, PlayerBody.ControllerInput
{
  public bool jumpRequested = false;
  public Quaternion aim = Quaternion.identity;
  public Vector3 aimOrigin = Vector3.zero;
  public Vector3 inputThrottle = Vector3.zero;
  public Vector3 worldThrottle = Vector3.zero;
  public string name = "";
  public bool action1Requested = false;
  public bool action2Requested = false;
  public bool groundingRequested = false;
  public bool active = true;

  // Flags to check for event firing.
  // Make sure to clear them before expecting them.
  public bool onJumpedCalled = false;
  public bool onJumpDeniedCalled = false;
  public bool onLandedCalled = false;
  public bool onDiedCalled = false;
  public bool onDamagedCalled = false;
  public bool onRespawnedCalled = false;

  public string GetName()
  {
    return name;
  }

  public bool IsJumpRequested()
  {
    return jumpRequested;
  }

  public Quaternion GetAim()
  {
    return aim;
  }

  public Vector3 GetAimOrigin()
  {
    return aimOrigin;
  }

  public bool IsAction1Requested()
  {
    return action1Requested;
  }

  public bool IsAction2Requested()
  {
    return action2Requested;
  }

  Vector3 PlayerBody.ControllerInput.GetWorldSpaceThrottle()
  {
    return worldThrottle;
  }

  public bool IsGroundingRequested()
  {
    return groundingRequested;
  }


  public PlayerBody.EventHandler GetEventHandler()
  {
    return this;
  }

  void PlayerBody.EventHandler.OnJumped()
  {
    this.onJumpedCalled = true;
  }

  void PlayerBody.EventHandler.OnLanded()
  {
    this.onLandedCalled = true;
  }

  void PlayerBody.EventHandler.OnJumpDenied()
  {
    this.onJumpDeniedCalled = true;
  }

  void PlayerBody.EventHandler.OnDamaged()
  {
    this.onDamagedCalled = true;
  }

  void PlayerBody.EventHandler.OnRespawned()
  {
    this.onRespawnedCalled = true;
  }

  void PlayerBody.EventHandler.OnDied()
  {
    this.onDiedCalled = true;
  }

  public PlayerBody.ControllerInput GetInput()
  {
    return this;
  }

  Vector2 PlayerBody.ControllerInput.GetLookAxes()
  {
    return Vector2.zero;
  }

  public void OnTintChanged(Color tint)
  {
  }

  public Vector3 GetInputThrottle()
  {
    return inputThrottle;
  }

  public bool GetKeyDown(string keyName)
  {
    throw new System.NotImplementedException();
  }

  public bool GetKeyHeld(string keyName)
  {
    throw new System.NotImplementedException();
  }

  public bool IsSprinting()
  {
    return false;
  }
}
