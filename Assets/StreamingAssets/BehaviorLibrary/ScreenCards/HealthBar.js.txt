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
  propNumber('X', 1350),
  propNumber('Y', 50),
  propNumber('Width', 200),
  propString('Text', 'Health'),
  propString('ColorHigh', '#00ff00'),
  propString('ColorMedium', '#ffff00'),
  propString('ColorLow', '#ff0000'),
  propString('AttribCur', "health"),
  propString('AttribMax', "startingHealth"),
  propDecimal('Opacity', 0.5)
]

export function onInit() {
  card.lastHealth = null;
  card.lastDecreaseTime = null;
}

export function onDrawScreen() {
  // If this actor is being controlled by a player, only show the health bar to
  // the player who is controlling it, not to everyone.
  const player = getControllingPlayer();
  if (player && player !== getLocalPlayer()) {
    return;
  }
  const HEIGHT = 60;
  const MARGIN = 5;

  const cur = getVar("isDead") ? 0 : (getVar(props.AttribCur) || mem[props.AttribCur] || 0);
  if (card.lastHealth !== null && card.lastHealth > cur) {
    card.lastDecreaseTime = getTime();
  }
  card.lastHealth = cur;
  const barBgColor = (card.lastDecreaseTime && getTime() - card.lastDecreaseTime < 0.2) ? UiColor.MAROON : UiColor.BLACK;

  uiRect(props.X, props.Y, props.Width, HEIGHT, barBgColor, {
    opacity: props.Opacity || 0.5
  });
  uiText(props.X + MARGIN, props.Y + MARGIN, props.Text, UiColor.WHITE);

  const max = Math.max(1, getVar(props.AttribMax) || mem[props.AttribMax] || 0);
  const fraction = Math.min(Math.max(cur / max, 0), 1);
  const barWidth = props.Width - 2 * MARGIN;
  const filledBarWidth = fraction * barWidth;

  const bgColor = fraction < 0.25 ? new THREE.Color(0.25, 0, 0) :
    fraction < 0.75 ? new THREE.Color(0.25, 0.25, 0) :
      new THREE.Color(0, 0.25, 0);
  const fgColor = fraction < 0.25 ? new THREE.Color(1, 0, 0) :
    fraction < 0.75 ? new THREE.Color(1, 1, 0) :
      new THREE.Color(0, 1, 0);

  uiRect(props.X + MARGIN, props.Y + MARGIN + 35, barWidth, 10, bgColor);
  uiRect(props.X + MARGIN, props.Y + MARGIN + 35, filledBarWidth, 10, fgColor);
  uiText(props.X + props.Width - MARGIN - 40, props.Y + MARGIN, cur, fgColor);
}