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
using System.Collections.Generic;
using System;

namespace PolyToolkitEditor {

/// <summary>
/// This is an error-checking proxy to GUI calls that require balancing ("BeginFoo()" .. "EndFoo()").
///
/// For example, BeginHorizontal()..EndHorizontal(), BeginArea()..EndArea(). Instead of directly
/// calling these methods in GUILayout, call them in this class instead for automatic error
/// checking.
///
/// At the end of your OnGUI() method, call FinishAndCheck() to verify if you left anything
/// unbalanced.
/// </summary>
public class GUIHelper {
  private enum Item {
    VERTICAL,
    HORIZONTAL,
    AREA,
    SCROLL_VIEW,
  }

  private Stack<Item> unbalancedItems = new Stack<Item>();

  public GUIHelper() {}

  public void BeginHorizontal(params GUILayoutOption[] options) {
    GUILayout.BeginHorizontal(options);
    unbalancedItems.Push(Item.HORIZONTAL);
  }

  public void EndHorizontal() {
    PopAndCheck(Item.HORIZONTAL);
    GUILayout.EndHorizontal();
  }

  public void BeginVertical(params GUILayoutOption[] options) {
    GUILayout.BeginVertical(options);
    unbalancedItems.Push(Item.VERTICAL);
  }

  public void EndVertical() {
    PopAndCheck(Item.VERTICAL);
    GUILayout.EndHorizontal();
  }

  public Vector2 BeginScrollView(Vector2 scrollPos) {
    unbalancedItems.Push(Item.SCROLL_VIEW);
    return GUILayout.BeginScrollView(scrollPos);
  }

  public void EndScrollView() {
    PopAndCheck(Item.SCROLL_VIEW);
    GUILayout.EndScrollView();
  }

  public void BeginArea(Rect screenRect) {
    unbalancedItems.Push(Item.AREA);
    GUILayout.BeginArea(screenRect);
  }

  public void EndArea() {
    PopAndCheck(Item.AREA);
    GUILayout.EndArea();
  }

  public void FinishAndCheck() {
    if (unbalancedItems.Count == 0) return;
    Debug.LogErrorFormat("{0} unbalanced GUI elements found.", unbalancedItems.Count);
    foreach (Item item in unbalancedItems) {
      Debug.LogErrorFormat("Unbalanced GUI element: {0}", item);
    }
    // Reset for the next GUI iteration.
    unbalancedItems.Clear();
  }

  private void PopAndCheck(Item expectedItem) {
    Item actualItem;
    if (unbalancedItems.Count == 0) {
      throw new Exception("Error: GUIHelper stack underflow. Unbalanced items in GUI.");
    } else if (expectedItem != (actualItem = unbalancedItems.Pop())) {
      throw new Exception(string.Format("Error: GUIHelper expeted to pop {0}, got {1} instead.",
        expectedItem, actualItem));
    }
  }
}
}
