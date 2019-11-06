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
using GameBuilder;
using System.IO;
using System.Linq;
using System;
#if USE_STEAMWORKS
using LapinerTools.Steam;
using LapinerTools.Steam.Data;
using Steamworks;
#endif
using CT = CommandTerminal;

public class WorkshopAssetSource : MonoBehaviour
{
  public delegate float GetUploadProgress();

#if USE_STEAMWORKS
  static string[] VisibleTypeTags =
  {
    SteamUtil.GameBuilderTags.CardPack.ToString().ToLowerInvariant(),
    SteamUtil.GameBuilderTags.Project.ToString().ToLowerInvariant(),
    SteamUtil.GameBuilderTags.Actor.ToString().ToLowerInvariant(),
  };

  public const string CARD_PACK_ICON_PATH = "BehaviorLibrary/WorkshopCardPack.png";

  // NOTE: By "cache" we mean the Steam-less cache. Of course Steam has its own
  // cache, stored in the user's Steam directory, but we need this cache to work
  // without Steam running.
  string cacheDirectory = null;

  HashSet<ulong> requestedItemIds = new HashSet<ulong>();

  void Awake()
  {
    // MUST do this regardless of SteamManager.Initialized - the whole point is
    // for this to work without it.
    cacheDirectory = GameBuilderApplication.CurrentGameOptions.workshopAssetCacheDir;

    // SteamManager needs to be kicked to be initialized...
    if (!SteamManager.Initialized)
    {
      Util.Log($"SteamWorkshop not available.");
      return;
    }

    SteamWorkshopMain.Instance.OnError += HandleWorkshopError;

  }

  void HandleWorkshopError(LapinerTools.Steam.Data.ErrorEventArgs args)
  {
    Util.LogError($"Steam Workshop error: {args.ErrorMessage}");
  }

  IEnumerator GetRoutine(ulong fileId, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress, System.Action<WorkshopItem> handleItem)
  {
    yield return null;

    // In the non-Steam cache?
    string cachedItemDir = Path.Combine(cacheDirectory ?? "", fileId.ToString());
    Util.Log($"Looking for cached {cachedItemDir} - cacherootDir: {cacheDirectory}");
    if (cacheDirectory != null && Directory.Exists(cachedItemDir))
    {
      Util.Log($"OK load asset from cache: {cachedItemDir}");
      onComplete(Util.Maybe<string>.CreateWith(cachedItemDir));
      yield break;
    }

    if (!SteamManager.Initialized)
    {
      onComplete(Util.Maybe<string>.CreateError("Steam Workshop not available. Are you logged in? Maybe it's down?"));
      yield break;
    }
    SteamUtil.GetWorkShopItem(fileId, maybeItem => GetWorkshopHandler(maybeItem, onComplete, onProgress, handleItem));
  }

  // 'onComplete' will be called with the local directory path of the downloaded
  // item, or the Maybe will contain an error message.
  // 'handleItem' is only called if the item is actually downloaded from Steam
  // (ie. not cached.)
  public void Get(ulong fileId, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress = null, System.Action<WorkshopItem> handleItem = null)
  {
    requestedItemIds.Add(fileId);
    StartCoroutine(GetRoutine(fileId, onComplete, onProgress, handleItem));
  }

  IEnumerator DownloadRoutine(WorkshopItem item, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress)
  {
    float t0 = Time.realtimeSinceStartup;
    while (true)
    {
      onProgress?.Invoke(SteamWorkshopMain.Instance.GetDownloadProgress(item));

      Util.Log($"Downloading work shop item {item.GetId()}.. progress: {SteamWorkshopMain.Instance.GetDownloadProgress(item)}. Still downloading? {item.IsDownloading}");

      //if (!item.IsSubscribed)
      //{
      //  onComplete(Util.Maybe<string>.CreateError($"Workshop item {item.GetId()} was requested for download but not subscribed?"));
      //  yield break;
      // }

      if (item.IsInstalled)
      {
        if (item.InstalledLocalFolder == null)
        {
          onComplete(Util.Maybe<string>.CreateError($"Workshop item {item.GetId()} was installed but with no local folder?"));
          yield break;
        }
        else
        {
          onComplete(Util.Maybe<string>.CreateWith(item.InstalledLocalFolder));
          yield break;
        }
      }

      if (Time.realtimeSinceStartup - t0 > 300f)
      {
        onComplete(Util.Maybe<string>.CreateError($"Workshop item {item.GetId()} has been downloading for over 5 minutes - giving up."));
      }

      // IMPORTANT: IsInstalled and IsDownloading can be set at the same time so we should always check
      // IsInstalled first before deciding that we failed.
      if (!item.IsDownloading)
      {
        // OK we failed some how.
        onComplete(Util.Maybe<string>.CreateError($"Workshop item {item.GetId()} failed to download. It may be missing."));
        yield break;
      }

      yield return new WaitForSeconds(1f);
    }
  }

