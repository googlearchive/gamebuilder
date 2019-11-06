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

// Create blocks\nPress E to create, F to delete

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag ability

/**
 * @param {HandlerApi} api
 */
export function OnKeyDown(api) {
  const me = api.getActor();
  const cell = me.getPosition()
    .addScaledVector(me.getForward(), 3.0)
    // Convert to cell coordinates
    .multiply(new Vector3(0.4, 0.66, 0.4))
    .add(new Vector3(0.5, 0.5, 0.5));
  if (api.message.keyName == 'e') {
    api.setCell(cell.x, cell.y, cell.z, 1, 0, Math.random() * 19);
  }
  else if (api.message.keyName == 'f') {
    api.setCell(cell.x, cell.y, cell.z, 0, 0, 0);
  }
}