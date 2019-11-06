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

public class HudManager : MonoBehaviour
{
  [SerializeField] RectTransform rectTransform;
  [SerializeField] RectTransform mainRect;
  [SerializeField] RectTransform canvasRect;
  [SerializeField] UserMain userMain;

  [SerializeField] RectTransform[] scalingRects;

  float currentHorizontalLeftOffset = 0;
  float horizontalLeftPercentOffset = 0;

  float currentHorizontalRightOffset = 0;
  float horizontalRightPercentOffset = 0;

  float verticalPercentOffset = 0;

  public void UpdateHorizontalLeftOffset(float newOffset)
  {
    if (currentHorizontalLeftOffset == newOffset)
    {
      return;
    }

    currentHorizontalLeftOffset = newOffset;
    horizontalLeftPercentOffset = newOffset / mainRect.rect.width;

    rectTransform.anchorMin = new Vector2(horizontalLeftPercentOffset, 0);
    UpdateCameraRect();
    UpdateScalingRects();
  }

  public void UpdateHorizontalRightOffset(float newOffset)
  {
    if (currentHorizontalRightOffset == newOffset)
    {
      return;
    }

    currentHorizontalRightOffset = newOffset;
    horizontalRightPercentOffset = newOffset / mainRect.rect.width;

    rectTransform.anchorMax = new Vector2(1 - horizontalRightPercentOffset, 1);
    UpdateCameraRect();
    UpdateScalingRects();
  }

  void UpdateCameraRect()
  {
    userMain.GetCamera().rect = new Rect(horizontalLeftPercentOffset, 0, 1 - horizontalLeftPercentOffset - horizontalRightPercentOffset, 1 - verticalPercentOffset);

  }

  void UpdateScalingRects()
  {
    // Vector3 elementScale = Vector3.one * (1 - horizontalLeftPercentOffset - horizontalRightPercentOffset);
    float scale = 1 - horizontalLeftPercentOffset - horizontalRightPercentOffset;
    float screenWidth = mainRect.rect.width * scale;
    foreach (RectTransform rt in scalingRects)
    {
      float delta = screenWidth - rt.rect.width;
      if (delta >= 0)
      {
        rt.localScale = Vector3.one;
      }
      else
      {
        rt.localScale = Vector3.one * (screenWidth / rt.rect.width);
      }

      // rt.localScale = elementScale;


    }
  }

  public void UpdateVerticalOffsetAsPercent(float newOffset)
  {
    if (verticalPercentOffset == newOffset)
    {
      return;
    }

    verticalPercentOffset = newOffset;

    mainRect.anchorMax = new Vector2(1, 1 - verticalPercentOffset);
    UpdateCameraRect();
  }

  public float GetVerticalPercent()
  {
    return mainRect.anchorMax.y;
  }
}
