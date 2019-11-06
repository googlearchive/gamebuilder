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
  propDecimal("Range", 40),

  propDecimal("OffsetX", 0),
  propDecimal("OffsetY", 0.5),
  propDecimal("OffsetZ", 0),

  propEnum("ColorMode", "USE_TINT", [
    { value: "USE_TINT", label: "same as tint" },
    { value: "OVERRIDE", label: "override" }
  ]),
  propColor("ColorOverride", "#ffffff", {
    requires: [requireEqual("ColorMode", "OVERRIDE")]
  }),
];

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction() {
  setLight(
    props.Range || 10,
    props.ColorMode === "OVERRIDE" ? props.ColorOverride || new Color(1, 1, 1) : null,
    vec3(props.OffsetX, props.OffsetY, props.OffsetZ));
}

export function getCardStatus() {
  const color = props.ColorMode === 'USE_TINT' ? '<color=white>same as tint</color>' :
    (`<color=${colorToHex(props.ColorOverride)}>${colorToHex(props.ColorOverride)}</color>`);
  return {
    description: `Turn lights on with range <color=yellow>${props.Range.toFixed(1)}</color> and color: ${color}`
  }
}