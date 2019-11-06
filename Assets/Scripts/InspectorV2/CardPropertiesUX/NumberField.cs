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

namespace BehaviorUX
{
  public class NumberField : TypeField<int>
  {
    [SerializeField] TMPro.TMP_InputField inputField;
    [SerializeField] UnityEngine.UI.Button AddButton, MinusButton;

    void Awake()
    {
      AddButton.onClick.AddListener(() => Increment(1));
      MinusButton.onClick.AddListener(() => Increment(-1));
      inputField.onEndEdit.AddListener(OnInputFieldEnd);
    }

    public override void SetValue(int value)
    {
      if (!inputField.isFocused) inputField.text = value.ToString();
    }

    private void Increment(int x)
    {
      int newNum = System.Int32.Parse(inputField.text) + x;
      onValueChanged?.Invoke(newNum);
    }

    private void OnInputFieldEnd(string s)
    {
      int res;
      System.Int32.TryParse(s, out res);
      onValueChanged?.Invoke(res);
    }

    public bool IsFocused()
    {
      return inputField.isFocused;
    }
  }
}