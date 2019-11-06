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

// Dialog box on touch

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// property Actor TextPrefab
// property String Message hello
// property Number yOffset 1
// property Number Duration 1

/**
 * @param {HandlerApi} api
 */
export async function OnTouchEnter(api) {
  const p = api.position;
  p.y += api.props.yOffset;

  if (!api.isValidActor(api.props.TextPrefab)) {
    return;
  }
  const inst = api.clone(api.props.TextPrefab, p, api.rotation);

  // TODO make the message a property
  api.sendMessage(inst, 'SetText', { text: api.props.Message });

  // Instead of sleeping async, send Destroy on a timer.
  await api.sleep(api.props.Duration);
  api.sendMessage(inst, "Destroy");
}