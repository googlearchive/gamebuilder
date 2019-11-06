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
  propDeck("winEventDeck", "GameRulesEvent", {
    label: "When do you win the game?"
  }),
  propDeck("winDeck", "Action", {
    label: "What happens when you win?"
  }),
]

export function onTick() {
  if (card.ended) {
    return;
  }
  /** @type {GEvent?} */
  let winEvent = null;
  callDeck(props.winEventDeck, "Check").forEach(result => winEvent = winEvent || result);
  if (winEvent) {
    // Someone won the game. Call the win action deck.
    callActionDeck("winDeck", { event: winEvent })
    card.ended = true;
  }
}

export function onResetGame() {
  delete card.ended;
}