  void GetWorkshopHandler(Util.Maybe<WorkshopItem> maybeItem, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress, System.Action<WorkshopItem> handleItem)
  {
    if (maybeItem.IsEmpty())
    {
      onComplete(Util.Maybe<string>.CreateError(maybeItem.GetErrorMessage()));
    }
    else
    {
      var item = maybeItem.Value;
      if (!item.IsInstalled)
      {
        // The only way we know how to install something is to subscribe to it..
        SteamWorkshopMain.Instance.Subscribe(item, args =>
        {
          handleItem?.Invoke(item);
          StartCoroutine(DownloadRoutine(item, onComplete, onProgress));
        });
      }
      else
      {
        Debug.Assert(item.InstalledLocalFolder != null, $"Workshop item was IsInstalled but LocalFolder was null?");
        onComplete(Util.Maybe<string>.CreateWith(maybeItem.Value.InstalledLocalFolder));
      }
    }
  }

  IEnumerator PutRoutine(
    string contentPath,
    string name,
    string description,
    SteamUtil.GameBuilderTags typeTag,
    string thumbnailPath,
    PublishedFileId_t? fileIdToUpdate,
    System.Action<Util.Maybe<ulong>> onComplete,
    System.Action<GetUploadProgress> onStatus = null)
  {
    yield return null;

    if (!SteamManager.Initialized)
    {
      onComplete(Util.Maybe<ulong>.CreateError("Steam Workshop not available. Are you logged in? Maybe it's down?"));
      yield break;
    }

    WorkshopItemUpdate updateInfo = null;

    // NOTE: If we want to update the item, we can just remember the old file ID. Not sure how best to do this...we'd have to remember who the item belongs to..?
    // if (File.Exists(Path.Combine(contentPath, SteamUtil.WorkshopItemInfoFileName)))
    // {
    //   uploadItem = SteamWorkshopMain.Instance.GetItemUpdateFromFolder(contentPath);
    // }

    updateInfo = new WorkshopItemUpdate();
    updateInfo.Name = name;
    updateInfo.Description = description;
    updateInfo.ContentPath = contentPath;
    updateInfo.IconPath = thumbnailPath;
    updateInfo.Tags.Add(SteamUtil.GameBuilderTags.Asset.ToString());
    updateInfo.Tags.Add(typeTag.ToString());
    if (fileIdToUpdate != null)
    {
      updateInfo.SteamNative.m_nPublishedFileId = fileIdToUpdate.Value;
    }
    if (updateInfo.IconPath == null && typeTag == SteamUtil.GameBuilderTags.CardPack)
    {
      updateInfo.IconPath = Path.Combine(
        Util.GetConcretePath(Util.AbstractLocation.StreamingAssets), CARD_PACK_ICON_PATH);
    }
    // uploadItem.IconPath // TODO
    SteamWorkshopMain.Instance.Upload(updateInfo, (args) => UploadCallback(args, onComplete));
    onStatus?.Invoke(() =>
    {
      return SteamWorkshopMain.Instance.GetUploadProgress(updateInfo);
    });
  }


  public void Put(
    string contentPath,
    string name,
    string description,
    SteamUtil.GameBuilderTags typeTag,
    string thumbnailPath,
    PublishedFileId_t? fileIdToUpdate,
    System.Action<Util.Maybe<ulong>> onComplete,
    System.Action<GetUploadProgress> onStatus = null)
  {
    StartCoroutine(PutRoutine(contentPath, name, description, typeTag, thumbnailPath, fileIdToUpdate, onComplete, onStatus));
  }

  void UploadCallback(WorkshopItemUpdateEventArgs args, System.Action<Util.Maybe<ulong>> onComplete)
  {
    if (args.IsError)
    {
      onComplete(Util.Maybe<ulong>.CreateError(args.ErrorMessage));
    }
    else
    {
      bool makePrivate = !args.Item.Tags.Any(tag => VisibleTypeTags.Contains(tag.ToLowerInvariant()));
      PerformWorkshopVisibilityCheck(args.Item.SteamNative.m_nPublishedFileId.m_PublishedFileId, onComplete, makePrivate);
    }
  }

