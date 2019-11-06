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

public class ColorEditFormUI : GradientFieldUI<Color>.EditForm
{

  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] ColorFieldUI colorWheel;
  private System.Action<Color> callback;

  void Awake()
  {
    colorWheel.OnColorChange += (color) =>
    {
      callback?.Invoke(color);
    };
    closeButton.onClick.AddListener(() =>
    {
      RequestClose();
    });
  }

  public override void Populate(Color value, System.Action<Color> callback)
  {
    this.callback = callback;
    colorWheel.SetColor(value);
  }
}
