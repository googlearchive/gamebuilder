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

public class AddCardEffect : MonoBehaviour
{
  [SerializeField] UnityEngine.UI.Image image;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] float speed;
  [SerializeField] float targetSize = 100;
  [SerializeField] Color color;

  Vector2 baseSize = new Vector2(400, 560);
  float percent = 0;

  void Update()
  {
    percent = Mathf.Clamp01(percent + Time.unscaledDeltaTime * speed);
    image.color = Color.Lerp(color, Color.clear, percent);
    rectTransform.sizeDelta = baseSize + Vector2.one * targetSize * percent;
    if (percent == 1)
    {
      Destroy(gameObject);
    }
  }

}
