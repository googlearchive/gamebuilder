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
using UnityEditor;
using System.IO;
using System.Text;
using PolyToolkit;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

/// <summary>
/// Generates the attributions file.
/// </summary>
public static class AttributionFileGenerator {
  [MenuItem("Poly/Update Attributions File")]
  public static void Generate() {
    PtAnalytics.SendEvent(PtAnalytics.Action.MENU_UPDATE_ATTRIBUTIONS_FILE);
    Generate(/* showUi */ true);
  }

  /// <summary>
  /// Scans the project for PtAsset assets marked as third-party and generates an attributions
  /// file in the user's Resources directory containing a list of those resources, the names
  /// of the authors and links to the original creations.
  /// </summary>
  public static void Generate(bool showUi) {
    string fileFullPath = Path.Combine(PtUtils.ToAbsolutePath(PtSettings.Instance.resourcesPath),
      AttributionGeneration.ATTRIB_FILE_NAME);

    string[] assetGuids = AssetDatabase.FindAssets(PtAsset.FilterString);
    // List of assets that are licensed under Creative Commons (require attribution).
    List<PtAsset> ccByAssets = new List<PtAsset>();
    foreach (string assetGuid in assetGuids) {
      string localPath = AssetDatabase.GUIDToAssetPath(assetGuid);
      PtAsset ptAsset = AssetDatabase.LoadAssetAtPath<PtAsset>(localPath);
      if (ptAsset != null && ptAsset.license == PolyAssetLicense.CREATIVE_COMMONS_BY) {
        ccByAssets.Add(ptAsset);
      }
    }

    if (ccByAssets.Count == 0) {
      // No need for an attribution file.
      if (File.Exists(fileFullPath)) {
        File.Delete(fileFullPath);
      }
      if (showUi) {
        EditorUtility.DisplayDialog("No Assets Require Attribution",
          "No Poly assets were found in the project that require attribution. " +
          "No attribution file was generated.", "OK");
      }
      return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(fileFullPath));
    StringBuilder sb = new StringBuilder();
    sb.AppendLine(AttributionGeneration.FILE_HEADER);
    ccByAssets.Sort((PtAsset a, PtAsset b) => { return a.title.CompareTo(b.title); });
    foreach (PtAsset ptAsset in ccByAssets) {
      sb.AppendLine();
      sb.Append(AttributionGeneration.GenerateAttributionString(ptAsset.title, ptAsset.author,
        ptAsset.url, AttributionGeneration.CC_BY_LICENSE)).AppendLine();
    }
    File.WriteAllText(fileFullPath, sb.ToString());

    if (showUi) {
      EditorUtility.DisplayDialog("File Generated",
        "Attributions file generated at:\n" + fileFullPath + ".\n\nYou can load this file at runtime " +
        "and display it with Resources.Load().", "OK");
    }
  }
}
}
