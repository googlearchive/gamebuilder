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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseoverTooltip : MonoBehaviour
{
  UserMain userMain;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform rectTransformParent;
  [SerializeField] TMPro.TextMeshProUGUI textField;

  const float TEXT_PADDING = 5;
  [SerializeField] Vector2 rectOffset = new Vector3(40, -65);

  void Update()
  {
    Vector2 localPos;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransformParent, Input.mousePosition, null, out localPos);
    Vector2 newPos = localPos + rectOffset;

    // If we're past the right edge of the screen, correct the position.
    if (newPos.x + rectTransform.sizeDelta.x > rectTransformParent.rect.width / 2f)
    {
      newPos.x = rectTransformParent.rect.width / 2f - rectTransform.sizeDelta.x;
    }

    rectTransform.anchoredPosition = newPos;

    if (userMain != null && !userMain.CursorActive())
    {
      SetText("");
    }
  }

  public void SetText(string _text)
  {
    gameObject.SetActive(_text.Length != 0);

    textField.text = _text;
    rectTransform.sizeDelta = textField.GetPreferredValues(_text) + Vector2.one * TEXT_PADDING; ;//newScale;

    if (gameObject.activeSelf) Update();
  }

  internal void Setup(UserMain userMain)
  {
    this.userMain = userMain;
  }
}
