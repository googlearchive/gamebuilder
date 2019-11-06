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

// Move: Move in direction
// Moves in direction relative to the actor

export const PROPS = [
  propDecimal("Speed", 1),
  // this should be a floating point vec3 property
  propDecimal("DirectionX", 0),
  propDecimal("DirectionY", 0),
  propDecimal("DirectionZ", 1),
  propBoolean("UseGlobalCoordinates", false)
];

export function onActiveTick() {
  let vel = vec3scale(vec3(props.DirectionX, props.DirectionY, props.DirectionZ), props.Speed);
  if (props.UseGlobalCoordinates) {
    moveGlobal(vel);
  } else {
    move(vel);
  }
}

export function getCardStatus() {
  const dir = `(${props.DirectionX.toFixed(1)},${props.DirectionY.toFixed(1)},${props.DirectionZ.toFixed(1)})`;
  const coordSys = props.UseGlobalCoordinates ? 'world' : 'self';
  return {
    description: `Move in direction <color=yellow>${dir}</color> [${coordSys}] with speed <color=green>${props.Speed.toFixed(1)}.`
  }
}