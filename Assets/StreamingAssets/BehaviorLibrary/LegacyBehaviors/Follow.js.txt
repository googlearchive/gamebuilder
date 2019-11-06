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

// Follow another actor

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Actor ObjectToFollow
// property Number FollowDistance 3
// property Number AwarenessDistance 999
// property Boolean CanFly false

// Ideally this would be a float. Meters per second.
// property Number Speed 2

// tag movement

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const moveSpeed = api.props.Speed || 1;

  const target = api.props.ObjectToFollow;
  if (api.isDead() || !api.doesActorExist(target)) {
    api.actor.useDesiredVelocity = false;
    return;
  }

  const myPos = api.position;
  const toTarget = api.getOtherPosition(target).sub(myPos);
  // Only follow horizontally
  toTarget.y = 0;

  const dist = toTarget.length();

  if (dist < (api.props.AwarenessDistance || 999) && dist > (api.props.FollowDistance || 0)) {
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
  else {
    api.actor.useDesiredVelocity = false;
    api.actor.desiredVelocity.set(0, 0, 0);
  }
}
