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

using PolyToolkitInternal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


namespace PolyToolkit {
/// <summary>
/// This is the main entry point for the Poly Toolkit Runtime API.
/// This class covers:
/// - Authentication and properties of the signed-in user.
/// - Wrappers around the Poly REST API endpoints.
/// - Importing assets and thumbnails.
/// - Managing the cache of assets.
/// </summary>
public static class PolyApi {
  // Whether this class has been initialized. If not, no operations are guaranteed to succeed.
  private static bool initialized = false;

  /// <summary>
  /// Initializes the Poly Toolkit runtime API. This will be called by PolyToolkitManager on its Awake method,
  /// you shouldn't need to call this method directly.
  /// </summary>
  public static void Init(PolyAuthConfig? authConfig = null, PolyCacheConfig? cacheConfig = null) {
    // NOTE: although it might seem strange, we have to support the use case of re-initializing PolyApi
    // (with possibly different config) because that's what happens when the project goes from play
    // mode back to edit mode -- the Poly Toolkit editor code will call PolyApi.Init with the editor
    // config, and in that case we should wipe out our previous state and initialize again.
    Shutdown();
    PtSettings.Init();
    PolyMainInternal.Init(authConfig, cacheConfig);
    Authenticator.Initialize(authConfig ?? PtSettings.Instance.authConfig);  
    initialized = true;
  }

  /// <summary>
  /// Attempts to authenticate a user.
  /// </summary>
  /// <param name="interactive">If true, we may trigger user interaction to complete the authentication
  /// flow (for example, launching a browser to ask the user to log in). If false, we will attempt
  /// to authenticate using any existing tokens, but won't prompt the user.</param>
  /// <param name="callback">The callback to call with the result of the authentication.</param>
  public static void Authenticate(bool interactive, Action<PolyStatus> callback) {
    CheckInitialized();
    Authenticator.Instance.Authenticate(interactive, callback);
  }

  /// <summary>
  /// Alternative form of Authenticate() that allows usage of existing auth tokens.
  /// </summary>
  /// <remarks>
  /// Use this method only if you are implementing your own authentication/authorization flow, and need to provide
  /// Poly Toolkit with tokens to use. This method should be used INSTEAD of Authenticate() and will have
  /// the same effect, except that instead of launching the auth flow, the provided tokens will be used instead.
  /// </remarks>
  /// <param name="accessToken">The access token to use with API requests.</param>
  /// <param name="refreshToken">The refresh token to use with API requests.</param>
  public static void Authenticate(string accessToken, string refreshToken, Action<PolyStatus> callback) {
    CheckInitialized();
    Authenticator.Instance.Authenticate(accessToken, refreshToken, callback);
  }


  /// <summary>
  /// Cancels the current authentication flow, if there's one in progress.
  /// Does nothing if there is no authentication flow in progress.
  /// </summary>
  public static void CancelAuthentication() {
    CheckInitialized();
    Authenticator.Instance.CancelAuthentication();
  }

  /// <summary>
  /// Signs out, deleting the current authentication token.
  /// </summary>
  public static void SignOut() {
    CheckInitialized();
    Authenticator.Instance.SignOut();
  }

  /// <summary>
  /// Returns whether or not the API is initialized.
  /// </summary>
  public static bool IsInitialized { get { return initialized && Authenticator.IsInitialized
    && PolyMainInternal.IsInitialized; } }

  /// <summary>
  /// Returns whether or not we are currently in the process of authenticating.
  /// </summary>
  public static bool IsAuthenticating {
    get {
      return Authenticator.IsInitialized && Authenticator.Instance.IsAuthenticating;
    }
  }

  /// <summary>
  /// Returns whether or not a user is currently authenticated.
  /// </summary>
  public static bool IsAuthenticated {
    get {
      return Authenticator.IsInitialized && Authenticator.Instance.IsAuthenticated;
    }
  }

  /// <summary>
  /// Returns the current user's access token, if a user is authenticated, or null if not.
  /// </summary>
  public static string AccessToken {
    get {
      return Authenticator.IsInitialized ? Authenticator.Instance.AccessToken : null;
    }
  }

  /// <summary>
  /// Returns the current user's refresh token, if a user is authenticated, or null if not.
  /// </summary>
  public static string RefreshToken {
    get {
      return Authenticator.IsInitialized ? Authenticator.Instance.RefreshToken : null;
    }
  }

