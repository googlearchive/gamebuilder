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
using System.Globalization;
using UnityEngine;
using BehaviorProperties;

namespace BehaviorUX
{
  public class DecimalField : PropertyField
  {
    [SerializeField] TMPro.TextMeshProUGUI label;
    [SerializeField] TMPro.TMP_InputField inputField;
    [SerializeField] UnityEngine.UI.Button AddButton, MinusButton;

    public event System.Action<float> onValueChanged;

    protected override void Initialize()
    {
      Debug.Assert(editor.propType == PropType.Decimal);
      AddButton.onClick.AddListener(() => Add(1f));
      MinusButton.onClick.AddListener(() => Add(-1f));
      inputField.onEndEdit.AddListener(OnInputFieldEnd);
      label.text = editor.labelForDisplay;
    }

    private void Update()
    {
      if (!inputField.isFocused)
      {
        inputField.text = editor.data.ToString();
      }
    }

    void Add(float x)
    {
      float newNum = (float)editor.data + x;
      UpdateNumber(newNum);
    }

    public override bool KeyLock()
    {
      return inputField.isFocused;
    }

    public void UpdateNumber(float x)
    {
      editor.SetData(x);
      onValueChanged?.Invoke(x);
    }

    void OnInputFieldEnd(string s)
    {
      float res = 0;
      System.Single.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out res);
      UpdateNumber(res);
    }
  }
}