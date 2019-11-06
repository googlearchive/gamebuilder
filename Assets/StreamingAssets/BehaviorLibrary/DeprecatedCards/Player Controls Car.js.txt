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

// Player Controls: Car.

export const PROPS = [
  propDecimal("Accel", 8),
  propDecimal("MaxSpeed", 16),
  propDecimal("BrakeAccel", 32),
];

export function onControl() {
  const COAST_ACC = 2;
  const TURN_SENSIVITY = 0.15;

  card.speed = card.speed || 0;

  enableGravity(true);
  enableKeepUpright(true);

  if (getAttrib("isDead", false)) {
    return;
  }
  const throttle = getThrottle();

  if (throttle.z > 0) {
    card.speed += deltaTime() * props.Accel * (card.reverse ? -1 : 1);
  } else {
    const brakeAccel = throttle.z < -0.1 ? props.BrakeAccel : COAST_ACC;
    card.speed = card.speed > 0 ?
      Math.max(0, card.speed - brakeAccel * deltaTime()) :
      Math.min(0, card.speed + brakeAccel * deltaTime());
  }
  card.speed = Math.min(Math.max(card.speed, -props.MaxSpeed), props.MaxSpeed);
  moveGlobal(getForward(card.speed));

  turn(throttle.x * TURN_SENSIVITY * deltaTime() * card.speed);

  if (card.reverse) {
    uiRect(750, 300, 130, 40, UiColor.BLACK);
    uiText(760, 310, "REVERSE", UiColor.RED);
  }

  uiText(1200, 800, "[X]: Toggle REVERSE", UiColor.WHITE);
}

export function onResetGame() {
  delete card.speed;
  delete card.reverse;
}

export function onKeyDown(msg) {
  if (msg.keyName === "x") {
    card.reverse = !card.reverse;
  }
}
