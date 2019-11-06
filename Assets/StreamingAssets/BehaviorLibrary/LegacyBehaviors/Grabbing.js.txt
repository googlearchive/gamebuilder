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

// Grabbing<size=70%>\nGives player ability to grab 'Grabbable' actors, such as rocks and tools.

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
function dropGrabbed(api) {
  if (!api.memory.grabbed) {
    return;
  }

  const droppedName = api.memory.grabbed;
  delete api.memory.grabbed;
  api.sendMessage(droppedName, "Dropped", { thrower: api.name });
  return droppedName;
}

/**
 * @param {HandlerApi} api 
 */
function canGrabAimedAt(api) {
  return api.isValidActor(api.getActor().getAimingAtName())
    && api.getOtherMemory(api.getActor().getAimingAtName(), 'grabbable') == true
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

  if (memory.grabbed) {
    const grabbed = api.getOtherActor(api.memory.grabbed);
    if (grabbed.hasTag('usable')) {
      api.addPlayerToolTip(name, 'action1', `Use`);
    }
    else {
      api.addPlayerToolTip(name, 'action1', `Throw`);
    }
    api.addPlayerToolTip(name, 'action2', 'Drop');
  }
  else {
    // Empty handed.
    if (canGrabAimedAt(api)) {
      api.addPlayerToolTip(name, 'action2', 'Grab');
    }
  }
}

/**
 * @param {HandlerApi} api 
 */
export function OnAction1Triggered(api) {
  if (api.memory.grabbed) {
    const grabbed = api.getOtherActor(api.memory.grabbed);

    if (grabbed.hasTag('usable')) {
      api.sendMessage(grabbed.getName(), 'TriggerUse');
    }
    else {
      const droppedName = dropGrabbed(api);
      const kickDir = api.move.getForward();
      kickDir.y += 0.2; // For a bit of an arc
      kickDir.normalize();
      kickDir.multiplyScalar(15);
      kick(droppedName, kickDir);
    }
  }
}

/**
 * @param {HandlerApi} api 
 */
export function OnAction2Triggered(api) {
  if (api.memory.grabbed) {
    dropGrabbed(api);
  }
  else if (canGrabAimedAt(api)) {
    api.sendMessage(api.getActor().getAimingAtName(), 'TryToGrab', { grabber: api.name });
  }
}

/**
 * @param {HandlerApi} api 
 */
export function OnDropGrabbed(api) {
  if (api.memory.grabbed) {
    dropGrabbed(api);
  }
}

/**
 * @param {HandlerApi} api 
 */
export function OnGrabbed(api) {
  api.memory.grabbed = api.message.object;
}

/**
 * @param {HandlerApi} api 
 */
export function OnDied(api) {
  if (api.memory.grabbed) {
    api.sendSelfMessage('DropGrabbed');
  }
}