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

using UnityEngine;
using UnityEngine.Networking;

namespace PolyToolkitInternal {
  /// <summary>
  /// Utilities for compatibility with multiple Unity versions.
  /// </summary>
  public static class UnityCompat {
    public static UnityWebRequest GetTexture(string url) {
#if UNITY_2017_1_OR_NEWER
      // New API.
      return UnityWebRequestTexture.GetTexture(url);
#else
      // Old API, removed in Unity 2017.1.
      return UnityWebRequest.GetTexture(url);
#endif
    }

    public static bool IsNetworkError(UnityWebRequest req) {
#if UNITY_2017_1_OR_NEWER
      // New API.
      return req.isNetworkError;
#else
      // Old API (Unity 5.6).
      return req.isError;
#endif
    }

    public static AsyncOperation SendWebRequest(UnityWebRequest req) {
#if UNITY_2017_2_OR_NEWER
      return req.SendWebRequest();
#else
      return req.Send();
#endif
    }
  }
}
