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
  propActor('target', '', {
    label: 'Target actor'
  }),
  propDeck('cameraDeck', 'Camera', {
    label: 'Camera:',
    deckOptions: {
      defaultCardURIs: ['builtin:Top Down Camera Card']
    }
  })
];

export function onInit() {
  // If card.overrideTarget is present, this overrides the target property.
  // Note that it may be null, and this still means "set the target to null".
  // If this is undefined, this means "use the property".
  delete card.overrideTarget;
}

function getCurrentTarget() {
  return card.overrideTarget === undefined ? props.target : card.overrideTarget;
}

export function onTick() {
  setSolid(false);
  setKinematic(true);

  const target = getCurrentTarget();

  // HACK: this redirects input from the target actor to ourselves.
  mem.hackObtainInputFrom = target || null;

  if (isPlayerControllable()) {
    logError("The Camera Panel can't be used on a player. The camera must be a separate actor.");
    return;
  } else if (target === myself()) {
    logError("Camera target can't be itself. Camera must be a separate actor that targets the player actor.");
    return;
  }

  if (props.cameraDeck.length > 0) {
    callDeck(props.cameraDeck, "CameraTick", { target: target });
  } else {
    // Revert to default camera properties.
    setCameraSettings({
      // The engine-provided default for this is false, but let's override it to true
      // because it's more friendly this way.
      cursorActive: true
    });
    detachFromParent();
  }

  if (exists(target) && getCameraActor(target) !== myself()) {
    setCameraActorPlease(target, myself());
    // Copy tint from target actor.
    setTintColor(getTintColor(target));
  }
}

export function onCardRemoved() {
  delete mem.isCameraActor;
}

// This message requests us to set a new target, overriding props.target.
// It's effectively until game reset.
//   msg.target: The new target. null is a valid value here, and means
//   assign null as target.
export function onAssignTarget(msg) {
  assert(msg.target !== undefined, "CameraPanel.onSetTarget: msg.target must be the target actor");
  onUnassignTarget();
  card.overrideTarget = msg.target;
}

// Requests us to forget our previous override target.
export function onUnassignTarget() {
  const oldTarget = getCurrentTarget();
  if (exists(oldTarget)) {
    setCameraActorPlease(oldTarget, null);
  }
  delete card.overrideTarget;
}