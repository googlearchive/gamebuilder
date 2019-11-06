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
  propActor("Projectile", "builtin:LaserBolt", {
    pickerPrompt: "What should I shoot?",
    allowOffstageActors: true
  }),
  propSound("Sound", Sounds.LASER),
  propDecimal("Velocity", 30),

  propDecimal("OffsetX", 0),
  propDecimal("OffsetY", 1),
  propDecimal("OffsetZ", 2),

  propEnum("ShootDir", "CAMERA_AIM", [
    { value: "CAMERA_AIM", label: "Camera aim" },
    { value: "FORWARD", label: "Forward" }
  ]),

  propBoolean("HasRotationOffset", false, {
    label: "Offset rotation?"
  }),
  propDecimal("OffsetRotX", 0, {
    label: "X rotation offset",
    requires: [requireTrue("HasRotationOffset")]
  }),
  propDecimal("OffsetRotY", 0, {
    label: "Y rotation offset",
    requires: [requireTrue("HasRotationOffset")]
  }),
  propDecimal("OffsetRotZ", 0, {
    label: "Z rotation offset",
    requires: [requireTrue("HasRotationOffset")]
  }),
]

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  // Calculate the position where we should spawn the projectile.
  const spawnPos = selfToWorldPos(vec3(props.OffsetX, props.OffsetY, props.OffsetZ));

  // Calculate the rotation of the projectile.
  const rot = computeShootRotation();

  // Compute shoot direction from rotation.
  const shootDir = vec3z();
  shootDir.applyQuaternion(rot);

  // Spawn the projectile.
  const proj = clone(props.Projectile, spawnPos, rot);

  // Set ourselves as the projectile's owner (for scoring).
  setVarPlease(proj, "owner", myself());

  // Push the projectile along our aim or forward direction.
  push(proj, vec3scale(shootDir, props.Velocity));

  // Play sound.
  if (props.Sound) {
    playSound(props.Sound);
  }
}

function computeShootRotation() {
  const baseShootDir = (isPlayerControllable() && props.ShootDir === "CAMERA_AIM") ?
    vec3normalized(getAimDirection()) : getForward();
  const mat = new THREE.Matrix4().lookAt(baseShootDir, vec3zero(), vec3(0, 1, 0));
  const rot = new Quaternion();
  rot.setFromRotationMatrix(mat);

  if (!props.HasRotationOffset) return rot;

  const euler = new THREE.Euler(
    degToRad(props.OffsetRotX), degToRad(props.OffsetRotY), degToRad(props.OffsetRotZ), 'YXZ');
  const quat = new Quaternion();
  quat.setFromEuler(euler);
  rot.multiply(quat);
  return rot;
}

export function onGetActionDescription() {
  return "Shoot";
}

export function getCardStatus() {
  return {
    description: `Fire projectile <color=white>${getDisplayName(props.Projectile)}</color> with velocity <color=yellow>${props.Velocity.toFixed(1)}</color>`
  }
}