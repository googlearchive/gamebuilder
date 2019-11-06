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
  propActorGroup("Targets", "@TAG:player", {
    label: "Who to chase",
    pickerPrompt: "Who should I chase?"
  }),
  propDecimal("Range", 20, {
    label: "Search range"
  }),
  propDecimal("Speed", 2),
  propDecimal("FollowDistance", 1, {
    label: "Follow distance"
  })
];

export function onActiveTick() {
  const target = getClosestActor(getActorsInGroup(props.Targets, props.Range));
  if (exists(target)) {
    lookAt(target, true);
    moveToward(target, props.Speed, props.FollowDistance);
  }
}

export function getCardStatus() {
  return {
    description: `Chase <color=yellow>${getActorGroupDescription(props.Targets)}</color> with speed <color=green>${props.Speed.toFixed(1)}`
  }
}