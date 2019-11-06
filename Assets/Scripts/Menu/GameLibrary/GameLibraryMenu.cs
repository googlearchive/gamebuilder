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
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif

using GameBuilder;
using System.Linq;
using System;

public class GameLibraryMenu : ThumbnailMenu
{

  [SerializeField] UnityEngine.UI.Button openSaveFolder;
  [SerializeField] UnityEngine.UI.Button openSteamWorkshop;
  [SerializeField] UnityEngine.UI.Button openSteamWorkshopSecondary;
  [SerializeField] UnityEngine.UI.ScrollRect libraryScrollRect;
  [SerializeField] RectTransform libraryViewport;
  [SerializeField] RectTransform libraryContainer;

  [SerializeField] TMPro.TextMeshProUGUI headerText;

  [SerializeField] UnityEngine.UI.Toggle showWorkshopGamesToggle;
  [SerializeField] UnityEngine.UI.Toggle showMyGamesToggle;
  [SerializeField] UnityEngine.UI.Toggle showAutosavesToggle;
  [SerializeField] GameObject gameThumbnailPrefab;
  [SerializeField] GameObject gameDetailPrefab;

  [SerializeField] Texture2D newGameTexture;
  [SerializeField] Texture2D tutorialGameTexture;
  [SerializeField] TemplateSelectorMenu templateSelectorMenu;

  GameDetail gameDetail;

  DynamicPopup popups;

  WorkshopAssetSource workshopSource;

  GameThumbnail newThumbnail;
  GameThumbnail tutorialThumbnail;

  public System.Action openWorkshop;

  LocalSort localSort;
  LoadingScreen loadingScreen;

  public enum LocalSort
  {
    Name,
    Recent
  };

