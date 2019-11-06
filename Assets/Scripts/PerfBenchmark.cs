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
using System.Linq;
using System.IO;
using SD = System.Diagnostics;

// Misc commands that don't fit into any existing module.
public class PerfBenchmark : MonoBehaviour
{
  static string[] BenchmarkVoosFiles = new string[] {
      "old-minimal-scene.voos",
      "old-tutorial.voos",
      "pug-bench-4ms.voos",
      "pug-bench-18ms.voos",
    };

  static BenchmarkState CurrState = null;
  static int CurrSceneIndex = 0;
  static float SceneLoadStartTime = 0;

  [RegisterCommand(Help = "Launch benchmark mode")]
  public static void CommandBenchmark(CommandArg[] args)
  {
    string note = "";
    if (args != null && args.Length >= 1)
    {
      note = string.Join(" ", from a in args select a.String);
    }

    var state = new BenchmarkState(note);
    CurrState = state;
    CurrSceneIndex = 0;
    GameBuilderSceneController scenes = FindObjectOfType<GameBuilderSceneController>();
    SceneLoadStartTime = Time.realtimeSinceStartup;
    scenes.RestartAndLoad(System.IO.Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Internal", BenchmarkVoosFiles[0]));
  }

  VoosEngine voosEngine;
  TerrainManager terrain;
  DynamicPopup popups;
  AutoSaveController autosaves;
  NetworkingController networking;

  static float TicksToMillis(long ticks)
  {
    return ticks * 1f / SD.Stopwatch.Frequency * 1e3f;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref terrain);
    Util.FindIfNotSet(this, ref autosaves);
    Util.FindIfNotSet(this, ref networking);
  }

  void CheckGlobals()
  {
    if (!Application.isFocused)
    {
      throw new System.Exception("Application lost focus? Can't proceed with benchmark.");
    }

    if (QualitySettings.vSyncCount != 0)
    {
      throw new System.Exception("vSyncCount is not 0! Can't proceed with benchmark.");
    }

    if (Application.targetFrameRate != -1)
    {
      throw new System.Exception("targetFrameRate != -1. Cannot benchmark.");
    }
  }

