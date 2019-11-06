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
 * If you implement this function, it will be called when a new player joins the game.
 *
 * @param {PlayerJoinedMessage} msg The message with information about the player who joined.
 */
// DOC_ONLY: function onPlayerJoined(msg) {}

/**
 * If you implement this function, it will be called when a player leaves the game.
 *
 * @param {PlayerLeftMessage} msg The message with information about the player who left.
 */
// DOC_ONLY: function onPlayerLeft(msg) {}

/**
 * Message passed to indicate that a player has joined.
 * See {@link onPlayerJoined}.
 * @typedef {Object} PlayerJoinedMessage
 * @property {string} playerId The ID of the player who joined.
 */
// DOC_ONLY: var PlayerJoinedMessage = {};

/**
 * Message passed to indicate that a player has left.
 * See {@link onPlayerLeft}.
 * @typedef {Object} PlayerLeftMessage
 * @property {string} playerId The ID of the player who left.
 */
// DOC_ONLY: var PlayerLeftMessage = {};
