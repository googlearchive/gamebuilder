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

// DO NOT RENAME this file.

export const PROPS = [
  // DO NOT RENAME this property.
  propNumber('playerNumber', 1, {
    label: "Player#"
  }),
  propDeck('controlsDeck', 'Controls', {
    label: 'Movement'
  }),
  propDeck('primaryActionDeck', 'Action', {
    label: 'Left Click'
  }),
  propDeck('secondaryActionDeck', 'Action', {
    label: 'Right Click'
  }),
];

export function onInit() {
  card.gameEnded = false;
}

export function onTick() {
  if (card.gameEnded) {
    return;
  }
  callDeck(props.controlsDeck, 'Control');
  let descs = callDeck(props.primaryActionDeck, 'GetActionDescription');
  descs = callDeck(props.secondaryActionDeck, 'GetActionDescription');

  // Old versions did not have props.playerNumber so default to 1 in that case.
  const playerNumber = props.playerNumber === undefined ? 1 : props.playerNumber;
  const playerId = playerNumber > 0 ? getPlayerByNumber(playerNumber) : null;
  setControllingPlayer(playerId);
}

export function onPrimaryAction() {
  if (card.gameEnded) return;
  // Call with a short pulse interval because we want it to pulse every time we
  // get onPrimaryAction.
  callActionDeck("primaryActionDeck", undefined, undefined, 0.01);
}

export function onSecondaryAction() {
  if (card.gameEnded) return;
  callActionDeck("secondaryActionDeck");
}

export function onCardRemoved() {
  setControllingPlayer(null);
  setCameraActor(null);
}

export function onGameEnd(msg) {
  card.gameEnded = true;
}


// DEPRECATED:
export function onRequestSetCamera(msg) {
  if (exists(msg.cameraActor)) {
    setCameraActor(msg.cameraActor);
  }
}
