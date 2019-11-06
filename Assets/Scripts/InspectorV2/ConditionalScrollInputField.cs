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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// This is intended to address the input field interfering with its parent scroll rect
// when the mouse is over it, even if the input field is single line or has no space to
// scroll to.
public class ConditionalScrollInputField : TMPro.TMP_InputField
{

  public override void OnScroll(PointerEventData ev)
  {
    // For now, just prevent scrolling completely (bubble to parent).
    // User can still "scroll" up and down by navigating with the keyboard.
    // Eventually, it would be nice to scroll only if we haven't hit the top/bottom.
    ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, ev, ExecuteEvents.scrollHandler);
  }

}
