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

// Pick-up Item\n<size=70%>Something players can pick up (by touching) once for some benefit.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag interact
// property Number points 1
// property Number HealAmount 0
// property Boolean EnablePhysics false

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.memory.isTaken = false;
  const enablePhysics = valueOr(api.props.EnablePhysics, false);
  api.getActor().setIsSolid(enablePhysics);
  api.getActor().setEnablePhysics(enablePhysics);
  api.getActor().setHideInPlayMode(false);
}

/**
 * @param {HandlerApi} api
 */
export function OnTouchEnter(api) {
  if (api.memory.isTaken) {
    return;
  }

  if (!api.doesActorExist(api.message.other)) {
    return;
  }

  const other = api.getOtherActor(api.message.other);

  if (!other.getIsPlayerControllable()) {
    return;
  }

  if (api.props.points) {
    api.sendMessageToAll('PointScored', { player: api.message.other, amount: api.props.points });
  }

  if (api.props.HealAmount) {
    api.sendMessage(other.getName(), 'HitByDamager', { damager: api.name, amount: -1 * api.props.HealAmount });
  }

  api.memory.isTaken = true;
  api.getActor().setIsSolid(false);
  api.getActor().setEnablePhysics(false);
  api.getActor().setHideInPlayMode(true);
}
