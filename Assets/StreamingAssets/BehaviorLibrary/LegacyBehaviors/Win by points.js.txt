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

// Score <color=green>[..]</color> points to win

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag win-conditions

/**
 * @param {HandlerApi} api
 */
export function OnPointScored(api) {
  const playerName = api.message.player;
  const pointsToWin = api.props['Points:'];
  log(`points to win: ${pointsToWin}`);
  assert(typeof playerName == 'string');
  api.sendSelfMessage('CheckPointThreshold', { pointsToWin: pointsToWin, player: playerName });
}
