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
  propString("Message", "Ping"),
  propBoolean("LimitRange", false, {
    label: "Limit range"
  }),
  propDecimal("Range", 20, {
    requires: [requireTrue("LimitRange")]
  }),
  propDecimal("Delay", 0)
];

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  if (props.LimitRange) {
    const targets = overlapSphere(getPos(), props.Range);
    if (props.Delay > 0) {
      sendToManyDelayed(props.Delay, targets, props.Message);
    } else {
      sendToMany(targets, props.Message);
    }
  } else {
    if (props.Delay > 0) {
      sendToAllDelayed(props.Delay, props.Message);
    } else {
      sendToAll(props.Message);
    }
  }
}

export function getCardStatus() {
  let delay = '';
  let range = '';
  if (props.Delay > 0) {
    delay = ` with delay <color=green>${props.Delay.toFixed(1)}s</color>`;
  }
  if (props.LimitRange) {
    range = ` with range <color=orange>${props.Range.toFixed(1)}</color>`;
  }
  return {
    description: `Broadcasts message <color=yellow>${props.Message}</color>${delay}${range}`
  }
}