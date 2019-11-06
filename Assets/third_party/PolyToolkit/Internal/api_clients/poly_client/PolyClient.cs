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

﻿using Newtonsoft.Json.Linq;
using PolyToolkit;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using PolyToolkitInternal.client.model.util;
using PolyToolkitInternal.model.util;
namespace PolyToolkitInternal.api_clients.poly_client {
  /// <summary>
  ///   Parses the response of a List Assets request from Poly into a PolyListResult.
  /// </summary>
  public class ParseAssetsBackgroundWork : BackgroundWork {
    private byte[] response;
    private PolyStatus status;
    private Action<PolyStatus, PolyListAssetsResult> callback;
    private PolyListAssetsResult polyListAssetsResult;
    public ParseAssetsBackgroundWork(byte[] response, Action<PolyStatus, PolyListAssetsResult> callback) {
      this.response = response;
      this.callback = callback;
    }
    public void BackgroundWork() {
      JObject result;
      status = PolyClient.ParseResponse(response, out result);
      if (status.ok) {
        status = PolyClient.ParseReturnedAssets(Encoding.UTF8.GetString(response), out polyListAssetsResult);
      }
    }
    public void PostWork() {
      callback(status, polyListAssetsResult);
    }
  }
  /// <summary>
  ///   Parses an asset from Poly into a PolyAsset.
  /// </summary>
  public class ParseAssetBackgroundWork : BackgroundWork {
    private byte[] response;
    private Action<PolyStatus,PolyAsset> callback;
    private PolyStatus status;
    private PolyAsset polyAsset;
    public ParseAssetBackgroundWork(byte[] response, Action<PolyStatus,PolyAsset> callback) {
      this.response = response;
      this.callback = callback;
    }
    public void BackgroundWork() {
      JObject result;
      status = PolyClient.ParseResponse(response, out result);
      if (status.ok) {
        status = PolyClient.ParseAsset(result, out polyAsset);
      }
    }
    public void PostWork() {
      callback(status, polyAsset);
    }
  }
  [ExecuteInEditMode]
  public class PolyClient : MonoBehaviour {
    /// <summary>
    /// Default cache expiration time (millis) for queries of public assets.
    /// Only applies for queries of PUBLIC assets (featured, categories, etc), not the user's private
    /// assets.
    /// </summary>
    private const long DEFAULT_QUERY_CACHE_MAX_AGE_MILLIS = 60 * 60 * 1000;  // 60 minutes.
    // The base for API requests to Poly.
    public static string BASE_URL = "https://poly.googleapis.com";

    private static readonly Dictionary<PolyCategory, string> CATEGORIES = new Dictionary<PolyCategory, string>() {
      {PolyCategory.ANIMALS, "animals"},
      {PolyCategory.ARCHITECTURE, "architecture"},
      {PolyCategory.ART, "art"},
      {PolyCategory.FOOD, "food"},
      {PolyCategory.NATURE, "nature"},
      {PolyCategory.OBJECTS, "objects"},
      {PolyCategory.PEOPLE, "people"},
      {PolyCategory.PLACES, "scenes"},
      {PolyCategory.TECH, "tech"},
      {PolyCategory.TRANSPORT, "transport"},
    };
    
    private static readonly Dictionary<PolyOrderBy, string> ORDER_BY = new Dictionary<PolyOrderBy, string>() {
      {PolyOrderBy.BEST, "BEST"},
      {PolyOrderBy.NEWEST, "NEWEST"},
      {PolyOrderBy.OLDEST, "OLDEST"},
      {PolyOrderBy.LIKED_TIME, "LIKED_TIME"},
    };
    
    private static readonly Dictionary<PolyFormatFilter, string> FORMAT_FILTER = new Dictionary<PolyFormatFilter, string>() {
      {PolyFormatFilter.BLOCKS, "BLOCKS"},
      {PolyFormatFilter.FBX, "FBX"},
      {PolyFormatFilter.GLTF, "GLTF"},
      {PolyFormatFilter.GLTF_2, "GLTF2"},
      {PolyFormatFilter.OBJ, "TILT"},
      {PolyFormatFilter.TILT, "TILT"},
    };
    
    private static readonly Dictionary<PolyVisibilityFilter, string> VISIBILITY = new Dictionary<PolyVisibilityFilter, string>() {
      {PolyVisibilityFilter.PRIVATE, "PRIVATE"},
      {PolyVisibilityFilter.PUBLISHED, "PUBLISHED"},
    };

