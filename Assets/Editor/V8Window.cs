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
using UnityEditor;
using UnityEngine;

public class V8Window : EditorWindow
{
    [MenuItem("V8/Show Window")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(V8Window));
    }

    [MenuItem("V8/Run Perf Tests")]
    public static void RunPerfTests()
    {
        V8InUnity.UnitTests.RunPerformanceExperiments();
    }

  string jsCode = "";

    void OnGUI()
    {
        GUILayout.Label("Type Javascript and run it:");
        jsCode = GUI.TextArea(GUILayoutUtility.GetRect(200, 100), jsCode);
        if (GUILayout.Button("Run!"))
        {
            V8InUnity.Native.Evaluate(jsCode);
        }
    }

  [System.Serializable]
  struct ClickArgs
  {
    public int x;
    public int y;
  }
  [System.Serializable]
  struct KeyPressArgs
  {
    public char key;
  }
  [System.Serializable]
  struct MyEvent<ArgsType>
  {
    public string name;
    public ArgsType args;
  }

  [MenuItem("V8/Test JSON")]
  public static void TestJSON()
  {
    var clickEv = new MyEvent<ClickArgs> { name = "clickccck", args = new ClickArgs { x = 12, y = 34 } };
    Debug.Log("click json: " + JsonUtility.ToJson(clickEv));

    var keyEv = new MyEvent<KeyPressArgs> { name = "keeeeyyy", args = new KeyPressArgs { key = 'f' } };
    Debug.Log("keeeeyyy json: " + JsonUtility.ToJson(keyEv));
  }
}