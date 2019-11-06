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

// Controllable<size=70%>\nPlayers can control this object, like a car or a horse.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag interact

function isMounted(api) {
  return api.isValidActor(api.actor.transformParent);
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  // TODO this should really be on start, or not stateful at all..?
  // Ie. maybe we should have a function that's like, "get supported tags"
  api.memory.mountable = true;

  if (isMounted(api)) {
    // Try to keep position..this gets out of sync in multiplayer sometimes due to physics stuff.
    const mounter = api.getOtherActor(api.actor.transformParent);
    api.getActor().setPosition(mounter.getPosition());
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnMountRequest(api) {
  if (isMounted(api)) {
    api.sendMessage(api.message.mounter, "MountDenied", { mountableId: api.name });
  }
  else {
    // Could optionally check if mounting allowed, etc.
    api.getActor().setIsSolid(false);
    api.sendMessage(api.message.mounter, "MountAllowed", { mountedId: api.name });
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnWasMounted(api) {
  const mounterId = api.message.mounterId;
  api.actor.transformParent = mounterId;
  api.memory.lastMounter = mounterId;
}

/**
 * @param {HandlerApi} api
 */
export function OnDismounted(api) {
  api.getActor().setIsSolid(true);
  api.actor.transformParent = null;
}

/**
 * @param {HandlerApi} api
 */
export function OnRespawned(api) {
  // By protocol, we will ask the current grabber to drop us.
  const lastMounter = api.memory.lastMounter;
  if (lastMounter) {
    api.sendMessage(lastMounter, 'Dismount', { mounted: api.name });
  }
}
