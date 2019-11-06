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
  propActor("ActorToClone", "", {
    label: "Actor to Clone",
    pickerPrompt: "Clone what actor?",
    allowOffstageActors: true
  }),
  propDecimal("Interval", 3, {
    label: "Interval (sec)"
  }),
  propNumber("Total", 10, {
    label: "Total #"
  })
];

export function onAction() {
  if (card.state) {
    // Just add more copies to spawn.
    card.state.clonesLeft += props.Total;
    return;
  }
  card.state = {
    clonesLeft: +props.Total,
    nextCloneTime: getTime(),
  };
}

export function onTick() {
  if (!exists(props.ActorToClone)) return;
  if (!card.state) return;
  if (getTime() > card.state.nextCloneTime) {
    card.state.nextCloneTime = getTime() + props.Interval;
    clone(props.ActorToClone, getPos(), getRot());
    if (--card.state.clonesLeft <= 0) {
      delete card.state;
    }
  }
}

export function onResetGame() {
  delete card.state;
}

export function getCardErrorMessage() {
  if (!exists(props.ActorToClone)) {
    return "NEED ACTOR TO CLONE. Click card to fix.";
  }
}

export function getCardStatus() {
  return {
    description: `Spawn <color=green>${props.Total}</color> copies of <color=white>${getDisplayName(props.ActorToClone)}</color> with interval of <color=yellow>${props.Interval.toFixed(1)}s</color> between spawns`
  }
}