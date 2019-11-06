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
  propString("Message", "Hi, how are you?"),
  propDecimal("OffsetAbove", 0),
  propNumber("TextSize", 1),
  propDecimal("HideDelay", 3)
];

const INITIAL_OFFSET_ABOVE = 1;
const INITIAL_SCALE = 6;

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  // Create popup text if we don't have it yet.
  if (!card.popupTextActor || !exists(card.popupTextActor)) {
    card.popupTextActor = clone("builtin:PopupText",
      getPointAbove(getBoundsSize().y + INITIAL_OFFSET_ABOVE + props.OffsetAbove));
    const scale = Math.min(Math.max(INITIAL_SCALE + (props.TextSize || 0) * 0.5, 1), 12);
    send(card.popupTextActor, "SetText", { text: props.Message });
    send(card.popupTextActor, "SetScale", scale);
    send(card.popupTextActor, "SetParent", { parent: myself() });
  }
  card.popupHideTime = getTime() + (props.HideDelay === undefined ? HIDE_DELAY_SECONDS : props.HideDelay);
}

export function onResetGame() {
  // Popup is a script clone, so it gets destroyed automatically.
  delete card.popupTextActor;
  delete card.popupHideTime;
}

export function onTick() {
  if (card.popupHideTime && getTime() > card.popupHideTime) {
    // Hide the popup.
    send(card.popupTextActor, "Destroy");
    delete card.popupTextActor;
    delete card.popupHideTime;
  }
}

export function getCardStatus() {
  let msg = props.Message;
  if (msg.length > 20) {
    msg = msg.substr(0, 20) + '[...]'
  }
  return {
    description: `Says '<color=yellow>${msg}</color>'`
  }
}