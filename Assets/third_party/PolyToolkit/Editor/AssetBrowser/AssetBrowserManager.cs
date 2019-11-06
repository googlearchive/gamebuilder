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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using PolyToolkit;
using PolyToolkitInternal;

namespace PolyToolkitEditor {
/// <summary>
/// Model and controller for the Asset Browser Window. This class holds the data and handles the actual
/// work).
/// </summary>
public class AssetBrowserManager {
  private const string PRODUCT_NAME = "PolyToolkit";
  private const string CLIENT_SECRET = "49385a554c3274635d6c47327d3a3c557d67793e79267852";
  private const string CLIENT_ID = "3539303a373737363831393b2178617c60227d7f7b7966252a74226e296f2d29174315175" +
    "15716131b1c5a4d1b034f5f40421c545b5a515b5d4c495e4e5e515134242c376a26292a";
  private const string API_KEY = "41487862577c4474616e3b5f4b39466e5161732a4b645d5b495752557276274673196e74496173";

  private const string DOWNLOAD_PROGRESS_TITLE = "Downloading...";
  private const string DOWNLOAD_PROGRESS_TEXT = "Downloading asset. Please wait...";

  private const int THUMBNAIL_REQUESTED_SIZE = 300;

  /// <summary>
  /// If true, we are currently performing a query and waiting for the query result.
  /// </summary>
  private bool querying = false;

  /// <summary>
  /// The most recent query result that we have.
  /// </summary>
  private PolyStatusOr<PolyListAssetsResult> listAssetsResult = null;

  /// <summary>
  /// List of assets that are currently being downloaded.
  /// </summary>
  private HashSet<PolyAsset> assetsBeingDownloaded = new HashSet<PolyAsset>();

  private Action refreshCallback = null;
  
  /// <summary>
  /// The most recent query result that we have. (read only)
  /// </summary>
  private PolyRequest currentRequest = null;

  /// <summary>
  /// Result of the most recent GetAsset request.
  /// </summary>
  private PolyAsset assetResult = null;

  /// <summary>
  /// Whether the current response has at least another page of results left that haven't been loaded yet. 
  /// </summary>
  public bool resultHasMorePages = false;

  /// <summary>
  /// Result of the most recent GetAsset request. (read only)
  /// </summary>
  public PolyAsset CurrentAssetResult { get { return assetResult; } }

  /// <summary>
  /// The ID of the query that we are currently expecting the answer for.
  /// This is incremented every time we send out a query.
  /// </summary>
  private int queryId = 0;

  /// <summary>
  /// If true, we are waiting for the initial "silent authentication" to finish.
  /// </summary>
  private bool waitingForSilentAuth = false;

  /// <summary>
  /// If this is not null, we are waiting for the authentication to finish before sending this request.
  /// This request will be sent as soon as we get the auth callback.
  /// </summary>
  private PolyRequest requestToSendAfterAuth = null;

  private PolyAuthConfig authConfig = new PolyAuthConfig(
    apiKey: Deobfuscate(API_KEY),
    clientId: Deobfuscate(CLIENT_ID),
    clientSecret: Deobfuscate(CLIENT_SECRET));

  private PolyCacheConfig cacheConfig = new PolyCacheConfig(
    cacheEnabled: true,
    maxCacheSizeMb: 512,
    maxCacheEntries: 2048);

  /// <summary>
  /// Cache of thumbnail images.
  /// </summary>
  private ThumbnailCache thumbnailCache = new ThumbnailCache();

  /// <summary>
  /// Returns whether or not we are currently running a query.
  /// </summary>
  public bool IsQuerying { get { return querying; } }

  /// <summary>
  /// The result of the latest query, or null if there were no queries.
  /// </summary>
  public PolyStatusOr<PolyListAssetsResult> CurrentResult { get { return listAssetsResult; } }

  /// <summary>
  /// If true, we're currently in the process of downloading assets.
  /// </summary>
  public bool IsDownloadingAssets { get { return assetsBeingDownloaded.Count > 0; } }

  /// <summary>
  /// Assets currently being displayed in the browser.
  /// </summary>
  private HashSet<string> assetsInUse;

