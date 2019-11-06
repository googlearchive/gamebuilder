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
  propDecimal("Interval", 3, {
    label: "Timer (sec)"
  }),
  propBoolean("Repeat", true, {
    label: "Repeat"
  }),
  propDeck('actionDeck', 'Action', {
    label: 'When the timer hits zero, what do I do?'
  })
];

export function onResetGame() {
  delete card.nextFireTime;
  delete card.hasFired;
}

export function onTick() {
  // If Repeat is undefined, it's a legacy file. Repeat defaults to true.
  if (props.Repeat === undefined) props.Repeat = true;
  // If we haven't schedule our first event yet, do that now.
  if (!card.hasFired && !card.nextFireTime) {
    // First time, schedule.
    scheduleNextFireTime();
  }
  // If the repeat property is on and we don't have a schedule fire time,
  // schedule it now.  This handles the case where the user checks "Repeat"
  // after the timer has already fired.
  if (props.Repeat && !card.nextFireTime) {
    scheduleNextFireTime();
  }
  if (getTime() > card.nextFireTime) {
    callActionDeck("actionDeck");
    card.hasFired = true;
    delete card.nextFireTime;
  }
}

function scheduleNextFireTime() {
  // Handle legacy properties: TimerAmountMin, TimerAmountMax:
  let interval = ((props.TimerAmountMin !== undefined && props.TimerAmountMax !== undefined) ?
    (props.TimerAmountMin + props.TimerAmountMax) / 2 : props.Interval) || 3;
  card.nextFireTime = getTime() + interval;
}
