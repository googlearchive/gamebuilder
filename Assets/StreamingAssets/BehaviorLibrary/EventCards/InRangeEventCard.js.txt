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
  propActorGroup("Who", "@ANY", {
    label: "Who needs to be in range?"
  }),
  propDecimal("Range", 5)
];

export function onTick() {
  const target = getClosestActor(getActorsInGroup(props.Who, props.Range));
  if (target) {
    // Trigger!
    card.triggeredEvent = { actor: target };
  }
}

/**
 * @return {GEvent|undefined} The event, if one occurred.
 */
export function onCheck() {
  if (card.triggeredEvent !== undefined) {
    const rv = card.triggeredEvent;
    delete card.triggeredEvent;
    return rv;
  }
  else {
    return undefined;
  }
}

export function onResetGame() {
  delete card.triggeredEvent;
}

export function getCardStatus() {
  return {
    description: `When <color=yellow>${getActorGroupDescription(props.Who, true)}</color> is within distance <color=green>${props.Range.toFixed(1)}`
  }
}