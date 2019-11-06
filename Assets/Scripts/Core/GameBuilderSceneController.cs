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
using UnityEngine.SceneManagement;
using System.IO;
using GameBuilder;
#if USE_STEAMWORKS
using LapinerTools.Steam.Data;
#endif

#if TEXTURE_ADJUSTMENTS
using VacuumShaders.TextureExtensions;
#endif

#if USE_STEAMWORKS
// Just the subset of info we need.
public struct LoadableWorkshopItem
{
  public ulong steamId;
  public string displayName;
  public string installedLocalFolder;

  public LoadableWorkshopItem(WorkshopItem item)
  {
    this.steamId = item.SteamNative.m_nPublishedFileId.m_PublishedFileId;
    this.displayName = item.Name;
    this.installedLocalFolder = item.InstalledLocalFolder;
  }
}
#endif

public class GameBuilderSceneController : MonoBehaviour
{
  public event System.Action OnBeforeReloadMainScene;
  public event System.Action onBeforeQuitToSplash;

  GameResuming resuming;
  GlobalExceptionHandler bsod;

  static float LastLoadStart = -1f;

  void Awake()
  {
    if (LastLoadStart > 0f)
    {
      float dt = Time.realtimeSinceStartup - LastLoadStart;
      Util.Log($"Load to awake seconds: {dt}. LoadStart: {LastLoadStart} Now: {Time.realtimeSinceStartup}");
    }

    Util.FindIfNotSet(this, ref resuming);

    // DNE in splash screen
    bsod = FindObjectOfType<GlobalExceptionHandler>();
  }

  string GetDebugSaveFilePath()
  {
    return Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Internal", "demo-everything.voos");
  }

  public static string GetMinimalScenePath(bool twoPlayer)
  {
    return Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Public", "template-empty.voos");
  }

  public void RestartAndLoadMinimalScene(GameBuilderApplication.GameOptions gameOptions = new GameBuilderApplication.GameOptions())
  {
    RestartAndLoad(GetMinimalScenePath(false), gameOptions);
  }

