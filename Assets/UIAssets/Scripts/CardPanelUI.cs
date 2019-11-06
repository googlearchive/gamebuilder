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

public class CardPanelUI : MonoBehaviour
{
  public UnityEngine.UI.Image headerIcon;
  public TMPro.TextMeshProUGUI headerTextInputTextField;
  public UnityEngine.UI.Button closeButton;
  public RectTransform fieldsParent;
  public RectTransform deckParent;
  public RectTransform referencRect;
  public UnityEngine.UI.Image headerBackground;
  public TMPro.TMP_InputField headerTextInput;
  public UnityEngine.UI.LayoutElement headerTextInputLayout;

  void Update()
  {

    if (headerTextInput.isFocused)
    {
      headerTextInputLayout.preferredWidth = headerTextInputTextField.GetPreferredValues(headerTextInput.text).x;
    }
  }

  public void SetHeaderText(string newtext)
  {
    headerTextInput.text = newtext;
    headerTextInputLayout.preferredWidth = headerTextInputTextField.GetPreferredValues(headerTextInput.text).x;
  }


}
