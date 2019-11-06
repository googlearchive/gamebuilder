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

namespace GameBuilder
{
  public static class SteamUtil
  {
    public enum GameBuilderTags
    {
      Project,
      Asset,
      WAV,
      FBX,
      AssetBundle,
      CardPack,
      Actor,
      Image,
      BlockStyle
    }

    public const string WorkshopItemInfoFileName = "WorkshopItemInfo.xml";

#if USE_STEAMWORKS

    public delegate void WorkShopItemHandler(Util.Maybe<WorkshopItem> item);

    public static ulong GetId(this WorkshopItem item)
    {
      return item.SteamNative.m_nPublishedFileId.m_PublishedFileId;
    }

    // TODO make this share code with FeaturedWorkshopItems
    public static void GetWorkShopItem(ulong publishedFileId, WorkShopItemHandler handleItem)
    {
      PublishedFileId_t[] fileIds = new PublishedFileId_t[] { new PublishedFileId_t(publishedFileId) };
      UGCQueryHandle_t queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(fileIds, (uint)fileIds.Length);

      Steamworks.CallResult<Steamworks.SteamUGCQueryCompleted_t>.APIDispatchDelegate onQueryCompleted = (SteamUGCQueryCompleted_t p_callback, bool p_bIOFailure) =>
      {
        OnQueryCompleted(p_callback, p_bIOFailure, handleItem, publishedFileId);
      };
      SteamWorkshopMain.Instance.Execute<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(queryHandle), onQueryCompleted);
    }

    private static void OnQueryCompleted(SteamUGCQueryCompleted_t p_callback, bool p_bIOFailure, WorkShopItemHandler handleItem, ulong requestedFileId)
    {
      if (p_bIOFailure || p_callback.m_eResult != EResult.k_EResultOK)
      {
        Util.LogError($"Failed to query workshop item {requestedFileId}. Result code: {p_callback.m_eResult}");
        handleItem(Util.Maybe<WorkshopItem>.CreateEmpty());
        return;
      }

      // There should just be 1..but doesn't hurt.
      for (uint i = 0; i < p_callback.m_unNumResultsReturned; i++)
      {
        SteamUGCDetails_t itemDetails;
        if (SteamUGC.GetQueryUGCResult(p_callback.m_handle, i, out itemDetails))
        {
          if (itemDetails.m_eResult != Steamworks.EResult.k_EResultOK)
          {
            Util.LogError($"Something is wrong with workshop item {requestedFileId} - returning empty. Result code: {itemDetails.m_eResult}");
            handleItem(Util.Maybe<WorkshopItem>.CreateEmpty());
          }
          else
          {
            WorkshopItem item = SteamWorkshopMain.Instance.RegisterQueryResultItem(p_callback.m_handle, i, itemDetails);
            handleItem(Util.Maybe<WorkshopItem>.CreateWith(item));
          }
        }
        else
        {
          Util.LogError($"Could not query details of workshop item {requestedFileId} - returning empty. Result code: {itemDetails.m_eResult}");
          handleItem(Util.Maybe<WorkshopItem>.CreateEmpty());
        }
      }
    }

    public static void QueryWorkShopItemVisibility(ulong publishedFileId, System.Action<Util.Maybe<bool>> handleResult)
    {
      PublishedFileId_t[] fileIds = new PublishedFileId_t[] { new PublishedFileId_t(publishedFileId) };
      UGCQueryHandle_t queryHandle = SteamUGC.CreateQueryUGCDetailsRequest(fileIds, (uint)fileIds.Length);

      Steamworks.CallResult<Steamworks.SteamUGCQueryCompleted_t>.APIDispatchDelegate onQueryCompleted = (SteamUGCQueryCompleted_t p_callback, bool p_bIOFailure) =>
      {
        OnVisibilityQueryCompleted(p_callback, p_bIOFailure, handleResult, publishedFileId);
      };
      SteamWorkshopMain.Instance.Execute<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(queryHandle), onQueryCompleted);
    }


    private static void OnVisibilityQueryCompleted(SteamUGCQueryCompleted_t p_callback, bool p_bIOFailure, System.Action<Util.Maybe<bool>> handleResult, ulong requestedFileId)
    {
      if (p_bIOFailure || p_callback.m_eResult != EResult.k_EResultOK)
      {
        Util.LogError($"Failed to query workshop item {requestedFileId}. Result code: {p_callback.m_eResult}");
        handleResult(Util.Maybe<bool>.CreateEmpty());
        return;
      }

      // There should just be 1..but doesn't hurt.
      for (uint i = 0; i < p_callback.m_unNumResultsReturned; i++)
      {
        SteamUGCDetails_t itemDetails;
        if (SteamUGC.GetQueryUGCResult(p_callback.m_handle, i, out itemDetails))
        {
          if (itemDetails.m_eResult != Steamworks.EResult.k_EResultOK)
          {
            Util.LogError($"Something is wrong with workshop item {requestedFileId} - returning empty. Result code: {itemDetails.m_eResult}");
            handleResult(Util.Maybe<bool>.CreateEmpty());
          }
          else
          {
            bool isPublicyVisible = itemDetails.m_eVisibility == Steamworks.ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
            handleResult(Util.Maybe<bool>.CreateWith(isPublicyVisible));
          }
        }
        else
        {
          Util.LogError($"Could not query details of workshop item {requestedFileId} - returning empty. Result code: {itemDetails.m_eResult}");
          handleResult(Util.Maybe<bool>.CreateEmpty());
        }
      }
    }
#endif
  }
}