  IEnumerator BenchmarkRoutine()
  {
    float loadToStart = Time.realtimeSinceStartup - SceneLoadStartTime;

    UnityEngine.Profiling.Profiler.enabled = false;
    SaveLoadController.SuppressLegacyWarning = true;

    // Let the framerate run free..
    QualitySettings.vSyncCount = 0;
    Application.targetFrameRate = -1;

    // Hitches..
    autosaves.SetPaused(true);

    CheckGlobals();

    while (!voosEngine.GetIsRunning())
    {
      yield return null;
    }

    float loadToVoos = Time.realtimeSinceStartup - SceneLoadStartTime;

    // Make sure we want for all terrain chunks too..
    while (!terrain.IsSettledForPerfMeasurement())
    {
      yield return new WaitForSecondsRealtime(0.5f);
    }

    float loadToTerrain = Time.realtimeSinceStartup - SceneLoadStartTime;

    // Let it settle down a bit..
    yield return new WaitForSecondsRealtime(2f);

    CheckGlobals();

    // Now collect some frame times.
    const int numSamples = 200;
    long[] sampleTicks = new long[numSamples];
    float[] voosUpdateSampleMillis = new float[numSamples];

    SD.Stopwatch watch = new SD.Stopwatch();
    // Don't run until we start our first frame.
    watch.Stop();

    int currSample = 0;

    voosEngine.onVoosUpdateTiming += millis =>
    {
      if (watch.IsRunning)
      {
        voosUpdateSampleMillis[currSample] = millis;
      }
    };

    while (true)
    {
      CheckGlobals();
      yield return new WaitForEndOfFrame();
      if (watch.IsRunning)
      {
        // Just finished recording a sample!
        watch.Stop();
        sampleTicks[currSample] = watch.ElapsedTicks;
        currSample++;
        if (currSample >= numSamples)
        {
          // All done!
          break;
        }
      }
      // Start next sample.
      watch.Restart();
    }

    // Sanity check voos.
    foreach (float voosMs in voosUpdateSampleMillis) Debug.Assert(voosMs > 0);

    float averageMs = TicksToMillis(sampleTicks.Sum()) / numSamples;

    float averageVoosMs = voosUpdateSampleMillis.Sum() / numSamples;

    // Update state file and kick off the next one..
    BenchmarkState state = CurrState;

    Array.Sort(sampleTicks);
    var res = new BenchmarkState.SceneResult
    {
      voosFile = BenchmarkVoosFiles[CurrSceneIndex],
      avgFrameMs = averageMs,
      avgVoosUpdateMs = averageVoosMs,
      percentile90 = TicksToMillis(sampleTicks.AtFractionalPosition(0.90f)),
      percentile95 = TicksToMillis(sampleTicks.AtFractionalPosition(0.95f)),
      percentile99 = TicksToMillis(sampleTicks.AtFractionalPosition(0.99f)),
      loadToStart = loadToStart,
      loadToTerrain = loadToTerrain,
      loadToVoos = loadToVoos,
      actorBytes = networking.GetVoosInitBytes().Length,
      terrainBytes = terrain.SerializeTerrainV2().Length
    };
    state.results = state.results ?? new BenchmarkState.SceneResult[0];
    state.results = state.results.ExpensiveWith(res);
    CurrSceneIndex++;

    Util.Log($"OK finished benchmark for scene {res.voosFile}. avgFrameMs={res.avgFrameMs} avgVoosUpdateMs={res.avgVoosUpdateMs}");

    CheckGlobals();

    if (CurrSceneIndex >= BenchmarkVoosFiles.Length)
    {
      string outDir = Path.Combine((Application.isEditor ? "editor-" : "release-") + "benchmark-results", System.Net.Dns.GetHostName());
      if (!Directory.Exists(outDir))
      {
        Directory.CreateDirectory(outDir);
      }
      // We're done! Save to file.
      string outPath = Path.Combine(outDir, System.DateTime.Now.ToString("yyyyMMddTHHmm") + ".json");
      File.WriteAllText(outPath, JsonUtility.ToJson(CurrState, true));
      CurrState = null;

      FindObjectOfType<GameBuilderSceneController>().LoadSplashScreen();
    }
    else
    {
      GameBuilderSceneController scenes = FindObjectOfType<GameBuilderSceneController>();
      SceneLoadStartTime = Time.realtimeSinceStartup;
      scenes.RestartAndLoad(System.IO.Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Internal", BenchmarkVoosFiles[CurrSceneIndex]));
    }
  }

  [System.Serializable]
  class BenchmarkState
  {
    [System.Serializable]
    public class SceneResult
    {
      public string voosFile;
      public float avgFrameMs;
      public float avgVoosUpdateMs;

      // frame time percentiles, milliseconds
      public float percentile90;
      public float percentile95;
      public float percentile99;

      public float loadToStart;
      public float loadToVoos;
      public float loadToTerrain;

      public int actorBytes;
      public int terrainBytes;
    }

    public string startTimestamp;
    public string note;
    public string builtCommit;
    public string host;
    public int screenWidth;
    public int screenHeight;
    public string fullscreenMode;
    public int qualityLevel;
    public string cpuInfo;
    public string gpuInfo;
    public bool terrainMips;
    public string actorMemCheckMode;

    public SceneResult[] results;

    public BenchmarkState(string note)
    {
      this.startTimestamp = System.DateTime.Now.ToString();
      this.note = note;
      this.builtCommit = GameBuilderApplication.ReadBuildCommit();
      this.host = System.Net.Dns.GetHostName();
      this.screenWidth = Screen.width;
      this.screenHeight = Screen.height;
      this.fullscreenMode = Screen.fullScreenMode.ToString();
      this.qualityLevel = QualitySettings.GetQualityLevel();
      this.terrainMips = TerrainSystem.UseMips;
      this.actorMemCheckMode = VoosEngine.MemCheckMode;
      this.cpuInfo = SystemInfo.processorType;
      this.gpuInfo = SystemInfo.graphicsDeviceName;
    }
  }

  void Start()
  {
    if (CurrState != null)
    {
      Util.Log($"Benchmark mode detected! Started: {CurrState.startTimestamp}");
      StartCoroutine(BenchmarkRoutine());
    }
  }
}