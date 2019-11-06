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

/**
 * Event Card.
 * 
 * An event card detects and signals the occurrence of a specific event (for example,
 * a collision, etc).
 *
 * <p>The only message an event card needs to receive is <tt>Check</tt>,
 * which should check to see if the event occurred. If so, it should return
 * the event (a {@link GEvent} object). If not, it should return <tt>null</tt>
 * or <tt>undefined</tt>.
 *
 * @typedef {Object} GEventCard
 * @property onCheck Called to check if the event happened. It should return
 *     a {@link GEvent} object if the event occurred, or <tt>undefined</tt>
 *     if it didn't. If this is a "predicate card" that is, a card that simply checks
 *     if a certain condition is true (like "there are fewer than 6 slimes in
 *     the world"), it can also return a boolean (true/false) to indicate
 *     its evaluation of that predicate.
 */
var GEventCard;