  public AssetBrowserManager() {
    PtDebug.Log("ABM initializing...");
    EnsurePolyIsReady();

    // Initially, show the featured assets home page.
    StartRequest(PolyListAssetsRequest.Featured());
  }

  public AssetBrowserManager(PolyRequest request) {
    PtDebug.Log("ABM initializing...");
    EnsurePolyIsReady();

    // If this is a request that needs authentication and we are in the process of authenticating,
    // wait until we're finished.
    bool needAuth = request is PolyListLikedAssetsRequest || request is PolyListUserAssetsRequest;
    if (needAuth && waitingForSilentAuth) {
      // Defer the request. Wait until auth is complete.
      PtDebug.Log("ABM: Deferring request until after auth.");
      requestToSendAfterAuth = request;
      return;
    }

    StartRequest(request);
  }

  /// <summary>
  /// Because Poly doesn't live in the Editor/ space (and couldn't, since it uses GameObjects and
  /// MonoBehaviours), it will die every time the user enters or exits play mode. This means
  /// that all of its state and objects will get wiped. So we have to check if it needs initialization
  /// every time we need to use it.
  /// </summary>
  public void EnsurePolyIsReady() {
    if (!PolyApi.IsInitialized) {
      PtDebug.Log("ABM: Initializing Poly.");
      // We need to set a service name for our auth config because we want to keep our auth credentials
      // separate in a different "silo", so they don't get confused with the runtime credentials
      // the user might be using in their project. Regular users would not set a service name, so they
      // use the default silo.
      authConfig.serviceName = "PolyToolkitEditor";
      PolyApi.Init(authConfig, cacheConfig);
      waitingForSilentAuth = true;
      PolyApi.Authenticate(interactive: false, callback: (PolyStatus status) => {
        waitingForSilentAuth = false;
        OnSignInFinished(/* wasInteractive */ false, status);
      });
    }
  }

  /// <summary>
  /// Launches the interactive sign-in flow (launches a browser to perform sign-in).
  /// </summary>
  public void LaunchSignInFlow() {
    PolyApi.Authenticate(interactive: true, callback: (PolyStatus status) => {
      OnSignInFinished(/* wasInteractive */ true, status);
    });
  }

  /// <summary>
  /// Cancels the authentication flow.
  /// </summary>
  public void CancelSignIn() {
    PolyApi.CancelAuthentication();
  }

  /// <summary>
  /// Sets the callback that will be called whenever there is a change in this object's
  /// data (search results, etc). This should be used to update any UI.
  /// </summary>
  /// <param name="refreshCallback">The callback to call, null for none.</param>
  public void SetRefreshCallback(Action refreshCallback) {
    this.refreshCallback = refreshCallback;
  }

  /// <summary>
  /// Returns whether or not the given asset is in the process of being downloaded.
  /// </summary>
  /// <param name="asset">The asset to check.</param>
  /// <returns>True if it's being downloaded, false if not.</returns>
  public bool IsDownloadingAsset(PolyAsset asset) {
    return assetsBeingDownloaded.Contains(asset);
  }

  /// <summary>
  /// Starts a new request. If there is already an existing request in progress, it will be cancelled.
  /// </summary>
  /// <param name="request">The request parameters; can be either a ListAssetsRequest or
  /// a PolyListUserAssetsRequest.</param>
  public void StartRequest(PolyRequest request) {
   StartRequest(request, OnRequestResult);
  }

  /// <summary>
  /// Clear the current get asset result.
  /// </summary>
  public void ClearCurrentAssetResult() {
    assetResult = null;
  }

  /// <summary>
  /// Get the next page of assets from the current request.
  /// </summary>
  public void GetNextPageRequest() {
   PtDebug.Log("ABM: getting next page of current request...");

   if (CurrentResult == null || !CurrentResult.Ok) {
     Debug.LogError("Request failed, no valid current result to get next page of.");
   }

   currentRequest.pageToken = CurrentResult.Value.nextPageToken;
   StartRequest(currentRequest, OnNextPageRequestResult);
  }
  