    private static readonly Dictionary<PolyMaxComplexityFilter, string> MAX_COMPLEXITY =
      new Dictionary<PolyMaxComplexityFilter, string>() {
        {PolyMaxComplexityFilter.SIMPLE, "SIMPLE"},
        {PolyMaxComplexityFilter.MEDIUM, "MEDIUM"},
        {PolyMaxComplexityFilter.COMPLEX, "COMPLEX"},
    };

    /// <summary>
    /// Return a Poly search URL representing a ListAssetsRequest.
    /// </summary>
    private static string MakeSearchUrl(PolyListAssetsRequest listAssetsRequest) {
      StringBuilder sb = new StringBuilder();
      sb.Append(BASE_URL)
        .Append("/v1/assets")
        .AppendFormat("?key={0}", WWW.EscapeURL(PolyMainInternal.Instance.apiKey));
      
      if (listAssetsRequest.formatFilter != null) {
        sb.AppendFormat("&format={0}", WWW.EscapeURL(FORMAT_FILTER[listAssetsRequest.formatFilter.Value]));
      }

      if (listAssetsRequest.keywords != null) {
        sb.AppendFormat("&keywords={0}", WWW.EscapeURL(listAssetsRequest.keywords));
      }
      
      if (listAssetsRequest.category != PolyCategory.UNSPECIFIED) {
        sb.AppendFormat("&category={0}", WWW.EscapeURL(CATEGORIES[listAssetsRequest.category]));
      }

      if (listAssetsRequest.curated) {
        sb.Append("&curated=true");
      }

      if (listAssetsRequest.maxComplexity != PolyMaxComplexityFilter.UNSPECIFIED) {
        sb.AppendFormat("&max_complexity={0}", WWW.EscapeURL(MAX_COMPLEXITY[listAssetsRequest.maxComplexity]));
      }

      sb.AppendFormat("&order_by={0}", WWW.EscapeURL(ORDER_BY[listAssetsRequest.orderBy]));
      sb.AppendFormat("&page_size={0}", listAssetsRequest.pageSize.ToString());
      if (listAssetsRequest.pageToken != null) {
        sb.AppendFormat("&page_token={0}", WWW.EscapeURL(listAssetsRequest.pageToken));
      }
      return sb.ToString();
    }

    /// <summary>
    /// Return a Poly search URL representing a ListUserAssetsRequest.
    /// </summary>
    private static string MakeSearchUrl(PolyListUserAssetsRequest listUserAssetsRequest) {
      StringBuilder sb = new StringBuilder();
      sb.Append(BASE_URL)
        .Append("/v1/users/me/assets")
        .AppendFormat("?key={0}", PolyMainInternal.Instance.apiKey);
        
      if (listUserAssetsRequest.formatFilter != null) {
        sb.AppendFormat("&format={0}", WWW.EscapeURL(FORMAT_FILTER[listUserAssetsRequest.formatFilter.Value]));
      }

      if (listUserAssetsRequest.visibility != PolyVisibilityFilter.UNSPECIFIED) {
        sb.AppendFormat("&visibility={0}", WWW.EscapeURL(VISIBILITY[listUserAssetsRequest.visibility]));
      }
      sb.AppendFormat("&order_by={0}", WWW.EscapeURL(ORDER_BY[listUserAssetsRequest.orderBy]));
      sb.AppendFormat("&page_size={0}", listUserAssetsRequest.pageSize);
      if (listUserAssetsRequest.pageToken != null) {
        sb.AppendFormat("&page_token={0}", WWW.EscapeURL(listUserAssetsRequest.pageToken));
      }

      return sb.ToString();
    }

    /// <summary>
    /// Return a Poly search URL representing a ListLikedAssetsRequest.
    /// </summary>
    private static string MakeSearchUrl(PolyListLikedAssetsRequest listLikedAssetsRequest) {
      StringBuilder sb = new StringBuilder();
      sb.Append(BASE_URL)
        .Append("/v1/users/me/likedassets")
        .AppendFormat("?key={0}", PolyMainInternal.Instance.apiKey);
        
      sb.AppendFormat("&order_by={0}", WWW.EscapeURL(ORDER_BY[listLikedAssetsRequest.orderBy]));
      sb.AppendFormat("&page_size={0}", listLikedAssetsRequest.pageSize);
      if (listLikedAssetsRequest.pageToken != null) {
        sb.AppendFormat("&page_token={0}", WWW.EscapeURL(listLikedAssetsRequest.pageToken));
      }

      return sb.ToString();
    }

