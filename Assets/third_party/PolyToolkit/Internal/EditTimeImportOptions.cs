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

using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

using PolyToolkit;

namespace PolyToolkitInternal {

/// <summary>
/// Options that indicate how the user wants to import a given asset at edit time.
/// This includes the options normally available for run-time importing (PolyImportOptions),
/// but adds some parameters that are only relevant to edit-time importing.
/// </summary>
[Serializable]
public struct EditTimeImportOptions {
  /// <summary>
  /// Basic options, which are the same as the runtime import options.
  /// </summary>
  public PolyImportOptions baseOptions;

  /// <summary>
  /// If true, also instantiate the imported object (add it to the scene).
  /// </summary>
  public bool alsoInstantiate;
}

#if UNITY_EDITOR
public static class ImportOptionsGui {
  public static EditTimeImportOptions ImportOptionsField(EditTimeImportOptions options) {
    GUILayout.BeginHorizontal();
    GUILayout.Space(10);

    GUILayout.BeginVertical();

    options.baseOptions.recenter = EditorGUILayout.Toggle(
        new GUIContent("Recenter", "If checked, object will be repositioned so it's centered " + 
        "in the GameObject"), options.baseOptions.recenter);
    options.alsoInstantiate = EditorGUILayout.Toggle(
        new GUIContent("Also Instantiate", "If checked, object will also be instantiated into scene."),
        options.alsoInstantiate);

    options.baseOptions.rescalingMode = (PolyImportOptions.RescalingMode)EditorGUILayout.EnumPopup(
        new GUIContent("Rescale Mode", "Indicates how the object will be scaled."),
        options.baseOptions.rescalingMode);

    switch (options.baseOptions.rescalingMode) {
      case PolyImportOptions.RescalingMode.CONVERT:
        GUILayout.BeginHorizontal();
        GUILayout.Space(150);
        GUILayout.Label("This object will automatically convert to your scene's measurement units. " +
            "The original object is assumed to be in meters.",
            EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(150);
        options.baseOptions.scaleFactor = EditorGUILayout.FloatField(new GUIContent("Scale factor",
            "Scale factor to apply to object."), options.baseOptions.scaleFactor);
        GUILayout.EndHorizontal();
        break;
      case PolyImportOptions.RescalingMode.FIT:
        GUILayout.BeginHorizontal();
        GUILayout.Space(150);
        GUILayout.Label("This object will be rescaled to fit the desired size. Enter the " +
            "desired size in scene units.",
            EditorStyles.wordWrappedLabel);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Space(150);
        options.baseOptions.desiredSize = EditorGUILayout.FloatField(new GUIContent("Desired size",
            "Desired size of object's bounding box (cube)"), options.baseOptions.desiredSize);
        GUILayout.EndHorizontal();
        break;
      default:
        throw new Exception("Unexpected rescaling mode: " + options.baseOptions.rescalingMode);
    }
    GUILayout.EndVertical();
    GUILayout.EndHorizontal();
    return options;
  }
}
#endif

}
