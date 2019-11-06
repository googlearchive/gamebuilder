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
  propActorGroup("Targets", "@ANY", {
    label: "Who to look for:",
    pickerPrompt: "What actor should I look for?"
  }),
  propDecimal("Range", 20),
  propDecimal("ViewConeDegrees", 60),
  propDecimal("EyeHeight", 0.5),
];

export function onTick() {
  const eyePos = getPointAbove(props.EyeHeight || 1);
  for (const target of getActorsInGroup(props.Targets, props.Range)) {
    const toTarget = getPos(target).sub(eyePos);
    toTarget.normalize();
    const forward = getForward();
    const degreesOff = radToDeg(forward.angleTo(toTarget));
    const isInViewCone = degreesOff < props.ViewConeDegrees / 2;
    if (!isInViewCone) continue;
    // If we got here, the actor is in the view cone; now we need to do
    // a raycast to see if we *actually* see it (something could be in
    // the way).
    const closestHit = castAdvanced(eyePos, toTarget, props.Range, 0, CastMode.CLOSEST, true, false, true);
    if (!closestHit || closestHit.actor === target) {
      // Either there's nothing in the way, or the first in thing we found is the target actor.
      // So we can see it.
      card.triggeredEvent = { actor: target };
      break;
    }
  }
}

/**
 * @return {GEvent|undefined} The event, if one occurred.
 */
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

export function getCardStatus() {
  return {
    description: `When I see <color=yellow>${getActorGroupDescription(props.Targets, true)}</color> within distance <color=green>${props.Range.toFixed(1)}</color>`
  }
}