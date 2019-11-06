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

using UnityEngine;

public class DetailContainer : MonoBehaviour
{
  [SerializeField] RectTransform highlightArrowTransform;
  [SerializeField] RectTransform contentContainer;

  public void Open()
  {
    gameObject.SetActive(true);
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  public void UpdateArrowPosition(float x)
  {
    Vector2 pos = highlightArrowTransform.anchoredPosition;
    pos.x = x;
    highlightArrowTransform.anchoredPosition = pos;
  }

  public RectTransform GetContentContainer()
  {
    return contentContainer;
  }

}