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
using System.IO;
using System.Linq;
using UnityEngine;

public class CreationLibrarySidebar : Sidebar
{

  [System.Serializable]
  public class CategoryButton
  {
    public Texture2D icon;
    public string displayName;
    public string category;
  }

  [SerializeField] CreationLibraryUI creationLibraryUI;
  [SerializeField] string challengeCategoryUrl;
  [SerializeField] Texture2D challengeCategoryIcon;
  [SerializeField] Texture2D allCategoryIcon;
  [SerializeField] Texture2D decorationCategoryIcon;
  [SerializeField] Texture2D savedCategoryIcon;
  [SerializeField] Texture2D polyCategoryIcon;
  [SerializeField] Texture2D gisCategoryIcon;
  [SerializeField] Texture2D soundsCategoryIcon;
  [SerializeField] Texture2D particlesCategoryIcon;
  [SerializeField] CreationLibraryParticles particleLibrary;
  [SerializeField] CreationLibrarySounds soundLibrary;
  [SerializeField] ActorPrefabUploadDialog actorPrefabUploadDialogPrefab;
  [SerializeField] ActorPrefabWorkshopMenu actorPrefabWorkshopMenuPrefab;
  private static string CATEGORY_CHALLENGE = "Cooking challenge";
  private static string CATEGORY_ALL = "All";
  private static string CATEGORY_DECORATIONS = "Decor";
  private static string CATEGORY_CUSTOM = "Custom";
  private static string CATEGORY_POLY = "Objects from web";
  private static string CATEGORY_GIS = "Images from web";
  private static string CATEGORY_PARTICLES = "Particles";
  private static string CATEGORY_SOUNDS = "Sounds";
  [SerializeField] List<CategoryButton> dynamicCategories;
  private EditMain editMain;
  private UserMain userMain;
  private AssetSearch assetSearch;
  private SceneActorLibrary sceneActorLibrary;
  private DynamicPopup popups;

  private System.Action<ActorableSearchResult> pickerCallback;
  private string selectedCategory;
  private List<SquareImageButtonUI> internalResults = new List<SquareImageButtonUI>();
  private List<SquareImageButtonUI> webResults = new List<SquareImageButtonUI>();
  private string searchString = "";
  private SquareImageButtonUI selectedResult;
  private ActorableSearchResult sfxActorTemplate;
  private ActorableSearchResult pfxActorTemplate;
  private Dictionary<SquareImageButtonUI, float> timers = new Dictionary<SquareImageButtonUI, float>();
  private float lastSearchTime = 0;
  ActorableSearchResult lastResult;
  public System.Action<ActorableSearchResult> updateAsset;
  private ActorPrefabUploadDialog actorPrefabUploadDialog;
  private ActorPrefabWorkshopMenu actorPrefabWorkshopMenu;

  System.Predicate<SquareImageButtonUI> ShouldShowInternalResult;
  //what to send when you get a null result
  string BACKUP_SEARCH_RESULT_ID = "Forest/Slime";

  const string IMPORT_FROM_DISK = "From disk";
  const string IMPORT_FROM_WORKSHOP = "From workshop";
  const string EXPORT_TO_DISK = "To disk";
  const string EXPORT_TO_WORKSHOP = "To workshop";
  const string UPDATE_WORKSHOP_PREFAB = "Update workshop pack";

  public bool IsSearchActive()
  {
    return creationLibraryUI.searchInput.isFocused;
  }

  private bool IsChallengeAsset(SquareImageButtonUI item)
  {
    return false;
  }

  public ActorableSearchResult GetLastResult()
  {
    return lastResult;
  }

  public override void Setup(SidebarManager _sidebarManager)
  {
    base.Setup(_sidebarManager);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref assetSearch);
    Util.FindIfNotSet(this, ref sceneActorLibrary);
    Util.FindIfNotSet(this, ref popups);

    ShouldShowInternalResult = (result) => { return false; };
    lastResult = assetSearch.GetBuiltInSearchResult("Forest/Slime");

    creationLibraryUI.searchInput.onEndEdit.AddListener(OnInputFieldEnd);
    creationLibraryUI.clearSearchButton.onClick.AddListener(ClearSearch);
    sceneActorLibrary.onActorPut += OnActorPut;
    sceneActorLibrary.onActorDelete += OnActorDelete;

