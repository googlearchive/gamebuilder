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

// Elevator\nHandles "GoUp" and "GoDown" messages.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Number speed 3
// property Number height 5

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.memory.state = 'rest';
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  const me = api.getActor();
  const maxHeight = me.getSpawnPosition().y + api.props.height;
  const minHeight = me.getSpawnPosition().y;

  const p = me.getPosition();
  if (api.memory.state == 'goUp') {
    p.y += api.dt * api.props.speed;
    if (p.y > maxHeight) {
      p.y = maxHeight;
      api.memory.state = 'still';
    }
    me.setPosition(p);
  }
  else if (api.memory.state == 'goDown') {
    p.y -= api.dt * api.props.speed;
    if (p.y < minHeight) {
      p.y = minHeight;
      api.memory.state = 'still';
    }
    me.setPosition(p);
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnGoUp(api) {
  api.memory.state = 'goUp';
}

/**
 * @param {HandlerApi} api
 */
export function OnGoDown(api) {
  api.memory.state = 'goDown';
}
