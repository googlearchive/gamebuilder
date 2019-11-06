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

// A driver will provide inputs to biped assets, like telling it which direction
// the biped is trying to walk, when to jump, etc.
public interface IBipedDriver
{
  bool IsValid();

  // The Y component will always be 0.
  Vector3 GetMoveThrottle();

  Vector3 GetLookDirection();

  bool IsGrounded();

  event System.Action onJump;
}
