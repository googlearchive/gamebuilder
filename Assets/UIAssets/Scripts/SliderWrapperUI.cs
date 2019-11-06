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
using UnityEngine.EventSystems;

// Written for the purposes of separating programmatic value changes from user-triggered ones,
// and also to only trigger value changes when the user has released the mouse
// (as opposed to everytime the slider gets dragged incrementally).
public class SliderWrapperUI : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
  [SerializeField] UnityEngine.UI.Slider slider;

  public event System.Action<float> onValueChanged;

  private float value;
  private bool isSelecting;

  public void SetSlider(UnityEngine.UI.Slider slider)
  {
    this.slider = slider;
  }

  public void SetValue(float value)
  {
    if (!isSelecting)
    {
      this.value = value;
      slider.value = value;
    }
  }

  public void OnPointerDown(PointerEventData data)
  {
    isSelecting = true;
  }

  public void OnPointerUp(PointerEventData data)
  {
    isSelecting = false;
    if (slider.value != value)
    {
      value = slider.value;
      onValueChanged?.Invoke(slider.value);
    }
  }

  void OnDisable()
  {
    isSelecting = false;
    slider.value = value;
  }

}
