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

// Shoot another object<size=70%>\nShoot clones of another object, like a baseball.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag ability

// property Actor ObjectToThrow
// property Boolean AutoFire true
// property Number CooldownTicks 5

// TODO cooldown, once we support floats

// HACKS
// suggest-builtin-on-actor-property ObjectToThrow Physics
// suggest-builtin-on-actor-property ObjectToThrow Self-destruct

function getCooldownSecs(api) {
  if (api.props.CooldownTicks === undefined) {
    return 0.5;
  }
  else {
    return api.props.CooldownTicks * 0.1;
  }
}

/**
 * @param {HandlerApi} api
 */
function getShotDirection(api, outVec) {
  if (api.isValidActor(api.actor.aimingAtName)) {
    // The reticle is over something. Throw it at the reticle-over point.
    outVec.copy(api.actor.lastAimHitPoint).sub(api.actor.position).normalize();
  }
  else {
    // Throw it in the aim direction. Like up in the air.
    if (api.getActor().getIsPlayerControllable()) {
      outVec.copy(api.aimDirection);
    }
    else {
      // For NPCs, just throw it forward.
      api.getActor().getForward(outVec);
    }
  }
}

/**
 * @param {HandlerApi} api
 */
function doThrow(api) {
  if (!api.isValidActor(api.props.ObjectToThrow)) {
    return;
  }
  const spawnPos = api.actor.position.clone();
  const spawnDir = new THREE.Vector3();
  getShotDirection(api, spawnDir);

  spawnPos.addScaledVector(spawnDir, 1.5);
  spawnPos.y += 0.5;

  const thrownName = api.clone(api.props.ObjectToThrow, spawnPos, api.rotation);

  const velocityChange = spawnDir.clone();
  velocityChange.normalize();
  velocityChange.multiplyScalar(20);
  kick(thrownName, velocityChange);
}

/**
 * @param {HandlerApi} api
 */
export function playerCanThrow(api) {
  return api.isValidActor(api.props.ObjectToThrow) && !api.isValidActor(api.memory.grabbed);
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const me = api.getActor();

  if (!api.isValidActor(api.props.ObjectToThrow)) {
    if (me.getIsPlayerControllable()) {
      api.addPlayerToolTip(api.name, 'action1', `(No ObjectToThrow set)`);
    }
    return;
  }

  if (api.props.ObjectToThrow == api.name) {
    if (me.getIsPlayerControllable()) {
      api.addPlayerToolTip(api.name, 'action1', `(Can't throw yourself)`);
    }
    return;
  }

  if (playerCanThrow(api)) {
    if (me.getIsPlayerControllable()) {
      api.addPlayerToolTip(api.name, 'action1', `Throw`);
    }
    else {
      // Auto-throw for NPCs
      const autoFire = valueOr(api.props.AutoFire, true);
      if (autoFire) {
        doThrow(api);
        // Limit how fast we fire.
        api.setCooldown(getCooldownSecs(api));
      }
    }
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnAction1Triggered(api) {
  if (playerCanThrow(api)) {
    doThrow(api);
    // Limit how fast we fire.
    api.setCooldown(getCooldownSecs(api));
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnTriggerUse(api) {
  if (playerCanThrow(api)) {
    doThrow(api);
    api.setCooldown(getCooldownSecs(api));
  }
}
