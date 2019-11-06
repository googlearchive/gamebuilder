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
  propNumber("Points", 1),
  propBoolean("OverrideWhoScores", false, {
    label: "Override who scores"
  }),
  propActor("WhoScores", "", {
    label: "Who scores?",
    requires: [requireTrue("OverrideWhoScores")]
  })
]

/** @type {GActionMessage} actionMessage */
export function onAction(actionMessage) {
  // Let's try to figure out what player scored the point.
  let scoringActor = null;
  if (props.OverrideWhoScores) {
    scoringActor = props.WhoScores;
  } else {
    scoringActor = actionMessage.event.actor || myself();
  }

  let player = getControllingPlayer(scoringActor);
  if (!player) {
    // Try using the actor that 'owns' the target actor (in case it's a grabbed object, etc).
    const ownerActor = getAttrib("owner", scoringActor);
    if (exists(ownerActor)) {
      player = getControllingPlayer(ownerActor);
    }
  }

  if (!player) {
    // We used to show this warning here, but it's too spammy, as random stuff might trigger it:
    //logError("Can't score point: actor " + getDisplayName(scoringActor) + " is not controlled by a player.");
    return;
  }

  // Broadcast a PointScored message to everyone. If there is a scoreboard actor in the scene,
  // it will get this message and do the right thing.
  sendToAll("PointScored", { player: player, amount: props.Points || 1 });
}

export function getCardStatus() {
  let forWho = '';
  if (props.OverrideWhoScores) {
    forWho = ` for <color=orange>${getDisplayName(props.WhoScores)}`;
  }
  return {
    description: `Score <color=green>${props.Points} point${props.Points === 1 ? '' : 's'}</color>${forWho}`
  }
}