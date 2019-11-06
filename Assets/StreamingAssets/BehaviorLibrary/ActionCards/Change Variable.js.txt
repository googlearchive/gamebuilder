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
  propCardTargetActor("CardTargetActor", {
    label: "Whose variable?"
  }),
  propString("VarName", "MyVar", {
    label: "Variable name"
  }),
  propEnum("Operation", "SET", ["SET", "ADD", "SUBTRACT", "MULTIPLY"]),
  propString("Value", "0"),
  propBoolean("Clamp", false, {
    label: "Enforce min/max"
  }),
  propDecimal("ClampMin", 0, {
    label: "Min",
    requires: [requireTrue("Clamp")]
  }),
  propDecimal("ClampMax", 100, {
    label: "Max",
    requires: [requireTrue("Clamp")]
  })
];

/**
 * @param {GActionMessage} actionMessage 
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("CardTargetActor", actionMessage);
  if (!target) {
    return;
  }
  const msg = {
    op: props.Operation,
    name: props.VarName,
    value: props.Value,
    clamp: props.Clamp
  };
  if (props.Clamp) {
    msg.clampMin = props.ClampMin;
    msg.clampMax = props.ClampMax;
  }
  send(target, "ChangeVar", msg);
}

export function getCardStatus() {
  const name = `variable <color=green>${props.VarName}</color>`;
  const val = `<color=orange>${props.Value}</color>`;
  const op = `<color=yellow>${props.Operation}</color>`;
  const phrase =
    props.Operation === 'SET' ? `${op} ${name} to ${val}` :
      props.Operation === 'ADD' ? `${op} ${val} to ${name}` :
        props.Operation === 'SUBTRACT' ? `${op} ${val} from ${name}` :
          `${op} ${name} by ${val}`;
  return { description: phrase };
}