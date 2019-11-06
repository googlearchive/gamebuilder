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
using System.Linq;
using UnityEngine;

public class RenderableLibrary : MonoBehaviour
{

  [System.Serializable]
  public class CategoryButton
  {
    public Texture2D icon;
    public string displayName;
    public string category;
  }

  [SerializeField] RenderableLibraryUI renderableLibraryUI;
  // [SerializeField] Texture2D challengeCategoryIcon;
  [SerializeField] string challengeCategoryUrl;
  [SerializeField] Texture2D allCategoryIcon;
  [SerializeField] Texture2D decorationCategoryIcon;
  [SerializeField] Texture2D polyCategoryIcon;
  [SerializeField] Texture2D gisCategoryIcon;
  // private static string CATEGORY_CHALLENGE = "Challenge";
  private static string CATEGORY_ALL = "All";
  private static string CATEGORY_DECORATIONS = "Decor";
  private static string CATEGORY_POLY = "Objects from web";
  private static string CATEGORY_GIS = "Images from web";
  [SerializeField] List<CategoryButton> dynamicCategories;
  private EditMain editMain;
  private UserMain userMain;
  private AssetSearch assetSearch;
  private SceneActorLibrary sceneActorLibrary;
  private System.Action<ActorableSearchResult> pickerCallback;
  private string selectedCategory;
  private List<SquareImageButtonUI> internalResults = new List<SquareImageButtonUI>();
  private List<SquareImageButtonUI> webResults = new List<SquareImageButtonUI>();
  private string searchString = "";
  private ActorableSearchResult emptyActorTemplate;
  private Dictionary<SquareImageButtonUI, float> timers = new Dictionary<SquareImageButtonUI, float>();
  private float lastSearchTime = 0;

  public bool IsSearchActive()
  {
    return renderableLibraryUI.searchInput.isFocused;
  }

  public void Setup()
  {
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref assetSearch);
    Util.FindIfNotSet(this, ref sceneActorLibrary);

    renderableLibraryUI.searchInput.onEndEdit.AddListener(OnInputFieldEnd);
    renderableLibraryUI.clearSearchButton.onClick.AddListener(ClearSearch);
    assetSearch.AddPrefabsProcessor((searchResult) => OnResult(searchResult, true));

