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
  propSound('Sound', 'builtin:Lose')
]

const BOX_TOP = 350;
const BOX_HEIGHT = 200;
const BOX_FILL_SPEED = 3000;
const BOX_COLOR = 0x202020;

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
  card.gameEnd = { how: "gameover", endTime: getTime() };
  sendToAll("GameEnd", { gameEnd: card.gameEnd });
  if (props.LoseSound) {
    playSound(props.LoseSound);
  }
  sendToSelfDelayed(DELAY_TO_RESET, "TimeToReset");
}

export function onGameEnd(msg) {
  card.gameEnd = deepCopy(msg.gameEnd);
}

export function onLocalTick() {
  if (!card.gameEnd || card.gameEnd.how !== "gameover") {
    // Game not ended, not was not a victory, so we should stay quiet.
    return;
  }
  const elapsed = getTime() - card.gameEnd.endTime;
  const boxWidth = min(elapsed * BOX_FILL_SPEED, 1600);

  uiRect(800 - boxWidth / 2, BOX_TOP, boxWidth, BOX_HEIGHT, BOX_COLOR);

  if (boxWidth > 800) {
    const timeToReset = DELAY_TO_RESET - elapsed;
    const timeToResetInt = Math.ceil(Math.max(timeToReset, 0));
    uiText(800, 430, "GAME OVER", UiColor.WHITE, { center: true });
    uiText(800, 470,
      timeToReset > -0.5 ? `Resetting game in ${timeToResetInt}s...` : "Resetting. Please wait...",
      UiColor.WHITE, { center: true });
  }
}

export function onKeyDown(msg) {
  if (card.gameEnd && card.gameEnd.how === "gameover" && msg.keyName === 'r') {
    resetGame();
  }
}

export function onTimeToReset() {
  resetGame();
}
