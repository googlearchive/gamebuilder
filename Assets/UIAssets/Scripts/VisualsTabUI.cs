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

public class VisualsTabUI : MonoBehaviour
{
  public UnityEngine.UI.Button changeAssetButton;

  public ColorFieldUI colorField;

  public TMPro.TextMeshProUGUI particleEffectName;
  public UnityEngine.UI.Button changeParticleEffect;

  public GameObject[] lightToggleObjects;
  public UnityEngine.UI.Toggle emitLightToggle;
  public UnityEngine.UI.Slider lightIntensitySlider;
  public UnityEngine.UI.Slider lightDistanceSlider;
  public UnityEngine.UI.Toggle independentColorToggle;
  public ColorFieldUI lightColorField;

  public TMPro.TextMeshProUGUI soundEffectName;
  public UnityEngine.UI.Button changeSoundEffect;
  public UnityEngine.UI.Button soundLoop;
  public UnityEngine.UI.Slider soundLoopVolumeSlider;

  public GameObject particlePicker;
  public GameObject particlePickerList;
  public ScrollingListItemUI particlePickerItemTemplate;

  public GameObject soundPicker;
  public GameObject soundPickerList;
  public ScrollingListItemUI soundPickerItemTemplate;

}