  void PerformWorkshopVisibilityCheck(ulong fileId, System.Action<Util.Maybe<ulong>> onComplete, bool makePrivate)
  {
    GameBuilder.SteamUtil.QueryWorkShopItemVisibility(fileId, maybeVisible =>
    {
      if (maybeVisible.IsEmpty() || maybeVisible.Value == false)
      {
        onComplete(Util.Maybe<ulong>.CreateError($"Item was uploaded with ID {fileId}, but we could not look it up afterwards. Are you sure you have agreed to the Steam Subscriber Agreement?"));
        return;
      }
      else
      {
        // All goooood
        onComplete(Util.Maybe<ulong>.CreateWith(fileId));

        if (makePrivate)
        {
          UGCUpdateHandle_t itemUpdateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), new Steamworks.PublishedFileId_t(fileId));
          SteamUGC.SetItemVisibility(itemUpdateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
          SteamUGC.SubmitItemUpdate(itemUpdateHandle, "hide asset");
        }
      }
    });
  }

  [CT.RegisterCommand(Help = "")]
  static void CommandWorkshopTest(CT.CommandArg[] args)
  {
    var source = FindObjectOfType<WorkshopAssetSource>();
    var dir = Util.CreateTempDirectory();
    File.WriteAllText(Path.Combine(dir, "hello.txt"), "hellllo world");
    source.Put(dir, "test from console", "this is just a test", SteamUtil.GameBuilderTags.FBX, null, null, maybeId =>
    {
      GameBuilderConsoleCommands.Log($"Put result: {maybeId}");

      if (!maybeId.IsEmpty())
      {
        source.Get(maybeId.Value, maybePath =>
        {
          GameBuilderConsoleCommands.Log($"Get result: {maybePath}");
          if (!maybePath.IsEmpty())
          {
            var actual = File.ReadAllText(Path.Combine(maybePath.Value, "hello.txt"));
            GameBuilderConsoleCommands.Log($"actual contents: {actual}");
          }
        });
      }
    });
  }

  public void SetCacheDirectory(string cacheDir)
  {
    this.cacheDirectory = cacheDir;
    Util.Log($"OK workshop assets will be read from {this.cacheDirectory} if a match exists.");
  }

  public void CacheAllRequestedInto(string cacheDir)
  {
    foreach (ulong itemId in requestedItemIds)
    {
      CacheItemTo(itemId, cacheDir);
    }
  }

  // 'itemContentDir' should be the same dir path you got from Get
  public void CacheItemTo(ulong itemId, string cacheDir)
  {
    this.Get(itemId, maybeDir =>
    {
      if (!maybeDir.IsEmpty())
      {
        string srcItemDir = maybeDir.Get();
        string destItemDir = Path.Combine(cacheDir, itemId.ToString());
        Directory.CreateDirectory(destItemDir);
        Util.CopyDirectoryRecursive(srcItemDir, destItemDir);
      }
    });
  }
#else

  // This is a simple, local disk implementation of the WorkshopAssetSource
  // interface. Just a directory of ID-named subdirs.

  string tempDir = null;

  void OnEnable()
  {
    tempDir = Util.CreateTempDirectory();
  }

  void OnDisable()
  {
    System.IO.Directory.Delete(tempDir, true);
  }

  IEnumerator CompleteRoutine(ulong id, System.Action<Util.Maybe<ulong>> onComplete)
  {
    yield return null;
    onComplete.Invoke(Util.Maybe<ulong>.CreateWith(id));
  }

  public void Put(
    string contentDir,
    string name,
    string description,
    SteamUtil.GameBuilderTags typeTag,
    System.Action<Util.Maybe<ulong>> onComplete,
    System.Action<GetUploadProgress> onStatus = null)
  {
    ulong id = 0;
    while (Directory.Exists(Path.Combine(tempDir, id.ToString()))) id++;
    string entryDir = Path.Combine(tempDir, id.ToString());
    Directory.CreateDirectory(entryDir);
    Util.CopyDirectoryRecursive(contentDir, entryDir);

    Util.Log($"Wrote all to {entryDir}");
    if (onComplete != null)
    {
      StartCoroutine(CompleteRoutine(id, onComplete));
    }
  }

  public void Put(
  string contentPath,
  string name,
  string description,
  SteamUtil.GameBuilderTags typeTag,
  string ignored,
  object ignored2,
  System.Action<Util.Maybe<ulong>> onComplete,
  System.Action<GetUploadProgress> onStatus = null)
  {
    Put(contentPath, name, description, typeTag, onComplete, onStatus);
  }

  IEnumerator GetRoutine(ulong fileId, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress)
  {
    yield return null;
    string entryDir = Path.Combine(tempDir, fileId.ToString());
    if (Directory.Exists(entryDir))
    {
      onComplete(Util.Maybe<string>.CreateWith(entryDir));
    }
    else
    {
      onComplete(Util.Maybe<string>.CreateEmpty());
    }
  }

  public void Get(ulong fileId, System.Action<Util.Maybe<string>> onComplete, System.Action<float> onProgress = null)
  {
    StartCoroutine(GetRoutine(fileId, onComplete, onProgress));
  }

  public void Save(string dir)
  {
    Directory.CreateDirectory(dir);
    Util.CopyDirectoryRecursive(tempDir, dir);
  }

  public void Load(string dir)
  {
    if (Directory.Exists(dir))
    {
      Util.CopyDirectoryRecursive(dir, tempDir);
    }
  }

#endif
}
