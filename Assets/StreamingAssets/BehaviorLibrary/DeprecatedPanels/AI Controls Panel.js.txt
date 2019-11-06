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
  propDeck('idleDeck', 'Action', {
    label: "What do I do when I don't see the player?",
    deckOptions: {
      defaultCardURIs: ['builtin:MoveRandomWalk']
    }
  }),
  propDecimal("VisibleRange", 10),
  propDeck('seePlayerDeck', 'Action', {
    label: 'What do I do if I see the player?',
    deckOptions: {
      defaultCardURIs: ['builtin:MoveRandomWalk']
    }
  }),
]

export function onTick() {
  var isSeeing = false;
  for (let actor of getPlayerActors()) {
    const toTarget = getPos(actor).sub(getPos());
    const dist = toTarget.length();

    if (dist > props.VisibleRange) {
      continue;
    }

    toTarget.normalize();
    const forward = getForward();
    const degreesOff = radToDeg(forward.angleTo(toTarget));

    if (degreesOff > 15) {
      continue;
    }

    // TODO ray cast?
    callActionDeck("seePlayerDeck", { event: { actor: actor } });
    isSeeing = true;
    break;
  }
  if (!isSeeing) {
    callActionDeck("idleDeck");
  }
}
