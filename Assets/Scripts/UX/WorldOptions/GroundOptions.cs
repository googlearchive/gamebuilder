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

public class GroundOptions : MonoBehaviour
{
  [SerializeField] BasicToolbar groundMenu;
  [SerializeField] ColorWheelOld colorWheel;
  [SerializeField] UnityEngine.UI.Image colorGroundImage;

  GameBuilderStage gbStage;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref gbStage);

    groundMenu.Setup();
    groundMenu.OnSelectIndex = OnGroundSelect;

    colorWheel.OnColorChange = OnColorChange;
  }

  void OnGroundSelect(int index)
  {
    GameBuilderStage.GroundType groundtype = (GameBuilderStage.GroundType)index;
    gbStage.SetGroundType(groundtype);
  }

  void OnColorChange(Color newColor)
  {
    gbStage.SetGroundColor(newColor);
    colorGroundImage.color = newColor;
  }

  public void Close()
  {
    colorWheel.ForceRelease();
  }

  void Update()
  {
    GameBuilderStage.GroundType curground = gbStage.GetGroundType();
    groundMenu.QuietSelectIndex((int)curground);
    colorWheel.SetColor(gbStage.GetGroundColor());
  }
}
