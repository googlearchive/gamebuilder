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
  propDecimal("OffsetX", 0),
  propDecimal("OffsetY", 1.5),
  propDecimal("OffsetZ", 0),
  propBoolean("HidePlayer", true),
  propDecimal("FieldOfView", 60),
]

export function onCameraTick(msg) {
  if (!exists(msg.target)) return;
  const target = msg.target;
  reparentIfNeeded(target);

  // If yaw is unset (null), initialize with the target's yaw.
  if (card.yaw === null) {
    card.yaw = getYaw(target) || 0;
  }

  setPos(selfToWorldPos(vec3(props.OffsetX, props.OffsetY, props.OffsetZ), msg.target));
  card.yaw += getLookAxes(target).x;
  card.pitch = Math.min(Math.max(card.pitch + getLookAxes(target).y, degToRad(-80)), degToRad(80));
  setYawPitchRoll(card.yaw, -card.pitch, 0);
  setCameraSettings({
    cursorActive: false,
    aimOrigin: getPos(),
    aimDir: getForward(),
    dontRenderActors: props.HidePlayer ? [msg.target] : null,
    fov: props.FieldOfView || 60,
  });
}

export function onInit() {
  card.yaw = null;  // null means "unset"
  card.pitch = 0;
}

export function reparentIfNeeded(target) {
  if ((target || null) === (getParent() || null)) return;
  if (exists(target)) {
    attachToParent(target);
  } else {
    detachFromParent();
  }
}