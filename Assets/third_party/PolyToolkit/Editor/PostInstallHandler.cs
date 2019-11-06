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

using System.IO;
using UnityEngine;
using UnityEditor;
using System;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

[InitializeOnLoad]
public class PostInstallHandler {
  static PostInstallHandler() {
    if (Application.isPlaying) return;

    // Don't run the upgrade logic in the Poly Toolkit source project. We only want it to run
    // when users have installed it.
    if (Application.companyName == "Google" && Application.productName == "PolyToolkitUnity") return;

    // Add HandlePostInstall method to the Editor update loop so it runs after the rest of
    // PolyToolkit has been initialized.
    EditorApplication.update += HandlePostInstall;
  }
  
  public static void HandlePostInstall() {
    // Remove from the Editor update loop; we only need this to run once.
    EditorApplication.update -= HandlePostInstall;

    // Check if user just installed or upgraded Poly Toolkit.
    string basePath = PtUtils.GetPtBaseLocalPath();
    string upgradeFilePath = PtUtils.ToAbsolutePath(basePath + "/upgrade.dat");
    string currentVersion = "";
    bool isUpgrade = false;
    try {
      currentVersion = File.ReadAllText(upgradeFilePath).Trim();
    } catch (Exception) {}

    if (currentVersion == PtSettings.Version.ToString()) return;
    isUpgrade = !string.IsNullOrEmpty(currentVersion);
    // Show the welcome window.
    WelcomeWindow.ShowWelcomeWindow();
    AssetBrowserWindow.BrowsePolyAssets();
    File.WriteAllText(upgradeFilePath, PtSettings.Version.ToString());

    // In the future, if we need to do any post-upgrade maintenance, we can add it here.
    PtAnalytics.SendEvent(isUpgrade ? PtAnalytics.Action.INSTALL_UPGRADE
      : PtAnalytics.Action.INSTALL_NEW_2, PtSettings.Version.ToString());
  }
}
}