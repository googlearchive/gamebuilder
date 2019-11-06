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
using UnityEngine.UI;
using System.Collections.Generic;

using PolyToolkit;

/// <summary>
/// Example that shows the top 5 featured models.
/// 
/// This example requests the list of featured assets from Poly. Then it imports the top 5
/// into the scene, side by side.
/// </summary>
public class ShowFeaturedExample : MonoBehaviour {

  // ATTENTION: Before running this example, you must set your API key in Poly Toolkit settings.
  //   1. Click "Poly | Poly Toolkit Settings..."
  //      (or select PolyToolkit/Resources/PtSettings.asset in the editor).
  //   2. Click the "Runtime" tab.
  //   3. Enter your API key in the "Api key" box.
  //
  // This example does not use authentication, so there is no need to fill in a Client ID or Client Secret.

  // Number of assets imported so far.
  private int assetCount = 0;

  // Text field where we display the attributions (credits) for the assets we display.
  public Text attributionsText;

  // Status bar text.
  public Text statusText;

  private void Start() {
    // Request a list of featured assets from Poly.
    Debug.Log("Getting featured assets...");
    statusText.text = "Requesting...";

    PolyListAssetsRequest request = PolyListAssetsRequest.Featured();
    // Limit requested models to those of medium complexity or lower.
    request.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
    PolyApi.ListAssets(request, ListAssetsCallback);
  }

  // Callback invoked when the featured assets results are returned.
  private void ListAssetsCallback(PolyStatusOr<PolyListAssetsResult> result) {
    if (!result.Ok) {
      Debug.LogError("Failed to get featured assets. :( Reason: " + result.Status);
      statusText.text = "ERROR: " + result.Status;
      return;
    }
    Debug.Log("Successfully got featured assets!");
    statusText.text = "Importing...";

    // Set the import options.
    PolyImportOptions options = PolyImportOptions.Default();
    // We want to rescale the imported meshes to a specific size.
    options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
    // The specific size we want assets rescaled to (fit in a 1x1x1 box):
    options.desiredSize = 1.0f;
    // We want the imported assets to be recentered such that their centroid coincides with the origin:
    options.recenter = true;

    // Now let's get the first 5 featured assets and put them on the scene.
    List<PolyAsset> assetsInUse = new List<PolyAsset>();
    for (int i = 0; i < Mathf.Min(5, result.Value.assets.Count); i++) {
      // Import this asset.
      PolyApi.Import(result.Value.assets[i], options, ImportAssetCallback);
      assetsInUse.Add(result.Value.assets[i]);
    }

    // Show attributions for the assets we display.
    attributionsText.text = PolyApi.GenerateAttributions(includeStatic: true, runtimeAssets: assetsInUse);
  }

  // Callback invoked when an asset has just been imported.
  private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result) {
    if (!result.Ok) {
      Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
      return;
    }

    // Position each asset evenly spaced from the next.
    assetCount++;
    result.Value.gameObject.transform.position = new Vector3(assetCount * 1.5f, 0f, 0f);

    statusText.text = "Imported " + assetCount + " assets";
  }
}