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

// Follow nearest with tag

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property String Tag
// property Number MinDistance 0
// property Number SearchRadius 50
// property Number Speed 2

// tag movement

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const moveSpeed = api.props.Speed || 1;
  const me = api.getActor();

  // Default to not trying to move.
  api.actor.useDesiredVelocity = false;

  if (api.isDead()) {
    return;
  }

  let bestDist = -1;
  let bestTarget = null;
  api.overlapSphere(me.getPosition(), api.props.SearchRadius || 50).forEach(targetName => {
    if (!api.isValidActor(targetName)) {
      return;
    }
    const target = api.getOtherActor(targetName);
    if (target.hasTag(api.props.Tag)) {
      const currDist = api.distanceBetween(me.getName(), targetName);
      if (bestTarget == null || currDist < bestDist) {
        bestDist = currDist;
        bestTarget = target;
      }
    }
  });

  if (bestTarget == null) {
    return;
  }

  const target = bestTarget;
  const myPos = me.getPosition();

  if (bestDist < api.props.MinDistance) {
    // Close enough.
    return;
  }

  const toTarget = target.getPosition().sub(myPos);
  toTarget.y = 0;  // Only follow horizontally
  toTarget.normalize();
  toTarget.multiplyScalar(moveSpeed);
  const velocity = toTarget;
  if (!api.actor.enablePhysics) {
    api.move.move(velocity, api.dt);
  }
  else {
    api.actor.useDesiredVelocity = true;
    api.actor.desiredVelocity.copy(velocity);
    if (!api.props.CanFly) {
      api.actor.ignoreVerticalDesiredVelocity = true;
    }
  }
}