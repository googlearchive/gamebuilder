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

// Grabbable<size=70%>\nPlayers can grab and throw this object.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag interact

// HACK
// suggest-builtin-on-player Grabbing

// property Boolean FaceForward false

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  // TODO this should really be on start
  api.memory.grabbable = true;
}

/**
 * @param {HandlerApi} api
 */
export function OnTryToGrab(api) {
  // Could optionally check if grabbing allowed, etc.
  api.sendMessage(api.message.grabber, "Grabbed", { object: api.name });
  api.sendMessage(api.name, "WasGrabbed", { grabber: api.message.grabber });
}

/**
 * @param {OtherActor} grabber
 * @param {OtherActor} grabbed
 * @returns { THREE.Vector3}
 */
function getGrabbedPosition(grabbed, grabber) {
  const size = grabbed.getBoundsSize();
  const center = grabbed.getBoundsCenter();
  const radius = Math.sqrt(size.x * size.x + size.z * size.z) * 0.5;
  const finalOrigin = grabber.getBoundsCenter();
  const pushForward = 0.5;
  finalOrigin.add(grabber.getForward().multiplyScalar(radius + pushForward));
  finalOrigin.y += size.y * 0.25;
  const centerToOrigin = grabbed.getPosition().sub(center);
  finalOrigin.add(centerToOrigin);
  return finalOrigin;
}

/**
 * @param {HandlerApi} api
 */
export function OnWasGrabbed(api) {
  const me = api.getActor();
  const grabberName = api.message.grabber;
  me.setTransformParent(grabberName);
  api.memory.lastGrabber = grabberName;
  me.setIsSolid(false);

  const grabber = api.getOtherActor(grabberName);
  me.setPosition(getGrabbedPosition(me, grabber));

  if (api.props.FaceForward) {
    me.setRotation(grabber.getRotation());
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnDropped(api) {
  api.getActor().setTransformParent(null);
  api.getActor().setIsSolid(true);
}

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.sendSelfMessage("Respawned");
}

/**
 * @param {HandlerApi} api
 */
export function OnRespawned(api) {
  // By protocol, we will ask the current grabber to drop us.
  const lastGrabber = api.memory.lastGrabber;
  if (lastGrabber) {
    api.sendMessage(lastGrabber, 'DropGrabbed', { grabbed: api.name });
  }
  api.getActor().setIsSolid(true);
}
