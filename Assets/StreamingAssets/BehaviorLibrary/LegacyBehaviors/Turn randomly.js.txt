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

// Turn Randomly<size=70%>\nTurns left/right randomly every so often.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag movement

// property Number IntervalMin 2
// property Number IntervalMax 4
// property Number TurnSpeed 5

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  if (api.isDead()) {
    return;
  }
  const intMin = api.props.IntervalMin;
  const intMax = api.props.IntervalMax;
  api.memory.timer = (api.memory.timer || 0) - api.dt;
  api.memory.turnDir = api.memory.turnDir || 1;
  if (api.memory.timer < 0) {
    api.memory.timer = intMin + Math.random() * (intMax - intMin);
    api.memory.turnDir = -api.memory.turnDir;
  }
  api.move.turnRight(api.dt * api.memory.turnDir * api.props.TurnSpeed);
}