    private static string MakeSearchUrl(PolyRequest request) {
      if (request is PolyListAssetsRequest) {
        return MakeSearchUrl(request as PolyListAssetsRequest);
      } else if (request is PolyListUserAssetsRequest) {
        return MakeSearchUrl(request as PolyListUserAssetsRequest);
      } else if (request is PolyListLikedAssetsRequest) {
        return MakeSearchUrl(request as PolyListLikedAssetsRequest);
      } else {
        throw new Exception("Must be a valid request type.");
      }
    }

    public void Setup() { }

    /// <summary>
    ///   Takes a string, representing either a ListAssetsResponse or ListUserAssetsResponse proto, and
    ///   fills polyListResult with relevant fields from the response and returns a success status
    ///   if the response is of the expected format, or a failure status if it's not.
    /// </summary>
    public static PolyStatus ParseReturnedAssets(string response, out PolyListAssetsResult polyListAssetsResult) {
      // Try and actually parse the string.
      JObject results = JObject.Parse(response);
      IJEnumerable<JToken> assets = results["assets"].AsJEnumerable();
      // If assets is null, check for a userAssets object, which would be present if the response was
      // a ListUserAssets response.
      if (assets == null) assets = results["userAssets"].AsJEnumerable();
      if (assets == null) {
        // Empty response means there were no assets that matched the request parameters.
        polyListAssetsResult = new PolyListAssetsResult(PolyStatus.Success(), /*totalSize*/ 0);
        return PolyStatus.Success();
      }
      List<PolyAsset> polyAssets = new List<PolyAsset>();
      foreach (JToken asset in assets) {
        PolyAsset polyAsset;
        if (!(asset is JObject)) {
          Debug.LogWarningFormat("Ignoring asset since it's not a JSON object: " + asset);
          continue;
        }
        JObject jObjectAsset = (JObject)asset;
        if (asset["asset"] != null) {
          // If this isn't null, means we are parsing a ListUserAssets response, which has an added
          // layer of nesting.
          jObjectAsset = (JObject)asset["asset"];
        }
        PolyStatus parseStatus = ParseAsset(jObjectAsset, out polyAsset);
        if (parseStatus.ok) {
          polyAssets.Add(polyAsset);
        } else {
          Debug.LogWarningFormat("Failed to parse a returned asset: {0}", parseStatus);
        }
      }
      var totalSize = results["totalSize"] != null ? int.Parse(results["totalSize"].ToString()) : 0;
      var nextPageToken = results["nextPageToken"] != null ? results["nextPageToken"].ToString() : null;
      polyListAssetsResult = new PolyListAssetsResult(PolyStatus.Success(), totalSize, polyAssets, nextPageToken);
      return PolyStatus.Success();
    }

    /// <summary>
    /// Parses a single asset.
    /// </summary>
    public static PolyStatus ParseAsset(JObject asset, out PolyAsset polyAsset) {
      polyAsset = new PolyAsset();
      
      if (asset["visibility"] == null) {
        return PolyStatus.Error("Asset has no visibility set.");
      }

      polyAsset.name = asset["name"].ToString();
      polyAsset.authorName = asset["authorName"].ToString();
      if (asset["thumbnail"] != null) {
        IJEnumerable<JToken> thumbnailElements = asset["thumbnail"].AsJEnumerable();
        polyAsset.thumbnail = new PolyFile(thumbnailElements["relativePath"].ToString(),
          thumbnailElements["url"].ToString(), thumbnailElements["contentType"].ToString());
      }
      
      if (asset["formats"] == null) {
        Debug.LogError("No formats found");
      } else {
        foreach (JToken format in asset["formats"]) {
          PolyFormat newFormat = ParseAssetsPackage(format);
          newFormat.formatType = ParsePolyFormatType(format["formatType"]);
          if (newFormat.formatType == PolyFormatType.UNKNOWN) {
            PtDebug.Log("Did not recognize format type: " + format["formatType"].ToString());
          }
          polyAsset.formats.Add(newFormat);
        }
      }
      polyAsset.displayName = asset["displayName"].ToString();
      polyAsset.createTime = DateTime.Parse(asset["createTime"].ToString());
      polyAsset.updateTime = DateTime.Parse(asset["updateTime"].ToString());
      polyAsset.visibility = ParsePolyVisibility(asset["visibility"]);
      polyAsset.license = ParsePolyAssetLicense(asset["license"]);
      polyAsset.description = asset["description"]?.ToString() ?? ""; // GAME BUILDER MOD: Actually read description..which we fetch anyway!
      if (asset["isCurated"] != null) {
       polyAsset.isCurated = bool.Parse(asset["isCurated"].ToString());
      }
      return PolyStatus.Success();
    }

