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

// Destroy If Unclaimed Clone

/******************************************************************************
 * 
 * This script is using the old API, which we will no longer support! You can
 * still look at it to get an idea of how to do things, but we encourage you to
 * write new scripts using API v2.
 * 
 *****************************************************************************/

/**
 * @param {HandlerApi} api
 */
export function OnTick(api) {
  // If this is not a clone, don't do anything
  if (!isClone()) return;
  // If the deadline has been exceeded, remove.
  if (mem.unclaimedDeadline && getTime() > mem.unclaimedDeadline) {
    destroySelf();
    return;
  }
  if (mem.unclaimedDeadline && isClaimed()) {
    // If we are claimed, so there is no deadline.
    delete mem.unclaimedDeadline;
  } else if (!mem.unclaimedDeadline && !isClaimed()) {
    // We are not claimed, so there should be deadline if there isn't one.
    mem.unclaimedDeadline = getTime() + 5;
  }
}
