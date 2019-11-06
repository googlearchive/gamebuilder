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
  propDecimal("Speed", 3),
  propDecimal("RestTime", 1)
];

export function onResetGame() {
  delete card.startPos;
  delete card.going;
  delete card.restUntil;
}

export function onActiveTick() {
  // If we didn't compute the motion parameters yet or if the user changed
  // the spawn position, recompute.
  if (!card.startPos || !vec3equal(getSpawnPos(), card.startPos, 0.01)) {
    resetMotion();
  }
  if (getTime() < card.restUntil) return;
  const targetPos = card.going ? getEndPos() : card.startPos;
  const distToTarget = getDistanceTo(targetPos);
  const speed = interp(0, 0.25, 1, 1, distToTarget) * props.Speed;
  moveToward(targetPos, speed);
  if (distToTarget < 0.1) {
    // Close enough to target position. Rest a bit, then invert motion.
    card.restUntil = getTime() + props.RestTime;
    card.going = !card.going;
  }
}

function getEndPos() {
  return vec3add(getSpawnPos(),
    selfToWorldDir(vec3(props.DistRight, props.DistUp, props.DistForward)));
}

function resetMotion() {
  card.startPos = getSpawnPos();
  card.going = true;
  card.restUntil = getTime() + props.RestTime;
}

export function getCardStatus() {
  temp.dirList = temp.dirList || [];
  temp.dirList.length = 0;
  getDirDescription(props.DistForward, "forward", "back", temp.dirList);
  getDirDescription(props.DistUp, "up", "down", temp.dirList);
  getDirDescription(props.DistRight, "right", "left", temp.dirList);
  return {
    description: `Moves back and forth (<color=yellow>${temp.dirList.join(', ')}</color>) with speed <color=yellow>${props.Speed.toFixed(1)}</color>, rest time <color=yellow>${props.RestTime.toFixed(1)}</color>.`
  }
}

function getDirDescription(dist, positiveWord, negativeWord, list) {
  if (dist > 0.01) {
    list.push(dist.toFixed(1) + " " + positiveWord);
  } else if (dist < -0.01) {
    list.push((-dist).toFixed(1) + " " + negativeWord);
  }
}