  public static string SAVED = "Saved";
  public static string WORKSHOP = "Workshop";
  public static string AUTOSAVES = "Autosaves";

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref workshopSource);
  }

  public override void Setup()
  {
    base.Setup();
    openSaveFolder.onClick.AddListener(OpenSaveFolder);
#if USE_STEAMWORKS
    openSteamWorkshop.onClick.AddListener(() => openWorkshop?.Invoke());
    openSteamWorkshopSecondary.onClick.AddListener(() => openWorkshop?.Invoke());
#else
    openSteamWorkshop.gameObject.SetActive(false);
    openSteamWorkshopSecondary.gameObject.SetActive(false);
#endif
    AddCategory(null);
    AddCategory(SAVED);
    AddCategory(WORKSHOP);
    AddCategory(AUTOSAVES);

    SetCategoryEnabled(SAVED, showMyGamesToggle.isOn);
#if USE_STEAMWORKS
   SetCategoryEnabled(WORKSHOP, showWorkshopGamesToggle.isOn);
    SetCategoryLoading(WORKSHOP, true);
#else
    SetCategoryEnabled(WORKSHOP, false);
    showWorkshopGamesToggle.gameObject.SetActive(false);
#endif

    SetCategoryEnabled(AUTOSAVES, showAutosavesToggle.isOn);

    searchField.onValueChanged.AddListener((searchString) => SetSearchString(searchString));
    searchClear.onClick.AddListener(() => searchField.text = "");

    showWorkshopGamesToggle.onValueChanged.AddListener(
      (on) => SetCategoryEnabled(WORKSHOP, showWorkshopGamesToggle.isOn));
    showMyGamesToggle.onValueChanged.AddListener(
      (on) => SetCategoryEnabled(SAVED, showMyGamesToggle.isOn));
    showAutosavesToggle.onValueChanged.AddListener(
      (on) => SetCategoryEnabled(AUTOSAVES, showAutosavesToggle.isOn));

    SetOpenEvent(CreateThumbnails);
    templateSelectorMenu.Setup();
    templateSelectorMenu.Close();

    gameDetail = Instantiate(gameDetailPrefab).GetComponent<GameDetail>();

    UpdateSort();
  }

  public void SetHeaderText(string newtext)
  {
    headerText.text = newtext;
  }

  public void Refresh()
  {
    ClearThumbnails();
    SetCategoryLoading(WORKSHOP, true);
    CreateThumbnails();
  }

  public override void Open()
  {
    sortText.text = localSort.ToString();
    base.Open();
  }

  protected override void Update()
  {
    base.Update();
    noResultsFeedback.SetActive(GetVisibleCount() < 3);
  }

  protected void CreateThumbnails()
  {
    AddNewThumbnail();
    AddTutorialThumbnail();
    StartCoroutine(AddSampleGameThumbnails());
    StartCoroutine(CreateMyLibraryThumbnails());

#if USE_STEAMWORKS
      CreateSteamSubscribedThumbnails();
#endif
  }

  void OpenSaveFolder()
  {
    Application.OpenURL(gameBundleLibrary.GetLibraryAbsolutePath());
  }

  protected override bool DoesThumbnailMatchSearch(ThumbnailItem thumbnail)
  {
    return thumbnail == newThumbnail || thumbnail == tutorialThumbnail || base.DoesThumbnailMatchSearch(thumbnail);
  }

  void AddNewThumbnail()
  {
    newThumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
    newThumbnail.SetThumbnail(newGameTexture);
    newThumbnail.SetGameSource(GameDetail.GameSource.Local);
    newThumbnail.SetName("New Game");
    newThumbnail.OnClick = OpenNew;
    newThumbnail.GetWriteTime = () => { return System.DateTime.Today; };
    newThumbnail.GetDescription = () => { return ""; };
    newThumbnail.SetSortPriorityHint(100);
    AddThumbnail(newThumbnail);
  }

  const string NEW_GAME_DESCRIPTION = "<b>New Game</b>\nStart a new project";
  void OpenNew()
  {
    templateSelectorMenu.Show();
  }

  void AddTutorialThumbnail()
  {
    tutorialThumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
    tutorialThumbnail.SetThumbnail(tutorialGameTexture);
    tutorialThumbnail.SetGameSource(GameDetail.GameSource.Local);
    tutorialThumbnail.SetName("Tutorial");
    tutorialThumbnail.OnClick = OpenTutorial;
    tutorialThumbnail.GetWriteTime = () => { return System.DateTime.Today; };
    tutorialThumbnail.GetDescription = () => { return ""; };
    tutorialThumbnail.SetSortPriorityHint(50);
    AddThumbnail(tutorialThumbnail);
  }

  IEnumerator AddSampleGameThumbnails()
  {
    int sortPriorityHint = 20;
    foreach (LocalSampleGames.GameInfo gameInfo in LocalSampleGames.LOCAL_SAMPLE_GAMES)
    {
      MakeSampleGameThumbnail(gameInfo, sortPriorityHint--);
      yield return null;
    }
  }

  private void MakeSampleGameThumbnail(LocalSampleGames.GameInfo gameInfo, int sortPriorityHint)
  {
    GameThumbnail thumb = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
    thumb.SetThumbnailUrl("file://" + gameInfo.GetThumbnailFilePath());
    thumb.SetGameSource(GameDetail.GameSource.Local);
    thumb.SetName(gameInfo.title);
    thumb.OnClick = () => OpenSampleGame(thumb, gameInfo);
    thumb.GetWriteTime = () => { return System.DateTime.Today; };
    thumb.GetDescription = () => { return gameInfo.description; };
    thumb.SetSortPriorityHint(sortPriorityHint);
    AddThumbnail(thumb);
  }

  void OpenSampleGame(GameThumbnail thumb, LocalSampleGames.GameInfo gameInfo)
  {
    SelectThumbnail(thumb, (rect) =>
    {
      gameDetail.FitTo(rect);
      string desc = "<b>Example game: " + gameInfo.title + "</b>\n" + gameInfo.description;
      gameDetail.OpenSpecial(desc, thumb.GetTexture(), playOpts =>
      {
        loadingScreen.ShowAndDo(() =>
        {
          var gameOpts = new GameBuilderApplication.GameOptions { playOptions = playOpts };
          loadingScreen.ShowAndDo(() => sceneController.RestartAndLoad(gameInfo.GetVoosFilePath(), gameOpts));
        });
      }, true);
    });
  }


  const string TUTORIAL_GAME_DESCRIPTION = "<b>Tutorial</b>\nLearn how to build games!";
  void OpenTutorial()
  {
    SelectThumbnail(tutorialThumbnail, (rect) =>
    {
      gameDetail.FitTo(rect);
      gameDetail.OpenSpecial(TUTORIAL_GAME_DESCRIPTION, tutorialGameTexture, playOpts =>
      {
        loadingScreen.ShowAndDo(() =>
        {
          sceneController.RestartAndLoadTutorial();
        });
      }, true);
    });
  }

  IEnumerator CreateMyLibraryThumbnails()
  {
    foreach (GameBundleLibrary.Entry entry in gameBundleLibrary.Enumerate())
    {
      GameThumbnail gameThumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
      GameBundle.Metadata metadata = entry.bundle.GetMetadata();

      Texture2D texture = entry.bundle.GetThumbnail();
      if (texture != null)
      {
        gameThumbnail.SetThumbnail(texture);
      }
      else
      {
        gameThumbnail.SetThumbnail(placeholderThumbnailTexture);
      }

      if (AutoSaveController.IsAutosave(entry))
      {
        gameThumbnail.SetGameSource(GameDetail.GameSource.Autosave);
      }
      else
      {
        gameThumbnail.SetGameSource(GameDetail.GameSource.Local);
      }
      gameThumbnail.SetName(metadata.name);
      gameThumbnail.OnClick = () => OpenLibraryEntry(gameThumbnail, entry);
      gameThumbnail.GetWriteTime = () => GetBundleWriteTime(entry.id);
      gameThumbnail.GetDescription = () => { return metadata.description; };
      AddThumbnail(gameThumbnail, AutoSaveController.IsAutosave(entry) ? AUTOSAVES : SAVED);
      yield return null;
    }

  }

