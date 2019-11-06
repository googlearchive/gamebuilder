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

public class SliderInput : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Slider slider;
  [SerializeField] TMPro.TMP_InputField input;
  public delegate void OnValueChanged(float value);
  private OnValueChanged onValueChanged = (v) => { };

  void Awake()
  {
    // TODO: better if we only listen to user-triggered events on slider
    slider.onValueChanged.AddListener((v) =>
    {
      input.text = v.ToString();
      onValueChanged(v);
    });
    input.onEndEdit.AddListener((i) =>
    {
      float value = slider.minValue;
      float.TryParse(i, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
      value = Mathf.Max(Mathf.Min(value, slider.maxValue), slider.minValue);
      input.text = value.ToString();
      slider.value = value;
      onValueChanged(value);
    });
  }

  public void AddValueChangedListener(OnValueChanged listener)
  {
    onValueChanged += listener;
  }

  public void SetValue(float value)
  {
    slider.value = value;
    input.text = value.ToString();
  }

  public void OnSliderEndDrag()
  {
    input.text = slider.value.ToString();
    onValueChanged(slider.value);
  }
}
