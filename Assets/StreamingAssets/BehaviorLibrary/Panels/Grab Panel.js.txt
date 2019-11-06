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

export const PROPS = [
  propDecimal("HoldDistance", 2),
  propDecimal("HoldHeight", 1),
  propDecimal("MaxGrabDist", 3),
  propBoolean("MoveUpDown", false, {
    label: "Move held item up/down"
  })
]

function resetState() {
  delete card.grabbedItem;
  delete card.canThrow;
}

export function onTick() {
  // Cleanup: if the grabbed item got deleted, forget it:
  if (card.grabbedItem && !exists(card.grabbedItem)) {
    resetState();
  }
  // Publish the name of the currently grabbed item for the benefit
  // of other actors who may want to check this.
  setVar("grabbedItem", card.grabbedItem || "");
}

export function onRequestGrabOrRelease() {
  if (card.grabbedItem) {
    // I am currently holding an item, drop it.
    send(card.grabbedItem, "GrabRelease");
    // Give it a push, if it's throwable.
    if (card.canThrow) {
      const kickDir = getForward();
      kickDir.y += 0.2; // For a bit of an arc
      kickDir.normalize();
      kickDir.multiplyScalar(15);
      push(card.grabbedItem, kickDir);
    }
    resetState();
    return;
  }
  const grabTarget = getGrabTarget();
  if (grabTarget) {
    // Attempting to grab the item can succeed or fail depending
    // on the item's state (for example, it might already have been
    // grabbed by some other player). So we can't do anything
    // until we get a GrabResponse reply from the item.
    const anchorOffset = vec3(0, props.HoldHeight, props.HoldDistance);
    send(grabTarget, "GrabRequest", { grabber: myself(), anchorOffset: anchorOffset });
  }
}

/* @type {GGrabResponseMessage} msg */
export function onGrabResponse(msg) {
  if (!msg.accepted) return;
  card.grabbedItem = msg.item;
  card.canThrow = !!msg.canThrow;
}

export function onResetGame() {
  // We don't need to notify the grabbed item because it will get
  // onResetGame too, and can will do the cleanup independently.
  resetState();
}

// TODO: display this action description on screen on onLocalTick:
//export function onGetActionDescription() {
//  if (card.grabbedItem) {
//    return (card.canThrow ? "Throw " : "Drop ") + getDisplayName(card.grabbedItem);
//  }
//  const grabTarget = getGrabTarget();
//  if (grabTarget) {
//   return "Grab " + getDisplayName(grabTarget);
//  }
//  return "";
//}

// Figures out what is the target of the grab.
function getGrabTarget() {
  if (card.grabbedItem) {
    // Already grabbing something, so can't grab anything else.
    return null;
  }
  const aimTarget = getAimTarget();
  // High priority target: if we have an actual aim target,
  // use it. It's the thing the player is really aiming at.
  if (aimTarget) {
    return getAttrib("grabbable", aimTarget) ? aimTarget : null;
  }
  // Fallback: find objects near the player, to catch objects that are
  // close enough to be picked up but not in a direct line of sight
  // (like a small object that's on the ground). This is necessary for
  // the user to be able to pick up small objects in Isometric view, as
  // the aim is always level with the ground plane and there is no way
  // to aim "down".
  const actorsNearby = overlapSphere(getPos(), props.MaxGrabDist);
  // Check if any of them are grabbable and are "forward" of us.
  const fwd = getForward();
  let bestCandidate = null;
  let bestAngle = 0;
  for (let actor of actorsNearby) {
    if (actor === myself()) continue;
    // Grabbable?
    if (!getAttrib("grabbable", actor)) continue;
    // Reasonably positioned "forward" from us?
    let p = getPos(actor);
    p.sub(getPos());
    const angle = p.angleTo(fwd);
    // If angle with forward is > 45, ignore it.
    if (angle > degToRad(45)) continue;
    // If we got here, it's a reasonable candidate.
    // If it's better-positioned than our current best candidate,
    // replace our best candidate.
    if (!bestCandidate || angle < bestAngle) {
      bestCandidate = actor;
      bestAngle = angle;
    }
  }
  return bestCandidate;
}

