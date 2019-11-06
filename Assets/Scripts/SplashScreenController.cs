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
#if USE_STEAMWORKS
using LapinerTools.Steam.Data;
#endif
using GameBuilder;

public class SplashScreenController : Photon.PunBehaviour, GameResuming.ResumeOptionHandler
{
  private static readonly bool ENABLE_FEATURED_GAMES = false;

  GameBuilderSceneController scenes;
  GameBundleLibrary library;
  GameResuming resuming;

  [SerializeField] TemplateSelectorMenu templateSelectorMenu;
  [SerializeField] UnityEngine.Audio.AudioMixer audioMixer;
  [SerializeField] UnityEngine.UI.Button newButton;
  [SerializeField] UnityEngine.UI.Button gameLibraryButton;
  [SerializeField] UnityEngine.UI.Button creditsCloseButton;
  [SerializeField] UnityEngine.UI.Button multiplayerButton;
  [SerializeField] UnityEngine.UI.Button moreGamesButton;
  [SerializeField] UnityEngine.UI.Button creditsButton;
  [SerializeField] GameThumbnail resumeButton;
  [SerializeField] UnityEngine.UI.Button quitButton;
  [SerializeField] MenuPanelManager menuPanelManager;
  [SerializeField] FeaturedWorkshopItems featuredWorkshopItems;
  [SerializeField] GameObject featuredCanvasObject;
  [SerializeField] GameFeatured[] featuredGames;
  [SerializeField] TMPro.TextMeshProUGUI featuredMessage;
  [SerializeField] GameObject creditsObject;
  DynamicPopup popups;
  LoadingScreen loadingScreen;
  protected GameBuilderSceneController sceneController;

  const string FEATURED_MSG = "Featured Creations";
  // const string FEATURED_LOADING = "Loading...";
  const string LIBRARY_HEADER = "CHOOSE A GAME";
  const int FEATURED_COUNT = 3;

  // If this is not null, we are featuring LOCAL games, not steam workshop games.
  static readonly LocalSampleGames.GameInfo[] LOCAL_FEATURED_GAMES = null; // LocalSampleGames.LOCAL_SAMPLE_GAMES;

  // HACK HACK (for debug)
  public static bool debugShortcutToMpJoin = false;

  void Awake()
  {
    Util.UpgradeUserDataDir();

    Util.FindIfNotSet(this, ref scenes);
    Util.FindIfNotSet(this, ref library);
    Util.FindIfNotSet(this, ref resuming);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref sceneController);

#if USE_STEAMWORKS
    foreach (GameFeatured featured in featuredGames)
    {
      featured.Setup();
    }
#endif

    newButton.onClick.AddListener(templateSelectorMenu.Show);
    creditsButton.onClick.AddListener(() => creditsObject.SetActive(true));
    creditsCloseButton.onClick.AddListener(() => creditsObject.SetActive(false));
    gameLibraryButton.onClick.AddListener(menuPanelManager.OpenGameLibrary);
    menuPanelManager.Setup();
    quitButton.onClick.AddListener(Application.Quit);

#if USE_PUN
    multiplayerButton.onClick.AddListener(menuPanelManager.OpenMultiplayerMenu);
#else
    multiplayerButton.gameObject.SetActive(false);
#endif

    moreGamesButton.onClick.AddListener(MoreGames);

    // featuredMessage.text = FEATURED_LOADING;
    featuredMessage.text = FEATURED_MSG;

    menuPanelManager.SetLibraryHeaderText(LIBRARY_HEADER);
    templateSelectorMenu.Setup();