  public void RestartAndLoadTutorial()
  {
    GameBuilderApplication.GameOptions gameOptions = new GameBuilderApplication.GameOptions();
    gameOptions.tutorialMode = true;
    RestartAndLoad(Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Public", "tutorial.voos"), gameOptions);
  }

  public void LoadMainSceneAsync(GameBuilderApplication.GameOptions gameOptions = new GameBuilderApplication.GameOptions())
  {
    bsod?.NotifySceneClosing();
    GameBuilderApplication.Get().SetGameOptionsDoNotUseExceptFromSceneController(gameOptions);
    OnBeforeReloadMainScene?.Invoke();
    LastLoadStart = Time.realtimeSinceStartup;
    SceneManager.LoadSceneAsync("main", LoadSceneMode.Single);
  }

  public void LoadSplashScreen()
  {
    bsod?.NotifySceneClosing();
    onBeforeQuitToSplash?.Invoke();
    LastLoadStart = Time.realtimeSinceStartup;
    SceneManager.LoadScene("splash", LoadSceneMode.Single);
  }

  public void RestartAndLoad(string filePath, GameBuilderApplication.GameOptions gameOptions = new GameBuilderApplication.GameOptions())
  {
    SaveLoadController.SaveGame save = SaveLoadController.ReadSaveGame(filePath);

    LoadMainSceneAsync(gameOptions);

    // TODO couldn't I put this in gameOptions instead..?
    // And then, we need to communicate the desire to load, so we use this SaveGameToLoad object.
    GameObject loadMe = new GameObject("LoadMePlease");
    GameObject.DontDestroyOnLoad(loadMe);
    var tag = loadMe.AddComponent<SaveGameToLoad>();
    tag.voosFilePath = filePath;
    tag.saved = save;
  }

  // DESTRUCTIVE TO thumbnail!
  byte[] MaybeGetThumbnailBytes(Texture2D thumbnail)
  {
#if TEXTURE_ADJUSTMENTS
    if (thumbnail == null)
    {
      return null;
    }
    else
    {
      int width = Mathf.Min(200, thumbnail.width);
      int height = Mathf.RoundToInt(thumbnail.height * 1f / thumbnail.width * width);
      thumbnail.ResizePro(width, height, false);
      return Util.TextureToZippedJpeg(thumbnail);
    }
#else
    return null;
#endif
  }

#if USE_STEAMWORKS
  public void LoadWorkshopItem(LoadableWorkshopItem item, GameBuilderApplication.PlayOptions playOptions, Texture2D thumbnail)
  {
    resuming.SetWorkshopItemForResuming(item.steamId);
    var options = new GameBuilderApplication.GameOptions
    {
      displayName = item.displayName,
      playOptions = playOptions,
      thumbnailZippedJpegBytes = MaybeGetThumbnailBytes(thumbnail),
      steamWorkShopFileId = item.steamId,
    };
    ulong itemId = item.steamId;
    this.RestartAndLoadBundleDirectory(item.installedLocalFolder, options);
  }
#endif

  public void RestartAndLoadLibraryBundle(GameBundleLibrary.Entry entry, GameBuilderApplication.PlayOptions playOptions)
  {
    if (!entry.IsAutosave())
    {
      resuming.SetBundleForResuming(entry.id);
    }
    var bundle = entry.bundle;
    var gameOptions = new GameBuilderApplication.GameOptions
    {
      bundleIdToLoad = entry.id,
      displayName = bundle.GetMetadata().name,
      playOptions = playOptions,
      thumbnailZippedJpegBytes = MaybeGetThumbnailBytes(bundle.GetThumbnail()),
    };

    this.LoadMainSceneAsync(gameOptions);
  }

  public void RestartAndLoadBundleDirectory(string directory, GameBuilderApplication.GameOptions gameOptions)
  {
    this.RestartAndLoad((new GameBundle(directory)).GetVoosPath(), gameOptions);
  }

  public void JoinMultiplayerGameByCode(string code)
  {
    StartCoroutine(JoinMultiplayerGameByCodeRoutine(code));
  }

  IEnumerator JoinMultiplayerGameByCodeRoutine(string code)
  {
    // Important that we disconnect first, since the join code may be for a
    // different region. So we need to disconnect and connect to that new
    // region.
    if (PhotonNetwork.connected && !PhotonNetwork.offlineMode)
    {
      PhotonNetwork.Disconnect();
      while (PhotonNetwork.connected)
      {
        yield return null;
      }
    }
    LoadMainSceneAsync(new GameBuilderApplication.GameOptions { playOptions = new GameBuilderApplication.PlayOptions { isMultiplayer = true }, joinCode = code });
  }

  private static string lastDebugLoadedFile = null;

  void Update()
  {
    // Some debug keys
    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
    {
#if USE_FILEBROWSER
      var filepaths = Crosstales.FB.FileBrowser.OpenFiles("Load scene", "", "voos");
      if (filepaths.Length > 0 && filepaths[0].Length > 0)
      {
        Util.Log("Loading " + filepaths[0]);
        this.RestartAndLoad(filepaths[0]);
        lastDebugLoadedFile = filepaths[0];
      }
#endif
    }

#if UNITY_EDITOR
    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
    {
      if (lastDebugLoadedFile != null)
      {
        this.RestartAndLoad(lastDebugLoadedFile);
      }
      else
      {
        this.RestartAndLoadMinimalScene();
      }
    }
#endif
  }

#if DEBUG
  [CommandTerminal.RegisterCommand(Help = "Starts a local multiplayer game", MinArgCount = 0, MaxArgCount = 0)]
  public static void CommandMpNew(CommandTerminal.CommandArg[] args)
  {
    GameBuilderApplication.GameOptions gameOptions = new GameBuilderApplication.GameOptions();
    gameOptions.playOptions.isMultiplayer = true;
    gameOptions.playOptions.startAsPublic = false;
    GameObject.FindObjectOfType<GameBuilderSceneController>().RestartAndLoadMinimalScene(gameOptions);
  }

#if USE_PUN
  [CommandTerminal.RegisterCommand(Help = "Join a local multiplayer game", MinArgCount = 0, MaxArgCount = 0)]
  public static void CommandMpJoin(CommandTerminal.CommandArg[] args)
  {
    SplashScreenController.debugShortcutToMpJoin = true;
    MultiplayerMenu.debugShortcutToMpJoin = true;
    GameObject.FindObjectOfType<GameBuilderSceneController>().LoadSplashScreen();
  }
#endif

#endif
}
