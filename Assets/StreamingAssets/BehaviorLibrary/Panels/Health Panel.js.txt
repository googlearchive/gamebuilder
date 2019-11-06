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
  propNumber("StartingHealth", 3),
  // TEMP propNumber("DamageCooldown", 1),
  propDeck('damageDeck', 'Action', {
    label: 'When Damaged:',
    deckOptions: {
      defaultCardURIs: ['builtin:Change Tint']
    }
  }),
  propDeck('preDeathDeck', 'Action', {
    label: 'When About to Die (before delay):'
  }),
  propDeck('deathDeck', 'Action', {
    label: 'At Death (after delay):',
    deckOptions: {
      defaultCardURIs: ['builtin:Destroy Self Action Card']
    }
  }),
  propBoolean("overrideDeathDelay", false, {
    label: "Override death delay"
  }),
  propDecimal("deathDelay", 0.2, {
    requires: [requireTrue("overrideDeathDelay")],
    label: "Death delay"
  }),
  propBoolean("hideWhileDying", true, {
    label: "Hide when dying/dead"
  })
]

export function onInit() {
  mem.health = props.StartingHealth;
  // Same as reviving.
  onRevive();
  updateVars();
}

function updateVars() {
  // Publish these vars for the benefit of other cards/actors:
  setVar("isDead", !!mem.isDead);
  setVar("health", mem.health || props.StartingHealth);
  setVar("startingHealth", props.StartingHealth);
}

export function onTick() {
  // If we find out that we died here, as opposed to onDamage, it's because we
  // died as a result of some behavior simply deducting health instead of using
  // a Damage message, so the "event" is a dummy event.

  /** @type {GEvent} */
  const event = { actor: myself() };
  checkDeath(event);

  // Publish attributes:
  updateVars();

  // If it's time to run the death actions, do it now.
  maybeRunDeathActions();
}

/**
 * Damage message handler.
 * When we receive a Damage message, we deduct health from the actor.
 * @param {GDamageMessage} damageMessage
 */
export function onDamage(damageMessage) {
  // If we were just revived, don't take damage yet. This avoids race conditions when you
  // get revived and teleported away from damage and still have a stray damage message
  // from the situation you were in.
  if (getTime() - (temp.reviveTime || 0) < 0.5) {
    return;
  }

  // If we are already dead, no further damage can be taken.
  if (mem.isDead) {
    return;
  }

  // Deduct the damage from health. Don't go below 0 or above maximum.
  // (Remember the damage can be negative to mean "heal").
  let amount = 1;
  if (damageMessage.amount !== undefined) {
    assertNumber(damageMessage.amount, "damageMessage.amount");
    amount = damageMessage.amount;
  }
  // event.actor is the "event causer", so we set it to the causer of the damage.
  let event = { actor: damageMessage.causer || myself() };

  mem.health = clamp(mem.health - amount, 0, props.StartingHealth);
  // Did we die?
  checkDeath(event);
  // Call any on-damage actions that were requested, if this in fact damage (amount > 0)
  if (amount > 0) {
    callActionDeck("damageDeck", { event: event });
    // Do the engine-provided damage effect.
    legacyApi().sendMessageToUnity("Damaged");
  }
  // Don't take damage for a while
  // TEMP cooldown(props.DamageCooldown);
  cooldown(0.5);
}

/**
 * Revive message handler.
 * When we receive a Revive message, we bring the actor back to life at full health.
 */
export function onRevive() {
  const wasDead = mem.isDead;
  mem.health = props.StartingHealth;
  mem.isDead = false;
  if (wasDead) {
    legacyApi().sendMessageToUnity("Respawned");
  }
  if (props.hideWhileDying) {
    show();
  }
  temp.reviveTime = getTime();
  delete card.death;
}

/** @param {GEvent} event The event that may have caused our death. */
function checkDeath(event) {
  if (mem.isDead || typeof mem.health !== 'number' || mem.health > 0) {
    // Not dead, or already dead. In any case, nothing new.
    return;
  }
  // We're dying! Oh no!

  // Run the "when about to die" deck now.
  callActionDeck("preDeathDeck", { event: event });

  mem.isDead = true;
  // Do the engine-provided death effect.
  legacyApi().sendMessageToUnity("Died");
  if (props.hideWhileDying) {
    hide();
  }
  // Note that we don't, by default, do anything special on death --
  // we leave that up to the action cards.

  // Give a bit of a delay so the animations can play for proper
  // dramatic effect...
  const deathDelay = props.overrideDeathDelay ? props.deathDelay : (isPlayerControllable() ? 3 : 0);
  card.death = {
    stage: 1,
    time: getTime() + deathDelay,
    event: event
  };
}

function maybeRunDeathActions() {
  if (!card.death || !card.death.stage) return;
  switch (card.death.stage) {
    case 0:
      // Not dying.
      break;
    case 1:
      // Waiting for the death timer.
      if (getTime() < card.death.time) return;
      // Fire the death message (in the next frame we will handle the death actions).
      /** @type {GDeathMessage} */
      let deathMessage = { actor: myself() };
      sendToAll("Death", deathMessage);
      // Don't run actions yet, to give time for the message receivers to do something
      // before the actor actually dies.
      card.death.stage = 2;
      break;
    case 2:
      // Time to run the death action cards.
      callActionDeck("deathDeck", { event: card.death.event });
      // TODO: what if one of the cards is Destroy Self and another card is some
      // effect like spin, etc, in that case we'd want to delay the destruction
      // until the spin effect has ran for a bit? Maybe not?
      delete card.death;
      break;
    default:
      throw new Error("Invalid card.death.stage " + card.death.stage);
  }
}

export function getDescription() {
  return `${getDisplayName()} will start with ${props.StartingHealth} HP`;
}
