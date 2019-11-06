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
  //TEMP propNumber("Duration", 1),
]

/** @param {GActionMessage} actionMessage */
export function onAction(actionMessage) {
  card.originalTintHex = card.originalTintHex || getTintHex();
  // setTintColor(new THREE.Color(
  //  props.ColorR / 255.0, props.ColorG / 255.0, props.ColorB / 255.0));
  setTintColor(new THREE.Color(1, 0, 0));
  // TEMP card.restoreTintTime = getTime() + props.Duration;
  card.restoreTintTime = getTime() + 0.5;
}

export function onTick() {
  if (card.restoreTintTime) {
    if (getTime() > card.restoreTintTime) {
      delete card.restoreTintTime;
      setTintHex(card.originalTintHex);
    }
    else {
      const red = Math.floor(getTime() * 6) % 2 == 0;
      setTintHex(red ? "#ff0000" : card.originalTintHex);
    }
  }
}