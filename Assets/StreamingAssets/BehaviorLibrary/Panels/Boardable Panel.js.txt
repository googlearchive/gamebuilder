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
  propDeck('OnBoardedDeck', 'Action', {
    label: 'When player boards, do:'
  }),
  propDeck('WhileBoardedDeck', 'Action', {
    label: 'While player boarded, do:'
  }),
  propDeck('AlightedDeck', 'Action', {
    label: 'When player alights, do:'
  })
];

export function onInit() {
  resetState();
  setVar('boardable', true);
}

function resetState() {
  delete card.riderActor;
  delete card.riderPlayer;
  setControllingPlayer(null);
  setIsPlayerControllable(true);
}

export function onTick() {
  if (getVar('hasPlayerControlsPanel')) {
    logError('Boardable panel is incompatible with Player Controls panel. Please remove the Player Controls panel.')
    return;
  }
  if (card.riderActor) {
    callActionDeck('WhileBoardedDeck', { event: { actor: card.riderActor } });
  }
}

export function onRequestBoard(msg) {
  assert(msg.actor, 'RequestBoard must specify actor');
  const playerId = getControllingPlayer(msg.actor);
  if (!playerExists(playerId)) {
    logError('Only players can board a Boardable actor. Not a player: ' + getDisplayName(msg.actor));
    return;
  }
  if (card.riderActor) {
    // Already boarded by another actor.
    return;
  }
  send(msg.actor, 'BoardingAccepted', { boardable: myself() });
  card.riderActor = msg.actor;
  card.riderPlayer = playerId;
  setControllingPlayer(playerId);
  callActionDeck('OnBoardedDeck', { event: { actor: msg.actor } });
}

export function onRequestAlight() {
  const riderActor = card.riderActor;
  if (!exists(riderActor)) return;
  send(riderActor, 'AlightFromBoardable', { boardable: myself(), playerId: card.riderPlayer });
  resetState();
  moveForward(0);
  callActionDeck('AlightedDeck', { event: { actor: riderActor } });
}
