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
using PolyToolkitInternal;

namespace PolyToolkitEditor {

/// <summary>
/// Custom editor to edit the PtSettings asset.
///
/// This editor organizes the PtSettings's properties into tabs for ease
/// of editing.
/// </summary>
[CustomEditor(typeof(PtSettings))]
public class PtSettingsEditor : Editor {
  private const string TITLE = "Poly Toolkit Settings";

  private TabDescriptor[] tabs;
  private string[] tabTitles;

  int selectedTabIndex;

  private void OnEnable() {
    // Defines the properties to show in each tab.
    tabs = new TabDescriptor[] {
      new TabDescriptor(serializedObject, "General", new string[] {
        "sceneUnit",
        "surfaceShaderMaterials",
        "basePbrOpaqueDoubleSidedMaterial",
        "basePbrBlendDoubleSidedMaterial",
        "warnOfApiCompatibility",
      }),
      new TabDescriptor(serializedObject, "Editor", new string[] {
        "assetObjectsPath",
        "assetSourcesPath",
        "resourcesPath",
        "defaultImportOptions",
        "sendEditorAnalytics",
      }),
      new TabDescriptor(serializedObject, "Runtime", new string[] {
        "authConfig",
        "cacheConfig",
      }),
    };
    tabTitles = new string[tabs.Length];
    for (int i = 0; i < tabs.Length; i++) {
      tabTitles[i] = tabs[i].title;
    }
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();

    GUILayout.Label(TITLE, EditorStyles.boldLabel);
    selectedTabIndex = GUILayout.Toolbar(selectedTabIndex, tabTitles);
    TabDescriptor selectedTab = tabs[selectedTabIndex];

    GUILayout.Space(10);
    GUILayout.Label("Hover the mouse over a setting to display a tooltip.", EditorStyles.wordWrappedMiniLabel);
    GUILayout.Space(10);

    foreach (SerializedProperty property in selectedTab.properties) {
      EditorGUILayout.PropertyField(property, /* includeChildren */ true);
    }

    serializedObject.ApplyModifiedProperties();
  }

  [MenuItem("Poly/Poly Toolkit Settings...", priority = 1000)]
  public static void ShowPolyToolkitSettings() {
    PtAnalytics.SendEvent(PtAnalytics.Action.MENU_SHOW_SETTINGS);
    Selection.activeObject = PtSettings.Instance;
  }

  private class TabDescriptor {
    public string title;
    public string[] propertyNames;
    public SerializedProperty[] properties;
    public TabDescriptor(SerializedObject serializedObject, string title, string[] propertyNames) {
      this.title = title;
      this.propertyNames = propertyNames;
      properties = new SerializedProperty[propertyNames.Length];
      for (int i = 0; i < properties.Length; i++) {
        properties[i] = serializedObject.FindProperty(propertyNames[i]);
        if (properties[i] == null) {
          throw new System.Exception("PtSettingsEditor: Property not found: " + propertyNames[i]);
        }
      }
    }
  }
}
}
