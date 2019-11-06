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
using UnityEngine.UI;

public class ThumbnailCategory : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI nameText;
  [SerializeField] TMPro.TextMeshProUGUI placeholderText;
  [SerializeField] UnityEngine.UI.GridLayoutGroup gridLayoutGroup;
  [SerializeField] RectTransform thumbnailContainerParent;
  [SerializeField] RectTransform thumbnailContainerTop;
  [SerializeField] DetailContainer detailContainer;
  [SerializeField] RectTransform thumbnailContainerBottom;
  private List<ThumbnailItem> thumbnailItems = new List<ThumbnailItem>();
  private ThumbnailItem selectedThumbnail;
  private Comparison<ThumbnailItem> sorter;
  private Predicate<ThumbnailItem> filter;
  private string name;
  private IEnumerator loadingTextAnimator;
  private const string LOADING_STRING = "Loading ...";
  private string placeholderString;

  void Awake()
  {
    placeholderString = placeholderText.text;
  }

  public string GetName()
  {
    return name;
  }

  public void SetName(string name)
  {
    this.name = name;
    this.nameText.text = name;
    this.nameText.gameObject.active = name != null && name != "";
  }

  public void SetPlaceholderText(string placeholderString)
  {
    placeholderText.text = placeholderString;
  }

  public void SetEnabled(bool enabled)
  {
    thumbnailContainerParent.gameObject.active = enabled;
  }

  public void SetLoading(bool loading)
  {
    if (loading)
    {
      placeholderText.text = LOADING_STRING;
      placeholderText.gameObject.active = true;
    }
    else if (!loading)
    {
      placeholderText.text = placeholderString;
      placeholderText.gameObject.active = GetVisibleCount() == 0;
    }
  }

  public void SelectThumbnail(ThumbnailItem thumbnail, System.Action<RectTransform> showContent)
  {
    if (thumbnailItems.Contains(thumbnail))
    {
      Debug.Log(name + " opening!");
      selectedThumbnail = thumbnail;
      detailContainer.Open();
      detailContainer.UpdateArrowPosition(selectedThumbnail.GetComponent<RectTransform>().anchoredPosition.x);
      showContent(detailContainer.GetContentContainer());
      UpdateThumbnailsForSelection();
    }
    else
    {
      CloseSelectedThumbnail();
    }
  }

  public void CloseSelectedThumbnail()
  {
    if (selectedThumbnail == null) return;
    selectedThumbnail = null;
    detailContainer.Close();
    UpdateThumbnailsForSelection();
  }

  public void AddThumbnail(ThumbnailItem thumbnail)
  {
    thumbnailItems.Add(thumbnail);
    if (filter != null)
    {
      thumbnail.SetVisibility(filter(thumbnail));
    }
    placeholderText.gameObject.active = GetVisibleCount() == 0;
    thumbnail.transform.SetParent(thumbnailContainerTop, false);
    Refresh();
  }

  public void SetSorting(Comparison<ThumbnailItem> sorter)
  {
    this.sorter = sorter;
    Refresh();
  }

  public int GetVisibleCount()
  {
    int count = 0;
    foreach (ThumbnailItem thumbnail in thumbnailItems)
    {
      if (thumbnail.GetVisibility())
      {
        count++;
      }
    }
    return count;
  }

  public void SetFilter(Predicate<ThumbnailItem> filter)
  {
    this.filter = filter;
    if (filter != null)
    {
      foreach (ThumbnailItem thumbnail in thumbnailItems)
      {
        bool visible = filter(thumbnail);
        thumbnail.SetVisibility(visible);
        if (!visible && thumbnail == selectedThumbnail)
        {
          CloseSelectedThumbnail();
        }
      }
      placeholderText.gameObject.active = GetVisibleCount() == 0;
      UpdateThumbnailsForSelection();
    }
  }

  public void ClearThumbnails()
  {
    CloseSelectedThumbnail();
    for (int i = 0; i < thumbnailItems.Count; i++)
    {
      thumbnailItems[i].Destruct();
    }
    thumbnailItems.Clear();
    placeholderText.gameObject.active = true;
  }

  private void Refresh()
  {
    CloseSelectedThumbnail();
    if (sorter != null)
    {
      thumbnailItems.Sort(sorter);
    }
    UpdateThumbnailsForSelection();
  }

  private void UpdateThumbnailsForSelection()
  {
    int index = -1;
    int splitIndex = thumbnailItems.Count;

    if (selectedThumbnail != null)
    {
      index = thumbnailItems.IndexOf(selectedThumbnail);


      int columnCount = GetGridColumnCount();
      splitIndex = ((GetVisibleIndex(index) / columnCount) + 1) * columnCount;
    }

    int visibleCount = 0;
    for (int i = 0; i < thumbnailItems.Count; i++)
    {
      thumbnailItems[i].ToggleSelect(i == index);

      if (visibleCount < splitIndex)
      {
        thumbnailItems[i].transform.SetParent(thumbnailContainerTop);
        thumbnailItems[i].transform.SetAsLastSibling();
      }
      else
      {
        thumbnailItems[i].transform.SetParent(thumbnailContainerBottom);
        thumbnailItems[i].transform.SetAsLastSibling();

      }

      if (thumbnailItems[i].GetVisibility()) visibleCount++;
    }
  }

  private int GetVisibleIndex(int index)
  {
    int visibleIndex = 0;
    for (int i = 0; i < index; i++)
    {
      if (thumbnailItems[i].GetVisibility()) visibleIndex++;
    }

    return visibleIndex;
  }

  private int GetGridColumnCount()
  {
    return Mathf.FloorToInt(((thumbnailContainerTop.sizeDelta.x - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right) + gridLayoutGroup.spacing.x) / (gridLayoutGroup.cellSize.x + gridLayoutGroup.spacing.x));
  }

}
