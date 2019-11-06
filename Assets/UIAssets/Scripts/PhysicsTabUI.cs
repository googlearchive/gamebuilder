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

public class PhysicsTabUI : MonoBehaviour
{
  public UnityEngine.UI.Toggle ghostPresetToggle;
  public UnityEngine.UI.Toggle solidPresetToggle;
  public UnityEngine.UI.Toggle objectPresetToggle;
  public UnityEngine.UI.Toggle characterPresetToggle;

  public TMPro.TextMeshProUGUI presetDescription;

  public UnityEngine.UI.Toggle solidToggle;
  public UnityEngine.UI.Toggle pushableToggle;
  public UnityEngine.UI.Toggle gravityToggle;
  public UnityEngine.UI.Toggle staysUprightToggle;
  public UnityEngine.UI.Toggle concaveToggle;

  public GameObject advancedSection;
  public GameObject slidersSection;

  public UnityEngine.UI.Toggle rotationLockToggle;
  public UnityEngine.UI.Toggle positionXLockToggle;
  public UnityEngine.UI.Toggle positionYLockToggle;
  public UnityEngine.UI.Toggle positionZLockToggle;

  public UnityEngine.UI.Slider massSlider;
  public TMPro.TMP_InputField massTextInput;
  public UnityEngine.UI.Slider massDragSlider;
  public TMPro.TMP_InputField massDragTextInput;
  public UnityEngine.UI.Slider bounceSlider;
  public TMPro.TMP_InputField bounceTextInput;
  public UnityEngine.UI.Slider angularDragSlider;
  public TMPro.TMP_InputField angularDragTextInput;
}
