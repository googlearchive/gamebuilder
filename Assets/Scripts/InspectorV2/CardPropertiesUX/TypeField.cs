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

using UnityEngine;

public abstract class TypeField<T> : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI label;

  public delegate void OnValueChanged(T result);
  protected OnValueChanged onValueChanged;

  public string GetLabel()
  {
    return label.text;
  }

  public void SetLabel(string text)
  {
    label.text = text;
  }

  public void SetListener(OnValueChanged listener)
  {
    this.onValueChanged = listener;
  }

  public abstract void SetValue(T value);

}