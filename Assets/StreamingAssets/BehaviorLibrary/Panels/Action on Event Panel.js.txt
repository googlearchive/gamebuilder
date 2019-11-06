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
  // OLD:
  // propBoolean('onlyOnce', false, {
  //   label: 'Trigger only once'
  // }),
  propDeck('eventDeck', 'Event', {
    label: 'If:',
    deckOptions: {
      defaultCardURIs: ['builtin:Collision Event Card']
    }
  }),
  propDeck('actionDeck', 'Action', {
    label: 'Then do:',
    deckOptions: {
      defaultCardURIs: ['builtin:Change Tint']
    }
  }),
  propDeck('elseDeck', 'Action', {
    label: 'Else do:',
    requires: [requireTrue('advanced')]
  }),
  propEnum(
    'triggerWhen',
    'CONTINUOUS', [
      { value: 'CONTINUOUS', label: 'Continuous' },
      { value: 'ONCE_EVENT', label: 'Once (event)' },
      { value: 'ONCE_PER_GAME', label: 'Once (per game)' }
    ],
    {
      label: "Trigger mode:",
    }
  ),
  propBoolean('advanced', false, { label: 'Advanced' }),
  propDecimal("MinInterval", 0.6, {
    label: "Action interval",
    requires: [requireTrue('advanced')]
  }),
  propDecimal("ActionDuration", 0.5, {
    label: "Action duration",
    requires: [requireTrue('advanced')]
  }),
  propBoolean("EnabledOffstage", false, {
    label: "Enabled when offstage",
    requires: [requireTrue('advanced')]
  })
];

function getTriggerWhenProp() {
  // Backwards compat:
  return props.triggerWhen || (props.onlyOnce ? 'ONCE_PER_GAME' : 'CONTINUOUS');
}

function processEvents(enabled) {
  // If we already triggered and triggerWhen is 'once per game', there is nothing
  // more to do.
  if (getTriggerWhenProp() === 'ONCE_PER_GAME' && card.triggered) {
    return;
  }

  const eventToDeliver = callEventDeck("eventDeck", getTriggerWhenProp() === 'ONCE_EVENT');
  if (enabled) {
    if (eventToDeliver) {
      card.triggered = true;
      // Activate the deck. There is an implicit default timeout to deactivate it.
      callActionDeck("actionDeck", { event: eventToDeliver },
        props.ActionDuration === undefined ? 0.6 : props.ActionDuration,
        props.MinInterval === undefined ? 0.5 : props.MinInterval);
    } else if (props.advanced) {
      // Call the 'else' deck.
      callActionDeck("elseDeck", { event: {} },
        props.ActionDuration === undefined ? 0.6 : props.ActionDuration,
        props.MinInterval === undefined ? 0.5 : props.MinInterval);
    }
  } else {
    // Don't do anything. We have this here because we still need to call
    // callEventDeck to pump the event cards, otherwise they might end up holding
    // on to a stale event.
  }
}

export function onTick() {
  processEvents(true);
}

export function onOffstageTick() {
  processEvents(props.EnabledOffstage);
}

export function onResetGame() {
  delete card.triggered;
  delete card.lastEventTime;
}

