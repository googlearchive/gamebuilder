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
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Widget that lets the user edit the speed of the sound.
// This is just a silly number stepper. It was going to be something fancier.
public class SynthSpeedWidget : MonoBehaviour
{
  private const int MIN_SPEED = 1;
  private const int MAX_SPEED = 16;

  public Button minusButton, plusButton;
  public TMPro.TMP_Text speedDisplay;
  public int speed { get; private set; }

  void Awake()
  {
    speed = 8;
    minusButton.onClick.AddListener(() => ModifySpeed(-1));
    plusButton.onClick.AddListener(() => ModifySpeed(1));
    UpdateView();
  }

  void ModifySpeed(int increment)
  {
    SetSpeed(Mathf.Clamp(speed + increment, MIN_SPEED, MAX_SPEED));
  }

  public void SetSpeed(int speed)
  {
    this.speed = speed;
    UpdateView();
  }

  void UpdateView()
  {
    speedDisplay.text = speed.ToString();
  }
}
