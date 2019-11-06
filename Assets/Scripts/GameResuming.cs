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
using System.Collections;
using UnityEngine;
using GameBuilder;
#if USE_STEAMWORKS
using LapinerTools.Steam.Data;
#endif

public class GameResuming : MonoBehaviour
{
  public interface ResumeOptionHandler
  {
#if USE_STEAMWORKS
    void HandleWorkshopItem(WorkshopItem item);
#endif
    void HandleBundleId(string bundleId);
    void HandleJoinCode(string joinCode);
  }

  const string ResumeInfoPlayerPrefKey = "f0fc62ba-resumeInfo";

  [SerializeField] GameBundleLibrary localLibrary;

  [System.Serializable]
  struct ResumeInfo
  {
    // Whichever is a non-default value should be assumed to be the one to use.
    public string bundleId;
    public ulong steamWorkshopFileId;
  }

  public void SetBundleForResuming(string bundleId)
  {
    string prefString = JsonUtility.ToJson(new ResumeInfo { bundleId = bundleId });
    PlayerPrefs.SetString(ResumeInfoPlayerPrefKey, prefString);
    PlayerPrefs.Save();
  }

  public void SetWorkshopItemForResuming(ulong workshopId)
  {
    string prefString = JsonUtility.ToJson(new ResumeInfo { steamWorkshopFileId = workshopId });
    PlayerPrefs.SetString(ResumeInfoPlayerPrefKey, prefString);
    PlayerPrefs.Save();
  }

  public void QueryResumeInfo(ResumeOptionHandler handler)
  {
    // Make it always async, even for immediate returns.
    StartCoroutine(QueryResumeInfoCoroutine(handler));
  }

  IEnumerator QueryResumeInfoCoroutine(ResumeOptionHandler handler)
  {
    if (PlayerPrefs.HasKey(NetworkingController.LastJoinedRoomPrefKey)
      && !PlayerPrefs.GetString(NetworkingController.LastJoinedRoomPrefKey).IsNullOrEmpty())
    {
      handler.HandleJoinCode(PlayerPrefs.GetString(NetworkingController.LastJoinedRoomPrefKey));
      yield break;
    }

    if (!PlayerPrefs.HasKey(ResumeInfoPlayerPrefKey))
    {
      yield break;
    }

    // Hmm ideally this is done in GetWorkShopItem, but I need to make that a MonoBehaviour.
#if USE_STEAMWORKS
    while (!SteamManager.Initialized)
    {
      yield return null;
    }
#endif

    ResumeInfo info = JsonUtility.FromJson<ResumeInfo>(PlayerPrefs.GetString(ResumeInfoPlayerPrefKey));
#if USE_STEAMWORKS
    if (info.steamWorkshopFileId != 0)
    {
      SteamUtil.GetWorkShopItem(info.steamWorkshopFileId, item =>
      {
        if (!item.IsEmpty())
        {
          handler.HandleWorkshopItem(item.Value);
        }
      });
      yield break;
    }
    else
#endif
    if (info.bundleId != null)
    {
      handler.HandleBundleId(info.bundleId);
      yield break;
    }
    else
    {
      Util.LogError($"resume pref key exists, but it didn't have valid info. Json: {PlayerPrefs.GetString(ResumeInfoPlayerPrefKey)}");
    }
  }
}