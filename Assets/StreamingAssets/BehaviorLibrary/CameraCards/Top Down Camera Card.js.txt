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
  propDecimal("Yaw", 45),
  propDecimal("Pitch", 45),
  propDecimal("Distance", 20),
  propDecimal("FieldOfView", 60),
]

export function onCameraTick(msg) {
  if (!exists(msg.target)) return;
  const target = msg.target;
  reparentIfNeeded(target);

  const pitch = degToRad(clamp(props.Pitch, 0, 90));

  const pointUnderMouse = getTerrainPointUnderMouse();
  const aimOrigin = getPos(msg.target);
  let aimDir = vec3z();
  if (pointUnderMouse) {
    aimDir = vec3sub(pointUnderMouse, getPos(target));
    aimDir.y = 0;
    aimDir = vec3length(aimDir) > 0.01 ? vec3normalized(aimDir) : vec3z();
  }

  setYawPitchRoll(degToRad(props.Yaw), pitch, 0);
  setPos(vec3add(getPos(target), getBackward(props.Distance)));
  setCameraSettings({
    cursorActive: true,
    aimOrigin: aimOrigin,
    aimDir: aimDir,
    fov: props.FieldOfView || 60,
  });
}

export function reparentIfNeeded(target) {
  if ((target || null) === (getParent() || null)) return;
  if (exists(target)) {
    attachToParent(target);
  } else {
    detachFromParent();
  }
}