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
  propCardTargetActor("Target", {
    label: "Shoot at who?"
  }),
  propActor("Projectile", "builtin:LaserBolt", {
    pickerPrompt: "What should I shoot?",
    allowOffstageActors: true
  }),
  propSound("Sound", Sounds.LASER),
  propDecimal("Velocity", 30),
  propBoolean("CanAimUpAndDown", false),
  propDecimal("OffsetX", 0),
  propDecimal("OffsetY", 1),
  propDecimal("OffsetZ", 2),
  propEnum("ShootDir", "CAMERA_AIM", [
    { value: "CAMERA_AIM", label: "Camera aim" },
    { value: "FORWARD", label: "Forward" }
  ])
]

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  const target = getCardTargetActor("Target", actionMessage);
  if (!target || target === myself()) {
    return;
  }

  // Face the target.
  lookAt(target, !props.CanAimUpDown);

  // Calculate the position where we should spawn the projectile.
  const spawnPos = selfToWorldPos(vec3(props.OffsetX, props.OffsetY, props.OffsetZ));

  // Get a vector to the target.
  const toTarget = vec3sub(getBoundsCenter(target), getPos());

  // Too close? (sanity check).
  if (vec3length(toTarget) < 0.01) return;

  // Calculate the shot direction. This is the aim direction, for player-controllable
  // actors, or the forward direction for everything else.
  const shootDir = vec3normalized(toTarget);

  // Rotation corresponding to the shoot direction.
  const rot = new Quaternion();
  rot.setFromUnitVectors(vec3z(), shootDir);

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

export function getCardErrorMessage() {
  if (props.Target === "MYSELF") {
    return "Target can't be set to MYSELF.";
  }
}

export function onGetActionDescription() {
  return "Shoot Target";
}

export function getCardStatus() {
  return {
    description: `Fire projectile <color=white>${getDisplayName(props.Projectile)}</color> at <color=green>${getCardTargetActorDescription('Target')}</color> with velocity <color=yellow>${props.Velocity.toFixed(1)}</color>`
  }
}