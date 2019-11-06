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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSettings : MonoBehaviour
{
  // [SerializeField] UnityEngine.UI.Slider mapSizeSliderX;
  // [SerializeField] UnityEngine.UI.Slider mapSizeSliderZ;
  [SerializeField] SkyOptions skyOptions;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] UnityEngine.UI.Button mapsizeButton;
  [SerializeField] UnityEngine.UI.Toggle dayLightToggle;
  [SerializeField] UnityEngine.UI.Toggle nightLightToggle;
  [SerializeField] UnityEngine.UI.Toggle darkLightToggle;

  GameBuilderStage gbStage;
  NetworkingController networking;

  // const float MIN_GROUND_SIZE = 50;
  // const float MAX_GROUND_SIZE = 5000;
  // const float GROUND_SIZE_MULT = 5;
  // const float CELL_SIZE = 2.5f;
  // const float MAX_SQ_AREA = 250000;
  [SerializeField] MapSize[] mapSizes;
  [SerializeField] UnityEngine.UI.Toggle[] mapSizeToggles;
  [SerializeField] GameObject mapSizeDisplayObject;
  [SerializeField] UnityEngine.UI.Button mapSizeCancel;
  [SerializeField] UnityEngine.UI.Button mapSizeClose;
  [SerializeField] UnityEngine.UI.Button mapSizeConfirm;

  [Serializable]
  struct MapSize
  {
    public float x;
    public float z;
  }

  public void Setup()
  {
    Util.FindIfNotSet(this, ref gbStage);
    Util.FindIfNotSet(this, ref networking);
    closeButton.onClick.AddListener(() => SetIsOpen(false));
    mapsizeButton.onClick.AddListener(() => SetMapSizeOpen(true));
    mapSizeClose.onClick.AddListener(() => SetMapSizeOpen(false));
    mapSizeCancel.onClick.AddListener(() => SetMapSizeOpen(false));
    mapSizeConfirm.onClick.AddListener(UpdateMapSize);

    UpdateSceneLightingToggles();

    nightLightToggle.onValueChanged.AddListener(on => { if (on) gbStage.SetSceneLightingMode(GameBuilderStage.SceneLightingMode.Night); });
    dayLightToggle.onValueChanged.AddListener(on => { if (on) gbStage.SetSceneLightingMode(GameBuilderStage.SceneLightingMode.Day); });
    darkLightToggle.onValueChanged.AddListener(on => { if (on) gbStage.SetSceneLightingMode(GameBuilderStage.SceneLightingMode.Dark); });

    skyOptions.Setup();
  }

  private void UpdateSceneLightingToggles()
  {
    nightLightToggle.isOn = gbStage.GetSceneLightingMode() == GameBuilderStage.SceneLightingMode.Night;
    dayLightToggle.isOn = gbStage.GetSceneLightingMode() == GameBuilderStage.SceneLightingMode.Day;
    darkLightToggle.isOn = gbStage.GetSceneLightingMode() == GameBuilderStage.SceneLightingMode.Dark;
  }

  private void UpdateMapSize()
  {
    MapSize ms = mapSizes[0];
    for (int i = 0; i < mapSizeToggles.Length; i++)
    {
      if (mapSizeToggles[i].isOn)
      {
        ms = mapSizes[i];
        break;
      }
    }
    gbStage.SetGroundSizeX(ms.x);
    gbStage.SetGroundSizeZ(ms.z);
    networking.TriggerTerrainReset();
    SetMapSizeOpen(false);
  }

  void SetMapSizeOpen(bool value)
  {
    mapSizeDisplayObject.SetActive(value);
  }

  void Update()
  {
    UpdateSceneLightingToggles();
  }

  public void SetIsOpen(bool open)
  {
    gameObject.SetActive(open);
    if (!open)
    {
      mapSizeDisplayObject.SetActive(false);
    }
  }

  internal bool GetIsOpen()
  {
    return gameObject.activeSelf;
  }
}
