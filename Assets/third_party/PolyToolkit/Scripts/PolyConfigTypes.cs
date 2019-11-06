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
using PolyToolkitInternal;
using UnityEngine;

namespace PolyToolkit {
/// <summary>
/// Holds authentication configuration information.
/// </summary>
[Serializable]
[AutoStringifiable]
public struct PolyAuthConfig {
  [HelpText("NOTE: API Key is required for all API calls. Client ID/secret are only needed " +
    "for authenticated API calls.")]
  /// <summary>
  /// API key used to access the Poly API.
  /// </summary>
  [Tooltip("Your project's API key for the Poly API (required for all API calls).")]
  public string apiKey;
  /// <summary>
  /// Your project's Client ID as supplied by Google Cloud Console.
  /// </summary>
  [Tooltip("Your project's client ID for the Poly API (only required for authenticated API calls).")]
  public string clientId;
  /// <summary>
  /// Your project's client secret as supplied by Google Cloud Console.
  /// </summary>
  [Tooltip("Your project's client secret for the Poly API (only required for authenticated API calls).")]
  public string clientSecret;
  /// <summary>
  /// Additional scopes required by your project (separated by spaces).
  /// </summary>
  [Tooltip("Additional Google API scopes to request on authentication (optional).")]
  public string[] additionalScopes;
  /// <summary>
  /// (For internal Poly Toolkit use only). Leave this as null.
  /// </summary>
  [HideInInspector]
  public string serviceName;

  // NOTE: serviceName is set by Poly Toolkit in editor mode so that it can have an independent
  // auth "silo" that's separate from the runtime project. See OAuth2Identity for how this
  // variable is used. Users of Poly Toolkit should leave it as null.

  /// <summary>
  /// Creates a new PolyAuthConfig with the given settings.
  /// </summary>
  public PolyAuthConfig(string apiKey, string clientId, string clientSecret) {
    this.apiKey = apiKey;
    this.clientId = clientId;
    this.clientSecret = clientSecret;
    this.serviceName = null;
    additionalScopes = null;
  }

  public override string ToString() {
    return AutoStringify.Stringify(this);
  }
}

/// <summary>
/// Indicates how to configure the Poly Toolkit cache.
/// </summary>
[Serializable]
[AutoStringifiable]
public struct PolyCacheConfig {
  /// <summary>
  /// Whether or not to use caching for web requests (this is recommended).
  /// </summary>
  [HelpText("Note: caching is only supported on Windows and MacOSX. This " +
      "setting will be ignored on other platforms.")]
  public bool cacheEnabled;

  /// <summary>
  /// Maximum cache size, in megabytes.
  /// </summary>
  public int maxCacheSizeMb;

  /// <summary>
  /// Maximum number of cache entries.
  /// </summary>
  public int maxCacheEntries;

  /// <summary>
  /// If not null, this is the path that will be used to store the cache.
  /// If null, the default path will be used.
  /// </summary>
  public string cachePathOverride;

  public PolyCacheConfig(bool cacheEnabled, int maxCacheSizeMb, int maxCacheEntries) {
    this.cacheEnabled = cacheEnabled;
    this.maxCacheSizeMb = maxCacheSizeMb;
    this.maxCacheEntries = maxCacheEntries;
    cachePathOverride = null;
  }

  public override string ToString() {
    return AutoStringify.Stringify(this);
  }
}
}
