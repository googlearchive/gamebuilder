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

// Gate

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Boolean OpenDown false
// property Number OpenDistance 1
// property Number OpenSpeed 1
// property Boolean IsLocked false
// property Number LockNumber 0

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.memory.opening = false;
}

/**
 * @param {HandlerApi} api
 */
export function OnTouchEnter(api) {
  if (api.props.IsLocked) {
    return;
  }

  if (!api.memory.opening) {
    api.memory.opening = true;
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  if (api.memory.opening) {
    const p0 = api.spawnPosition;
    const p = api.position;
    if (p.distanceTo(p0) < api.props.OpenDistance) {
      const ySign = api.props.OpenDown ? -1 : 1;
      const yVel = ySign * api.props.OpenSpeed;
      p.y += api.dt * yVel;
      api.position = p;
    }
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnTryUnlock(api) {
  if (api.message.lockNumber == api.props.LockNumber) {
    api.memory.opening = true;
  }
}