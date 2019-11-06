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

namespace PolyToolkitInternal {

public static class PtDebug {
  /// <summary>
  /// Set this to true to have Poly Toolkit print debug logs.
  /// </summary>
  public const bool DEBUG_LOG = false;

  /// <summary>
  /// Set this to true (in addition to DEBUG_LOG) to have Poly Toolkit print very verbose debug logs.
  /// </summary>
  public const bool DEBUG_LOG_VERBOSE = false;

  public static void Log(string message) {
    #pragma warning disable 0162  // Don't warn about unreachable code.
    if (DEBUG_LOG) Debug.Log("[PT] " + message);
    #pragma warning restore 0162
  }

  public static void LogFormat(string format, params object[] args) {
    #pragma warning disable 0162  // Don't warn about unreachable code.
    if (DEBUG_LOG) Debug.LogFormat("[PT] " + format, args);
    #pragma warning restore 0162
  }

  public static void LogVerbose(string message) {
    #pragma warning disable 0162  // Don't warn about unreachable code.
    if (DEBUG_LOG_VERBOSE) Debug.Log("[PT VERBOSE] " + message);
    #pragma warning restore 0162
  }

  public static void LogVerboseFormat(string format, params object[] args) {
    #pragma warning disable 0162  // Don't warn about unreachable code.
    if (DEBUG_LOG_VERBOSE) Debug.LogFormat("[PT VERBOSE] " + format, args);
    #pragma warning restore 0162
  }
}
}