    // To help diagnose things
    Util.Log($"Culture info: {System.Threading.Thread.CurrentThread.CurrentCulture}");
    Util.Log($"GB build commit: {GameBuilderApplication.ReadBuildCommit()}");
  }

  private void MoreGames()
  {
    menuPanelManager.OpenSteamWorkshop();
  }

  public IEnumerator FrameDelay(System.Action action)
  {
    yield return null;
    action.Invoke();
  }

  void LaunchTutorial()
  {
    loadingScreen.ShowAndDo(() =>
    {
      scenes.RestartAndLoadTutorial();
    });
  }

  void Start()
  {
    float sfxvolume = PlayerPrefs.GetFloat("sfxVolume", .5f);
    float musicvolume = PlayerPrefs.GetFloat("musicVolume", 1f);

    if (musicvolume != 0)
    {
      audioMixer.SetFloat("musicVolume", Mathf.Log10(musicvolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("musicVolume", -80);
    }

    if (sfxvolume != 0)
    {
      audioMixer.SetFloat("sfxVolume", Mathf.Log10(sfxvolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("sfxVolume", -80);
    }

    SetupFeaturedGames();

    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;

    if (debugShortcutToMpJoin)
    {
      menuPanelManager.OpenMultiplayerMenu();
      debugShortcutToMpJoin = false;
    }

    resuming.QueryResumeInfo(this);
  }

  void SetupFeaturedGames()
  {
#if USE_STEAMWORKS
    if (!ENABLE_FEATURED_GAMES)
    {
      featuredCanvasObject.SetActive(false);
      return;
    }
    if (LOCAL_FEATURED_GAMES != null)
    {
      // featuredMessage.text = FEATURED_MSG;
      for (int i = 0; i < Mathf.Min(LOCAL_FEATURED_GAMES.Length, FEATURED_COUNT); i++)
      {
        featuredGames[i].SetLocalVoosFile(
          LOCAL_FEATURED_GAMES[i].GetVoosFilePath(),
          LOCAL_FEATURED_GAMES[i].GetThumbnailFilePath(),
          LOCAL_FEATURED_GAMES[i].title,
          ""
        // LOCAL_FEATURED_GAMES[i].description
        );
        featuredGames[i].gameObject.SetActive(true);
      }

      featuredCanvasObject.SetActive(true);
    }
    else
    {
      StartCoroutine(FeaturedGameCheck());
    }
#endif
  }
#if USE_STEAMWORKS
  // TODO move this into GamesFeatured?
  IEnumerator FeaturedGameCheck()
  {
    // featuredMessage.text = FEATURED_LOADING;

    while (featuredWorkshopItems.IsWaitingOnQuery())
    {
      yield return null;
    }

    yield return null;

    List<WorkshopItem> items = new List<WorkshopItem>(featuredWorkshopItems.GetItems());
    if (items.Count > FEATURED_COUNT)
    {
      Util.LogError($"GUI cannot accomodate more featured items. Only showing the first {FEATURED_COUNT}");
    }
    for (int i = 0; i < FEATURED_COUNT; i++)
    {
      featuredGames[i].SetWorkshopItem(items[i]);
      featuredGames[i].gameObject.SetActive(true);
    }
    featuredMessage.text = FEATURED_MSG;

    featuredCanvasObject.SetActive(true);

  }
#endif

  void Update()
  {
    if (Input.GetKeyDown(KeyCode.F8) && Input.GetKey(KeyCode.LeftControl))
    {
      PerfBenchmark.CommandBenchmark(null);
    }

#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.F9) && Input.GetKey(KeyCode.LeftControl))
    {
      var gameOpts = new GameBuilderApplication.GameOptions
      {
        playOptions = new GameBuilderApplication.PlayOptions
        {
          isMultiplayer = true,
          startAsPublic = false,
          startInBuildMode = true
        }
      };
      string path = System.IO.Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Internal", "template-small.voos");
      loadingScreen.ShowAndDo(() => sceneController.RestartAndLoad(path, gameOpts));
    }
#endif

#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.F10) && Input.GetKey(KeyCode.LeftControl))
    {
      sceneController.JoinMultiplayerGameByCode($"1-dev-{System.Net.Dns.GetHostName().ToLowerInvariant()}");
    }
#endif

    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.N))
    {
      sceneController.RestartAndLoadMinimalScene(new GameBuilderApplication.GameOptions());
    }

    if (Input.GetButtonDown("Cancel"))
    {
      if (creditsObject.activeSelf)
      {
        creditsObject.SetActive(false);
      }
      else if (templateSelectorMenu.IsOpen())
      {
        templateSelectorMenu.Close();
      }
      else
      {
        menuPanelManager.Back();
      }
    }

    AudioListener.volume = Application.isFocused ? 1 : 0;
  }
#if USE_STEAMWORKS
  void GameResuming.ResumeOptionHandler.HandleWorkshopItem(WorkshopItem item)
  {
    // tutorialButton.gameObject.SetActive(false);
    resumeButton.gameObject.SetActive(true);
    resumeButton.SetThumbnailUrl(item.PreviewImageURL);
    resumeButton.SetName(item.Name);
    resumeButton.OnClick = () =>
    {
      popups.AskHowToPlay(playOpts =>
      {
        loadingScreen.ShowAndDo(() =>
        {
          scenes.LoadWorkshopItem(new LoadableWorkshopItem(item), playOpts, null);
        });
      });
    };
  }
#endif
  void GameResuming.ResumeOptionHandler.HandleBundleId(string bundleId)
  {
    if (!library.BundleExists(bundleId))
    {
      return;
    }
    var entry = library.GetBundleEntry(bundleId);
    // tutorialButton.gameObject.SetActive(false);
    resumeButton.gameObject.SetActive(true);
    resumeButton.SetThumbnail(entry.bundle.GetThumbnail());
    resumeButton.SetName(entry.bundle.GetMetadata().name);
    resumeButton.OnClick = () =>
    {
      popups.AskHowToPlay(playOpts =>
      {
        loadingScreen.ShowAndDo(() =>
        {
          scenes.RestartAndLoadLibraryBundle(entry, playOpts);
        });
      });
    };
  }

  void GameResuming.ResumeOptionHandler.HandleJoinCode(string joinCode)
  {
    // tutorialButton.gameObject.SetActive(false);
    resumeButton.gameObject.SetActive(true);
    resumeButton.SetThumbnailUrl(null);
    resumeButton.SetName($"Re-join {joinCode}");
    resumeButton.OnClick = () =>
    {
      loadingScreen.ShowAndDo(() =>
      {
        sceneController.JoinMultiplayerGameByCode(joinCode);
      });
    };

  }
}
