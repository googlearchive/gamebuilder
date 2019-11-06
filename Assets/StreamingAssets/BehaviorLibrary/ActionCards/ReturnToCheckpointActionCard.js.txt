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

/** @param {GActionMessage} actionMessage */
export function onAction(actionMessage) {
  const pos = card.lastCheckPos || getSpawnPos();
  setPos(pos);
  sendToSelf('Revive');
}

export function onResetGame() {
  delete card.lastCheckPos;
}

export function onSetCheckpoint(msg) {
  log(`Checkpoint reached! Pos is ${msg.pos}`);
  card.lastCheckPos = msg.pos;
}
