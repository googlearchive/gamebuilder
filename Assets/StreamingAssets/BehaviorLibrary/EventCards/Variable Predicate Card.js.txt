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
  propCardTargetActor("Target", {
    label: "Whose variable?"
  }),
  propString("VarName", "MyVar", {
    label: "Variable name"
  }),
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
  })
];

export function onCheck(msg) {
  const target = getCardTargetActor("Target", msg);
  let curValue = getVar(props.VarName, target) || 0;
  let compValue = props.Value;

  // If both sides can be interpreted as numbers, compare as numbers.
  // Else, compare as string.
  if (compValue !== "" && !isNaN(+curValue) && !isNaN(+compValue)) {
    curValue = +curValue;
    compValue = +compValue;
  } else {
    curValue = "" + curValue;
    compValue = "" + compValue;
  }

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
  return {
    description: `When variable <color=yellow>${props.VarName} ${props.Operator} ${props.Value}</color>`
  }
}