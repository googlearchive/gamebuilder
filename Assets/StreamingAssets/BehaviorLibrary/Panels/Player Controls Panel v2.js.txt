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
  // Note: this is an enum just for UX purposes; the underlying value is always
  // parsed as an integer. If you create values that are not integers here,
  // also update PlayerControllableWizard appropriately.
  propEnum('PlayerNumber', '1', [
    { value: '0', label: 'NOBODY' },
    { value: '1', label: 'Player 1' },
    { value: '2', label: 'Player 2' },
    { value: '3', label: 'Player 3' },
    { value: '4', label: 'Player 4' },
    { value: '5', label: 'Player 5' },
    { value: '6', label: 'Player 6' },
    { value: '7', label: 'Player 7' },
    { value: '8', label: 'Player 8' },
    { value: '9', label: 'Player 9' },
    { value: '10', label: 'Player 10' },
    { value: '11', label: 'Player 11' },
    { value: '12', label: 'Player 12' },
    { value: '13', label: 'Player 13' },
    { value: '14', label: 'Player 14' },
    { value: '15', label: 'Player 15' },
    { value: '16', label: 'Player 16' },
  ], {
      label: "Controlled by",
      requires: [requireFalse('AutoAssign')]
    }),
  propBoolean('AutoAssign', false, {
    label: 'Auto assign'
  }),

  propBoolean('AutoColor', false, {
    label: "Automatic color"
  })
  // *** DO NOT ADD propDeck PROPERTIES TO THIS PANEL.
  // See PLAYER_CONTROLS_PANEL_HAS_NO_DECKS_ASSUMPTION in the source code!
];

const PLAYER_COLORS = [
  "#ffffff",
  "#a00000",
  "#00a000",
  "#a0a000",
  "#0000a0",
  "#a000a0",
  "#00a0a0",
  "#a0a0a0",
]

export function onInit() {
  // If present, assignedPlayer is the player that we are assigned to until further notice.
  // This may be null to mean we are (intentionally) assigned to nobody, so this being null
  // is different from it being undefined.
  delete card.assignedPlayer;
  card.lastAutoAssignCheck = getTime();
  card.lastPlayerNumber = null;
  card.oldSelf = myself(); // to detect copy/paste
  delete card.activeControlLocks;
}

function getDesiredPlayerNumber() {
  // For backward compat (we changed the prop type from number to enum).
  return +(props.PlayerNumber || 0);
}

export function onTick() {
  setVar("hasPlayerControlsPanel", true);
  setIsPlayerControllable(true);
  // If we were just copy/pasted, reinit.
  if (myself() !== card.oldSelf) {
    onInit();
  }
  // If we were assigned to a player and the player no longer exists, unassign.
  if (card.assignedPlayer !== undefined && card.assignedPlayer !== null && !playerExists(card.assignedPlayer)) {
    delete card.assignedPlayer;
  }

  if (card.assignedPlayer !== undefined) {
    // If there is a card.assignedPlayer, that takes precedence over everything.
    setVar("playerAutoAssignAvailable", false);
    setControllingPlayer(card.assignedPlayer); // might be null to mean "nobody"
  } else if (props.AutoAssign) {
    setVar("playerAutoAssignAvailable", true);
    maybeAutoAssign();
  } else {
    setVar("playerAutoAssignAvailable", false);
    // Otherwise, go by player#.
    const desiredPlayerNumber = getDesiredPlayerNumber();
    const playerId = desiredPlayerNumber > 0 ? getPlayerByNumber(desiredPlayerNumber) : null;
    setControllingPlayer(playerId);
  }
  if (props.AutoColor) {
    updateColor();
  }
  drawControlsLock();
  dialogueOnTick();
}

export function onCardRemoved() {
  deleteVar("hasPlayerControlsPanel", false);
  setControllingPlayer(null);
  setCameraActor(null);
  setIsPlayerControllable(false);
}

