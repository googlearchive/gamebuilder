/*
 * Copyright 2019 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

// Looks through all prefabs in scene for ScrollRect and adds a SetScrollRectSensitivityByPlatform component
// if the corresponding GameObject doesn't already have one.
public class FixAllScrollRectSentivities : EditorWindow
{
  [MenuItem("Game Builder/Fix all scroll rects")]
  static void Init()
  {
    Debug.Log("***** BEGIN FIXING SCROLL RECT SENSITIVITIES ****");
    string[] prefabPaths = GetAllValidPrefabPaths();
    for (int i = 0; i < prefabPaths.Length; i++)
    {
      var root = PrefabUtility.LoadPrefabContents(prefabPaths[i]);
      ScrollRect[] rects = root.GetComponentsInChildren<UnityEngine.UI.ScrollRect>();
      bool modified = false;
      for (int j = 0; j < rects.Length; j++)
      {
        GameObject go = rects[j].gameObject;
        if (go.GetComponent<SetScrollSensitivityByPlatform>() == null)
        {
          Debug.Log("Adding a SetScrollSensitivityByPlatform component to " + prefabPaths[i]);
          go.AddComponent<SetScrollSensitivityByPlatform>();
          modified = true;
        }
      }
      if (modified)
      {
        bool success = PrefabUtility.SaveAsPrefabAsset(root, prefabPaths[i]);
        Debug.Log("Saved successfully? " + success);
      }
      PrefabUtility.UnloadPrefabContents(root);
    }
    Debug.Log("***** END FIXING SCROLL RECT SENSITIVITIES ****");
  }

  public static string[] GetAllValidPrefabPaths()
  {
    string[] temp = AssetDatabase.GetAllAssetPaths();
    List<string> result = new List<string>();
    foreach (string s in temp)
    {
      if (!s.StartsWith("Assets/third_party") && s.Contains(".prefab")) result.Add(s);
    }
    return result.ToArray();
  }
}