    List<Util.Tuple<string, Texture2D>> allCategories = new List<Util.Tuple<string, Texture2D>>();
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_ALL, allCategoryIcon));
    foreach (CategoryButton category in dynamicCategories)
    {
      allCategories.Add(new Util.Tuple<string, Texture2D>(category.displayName, category.icon));
    }
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_DECORATIONS, decorationCategoryIcon));
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_POLY, polyCategoryIcon));
    allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_GIS, gisCategoryIcon));
    // allCategories.Add(new Util.Tuple<string, Texture2D>(CATEGORY_CHALLENGE, challengeCategoryIcon));

    foreach (Util.Tuple<string, Texture2D> categoryTuple in allCategories)
    {
      string category = categoryTuple.first;
      Texture2D texture = categoryTuple.second;
      TextIconButtonUI categoryButton = Instantiate(
        renderableLibraryUI.categoryButtonPrefab, renderableLibraryUI.categoriesList.transform);
      categoryButton.text.text = category;
      categoryButton.icon.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
      categoryButton.button.onClick.AddListener(() =>
      {
        selectedCategory = category;
        UpdateAll();
      });
      // if (category == CATEGORY_CHALLENGE)
      // {
      //   categoryButton.medallion.gameObject.SetActive(true);
      // }
      categoryButton.gameObject.SetActive(true);
    }

    renderableLibraryUI.inCategoryBackButton.onClick.AddListener(() =>
    {
      selectedCategory = null;
      UpdateAll();
    });

    renderableLibraryUI.inCategoryLink.onClick.AddListener(() =>
    {
      Application.OpenURL(challengeCategoryUrl);
    });

    renderableLibraryUI.closeButton.onClick.AddListener(() =>
    {
      gameObject.SetActive(false);
    });
  }

  private bool IsChallengeAsset(SquareImageButtonUI item)
  {
    return false;
    // return item.GetSearchResult().actorPrefab?.GetAssetPackName() == "Cooking";
  }

  private void UpdateAll()
  {
    renderableLibraryUI.categoriesList.SetActive(searchString == "" && selectedCategory == null);
    renderableLibraryUI.inCategoryContainer.SetActive(selectedCategory != null);
    renderableLibraryUI.inCategoryLabel.text = selectedCategory;
    // renderableLibraryUI.inCategoryLink.gameObject.SetActive(selectedCategory == CATEGORY_CHALLENGE);
    renderableLibraryUI.inCategoryLink.gameObject.SetActive(false);
    renderableLibraryUI.webSearchHint.gameObject.SetActive(
      selectedCategory == CATEGORY_POLY || selectedCategory == CATEGORY_GIS);

    renderableLibraryUI.resultsRectContainer.SetActive(false);
    if (selectedCategory != null || searchString != "")
    {
      renderableLibraryUI.resultsRectContainer.SetActive(true);
      if (selectedCategory == null || selectedCategory == CATEGORY_ALL)
      {
        ShowAllModels();
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
      // else if (selectedCategory == CATEGORY_CHALLENGE)
      // {
      //   ShowChallengeCategory();
      // }
      else
      {
        ShowCategory(selectedCategory);
      }
    }
  }

  void ShowAllModels()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, DoesInternalResultMatchSearch(result));
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
    }
    foreach (SquareImageButtonUI result in webResults)
    {
      SetResultShowing(result, false);
    }
  }

  void ShowChallengeCategory()
  {
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result,
        IsChallengeAsset(result) && DoesInternalResultMatchSearch(result));
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

  void ShowCategory(string displayName)
  {
    string categoryName = dynamicCategories.Find((d) => d.displayName == displayName).category;
    foreach (SquareImageButtonUI result in internalResults)
    {
      SetResultShowing(result, result.GetSearchResult().actorPrefab.GetCategory() == categoryName &&
        DoesInternalResultMatchSearch(result) && !IsChallengeAsset(result));
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

  public void AddResultClickedListener(System.Action<ActorableSearchResult> pickerCallback)
  {
    this.pickerCallback = pickerCallback;
  }

  void ResultClicked(SquareImageButtonUI _result)
  {
    pickerCallback?.Invoke(_result.GetSearchResult());
  }

  void ClearSearch()
  {
    searchString = "";
    renderableLibraryUI.searchInput.text = "";
    ClearAssets();
    lastSearchTime = Time.realtimeSinceStartup;
    UpdateAll();
  }

  void ClearAssets()
  {
    for (int i = 0; i < webResults.Count; i++)
    {
      webResults[i].RequestDestroy();
    }
    webResults.Clear();
  }

  void RemoveInternalResultAtIndex(int index)
  {
    internalResults[index].RequestDestroy();
    internalResults.RemoveAt(index);
  }

  void OnInputFieldEnd(string s)
  {
    if (Input.GetButtonDown("Submit"))
    {
      searchString = renderableLibraryUI.searchInput.text;
      NewSearch();
    }
  }

  void Update()
  {
    renderableLibraryUI.clearSearchButton.gameObject.SetActive(searchString != "");

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
      renderableLibraryUI.webSearchHint.gameObject.SetActive(webResults.Count == 0);
    }
  }

  void OnResult(ActorableSearchResult incomingResult, bool isInternal)
  {
    // hacckk
    if (incomingResult.name == "Empty")
    {
      emptyActorTemplate = incomingResult;
      return;
    }

    SquareImageButtonUI newResult = Instantiate(renderableLibraryUI.resultsPrefab, renderableLibraryUI.resultsRect);
    newResult.SetImage(incomingResult.thumbnail);
    newResult.onPointerDown = () => ResultClicked(newResult);
    newResult.SetSearchResult(incomingResult);

    ItemWithTooltipWithEventSystem tooltip = newResult.gameObject.AddComponent<ItemWithTooltipWithEventSystem>();
    tooltip.SetDescription(incomingResult.name);

    // int categoryValue = creationLibraryUI.categoryDropdown.value;
    if (isInternal)
    {
      internalResults.Add(newResult);
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
}
