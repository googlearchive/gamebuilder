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

// Side to side

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag movement

// property Number amplitude 1
// property Number period 1
// property Boolean NorthSouth false

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const p = api.actor.spawnPosition.clone();
  const d = api.props.amplitude * Math.sin(2 * Math.PI / api.props.period * api.time);
  if (api.props.NorthSouth) {
    p.z += d;
  }
  else {
    p.x += d;
  }
  api.position = p;
}