function maybeAutoAssign() {
  // Run this logic once every 2 seconds, not every frame.
  if (getTime() < (card.lastAutoAssignCheck || 0) + 2) {
    return;
  }
  const allActors = getActors();
  card.lastAutoAssignCheck = getTime();
  // If we don't have priority, yield.
  if (!doWeHaveAutoAssignPriority(allActors)) return;
  // Okay, we have priority, so check if there is a player without an actor.
  const playerHasActor = {};
  for (const actor of allActors) {
    const player = getControllingPlayer(actor);
    if (player) playerHasActor[player] = true;
  }
  for (const playerId of getAllPlayers()) {
    if (!playerHasActor[playerId]) {
      card.assignedPlayer = playerId;
      return;
    }
  }
}

function updateColor() {
  const player = getControllingPlayer();
  const playerNumber = player ? (getPlayerNumber(player) || 0) : 0;
  if (playerNumber !== card.lastPlayerNumber) {
    setTintHex(PLAYER_COLORS[playerNumber % PLAYER_COLORS.length]);
    card.lastPlayerNumber = playerNumber;
  }
}

function doWeHaveAutoAssignPriority(allActors) {
  // The actor that has auto assign priority is the one with the lowest ID.
  for (const actor of allActors) {
    if (exists(actor) && getVar("playerAutoAssignAvailable", actor) && actor < myself()) {
      return false;
    }
  }
  return true;
}

// AssignPlayer message requests us to assign to a given player until further notice.
// msg.playerId is the player ID to assign to, or null to unassign.
export function onAssignPlayer(msg) {
  assert(msg.playerId === null || typeof msg.playerId === 'string', 'msg.playerId in AssignPlayer message must be string or null');
  card.assignedPlayer = msg.playerId;
}

// UnassignPlayer message requests us to unassign, returning to our default behavior.
export function onUnassignPlayer() {
  delete card.assignedPlayer;
}

// DEPRECATED:
export function onRequestSetCamera(msg) {
  if (exists(msg.cameraActor)) {
    setCameraActor(msg.cameraActor);
  }
}

// Someone is requesting us to lock player controls.
export function onLockControls(msg) {
  assert(msg.name, "LockControls: msg.name must be non-empty");
  card.activeControlLocks = card.activeControlLocks || {};
  card.activeControlLocks[msg.name] = msg.debugString || "";
  setVar("ControlsLocked", true);
}

// Someone is requesting us to unlock player controls.
export function onUnlockControls(msg) {
  assert(msg.name, "UnlockControls: msg.name must be non-empty");
  if (card.activeControlLocks) delete card.activeControlLocks[msg.name];
  setVar("ControlsLocked", card.activeControlLocks && Object.keys(card.activeControlLocks).length > 0);
}

function drawControlsLock() {
  if (!getVar("ControlsLocked")) return;
  uiText(800, uiGetScreenHeight() - 30, "[Controls locked]", UiColor.WHITE, { opacity: 0.8, center: true });
}

export function onResetGame() {
  dialogueResetGame();
}

export function onKeyDown(msg) {
  dialogueOnKeyDown(msg);
}


/* ================================================================================================ */
/* DIALOGUE FUNCTIONALITY */
/* ================================================================================================ */

const DIA_LOCK_NAME = "DialoguePanel";
const DIA_PADDING = 30;
const DIA_LINE_HEIGHT = 25;
const DIA_SPEAKER_LINE_HEIGHT = 40;
const DIA_SPACE_BETWEEN_TEXT_AND_REPLIES = 30;
const DIA_KEY_CHOICES = {
  "return": -1,
  "enter": -1,
  "1": 0,
  "2": 1,
  "3": 2,
  "[1]": 0,
  "[2]": 1,
  "[3]": 2,
}

function dialogueResetGame() {
  delete card.dia;
}

