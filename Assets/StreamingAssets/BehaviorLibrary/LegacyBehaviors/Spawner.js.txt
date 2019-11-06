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

// Spawner<size=70%>\nSpawn a clone of something on game reset

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag utility

// property Actor source

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  api.actor.isSolid = false;
  api.actor.hideInPlayMode = true;
}

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  if (!api.props.source) {
    logError(`Spawner ${api.actor.displayName}: You need to set a source actor to clone from!`);
    return;
  }
  const clone = api.clone(api.props.source, api.getActor().getPosition(), api.getActor().getRotation());
}
