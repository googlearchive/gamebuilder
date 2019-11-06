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
  propDeck('screenDeck', 'Screen', {
    label: 'What appears on the screen?'
  }),
  propEnum("whoseScreen", "EVERYBODY", [
    { value: "EVERYBODY", label: "Everybody" },
    { value: "THIS_PLAYER", label: "This Player" },
  ], {
      label: "Whose screen?"
    })
];

export function onLocalTick() {
  const whose = props.whoseScreen || "THIS_PLAYER";
  if (whose === "EVERYBODY" || getControllingPlayer() === getLocalPlayer()) {
    callDeck(props.screenDeck, "DrawScreen");
  }
}
