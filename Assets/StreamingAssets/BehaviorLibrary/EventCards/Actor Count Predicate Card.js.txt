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
  propActorGroup("WithWhat", "@ANY", {
    label: "Count what actors?:",
    pickerPrompt: "Count what actors?",
  }),
  propBoolean("LimitRange", false),
  propDecimal("Range", 1000, {
    label: "Max range",
    requires: [requireTrue("LimitRange")]
  }),
  propBoolean("IncludeSelf", false),
  propEnum("Operator", "==", [
    { value: "==", label: "Equal" },
    { value: "!=", label: "Not equal" },
    { value: "<", label: "Less than" },
    { value: ">", label: "Greater than" },
    { value: ">=", label: "Greater than or equal" },
    { value: "<=", label: "Less than or equal" },
  ], {
      label: "Comparison"
    }
  ),
  propString("Value", "0", {
    label: "Comparison value"
  }),
];

export function onCheck() {
  const matches = getActorsInGroup(props.WithWhat, props.LimitRange ? props.Range : null, props.IncludeSelf);
  const curValue = matches ? matches.length : 0;
  const compValue = +props.Value;
  switch (props.Operator) {
    case "==": return curValue === compValue;
    case "!=": return curValue !== compValue;
    case "<": return curValue < compValue;
    case ">": return curValue > compValue;
    case ">=": return curValue >= compValue;
    case "<=": return curValue <= compValue;
    default:
      throw new Error("Invalid operator: " + props.Operator);
  }
}

export function getCardStatus() {
  const range = props.LimitRange ? ` (range <color=orange>${props.Range.toFixed(1)}</color>)` : '';
  return {
    description: `When the # of <color=green>${getActorGroupDescription(props.WithWhat)}</color> is <color=yellow>${props.Operator} ${props.Value}</color>${range}`
  }
}