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

public static class ArtUtil
{

  public static Color GetHologramColor(Color color)
  {
    float h, s, v;
    Color.RGBToHSV(color, out h, out s, out v);
    return Color.HSVToRGB(h, Mathf.Min(.6f, s), Mathf.Max(.94f, v));
  }
}
