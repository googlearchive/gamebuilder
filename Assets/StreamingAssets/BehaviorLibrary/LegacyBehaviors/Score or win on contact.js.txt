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

// Score or win on touch<size=70%>\nIf this actor touches another specified actor, score a point or declare winner for the 'WhoWinsOrScores' actor.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag gamerules
// property Actor TouchesWhat
// property Actor WhoWinsOrScores
// property Boolean Wins
// property Number Points 1

/**
 * @param {HandlerApi} api
 */
export function OnTouchEnter(api) {
  const me = api.getActor();
  const goalName = api.props.TouchesWhat;
  const points = valueOr(api.props.Points, 1);

  if (api.message.other == goalName || goalName == '') {
    log(`scoring.`);
    // If no specific winner is set, we assume it's whoever touched.
    const winner = stringOr(api.props.WhoWinsOrScores, api.message.other);
    if (api.props.Wins) {
      api.sendMessageToAll('PlayerWon', { player: winner, reason: `${api.message.other} touched ${me.getName()} FTW` });
    }
    else {
      api.sendMessageToAll('PointScored', { player: winner, amount: points });
    }

    api.setCooldown(0.5);
  }
}
