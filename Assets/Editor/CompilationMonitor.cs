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
using UnityEditor;
using UnityEditor.Compilation;
using SD = System.Diagnostics;
using System;

public class CompilationMonitor
{
  static Dictionary<string, SD.Stopwatch> watchByAsm = new Dictionary<string, SD.Stopwatch>();

  [InitializeOnLoadMethod]
  static void Init()
  {
    // totalRebuildWatch.Reset();
    CompilationPipeline.assemblyCompilationStarted += AssemblyCompilationStarted;
    CompilationPipeline.assemblyCompilationFinished += AssemblyCompilationFinished;
    AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
  }

  private static void OnBeforeAssemblyReload()
  {
    Debug.Log($"OnBeforeAssemblyReload @ {System.DateTime.Now}");
  }

  private static void OnAfterAssemblyReload()
  {
    Debug.Log($"OnAfterAssemblyReload @ {System.DateTime.Now}");
  }

  private static void AssemblyCompilationStarted(string asmPath)
  {
    Debug.Log($"Start compiling @ {System.DateTime.Now} ({asmPath})");
    Debug.Assert(!watchByAsm.ContainsKey(asmPath));
    watchByAsm[asmPath] = new SD.Stopwatch();
    watchByAsm[asmPath].Start();
  }

  private static void AssemblyCompilationFinished(string asmPath, CompilerMessage[] msgs)
  {
    var watch = watchByAsm[asmPath];
    watchByAsm.Remove(asmPath);
    Debug.Assert(watch != null);
    watch.Stop();
    Debug.Log($"{watch.ElapsedMilliseconds / 1000f}s to compile {asmPath}");
  }
}