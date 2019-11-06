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

public class AlwaysFaceCamera : MonoBehaviour
{
  void Start()
  {
    UpdateRotation();
  }

  void Update()
  {
    UpdateRotation();
  }

  // Simple hack, cuz Camera.main is slow.
  // Call it no more than once per frame.
  static int lastFrameMainCameraUpdated = 0;
  static Camera mainCamera;

  private void UpdateRotation()
  {
    if (lastFrameMainCameraUpdated != Time.frameCount)
    {
      // Update it once for every instance out there.
      lastFrameMainCameraUpdated = Time.frameCount;
      mainCamera = Camera.main;
    }

    if (mainCamera != null)
    {
      Vector3 mainEuler = mainCamera.transform.rotation.eulerAngles;
      transform.rotation = Quaternion.Euler(-mainEuler.x, 180 + mainEuler.y, 0);
    }
  }
}
