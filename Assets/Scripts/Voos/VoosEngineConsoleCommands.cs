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

using CommandTerminal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class VoosEngine
{
  static VoosEngine consoleInstance = null;

  [RegisterCommand(Help = "List all actors. If an argument is given, it's a substring of display name to filter by.")]
  static void CommandActors(CommandArg[] args)
  {
    if (consoleInstance == null)
    {
      return;
    }

    string substring = args.Length > 0 ? args[0].ToString().ToLowerInvariant() : null;

    int i = 0;
    foreach (var actor in consoleInstance.EnumerateActors())
    {
      if (substring == null || (actor.GetDisplayName() ?? "").ToLowerInvariant().Contains(substring))
      {
        HeadlessTerminal.Log($"#{i}: {actor.GetDisplayName()} ({actor.GetName()})");
      }
      i++;
    }
  }

  [RegisterCommand(Help = "List all LOCK-WANTED actors")]
  static void CommandLocked(CommandArg[] args)
  {
    if (consoleInstance == null)
    {
      return;
    }

    int i = 0;
    foreach (var actor in consoleInstance.EnumerateActors())
    {
      if (actor.IsLockWantedLocally())
      {
        HeadlessTerminal.Log($"#{i}: {actor.GetDisplayName()} ({actor.GetName()})");
        i++;
      }
    }
  }

  [RegisterCommand(Help = "Record/report VoosUpdate perf stats for the next X seconds")]
  static void CommandVperf(CommandArg[] args)
  {
    try
    {
      float measureSecs = args.Length == 0 ? 3f : args[0].Float;
      HeadlessTerminal.Log($"Measuring VoosUpdate perf for the next {measureSecs} seconds..");
      consoleInstance.StartCoroutine(consoleInstance.VPerfRoutine(measureSecs));
    }
    catch (System.Exception e)
    {
      HeadlessTerminal.Log($"Failed: {e}");
    }
  }

  IEnumerator VPerfRoutine(float measuresSecs)
  {
    voosUpdateWatch.Reset();
    numVoosUpdates = 0;
    yield return new WaitForSecondsRealtime(measuresSecs);
    HeadlessTerminal.Log($"After {measuresSecs}s, mean ms/frame = {voosUpdateWatch.ElapsedMilliseconds * 1f / numVoosUpdates}");
  }

  [RegisterCommand(Help = "Toggle terrain collision messages to actors")]
  static void CommandTerrainCols(CommandArg[] args)
  {
    VoosEngine.TerrainCollisionsEnabled = !VoosEngine.TerrainCollisionsEnabled;
    HeadlessTerminal.Log($"new val: {VoosEngine.TerrainCollisionsEnabled}");
  }

  [RegisterCommand(Help = "Toggle whether or not we check memories after each message handler runs.")]
  static void CommandCheckMem(CommandArg[] args)
  {
    VoosEngine.MemCheckMode = args[0].String;
    HeadlessTerminal.Log($"VoosEngine.MemCheckMode: {VoosEngine.MemCheckMode}");
  }

  [RegisterCommand(Help = "Toggle profiling from JavaScript. Mostly here to check the performance effects of script profiling itself...")]
  static void CommandJSProf(CommandArg[] args)
  {
    VoosEngine.EnableProfilingFromScript = !VoosEngine.EnableProfilingFromScript;
    HeadlessTerminal.Log($"VoosEngine.EnableProfilingFromScript: {VoosEngine.EnableProfilingFromScript}");
  }

  [RegisterCommand(Help = "")]
  static void CommandPIStats(CommandArg[] args)
  {
    var engine = consoleInstance;

    System.Action doBinTest = () =>
    {
      byte[] buffer = new byte[10 * 1024 * 1024];
      var writer = new UnityEngine.Networking.NetworkWriter(buffer);
      engine.SerializePlayerInitPayloadV2(writer);

      byte[] bytesUsed = writer.ToArray();
      byte[] zipped = Util.GZip(bytesUsed);
      Util.Log($"{zipped.Length / 1024} kb zipped, {bytesUsed.Length / 1024} kb orig");
    };

    doBinTest();

    Util.Log($"Current stats:");
    FindObjectOfType<NetworkingController>().LogInitPlayerPayloadStats();
  }

}