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

public class DamageScreenEffect : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Image effectImage;
  [SerializeField] float flashUpSpeed = 15;
  [SerializeField] float flashDownSpeed = 2;

  public void TriggerEffect()
  {
    if (effectRoutine != null) StopCoroutine(effectRoutine);
    effectRoutine = StartCoroutine(EffectRoutine());
  }

  Coroutine effectRoutine;
  IEnumerator EffectRoutine()
  {
    effectImage.color = Color.clear;
    effectImage.enabled = true;
    float progress = 0;

    while (progress < 1)
    {
      progress = Mathf.Clamp01(progress + Time.deltaTime * flashUpSpeed);
      effectImage.color = Color.Lerp(Color.clear, Color.white, progress);
      yield return null;
    }


    while (progress > 0)
    {
      progress = Mathf.Clamp01(progress - Time.deltaTime * flashDownSpeed);
      effectImage.color = Color.Lerp(Color.clear, Color.white, progress);
      yield return null;
    }

    effectImage.enabled = false;
  }

}
