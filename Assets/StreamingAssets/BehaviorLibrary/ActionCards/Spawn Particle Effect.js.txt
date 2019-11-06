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
  propParticleEffect("ParticleEffect", Particles.EXPLOSION),
  propDecimal("OffsetX", 0),
  propDecimal("OffsetY", 0),
  propDecimal("OffsetZ", 0),
  propDecimal("RotationX", 0),
  propDecimal("RotationY", 0),
  propDecimal("RotationZ", 0),
  propDecimal("Scale", 1),
]

export function onAction() {
  if (props.ParticleEffect) {
    const euler = new THREE.Euler(
      THREE.Math.degToRad(props.RotationX),
      THREE.Math.degToRad(props.RotationY),
      THREE.Math.degToRad(props.RotationZ));
    const addRot = new Quaternion();
    addRot.setFromEuler(euler);
    const finalRot = getRot().multiply(addRot);
    const finalEuler = new THREE.Euler();
    finalEuler.setFromQuaternion(finalRot);
    const spawnPos = selfToWorldPos(vec3(props.OffsetX, props.OffsetY, props.OffsetZ));
    spawnParticleEffect(
      props.ParticleEffect,
      spawnPos,
      finalEuler.toVector3(),
      props.Scale);
  }
}
