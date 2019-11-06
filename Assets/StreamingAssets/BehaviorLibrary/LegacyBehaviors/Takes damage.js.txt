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

// Takes damage<size=70%>\nMake it have health and be able to take damage.

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

// tag damage
// property Boolean BroadcastDeath true
// property Number StartingHealth 3
// property Number SecondsToRespawn 3
// property Boolean DestroyCloneOnDie true
// property Number Team 0

/**
 * @param {HandlerApi} api
 */
export function OnResetGame(api) {
  api.memory.health = api.props.StartingHealth;
}

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  if (api.memory.health === undefined) {
    api.memory.health = api.props.StartingHealth;
  }

  api.memory.StartingHealth = api.props.StartingHealth;
  api.memory.team = api.props.Team || 0;
}

/**
 * @param {HandlerApi} api
 */
export async function OnHitByDamager(api) {
  const memory = api.memory;
  const actor = api.actor;
  const name = actor.name;
  const properties = api.props;

  if (memory.health > 0) {
    memory.health = Math.min(api.props.StartingHealth, Math.max(0, memory.health - api.message.amount));
    log(`new health ${memory.health}`);

    assert(memory.health !== undefined);

    if (api.message.amount > 0) {
      api.sendSelfMessage('DamageDone', { damager: api.message.damager, amount: api.message.amount });
    }

    if (memory.health === 0) {
      const hadPhysics = actor.enablePhysics;
      const hadGravity = actor.enableGravity;
      actor.enablePhysics = true;
      actor.enableGravity = true;

      assert(memory.health !== undefined);
      await api.sleep(0.1);
      kick(name, new THREE.Vector3(0, 5, 0));
      api.setTintRGB(0.2, 0.2, 0.2);

      api.sendSelfMessage('Died', { causer: api.message.damager });
      api.sendMessageToUnity('Died');

      if (api.props.BroadcastDeath) {
        api.sendMessageToAll('BroadcastDeath', { name: api.name });
      }

      // Revive after a bit
      const secondsToRespawn = properties.SecondsToRespawn;
      assert(memory.health !== undefined);
      await api.sleep(secondsToRespawn);

      if (api.props.DestroyCloneOnDie && isClone()) {
        api.destroySelf();
      }
      else {
        actor.enablePhysics = hadPhysics;
        actor.enableGravity = hadGravity;
        assert(memory.health !== undefined);
        memory.health = api.properties.StartingHealth;
        assert(memory.health !== undefined);
        api.setTintRGB(1, 1, 1);

        // Let other behaviors handle WHERE to respawn.
        api.sendSelfMessage('ShouldRespawn', {});
        api.sendMessageToUnity('Respawned');
      }
    }
    else {
      if (api.message.amount > 0) {
        api.sendMessageToUnity('Damaged');

        // Hit flash, but also doubles as grace period
        const dt = 0.05;
        for (let i = 0; i < 5; i++) {
          api.setTintRGB(1, 0, 0);
          await api.sleep(dt);
          api.setTintRGB(1, 1, 1);
          await api.sleep(dt);
        }
      }
    }

  }
}