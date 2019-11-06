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
  propStringArray("Messages", ["Hello", "Goodbye"]),
  propNumberArray("Probabilities", [50, 50]),
  propDecimal("Delay", 0)
];

export function getCardStatus() {
  const errorMessage = getError();
  if (errorMessage) {
    return { errorMessage: errorMessage };
  }
  const list = props.Messages.map((message, i) => `${message} [${props.Probabilities[i]}%]`).join(', ');
  return {
    description: `Sends random message (<color=green>${list}</color>) to <color=yellow>${getCardTargetActorDescription('Target')}`
  }
}

function getError() {
  if (props.Messages.length != props.Probabilities.length) {
    return "Error: Number of messages is not equal to number of probabilities";
  }
  const sum = props.Probabilities.reduce((sum, probability) => probability + sum, 0);
  if (sum !== 100) return "Error: Probabilities don't add up to 100";
  return null;
}

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("Target", actionMessage);
  if (!target) {
    return;
  }
  const messageName = getMessageToSend();
  if (!messageName) return;

  if (props.Delay > 0) {
    sendDelayed(props.Delay, target, messageName);
  } else {
    send(target, messageName);
  }
}

function getMessageToSend() {
  // roll is in [0, 99]
  let roll = Math.floor(Math.random() * 100);
  for (let i = 0; i < props.Probabilities.length; i++) {
    roll -= props.Probabilities[i];
    if (roll < 0) return props.Messages[i];
  }
  // If the probabilities add up to 100, we should never get here.
  return null;
}
