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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PolyToolkitInternal {

public class HelpTextAttribute : PropertyAttribute {
  public string text;
  public HelpTextAttribute(string text) {
    this.text = text;
  }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HelpTextAttribute))]
public class HelpTextDrawer : PropertyDrawer {
  private const int MARGIN = 8;

  private float GetHelpHeight() {
    string text = ((HelpTextAttribute)attribute).text;
    return EditorStyles.wordWrappedMiniLabel.CalcHeight(
      new GUIContent(text), EditorGUIUtility.currentViewWidth);
  }

  public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
    return base.GetPropertyHeight(property, label) + GetHelpHeight() + 2 * MARGIN;
  }

  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    EditorGUI.BeginProperty(position, label, property);
    float helpHeight = GetHelpHeight();
    EditorGUI.LabelField(
      new Rect(position.x, position.y + MARGIN, position.width, helpHeight),
      ((HelpTextAttribute)attribute).text, EditorStyles.wordWrappedMiniLabel);
    EditorGUI.PropertyField(
      new Rect(position.x, position.y + helpHeight + 2 * MARGIN,
        position.width, position.height - helpHeight - 2 * MARGIN), property);
    EditorGUI.EndProperty();
  }
}
#endif

}