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
using UnityEditor;
using UnityEngine;
using PolyToolkitInternal;

namespace PolyToolkitEditor {
/// <summary>
/// Welcome window that shows after the Poly Toolk was just installed.
/// </summary>
public class WelcomeWindow : EditorWindow {
  /// <summary>
  /// Title of the window (shown in the Unity UI).
  /// </summary>
  private const string WINDOW_TITLE = "Welcome to Poly Toolkit";

  /// <summary>
  /// Texture to use for the welcome image.
  /// </summary>
  private const string WELCOME_TEX = "Editor/Textures/PolyToolkitWelcome.png";

  /// <summary>
  /// URL for online documentation page.
  /// </summary>
  private const string ONLINE_DOCUMENTATION_URL = "https://developers.google.com/poly/develop/unity";

  private const int DEFAULT_WIDTH = 500;
  private const int DEFAULT_HEIGHT = 500;
  private const int WELCOME_TEX_WIDTH = 500;
  private const int WELCOME_TEX_HEIGHT = 150;

  /// <summary>
  /// Height of the welcome bar.
  /// </summary>
  private const int WELCOME_BAR_HEIGHT = 150;

  private const int PADDING = 20;

  [NonSerialized]
  private bool initialized = false;

  private Texture2D welcomeTex;

  /// <summary>
  /// GUI helper that keeps track of our open layouts.
  /// </summary>
  private GUIHelper guiHelper = new GUIHelper();

  [MenuItem("Poly/General Information...")]
  public static void ShowWelcomeWindow() {
    PtAnalytics.SendEvent(PtAnalytics.Action.MENU_GENERAL_INFORMATION);
    GetWindow<WelcomeWindow>().Show();
  }

  [MenuItem("Poly/Online documentation...")]
  public static void ShowOnlineDocumentation() {
    PtAnalytics.SendEvent(PtAnalytics.Action.MENU_ONLINE_DOCUMENTATION);
    Application.OpenURL(ONLINE_DOCUMENTATION_URL);
  }

  private void Initialize() {
    welcomeTex = PtUtils.LoadTexture2DFromRelativePath(WELCOME_TEX);
    position = new Rect(position.xMin, position.yMin, DEFAULT_WIDTH, DEFAULT_HEIGHT);
  }

  public void OnGUI() {
    if (!initialized) {
      Initialize();
      initialized = true;
    }
    GUI.DrawTexture(new Rect(0, 0, position.width, WELCOME_BAR_HEIGHT), Texture2D.whiteTexture);
    GUI.DrawTexture(new Rect((position.width - WELCOME_TEX_WIDTH) * 0.5f, 0, WELCOME_TEX_WIDTH,
        WELCOME_TEX_HEIGHT), welcomeTex);

    guiHelper.BeginArea(new Rect(PADDING, WELCOME_TEX_HEIGHT + PADDING,
        position.width - 2 * PADDING, position.height - 2 * PADDING));
    GUILayout.Label("Welcome to Poly Toolkit!", EditorStyles.boldLabel);
    GUILayout.Label("Version: " + PtSettings.Version);
    GUILayout.Space(10);

    GUILayout.Label(
        "This toolkit allows you to import assets from Poly into your project " +
        "at edit time and at runtime.\n\n" +
        "The Poly Toolkit window was added to your editor. You can use it as a separate " +
        "window or dock like any tool window. If you close it, you can access it again " +
        "from the Poly menu.\n\n" +
        PolyInternalUtils.ATTRIBUTION_NOTICE + "\n\n" +
        "Have fun!",
        EditorStyles.wordWrappedLabel);

    guiHelper.BeginHorizontal();
    GUILayout.FlexibleSpace();
    if (GUILayout.Button("Close")) {
      Close();
    }
    GUILayout.FlexibleSpace();
    guiHelper.EndHorizontal();
    guiHelper.EndArea();
  }
}

}
