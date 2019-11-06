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

public class ColorGradientFieldUI : GradientFieldUI<Color>
{

  [SerializeField] UnityEngine.UI.RawImage gradientImage;
  [SerializeField] ColorEditFormUI colorEditForm;

  protected override void UpdateStops()
  {
    RedrawTexture();
  }

  protected override GradientFieldUI<Color>.EditForm GetEditForm()
  {
    return colorEditForm;
  }

  private void RedrawTexture()
  {
    // Create new texture.
    int width = Mathf.CeilToInt(gradientTransform.rect.width);
    int height = 1;
    Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

    // Generate Gradient from stops.
    IModel model = GetModel();
    Gradient gradient = new Gradient();
    GradientColorKey[] colorKeys = new GradientColorKey[model.GetCount()];
    GradientAlphaKey[] alphaKeys = new GradientAlphaKey[model.GetCount()];
    for (int i = 0; i < model.GetCount(); i++)
    {
      colorKeys[i] = new GradientColorKey(model.GetValue(i), model.GetPosition(i));
      alphaKeys[i] = new GradientAlphaKey(model.GetValue(i).a, model.GetPosition(i));
    }
    gradient.SetKeys(colorKeys, alphaKeys);

    // Apply gradient to texture.
    for (int x = 0; x < width; x++)
    {
      float time = (x * 1.0f) / width;
      Color color = gradient.Evaluate(time);
      for (int y = 0; y < height; y++)
      {
        texture.SetPixel(x, y, color);
      }
    }
    texture.Apply();
    gradientImage.texture = texture;
  }

  protected override bool CanAddStop()
  {
    return GetNumStops() <= 7;
  }

}