  /// <summary>
  /// Starts a new request. If there is already an existing request in progress, it will be cancelled.
  /// </summary>
  /// <param name="request">The request parameters; can be either a ListAssetsRequest or
  /// a PolyListUserAssetsRequest.</param>
  /// <param name="callback"> The callback to invoke when the request finishes.</param>
  private void StartRequest(PolyRequest request, Action<PolyStatusOr<PolyListAssetsResult>> callback) {
   int thisQueryId = PrepareForNewQuery(); // for the closure below.
   currentRequest = request;

   if (request is PolyListAssetsRequest) {
    PolyListAssetsRequest listAssetsRequest = request as PolyListAssetsRequest;
    PolyApi.ListAssets(listAssetsRequest, (PolyStatusOr<PolyListAssetsResult> result) => {
      // Only process result if this is indeed the most recent query that we issued.
      // If we have issued another query since (in which case thisQueryId < queryId),
      // then ignore the result.
     if (thisQueryId == queryId && callback != null) callback(result);
    });
   } else if (request is PolyListUserAssetsRequest) {
    PolyListUserAssetsRequest listUserAssetsRequest = request as PolyListUserAssetsRequest;
    PolyApi.ListUserAssets(listUserAssetsRequest, (PolyStatusOr<PolyListAssetsResult> result) => {
     if (thisQueryId == queryId && callback != null) callback(result);
    });
   } else if (request is PolyListLikedAssetsRequest) {
    PolyListLikedAssetsRequest listLikedAssetsRequest = request as PolyListLikedAssetsRequest;
    PolyApi.ListLikedAssets(listLikedAssetsRequest, (PolyStatusOr<PolyListAssetsResult> result) => {
     if (thisQueryId == queryId && callback != null) callback(result);
    });
   } else {
    Debug.LogError("Request failed. Must be either a PolyListAssetsRequest or PolyListUserAssetsRequest");
   }
  }

  /// <summary>
  /// Starts a new request for a specific asset. If there is already an existing
  /// request in progress, it will be cancelled.
  /// </summary>
  /// <param name="assetId">Id of the asset to get.</param>
  public void StartRequestForSpecificAsset(string assetId) {
    int thisQueryId = PrepareForNewQuery();

    PolyApi.GetAsset(assetId, (PolyStatusOr<PolyAsset> result) => {
     if (thisQueryId == queryId) OnRequestForSpecificAssetResult(result);
    });
  }

  /// <summary>
  /// Helper method to prepare the manager for starting a new query.
  /// </summary>
  private int PrepareForNewQuery() {
    querying = true;
    queryId++;
    assetResult = null;
    return queryId;
  }

  /// <summary>
  /// Clears the current request. Also cancels any pending request.
  /// </summary>
  public void ClearRequest() {
    PtDebug.Log("ABM: clearing request...");
    querying = false;
    // Increasing the ID will cause us to ignore the results of any pending requests
    // (we will know they are obsolete by their query ID).
    queryId++;
    listAssetsResult = null;
    resultHasMorePages = false;
  }

  /// <summary>
  /// Called when sign in finishes.
  /// </summary>
  /// <param name="wasInteractive">If true, this was the interactive (browser-based) sign-in flow.</param>
  /// <param name="status">The result of the sign in process.</param>
  private void OnSignInFinished(bool wasInteractive, PolyStatus status) {
    if (status.ok) {
      string tok = PolyApi.AccessToken;
      PtDebug.LogFormat("ABM: Sign in success. Access token: {0}",
        (tok != null && tok.Length > 6) ? tok.Substring(0, 6) + "..." : "INVALID");
      PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_SIGN_IN_SUCCESS);
    } else if (wasInteractive) {
      Debug.LogErrorFormat("Failed to sign in. Please try again: " + status);
      PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_SIGN_IN_FAILURE, status.ToString());
    }
    if (null != refreshCallback) refreshCallback();

