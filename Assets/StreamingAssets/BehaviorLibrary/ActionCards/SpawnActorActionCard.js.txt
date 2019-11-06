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
  propActor('ActorToClone', '', {
    label: "Actor to Clone",
    pickerPrompt: "Clone what actor?",
    allowOffstageActors: true
  }),
  propDecimal('XOffset', 0),
  propDecimal('YOffset', 1),
  propDecimal('ZOffset', 0),
  propBoolean('RandomizePosition', false),
  propDecimal('RandomDistMin', 5, { requires: [requireTrue("RandomizePosition")] }),
  propDecimal('RandomDistMax', 10, { requires: [requireTrue("RandomizePosition")] })
];

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  if (!exists(props.ActorToClone)) {
    logError(`No ActorToClone set!`);
    return;
  }
  const offset = vec3(props.XOffset, props.YOffset, props.ZOffset);
  const p = getPos();
  p.add(offset);

  if (props.RandomizePosition) {
    let angle = randBetween(0, 6.29);
    const dist = randBetween(props.RandomDistMin, props.RandomDistMax);

    // Try to vary the angle a bit (best effort).
    if (temp.lastAngle) {
      let attempts = 10;
      while (Math.abs(temp.lastAngle - angle) < 1 && --attempts > 0) {
        angle = randBetween(0, 6.29);
      }
      temp.lastAngle = angle;
    }

    p.x += dist * Math.sin(angle);
    p.z += dist * Math.cos(angle);
  }

  clone(props.ActorToClone, p, getRot());
  cooldown(0.1);
}

export function getCardErrorMessage() {
  if (!exists(props.ActorToClone)) {
    return "NEED ACTOR TO CLONE. Click card to fix.";
  }
}

export function getCardStatus() {
  return {
    description: `Spawn actor <color=white>${getDisplayName(props.ActorToClone)}</color>`
  }
}