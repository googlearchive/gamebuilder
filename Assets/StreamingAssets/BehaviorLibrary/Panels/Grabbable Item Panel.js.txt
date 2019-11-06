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
  propBoolean("CustomRotation", false),
  propDecimal("RotationX", 0, { requires: [requireTrue("CustomRotation")] }),
  propDecimal("RotationY", 0, { requires: [requireTrue("CustomRotation")] }),
  propDecimal("RotationZ", 0, { requires: [requireTrue("CustomRotation")] }),

  propBoolean("CustomOffset", false),
  propDecimal("OffsetX", 0, { requires: [requireTrue("CustomOffset")] }),
  propDecimal("OffsetY", 0, { requires: [requireTrue("CustomOffset")] }),
  propDecimal("OffsetZ", 0, { requires: [requireTrue("CustomOffset")] }),

  propBoolean("CanThrow", false, {
    label: "Can throw?"
  }),
  propBoolean("CanUse", false, {
    label: "Can use?"
  }),
  propDeck("UseDeck", "Action", {
    label: "When used, do:",
    requires: [
      requireTrue("CanUse")
    ]
  })
]

export function onGrabRequest(msg) {
  if (card.grabbedState) {
    // Already grabbed by someone.
    send(msg.grabber, "GrabResponse",
      { accepted: false, item: myself() });
    return;
  }
  // Someone is requesting to grab us, so we need to set it as our
  // transform parent.
  assert(exists(msg.grabber), "Grab requester does not exist");
  assertVector3(msg.anchorOffset, "GrabRequest must contain anchorOffset");

  // Remember our settings for restoring later.
  card.grabbedState = {
    solid: isSolid(),
    kinematic: isKinematic(),
    grabber: msg.grabber,
    anchorOffset: msg.anchorOffset
  };
  updatePosition();
  // Make ourselves non-solid and kinematic until we're released.
  setSolid(false);
  setKinematic(true);
  attachToParent(msg.grabber);
  send(msg.grabber, "GrabResponse", { accepted: true, item: myself(), canThrow: !!props.CanThrow });
  // Publish the name of the actor who grabbed this item as an attribute in case
  // other cards want to do something with it.
  setVar("owner", msg.grabber);
}

function updatePosition() {
  if (!card.grabbedState) return;

  const grabberPos = getPos(card.grabbedState.grabber);
  const grabberCenter = getBoundsCenter(card.grabbedState.grabber);
  const grabberForward = getForward(1, card.grabbedState.grabber);
  let grabberAim;

  let forwardToAim;
  if (isPlayerControllable(card.grabbedState.grabber)) {
    grabberAim = getAimDirection(card.grabbedState.grabber);
    forwardToAim = new Quaternion(0, 0, 0, 1);
    forwardToAim.setFromUnitVectors(
      getForward(1, card.grabbedState.grabber),
      grabberAim);
  } else {
    grabberAim = grabberForward;
  }

  // pos is relative to the grabber (for now)
  let pos = card.grabbedState.anchorOffset.clone();
  if (props.CustomOffset) {
    pos.x += props.OffsetX;
    pos.y += props.OffsetY;
    pos.z += props.OffsetZ;
  }
  // Convert pos to world position.
  pos = selfToWorldPos(pos, card.grabbedState.grabber);
  // pos = vector from owner bounds center to the position, in world space.
  pos.sub(grabberCenter);
  // Apply the quaternion to pos to rotate it in the same way that forward
  // is rotated to match aim.
  if (forwardToAim) pos.applyQuaternion(forwardToAim);
  // Now rebase pos back to the owners position.
  pos.add(grabberCenter);
  setPos(pos);

  // Set my rotation to be the same as the grabber's rotation so we're aligned.
  lookDir(grabberAim, false);

  // Apply rotation offset, if any.
  if (props.CustomRotation) {
    // turn() turns about a local axis, not a world axis.
    turn(degToRad(props.RotationY), vec3y());
    turn(degToRad(props.RotationX), vec3x());
    turn(degToRad(props.RotationZ), vec3z());
  }
}

export function onGrabRelease() {
  maybeRestore(false);
}

export function onResetGame() {
  maybeRestore(true);
}

export function onTick() {
  setVar('grabbable', true);
  updatePosition();
}

// Sent by the Use Grabbed Item card.
export function onRequestUseItem(msg) {
  // msg.actor is the user of the item (the actor holding it).
  // So the "target" of the use is whatever the user of the item is aiming at.
  const target = getAimTarget(msg.actor);
  // Use a short pulse interval because we want to pulse the action every time we get
  // a request.
  callActionDeck("UseDeck", { event: target ? { actor: target } : {} }, undefined, 0.01);
}

function maybeRestore(isResetGame) {
  if (!isResetGame) {
    detachFromParent();
  }
  if (card.grabbedState) {
    setSolid(card.grabbedState.solid);
    setKinematic(card.grabbedState.kinematic);
    setVar("owner", null);
    delete card.grabbedState;
  }
}

