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
  propDecimal("Speed", 2),
  propBoolean("Loop", true),
  propActorArray("Waypoints"),
]

const WAYPOINT_RANGE = 0.5;

export function onActiveTick() {
  card.waypointNumber = card.waypointNumber || 0;
  let waypoint = props.Waypoints[card.waypointNumber];
  // If the current waypoint does not exist, or if we are close enough to it,
  // advance to the next one.
  if (!exists(waypoint) || getDistanceTo(waypoint) < WAYPOINT_RANGE) {
    advanceToNextWaypoint();
  }
  waypoint = props.Waypoints[card.waypointNumber];
  if (exists(waypoint)) {
    moveToward(waypoint, props.Speed);
    lookToward(waypoint, 6, true);
  }
}

function advanceToNextWaypoint() {
  // Advance to next waypoint if possible.
  card.waypointNumber = props.Loop ?
    (card.waypointNumber >= props.Waypoints.length ? 0 : card.waypointNumber + 1) :
    Math.min(card.waypointNumber + 1, props.Waypoints.length);
}

export function onResetGame() {
  delete card.waypointNumber;
}

export function onCollision(msg) {
  // Bumping into the next waypoint is just as good as reaching it :)
  let waypoint = props.Waypoints[card.waypointNumber];
  if (msg.other === waypoint) {
    // Advance.
    advanceToNextWaypoint();
  }
}

export function getCardStatus() {
  let count = 0;
  for (let i = 0; i < props.Waypoints.length; i++) {
    if (exists(props.Waypoints[i])) count++;
  }
  const loop = props.Loop ? "loop" : "no loop";
  return {
    description: `Follows waypoints (<color=yellow>${count}</color> waypoints, <color=yellow>${loop}</color>), speed <color=green>${props.Speed.toFixed(1)}</color>`,
    errorMessage: (count > 0 ? null : "NEED WAYPOINTS! Click card to fix.")
  };
}
