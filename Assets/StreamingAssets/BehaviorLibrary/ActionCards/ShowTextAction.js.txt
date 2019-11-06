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
  propString("Text", "Hello"),
  propColor("TextColor", "#ffffff"),
  propColor("Background", "#000020"),
  propBoolean("FullScreen", true),
  propDecimal("StartDelay", 0),
  propDecimal("Duration", 3)
];

function resetState() {
  delete card.showing;
  delete card.showTime;
}

export function onResetGame() {
  resetState();
}

export function onAction(actionMessage) {
  sendToSelfDelayed(props.StartDelay, "StartShowing");
}

export function onStartShowing() {
  card.showing = true;
  card.showTime = getTime();
  sendToSelfDelayed(props.Duration, "StopShowing");
}

export function onStopShowing() {
  resetState();
}

export function onLocalTick() {
  if (!card.showing) return;
  const width = uiGetTextWidth(props.Text);
  const height = uiGetTextHeight(props.Text);
  const bgWidth = props.FullScreen ? 1610 : width + 20;
  const bgHeight = props.FullScreen ? 910 : height + 20;
  uiRect(800 - bgWidth / 2, 450 - bgHeight / 2, bgWidth, bgHeight, props.Background);
  uiText(800 - width / 2, 450 - height / 2, props.Text, props.TextColor);
}

export function getCardStatus() {
  let msg = props.Text;
  if (msg.length > 20) {
    msg = msg.substr(0, 20) + '[...]'
  }
  let delay = '';
  if (props.StartDelay > 0) {
    delay = ` after delay of <color=orange>${props.StartDelay.toFixed(1)}s</color>`;
  }
  return {
    description: `Show text '<color=yellow>${msg}</color>' for <color=green>${props.Duration.toFixed(1)}s</color>${delay}`
  }
}