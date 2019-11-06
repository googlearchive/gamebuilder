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
using Steamworks;
#endif

using GameBuilder;
using System.Linq;

public class WorkshopMenu : ThumbnailMenu
{
  [SerializeField] protected GameObject gameThumbnailPrefab;
  [SerializeField] protected GameDetail gameDetailPrefab;
  [SerializeField] protected GameObject pageObject;
  [SerializeField] protected TMPro.TextMeshProUGUI pageText;
  [SerializeField] protected UnityEngine.UI.Button firstPage;
  [SerializeField] protected UnityEngine.UI.Button lastPage;
  [SerializeField] protected UnityEngine.UI.Button nextPage;
  [SerializeField] protected UnityEngine.UI.Button prevPage;

#if USE_STEAMWORKS

  WorkshopSort workshopSort;
  GameDetail gameDetail;

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

    SetSort(WorkshopSort.Trending);

    // SetOpenEvent(CreateThumbnails);
    AddCategory(/* name */ null, /* placeholderText */ "");

    gameDetail = Instantiate(gameDetailPrefab).GetComponent<GameDetail>();
  }

  public override void Open()
  {
    sortText.text = workshopSort.ToString();
    gettingItemList = false;
    base.Open();
  }

  void OnSearch(string searchString)
  {
    if (searchString != GetSearchString())
    {
      SetSearchString(searchString);
      ClearThumbnails();
      CreateSteamThumbnails();
    }
  }

  protected void CreateThumbnails()
  {
    CreateSteamThumbnails();
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

  void CreateSteamThumbnails()
  {
    itemListData = new ItemListData()
    {
      sort = workshopSort,
      searchString = currentSearchString,
      page = curPage
    };

    if (gettingItemList) return;

    if (!SteamManager.Initialized)
    {
      Debug.Log($"Steam Workshop is not available right now. Did you launch from Steam?");
      return;
    }

    workingFeedback.SetActive(true);
    noResultsFeedback.SetActive(false);

    // TODO need to edit SteamWorkshopMain to use SetExcludeTags and exclude Assets

    SteamWorkshopMain.Instance.ResetItemSearch();
    SteamWorkshopMain.Instance.SearchText = currentSearchString;
    SteamWorkshopMain.Instance.m_excludeTags.Add(SteamUtil.GameBuilderTags.Asset.ToString());
    SteamWorkshopMain.Instance.Sorting = new WorkshopSortMode(GetSortMode());

    itemListData = new ItemListData()
    {
      sort = workshopSort,
      searchString = currentSearchString,
      page = curPage
    };

    ItemListData data = itemListData;

    gettingItemList = true;
    SteamWorkshopMain.Instance.GetItemList(curPage, (args) => OnWorkshopListLoaded(args, data));
  }
  bool gettingItemList = false;
  struct ItemListData
  {
    public WorkshopSort sort;
    public string searchString;
    public uint page;

    public bool Equals(ItemListData data)
    {
      return sort == data.sort && searchString == data.searchString && page == data.page;
    }

    public override string ToString()
    {
      return sort + " " + searchString + " " + page;
    }
  }

  ItemListData itemListData;

  protected override void Update()
  {
    base.Update();
    pageText.text = $"Page {curPage} of {pageCount}";
  }

  void OnWorkshopListLoaded(WorkshopItemListEventArgs args, ItemListData data)
  {
    gettingItemList = false;

    if (args.IsError)
    {
      Debug.Log($"Sorry, encountered an error: {args.ErrorMessage}");
      return;
    }

    if (!itemListData.Equals(data))
    {
      Invoke("CreateSteamThumbnails", .1f);
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
      gameThumbnail.GetDescription = () => { return item.Description; };
      AddThumbnail(gameThumbnail);
    }
  }

  void OpenWorkshopEntry(GameThumbnail gameThumbnail, WorkshopItem item)
  {
    SelectThumbnail(gameThumbnail, (rect) =>
    {
      gameDetail.FitTo(rect);
      gameDetail.OpenWorkshop(gameThumbnail.GetTexture(), item);
    });
  }

  protected override void ChangeSort(Direction direction)
  {
    workshopSort = (WorkshopSort)(((int)workshopSort + 1) % 3);
    sortText.text = workshopSort.ToString();
    ClearThumbnails();
    CreateSteamThumbnails();
  }

  public void SetSort(WorkshopSort sort)
  {
    workshopSort = sort;
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
