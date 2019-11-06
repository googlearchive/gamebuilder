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
  propDecimal("Degrees", 90),
  propBoolean("Counterclockwise", false),
  propDecimal("Speed", 90),
  propEnum("Axis", "Y", ["X", "Y", "Z"]),
];

export function onResetGame() {
  delete card.degreesLeft;
}

export function onAction() {
  card.degreesLeft = props.Degrees;
}

export function onTick() {
  if (card.degreesLeft) {
    const degreesToTurn = min(card.degreesLeft, props.Speed * deltaTime());
    card.degreesLeft -= degreesToTurn;
    turn((props.Counterclockwise ? -1 : 1) * degToRad(degreesToTurn),
      props.Axis === "X" ? vec3x() : props.Axis === "Z" ? vec3z() : vec3y());
    if (card.degreesLeft < 0.01) {
      delete card.degreesLeft;
    }
  }
}

export function getCardStatus() {
  return {
    description: `Turn <color=yellow>${props.Degrees} deg ${props.Counterclockwise ? 'counter' : ''}clockwise</color> ` +
      `at speed <color=green>${props.Speed.toFixed(1)}</color> about the <color=orange>${props.Axis} axis</color>`
  }
}
