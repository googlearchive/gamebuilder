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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PolyToolkitEditor {

/// <summary>
/// Editor scripts that gives a warning to the user whenever they seem to be using the
/// PolyToolkitInternal or PolyToolkitEditor namespaces in one of their scripts.
/// </summary>
[InitializeOnLoad]
public class WarnAboutUsingPolyInternal {
  private const string ERROR_MESSAGE_TITLE = "Warning about non-exported APIs";
  private const string ERROR_MESSAGE_HEADER =
    "Your project seems to be using internal Poly Toolkit classes (from the PolyToolkitInternal or " +
    "PolyToolkitEditor namespaces).\n\n" +
    "These classes are for only meant for internal use by the toolkit, not " +
    "for external consumption, as they can break or change at any time.\n\n" +
    "These are the files that seem to be using private Poly Toolkit classes:\n\n";

  static WarnAboutUsingPolyInternal() {
    string basePath = PtUtils.GetPtBaseLocalPath();

    List<string> offendingFiles = new List<string>();
    foreach (string asset in AssetDatabase.GetAllAssetPaths()) {
      // Only check .cs files.
      if (!asset.EndsWith(".cs")) continue;
      // Don't check Poly Toolkit script files.
      if (asset.StartsWith(basePath + "/")) continue;
      // If we got here, this is a user-defined script file. Let's check that it's not using PolyToolkitInternal.
      // Note that the asset database cannot always be trusted; sometimes the assets do not exist.
      string contents;
      try { contents = File.ReadAllText(asset); }
      catch (FileNotFoundException) { continue; }

      // If the user has silenced the warning by adding the special marker, skip this file.
      if (contents.Contains("NO_POLY_TOOLKIT_INTERNAL_CHECK")) continue;

      // This is a pretty naive check but I can't think of any legitimate reason someone would have the string
      // "PolyToolkitInternal" or "PolyToolkitEditor" in their script file. But if they do, they can add
      // NO_POLY_TOOLKIT_INTERNAL_CHECK to the file to silence the warning.
      if (contents.Contains("PolyToolkitInternal") || contents.Contains("PolyToolkitEditor")) {
        offendingFiles.Add(asset);
      }
    }
    if (offendingFiles.Count > 0) {
      string errorMessage = ERROR_MESSAGE_HEADER + string.Join("\n", offendingFiles.ToArray());
      Debug.LogError(errorMessage);
      EditorUtility.DisplayDialog(ERROR_MESSAGE_TITLE, errorMessage, "OK");
    }
  }
}

}