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
  propActorGroup("Who", "@TAG:enemy", {
    pickerPrompt: "Show health bars for who?"
  }),
  propDecimal("Range", 20),
];

export function onDrawScreen() {
  getActorsInGroup(props.Who, props.Range).forEach(actor => {
    const { x, y, w, h } = getScreenRect(actor);
    const health = getAttrib("health", actor);
    const max = getAttrib("startingHealth", actor);
    if (typeof health === 'number' && typeof max === 'number') {
      drawHealthBar(x + w * 0.5, y - 5, health / max);
    } else {
      uiText(x, y, "???");
    }
  });
}

const HEALTH_BAR_WIDTH = 30;
const HEALTH_BAR_HEIGHT = 6;
function drawHealthBar(centerX, centerY, fraction) {
  const [fgColor, bgColor] = fraction > 0.7 ? [0x00ff00, 0x008000] :
    fraction > 0.3 ? [0xffff00, 0x808000] : [0xff0000, 0x800000];
  const leftX = centerX - 0.5 * HEALTH_BAR_WIDTH;
  const topY = centerY - 0.5 * HEALTH_BAR_HEIGHT;
  uiRect(leftX, topY, HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, bgColor);
  uiRect(leftX, topY, HEALTH_BAR_WIDTH * fraction, HEALTH_BAR_HEIGHT, fgColor);
}

