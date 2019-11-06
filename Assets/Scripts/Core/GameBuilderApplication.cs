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

using UnityEngine;
using System.IO;

// The one static singleton I'll allow...
// Any cross-scene state can be stored here.
public class GameBuilderApplication
{
  // Make sure this matches what Unity shows in the startup dialog
  public enum Quality : int
  {
    High = 0,
    Medium = 1,
    Low = 2
  }

  [System.Serializable]
  class DisplaySettings
  {
    public int width;
    public int height;
    public int refreshRate;
    public bool fullscreen;

    public void Apply()
    {
      Screen.SetResolution(
        width,
        height,
        fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed,
        refreshRate);
    }

    public void Capture()
    {
      this.width = Screen.width;
      this.height = Screen.height;
      this.fullscreen = Screen.fullScreen;
      this.refreshRate = Screen.currentResolution.refreshRate;
    }
  }

  static void RestoreDisplaySettings()
  {
    string json = PlayerPrefs.GetString("GBDisplaySettings", "");
    if (json.IsNullOrEmpty())
    {
      Util.Log($"No display settings to restore");
      return;
    }
    var settings = JsonUtility.FromJson<DisplaySettings>(json);
    settings.Apply();
    Util.Log($"Restored display settings: " + JsonUtility.ToJson(settings));
  }

  public static void SaveDisplaySettings()
  {
    var settings = new DisplaySettings();
    settings.Capture();
    string json = JsonUtility.ToJson(settings);
    PlayerPrefs.SetString("GBDisplaySettings", json);
    PlayerPrefs.Save();
  }

  // Bleh sorry.
  public static event System.Action onQualityLevelChanged;

  static GameBuilderApplication()
  {
    UpdateVsync();
    RestoreDisplaySettings();
    Application.quitting += SaveDisplaySettings;
  }

  static void UpdateVsync()
  {
    if (GetQuality() == Quality.Low)
    {
      QualitySettings.vSyncCount = 0;
    }
    else
    {
      QualitySettings.vSyncCount = 1;
    }
  }

  public static void NotifyQualityLevelChanged()
  {
    onQualityLevelChanged?.Invoke();
    UpdateVsync();
  }

  public static Quality GetQuality()
  {
    return (Quality)QualitySettings.GetQualityLevel();
  }

  // TODO probably better to define an interface for "settings" than these "god
  // structs."

  // More like..HOW to play, rather than WHAT to play.
  public struct PlayOptions
  {
    public bool isMultiplayer;

    // Only relevant if isMultiplayer
    public bool startAsPublic;

    public bool startInBuildMode;
  }

  public class MutableState
  {
    public string lastManuallySavedBundleId = null;
  }

  public struct GameOptions
  {
    public string displayName;

    public string joinCode;

    public PlayOptions playOptions;

    public byte[] thumbnailZippedJpegBytes;

    // The workshop item we're loading. 0 if not a workshop item.
    public ulong steamWorkShopFileId;

    // Is null if we're not loading a local bundle.
    public string bundleIdToLoad;

    public MutableState mutable;

    public bool tutorialMode;

    public bool isStandaloneExport;

    public string workshopAssetCacheDir;
  }

  private GameOptions options;

  private static GameBuilderApplication Instance;

  public GameBuilderApplication()
  {
    this.options.mutable = new MutableState();
  }

  public void SetGameOptionsDoNotUseExceptFromSceneController(GameOptions options)
  {
    this.options = options;
    this.options.mutable = new MutableState();
  }

  public static GameBuilderApplication Get()
  {
    if (Instance == null)
    {
      Instance = new GameBuilderApplication();
    }
    return Instance;
  }

  // Get a copy of the struct.
  public static GameOptions CurrentGameOptions { get { return Get().options; } }

  public static bool IsRecoveryMode
  {
    get
    {
      return AutoSaveController.IsAutosave(CurrentGameOptions.bundleIdToLoad)
      // Once the user saves the game, consider it recovered and back to normal.
      && CurrentGameOptions.mutable.lastManuallySavedBundleId == null;
    }
  }

  public static bool IsTutorialMode
  {
    get
    {
      return CurrentGameOptions.tutorialMode;
    }
  }

  public static bool IsStandaloneExport { get { return CurrentGameOptions.isStandaloneExport; } }

  // By "active" we mean we can upload it and save to it.
  public static string ActiveBundleId
  {
    get
    {
      var opts = CurrentGameOptions;

      if (IsRecoveryMode)
      {
        return null;
      }
      if (opts.mutable.lastManuallySavedBundleId != null)
      {
        return opts.mutable.lastManuallySavedBundleId;
      }
      else if (opts.bundleIdToLoad != null && !AutoSaveController.IsAutosave(opts.bundleIdToLoad))
      {
        return opts.bundleIdToLoad;
      }
      else
      {
        return null;
      }
    }
  }

  static string BuildCommitCache = null;

  public static string ReadBuildCommit()
  {
    if (BuildCommitCache != null)
    {
      return BuildCommitCache;
    }

    if (File.Exists("built-commit"))
    {
      BuildCommitCache = File.ReadAllText("built-commit").Substring(0, 9);
      return BuildCommitCache;
    }
    else
    {
      return null;
    }
  }

  static string UploadInfoCache = null;

  public static string ReadUploadInfo()
  {
    if (UploadInfoCache != null)
    {
      return UploadInfoCache;
    }

    if (File.Exists("upload-info.txt"))
    {
      UploadInfoCache = File.ReadAllText("upload-info.txt").Trim();
      return UploadInfoCache;
    }
    else
    {
      return null;
    }
  }
}
