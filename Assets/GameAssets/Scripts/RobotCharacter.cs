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

public class RobotCharacter : MonoBehaviour
{
  const float MOVE_SPEED = 5;

  [SerializeField] Animator animator;
  IBipedDriver driver;

  public void SetDriver(IBipedDriver driver)
  {
    this.driver = driver;
  }

  void Update()
  {
    if (driver == null) return;
    animator.SetBool("IsGrounded", driver.IsGrounded());

    Vector3 relativeVelocity = Quaternion.Inverse(Quaternion.LookRotation(transform.forward)) * driver.GetMoveThrottle();
    animator.SetFloat("VelX", relativeVelocity.x / MOVE_SPEED, 0.5f, Time.unscaledDeltaTime * 7.5f);
    animator.SetFloat("VelY", relativeVelocity.z / MOVE_SPEED, 0.5f, Time.unscaledDeltaTime * 7.5f);
  }
}