  /// <summary>
  /// Returns the current user's profile icon, if a user is authenticated, or null if not.
  /// </summary>
  public static Sprite UserIcon {
    get {
      return Authenticator.IsInitialized ? Authenticator.Instance.UserIcon : null;
    }
  }

  /// <summary>
  /// Returns the current user's display name, if a user is authenticated, or null if not.
  /// </summary>
  public static string UserName {
    get {
      return Authenticator.IsInitialized ? Authenticator.Instance.UserName : null;
    }
  }

  /// <summary>
  /// Delegate type for the callback of <see cref="ListAssets"/>.
  /// </summary>
  /// <param name="result"></param>
  public delegate void ListAssetsCallback(PolyStatusOr<PolyListAssetsResult> result);

  /// <summary>
  /// Requests a listing of assets according to the specified parameters.
  /// </summary>
  /// <param name="request">The request parameters.</param>
  /// <param name="callback">The callback to call when the request finishes.</param>
  public static void ListAssets(PolyListAssetsRequest request, ListAssetsCallback callback) {
    CheckInitialized();
    PolyMainInternal.Instance.ListAssets(request, callback);
  }

  /// <summary>
  /// Requests a listing of the logged-in user's own assets according to the specified parameters.
  /// </summary>
  /// <param name="request">The request parameters.</param>
  /// <param name="callback">The callback to call when the request finishes.</param>
  public static void ListUserAssets(PolyListUserAssetsRequest request, ListAssetsCallback callback) {
    CheckInitialized();
    PolyMainInternal.Instance.ListUserAssets(request, callback);
  }

  /// <summary>
  /// Requests a listing of liked assets according to the specified parameters. Currently, only the logged-in
  /// user's own likes are supported.
  /// </summary>
  /// <param name="request">The request parameters.</param>
  /// <param name="callback">The callback to call when the request finishes.</param>
  public static void ListLikedAssets(PolyListLikedAssetsRequest request, ListAssetsCallback callback) {
    CheckInitialized();
    PolyMainInternal.Instance.ListLikedAssets(request, callback);
  }

  /// <summary>
  /// Delegate type for the callback of <see cref="GetAsset"/>.
  /// </summary>
  /// <param name="result"></param>
  public delegate void GetAssetCallback(PolyStatusOr<PolyAsset> result);

  /// <summary>
  /// Gets an asset by name (id).
  /// </summary>
  /// <param name="name">The name (id) of the asset to get. Note that even though this
  /// is called 'name', it does not mean the asset's display name, but its unique ID.
  /// Example: "assets/5vbJ5vildOq".</param>
  /// <param name="callback">The callback to call when the request finishes.</param>
  public static void GetAsset(string name, GetAssetCallback callback) {
    CheckInitialized();
    PolyMainInternal.Instance.GetAsset(name, callback);
  }

  /// <summary>
  /// Delegate type for the callback of <see cref="FetchFormatFiles"/>
  /// </summary>
  /// <param name="asset">The asset for which we are fetching files.</param>
  /// <param name="status">The result of the operation.</param>
  public delegate void FetchFormatFilesCallback(PolyAsset asset, PolyStatus status);

  /// <summary>
  /// Retrieves information about the given asset in the given format, then calls the given callback.
  /// </summary>
  /// <param name="asset">The asset for which to fetch the files.</param>
  /// <param name="format">The desired format.</param>
  /// <param name="callback">The callback to call when the fetch is complete.</param>
  public static void FetchFormatFiles(PolyAsset asset, PolyFormatType format, FetchFormatFilesCallback callback) {
    CheckInitialized();
    PolyMainInternal.Instance.FetchFormatFiles(asset, format, callback);
  }

  /// <summary>
  /// Delegate type for the callback of <see cref="Import"/>.
  /// </summary>
  /// <param name="asset">The asset to which the import operation pertains.</param>
  /// <param name="result">The result of the operation.</param>
  public delegate void ImportCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result);

  /// <summary>
  /// Imports the given asset as a GameObject.
  /// </summary>
  /// <remarks>
  /// This includes fetching any necessary files and importing them. The end result will be
  /// a GameObject that represents the imported asset. It will be placed at the origin with
  /// identity rotation and scale.
  /// </remarks>
  /// <param name="asset">The asset to import.</param>
  /// <param name="options">The options that control how to import the asset.</param>
  /// <param name="callback">The callback to call when the operation is complete (optional).</param>
  public static void Import(PolyAsset asset, PolyImportOptions options, ImportCallback callback = null) {
    CheckInitialized();
    PolyMainInternal.Instance.Import(asset, options, callback);
  }

