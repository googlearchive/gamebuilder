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

// Player Controls: Hovercraft.

export const PROPS = [
  propDecimal("Accel", 8),
  propDecimal("MaxSpeed", 16),
  propDecimal("BrakeAccel", 32),
  propDecimal("TurnSpeed", 2),
  propDecimal("PitchOffset", 20),
  propDecimal("MaxPitch", 20),
];

export function onControl() {
  const COAST_ACC = 2;

  card.speed = card.speed || 0;

  enableGravity(false);
  enableKeepUpright(false);

  if (getAttrib("isDead", false)) {
    return;
  }
  const throttle = getThrottle();

  if (throttle.z > 0) {
    card.speed += deltaTime() * props.Accel;
  } else {
    const brakeAccel = throttle.z < -0.1 ? props.BrakeAccel : COAST_ACC;
    card.speed = card.speed > 0 ?
      Math.max(0, card.speed - brakeAccel * deltaTime()) :
      Math.min(0, card.speed + brakeAccel * deltaTime());
  }
  card.speed = Math.min(Math.max(card.speed, -props.MaxSpeed), props.MaxSpeed);

  const desiredDir = getAimDirection().clone();

  // Offset the pitch so that it's more comfortable to control.
  // We don't HAVE to do it, but users probably expect to look at a ship
  // from above and have it fly perfectly level.
  desiredDir.applyAxisAngle(getRight(), -degToRad(props.PitchOffset));

  lookTowardDir(desiredDir, props.TurnSpeed);
  moveForward(card.speed);
}

export function onResetGame() {
  delete card.speed;
}