    assetSearch.AddPrefabsProcessor((searchResult) => OnResult(searchResult, true));


    List<Util.Tuple<string, Texture2D>> allCategories = new List<Util.Tuple<string, Texture2D>>();
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_ALL, allCategoryIcon));
    foreach (CategoryButton category in dynamicCategories)
    {
      allCategories.Add(new Util.Tuple<string, Texture2D>(category.displayName, category.icon));
    }
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_DECORATIONS, decorationCategoryIcon));
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_CUSTOM, savedCategoryIcon));

    //NO_POLY_TOOLKIT_INTERNAL_CHECK 
    if (!PolyToolkitInternal.PtSettings.Instance.authConfig.apiKey.Contains("INSERT YOUR"))
    {
      allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_POLY, polyCategoryIcon));
    }
    if (GisSearchManager.APIkey != "PUT YOUR KEY HERE")
    {
      allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_GIS, gisCategoryIcon));
    }

    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_PARTICLES, particlesCategoryIcon));
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_SOUNDS, soundsCategoryIcon));

    foreach (Util.Tuple<string, Texture2D> categoryTuple in allCategories)
    {
      string category = categoryTuple.first;
      Texture2D texture = categoryTuple.second;
      TextIconButtonUI categoryButton = Instantiate(
        creationLibraryUI.categoryButtonPrefab, creationLibraryUI.categoriesList.transform);
      categoryButton.text.text = category;
      categoryButton.icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
      categoryButton.button.onClick.AddListener(() =>
      {
        selectedCategory = category;
        UpdateAll();
      });
      if (category == CATEGORY_CHALLENGE)
      {
        categoryButton.medallion.gameObject.SetActive(true);
      }
      categoryButton.gameObject.SetActive(true);
    }

    creationLibraryUI.inCategoryBackButton.onClick.AddListener(() =>
    {
      selectedCategory = null;
      ClearSearch(); //includes update all
      // UpdateAll();
    });

    particleLibrary.Setup();
    particleLibrary.onParticleEffectSelected += OnParticleEffectSelected;
    soundLibrary.onSoundSelected += OnSoundEffectSelected;
    soundLibrary.Setup();

    creationLibraryUI.inCategoryLink.onClick.AddListener(() =>
    {
      Application.OpenURL(challengeCategoryUrl);
    });

#if USE_STEAMWORKS
    actorPrefabUploadDialog = Instantiate(actorPrefabUploadDialogPrefab);
    actorPrefabUploadDialog.Setup();
    actorPrefabWorkshopMenu = Instantiate(actorPrefabWorkshopMenuPrefab);
    actorPrefabWorkshopMenu.Setup();
#endif

    creationLibraryUI.exportDropdownMenu.onOptionClicked += (value) =>
    {
      SquareImageButtonUI exportResult = selectedResult;
      Debug.Assert(selectedResult != null);
      ActorPrefab prefab = exportResult.GetSearchResult().actorPrefab;
      Debug.Assert(sceneActorLibrary.Exists(prefab?.GetId()));

      if (value == EXPORT_TO_DISK)
      {
        ExportToDisk(prefab);
      }
#if USE_STEAMWORKS
      else if (value == UPDATE_WORKSHOP_PREFAB)
      {
        ulong? id = GetWorkshopIdForActor(prefab);
        actorPrefabUploadDialog.Open(prefab, Util.Maybe<ulong>.CreateWith(id.Value));
      }
      else
      {
        actorPrefabUploadDialog.Open(prefab, Util.Maybe<ulong>.CreateEmpty());
      }
#endif
    };

#if USE_STEAMWORKS
    creationLibraryUI.importDropdownMenu.SetOptions(new List<string>() {
      IMPORT_FROM_DISK, IMPORT_FROM_WORKSHOP
    });
#else
    creationLibraryUI.importCustomButton.onClick.AddListener(ImportFromDisk);