// msg.requester: the actor requesting the dialogue
// msg.speaker: the name of the speaker
// msg.color: the color to use when displaying the speaker name
// msg.text: the text to speak
// msg.cps: text speed in characters per second
// msg.replies[]: possible replies, each:
//     text: the text of the reply
//     message: message to send to the requester when this reply is chosen
export function onLaunchDialogue(msg) {
  if (card.dia) return;
  card.dia = JSON.parse(JSON.stringify(msg));
  card.dia.startTime = getTime();
  card.dia.text = card.dia.text || "Missing dialogue text";
  card.dia.replies = card.dia.replies || [];
  const textLines = msg.text.split("\n").length;

  let textWidth = uiGetTextWidth(msg.text);
  for (const reply of card.dia.replies) {
    textWidth = Math.max(textWidth, uiGetTextWidth("[1]: " + reply.text));
  }

  const width = textWidth + 2 * DIA_PADDING;
  let height = textLines * DIA_LINE_HEIGHT +
    (card.dia.speaker ? DIA_SPEAKER_LINE_HEIGHT : 0) +
    2 * DIA_PADDING +
    DIA_SPACE_BETWEEN_TEXT_AND_REPLIES +
    DIA_LINE_HEIGHT * (card.dia.replies || [0]).length;
  card.dia.rect = {
    x: (uiGetScreenWidth() - width) / 2,
    y: (uiGetScreenHeight() - height) / 2,
    w: width,
    h: height
  };
  card.dia.animating = true;
  card.dia.charsShown = 0;
  sendToSelf("LockControls", { name: DIA_LOCK_NAME });
}

// onTick, not onLocalTick because we only want to show UI on THIS player.
function dialogueOnTick() {
  if (!card.dia) return;
  if (card.dia.animating) {
    card.dia.charsShown += (card.dia.cps || 20) * deltaTime();
    if (card.dia.charsShown >= card.dia.text.length) {
      card.dia.animating = false;
    }
  }
  const textToPrint = card.dia.text.substr(0, Math.ceil(card.dia.charsShown));
  uiRect(card.dia.rect.x, card.dia.rect.y, card.dia.rect.w, card.dia.rect.h, 0x000020, { opacity: 0.85 });
  uiRect(card.dia.rect.x, card.dia.rect.y, card.dia.rect.w, card.dia.rect.h, 0xffffff, { style: "BORDER" });

  let y = card.dia.rect.y + DIA_PADDING;

  if (card.dia.speaker) {
    uiText(card.dia.rect.x + DIA_PADDING, y, "[ " + card.dia.speaker + " ]", card.dia.color);
    y += DIA_SPEAKER_LINE_HEIGHT;
  }

  for (const line of textToPrint.split("\n")) {
    uiText(card.dia.rect.x + DIA_PADDING, y, line);
    y += DIA_LINE_HEIGHT;
  }
  y += DIA_SPACE_BETWEEN_TEXT_AND_REPLIES;

  if (card.dia.animating) return;

  const repliesX = card.dia.rect.x + DIA_PADDING;

  // If no replies, just show prompt to press ENTER.
  if (card.dia.replies.length === 0) {
    blinkText(repliesX, y, "[ ENTER ]");
    return;
  }

  // Show possible replies.
  for (let i = 0; i < card.dia.replies.length; i++) {
    blinkText(repliesX, y, "[" + (i + 1) + "]");
    uiText(repliesX + 60, y, card.dia.replies[i].text);
    y += DIA_LINE_HEIGHT;
  }
}

function dialogueOnKeyDown(msg) {
  if (!card.dia) return;

  const choice = DIA_KEY_CHOICES[msg.keyName];
  if (card.dia.replies.length === 0 && choice === -1) {
    dismissDialogue();
  }
  if (choice >= 0 && choice < card.dia.replies.length) {
    // Reply chosen.
    if (exists(card.dia.requester)) {
      send(card.dia.requester, card.dia.replies[choice].message);
    }
    dismissDialogue();
  }
}

function blinkText(x, y, text) {
  const color = getTime() % 1 < 0.5 ? 0x000000 : 0x00ff00;
  uiText(x, y, text, color);
}

function dismissDialogue() {
  sendToSelf("UnlockControls", { name: DIA_LOCK_NAME });
  delete card.dia;
}