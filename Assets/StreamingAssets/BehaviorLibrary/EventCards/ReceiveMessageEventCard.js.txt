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
  propString("Message", "Ping")
]

/**
 * @return {GEvent|undefined} The event, if one occurred.
 */
export function onCheck() {
  if (card.triggeredEvent !== undefined) {
    const rv = card.triggeredEvent;
    delete card.triggeredEvent;
    return rv;
  }
  else {
    return undefined;
  }
}

export function getOtherMessagesHandled(properties) {
  if (properties.Message !== undefined) {
    return [properties.Message];
  }
  else {
    return [];
  }
}

export function handleOtherMessage(msg, meta) {
  // If the message contains an explicit event causer, use it.
  // Otherwise the sender is the event causer.
  const causer = msg.eventCauser || getMessageSender();
  card.triggeredEvent = exists(causer) ? { actor: causer } : {};
}

export function onResetGame() {
  delete card.triggeredEvent;
}

export function getCardStatus() {
  return {
    description: `When I receive the message <color=yellow>${props.Message}</color>.`
  }
}