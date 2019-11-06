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

// Blink <color=orange>orange</color>

// tag visual

export const PROPS = [
  propNumber('Frequency', 2)
];

export function onTick(api) {
  const on = Math.floor(getTime() * 2 * props.Frequency) % 2 == 0;
  if (on) {
    setTint(1, 0.5, 0);
  }
  else {
    setTint(1, 1, 1);
  }
}
