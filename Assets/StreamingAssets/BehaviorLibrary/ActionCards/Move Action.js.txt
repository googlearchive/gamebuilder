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
  propDecimal("DistForward", 5),
  propDecimal("DistUp", 0),
  propDecimal("DistRight", 0),
  propDecimal("Speed", 5)
]

/** @type {GActionMessage} actionMessage */
export function onAction(actionMessage) {
  if (props.Speed <= 0) return;
  card.targetPos = selfToWorldPos(vec3(props.DistRight, props.DistUp, props.DistForward));
  card.endTime = getTime() + 1 + getDistanceTo(card.targetPos) / props.Speed;
}

export function onTick() {
  if (!card.targetPos) return;
  // Move towards target point, at given speed.
  moveToward(card.targetPos, props.Speed);
  // Stop trying to move after enough time has elapsed.
  if (getTime() > card.endTime) {
    resetCard();
  }
}

function resetCard() {
  delete card.targetPos;
  delete card.endTime;
}

export function onResetGame() {
  resetCard();
}

export function getCardStatus() {
  temp.dirList = temp.dirList || [];
  temp.dirList.length = 0;
  getDirDescription(props.DistForward, "forward", "back", temp.dirList);
  getDirDescription(props.DistUp, "up", "down", temp.dirList);
  getDirDescription(props.DistRight, "right", "left", temp.dirList);
  return {
    description: `Moves (<color=green>${temp.dirList.join(', ')}</color>) with speed <color=yellow>${props.Speed.toFixed(1)}`
  }
}

function getDirDescription(dist, positiveWord, negativeWord, list) {
  if (dist > 0.01) {
    list.push(dist.toFixed(1) + " " + positiveWord);
  } else if (dist < -0.01) {
    list.push((-dist).toFixed(1) + " " + negativeWord);
  }
}
