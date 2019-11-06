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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorManager : MonoBehaviour
{

  [SerializeField] Texture2D[] cursorTextures;

  Vector2 defaultHotspot = Vector2.zero;
  Vector2 textHotspot = new Vector2(16, 16);

  public enum CursorType
  {
    Pointer = 0,
    HandOpen = 1,
    HandClosed = 2,
    Text = 3,
    Zoom = 4
  };

  CursorType cursorType = CursorType.Pointer;

  public void SetCursor(CursorType pointer)
  {
    if (cursorType == pointer) return;
    cursorType = pointer;
    Cursor.SetCursor(cursorTextures[(int)cursorType], cursorType == CursorType.Text ? textHotspot : defaultHotspot, CursorMode.Auto);
  }

  public void ReturnToDefault()
  {
    SetCursor(CursorType.Pointer);
  }
}