  /// <summary>
  /// Delegate type for the callback of <see cref="FetchThumbnail"/>.
  /// </summary>
  /// <remarks>
  /// If this callback reports success, then the thumbnail will be available in the asset's
  /// <see cref="PolyAsset.thumbnailTexture"/> field.
  /// </remarks>
  /// <param name="asset">The asset to whose thumbnail we were fetching.</param>
  /// <param name="status">The result of the fetch.</param>
  public delegate void FetchThumbnailCallback(PolyAsset asset, PolyStatus status);

  /// <summary>
  /// Fetches the thumbnail for the given asset.
  /// </summary>
  /// <param name="asset">The asset for which to fetch the thumbnail.</param>
  /// <param name="callback">The callback to call when the fetch finishes (optional).</param>
  public static void FetchThumbnail(PolyAsset asset, FetchThumbnailCallback callback = null) {
    CheckInitialized();
    PolyMainInternal.Instance.FetchThumbnail(asset, null, callback);
  }

  /// <summary>
  /// Fetches the thumbnail for the given asset.
  /// </summary>
  /// <param name="asset">The asset for which to fetch the thumbnail.</param>
  /// <param name="options">The extra options, if any (can be null).</param>
  /// <param name="callback">The callback to call when the fetch finishes (optional).</param>
  public static void FetchThumbnail(PolyAsset asset, PolyFetchThumbnailOptions options,
      FetchThumbnailCallback callback = null) {
    CheckInitialized();
    PolyMainInternal.Instance.FetchThumbnail(asset, options, callback);
  }

  /// <summary>
  /// The Poly class is not valid until it has been explicitly initialized. This method checks that initialization has
  /// completed, and throws an exception if not. This method should be called before any operation that depends on the
  /// runtime API being initialized.
  /// </summary>
  private static void CheckInitialized() {
    if (!initialized) {
      throw new Exception("Poly Toolkit runtime API not initialized. You must have a PolyToolkitManager in your " +
        "scene and wait until after its Awake() method runs, or explicitly call PolyApi.Init() before " +
        "using Poly API methods.");
    }
  }
  
  /// <summary>
  /// Clears the local web cache. This is asynchronous (the cache will be cleared in the background).
  /// </summary>
  public static void ClearCache() {
    PolyMainInternal.Instance.ClearCache();
  }

  /// <summary>
  /// Generates a list of attribution information of each asset passed, including the names
  /// of the authors and links to the original creations.
  /// </summary>
  /// <param name="includeStatic">Indicates whether to include attribution information for assets imported
  /// into the project at edit time (recommended).</param>
  /// <param name="runtimeAssets">Additional assets to include attribution information for. Set this to the list
  /// of assets imported at runtime that you are using in your scene or project.</param>
  public static string GenerateAttributions(bool includeStatic = true, List<PolyAsset> runtimeAssets = null) {
    StringBuilder sb = new StringBuilder();
    
    bool hasHeader = false;
    if (includeStatic) {
      // Append static file first, because it includes the header.
      TextAsset staticAttributionFile = Resources.Load<TextAsset>(
        Path.GetFileNameWithoutExtension(AttributionGeneration.ATTRIB_FILE_NAME));
      if (staticAttributionFile != null) {
        sb.Append(staticAttributionFile.text);
        hasHeader = true;
      }
    }

    if (!hasHeader) {
      sb.Append(AttributionGeneration.FILE_HEADER).AppendLine();
    }

    // Sort the assets so they appear in alphabetical order, by display name.
    runtimeAssets.Sort((PolyAsset a, PolyAsset b) => { return a.displayName.CompareTo(b.displayName); });
    foreach (PolyAsset asset in runtimeAssets) {
      if (asset.license == PolyAssetLicense.CREATIVE_COMMONS_BY) {
        // Only include attribution information for third party assets.
        sb.AppendLine();
        sb.Append(asset.AttributionInfo).AppendLine();
      }
    }

    return sb.ToString();
  }

  /// <summary>
  /// Shuts down the Poly Toolkit runtime API. Calling this method should not normally be necessary
  /// for most applications. It exists for some very specific use cases, such as writing an editor
  /// extension that needs to have precise control over initialization and deinitialization of
  /// components.
  /// </summary>
  public static void Shutdown() {
    if (PolyMainInternal.IsInitialized) {
      PolyMainInternal.Shutdown();
    }
    if (Authenticator.IsInitialized) {
      Authenticator.Shutdown();
    }
    initialized = false;
  }
}
}
