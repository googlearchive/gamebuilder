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
  propActorGroup("withWhat", "@ANY", {
    label: "Collide with what:",
    pickerPrompt: "When I collide with what?",
  }),
  propBoolean("ignoreHidden", false, {
    label: "Ignore hidden actors"
  })
];

// Collision is a dirty signal, so once we trigger we hold the signal for
// this amount of time to clean it up.
export const COLLISION_STICKY_DURATION = 0.2;

export function onCollision(msg) {
  // (hack: some legacy files don't have this set)
  props.withWhat = props.withWhat === undefined ? "@ANY" : props.withWhat;

  if (!isActorInGroup(msg.other, props.withWhat)) {
    return;
  }

  if (props.ignoreHidden && !isVisible(msg.other)) {
    return;
  }

  /** @type {GEvent} */
  card.triggeredEvent = {
    actor: msg.other
  };
  card.stickyUntil = getTime() + COLLISION_STICKY_DURATION;
}

// onCheck isn't necessarily always called, so it's important we clear our
// triggered event on reset.
export function onResetGame() {
  delete card.triggeredEvent;
  delete card.stickyUntil;
}

/**
 * @return {GEvent|undefined} The event, if one occurred.
 */
export function onCheck() {
  if (card.triggeredEvent !== undefined) {
    const rv = card.triggeredEvent;
    if (getTime() > card.stickyUntil) {
      delete card.stickyUntil;
      delete card.triggeredEvent;
    }
    return rv;
  }
  else {
    return undefined;
  }
}

export function getCardStatus() {
  const groupName = getActorGroupDescription(props.withWhat, true);
  return {
    description: "When I collide with <color=yellow>" + groupName
  }
}
