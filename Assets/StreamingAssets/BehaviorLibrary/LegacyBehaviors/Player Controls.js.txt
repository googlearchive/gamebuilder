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

// Player Controls<size=70%>\nBasic WASD movement

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Number Speed 8
// property Number SprintSpeed 12
// property Number JumpSpeed 15
// tag ability

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const me = api.getActor();

  if (me.getName() == "RedPlayer" && getControllingPlayer(me.getName()) == '') {
    setControllingPlayer(getPlayerByNumber(1));
  }

  if (api.isDead()) {
    api.getActor().setUseDesiredVelocity(false);
    return;
  }
  api.memory.jumpCooldown = api.memory.jumpCooldown ? Math.max(api.memory.jumpCooldown - api.dt, 0) : 0;

  // Enforce throttle.
  me.setUseDesiredVelocity(true);
  me.setIgnoreVerticalDesiredVelocity(true);

  const speed = me.getIsSprinting() ?
    valueOr(api.props.SprintSpeed, 12) :
    valueOr(api.props.Speed, 8);

  const velocity = me.getWorldSpaceThrottle();
  velocity.multiplyScalar(speed);
  me.setDesiredVelocity(velocity);
}

/**
 * @param {HandlerApi} api
 */
export function OnJumpTriggered(api) {
  if (api.isDead()) {
    return;
  }
  if (api.getActor().getIsGrounded() && api.memory.jumpCooldown == 0) {
    api.memory.jumpCooldown = 0.1;
    var jumpSpeed = valueOr(api.props.JumpSpeed, 15);
    kick(api.actor.name, new THREE.Vector3(0, jumpSpeed, 0));
  }
}