#if USE_STEAMWORKS
  void CreateSteamSubscribedThumbnails()
  {
    if (!SteamManager.Initialized)
    {
      SetCategoryLoading(WORKSHOP, false);
      Debug.Log($"Steam Workshop is not available right now. Did you launch from Steam?");
      return;
    }
    SteamWorkshopMain.Instance.ResetItemSearch();
    SteamWorkshopMain.Instance.Sorting = new WorkshopSortMode(EWorkshopSource.SUBSCRIBED);
    SteamWorkshopMain.Instance.GetItemList(1, OnSubscribedWorkshopListLoaded);
  }

  IEnumerator GenerateWorkshopThumbnails(List<WorkshopItem> items)
  {
    foreach (WorkshopItem item in items)
    {
      string[] tags = item.SteamNative.m_details.m_rgchTags.Split(',');
      if (tags.Contains(SteamUtil.GameBuilderTags.Asset.ToString())) continue;
      GameThumbnail gameThumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
      if (item.PreviewImageURL != null && item.PreviewImageURL.Length > 0)
      {
        gameThumbnail.SetThumbnailUrl(item.PreviewImageURL);
      }
      else
      {
        gameThumbnail.SetThumbnail(placeholderThumbnailTexture);
      }

      gameThumbnail.SetGameSource(GameDetail.GameSource.Workshop);
      gameThumbnail.SetName(item.Name);
      gameThumbnail.OnClick = () => OpenWorkshopEntry(gameThumbnail, item);
      gameThumbnail.GetWriteTime = () => GetWorkshopWriteTime(item);
      gameThumbnail.GetDescription = () => { return item.Description; };
      AddThumbnail(gameThumbnail, WORKSHOP);
      yield return null;
    }

  }


  void OnSubscribedWorkshopListLoaded(WorkshopItemListEventArgs args)
  {
    SetCategoryLoading(WORKSHOP, false);
    if (!IsOpen())
    {
      return;
    }

    if (args.IsError)
    {
      Debug.Log($"Sorry, encountered an error: {args.ErrorMessage}");
      return;
    }

    StartCoroutine(GenerateWorkshopThumbnails(args.ItemList.Items));


  }
#endif

  void OpenLibraryEntry(GameThumbnail gameThumbnail, GameBundleLibrary.Entry entry)
  {

#if UNITY_EDITOR
    // Dev only for deleting
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt))
    {
      UnityEngine.Windows.Directory.Delete(gameBundleLibrary.GetBundleDirectory(entry.id));
      return;
    }
#endif

    if (Input.GetKey(KeyCode.LeftControl))
    {
      Application.OpenURL($"file://{gameBundleLibrary.GetBundleDirectory(entry.id)}");
      return;
    }

    SelectThumbnail(gameThumbnail, (rect) =>
    {
      gameDetail.FitTo(rect);
      gameDetail.OpenLocal(entry);
    });

    ScrollToThumbnail(gameThumbnail);
  }

  private void ScrollToThumbnail(GameThumbnail gameThumbnail)
  {
    Vector2 thumbnailCoordinates = Util.FindRectTransformScreenPoint(gameThumbnail.GetComponent<RectTransform>());
    RectTransformUtility.ScreenPointToLocalPointInRectangle(libraryViewport, thumbnailCoordinates, null, out Vector2 viewportPoint);

    if (viewportPoint.y < libraryViewport.offsetMax.y || viewportPoint.y > libraryViewport.offsetMin.y)
    {
      RectTransformUtility.ScreenPointToLocalPointInRectangle(libraryContainer, thumbnailCoordinates, null, out Vector2 localPoint);
      libraryScrollRect.verticalNormalizedPosition = Mathf.Clamp01(1 - Mathf.Abs(localPoint.y / libraryContainer.rect.height));
    }
  }

#if USE_STEAMWORKS
  void OpenWorkshopEntry(GameThumbnail gameThumbnail, WorkshopItem item)
  {
    SelectThumbnail(gameThumbnail, (rect) =>
    {
      gameDetail.FitTo(rect);
      gameDetail.OpenWorkshop(gameThumbnail.GetTexture(), item);
    });
    ScrollToThumbnail(gameThumbnail);
  }
#endif

  protected override void ChangeSort(Direction direction)
  {
    if (localSort == LocalSort.Name)
    {
      localSort = LocalSort.Recent;
    }
    else
    {
      localSort = LocalSort.Name;
    }
    sortText.text = localSort.ToString();
    UpdateSort();
  }

  protected void UpdateSort()
  {
    SetSorting((t1, t2) =>
    {
      if (t1.GetSortPriorityHint() != t2.GetSortPriorityHint())
      {
        return t2.GetSortPriorityHint().CompareTo(t1.GetSortPriorityHint());
      }
      if (localSort == LocalSort.Name)
      {
        return t1.GetName().CompareTo(t2.GetName());
      }
      else
      {
        return t2.GetWriteTime().CompareTo(t1.GetWriteTime());
      }
    });
  }

  System.DateTime GetBundleWriteTime(string bundleId)
  {
    string dir = gameBundleLibrary.GetBundleDirectory(bundleId);
    return System.IO.File.GetLastWriteTime(dir);
  }

#if USE_STEAMWORKS
  System.DateTime GetWorkshopWriteTime(WorkshopItem item)
  {
    return item.InstalledTimestamp;
  }
#endif
}
