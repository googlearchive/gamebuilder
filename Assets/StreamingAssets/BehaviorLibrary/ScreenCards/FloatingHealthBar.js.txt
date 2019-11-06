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
  propDecimal('OffsetY', 1),
  propDecimal('Size', 1),
  propColor('FgHigh', '#00ff00'),
  propColor('BgHigh', '#002000'),
  propColor('FgMedium', '#ffff00'),
  propColor('BgMedium', '#202000'),
  propColor('FgLow', '#ff0000'),
  propColor('BgLow', '#200000'),
  propString('AttribCur', "health"),
  propString('AttribMax', "startingHealth"),
  propDecimal('Opacity', 0.8),
  propBoolean('OnlyIfVisible', true, {
    label: "Only draw if visible"
  }),
]

export function onDrawScreen() {
  const worldAnchor = getBoundsCenter();
  worldAnchor.y += props.OffsetY;

  if (props.OnlyIfVisible) {
    // Only draw if the world anchor position is visible from the camera.
    const cameraPos = getLocalCameraPos();
    if (cameraPos === null) return;
    const cameraToAnchor = vec3normalized(vec3sub(worldAnchor, cameraPos));
    const maxDist = Math.max(0, getDistanceBetween(cameraPos, worldAnchor) - 0.5);
    // Request boolean raycast with actors and terrain, exclude self:
    if (castAdvanced(cameraPos, cameraToAnchor, maxDist, 0, CastMode.BOOLEAN, true, false, true)) {
      // Hit some obstruction.
      return;
    }
  }

  const screenSphere = getScreenSphere(worldAnchor, props.Size);

  if (!screenSphere) return;  // Off-screen.

  const width = screenSphere.radius;
  const height = width * 0.2;
  const left = screenSphere.center.x - width / 2;
  const top = screenSphere.center.y - height;

  const cur = getVar("isDead") ? 0 : (getVar(props.AttribCur) || mem[props.AttribCur] || 0);
  const max = Math.max(1, getVar(props.AttribMax) || mem[props.AttribMax] || 0);
  const fraction = clamp(cur / max, 0, 1);

  const fgColor = getColorPropForHealthFraction(fraction, 'FgLow', 'FgMedium', 'FgHigh');
  const bgColor = getColorPropForHealthFraction(fraction, 'BgLow', 'BgMedium', 'BgHigh');

  // Background.
  uiRect(left, top, width, height, bgColor, { opacity: props.Opacity });
  // Filled part.
  uiRect(left, top, width * fraction, height, fgColor, { opacity: props.Opacity });
  // Border.
  uiRect(left, top, width, height, fgColor, { style: "BORDER", opacity: props.Opacity });
}

function getColorPropForHealthFraction(fraction, propLow, propMedium, propHigh) {
  return fraction > 0.75 ? props[propHigh] :
    fraction > 0.25 ? props[propMedium] : props[propLow];
}
