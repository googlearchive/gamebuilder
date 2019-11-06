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

];

const START_X = 50;
const START_Y = 800;
const BOX_WIDTH_CHARS = 10;
const BOX_HEIGHT = 80;
const HEADER_HEIGHT = 22;

const PLAYER_COLORS = [
  "#808080",
  "#a00000",
  "#00a000",
  "#a0a000",
  "#0000a0",
  "#a000a0",
  "#00a0a0",
  "#a0a0a0",
]


export function onInit() {
  // Table of scores.
  //   key: player ID (string)
  //   value: current score (integer)
  card.scores = {};
}

export function onTick() {
  setCommentText(formatScoreBoardText());
}

export function onDrawScreen() {
  // TODO: make this UI better
  const numPlayers = getAllPlayers().length;
  const boxWidth = BOX_WIDTH_CHARS * UI_TEXT_CHAR_WIDTH;
  const myPlayerNumber = getPlayerNumber();
  uiRect(START_X, START_Y, boxWidth * numPlayers, HEADER_HEIGHT, 0x303030);
  for (let i = 1; i <= numPlayers; i++) {
    uiRect(START_X + (i - 1) * boxWidth, START_Y + HEADER_HEIGHT, boxWidth, BOX_HEIGHT - HEADER_HEIGHT,
      colorFromHex(PLAYER_COLORS[i % PLAYER_COLORS.length]));
    if (myPlayerNumber === i) {
      uiRect(START_X + (i - 1) * boxWidth, START_Y, boxWidth, BOX_HEIGHT,
        0xffffff, { style: RectStyle.BORDER });
    }
  }
  uiText(START_X, START_Y, formatScoreBoardText());
}

export function onPointScored(msg) {
  if (!msg.player) {
    logError("ScoreBoard got a PointScored message with no player. Ignoring.");
    return;
  }
  assertString(msg.player, "ScoreBoard: player ID must be a string.");
  card.scores[msg.player] = (card.scores[msg.player] || 0) + (msg.amount || 1);
}

function formatScoreBoardText() {
  const pieces = [];
  const players = getAllPlayers();
  const me = getLocalPlayer();
  pieces.push('SCORE\n');
  for (let i = 1; i <= players.length; i++) {
    pieces.push(toFixedLength('Player ' + i, BOX_WIDTH_CHARS - 1, true));
    pieces.push(' ');
  }
  pieces.push('\n');
  for (let i = 1; i <= players.length; i++) {
    const playerId = getPlayerByNumber(i);
    pieces.push(toFixedLength(card.scores[playerId] || 0, BOX_WIDTH_CHARS - 1, true));
    pieces.push(' ');
  }
  return pieces.join('');
}
