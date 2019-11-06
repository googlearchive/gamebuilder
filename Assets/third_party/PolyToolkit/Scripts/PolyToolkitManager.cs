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

using PolyToolkitInternal;

namespace PolyToolkit {
[ExecuteInEditMode]
/// Manages configuration and initialization of the Poly Toolkit.
/// A PolyToolkitManager should be present in every scene that depends on the Poly Toolkit.
public class PolyToolkitManager : MonoBehaviour {
  void Awake() {
    if (Application.isPlaying) {
      // Initialize the Poly Toolkit runtime API, if necessary.
      // (This is a no-op if it was already initialized by the developer).
      PolyApi.Init();
    }

    // Set shader keywords from the settings. This only needs to be done once during runtime
    // (since the settings can't change when the app is running).
    // The settings might change while in edit mode, though, which is why we install the
    // Update() hook below.
    SetKeywordFromSettings();
  }

#if UNITY_EDITOR
  void Update() {
    // PtSettings only changes asynchronously in Editor/edit-mode,
    // so this is unnecessary in Standalone and in Editor/play-mode.
    if (!Application.isPlaying) {
      SetKeywordFromSettings();
    }
  }
#endif

  // Copy value from settings into shader state
  void SetKeywordFromSettings() {
    PtSettings settings = PtSettings.Instance;
    if (settings != null) {
      if (settings.playerColorSpace == ColorSpace.Linear) {
        Shader.EnableKeyword("TBT_LINEAR_TARGET");
      } else {
        Shader.DisableKeyword("TBT_LINEAR_TARGET");
      }
    }
  }
}

}
