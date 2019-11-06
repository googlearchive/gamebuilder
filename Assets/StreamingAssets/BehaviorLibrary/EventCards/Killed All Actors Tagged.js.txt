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
  propActorGroup("targets", "@TAG:enemy", {
    label: "Kill what actors:",
    pickerPrompt: "What actors must be killed?"
  }),
]

/** @return {GEvent|undefined} The event that fired, if any. */
export function onCheck() {
  if (card.triggeredEvent) {
    const e = card.triggeredEvent;
    delete card.triggeredEvent;
    return e;
  }
}

/** @param {GDeathMessage} deathMessage */
export function onDeath(deathMessage) {
  if (!isActorInGroup(deathMessage.actor, props.targets)) {
    return;
  }
  for (let actor of getActorsInGroup(props.targets)) {
    if (actor !== deathMessage.actor && isOnstage(actor)) {
      // Found at least one alive and onstage, so don't fire yet.
      return;
    }
  }
  // Do not pass an actor here, since that doesn't entirely make sense.
  /** @type {GEvent} */
  card.triggeredEvent = {};
}

export function onResetGame() {
  delete card.triggeredEvent;
}

export function getCardStatus() {
  return {
    description: `When the last of <color=yellow>${getActorGroupDescription(props.targets)}</color> is killed.`
  }
}