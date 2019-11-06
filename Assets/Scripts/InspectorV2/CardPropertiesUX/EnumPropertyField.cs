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
using System.Linq;

namespace BehaviorUX
{
  public class EnumPropertyField : PropertyField
  {
    [SerializeField] EnumField enumField;

    public delegate void OnValueChanged(string value);
    public event OnValueChanged onValueChanged;

    bool expandedLastFrame = false;

    protected override void Initialize()
    {
      enumField.SetAllowedValues(editor.allowedValues);
      enumField.SetLabel(editor.labelForDisplay);
      enumField.SetListener(UpdateValue);
    }

    void Update()
    {
      enumField.SetValue((string)editor.data);
    }

    void UpdateValue(string value)
    {
      editor.SetData(value);
      onValueChanged?.Invoke(value);
    }

  }

}
