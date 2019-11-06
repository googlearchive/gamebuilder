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

public class CardTrash : MonoBehaviour
{
  [SerializeField] RectTransform rectTransform;
  [SerializeField] UnityEngine.UI.Image image;
  [SerializeField] Color baseColor;
  [SerializeField] Color activeColor;

  public bool IsMouseOver()
  {
    return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition);
  }

  public void SetCardOver(bool on)
  {
    image.color = on ? activeColor : baseColor;
  }

  public void Show()
  {
    SetCardOver(false);
    StopAllCoroutines();
    gameObject.SetActive(true);
  }

  public void Hide()
  {
    gameObject.SetActive(false);
  }

  public void FadeOut()
  {
    StartCoroutine(FadeOutRoutine());
  }

  IEnumerator FadeOutRoutine()
  {
    yield return new WaitForSeconds(0.4f);
    Hide();
  }

}
