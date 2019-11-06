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

// Look at Actor

export const PROPS = [
  propActorGroup("Targets", "@TAG:player", {
    label: "Look at who:",
    pickerPrompt: "Who should I look at?"
  }),
  propDecimal("Range", 20, {
    label: "Max distance"
  }),
  propBoolean("YawLock", true, {
    label: "Only horizontal (yaw)"
  })
];

export function onActiveTick() {
  props.Targets = props.Targets === undefined ? "@TAG:player" : props.Targets; // for legacy games
  const target = getClosestActor(getActorsInGroup(props.Targets, props.Range));
  if (target) {
    lookAt(target, props.YawLock);
  }
}

export function getCardStatus() {
  const axes = props.YawLock ? "horizontal only" : "all 3 axes";
  return {
    description: `Look at <color=yellow>${getActorGroupDescription(props.Targets)}</color> (${axes})`
  }
}
