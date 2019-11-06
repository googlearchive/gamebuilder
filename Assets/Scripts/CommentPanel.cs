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

public class CommentPanel : MonoBehaviour, VoosActor.TextRenderer
{
  float padding = .1f;
  [SerializeField] TMPro.TextMeshPro textField;
  [SerializeField] Transform backPanel;

  public void SetText(string s)
  {
    textField.text = s;
    Vector2 newScale = textField.GetPreferredValues(s) + Vector2.one * padding;
    backPanel.localScale = new Vector3(newScale.x, newScale.y, 1);
    textField.rectTransform.sizeDelta = newScale;
  }
}