#endif

    creationLibraryUI.importDropdownMenu.onOptionClicked += (value) =>
    {
      if (value == IMPORT_FROM_DISK)
      {
        ImportFromDisk();
      }
      else
      {
        actorPrefabWorkshopMenu.Open();
      }
    };

    pfxActorTemplate = assetSearch.GetBuiltInSearchResult("Empty");
    sfxActorTemplate = assetSearch.GetBuiltInSearchResult("Sound");
  }

  private ulong? GetWorkshopIdForActor(ActorPrefab actorPrefab)
  {
    foreach (SceneActorLibrary.SavedActorPack pack in sceneActorLibrary.GetActorPacks().actorPacks)
    {
      string id = pack.ids[0];
      if (id == actorPrefab.GetId())
      {
        return pack.workshopId;
      }
    }
    return null;
  }

  private void ExportToDisk(ActorPrefab prefab)
  {
#if USE_FILEBROWSER
    string path = Crosstales.FB.FileBrowser.OpenSingleFolder("Select save location");
    if (!path.IsNullOrEmpty())
    {
      sceneActorLibrary.WritePrefabToDir(prefab.GetId(), path);
      popups.Show("Actor exported!", "Ok");
    }
#else
    sceneActorLibrary.WritePrefabToDir(prefab.GetId(), System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "GBActors"));
    popups.Show("Actor exported to Documents/GBActors folder.\nFor more control over export location, build with the free CrossTales FileBrowser plugin.", "Ok");
