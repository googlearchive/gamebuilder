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
using UnityEditor;
using UnityEditor.Build;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

[InitializeOnLoad]
public class CheckBuild : IPreprocessBuild {
  public int callbackOrder { get { return 0; } }
  public void OnPreprocessBuild(BuildTarget target, string path) {
    CheckApiCompatibilitySetting();
    // In the future, we can add other checks here.
  }

  private static void CheckApiCompatibilitySetting() {
    if (!PtSettings.Instance.warnOfApiCompatibility) return;
    BuildTargetGroup target = EditorUserBuildSettings.selectedBuildTargetGroup;
    ApiCompatibilityLevel apiCompat = PlayerSettings.GetApiCompatibilityLevel(target);
    ScriptingImplementation scriptBackend = PlayerSettings.GetScriptingBackend(target);

    // If the user is trying to build for the IL2CPP backend, and has something other than .NET 2.0 full
    // selected as API compatibility level, warn them.
    if (scriptBackend == ScriptingImplementation.IL2CPP && apiCompat != ApiCompatibilityLevel.NET_2_0) {
      EditorUtility.DisplayDialog("API Compatibility", "Warning: You are building for the IL2CPP script " +
        "backend (AOT compilation) and have '.NET 2.0 Subset' selected as API compatibility level.\n\n" +
        "Poly Toolkit runtime needs '.NET 2.0' (full) when compiling for IL2CPP, or runtime errors " +
        "may occur.\n\n" +
        "If you see problems, go to Player Settings and change your API compatibility level " + 
        "to '.NET 2.0', and try to build again.\n\n" +
        "(You can silence this warning in Poly Toolkit settings if it's not useful)", "OK");
    }
  }
}
}