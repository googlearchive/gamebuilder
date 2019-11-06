// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using UnityEngine;
using PolyToolkitInternal.api_clients.poly_client;
using PolyToolkitInternal.client.model.util;
using PolyToolkitInternal.model.util;
using PolyToolkit;
using System.IO;
using System.Text;
using System.Collections;

namespace PolyToolkitInternal {
  /// <summary>
  /// Singleton MonoBehaviour for the library's internal use (not part of the library's API).
  /// </summary>
  [ExecuteInEditMode]
  public class PolyMainInternal : MonoBehaviour {
    /// <summary>
    /// Maximum cache age for immutable downloaded assets.
    /// Technically this could be infinity, since once downloaded the asset should always be the same, but
    /// we set it to a reasonable finite value to cover the case were we fix stuff on the server and
    /// want people to download the assets again after some time.
    /// </summary>
    private const long IMMUTABLE_ASSET_MAX_CACHE_AGE = 14 * 24 * 60 * 60 * 1000L;  // A fortnight.

    /// <summary>
    /// Maximum cache age for MUTABLE downloaded assets.
    /// Only the user's private assets are mutable (as the user can go into Blocks/TB and edit them).
    /// </summary>
    private const long MUTABLE_ASSET_MAX_CACHE_AGE = WebRequestManager.CACHE_NONE;

    /// <summary>
    /// A symbolic index into the resources of a fetch request indiciating the root file, as opposed to a resource.
    /// </summary>
    private const int ROOT_FILE_INDEX = -1;

    /// <summary>
    /// Delegate type for the progress callback of <see cref="FetchFormatFiles"/>
    /// </summary>
    /// <param name="asset">The asset for which we are fetching files.</param>
    /// <param name="progress">The progress of the operation (0.0 to 1.0).</param>
    public delegate void FetchProgressCallback(PolyAsset asset, float progress0to1);

    /// <summary>
    /// API key for use with the Poly server.
    /// </summary>
    public string apiKey { get; private set; }

    /// <summary>
    /// The web request manager, which handles any Poly web requests we need to make.
    /// </summary>
    public WebRequestManager webRequestManager { get; private set; }

    /// <summary>
    /// The client we use to communicate with the Poly.
    /// </summary>
    public PolyClient polyClient { get; private set; }

    /// <summary>
    /// Access token for Poly API access.
    /// This can be manually set by the user by calling Poly.SetAccessToken. If unset, we will get access tokens
    /// from Authenticator (default behaviour).
    /// </summary>
    private string manuallyProvidedAccessToken = null;

    /// <summary>
    /// Asynchronous importer responsible for importing GLTF in the background.
    /// </summary>
    private AsyncImporter asyncImporter = null;

    private static GameObject polyObject;
    private static PolyMainInternal instance;
    private BackgroundMain backgroundMain;

    public static PolyMainInternal Instance {
      get {
        PolyUtils.AssertNotNull(instance, "Can't call PolyMainInternal.Instance before PolyMainInternal.Init().");
        return instance;
      }
    }

    /// <summary>
    /// Returns whether or not initialization was done.
    /// </summary>
    public static bool IsInitialized { get { return instance != null; } }

    /// <summary>
    /// Initializes Poly Toolkit runtime. Must be called once, before any other use of the library.
    /// Call only once in your app's lifetime, not once per scene (Poly Toolkit survives scene loads).
    /// </summary>
    public static void Init(PolyAuthConfig? authConfig, PolyCacheConfig? cacheConfig) {
      string objName = Application.isPlaying ? "Poly Main" : "Poly Main (EDITOR)";
      PolyUtils.AssertTrue(instance == null, "PolyMainInternal.Init() already called. Can only be called once.");

      polyObject = PolyInternalUtils.CreateSingletonGameObject(objName);
      instance = polyObject.AddComponent<PolyMainInternal>();
      instance.Setup(authConfig, cacheConfig);
    }

    public static void Shutdown() {
      DestroyImmediate(polyObject);
      instance = null;
    }

