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


export const PROPS = [
  propDeck('actionDeck', 'Action', {
    label: 'Always do:'
  }),
  propBoolean('advanced', false, { label: 'Advanced' }),
  propDecimal("MinInterval", 0.6, {
    label: "Action interval",
    requires: [requireTrue('advanced')]
  }),
];

export function onTick() {
  // Note: we pass 1 for action duration just because we have to pass something, but since this
  // is an "Always" panel the action deck will be continually active and the duration doesn't
  // matter as long as it's positive, I guess?
  callActionDeck("actionDeck", { event: { actor: myself() } }, 1, props.MinInterval || 0.5);
}

