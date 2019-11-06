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

// Controlling<size=70%>\nGives player ability to use 'Controllable' actors, such as cars and horses.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag ability

/**
 * @param {HandlerApi} api
 */
function dismount(api) {
  if (!api.memory.mounted) {
    return;
  }

  const droppedName = api.memory.mounted;
  delete api.memory.mounted;
  api.sendMessage(droppedName, "Dismounted", { mounter: api.name });
  return droppedName;
}

/**
 * @param {HandlerApi} api
 */
function canMountAimed(api) {
  const state = api.actor;

  return api.isValidActor(state.aimingAtName)
    && api.getOtherMemory(state.aimingAtName, 'mountable') == true
    && api.position.distanceTo(api.actor.lastAimHitPoint) < 3;
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const name = api.name;
  const state = api.actor;
  const memory = api.memory;

  if (api.isDead()) {
    state.enableAiming = false;
    return;
  }

  state.enableAiming = true;

  if (memory.mounted) {
    api.addPlayerToolTip(name, 'action2', 'Dismount');
  }
  else {
    if (canMountAimed(api)) {
      api.addPlayerToolTip(name, 'action2', 'Mount');
    }
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnAction2Triggered(api) {
  const name = api.name;
  const state = api.actor;
  const memory = api.memory;
  if (memory.mounted) {
    dismount(api);
  }
  else {
    if (canMountAimed(api)) {
      api.sendMessage(state.aimingAtName, 'MountRequest', { mounter: name });
    }
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnDismount(api) {
  dismount(api);
}

/**
 * @param {HandlerApi} api
 */
export function OnMountAllowed(api) {
  const me = api.getActor();
  const mountedId = api.message.mountedId;
  const other = api.getOtherActor(mountedId);
  api.memory.mounted = mountedId;

  // Move ourselves into it
  me.setPosition(other.getPosition());
  me.setRotation(other.getRotation());

  api.sendMessage(mountedId, 'WasMounted', { mounterId: api.name });
}

/**
 * @param {HandlerApi} api
 */
export function OnDied(api) {
  if (api.memory.mounted) {
    api.sendSelfMessage('Dismount');
  }
}