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

// Grid Spawner<size=70%>\nOn reset, spawn clones of an actor in a grid.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag utility

// property Actor proto
// property Number rows
// property Number cols
// property Number spacing 1

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  const S = api.props.spacing;
  const origin = api.position;
  const spawnPos = new THREE.Vector3();
  for (var x = 0; x < api.props.cols; x++) {
    for (var y = 0; y < api.props.rows; y++) {
      spawnPos.set(x * S, 0, y * S);
      spawnPos.add(origin);
      api.clone(api.props.proto, spawnPos, ID_QUAT);
    }
  }
}