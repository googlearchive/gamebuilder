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

// Trampoline<size=70%>\nAnything that touches it will get launched in the sky!

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Number BounceSpeed 10
// tag physics

/**
 * @param {HandlerApi} api
 */
export function OnTouchEnter(api) {
  if (api.isDead()) {
    return;
  }

  const other = api.message.other;
  let ky = api.props.BounceSpeed;
  // Cancel out any falling velocity
  let currY = api.getOtherVelocity(other).y;
  if (currY < 0) {
    ky += Math.abs(currY);
  }
  kick(other, new THREE.Vector3(0, ky, 0));
  api.setCooldown(0.5);
}