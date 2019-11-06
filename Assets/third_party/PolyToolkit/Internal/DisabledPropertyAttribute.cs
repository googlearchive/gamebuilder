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

/// <summary>
/// Annotation that makes a property show up as disabled (read-only) in the Unity Editor inspector.
/// </summary>
public class DisabledPropertyAttribute : PropertyAttribute {}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(DisabledPropertyAttribute))]
public class DisabledPropertyDrawer : PropertyDrawer {
  public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
    bool wasEnabled = GUI.enabled;
    GUI.enabled = false;
    EditorGUI.PropertyField(position, property, label);
    GUI.enabled = wasEnabled;
  }
}
#endif

}