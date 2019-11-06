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

public class GameVisualsTabController : AbstractTabController
{


  [SerializeField] UnityEngine.UI.Slider mapSizeSlider;
  [SerializeField] SkyOptions skyOptions;
  [SerializeField] GroundOptions groundOptions;

  [SerializeField] UnityEngine.UI.Toggle[] cameraRadioButtons;
  [SerializeField] UnityEngine.UI.Button rotateButton;
  [SerializeField] GameObject isometricSettings;
  [SerializeField] UnityEngine.UI.Slider zoomSlider;

  GameBuilderStage gbStage;
  UserMain userMain;


  const float MIN_GROUND_EXP = .74f;
  const float MAX_GROUND_EXP = 2;
  const float GROUND_SIZE_MULT = 5;
  const float CELL_SIZE = 2.5f;

  public override void Open(VoosActor actor, Dictionary<string, object> props)
  {
  }

  public override void Setup()
  {
    Util.FindIfNotSet(this, ref gbStage);

    mapSizeSlider.onValueChanged.AddListener(SetNormalizedMapSize);

    skyOptions.Setup();
    groundOptions.Setup();

    Util.FindIfNotSet(this, ref userMain);
    for (int i = 0; i < cameraRadioButtons.Length; i++)
    {
      int n = i;
      cameraRadioButtons[i].onValueChanged.AddListener((on) => { if (on) SetCameraOn(n); });
    }

    rotateButton.onClick.AddListener(() => userMain.RotateCameraView());

    zoomSlider.onValueChanged.AddListener((f) => userMain.SetCameraViewZoom(f));
  }

  void Update()
  {
    mapSizeSlider.value = GetNormalizedMapSize();

    CameraView camView = userMain.GetCameraView();
    int index = (int)camView;
    for (int i = 0; i < cameraRadioButtons.Length; i++)
    {
      cameraRadioButtons[i].isOn = (i == index);
    }
    zoomSlider.value = userMain.GetCameraViewZoom();
    isometricSettings.SetActive(camView == CameraView.Isometric);

    base.Update();
  }

  float GetNormalizedMapSize()
  {
    float rawvalue = gbStage.GetGroundSizeX() / GROUND_SIZE_MULT;
    float lerpval = Mathf.Log(rawvalue, 10);
    return Mathf.InverseLerp(MIN_GROUND_EXP, MAX_GROUND_EXP, lerpval);
  }

  void SetNormalizedMapSize(float value)
  {
    throw new System.NotImplementedException("Isn't this menu unused?");
    float raw = Mathf.Pow(10, Mathf.Lerp(MIN_GROUND_EXP, MAX_GROUND_EXP, value)) * GROUND_SIZE_MULT;
    float cellRound = Mathf.Round(raw / CELL_SIZE);
    gbStage.SetGroundSizeX(cellRound * CELL_SIZE);
  }

  void SetCameraOn(int index)
  {
    if (!userMain.SetCameraView((CameraView)index, byUserRequest: true))
    {
      return;
    }
  }

  /* public override void RequestClose()
  {
    skyOptions.Close();
    groundOptions.Close();
    base.RequestClose();
  } 
 */
}
