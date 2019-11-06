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

public class InputFieldOracle : MonoBehaviour
{
  int lastFocusedFrame = -1;

  GameObject prevSelected = null;
  TMPro.TMP_InputField fieldCache = null;

  void Update()
  {
    var selected = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;

    // Update fieldCache if needed.
    if (selected == null)
    {
      prevSelected = null;
      fieldCache = null;
    }
    else if (selected != prevSelected)
    {
      prevSelected = selected;
      fieldCache = selected.GetComponent<TMPro.TMP_InputField>();
    }

    if (fieldCache != null && fieldCache.isFocused)
    {
      lastFocusedFrame = Time.frameCount;
    }
  }

  public bool WasAnyFieldFocusedRecently()
  {
    return (fieldCache != null && fieldCache.isFocused) ||
    // Why the +2? Because if the user hits ESCAPE, isFocused will become
    // immediately false, but GetKeyDown(Esc) will be true. So, let's just pad
    // it out for 2 frames for that case.
    Time.frameCount < lastFocusedFrame + 2;
  }
}