    private void Setup(PolyAuthConfig? authConfig, PolyCacheConfig? cacheConfig) {
      authConfig = authConfig ?? PtSettings.Instance.authConfig;
      cacheConfig = cacheConfig ?? PtSettings.Instance.cacheConfig;

      // Check that the user actually set up their API key in authConfig, and didn't just leave the default
      // value in (the placeholder value starts with "**" so that's what we check for).
      if (string.IsNullOrEmpty(authConfig.Value.apiKey) || authConfig.Value.apiKey.StartsWith("**")) {
        throw new System.Exception("API Key not configured. Set your API key in in Poly Toolkit " +
            "Settings ('Runtime' section).");
      }

      this.apiKey = authConfig.Value.apiKey;
      backgroundMain = gameObject.AddComponent<BackgroundMain>();
      backgroundMain.Setup();
      webRequestManager = gameObject.AddComponent<WebRequestManager>();
      webRequestManager.Setup(cacheConfig.Value);
      polyClient = gameObject.AddComponent<PolyClient>();
      polyClient.Setup();
      asyncImporter = gameObject.AddComponent<AsyncImporter>();
      asyncImporter.Setup();
    }

    /// <summary>
    /// Sets a manually provided access token. Only use this if NOT using Authenticator. If this is not set,
    /// the token provided by the Authenticator module will be used by default.
    /// </summary>
    /// <param name="token"></param>
    public void SetAccessToken(string token) {
      this.manuallyProvidedAccessToken = token;
    }

    /// <summary>
    /// Gets the currently valid access token, or null if not available.
    /// </summary>
    public string GetAccessToken() {
      // Return the manually provided access token (priority) or the access token provided by Authenticator.
      return manuallyProvidedAccessToken ?? (Authenticator.IsInitialized ? Authenticator.Instance.AccessToken : null);
    }

    /// <summary>
    /// Performs the given work in the background thread.
    /// </summary>
    /// <param name="work">The work to perform.</param>
    public void DoBackgroundWork(BackgroundWork work) {
      backgroundMain.DoBackgroundWork(work);
    }

    /// <summary>
    /// As documented in PolyClient.ListAssets.
    /// </summary>
    public void ListAssets(PolyListAssetsRequest listAssetsRequest, PolyApi.ListAssetsCallback callback) {
      polyClient.SendRequest(listAssetsRequest, (PolyStatus status, PolyListAssetsResult polyListResult) => {
        if (status.ok) {
          ProcessRequestResult(polyListResult, callback);
        } else {
          callback(new PolyStatusOr<PolyListAssetsResult>(PolyStatus.Error(status, "Request failed")));
        }
      });
    }

    /// <summary>
    /// As documented in PolyClient.ListUserAssets.
    /// </summary>
    public void ListUserAssets(PolyListUserAssetsRequest listUserAssetsRequest, PolyApi.ListAssetsCallback callback) {
      // Users expect their own private assets to update quickly once they make a change (one use case
      // being: I go to Blocks or Tilt Brush, modify my asset, come back to PolyToolkit, I expect it to be updated).
      // So we don't use caching for these.
      polyClient.SendRequest(listUserAssetsRequest, (PolyStatus status, PolyListAssetsResult polyListResult) => {
        if (status.ok) {
          ProcessRequestResult(polyListResult, callback);
        } else {
          callback(new PolyStatusOr<PolyListAssetsResult>(PolyStatus.Error(status, "Request failed")));
        }
      }, /*maxCacheAge*/ WebRequestManager.CACHE_NONE);
    }

    /// <summary>
    /// As documented in PolyClient.ListUserAssets.
    /// </summary>
    public void ListLikedAssets(PolyListLikedAssetsRequest listLikedAssetsRequest, PolyApi.ListAssetsCallback callback) {
      polyClient.SendRequest(listLikedAssetsRequest, (PolyStatus status, PolyListAssetsResult polyListResult) => {
        if (status.ok) {
          ProcessRequestResult(polyListResult, callback);
        } else {
          callback(new PolyStatusOr<PolyListAssetsResult>(PolyStatus.Error(status, "Request failed")));
        }
      }, /*maxCacheAge*/ WebRequestManager.CACHE_NONE);
    }

    /// <summary>
    /// Fetch a specific Poly asset.
    /// </summary>
    /// <param name="id">The ID of the sought asset.</param>
    /// <param name="callback">The callback.</param>
    public void GetAsset(string id, PolyApi.GetAssetCallback callback) {
      polyClient.GetAsset(id, (PolyStatus status, PolyAsset result) => {
        if (status.ok) {
          callback(new PolyStatusOr<PolyAsset>(result));
        } else {
          callback(new PolyStatusOr<PolyAsset>(PolyStatus.Error(status, "Failed to get asset {0}", id)));
        }
      });
    }

