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
  propBoolean("Everyone", true, {
    label: "Everyone wins (co-op)"
  }),
  propCardTargetActor("Target", {
    label: "Who wins?",
    requires: [requireFalse("Everyone")]
  }),
  propSound('WinSound', 'builtin:Win'),
  propSound('LoseSound', 'builtin:Lose', {
    requires: [requireFalse("Everyone")]
  })
]

const BOX_TOP = 350;
const BOX_HEIGHT = 200;
const BOX_FILL_SPEED = 3000;
const LOST_COLOR = 0x202020;
const WON_COLOR = 0x1a4d0d;

const DELAY_TO_RESET = 5;

export function onInit() {
  // If not null, then this is the game-end state (describes how the game ended, who won, etc).
  card.gameEnd = null;
}

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  if (card.gameEnd) {
    // Game was already won/lost, so activating this card has no effect.
    return;
  }
  card.gameEnd = {
    how: "victory",
    winnerPlayer: getWinningPlayer(actionMessage),
    endTime: getTime()
  };
  sendToAll("GameEnd", { gameEnd: card.gameEnd });
  sendToSelfDelayed(DELAY_TO_RESET, "TimeToReset");
}

function getWinningPlayer(actionMessage) {
  if (props.Everyone) return "@EVERYONE";
  let target = getCardTargetActor("Target", actionMessage);
  if (!target) {
    // If there is no actor target, then the only choice is myself.
    target = myself();
  }
  let player = getControllingPlayer(target);
  if (!player) {
    // Maybe the target is not a player per se, but is currently owned by a player.
    const ownerActor = getAttrib("owner", target);
    if (exists(ownerActor)) {
      player = getControllingPlayer(ownerActor);
    }
  }
  // Note: if we didn't find a player by this point... I guess everyone wins?
  return player || "@EVERYONE";
}

function didIWin() {
  return card.gameEnd.winnerPlayer === "@EVERYONE" || card.gameEnd.winnerPlayer === getLocalPlayer();
}

export function onGameEnd(msg) {
  card.gameEnd = deepCopy(msg.gameEnd);
  const won = didIWin();
  if (won && props.WinSound) {
    playSound(props.WinSound);
  }
  if (!won && props.LoseSound) {
    playSound(props.LoseSound);
  }
}

export function onTimeToReset() {
  resetGame();
}

export function onLocalTick() {
  if (!card.gameEnd || card.gameEnd.how !== "victory") {
    // Game not ended, not was not a victory, so we should stay quiet.
    return;
  }
  const elapsed = getTime() - card.gameEnd.endTime;
  const won = didIWin();

  const boxWidth = min(elapsed * BOX_FILL_SPEED, 1600);

  uiRect(800 - boxWidth / 2, BOX_TOP, boxWidth, BOX_HEIGHT, won ? WON_COLOR : LOST_COLOR);

  if (boxWidth > 800) {
    uiText(800, 430, won ? "YOU WON" : "YOU LOST", UiColor.WHITE, { center: true });
    const timeToReset = DELAY_TO_RESET - elapsed;
    const timeToResetInt = Math.ceil(Math.max(timeToReset, 0));
    uiText(800, 470,
      timeToReset > -0.5 ? `Resetting game in ${timeToResetInt}s...` : "Resetting. Please wait...",
      UiColor.WHITE, { center: true });
  }
}
