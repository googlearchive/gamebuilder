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
  propDecimal("MaxDist", 5, {
    label: "Max dist to boardable"
  }),
  propBoolean("MustBeAiming", true, {
    label: "Must be facing boardable"
  })
];

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  // Look for something boardable.
  const boardable = findOutWhatToBoard();
  if (boardable) {
    // Ok, request boarding.
    send(boardable, 'RequestBoard', { actor: myself() });
  }
}

function findOutWhatToBoard() {
  // props.MustBeAiming is new so old files don't have it (defaults to true):
  const mustBeAiming = props.MustBeAiming === undefined ? true : props.MustBeAiming;
  if (mustBeAiming) {
    const target = getAimTarget();
    return canBoard(target) ? target : null;
  } else {
    const actorsInRange = overlapSphere(getPos(), props.MaxDist);
    let closest = null;
    for (const actor of actorsInRange) {
      if (!canBoard(actor)) continue;
      closest = (!closest || getDistanceTo(actor) < getDistanceTo(closest)) ? actor : closest;
    }
    return closest;
  }
}

function canBoard(actor) {
  // props.MaxDist is the separation distance; This converts it to distance from center to center:
  const maxDist = getBoundsRadiusOuter(actor) + getBoundsRadiusOuter() + props.MaxDist;
  // Actor needs to exist, needs to be boardable and needs to be close enough.
  return exists(actor) && getVar('boardable', actor) && getDistanceTo(actor) <= maxDist;
}

export function onBoardingAccepted(msg) {
  const boardable = msg.boardable;
  assert(exists(boardable), 'onBoardingAccepted: actor does not exist?');
  // While we're riding the vehicle, hide ourselves and remain attached to it.
  attachToParent(boardable);
  hide();
  setControllingPlayer(null);
  // Send message (to Player Controls panel) asking to assign no player.
  send(myself(), 'AssignPlayer', { playerId: null });
  card.isBoarded = true;
}

export function onAlightFromBoardable(msg) {
  const boardable = msg.boardable;
  const playerId = msg.playerId;
  assert(exists(boardable), 'onAlightFromBoardable: boardable does not exist?');
  assert(playerId, 'onAlightFromBoardable: playerId not specified');
  detachFromParent();
  const dist = getBoundsRadiusOuter(boardable) + getBoundsRadiusOuter();
  // Unboard to the left of the vehicle. Somewhat arbitrary?
  setPos(getPointToLeftOf(dist, boardable));
  show();
  // Return to previous player controls scheme.
  send(myself(), 'UnassignPlayer');
}

export function onResetGame() {
  if (card.isBoarded) {
    delete card.isBoarded;
    detachFromParent();
    show();
  }
}