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

using PolyToolkit;

/// <summary>
/// Simple example that loads and displays one asset.
/// 
/// This example requests a specific asset and displays it.
/// </summary>
public class HelloPoly : MonoBehaviour {

  // ATTENTION: Before running this example, you must set your API key in Poly Toolkit settings.
  //   1. Click "Poly | Poly Toolkit Settings..."
  //      (or select PolyToolkit/Resources/PtSettings.asset in the editor).
  //   2. Click the "Runtime" tab.
  //   3. Enter your API key in the "Api key" box.
  //
  // This example does not use authentication, so there is no need to fill in a Client ID or Client Secret.

  // Text where we display the current status.
  public Text statusText;

  private void Start() {
    // Request the asset.
    Debug.Log("Requesting asset...");
    PolyApi.GetAsset("assets/5vbJ5vildOq", GetAssetCallback);
    statusText.text = "Requesting...";
  }

  // Callback invoked when the featured assets results are returned.
  private void GetAssetCallback(PolyStatusOr<PolyAsset> result) {
    if (!result.Ok) {
      Debug.LogError("Failed to get assets. Reason: " + result.Status);
      statusText.text = "ERROR: " + result.Status;
      return;
    }
    Debug.Log("Successfully got asset!");

    // Set the import options.
    PolyImportOptions options = PolyImportOptions.Default();
    // We want to rescale the imported mesh to a specific size.
    options.rescalingMode = PolyImportOptions.RescalingMode.FIT;
    // The specific size we want assets rescaled to (fit in a 5x5x5 box):
    options.desiredSize = 5.0f;
    // We want the imported assets to be recentered such that their centroid coincides with the origin:
    options.recenter = true;

    statusText.text = "Importing...";
    PolyApi.Import(result.Value, options, ImportAssetCallback);
  }

  // Callback invoked when an asset has just been imported.
  private void ImportAssetCallback(PolyAsset asset, PolyStatusOr<PolyImportResult> result) {
    if (!result.Ok) {
      Debug.LogError("Failed to import asset. :( Reason: " + result.Status);
      statusText.text = "ERROR: Import failed: " + result.Status;
      return;
    }
    Debug.Log("Successfully imported asset!");

    // Show attribution (asset title and author).
    statusText.text = asset.displayName + "\nby " + asset.authorName;

    // Here, you would place your object where you want it in your scene, and add any
    // behaviors to it as needed by your app. As an example, let's just make it
    // slowly rotate:
    result.Value.gameObject.AddComponent<Rotate>();
  }
}