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

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CheckUnityVersion
{
  private const string EXPECTED_UNITY_VERSION = "2018.4.12f1";
  static CheckUnityVersion()
  {
    if (Application.unityVersion != EXPECTED_UNITY_VERSION)
    {
      EditorUtility.DisplayDialog(
        "WARNING: Unexpected Unity version.",
        string.Format(
          "**** UNEXPECTED UNITY VERSION ****\n\n" +
          "You are using Unity version:\n        {0}.\n" +
          "The expected Unity version for this project is:\n        {1}.\n\n" +
          "If you are intentionally upgrading, please edit Editor/CheckUnityVersion.cs and change EXPECTED_UNITY_VERSION to fix this warning.",
          Application.unityVersion, EXPECTED_UNITY_VERSION), "OK");
    }
  }
}