    // If we had a deferred request that was waiting for auth, send it now.
    if (requestToSendAfterAuth != null) {
      PtDebug.Log("Sending deferred request that was waiting for auth.");  
      PolyRequest request = requestToSendAfterAuth;
      requestToSendAfterAuth = null;
      StartRequest(request);
    }
  }

  /// <summary>
  /// Callback invoked when the request for the next page of assets returns; appends
  /// received assets to the existing result.
  /// </summary>
  private void OnNextPageRequestResult(PolyStatusOr<PolyListAssetsResult> result) {
    if (result.Ok) {
      PtDebug.LogFormat("ABM: request results received ({0} assets).", result.Value.assets.Count);
      this.listAssetsResult.Value.assets.AddRange(result.Value.assets);
      this.listAssetsResult.Value.nextPageToken = result.Value.nextPageToken;
      resultHasMorePages = result.Value.nextPageToken != null;
    } else {
      Debug.LogError("Asset request failed. Try again later: " + result.Status);
      this.listAssetsResult = result;
    }
    querying = false;
    if (null != refreshCallback) refreshCallback();
    assetsInUse = GetAssetsInUse();
    FinishFetchingThumbnails(result);
  }

  /// <summary>
  /// Callback invoked when we get request results.
  /// </summary>
  private void OnRequestResult(PolyStatusOr<PolyListAssetsResult> result) {
    if (result.Ok) {
      PtDebug.LogFormat("ABM: request results received ({0} assets).", result.Value.assets.Count);
      this.listAssetsResult = result;
      resultHasMorePages = result.Value.nextPageToken != null;
    } else {
      Debug.LogError("Asset request failed. Try again later: " + result.Status);
      this.listAssetsResult = result;
    }
    querying = false;
    if (null != refreshCallback) refreshCallback();
    
    if (result.Ok) {
      assetsInUse = GetAssetsInUse();
      FinishFetchingThumbnails(result);
    }
  }

  /// <summary>
  /// Callback invoked when we receive the result of a request for a specific asset.
  /// </summary>
  private void OnRequestForSpecificAssetResult(PolyStatusOr<PolyAsset> result) {
    if (result.Ok) {
      PtDebug.Log("ABM: get asset request received result.");
      assetResult = result.Value;

      if (!thumbnailCache.TryGet(assetResult.name, out assetResult.thumbnailTexture)) {
        FetchThumbnail(assetResult);
      }
    } else {
      Debug.LogError("Error: " + result.Status.errorMessage);
    }
    querying = false;
    if (null != refreshCallback) refreshCallback();
  }

  /// <summary>
  /// Fetches thumbnails that do not yet exist in the cache.
  /// </summary>
  private void FinishFetchingThumbnails(PolyStatusOr<PolyListAssetsResult> result) {
    if (result.Ok) {
      List<PolyAsset> assetsMissingThumbnails = new List<PolyAsset>();
      foreach (PolyAsset asset in listAssetsResult.Value.assets) {
        if (!thumbnailCache.TryGet(asset.name, out asset.thumbnailTexture)) {
          assetsMissingThumbnails.Add(asset);
        }
      }
      foreach (PolyAsset asset in assetsMissingThumbnails) {
        FetchThumbnail(asset);
      }
    }
  }

  private void FetchThumbnail(PolyAsset asset) {
    PolyFetchThumbnailOptions options = new PolyFetchThumbnailOptions();
    options.SetRequestedImageSize(THUMBNAIL_REQUESTED_SIZE);
    PolyApi.FetchThumbnail(asset, options, OnThumbnailFetched);
  }

  /// <summary>
  /// Callback invoked when an asset thumbnail is fetched.
  /// </summary>
  private void OnThumbnailFetched(PolyAsset asset, PolyStatus status) {
    if (status.ok) {
      thumbnailCache.Put(asset.name, asset.thumbnailTexture);
      // Preserve the texture so it survives round-trips to play mode and back.
      asset.thumbnailTexture.hideFlags = HideFlags.HideAndDontSave;
    }
    if (null != refreshCallback) refreshCallback();

    thumbnailCache.TrimCacheWithExceptions(assetsInUse);
  }

  /// <summary>
  /// Returns a set of asset ids of assets currently being displayed.
  /// </summary>
  private HashSet<string> GetAssetsInUse() {
    HashSet<string> assetsInUse = new HashSet<string>();
    if (listAssetsResult == null || !listAssetsResult.Ok) return assetsInUse;
    foreach (var asset in listAssetsResult.Value.assets) {
      assetsInUse.Add(asset.name);
    }
    return assetsInUse;
  }

  /// <summary>
  /// Starts downloading and importing the given asset (in the background). When done, the asset will
  /// be saved to the user's Assets folder.
  /// </summary>
  /// <param name="asset">The asset to download and import.</param>
  /// <param name="ptAssetLocalPath">Path to the PtAsset that should be created (or replaced).</param>
  /// <param name="options">Import options.</param>
  public void StartDownloadAndImport(PolyAsset asset, string ptAssetLocalPath, EditTimeImportOptions options) {
    if (!assetsBeingDownloaded.Add(asset)) return;
    PtDebug.LogFormat("ABM: starting to fetch asset {0} ({1}) -> {2}", asset.name, asset.displayName,
      ptAssetLocalPath);

    // Prefer glTF1 to glTF2.
    // It used to be that no Poly assets had both formats, so the ordering did not matter.
    // Blocks assets now have both glTF1 and glTF2. PT does not understand the glTF2 version,
    // so the ordering matters a great deal.
    PolyFormat glTF2format = asset.GetFormatIfExists(PolyFormatType.GLTF_2);
    PolyFormat glTFformat = asset.GetFormatIfExists(PolyFormatType.GLTF);

    PolyMainInternal.FetchProgressCallback progressCallback = (PolyAsset assetBeingFetched, float progress) => {
      EditorUtility.DisplayProgressBar(DOWNLOAD_PROGRESS_TITLE, DOWNLOAD_PROGRESS_TEXT, progress);
    };

    if (glTFformat != null) {
      EditorUtility.DisplayProgressBar(DOWNLOAD_PROGRESS_TITLE, DOWNLOAD_PROGRESS_TEXT, 0.0f);
      PolyMainInternal.Instance.FetchFormatFiles(asset, PolyFormatType.GLTF,
          (PolyAsset resultAsset, PolyStatus status) => {
        EditorUtility.ClearProgressBar();
        OnFetchFinished(status, resultAsset, /*isGltf2*/ false, ptAssetLocalPath, options);
      }, progressCallback);
    } else if (glTF2format != null) {
      EditorUtility.DisplayProgressBar(DOWNLOAD_PROGRESS_TITLE, DOWNLOAD_PROGRESS_TEXT, 0.0f);
      PolyMainInternal.Instance.FetchFormatFiles(asset, PolyFormatType.GLTF_2,
          (PolyAsset resultAsset, PolyStatus status) => {
        EditorUtility.ClearProgressBar();
        OnFetchFinished(status, resultAsset, /*isGltf2*/ true, ptAssetLocalPath, options);
      }, progressCallback);
    } else {
      Debug.LogError("Asset not in GLTF_2 or GLTF format. Can't import.");
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_FAILED, "Unsupported format");
    }
  }

  /// <summary>
  /// Clears all caches (downloads and thumbnails).
  /// </summary>
  public void ClearCaches() {
    PolyApi.ClearCache();
    thumbnailCache.Clear();
  }

  private void OnFetchFinished(PolyStatus status, PolyAsset asset, bool isGltf2,
      string ptAssetLocalPath, EditTimeImportOptions options) {
    if (!status.ok) {
      Debug.LogErrorFormat("Error fetching asset {0} ({1}): {2}", asset.name, asset.displayName, status);
      EditorUtility.DisplayDialog("Download Error",
        string.Format("*** Error downloading asset '{0}'. Try again later.", asset.displayName), "OK");
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_FAILED, "Asset fetch failed");
      return;
    }

    string baseName, downloadLocalPath;
    if (!PrepareDownload(asset, out baseName, out downloadLocalPath)) {
      return;
    }

    string absPath = PtUtils.ToAbsolutePath(downloadLocalPath);

    string extension = isGltf2 ? ".gltf2" : ".gltf";
    string fileName = baseName + extension;

    // We have to place an import request so that PolyImporter does the right thing when it sees the new file.
    PolyImporter.AddImportRequest(new PolyImporter.ImportRequest(
      downloadLocalPath + "/" + fileName, ptAssetLocalPath, options, asset));

    // Now unpackage it. GltfProcessor will pick it up automatically.
    UnpackPackageToFolder(isGltf2 ? asset.GetFormatIfExists(PolyFormatType.GLTF_2) :
      asset.GetFormatIfExists(PolyFormatType.GLTF), absPath, fileName);

    PtDebug.LogFormat("ABM: Successfully downloaded {0} to {1}", asset, absPath);
    AssetDatabase.Refresh();
    if (null != refreshCallback) refreshCallback();
  }

  private void UnpackPackageToFolder(PolyFormat package, string destFolder, string mainFileName) {
    // First write the resources, then the main file, so that when the main file is imported
    // all the necessary resources are already in place.

    // Maintain a mapping of original file names to their corresponding hash.
    StringBuilder fileMapSb = new StringBuilder();
    foreach (PolyFile file in package.resources) {
      // In order to avoid having to replicate the original directory structure of the
      // asset (which might be incompatible with our file system, or even maliciously constructed),
      // we replace the original path of each resource file with the MD5 hash of the path.
      // That maintains uniqueness of paths and flattens the structure so that every resource
      // can live in the same directory.
      string path = Path.Combine(destFolder, PolyInternalUtils.ConvertFilePathToHash(file.relativePath));
      if (file.contents != null) {
        File.WriteAllBytes(path, file.contents);
        fileMapSb.AppendFormat("{0} -> {1}\n", file.relativePath,
          PolyInternalUtils.ConvertFilePathToHash(file.relativePath));
      }
    }
    // Lastly, write the main file.
    File.WriteAllBytes(Path.Combine(destFolder, mainFileName), package.root.contents);
    // Write the file mapping.
    File.WriteAllText(Path.Combine(destFolder, "FileNameMapping.txt"), fileMapSb.ToString());
  }

  private bool PrepareDownload(PolyAsset asset, out string baseName, out string downloadLocalPath) {
    assetsBeingDownloaded.Remove(asset);
    PtDebug.LogFormat("ABM: Preparing to download {0}", asset);

    // basePath is something like Assets/Poly/Sources.
    string baseLocalPath = PtUtils.NormalizeLocalPath(PtSettings.Instance.assetSourcesPath);

    if (!baseLocalPath.StartsWith("Assets/")) {
      Debug.LogErrorFormat("Invalid asset sources folder {0}. Must be under Assets folder.");
      baseName = downloadLocalPath = null;
      return false;
    }

    // basePathAbs is something like C:\Users\foo\bar\MyUnityProject\Assets\Poly\Sources
    string baseFullPath = PtUtils.ToAbsolutePath(baseLocalPath);

    if (!Directory.Exists(baseFullPath)) {
      Directory.CreateDirectory(baseFullPath);
    }

    baseName = PtUtils.GetPtAssetBaseName(asset);
    PtDebug.LogFormat("Import name: {0}", baseName);

    // downloadLocalPath is something like Assets/Poly/Sources/assetTitle_assetId
    downloadLocalPath = baseLocalPath + "/" + baseName;
    string downloadFullPath = PtUtils.ToAbsolutePath(downloadLocalPath);

    if (Directory.Exists(downloadFullPath)) {
      if (PtSettings.Instance.warnOnSourceOverwrite && 
        !EditorUtility.DisplayDialog("Warning: Overwriting asset source folder",
          string.Format("The asset source folder '{0}' will be deleted and created again. " + 
              "This should be safe *unless* you have manually made changes to its contents, " +
              "in which case you will lose those changes.\n\n" +
              "(You can silence this warning in Poly Toolkit settings)",
              asset.displayName, downloadLocalPath), "OK", "Cancel")) return false;
      Directory.Delete(downloadFullPath, /* recursive */ true);
    }

    // Create the download folder.
    // Something like C:\Users\foo\bar\MyUnityProject\Assets\Poly\Sources\assetTitle_assetId
    Directory.CreateDirectory(downloadFullPath);
    return true;
  }

  private static string Obfuscate(string input) {
    byte[] data = Encoding.UTF8.GetBytes(input);
    StringBuilder sb = new StringBuilder();
    for (int i = 0; i < data.Length; i++) {
      byte b = (byte)(data[i] ^ i);
      sb.Append(b.ToString("x2"));
    }
    return sb.ToString();
  }

  public static string Deobfuscate(string input) {
    byte[] data = new byte[input.Length / 2];
    for (int i = 0; i < data.Length; i++) {
      byte b = Convert.ToByte(input.Substring(i * 2, 2), 16);
      data[i] = (byte)(b ^ i);
    }
    return Encoding.UTF8.GetString(data);
  }
}

}
