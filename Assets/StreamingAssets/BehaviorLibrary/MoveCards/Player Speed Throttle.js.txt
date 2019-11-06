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
  propDecimal("Accel", 14, { label: "Accelerate speed" }),
  propDecimal("MaxSpeed", 16, { label: "Top speed" }),
  propDecimal("Slowdown", 5, { label: "Slow down (friction)" }),
  propDecimal("StopTime", .2, { label: "Time stopped (before reverse)" })
];

export function onActiveTick() {
  if (getAttrib("isDead", false) || getAttrib("ControlsLocked", false)) {
    moveGlobal(vec3zero());
    return;
  }

  card.speed = card.speed || 0;
  card.transitionTimer = card.transitionTimer || 0;

  const throttle = getThrottle().z;

  let decel = 0;
  if (card.speed > 0) decel = -props.Slowdown;
  else if (card.speed < 0) decel = props.Slowdown;

  const totalAccel = (throttle * props.Accel + decel) * deltaTime();

  if (card.goingForward) {
    card.speed = Math.max(0, Math.min(props.MaxSpeed, card.speed + totalAccel));
  } else {
    card.speed = Math.min(0, Math.max(-props.MaxSpeed, card.speed + totalAccel));
  }

  if (card.speed == 0 && throttle != 0) changeDirectionCheck();

  if (Math.abs(card.speed) < 0.1) {
    moveGlobal(vec3zero());
  } else {
    moveGlobal(getForward(card.speed));
  }

}

function changeDirectionCheck() {
  const throttle = getThrottle().z;

  const wantTransition =
    (throttle > 0 && !card.goingForward) ||
    (throttle < 0 && card.goingForward);

  if (wantTransition) {
    card.transitionTimer += deltaTime();
    if (card.transitionTimer >= props.StopTime) {
      card.transitionTimer = 0;
      card.goingForward = !card.goingForward;
    }
  } else {
    card.transitionTimer = 0;
  }
}

export function onResetGame() {
  delete card.speed;
}

