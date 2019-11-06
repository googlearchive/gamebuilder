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
using System.IO;

public class OpenLogByHotKey : MonoBehaviour
{
  // Update is called once per frame
  void Update()
  {
    if (Input.GetKeyDown(KeyCode.L) && Util.IsControlOrCommandHeld() && Util.IsShiftHeld())
    {
#if UNITY_EDITOR_WIN
      string path = Path.Combine(System.Environment.GetEnvironmentVariable("AppData"), "..", "Local", "Unity", "Editor");
#elif UNITY_STANDALONE_WIN
      string path = Path.Combine(System.Environment.GetEnvironmentVariable("AppData"), "..", "LocalLow", Application.companyName, Application.productName);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
      string path = Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), "Library", "Logs", "Unity");
#endif
      Util.Log($"opening {path}");
      Application.OpenURL($"file://{path}");

    }
  }
}
