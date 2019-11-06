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

using UnityEngine.Networking;
using PolyToolkit;

namespace PolyToolkitInternal.api_clients.poly_client {
  public static class PolyClientUtils {
    public delegate void GetRawFileDataTextCallback(PolyStatus status, string fileData);
    public delegate void GetRawFileDataBytesCallback(PolyStatus status, byte[] fileData);

    /// <summary>
    /// Gets raw file data from Poly given a data URL.
    /// </summary>
    /// <param name="dataUrl">Data URL to retrieve from.</param>
    /// <param name="accessToken">The access token to use for authentication.</param>
    /// <param name="callback">The callback to call when the download is complete.</param>
    /// <param name="maxCacheAgeMillis">Maximum age of the cached copy, in millis. See
    /// WebRequestManager for useful constants.</param>
    public static void GetRawFileText(string dataUrl, string accessToken, long maxCacheAgeMillis,
        GetRawFileDataTextCallback callback) {
      PolyMainInternal.Instance.webRequestManager.EnqueueRequest(
        () => { return MakeRawFileGetRequest(dataUrl, accessToken); },
        (PolyStatus status, int responseCode, byte[] response) => {
          if (!status.ok) {
            callback(PolyStatus.Error(status, "Failed to get raw file text for {0}", dataUrl), null);
          } else {
            callback(PolyStatus.Success(), System.Text.Encoding.UTF8.GetString(response));
          }
        }, maxCacheAgeMillis);
    }
    
    /// <summary>
    /// Gets raw file data from Poly given a data URL.
    /// </summary>
    /// <param name="dataUrl">Data URL to retrieve from.</param>
    /// <param name="accessToken">The access token to use for authentication.</param>
    /// <param name="callback">The callback to call when the download is complete.</param>
    /// <param name="maxCacheAgeMillis">Maximum age of the cached copy, in millis. See
    /// WebRequestManager for useful constants.</param>
    public static void GetRawFileBytes(string dataUrl, string accessToken,
        long maxCacheAgeMillis, GetRawFileDataBytesCallback callback) {

      PolyMainInternal.Instance.webRequestManager.EnqueueRequest(
        () => { return MakeRawFileGetRequest(dataUrl, accessToken); },
        (PolyStatus status, int responseCode, byte[] response) => {
          if (!status.ok) {
            callback(PolyStatus.Error(status, "Failed to get raw file bytes for {0}", dataUrl), null);
          } else {
            callback(PolyStatus.Success(), response);
          }
        }, maxCacheAgeMillis);
    }

    /// <summary>
    /// Constructs a GET request for the given data URL with the given access token.
    /// </summary>
    /// <param name="dataUrl">The data URL to retrieve.</param>
    /// <param name="accessToken">The access token. If null, no authentication will be provided (should only be used
    /// for URLs requiring no authentication).</param>
    /// <returns>The web request. It will NOT be sent (the caller is responsible for sending).</returns>
    private static UnityWebRequest MakeRawFileGetRequest(string dataUrl, string accessToken) {
      UnityWebRequest request = new UnityWebRequest(dataUrl.ToString());
      request.method = UnityWebRequest.kHttpVerbGET;
      request.SetRequestHeader("Content-Type", "text/plain");
      request.SetRequestHeader("Token", "c1820c69-9818-45b9-83ae-a5b9784a90a3");
      if (accessToken != null) {
        request.SetRequestHeader("Authorization", string.Format("Bearer {0}", accessToken));
      }
      return request;
    }
  }
}