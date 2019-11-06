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
  propCardTargetActor("Target", {
    label: "Send to:"
  }),
  propString("Message", "Ping"),
  propDecimal("Delay", 0),
  propBoolean("ForwardEventCauser", false, {
    label: "Forward event causer to recipient"
  })
];

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("Target", actionMessage);
  if (!target) {
    return;
  }
  const arg = {};
  if (props.ForwardEventCauser && actionMessage.event && actionMessage.event.actor) {
    arg.eventCauser = actionMessage.event.actor;
  }
  if (props.Delay > 0) {
    sendDelayed(props.Delay, target, props.Message, arg);
  } else {
    send(target, props.Message, arg);
  }
}

export function getCardStatus() {
  let delay = '';
  if (props.Delay > 0) {
    delay = ` with delay <color=orange>${props.Delay.toFixed(1)}</color>`;
  }
  return {
    description: `Send message <color=yellow>${props.Message}</color> to <color=green>${getCardTargetActorDescription('Target')}</color>${delay}`
  }
}