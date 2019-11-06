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

// Move: Oscillate
// Oscillates along a direction

export const PROPS = [
  propDecimal("Distance", 3),
  propDecimal("Speed", 5),
  // this should be a floating point vec3 property
  propDecimal("DirectionX", 0),
  propDecimal("DirectionY", 0),
  propDecimal("DirectionZ", 1),
  propBoolean("UseGlobalCoordinates", false),
  propBoolean("AbsolutePosition", false)
];

// really want these mem values to be local to script
// (in case you have other oscillators on this actor)
export function onResetGame() {
  mem.oscValue = 0;
  mem.sineParam = 0;
}

export function onActiveTick() {
  // update clock for sine wave
  const freq = props.Speed * 0.1;
  mem.sineParam = (mem.sineParam || 0) + (2 * Math.PI * freq * deltaTime());
  if (mem.sineParam > Math.PI * 2) {
    mem.sineParam -= Math.PI * 2;
  }

  // get the new oscillator value
  const amp = props.Distance / 2;
  const oscValue = Math.sin(mem.sineParam) * amp;

  //find the direction of the oscillation
  let dir = vec3(props.DirectionX, props.DirectionY, props.DirectionZ);
  if (!props.UseGlobalCoordinates) {
    dir = getLocalVec3(dir);
  }

  //move relative or move around the spawn position
  if (props.AbsolutePosition) {
    dir.normalize().multiplyScalar(oscValue);
    setPos(dir.add(getSpawnPos()))
  } else {
    const oscDelta = oscValue - (mem.oscValue || 0);
    dir.normalize().multiplyScalar(oscDelta);
    setPos(vec3add(getPos(), dir));
  }

  mem.oscValue = oscValue;
}

// please move this functionality into API
function getLocalVec3(localVec) {
  let newVec = getRight(localVec.x);
  newVec.add(getUp(localVec.y));
  newVec.add(getForward(localVec.z));
  return newVec;
}