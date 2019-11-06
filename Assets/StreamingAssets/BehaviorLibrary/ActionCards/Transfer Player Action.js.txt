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
  propCardTargetActor("FromActor"),
  propActor("ToActor", "", {
    label: "To actor:"
  }),
  propBoolean("EvenIfTaken", false, {
    label: "Even if already taken"
  })
];

export function getCardErrorMessage() {
  if (!exists(props.ToActor)) {
    return "*** Must specify To Actor.";
  }
  if (!isPlayerControllable(props.ToActor)) {
    return "*** To Actor is not player controllable.";
  }
}

export function onAction(actionMessage) {
  const sourceActor = getCardTargetActor("FromActor", actionMessage);
  const errorMessage = getCardErrorMessage();
  if (errorMessage) {
    logError("Transfer Player card: " + errorMessage);
    return;
  }
  if (!exists(sourceActor)) {
    logError("Transfer Player card could not find a source actor.");
    return;
  }
  if (!isPlayerControllable(sourceActor)) {
    logError("Transfer Player card: source actor is not player controllable: " + getDisplayName(sourceActor));
    return;
  }
  const playerId = getControllingPlayer(sourceActor);
  if (!playerId) {
    // This is not an error because a playerless actor might happen to collide with something that
    // has this card and it's not an error.
    return;
  }
  if (getControllingPlayer(props.ToActor) && !props.EvenIfTaken) {
    log("Not transferring; target actor already taken.");
    return;
  }
  send(sourceActor, "AssignPlayer", { playerId: null });
  send(props.ToActor, "AssignPlayer", { playerId: playerId });
}

export function getCardStatus() {
  return {
    description: `Transfer player from <color=yellow>${getCardTargetActorDescription('FromActor')}</color> to <color=green>${getDisplayName(props.ToActor)}</color>.`
  }
}