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
  private const string SUPPORTED_UNITY_VERSION = "2018.4.9f1";
  static CheckUnityVersion()
  {
    if (Application.unityVersion != SUPPORTED_UNITY_VERSION)
    {
      EditorUtility.DisplayDialog(
        "Incorrect Unity version.",
        string.Format(
          "**** WRONG UNITY VERSION ****\n\n" +
          "You are using Unity version:\n        {0}.\n" +
          "The correct Unity version for this project is:\n        {1}.\n\n" +
          "Please DO NOT COMMIT any Unity asset files generated with an unsupported Unity " +
          "version, as that might break other team members.\n\n" +
          "Please switch to the supported Unity version!\n\n" +
          "UNITY WILL NOW QUIT!",
          Application.unityVersion, SUPPORTED_UNITY_VERSION), "OK");
      EditorApplication.Exit(0);
    }
  }
}