    /// <summary>
    /// As documented in PolyApi.FetchThumbnails.
    /// </summary>
    public void FetchThumbnail(PolyAsset asset, PolyFetchThumbnailOptions options,
        PolyApi.FetchThumbnailCallback callback) {
      ThumbnailFetcher fetcher = new ThumbnailFetcher(asset, options, callback);
      // ThumbnailFetcher will handle fetching, converting and calling the callback.
      fetcher.Fetch();
    }

    /// <summary>
    /// As documented in PolyApi.ClearCache.
    /// </summary>
    public void ClearCache() {
      webRequestManager.ClearCache();
    }

    public void Import(PolyAsset asset, PolyImportOptions options, PolyApi.ImportCallback callback = null) {
      PolyFormat gltfFormat = asset.GetFormatIfExists(PolyFormatType.GLTF);
      PolyFormat gltf2Format = asset.GetFormatIfExists(PolyFormatType.GLTF_2);

      if (gltf2Format != null && gltfFormat == null) {
        FetchAndImportFormat(asset, gltf2Format, options, callback);
      } else if (gltfFormat != null) {
        FetchAndImportFormat(asset, gltfFormat, options, callback);
      } else {
          callback(asset, new PolyStatusOr<PolyImportResult>(
            PolyStatus.Error("Neither glTF or glTF_2 format was present in asset")));
      }
    }

    /// <summary>
    /// Checks if the asset has the contents of the format to import, fetching them if need be; then imports
    /// the asset.
    /// </summary>
    /// <param name="asset">The asset who's format is being imported.</param>
    /// <param name="format">The format to import.</param>
    /// <param name="options">The import options for this asset.</param>
    /// <param name="callback">The callback to call when this is finished.</param>
    private void FetchAndImportFormat(PolyAsset asset, PolyFormat format, PolyImportOptions options,
      PolyApi.ImportCallback callback = null) {
      if (format.root.contents != null) {
        // If asset already has the gltf package, proceed directly to importing it.
        ImportFormat(asset, format, options, callback);
      } else {
        // Otherwise, first fetch the package and then import the model.
        FetchFormatFiles(asset, format.formatType, (PolyAsset resultAsset, PolyStatus status) => {
          PolyFormat fetchedFormat = resultAsset.GetFormatIfExists(format.formatType);
          if (fetchedFormat != null) {
            ImportFormat(asset, fetchedFormat, options, callback);
          } else {
            if (callback != null) {
              callback(asset, new PolyStatusOr<PolyImportResult>(
                  PolyStatus.Error("Could not fetch format files for asset")));
            }
          }
        });
      }
    }

    /// <summary>
    /// Imports the relevant format and corrects that the designated glTF format if need be.
    /// </summary>
    private void ImportFormat(PolyAsset asset, PolyFormat format, PolyImportOptions options,
        PolyApi.ImportCallback callback) {
      asyncImporter.ImportAsync(asset, format, options,
          (PolyStatus status, GameObject root, IEnumerable meshCreator) => {
        if (!status.ok) {
          // Failed.
          callback(asset, new PolyStatusOr<PolyImportResult>(status));
          return;
        }
        PolyImportResult result = new PolyImportResult(root);
        result.mainThreadThrottler = meshCreator;
        callback(asset, new PolyStatusOr<PolyImportResult>(result));
      });
    }