    private static PolyFormatType ParsePolyFormatType(JToken token) {
      if (token == null) return PolyFormatType.UNKNOWN;
      string tokenValue = token.ToString();
      return tokenValue == "OBJ" ?  PolyFormatType.OBJ :
        tokenValue == "GLTF2" ? PolyFormatType.GLTF_2 :
        tokenValue == "GLTF" ? PolyFormatType.GLTF :
        tokenValue == "TILT" ? PolyFormatType.TILT :
        tokenValue == "FBX" ? PolyFormatType.FBX : // GAME BUILDER MOD
        PolyFormatType.UNKNOWN;
    }

    private static PolyVisibility ParsePolyVisibility(JToken token) {
      if (token == null) return PolyVisibility.UNSPECIFIED;
      string tokenValue = token.ToString();
      return tokenValue == "PRIVATE" ?  PolyVisibility.PRIVATE :
        tokenValue == "UNLISTED" ? PolyVisibility.UNLISTED :
        tokenValue == "PUBLIC" ? PolyVisibility.PUBLISHED :
        PolyVisibility.UNSPECIFIED;
    }

    private static PolyAssetLicense ParsePolyAssetLicense(JToken token) {
      if (token == null) return PolyAssetLicense.UNKNOWN;
      string tokenValue = token.ToString();
      return tokenValue == "CREATIVE_COMMONS_BY" ? PolyAssetLicense.CREATIVE_COMMONS_BY :
          tokenValue == "ALL_RIGHTS_RESERVED" ? PolyAssetLicense.ALL_RIGHTS_RESERVED :
          PolyAssetLicense.UNKNOWN;
    }

    // As above, accepting a string response (such that we can parse on a background thread).
    public static PolyStatus ParseAsset(string response, out PolyAsset objectStoreEntry,
      bool hackUrls) {
      return ParseAsset(JObject.Parse(response), out objectStoreEntry);
    }

    private static PolyFormat ParseAssetsPackage(JToken token) {
      PolyFormat package = new PolyFormat();
      package.root = new PolyFile(token["root"]["relativePath"].ToString(),
        token["root"]["url"].ToString(), token["root"]["contentType"].ToString());
      // Get the supporting files (resources).
      // Supporting files (including MTL files) are listed under /resource:
      package.resources = new List<PolyFile>();
      if (token["resources"] != null) {
        IJEnumerable<JToken> resourceTags = token["resources"].AsJEnumerable();
        if (resourceTags != null) {
          foreach (JToken resourceTag in resourceTags) {
            if (resourceTag["url"] != null) {
              package.resources.Add(new PolyFile(
                resourceTag["relativePath"].ToString(),
                resourceTag["url"].ToString(),
                resourceTag["contentType"].ToString()));
            }
          }
        }
      }
      // Get the format complexity
      if (token["formatComplexity"] != null) {
        package.formatComplexity = new PolyFormatComplexity();
        if (token["formatComplexity"]["triangleCount"] != null) {
         package.formatComplexity.triangleCount = int.Parse(token["formatComplexity"]["triangleCount"].ToString());
        }
        if (token["formatComplexity"]["lodHint"] != null) {
         package.formatComplexity.lodHint = int.Parse(token["formatComplexity"]["lodHint"].ToString());
        }
      }
      return package;
    }

