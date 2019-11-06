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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameBuilder;
public abstract class ThumbnailMenu : MonoBehaviour, IMenuPanelInterface
{
  [SerializeField] protected UnityEngine.UI.Button closeButton;
  [SerializeField] protected UnityEngine.UI.Button sortLeft;
  [SerializeField] protected TMPro.TextMeshProUGUI sortText;
  [SerializeField] protected UnityEngine.UI.Button sortRight;
  [SerializeField] protected TMPro.TMP_InputField searchField;
  [SerializeField] protected UnityEngine.UI.Button searchClear;
  [SerializeField] protected GameObject noResultsFeedback;
  [SerializeField] protected GameObject workingFeedback;
  [SerializeField] protected GameObject thumbnailCategoryPrefab;
  [SerializeField] protected GameObject thumbnailCategoryContainer;
  [SerializeField] protected Texture2D placeholderThumbnailTexture;

  public System.Action OnOpen;
  public System.Action OnClose;

  protected GameBundleLibrary gameBundleLibrary;
  protected GameBuilderSceneController sceneController;

  private List<ThumbnailCategory> categories = new List<ThumbnailCategory>();
  private ThumbnailItem selectedThumbnail;

  protected string currentSearchString;

  public enum Direction
  {
    Left,
    Right
  };

  public virtual void Setup()
  {
    Util.FindIfNotSet(this, ref gameBundleLibrary);
    Util.FindIfNotSet(this, ref sceneController);

    sortLeft.onClick.AddListener(() => ChangeSort(Direction.Left));
    sortRight.onClick.AddListener(() => ChangeSort(Direction.Right));

    closeButton.onClick.AddListener(Close);
  }

  protected void AddCategory(string name = null, string placeholderText = null)
  {
    ThumbnailCategory thumbnailCategory =
        Instantiate(thumbnailCategoryPrefab, thumbnailCategoryContainer.transform).GetComponent<ThumbnailCategory>();
    //  thumbnailCategory.transform.parent = thumbnailCategoryContainer.transform;
    thumbnailCategory.SetName(name);
    if (placeholderText != null)
    {
      thumbnailCategory.SetPlaceholderText(placeholderText);
    }
    categories.Add(thumbnailCategory);
    thumbnailCategory.SetFilter(DoesThumbnailMatchSearch);
  }

  protected void SetCategoryEnabled(string categoryName, bool enabled)
  {
    ThumbnailCategory foundCategory = categories.Find(
      category => { return category.GetName() == categoryName; });
    if (foundCategory != null)
    {
      foundCategory.SetEnabled(enabled);
    }
  }

  protected void SetCategoryLoading(string categoryName, bool loading)
  {
    ThumbnailCategory foundCategory = categories.Find(
      category => { return category.GetName() == categoryName; });
    if (foundCategory != null)
    {
      foundCategory.SetLoading(loading);
    }
  }

  protected void AddThumbnail(ThumbnailItem thumbnail, string categoryName = null)
  {
    ThumbnailCategory foundCategory = categories.Find(
      category => { return category.GetName() == categoryName; });
    if (foundCategory != null)
    {
      foundCategory.AddThumbnail(thumbnail);
    }
  }

  protected void SelectThumbnail(ThumbnailItem thumbnail, System.Action<RectTransform> showContent)
  {
    foreach (ThumbnailCategory category in categories)
    {
      category.SelectThumbnail(thumbnail, showContent);
    }
  }

  protected abstract void ChangeSort(Direction direction);
  protected void SetSorting(Comparison<ThumbnailItem> sorter)
  {
    foreach (ThumbnailCategory category in categories)
    {
      category.SetSorting(sorter);
    }
  }

  protected string GetSearchString()
  {
    return currentSearchString;
  }

  protected void SetSearchString(string searchString)
  {
    currentSearchString = searchString;
    foreach (ThumbnailCategory category in categories)
    {
      category.SetFilter(DoesThumbnailMatchSearch);
    }
  }

  protected void ClearThumbnails()
  {
    foreach (ThumbnailCategory category in categories)
    {
      category.ClearThumbnails();
    }
  }

  protected int GetVisibleCount()
  {
    int count = 0;
    foreach (ThumbnailCategory category in categories)
    {
      count += category.GetVisibleCount();
    }
    return count;
  }

  protected virtual void Update()
  {
  }

  public virtual void Open()
  {
    gameObject.SetActive(true);
    OnOpen?.Invoke();
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }
  public void SetOpenEvent(System.Action action)
  {
    OnOpen = action;
  }

  public virtual void Close()
  {
    gameObject.SetActive(false);
    ClearThumbnails();
    OnClose?.Invoke();
  }

  public void SetCloseEvent(System.Action action)
  {
    OnClose = action;
  }

  protected virtual bool DoesThumbnailMatchSearch(ThumbnailItem thumbnailItem)
  {
    if (currentSearchString == null || currentSearchString == "") return true;
    string lowerSearchString = currentSearchString.ToLower();
    string name = Util.EmptyIfNull(thumbnailItem.GetName()).ToLower();

    string description = Util.EmptyIfNull(thumbnailItem.GetDescription()).ToLower();

    return name.Contains(lowerSearchString) || description.Contains(lowerSearchString);
  }
}
