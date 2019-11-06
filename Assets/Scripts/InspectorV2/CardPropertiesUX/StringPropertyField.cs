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
  public class StringPropertyField : PropertyField
  {
    [SerializeField] StringField stringField;

    public delegate void OnValueChanged(string value);
    public event OnValueChanged onValueChanged;

    protected override void Initialize()
    {
      stringField.SetListener(OnStringValueChanged);
      stringField.SetLabel(editor.labelForDisplay);
    }

    private void Update()
    {
      stringField.SetValue(editor.data.ToString());
    }

    public override bool KeyLock()
    {
      return stringField.IsFocused();
    }

    void OnStringValueChanged(string newstring)
    {
      SetStringField(newstring);
    }

    void SetStringField(string newstring)
    {
      editor.SetData(newstring);
      onValueChanged?.Invoke(newstring);
    }

    string GetStringField()
    {
      return (string)editor.data;
    }
  }
}