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

// Isometric Auto-Run Controls<size=70%>\nBasic WASD movement

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Number Speed 5
// tag ability

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  // This is how we'll move the player.
  api.actor.useDesiredVelocity = true;

  // But don't worry about vertical velocity - leave that to gravity, jumping, etc.
  api.actor.ignoreVerticalDesiredVelocity = true;

  // This reflects the WASD keys. Holding A/D will set throttle.x to -1/1, and
  // W/S will set throttle.z to 1/-1.
  const throttle = api.inputThrottle;

  const speed = api.props.Speed;

  if (Math.abs(throttle.x) > 0) {
    api.actor.desiredVelocity.set(Math.sign(throttle.x) * speed, 0, 0);
  }
  else if (Math.abs(throttle.z) > 0) {
    api.actor.desiredVelocity.set(0, 0, Math.sign(throttle.z) * speed);
  }
  else {
    // Leave whatever desired velocity was previously there - thus "auto" run.
    return;
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.actor.desiredVelocity.set(0, 0, 0);
}
