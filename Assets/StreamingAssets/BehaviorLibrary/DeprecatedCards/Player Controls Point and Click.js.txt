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

// Player Controls: Basic WASD.

export const PROPS = [
  propDecimal("Speed", 8),
];

const DIST_THRESHOLD = 0.2;

export function onInit() {
  // The target point we are trying to move to; if null, we have no target point.
  card.targetPoint = null;
  // At what time we stop trying to move to the target point (if we take longer than
  // this, it's probably because we're stuck).
  card.endMoveTime = null;
}

export function onControl() {
  checkIfArrived();
  if (card.targetPoint) {
    // Move toward target point.
    moveToward(card.targetPoint, props.Speed);
    // Render a little dot so we can see where the target point is.
    const { x, y } = getScreenPoint(card.targetPoint);
    uiRect(x - 2, y - 2, 4, 4, UiColor.WHITE);
  } else {
    // Don't move
    move(vec3zero());
  }
  lookDir(getAimDirection(), true);
}

function checkIfArrived() {
  if (card.targetPoint && (getDistanceTo(card.targetPoint) <= DIST_THRESHOLD || getTime() > card.endMoveTime)) {
    // Arrived or timed out. Reset state.
    onInit();
  }
}

export function onMouseDown() {
  const clickedPoint = getTerrainPointUnderMouse();
  if (!clickedPoint || props.Speed <= 0) return;

  const distanceToTarget = getDistanceTo(clickedPoint);
  const etaSeconds = distanceToTarget / props.Speed;

  card.targetPoint = clickedPoint;
  // If we take twice as much as expected to get there, give up (we're probably stuck).
  card.endMoveTime = getTime() + etaSeconds * 2;
}
