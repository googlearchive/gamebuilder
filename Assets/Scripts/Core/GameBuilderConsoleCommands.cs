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
using GameBuilder;

// Misc commands that don't fit into any existing module.
public class GameBuilderConsoleCommands : MonoBehaviour
{
  static GameBuilderConsoleCommands main;

  GameBuilderLogHandler logHandler;
  BehaviorSystem behaviorSystem;
  VoosEngine voosEngine;
  UIMonkeyTester monkeyTester;
  WorkshopAssetSource workshop;

  void Awake()
  {
    Debug.Assert(main == null);
    Util.FindIfNotSet(this, ref logHandler);
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref monkeyTester);
    Util.FindIfNotSet(this, ref workshop);
    main = this;
  }

  public static void Log(string message)
  {
    Util.Log(message);
    CommandTerminal.HeadlessTerminal.Buffer.HandleLog(message, TerminalLogType.Message, null);
  }

  public static string JoinTailToPath(CommandArg[] args, int firstArg = 0)
  {
    if (args.Length > firstArg)
    {
      return String.Join(" ", args.Select(arg => arg.String));
    }
    else
    {
      return null;
    }
  }

  [RegisterCommand(Help = "List system info")]
  static void CommandSys(CommandArg[] args)
  {
    Log($"curr res: {Screen.currentResolution}, DPI: {Screen.dpi}, vsyncCount: {QualitySettings.vSyncCount}, fullscreenMode: {Screen.fullScreenMode}");
  }

  [RegisterCommand(Help = "Set networking log verbosity level (0, 1, 2)")]
  static void CommandNetV(CommandArg[] args)
  {
#if USE_PUN
    try
    {
      main.logHandler.showUnityLogsWarningsInConsole = true;
      if (args.Length > 0)
      {
        int level = args[0].Int;
        PhotonNetwork.logLevel = (PhotonLogLevel)level;
      }
      Log($"net log verbosity is {(int)PhotonNetwork.logLevel} ({PhotonNetwork.logLevel})");
    }
    catch (System.Exception e)
    {
      Log($"Failed with exception: {e}");
      Log("Usage: netv [0-2]");
    }
#endif
  }

  [RegisterCommand(Help = "Clear player prefs and mark developer mode")]
  static void CommandClearPrefs(CommandArg[] args)
  {
    PlayerPrefs.DeleteAll();
    PlayerPrefs.Save();
    Application.Quit();
  }

  [RegisterCommand(Help = "Print VOOS/behavior system info")]
  static void CommandVoosInfo(CommandArg[] args)
  {
    var bdb = main.behaviorSystem.SaveDatabase();
    Log($"{bdb.behaviorIds.Length} behaviors");
    Log($"{bdb.brainIds.Length} brains");
    Log($"{main.voosEngine.EnumerateActors().Count()} actors");

  }

  IEnumerator RpcFloodRoutine()
  {
    for (int i = 0; i < 20; i++)
    {
      var terrain = FindObjectOfType<TerrainManager>();
      for (int x = 0; x < 50; x++)
      {
        for (int y = 0; y < 10; y++)
        {
          terrain.SetCellValue(
            new TerrainManager.Cell { x = x, y = y, z = 0 },
            new TerrainManager.CellValue
            {
              blockType = TerrainManager.BlockShape.Full,
              direction = (TerrainManager.BlockDirection)(i % 4)
            });
        }
      }

      yield return null;
    }
  }

  [RegisterCommand(Help = "Cause a multiplayer disconnect")]
  static void CommandDisconnect(CommandArg[] args)
  {
    main.StartCoroutine(main.RpcFloodRoutine());
  }


  [RegisterCommand(Help = "UI monkey test")]
  static void CommandUIMonkeyTest(CommandArg[] args)
  {
    int iterations = 240;
    try
    {
      if (args.Length > 0)
      {
        iterations = args[0].Int;
      }
    }
    catch (System.Exception e)
    {
      Log($"Failed with exception: {e}");
      Log("Usage: uimonkeytest [iterations]");
    }
    main.StartCoroutine(UIMonkeyTest(iterations));
  }

  private static System.Collections.IEnumerator UIMonkeyTest(int iterations)
  {
    Log($"Start monkey test");
    yield return main.monkeyTester.MonkeyTestUI(iterations);
    Log($"End monkey test");
  }

  [RegisterCommand(Help = "")]
  static void CommandTerrainTest(CommandArg[] args)
  {
    var terrain = FindObjectOfType<TerrainSystem>();
    terrain.SetWorldDimensions(new Int3(100, 100, 100));
    for (int i = 0; i < 1000; i++)
    {
      terrain.SetCell(new Int3(UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100), UnityEngine.Random.Range(0, 100)),
      UnityEngine.Random.Range(0, 3),
      UnityEngine.Random.Range(0, 3),
      UnityEngine.Random.Range(0, 3));
    }
  }

  [RegisterCommand(Help = "Search for a string in all custom card JavaScript")]
  static void CommandFindJs(CommandArg[] args)
  {
    try
    {
      if (args.Length == 0)
      {
        Log($"Usage: findjs some word or phrase");
        return;
      }
      string q = String.Join(" ", args.Select(a => a.String)).ToLowerInvariant();
      Log($"Searching for \"{q}\"");
      foreach (var cardUri in main.behaviorSystem.GetCards())
      {
        if (!BehaviorSystem.IsEmbeddedBehaviorUri(cardUri)) continue;
        var data = main.behaviorSystem.GetBehaviorData(cardUri);
        if (data.javascript.ToLowerInvariant().Contains(q))
        {
          var cardMd = BehaviorCards.CardMetadata.GetMetaDataFor(data);

          foreach (var line in data.javascript.EnumerateNumberedLines())
          {
            if (line.line.ToLowerInvariant().Contains(q))
            {
              Log($"<color=#888888>{cardMd.title}:{line.number}</color> {line.line}");
            }
          }
        }
      }
    }
    catch (System.Exception e)
    {
      Log($"Failed with exception: {e}");
      Log($"Usage: findjs 'some word or phrase'");
    }
  }

  [RegisterCommand(Help = "For all selected objects, set renderable to the given URI")]
  static void CommandRenderUri(CommandArg[] args)
  {
    if (args.Length != 1)
    {
      Log($"Usage: renderuri steamworkshop:1234");
      return;
    }

    var edit = FindObjectOfType<EditMain>();
    if (edit == null)
    {
      Log($"Could not find EditMain..?");
      return;
    }

    foreach (var actor in edit.GetTargetActors())
    {
      if (actor != null && !actor.IsLockedByAnother())
      {
        actor.RequestOwnershipThen(() =>
        {
          Log($"Setting renderable for {actor.GetDebugName()}");
          actor.SetRenderableUri(args[0].String);
        });
      }
    }
  }

  [RegisterCommand(Help = "For all selected actors, set their debug flags to true")]
  static void CommandDebugActors(CommandArg[] args)
  {
    var edit = FindObjectOfType<EditMain>();
    if (edit == null)
    {
      Log($"Could not find EditMain..?");
      return;
    }

    foreach (var actor in edit.GetTargetActors())
    {
      if (actor != null)
      {
        actor.debug = true;
      }
    }
  }

  [RegisterCommand(Help = "Toggle terrain v2")]
  static void CommandTerrainV2(CommandArg[] args)
  {
    TerrainManager.TEST_V2 = !TerrainManager.TEST_V2;
    Log($"V2? {TerrainManager.TEST_V2}");
  }

  [RegisterCommand(Help = "Ground test")]
  static void CommandDig(CommandArg[] args)
  {
    var terrain = FindObjectOfType<TerrainManager>();
    for (int y = -1; y >= -20; y--)
    {
      terrain.SetCellValue(new TerrainManager.Cell { x = 0, y = y, z = 0 }, new TerrainManager.CellValue { blockType = TerrainManager.BlockShape.Full });
    }
  }

  [RegisterCommand(Help = "t2serial")]
  static void CommandT2Serial(CommandArg[] args)
  {
    var terrain = FindObjectOfType<TerrainManager>();
    var sd = new SD.Stopwatch();
    sd.Restart();
    byte[] data = terrain.SerializeTerrainV2();
    sd.Stop();
    Log($"V2 took {sd.ElapsedMilliseconds} to serialize");
  }

  [RegisterCommand(Help = "test terrain styles")]
  static void CommandTerrainStyles(CommandArg[] args)
  {
    var terrain = FindObjectOfType<TerrainManager>();
    terrain.DoStylesTest();
  }

  [RegisterCommand(Help = "toggle profiler")]
  static void CommandProf(CommandArg[] args)
  {
    var prof = FindObjectOfType<InGameProfiler>();
    prof.enabled = !prof.enabled;
    Log($"Prof enabled? {prof.enabled}");
  }

  [RegisterCommand(Help = "toggle terrain texture mip mapping (does not take effect until reload)")]
  static void CommandTerrainMips(CommandArg[] args)
  {
    TerrainSystem.UseMips = !TerrainSystem.UseMips;
    Log($"Use mips? {TerrainSystem.UseMips}");
  }

  [RegisterCommand(Help = "show or hide UI")]
  static void ToggleUI(CommandArg[] args)
  {
    UserMain userMain = FindObjectOfType<UserMain>();
    userMain.ToggleUI();
  }

  [RegisterCommand(Help = "show or hide HUD part of UI")]
  static void ToggleHUD(CommandArg[] args)
  {
    UserMain userMain = FindObjectOfType<UserMain>();
    userMain.ToggleHUD();
  }


  [RegisterCommand(Help = "show or hide avatar in edit mode")]
  static void ToggleEditAvatar(CommandArg[] args)
  {
    UserMain userMain = FindObjectOfType<UserMain>();
    userMain.ToggleEditAvatar();
  }

  [RegisterCommand(Help = "log update mismatches")]
  static void CommandSkipLog(CommandArg[] args)
  {
    var checker = FindObjectOfType<UpdateMismatchChecker>();
    checker.log = !checker.log;
    Log($"logging mismatches? {checker.log}");
  }

  [RegisterCommand(Help = "Empty terrain but for 1 block in center")]
  static void EmptyTerrain(CommandArg[] args)
  {
    var terrain = FindObjectOfType<TerrainManager>();
    terrain.EmptyAllButOne();
  }

  [RegisterCommand(Help = "Find/replace terrain. Ex to replace grass with stone: terrainreplace Grass Stone")]
  static void CommandTerrainReplace(CommandArg[] args)
  {
    var find = Util.ParseEnum<TerrainManager.BlockStyle>(args[0].String);
    var replace = Util.ParseEnum<TerrainManager.BlockStyle>(args[1].String);
    Log($"Replacing {find} with {replace} - YOU NEED TO SAVE AND RELOAD!");
    var terrain = FindObjectOfType<TerrainManager>();
    terrain.FindReplace(find, replace);
  }

  [RegisterCommand(Help = "Change culture info to Turkey and see what breaks...")]
  static void CommandTurkey(CommandArg[] args)
  {
    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
  }

  [RegisterCommand(Help = "Change culture info to German and see what breaks...")]
  static void CommandGerman(CommandArg[] args)
  {
    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
  }

  [RegisterCommand(Help = "Toggle motion blur")]
  static void CommandBlur(CommandArg[] args)
  {
    var stage = FindObjectOfType<GameBuilderStage>();
    var blur = stage.GetMainPostVolume().profile.GetSetting<UnityEngine.Rendering.PostProcessing.MotionBlur>();
    blur.enabled.value = !blur.enabled.value;
    Log($"OK motion blur enabled = {blur.enabled.value}");
  }

  [RegisterCommand(Help = "Set vsync and targetframerate")]
  static void CommandFrameLimits(CommandArg[] args)
  {
    if (args.Length == 2)
    {
      QualitySettings.vSyncCount = args[0].Int;
      Application.targetFrameRate = args[1].Int;
    }
    Log($"vSyncCount: {QualitySettings.vSyncCount}. targetFrameRate: {Application.targetFrameRate}");
  }

  [RegisterCommand(Help = "Set play mode only to (t)rue or (f)alse")]
  static void PlayModeOnly(CommandArg[] args)
  {

    if (args.Length == 0)
    {
      Log($"Usage: say t of ");
      return;
    }
    string q = String.Join(" ", args.Select(a => a.String)).ToLowerInvariant();
    bool playlock = q[0] == 't';

    UserMain userMain = FindObjectOfType<UserMain>();
    userMain.SetPlayModeOnly(playlock);
    Log($"Play lock set to {playlock}");
  }

  [RegisterCommand(Help = "Snap remote rigidbodies immediately upon UDP receive")]
  static void CommandRbSnap(CommandArg[] args)
  {
    GlobalUnreliableData.SnapRigidbodyOnRecv = !GlobalUnreliableData.SnapRigidbodyOnRecv;
    Log($"Snap? {GlobalUnreliableData.SnapRigidbodyOnRecv}");
  }

  static string lastVoosFilePathUsed = null;

  [RegisterCommand(Help = "Load a VOOS file directly. Optional: provide an absolute path as argument directly (spaces are OK).")]
  static void CommandLoad(CommandArg[] args)
  {
    var scenes = FindObjectOfType<GameBuilderSceneController>();
    if (args.Length > 0)
    {
      string path = "";
      for (int i = 0; i < args.Length; i++)
      {
        path += args[i].String + " ";
      }
      if (!File.Exists(path))
      {
        Log($"ERROR: VOOS file '{path}' does not exist.");
        return;
      }
      // Assume it's a path we can load directly
      Util.Log("Loading " + path);
      scenes.RestartAndLoad(path);
      lastVoosFilePathUsed = path;
    }
    else
    {
#if USE_FILEBROWSER
      string path = Crosstales.FB.FileBrowser.SaveFile("Save scene", "", "scene.voos", "voos");
      if (!path.IsNullOrEmpty())
      {
        Util.Log("Loading " + path);
        scenes.RestartAndLoad(path);
        lastVoosFilePathUsed = path;
      }
#endif
    }
  }


  [RegisterCommand(Help = "Save a VOOS file directly.")]
  static void CommandSave(CommandArg[] args)
  {
    var saver = FindObjectOfType<SaveLoadController>();
#if USE_FILEBROWSER
    var path = Crosstales.FB.FileBrowser.SaveFile("Save scene", "", "scene.voos", "voos");

    if (!path.IsNullOrEmpty())
    {
      saver.RequestSave(path, () =>
      {
        Util.Log($"OK saved to {path}");
        lastVoosFilePathUsed = path;
      });
    }
#endif
  }

  [RegisterCommand(Help = "Re-load the last VOOS file loaded with 'load' or saved with 'save'.")]
  static void CommandReload(CommandArg[] args)
  {
    var scenes = FindObjectOfType<GameBuilderSceneController>();
    if (lastVoosFilePathUsed != null)
    {
      scenes.RestartAndLoad(lastVoosFilePathUsed);
    }
    else
    {
      var net = FindObjectOfType<NetworkingController>();
      if (!net.lastLoadedBundleId.IsNullOrEmpty())
      {
        var bundles = FindObjectOfType<GameBuilder.GameBundleLibrary>();
        scenes.RestartAndLoadLibraryBundle(bundles.GetBundleEntry(net.lastLoadedBundleId),
        new GameBuilderApplication.PlayOptions());
      }
      else
      {
#if USE_FILEBROWSER
        string path = Crosstales.FB.FileBrowser.OpenSingleFile("Load scene", "", "voos");
        if (!path.IsNullOrEmpty())
        {
          Util.Log("Loading " + path);
          scenes.RestartAndLoad(path);
          lastVoosFilePathUsed = path;
        }
#endif
      }
    }
  }

  [RegisterCommand(Help = "TEST to make all selected actors use concave colliders.")]
  static void CommandConcave(CommandArg[] args)
  {
    var editMain = FindObjectOfType<EditMain>();
    foreach (var actor in editMain.GetTargetActors())
    {
      actor.SetUseConcaveCollider(true);
    }
  }

  [RegisterCommand(Help = "Clears the cache of models from the web (Poly)")]
  static void CommandClearPolyCache(CommandArg[] args)
  {
    PolyToolkit.PolyApi.ClearCache();
    Log($"OK cleared Poly cache");
  }

  [RegisterCommand(Help = "Enable speculative collision detection on selected actors")]
  static void CommandSpecCol(CommandArg[] args)
  {
    var editMain = FindObjectOfType<EditMain>();
    if (editMain == null)
    {
      Log($"You need to be in BUILD mode and have some actors selected.");
      return;
    }

    if (editMain.GetTargetActorsCount() == 0)
    {
      Log($"Please select some actors.");
      return;
    }

    foreach (var actor in editMain.GetTargetActors())
    {
      actor.SetSpeculativeColDet(true);
      Log($"Enabled speculative collision on {actor.name}");
    }
  }

  [RegisterCommand(Help = "Disable speculative collision detection on selected actors")]
  static void CommandNoSpecCol(CommandArg[] args)
  {
    var editMain = FindObjectOfType<EditMain>();
    foreach (var actor in editMain.GetTargetActors())
    {
      actor.SetSpeculativeColDet(false);
      Log($"Disabled speculative collision on {actor.name}");
    }
  }

  [RegisterCommand(Help = "")]
  static void CommandCacheReportFunc(CommandArg[] args)
  {
    V8InUnity.Services.CacheReportResult = !V8InUnity.Services.CacheReportResult;
    Log($"V8InUnity.Services.CacheReportResult: {V8InUnity.Services.CacheReportResult}");
  }

  [RegisterCommand(Help = "Toggles default cards on panels")]
  static void CommandDefaultCards(CommandArg[] args)
  {
    int val = PlayerPrefs.GetInt(BehaviorCards.AddDefaultCardPref, 1);
    val = 1 - val;
    PlayerPrefs.SetInt(BehaviorCards.AddDefaultCardPref, val);
    PlayerPrefs.Save();
    Log($"AddDefaultCards? {PlayerPrefs.GetInt(BehaviorCards.AddDefaultCardPref, 1)}");
  }

  [RegisterCommand(Help = "TODO")]
  static void CommandOverrideTerrainStyle(CommandArg[] args)
  {
    if (args.Length >= 1)
    {
      TerrainManager.OVERRIDE_STYLE = args[0].Int;
    }
    Log($"Current: {TerrainManager.OVERRIDE_STYLE}");
  }

  [RegisterCommand(Help = "Broadcast message without args too all objects")]
  static void CommandBroadcastMessage(CommandArg[] args)
  {
    FindObjectOfType<VoosEngine>().BroadcastMessageNoArgs(args[0].String);
  }

  static HashSet<string> EmptyPanelsToRemove = new HashSet<string> { "builtin:Action on Event Panel", "builtin:Movement Panel" };

  [RegisterCommand(Help = "TODO")]
  static void CommandRemoveEmptyPanels(CommandArg[] args)
  {
    var removedPanelNames = new HashSet<string>();
    var bce = FindObjectOfType<BehaviorCards>();
    foreach (VoosActor actor in main.voosEngine.EnumerateActors())
    {
      if (actor.GetCloneParentActor() != null) continue;
      var cards = bce.GetCardManager(actor);
      var toRemove = new List<CardPanel.IAssignedPanel>();

      foreach (var panel in cards.GetAssignedPanels())
      {
        if (panel.IsFake()) continue;
        if (!EmptyPanelsToRemove.Contains(panel.GetBehavior()?.GetBehaviorUri())) continue;

        bool someDeckHasCards = false;
        foreach (var deck in panel.GetDecks())
        {
          if (deck.GetAssignedCards().Any())
          {
            someDeckHasCards = true;
            break;
          }
        }

        if (!someDeckHasCards)
        {
          toRemove.Add(panel);
        }
      }

      foreach (var panel in toRemove)
      {
        removedPanelNames.Add(panel.GetBehavior().GetBehaviorUri());
        panel.Remove();
      }
    }

    foreach (var name in removedPanelNames)
    {
      Log($"Removed panels named {name}");
    }
  }

  [RegisterCommand(Help = "WARNING: DANGEROUS! Case-sensitive.")]
  static void DelActors(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"ERROR: No substring provided");
      return;
    }
    string q = args[0].String;
    var toDestroy = new HashSet<VoosActor>(
      from a in main.voosEngine.EnumerateActors()
      where a.GetDisplayName().Contains(q)
      select a);

    foreach (var actor in toDestroy)
    {
      main.voosEngine.DestroyActor(actor);
    }

    Log($"OK destroyed {toDestroy.Count} actors");
  }

  [RegisterCommand(Help = "WARNING: DANGEROUS! Case-sensitive.")]
  static void DelActorsNot(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"ERROR: No substring provided");
      return;
    }
    string q = args[0].String;
    var toDestroy = new HashSet<VoosActor>(
      from a in main.voosEngine.EnumerateActors()
      where !a.GetDisplayName().Contains(q)
      select a);

    foreach (var actor in toDestroy)
    {
      main.voosEngine.DestroyActor(actor);
    }

    Log($"OK destroyed {toDestroy.Count} actors");
  }


  [RegisterCommand(Help = "WARNING: DANGEROUS! Case-sensitive.")]
  static void DelBrainDead(CommandArg[] args)
  {
    bool rbOnly = args.Length > 0 && args[0].String == "rbonly";

    if (rbOnly)
    {
      Log($"Destroying rigidbodies only");
    }

    BehaviorCards cards = FindObjectOfType<BehaviorCards>();

    bool ShouldDestroy(VoosActor actor)
    {
      var rb = actor.GetComponent<Rigidbody>();
      if (rbOnly && rb.isKinematic) return false;

      if (actor.GetBrainName() == VoosEngine.DefaultBrainUid) return true;

      if (cards.GetCardManager(actor).GetAssignedPanels().Sum(p => p.GetDecks().Sum(d => d.GetAssignedCards().Count())) == 0) return true;

      return false;
    }

    var toDestroy = new HashSet<VoosActor>(
      from a in main.voosEngine.EnumerateActors()
      where ShouldDestroy(a)
      select a);

    foreach (var actor in toDestroy)
    {
      main.voosEngine.DestroyActor(actor);
    }

    Log($"OK destroyed {toDestroy.Count} actors");
  }

  [RegisterCommand(Help = "WARNING: DANGEROUS! Case-sensitive.")]
  static void BrainDeadNot(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"ERROR: No substring provided");
      return;
    }
    string q = args[0].String;
    var toDestroy = new HashSet<VoosActor>(
      from a in main.voosEngine.EnumerateActors()
      where !a.GetDisplayName().Contains(q)
      select a);

    foreach (var actor in toDestroy)
    {
      actor.SetBrainName(VoosEngine.DefaultBrainUid);
    }

    Log($"OK brain-deaded {toDestroy.Count} actors");
  }

  [RegisterCommand(Help = "TODO")]
  static void ConcaveAll(CommandArg[] args)
  {
    bool val = args.Length > 0 ? args[0].Bool : true;
    foreach (var actor in main.voosEngine.EnumerateActors())
    {
      actor.SetUseConcaveCollider(val);
    }
  }

  [RegisterCommand(Help = "TODO")]
  static void CommandTEMPLATE(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"TODO HELP");
      return;
    }
    Log($"TODO");
  }

#if UNITY_EDITOR
  [RegisterCommand(Help = "INTERNAL TESTING ONLY")]
  static void CommandNukeBehaviors(CommandArg[] args)
  {
    var bs = FindObjectOfType<BehaviorSystem>();
    foreach (var id in bs.GetEmbeddedBehaviorIds().ToList())
    {
      bs.DeleteBehavior(id);
    }

  }
#endif
}