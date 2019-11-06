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

public class HudTransitionControl : MonoBehaviour
{
  [SerializeField] HeaderMenu headerMenu;
  [SerializeField] EditToolbar editToolbar;
  [SerializeField] CanvasGroup playTooltip;
  [SerializeField] CanvasGroup playHeader;

  float LERP_SPEED = 4;

  public void SetEditMode()
  {
    StopAllCoroutines();
    StartCoroutine(ToEditRoutine());
  }

  public void SetPlayMode()
  {
    StopAllCoroutines();
    StartCoroutine(ToPlayRoutine());
  }

  IEnumerator ToPlayRoutine()
  {
    editToolbar.Close();
    float lerpVal = 0;
    headerMenu.SetEditCanvasInteractable(false);
    playHeader.interactable = true;
    playHeader.blocksRaycasts = true;
    if (!GameBuilderApplication.IsStandaloneExport)
    {
      headerMenu.SetPlayBackground(true);
    }

    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * LERP_SPEED);
      headerMenu.SetEditCanvasAlpha(1 - lerpVal);
      playTooltip.alpha = lerpVal;
      playHeader.alpha = lerpVal;
      yield return null;
    }

    headerMenu.SetEditCanvasAlpha(0);
    playTooltip.alpha = 1;
    playHeader.alpha = 1;
  }

  IEnumerator ToEditRoutine()
  {
    editToolbar.Open();
    float lerpVal = 0;
    headerMenu.SetEditCanvasInteractable(true);
    playHeader.interactable = false;
    playHeader.blocksRaycasts = false;
    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * LERP_SPEED);
      headerMenu.SetEditCanvasAlpha(lerpVal);

      playTooltip.alpha = 1 - lerpVal;
      playHeader.alpha = 1 - lerpVal;
      yield return null;
    }

    headerMenu.SetPlayBackground(false);
    headerMenu.SetEditCanvasAlpha(1);
    playTooltip.alpha = 0;
    playHeader.alpha = 0;
  }

}
