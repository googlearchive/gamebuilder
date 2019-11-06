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

public class ProgressBlockStyleUI : MonoBehaviour
{
  [SerializeField] RectTransform progressBarContainer;
  [SerializeField] RectTransform progressBar;
  [SerializeField] UnityEngine.UI.Image image;

  public void SetThumbnail(Texture2D texture)
  {
    image.sprite = Util.TextureToSprite(texture);
  }

  public void SetProgress(float progress)
  {
    progressBar.sizeDelta = new Vector2(
        progressBarContainer.rect.width * progress,
        progressBar.sizeDelta.y);
  }
}
