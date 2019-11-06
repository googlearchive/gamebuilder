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
  propBoolean("AnyBlock", true, {
    label: "Any block type"
  }),
  propEnum("BlockStyle", "" + BlockStyle.DIRT, getBlockStyleEnumValues(), {
    label: "Only blocks of type:",
    requires: [
      requireFalse("AnyBlock")
    ]
  })
];

export function onInit() {
  card.triggered = false;
}

export function onTerrainCollision(msg) {
  if (props.AnyBlock || msg.blockStyle == +props.BlockStyle) {
    card.triggered = true;
    cooldown(0.05);
  }
}

export function onCheck() {
  const rv = card.triggered === true ? {} : undefined;
  card.triggered = false;
  return rv;
}

function getBlockStyleEnumValues() {
  const ret = [];
  for (const key in BlockStyle) {
    ret.push({ value: "" + BlockStyle[key], label: key });
  }
  return ret;
}

function getCurBlockStyleName() {
  if (props.AnyBlock) return 'ANY';
  for (const key in BlockStyle) {
    if (BlockStyle[key] == props.BlockStyle) return key;
  }
  return '???';
}

export function getCardStatus() {
  return {
    description: `When I hit terrain of type <color=yellow>${getCurBlockStyleName()}`
  }
}