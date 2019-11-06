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
  propCardTargetActor("CardTargetActor", {
    label: "Push who?"
  }),
  propDecimal("VelX", 0),
  propDecimal("VelY", 20),
  propDecimal("VelZ", 0),
  propEnum("RelativeTo", "WORLD", ["WORLD", "MYSELF", "TARGET"])
];

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("CardTargetActor", actionMessage);
  if (!target) {
    return;
  }
  let vel = vec3(props.VelX, props.VelY, props.VelZ);
  vel = props.RelativeTo === "MYSELF" ? selfToWorldDir(vel) :
    props.RelativeTo === "TARGET" ? selfToWorldDir(vel, target) : vel;
  push(target, vel);
}

export function getCardStatus() {
  return {
    description: `Push <color=yellow>${getCardTargetActorDescription('CardTargetActor')}</color> with velocity <color=green>${vec3toString(vec3(props.VelX, props.VelY, props.VelZ), 1)}</color> relative to <color=orange>${props.RelativeTo.toLowerCase()}`
  }
}