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
  propCardTargetActor("Target", {
    label: "Damage who?"
  }),
  propNumber('Amount', 1)
]

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("Target", actionMessage);
  if (target) {
    send(target, "Damage", { causer: myself(), amount: props.Amount });
  }
}

export function getCardStatus() {
  return {
    description: `Causes <color=yellow>${props.Amount}</color> points of damage to <color=green>${getCardTargetActorDescription('Target')}`
  }
}