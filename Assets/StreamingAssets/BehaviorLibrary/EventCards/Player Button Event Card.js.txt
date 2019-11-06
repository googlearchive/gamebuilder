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
  propEnum("WhichButton", "PRI", [
    { value: "PRI", label: "Left click" },
    { value: "SEC", label: "Right click" },
    { value: "JUMP", label: "Jump (space)" },
    { value: "KEY", label: "Key" },
  ]),
  propEnum("WhichKey", "f", getKeyEnumValues(), {
    label: "Which key?",
    requires: [requireEqual("WhichButton", "KEY")]
  }),
  propEnum("DetectWhen", "HELD", [
    { value: "HELD", label: "Held" },
    { value: "DOWN", label: "Pressed" },
    { value: "UP", label: "Released" },
  ], {
      label: 'Detect when:'
    })
];

function getMyKeyName() {
  switch (props.WhichButton) {
    case "PRI": return KeyCode.PRIMARY_ACTION;
    case "SEC": return KeyCode.SECONDARY_ACTION;
    case "JUMP": return KeyCode.JUMP;
    default:
      // Note: old versions of this card accepted WhichKey as string, which means
      // it can be in uppercase, which is why we convert below:
      return (props.WhichKey || "").toLowerCase();
  }
}

export function onKeyHeld(msg) {
  if (msg.keyName === getMyKeyName() && (props.DetectWhen || "HELD") === "HELD") {
    card.triggeredEvent = { actor: myself() };
  }
}

export function onKeyDown(msg) {
  if (msg.keyName === getMyKeyName() && props.DetectWhen === "DOWN") {
    card.triggeredEvent = { actor: myself() };
  }
}

export function onKeyUp(msg) {
  if (msg.keyName === getMyKeyName() && props.DetectWhen === "UP") {
    card.triggeredEvent = { actor: myself() };
  }
}

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

export function onResetGame() {
  delete card.triggeredEvent;
}

function getKeyEnumValues() {
  const ret = [];
  for (const keyName in KeyCode) {
    ret.push({ value: KeyCode[keyName], label: keyName });
  }
  return ret;
}

export function getCardStatus() {
  const buttonName = "<color=yellow>" + getHumanReadableButtonName() + "</color>";
  const action = "<color=green>" + (props.DetectWhen || "HELD") + "</color>";
  const actionWithVerb = props.DetectWhen === "HELD" ? ("is " + action) : ("goes " + action);
  return {
    description: "When " + buttonName + " " + actionWithVerb
  }
}

function getHumanReadableButtonName() {
  switch (props.WhichButton) {
    case "PRI": return "Left Click";
    case "SEC": return "Right Click";
    case "JUMP": return "Jump";
    default:
      return "[" + (props.WhichKey || "").toUpperCase() + "]";
  }

}