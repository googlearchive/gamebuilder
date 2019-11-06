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
 * Action Card.
 * 
 * An action card executes a specific action. This can be a one-shot action or a
 * continued action.
 * 
 * <p>One-shot actions (like firing a projectile, for instance) must implement
 * <tt>onAction</tt>. This is called when the action should occur.
 * 
 * <p>Continuous actions (such as movement) can implement onActivate (called when
 * the card becomes active), onActiveTick (called every frame when the card is active),
 * and onDeactivate (called when the card becomes inactive).
 *
 * @typedef {Object} GActionCard
 * @property onAction Called when the action is to be executed. The
 *     message parameter is {@link GActionMessage}.
 * @property onActivate Called when the card is activated.
 * @property onActiveTick Called every tick while
 *     the action card is active.
 * @property onDeactivate Called when the card is deactivated.
 * @property onGetActionDescription Called to obtain a description of
 *     this action (returns string).
 */
var GActionCard;