    /// <summary>
    /// Fetches a list of Poly assets together with metadata, using the given request params.
    /// </summary>
    /// <param name="request">The request to send; can be either a ListAssetsRequest, a ListUserAssetsRequest, or
    /// a ListLikedAssetsRequest.</param>
    /// <param name="callback">The callback to call when the request is complete.</param>
    /// <param name="maxCacheAge">The maximum cache age to use.</param>
    /// <param name="isRecursion"> If true, this is a recursive call to this function, and no
    /// further retries should be attempted.</param>
    public void SendRequest(PolyRequest request, Action<PolyStatus, PolyListAssetsResult> callback,
      long maxCacheAge = DEFAULT_QUERY_CACHE_MAX_AGE_MILLIS, bool isRecursion = false) {
      PolyMainInternal.Instance.webRequestManager.EnqueueRequest(
        () => { return GetRequest(MakeSearchUrl(request), "text/text"); },
        (PolyStatus status, int responseCode, byte[] response) => {
          // Retry the request if this was the first failure. The failure may be a server blip, or may indicate
          // an authentication token has become stale and must be refreshed.
          if (responseCode == 401 || !status.ok) {
            if (isRecursion || !Authenticator.IsInitialized || !Authenticator.Instance.IsAuthenticated) {
              callback(PolyStatus.Error(status, "Query error ({0})", responseCode), null);
              return;
            } else {
              Authenticator.Instance.Reauthorize((PolyStatus reauthStatus) => {
                if (reauthStatus.ok) {
                  SendRequest(request, callback, maxCacheAge: maxCacheAge, isRecursion: true);
                } else {
                  callback(PolyStatus.Error(reauthStatus, "Failed to reauthorize."), null);
                }
              });
            }
          } else {
            PolyMainInternal.Instance.DoBackgroundWork(new ParseAssetsBackgroundWork(
              response, callback));
          }
        }, maxCacheAge);
    }

    /// <summary>
    ///   Fetch a specific asset.
    /// </summary>
    /// <param name="assetId">The asset to be fetched.</param>
    /// <param name="callback">A callback to call with the result of the operation.</param>
    /// <param name="isRecursion">
    ///   If true, this is a recursive call to this function, and no further retries should be attempted.
    /// </param>
    public void GetAsset(string assetId, Action<PolyStatus,PolyAsset> callback, bool isRecursion = false) {  
      // If the user passed in a raw asset ID (no "assets/" prefix), fix it.
      if (!assetId.StartsWith("assets/")) {
        assetId = "assets/" + assetId;
      }
      PolyMainInternal.Instance.webRequestManager.EnqueueRequest(
        () => {
          string url = String.Format("{0}/v1/{1}?key={2}", BASE_URL, assetId, PolyMainInternal.Instance.apiKey);
          return GetRequest(url, "text/text");
        },
        (PolyStatus status, int responseCode, byte[] response) => {
          if (responseCode == 401 || !status.ok) {
            if (isRecursion || !Authenticator.IsInitialized) {
              callback(PolyStatus.Error("Get asset error ({0})", responseCode), null);
              return;
            } else {
              Authenticator.Instance.Reauthorize((PolyStatus reauthStatus) => {
                if (reauthStatus.ok) {
                  GetAsset(assetId, callback, isRecursion: true);
                } else {
                  callback(PolyStatus.Error(reauthStatus, "Failed to reauthenticate to get asset {0}", assetId), null);
                }
              });
            }
          } else {
            PolyMainInternal.Instance.DoBackgroundWork(new ParseAssetBackgroundWork(response,
              callback));
          }
        }, DEFAULT_QUERY_CACHE_MAX_AGE_MILLIS);
    }

    /// <summary>
    ///   Forms a GET request from a HTTP path.
    /// </summary>
    public UnityWebRequest GetRequest(string path, string contentType) {
      // The default constructor for a UnityWebRequest gives a GET request.
      UnityWebRequest request = new UnityWebRequest(path);
      request.SetRequestHeader("Content-type", contentType);
      string token = PolyMainInternal.Instance.GetAccessToken();
      if (token != null) {
        request.SetRequestHeader("Authorization", string.Format("Bearer {0}", token));
      }
      return request;
    }

    public static PolyStatus ParseResponse(byte[] response, out JObject result) {
      try {
        result = JObject.Parse(Encoding.UTF8.GetString(response));
        JToken errorToken = result["error"];
        if (errorToken != null) {
          IJEnumerable<JToken> error = errorToken.AsJEnumerable();
          return PolyStatus.Error("{0}: {1}", error["code"] != null ? error["code"].ToString() : "(no error code)",
              error["message"] != null ? error["message"].ToString() : "(no error message)");
        } else {
          return PolyStatus.Success();
        }
      } catch (Exception ex) {
        result = null;
        return PolyStatus.Error("Failed to parse Poly API response, encountered exception: {0}", ex.Message);
      }

    }
  }
}
