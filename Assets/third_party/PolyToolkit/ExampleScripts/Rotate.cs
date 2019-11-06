// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

/// <summary>
/// Script that rotates an object at a constant speed.
/// </summary>
public class Rotate : MonoBehaviour {
  public Vector3 axis = Vector3.up;
  public float angularSpeed = 20.0f;
  private void Update() {
    gameObject.transform.Rotate(axis, angularSpeed * Time.deltaTime, Space.World);
  }
}