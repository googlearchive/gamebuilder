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

// Respawning<size=70%>\nRespawn (on death, reset, etc.)

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag gamerules
// property Actor SpawnPoint
// property Boolean RespawnOnDeath true
// property Boolean RespawnOnGameReset true
// property Boolean RespawnOnScore
// property Number YOffset 1

/**
 * @param {HandlerApi} api
 */
function respawn(api) {
  const dest = api.props.SpawnPoint;
  if (dest && api.doesActorExist(dest)) {
    const pos = api.getOtherPosition(dest);
    pos.y += api.props.YOffset;
    api.position = pos;
    api.rotation = api.getOtherRotation(dest);
  }
  else {
    api.position = api.actor.spawnPosition;
    api.rotation = api.actor.spawnRotation;
    api.actor.transformParent = api.actor.spawnTransformParent;
  }

  api.actor.velocity.set(0, 0, 0);
  api.sendSelfMessage('Respawned', {});
}

/**
 * @param {HandlerApi} api
 */
export function OnShouldRespawn(api) {
  if (api.properties.RespawnOnDeath) {
    respawn(api);
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  if (api.properties.RespawnOnGameReset) {
    respawn(api);
  }
}

/**
 * @param {HandlerApi} api
 */
export function OnPointScored(api) {
  if (api.properties.RespawnOnScore) {
    respawn(api);
  }
}