    public void FetchFormatFiles(PolyAsset asset, PolyFormatType formatType,
        PolyApi.FetchFormatFilesCallback completionCallback, FetchProgressCallback progressCallback = null) {
      PolyUtils.AssertNotNull(asset, "Asset can't be null.");
      PolyUtils.AssertNotNull(formatType, "formatType can't be null.");
      PolyFormat packageToFetch = asset.GetFormatIfExists(formatType);
      if (packageToFetch == null) {
        if (completionCallback != null) {
          completionCallback(asset, PolyStatus.Error("Format type not present in asset"));
        }
        return;
      }

      PolyUtils.AssertNotNull(packageToFetch.root, "packageToFetch.root can't be null.");
      PolyUtils.AssertNotNull(packageToFetch.root.url, "packageToFetch.root.url can't be null.");
      PolyUtils.AssertNotNull(packageToFetch.resources, "packageToFetch.resources can't be null.");

      string accessToken = GetAccessToken();

      FetchOperationState state = new FetchOperationState();
      state.asset = asset;
      state.completionCallback = completionCallback;
      state.progressCallback = progressCallback;
      state.packageBeingFetched = packageToFetch;

      // Indicates how many files are pending download (1 for main file + 1 for each resource).
      state.totalFiles = state.pendingFiles = 1 + packageToFetch.resources.Count;

      // Note that the callbacks are asynchronous so they may complete in any order.  What we do know is that they
      // will all be called on the main thread, so they won't be called concurrently.

      long maxCacheAge = asset.IsMutable ? MUTABLE_ASSET_MAX_CACHE_AGE : IMMUTABLE_ASSET_MAX_CACHE_AGE;

      PolyClientUtils.GetRawFileBytes(packageToFetch.root.url, accessToken, maxCacheAge,
        (PolyStatus status, byte[] data) => { ProcessFileFetchResult(state, ROOT_FILE_INDEX, status, data); });

      for (int i = 0; i < packageToFetch.resources.Count; i++) {
        int thisIndex = i;  // copy of variable, for closure below.
        PolyClientUtils.GetRawFileBytes(packageToFetch.resources[i].url, accessToken, maxCacheAge,
          (status, data) => { ProcessFileFetchResult(state, thisIndex, status, data); });
      }
    }

    /// <summary>
    /// Processes the result of fetching an individual file.
    /// </summary>
    /// <param name="state">Indicates the state of the ongoing fetch operation (as set up in FetchObj).</param>
    /// <param name="index">If ROOT_FILE_INDEX, then this is a result for the main file; else this is a result for
    /// the resource file with that index.</param>
    /// <param name="status">The status indicating if the download succeed</param>
    /// <param name="data">The data that was downloaded.</param>
    private void ProcessFileFetchResult(FetchOperationState state, int index, PolyStatus status, byte[] data) {
      if (state.pendingFiles == 0) {
        // Another request for this format failed, so we ignore any further responses.
        return;
      }

      if (!status.ok) {
        // This request failed, so we set pendingFiles to 0 so we ignore any further responses, and callback with
        // an error message.
        state.pendingFiles = 0;
        state.completionCallback(state.asset, PolyStatus.Error(status, "Failed to fetch file #{0}", index));
        return;
      }

      PolyFormat package = state.packageBeingFetched;
      PolyFile file = index == ROOT_FILE_INDEX ? package.root : package.resources[index];
      file.contents = data;

      --state.pendingFiles;
      if (state.progressCallback != null) {
        state.progressCallback(state.asset, 1.0f - ((float)state.pendingFiles / state.totalFiles));
      }
      if (state.pendingFiles <= 0) {
        // All files done, call callback indicating success.
        state.completionCallback(state.asset, PolyStatus.Success());
      }
    }

    /// <summary>
    /// Processes request results and delivers them to the callback.
    /// </summary>
    /// <param name="result">The result.</param>
    private void ProcessRequestResult(PolyListAssetsResult result, PolyApi.ListAssetsCallback callback) {
      if (result == null) {
        callback(new PolyStatusOr<PolyListAssetsResult>(PolyStatus.Error("No request result.")));
        return;
      }

      if (result.assets == null) {
        // Nothing wrong with the request, there were just no assets that matched those parameters.
        // Put an empty list in the result.
        result.assets = new List<PolyAsset>();
      }

      callback(new PolyStatusOr<PolyListAssetsResult>(result));
    }

    /// <summary>
    /// The state of a fetch operation.
    /// </summary>
    private class FetchOperationState {
      // The total number of files to fetch.
      public int totalFiles = 0;
      // The number of files that have not yet been fetched.
      public int pendingFiles = 0;
      // The asset whose contents are being fetched.
      public PolyAsset asset;
      // The type of format being fetched.
      public PolyFormat packageBeingFetched;
      // A callback, called when the fetch operation succeeds or fails.
      public PolyApi.FetchFormatFilesCallback completionCallback;
      // Progress callback (optional).
      public FetchProgressCallback progressCallback;
    }
  }
}
