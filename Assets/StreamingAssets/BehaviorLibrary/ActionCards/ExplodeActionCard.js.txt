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
  propDecimal('BlastRadius', 10),
  propDecimal('BlastSpeed', 30),
  propNumber('Damage', 1),
  propDecimal('CenterOffsetX', 0),
  propDecimal('CenterOffsetY', 0),
  propDecimal('CenterOffsetZ', 0),
  propParticleEffect('Particles'),
  propSound('Sound', 'builtin:Explosion'),
  propDecimal('UpBlastSpeed', 10),
];

/**
 * @param {GActionMessage} actionMessage
 */
export function onAction(actionMessage) {
  const boundsCenter = getBoundsCenter();
  const worldOffset = selfToWorldDir(vec3(props.CenterOffsetX, props.CenterOffsetY, props.CenterOffsetZ));
  const center = vec3add(boundsCenter, worldOffset);
  const actorsToPush = overlapSphere(center, props.BlastRadius);
  for (const actor of actorsToPush) {
    const thisPos = getPos(actor);
    let toActor = vec3sub(thisPos, center);
    let distToActor = vec3length(toActor);
    toActor = distToActor < 0.01 ? vec3y(1) : vec3normalized(toActor);
    let blastSpeed = interp(0, props.BlastSpeed, props.BlastRadius, 0, distToActor);
    push(actor, vec3add(vec3scale(toActor, blastSpeed), vec3y(props.UpBlastSpeed)), false);
    // Tell the actor to take damage. If it has a Health Panel, it will handle this.
    if (props.Damage > 0) {
      send(actor, "Damage", { causer: myself(), amount: props.Damage });
    }
  }
  spawnParticleEffect(props.Particles || "builtin:ExplosionParticles", center);
  if (props.Sound) {
    playSound(props.Sound);
  }
}

export function getCardStatus() {
  return {
    description: `Explode with blast radius <color=yellow>${props.BlastRadius.toFixed(1)}</color> and speed <color=green>${props.BlastSpeed.toFixed(1)}</color>`
  }
}
