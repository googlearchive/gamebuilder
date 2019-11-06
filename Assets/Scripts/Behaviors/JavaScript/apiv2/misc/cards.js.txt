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
 * <p>If you implement this function, it will get called
 * periodically to dynamically get your card's title,
 * description and debug information for display on the UI.
 *
 * <p>You must return an object with your card's dynamic data,
 * such as your title, description, error message and debug text.
 *
 * <p>This function runs frequently (currently every 1 second) while the
 * user is editing cards (in the Logic tool). This
 * function is running on every single card the player is seeing on the screen
 * every 1 second, so the implementation should be as fast as possible.
 * In particular, don't try to compute anything too expensive here!
 *
 * @example
 * export const PROPS = [
 *   propDecimal('Speed', 2)
 * ];
 *
 * export function getCardStatus() {
 *   return {
 *     title: 'Move (' + props.Speed + ')';
 *     description: 'Moves with a speed of <color=yellow>' + props.Speed;
 *   }
 * }
 *
 * @return {RuntimeCardStatus} The status of the card.
 */
// DOC_ONLY: function getCardStatus() {}

/**
 * @typedef {Object} RuntimeCardStatus
 * @prop {string?} title The dynamic title of your card. If this is unset or null,
 *     the editor will display the default static title.
 * @prop {string?} description The dynamic description of your card. If this is unset
 *     or null, the editor will display the default static description.
 *     You can use color tags to color portions of the text if you want.
 *     You don't have to close your color tags. We won't judge you.
 * @prop {string?} errorMessage The error message of your card. If this is unset or
 *     null, it means there is no error, which is great.
 * @prop {string?} debugText The debug text to show on the card when the you
 *     press the LEFT ALT key while looking at the card. You can use this to show
 *     the current state of the card. Or a poem. Or anything, really.
 */
// DOC_ONLY: var RuntimeCardStatus;

/**
 * <p>DEPRECATED. Please implement {@link getCardStatus} instead.
 *
 * <p>If you implement this function, it will get called to get
 * an error message to show on top of the card, to the user.
 *
 * <p>You can use it for basic validation of card properties.</p>
 *
 * @example
 * export const PROPS = [
 *   propDecimal('interval', 2)
 * ];
 *
 * export function getCardErrorMessage() {
 *   if (props.interval < 1) {
 *     return "Interval can't be < 1!";
 *   }
 * }
 */
// DOC_ONLY: function getCardErrorMessage() {}

/**
 * If you implement this function, it will get called to let
 * you know that the card is being removed from the actor.
 *
 * <p>You can use it to do any cleanup you need, for example,
 * delete stuff from actor memory or reset any actor properties
 * that are controlled by your card.</p>
 *
 * <p>You don't need to clean up {@link card} memory, because
 * your card is getting deleted anyway. The only cleanup you need
 * to do is anything other than card memory, such as anything
 * you put in the Actor's {@link mem}, etc.</p>
 */
// DOC_ONLY: function onCardRemoved() {}
