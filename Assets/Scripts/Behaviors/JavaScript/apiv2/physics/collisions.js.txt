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

// VISIBLE_TO_MONACO

/**
 * If you implement this function, it will be called when the actor
 * collides against another actor.
 *
 * <p>The actor with which the collision happened is available as
 * <tt>msg.other</tt>.
 *
 * @example
 * export function onCollision(msg) {
 *   log("I collided with " + getDisplayName(msg.other));
 * }
 *
 * @param msg The collision message.
 */
// DOC_ONLY: function onCollision(msg) {}


/**
 * If you implement this function, it will be called when the actor
 * collides against terrain (construction blocks, ground, etc).
 *
 * <p>Note: <tt>msg.blockStyle</tt> indicates which type of block the actor
 * collided with. For a list of block styles see {@link BlockStyle}.
 *
 * @param msg The collision message.
 *
 * @example
 * export function onTerrainCollision(msg) {
 *   log("I collided against a block of style " + msg.blockStyle);
 * }
 */
// DOC_ONLY: function onTerrainCollision(msg) {}