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
using UnityEngine.Networking;

// Biped driver for non-player-controllable actors.
// This exists so that animated characters like the fox can "walk/run"
// even if they are NPCs.
public class NonPlayerBipedDriver : MonoBehaviour, IBipedDriver
{
  VoosActor actor;
  public void Setup(VoosActor actor)
  {
    this.actor = actor;
  }

  public Vector3 GetMoveThrottle()
  {
    return Quaternion.Inverse(actor.GetRotation()) * actor.GetDesiredVelocity();
  }

  public Vector3 GetLookDirection()
  {
    return actor.GetRotation() * Vector3.forward;
  }

  public bool IsGrounded()
  {
    return false;
  }

  public bool IsValid()
  {
    return this != null && this.gameObject != null && actor != null;
  }

  public event System.Action onJump;
}
