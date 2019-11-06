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

using System.Collections.Generic;
#if USE_STEAMWORKS
using Steamworks;
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
#endif
using UnityEngine;

public class FeaturedWorkshopItems : MonoBehaviour
{
#if USE_STEAMWORKS
  // https://steamcommunity.com/sharedfiles/filedetails/?id=1569305489
  public static PublishedFileId_t[] FeaturedPublishedFileIds = new PublishedFileId_t[] {
    new PublishedFileId_t(1790543051),
    new PublishedFileId_t(1790375733),
    new PublishedFileId_t(1809912386)
  };

  List<WorkshopItem> items = new List<WorkshopItem>();

  bool waitingOnQuery = true;

  GameBuilderSceneController scenes;

  void Awake()
  {
    Util.FindIfNotSet(this, ref scenes);
  }

  // TODO make this share code with SteamUtil
  System.Collections.IEnumerator Start()
  {
    while (!SteamManager.Initialized)
    {
      yield return null;
    }
    UGCQueryHandle_t queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(FeaturedPublishedFileIds, (uint)FeaturedPublishedFileIds.Length);
    SteamWorkshopMain.Instance.Execute<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(queryHandle), OnQueryCompleted);
  }

  private void OnQueryCompleted(SteamUGCQueryCompleted_t p_callback, bool p_bIOFailure)
  {
    waitingOnQuery = false;
    items.Clear();

    if (p_bIOFailure || p_callback.m_eResult != EResult.k_EResultOK)
    {
      Util.LogError($"Failed to query featured workshop items. Result code: {p_callback.m_eResult}");
      return;
    }

    for (uint i = 0; i < p_callback.m_unNumResultsReturned; i++)
    {
      SteamUGCDetails_t itemDetails;
      if (SteamUGC.GetQueryUGCResult(p_callback.m_handle, i, out itemDetails))
      {
        if (itemDetails.m_eResult != Steamworks.EResult.k_EResultOK)
        {
          Util.LogError($"Something is wrong with featured file ID {FeaturedPublishedFileIds[i]} - ignoring.");
        }
        else
        {
          WorkshopItem item = SteamWorkshopMain.Instance.RegisterQueryResultItem(p_callback.m_handle, i, itemDetails);
          items.Add(item);
          // Util.Log($"got WorkshopItem for featured ID {item.SteamNative.m_nPublishedFileId}, name is {item.Name} {item.PreviewImageURL}. sub'd? {item.IsSubscribed}");
        }
      }
      else
      {
        Util.LogError($"Could not query details of featured file ID {FeaturedPublishedFileIds[i]} - ignoring.");
      }
    }
  }

  // NOTE: These items will probably not reflect the full state of a normal
  // SteamWorkShopMain item. For example, their IsFavorited state should
  // probably not be trusted.
  public ICollection<WorkshopItem> GetItems()
  {
    return items;
  }

  public bool IsWaitingOnQuery()
  {
    return waitingOnQuery;
  }

#endif
}