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

#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
using Steamworks;
#endif
using System.Collections;
using System.Collections.Generic;
using GameBuilder;
using UnityEngine;

public class CardPackWorkshopMenu : ThumbnailMenu
{
  [SerializeField] protected GameObject pageObject;
  [SerializeField] protected TMPro.TextMeshProUGUI pageText;
  [SerializeField] protected UnityEngine.UI.Button firstPage;
  [SerializeField] protected UnityEngine.UI.Button lastPage;
  [SerializeField] protected UnityEngine.UI.Button nextPage;
  [SerializeField] protected UnityEngine.UI.Button prevPage;
  [SerializeField] protected ThumbnailItem cardPackThumbnailPrefab;
  [SerializeField] protected GameObject cardPackDetailPrefab;

#if USE_STEAMWORKS
  WorkshopSort workshopSort;
  CardPackDetail cardPackDetail;

  public enum WorkshopSort
  {
    MostVotes,
    Newest,
    Trending
  };

  protected uint curPage = 1;
  protected uint pageCount = 1;

  public override void Setup()
  {
    base.Setup();
    searchField.onEndEdit.AddListener(OnSearch);
    searchClear.onClick.AddListener(ClearSearch);

    firstPage.onClick.AddListener(FirstPage);
    lastPage.onClick.AddListener(LastPage);
    nextPage.onClick.AddListener(NextPage);
    prevPage.onClick.AddListener(PreviousPage);

    SetOpenEvent(CreateThumbnails);
    AddCategory(/* name */ null, /* placeholderText */ "");

    cardPackDetail = Instantiate(cardPackDetailPrefab).GetComponent<CardPackDetail>();
  }

  public override void Open()
  {
    sortText.text = workshopSort.ToString();
    base.Open();
  }

  void OnSearch(string searchString)
  {
    if (searchString != GetSearchString())
    {
      SetSearchString(searchString);
      ClearThumbnails();
      SteamWorkshopMain.Instance.SearchText = searchString;
      CreateSteamThumbnails();
    }
  }

  protected void CreateThumbnails()
  {
    CreateSteamThumbnails(curPage);
  }

  void OnInputFieldEnd(string searchString)
  {
    if (Input.GetButtonDown("Submit"))
    {
      OnSearch(searchString);
    }
  }

  void ClearSearch()
  {
    searchField.text = "";
    OnSearch("");
  }

  EUGCQuery GetSortMode()
  {
    switch (workshopSort)
    {
      case (WorkshopSort.MostVotes):
        return EUGCQuery.k_EUGCQuery_RankedByVote;
      case (WorkshopSort.Trending):
        return EUGCQuery.k_EUGCQuery_RankedByTrend;
      default:
        return EUGCQuery.k_EUGCQuery_RankedByPublicationDate;
    }
  }

  void CreateSteamThumbnails(uint page = 1)
  {
    if (!SteamManager.Initialized)
    {
      Debug.Log($"Steam Workshop is not available right now. Did you launch from Steam?");
      return;
    }

    workingFeedback.SetActive(true);
    noResultsFeedback.SetActive(false);

    // TODO need to edit SteamWorkshopMain to use SetExcludeTags and exclude Assets
    SteamWorkshopMain.Instance.ResetItemSearch();
    SteamWorkshopMain.Instance.m_excludeTags.Clear();
    SteamWorkshopMain.Instance.SearchTags.Add(SteamUtil.GameBuilderTags.CardPack.ToString());
    SteamWorkshopMain.Instance.Sorting = new WorkshopSortMode(GetSortMode());
    SteamWorkshopMain.Instance.GetItemList(curPage, OnWorkshopListLoaded);
  }

  protected override void Update()
  {
    base.Update();
    pageText.text = $"Page {curPage} of {pageCount}";
  }

  void OnWorkshopListLoaded(WorkshopItemListEventArgs args)
  {
    if (args.IsError)
    {
      Debug.Log($"Sorry, encountered an error: {args.ErrorMessage}");
      return;
    }

    pageCount = args.ItemList.PagesItems;

    workingFeedback.SetActive(false);

    if (args.ItemList.Items.Count == 0)
    {

      noResultsFeedback.SetActive(true);
    }

    foreach (WorkshopItem item in args.ItemList.Items)
    {
      if (item.Name.IsNullOrEmpty())
      {
        continue;
      }
      ThumbnailItem thumbnailItem = Instantiate(cardPackThumbnailPrefab).GetComponent<ThumbnailItem>();
      // TEMP: Ignore workshop thumbnail texture for now since it's ugly
      // if (item.PreviewImageURL != null && item.PreviewImageURL.Length > 0)
      // {
      //   thumbnailItem.SetThumbnailUrl(item.PreviewImageURL);
      // }
      // else
      // {
      thumbnailItem.SetThumbnail(placeholderThumbnailTexture);
      // }

      thumbnailItem.SetName(item.Name);
      thumbnailItem.OnClick = () => OpenWorkshopEntry(thumbnailItem, item);
      thumbnailItem.GetDescription = () => { return item.Description; };
      AddThumbnail(thumbnailItem);
    }
  }

  void OpenWorkshopEntry(ThumbnailItem thumbnailItem, WorkshopItem item)
  {
    SelectThumbnail(thumbnailItem, (rect) =>
    {
      cardPackDetail.FitTo(rect);
      cardPackDetail.Open(thumbnailItem.GetTexture(), item);
    });
  }

  protected override void ChangeSort(Direction direction)
  {
    workshopSort = (WorkshopSort)(((int)workshopSort + 1) % 3);
    sortText.text = workshopSort.ToString();
    ClearThumbnails();
    CreateSteamThumbnails();
  }

  void NextPage()
  {
    if ((curPage + 1) <= pageCount)
    {
      curPage++;
      ClearThumbnails();
      CreateThumbnails();
    }
  }

  void PreviousPage()
  {
    if ((curPage - 1) >= 1)
    {
      curPage--;
      ClearThumbnails();
      CreateThumbnails();
    }
  }

  void FirstPage()
  {
    curPage = 1;
    ClearThumbnails();
    CreateThumbnails();
  }

  void LastPage()
  {
    curPage = pageCount;
    ClearThumbnails();
    CreateThumbnails();
  }
#else
  protected override void ChangeSort(Direction direction)
  {
    throw new System.NotImplementedException();
  }
#endif
}
