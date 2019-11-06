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
  propDecimal('Amplitude', 0.2),
  propDecimal('Duration', 0.1),
];

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  if (!isPlayerControllable()) {
    logError("Camera Shake only works on a player actor.");
    return;
  }
  card.shake = {
    actor: myself(),
    startTime: getTime()
  };
}

export function getCardErrorMessage() {
  if (!isPlayerControllable()) {
    return "*** Card only works on a player actor!";
  }
}

export function onTick() {
  if (!card.shake) return;
  const t = getTime() - card.shake.startTime;
  if (t > props.Duration) {
    stopShaking();
    return;
  }
  // Decay factor starts at 1 and linearly decreases to 0 as the effect progresses.
  const decay = interp(0, 1, props.Duration, 0, t);

  const randomX = -0.5 + Math.random() * 2;
  const randomY = -0.5 + Math.random() * 2;

  // Request that offset.
  requestCameraOffset(vec3(randomX * props.Amplitude * decay,
    randomY * props.Amplitude * decay, 0), card.shake.actor);
}

function stopShaking() {
  delete card.shake;
}

export function onResetGame() {
  stopShaking();
}
