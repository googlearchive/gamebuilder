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

// Does damage<size=70%>\nDamages actors that can take damage on contact. If team is not 0, it will not affect others with the same team.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag damage
// property Number DamagePoints 1
// property Number Team 0
// property Boolean DestroyOnContact false

/**
 * @param {HandlerApi} api
 */
export function OnTouchEnter(api) {
  if (api.isDead()) {
    return;
  }

  if (!api.doesActorExist(api.message.other)) {
    return;
  }

  // Guard friendly fire.
  const myTeam = api.props.Team || 0;
  const theirTeam = api.getOtherMemory(api.message.other, "team");
  if (myTeam && theirTeam && myTeam == theirTeam) {
    return;
  }

  api.sendMessage(api.message.other, 'HitByDamager', { damager: api.name, amount: api.props.DamagePoints });

  if (api.props.DestroyOnContact && api.getActor().getWasClonedByScript()) {
    api.destroySelf();
  }
}