#endif
  }

  [CommandTerminal.RegisterCommand(Help = "Import actor by path")]
  static void ImportActor(CommandTerminal.CommandArg[] args)
  {
    CreationLibrarySidebar inst = GameObject.FindObjectOfType<CreationLibrarySidebar>();
    if (inst == null)
    {
      GameBuilderConsoleCommands.Log($"Please open the custom actor library (via Creation Library) before using this command.");
      return;
    }
    string path = GameBuilderConsoleCommands.JoinTailToPath(args);
    if (path != null)
    {
      inst.ImportFromPath(path);
    }
  }

  private void ImportFromDisk()
  {
#if USE_FILEBROWSER
    string path = Crosstales.FB.FileBrowser.OpenSingleFolder("Import from folder");
    if (path.IsNullOrEmpty()) return;
ImportFromPath(path);
   
#else
    popups.Show("Use the console command 'importactor C:\\path\\to\\actorfolder' instead. Or, build with the free CrossTales FileBrowser plugin.", "OK");
#endif
  }

  public void ImportFromPath(string path)
  {
    Dictionary<string, SavedActorPrefab> prefabs = SceneActorLibrary.ReadPrefabsFromDir(path);
    if (prefabs.Count == 0)
    {
      popups.Show("No actors found!", "Ok");
      return;
    }

    bool containsOverrides = false;
    foreach (var entry in prefabs)
    {
      if (sceneActorLibrary.Exists(entry.Key))
      {
        containsOverrides = true;
      }
    }
    if (containsOverrides)
    {
      popups.ShowThreeButtons(
        "Actor(s) already exists in your library.",
        "Overwrite", () =>
        {
          sceneActorLibrary.PutPrefabs(prefabs, true);
          popups.Show(
            $"Actor(s) was successfully imported. Check your custom actors!",
            "Ok"
          );
        },
        "Duplicate", () =>
        {
          sceneActorLibrary.PutPrefabs(prefabs);
          popups.Show(
            $"Actor(s) was successfully imported. Check your custom actors!",
            "Ok"
          );
        },
        "Cancel", () => { });
    }
    else
    {
      sceneActorLibrary.PutPrefabs(prefabs);
      popups.Show(
        $"Actor(s) was successfully imported. Check your custom actors!",
        "Ok"
      );
    }
  }


  private void DeleteSelectedResult()
  {
    SquareImageButtonUI deleteResult = selectedResult;
    SetSelectedResult(null);
    sceneActorLibrary.Delete(deleteResult.GetSearchResult().actorPrefab.GetId());
  }

  private void UpdateAll()
  {
    creationLibraryUI.categoriesList.SetActive(searchString == "" && selectedCategory == null);
    creationLibraryUI.inCategoryLabel.text = selectedCategory;
    creationLibraryUI.webSearchHint.gameObject.SetActive(
      selectedCategory == CATEGORY_POLY || selectedCategory == CATEGORY_GIS);

    particleLibrary.Hide();
    soundLibrary.Close();
    creationLibraryUI.resultsRectContainer.SetActive(false);
    creationLibraryUI.actionButtonsContainer.SetActive(false);
    creationLibraryUI.createButton.gameObject.SetActive(true);
    creationLibraryUI.importDropdownMenu.gameObject.SetActive(false);
    creationLibraryUI.trashButton.onClick.RemoveListener(DeleteSelectedResult);

    creationLibraryUI.inCategoryLink.gameObject.SetActive(selectedCategory == CATEGORY_CHALLENGE);

    if (selectedCategory != null || searchString != "")
    {
      if (selectedCategory == CATEGORY_PARTICLES)
      {
        particleLibrary.Show();
      }
      else if (selectedCategory == CATEGORY_SOUNDS)
      {
        soundLibrary.Open();
      }
      else
      {
        creationLibraryUI.resultsRectContainer.SetActive(true);
        creationLibraryUI.actionButtonsContainer.SetActive(true);
        creationLibraryUI.createButton.gameObject.SetActive(false);
        creationLibraryUI.trashButton.onClick.AddListener(DeleteSelectedResult);

        if (selectedCategory == null || selectedCategory == CATEGORY_ALL)
        {
          ShowAllModels();
        }
        else if (selectedCategory == CATEGORY_CUSTOM)
        {
          ShowSaved();
        }
        else if (selectedCategory == CATEGORY_POLY)
        {
          ShowPoly();
        }
        else if (selectedCategory == CATEGORY_GIS)
        {
          ShowGIS();
        }
        else if (selectedCategory == CATEGORY_DECORATIONS)
        {
          ShowMiscCategory();
        }
        else if (selectedCategory == CATEGORY_CHALLENGE)
        {
          ShowChallengeCategory();
        }
        else
        {
          ShowCategory(selectedCategory);
        }
      }
    }

    creationLibraryUI.inCategoryContainer.SetActive(selectedCategory != null);
  }

  void ShowAllModels()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, DoesInternalResultMatchSearch(result));
      ShouldShowInternalResult = (_result) => { return true; };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }

  void ShowSaved()
  {
    creationLibraryUI.importDropdownMenu.gameObject.SetActive(true);
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, sceneActorLibrary.Exists(result.GetSearchResult().actorPrefab.GetId()) &&
        DoesInternalResultMatchSearch(result));
      ShouldShowInternalResult = (_result) =>
      {
        return sceneActorLibrary.Exists(_result.GetSearchResult().actorPrefab.GetId()) &&
        DoesInternalResultMatchSearch(_result);
      };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }

  void ShowPoly()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, false);
      ShouldShowInternalResult = (_result) =>
{
  return false;
};
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, result.GetSearchResult().renderableReference.assetType == AssetType.Poly);
    }
  }

  void ShowGIS()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, false);
      ShouldShowInternalResult = (_result) =>
     {
       return false;
     };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, result.GetSearchResult().renderableReference.assetType == AssetType.Image);
    }
  }

  void ShowMiscCategory()
  {
    // TODO implement categories
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, result.GetSearchResult().actorPrefab.GetCategory().IsNullOrEmpty() &&
        DoesInternalResultMatchSearch(result) && !IsChallengeAsset(result));

      ShouldShowInternalResult = (_result) =>
    {
      return _result.GetSearchResult().actorPrefab.GetCategory().IsNullOrEmpty() &&
      DoesInternalResultMatchSearch(_result) && !IsChallengeAsset(_result);
    };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }

  void SetResultShowing(SquareImageButtonUI result, bool showing)
  {
    if (showing)
    {
      result.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
      result.gameObject.SetActive(true);
      timers[result] = 0;
    }
    else
    {
      result.gameObject.SetActive(false);
      timers.Remove(result);
    }
  }

  void ShowChallengeCategory()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result,
        IsChallengeAsset(result) && DoesInternalResultMatchSearch(result));

      ShouldShowInternalResult = (_result) =>
    {
      return IsChallengeAsset(_result) && DoesInternalResultMatchSearch(_result);
    };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }


  void UpdateInternalResultShowing(SquareImageButtonUI result)
  {
    if (result == null) return;
    SetResultShowing(result, ShouldShowInternalResult(result));
  }

  void ShowCategory(string displayName)
  {
    string categoryName = dynamicCategories.Find((d) => d.displayName == displayName).category;
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, result.GetSearchResult().actorPrefab.GetCategory() == categoryName &&
        DoesInternalResultMatchSearch(result) && !IsChallengeAsset(result));

      ShouldShowInternalResult = (_result) =>
    {
      return _result.GetSearchResult().actorPrefab.GetCategory() == categoryName &&
      DoesInternalResultMatchSearch(_result) && !IsChallengeAsset(_result);
    };
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }

  void DefaultWebSearch()
  {
    assetSearch.DefaultSearch((searchResult) => OnResult(searchResult, false));
  }

  public enum LibraryMode
  {
    CreateTool,
    Picker
  };

  LibraryMode libraryMode = LibraryMode.CreateTool;

  public void SetToCreateTool()
  {
    libraryMode = LibraryMode.CreateTool;
  }

  public void SetToPicker(System.Action<ActorableSearchResult> pickerCallback)
  {
    libraryMode = LibraryMode.Picker;
    this.pickerCallback = pickerCallback;
  }

  void ResultClicked(SquareImageButtonUI _result)
  {
    sidebarManager.OnClickSoundEffect();
    SetSelectedResult(_result);
    if (libraryMode != LibraryMode.CreateTool && pickerCallback != null)
    {
      pickerCallback(_result.GetSearchResult());
    }
  }

  void SetSelectedResult(SquareImageButtonUI result)
  {
    if (selectedResult != null) selectedResult.SetSelected(false);
    selectedResult = result;
    if (selectedResult != null)
    {
      selectedResult.SetSelected(true);
    }
    bool isSaved = selectedResult != null && sceneActorLibrary.Exists(selectedResult.GetSearchResult().actorPrefab?.GetId());
    creationLibraryUI.trashButton.gameObject.SetActive(isSaved);
    creationLibraryUI.exportDropdownMenu.gameObject.SetActive(isSaved);
    if (libraryMode == LibraryMode.CreateTool)
    {
      if (result != null)
      {
        UpdateResult(result.GetSearchResult());
      }
    }
    if (isSaved)
    {
#if USE_STEAMWORKS
      if (GetWorkshopIdForActor(selectedResult.GetSearchResult().actorPrefab).HasValue)
      {
        creationLibraryUI.exportDropdownMenu.SetOptions(new List<string>() {
          EXPORT_TO_DISK, UPDATE_WORKSHOP_PREFAB, EXPORT_TO_WORKSHOP
        });
      }
      else
      {
        creationLibraryUI.exportDropdownMenu.SetOptions(new List<string>() {
          EXPORT_TO_DISK, EXPORT_TO_WORKSHOP
        });
      }
#else
      SquareImageButtonUI exportResult = selectedResult;
      ActorPrefab prefab = exportResult.GetSearchResult().actorPrefab;
      creationLibraryUI.exportCustomButton.onClick.RemoveAllListeners();
      creationLibraryUI.exportCustomButton.onClick.AddListener(() => ExportToDisk(prefab));
#endif
    }
  }

  void UpdateResult(ActorableSearchResult newResult)
  {
    lastResult = newResult;
    updateAsset?.Invoke(newResult);
  }

  void ClearSearch()
  {
    searchString = "";
    creationLibraryUI.searchInput.text = "";
    ClearAssets();
    lastSearchTime = Time.realtimeSinceStartup;
    // DefaultWebSearch();
    UpdateAll();
  }

  void ClearAssets()
  {
    for (int i = 0; i < webResults.Count; i++)
    {
      if (webResults[i] == selectedResult) SetSelectedResult(null);
      webResults[i].RequestDestroy();
    }
    webResults.Clear();
  }

  void RemoveInternalResultAtIndex(int index)
  {
    if (internalResults[index] == selectedResult) SetSelectedResult(null);
    internalResults[index].RequestDestroy();
    internalResults.RemoveAt(index);
  }

  void OnInputFieldEnd(string s)
  {
    if (Input.GetButtonDown("Submit"))
    {
      searchString = creationLibraryUI.searchInput.text;
      NewSearch();
    }
  }

  void Update()
  {
    creationLibraryUI.clearSearchButton.gameObject.SetActive(searchString != "");

    foreach (var key in timers.Keys.ToList())
    {
      if (key == null)
      {
        timers.Remove(key);
        return;
      }
      float newTime = timers[key] + Time.unscaledDeltaTime;
      timers[key] = newTime;
      float percent = Mathf.Clamp(newTime / 0.25f, 0.01f, 1f);
      key.GetComponent<RectTransform>().localScale = Vector3.one * percent;
      if (percent > 1) timers.Remove(key);
    }

    if (selectedCategory == CATEGORY_POLY || selectedCategory == CATEGORY_GIS)
    {
      creationLibraryUI.webSearchHint.gameObject.SetActive(webResults.Count == 0);
    }
  }

  void OnResult(ActorableSearchResult incomingResult, bool isInternal)
  {
    // Ignore pfx/sfx results, they are loaded already
    if (incomingResult.name == "Empty" && incomingResult.name == "Sound") return;

    SquareImageButtonUI newResult = Instantiate(creationLibraryUI.resultsPrefab, creationLibraryUI.resultsRect);
    newResult.SetImage(incomingResult.thumbnail);
    newResult.onPointerDown = () => ResultClicked(newResult);
    newResult.SetSearchResult(incomingResult);

    ItemWithTooltipWithEventSystem tooltip = newResult.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    tooltip.SetupWithUserMain(userMain);
    tooltip.SetDescription(incomingResult.name);

    // int categoryValue = creationLibraryUI.categoryDropdown.value;
    if (isInternal)
    {
      internalResults.Add(newResult);
      // if (incomingResult.actorPrefab != null && !sceneActorLibrary.Exists(incomingResult.actorPrefab.GetId()))
      // {
      //   DropdownCategoryPut(incomingResult.actorPrefab.GetAssetPackName());
      // }
      if (sceneActorLibrary.Exists(incomingResult.actorPrefab.GetId()))
      {
        SetResultShowing(newResult,
          selectedCategory == CATEGORY_CUSTOM ||
          selectedCategory == null ||
          selectedCategory == CATEGORY_ALL);
      }
      else
      {
        UpdateInternalResultShowing(newResult);
      }
    }
    else
    {
      webResults.Add(newResult);
      if (incomingResult.renderableReference.assetType == AssetType.Poly)
      {
        SetResultShowing(newResult,
          selectedCategory == CATEGORY_POLY);
      }
      else if (incomingResult.renderableReference.assetType == AssetType.Image)
      {
        SetResultShowing(newResult,
          selectedCategory == CATEGORY_GIS);
      }
    }
  }

  void OnParticleEffectSelected(string pfxId)
  {
    if (pfxId != null)
    {
      pfxActorTemplate.pfxId = pfxId;
      UpdateResult(pfxActorTemplate);
    }
    else
    {
      updateAsset?.Invoke(assetSearch.GetBuiltInSearchResult("Forest/Slime"));
    }
  }

  void OnSoundEffectSelected(string sfxId)
  {
    if (sfxId != null)
    {
      sfxActorTemplate.sfxId = sfxId;
      UpdateResult(sfxActorTemplate);
    }
    else
    {
      updateAsset?.Invoke(assetSearch.GetBuiltInSearchResult("Forest/Slime"));
    }
  }

  public void NewSearch()
  {
    ClearAssets();
    lastSearchTime = Time.realtimeSinceStartup;
    float currSearchTime = lastSearchTime;
    assetSearch.Search(searchString, (searchResult) =>
    {
      if (lastSearchTime == currSearchTime)
      {
        OnResult(searchResult, false);
      }
    });
    UpdateAll();
  }

  private bool DoesInternalResultMatchSearch(SquareImageButtonUI result)
  {
    if (searchString == "") return true;
    string stringForSearch = result.GetSearchResult().actorPrefab.GetLabel().ToLower();
    return stringForSearch.Contains(searchString.ToLower());
  }

  public void OnActorPut(string prefabID)
  {
    for (int i = 0; i < internalResults.Count; i++)
    {
      if (internalResults[i].GetSearchResult().actorPrefab.GetId() == prefabID)
      {
        RemoveInternalResultAtIndex(i);
        break;
      }
    }

    ActorPrefab actorprefab = sceneActorLibrary.Get(prefabID);
    OnResult(assetSearch.TurnPrefabIntoSearchResult(actorprefab), true);
  }

  public void OnActorDelete(string prefabID)
  {
    for (int i = 0; i < internalResults.Count; i++)
    {
      if (internalResults[i].GetSearchResult().actorPrefab.GetId() == prefabID)
      {
        RemoveInternalResultAtIndex(i);
        break;
      }
    }
  }

  public bool OnEscape()
  {
#if USE_STEAMWORKS
    if (actorPrefabWorkshopMenu.IsOpen())
    {
      actorPrefabWorkshopMenu.Close();
      return true;
    }
    return false;
#endif
    return false;
  }

}
