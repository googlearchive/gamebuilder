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
using UnityEngine.EventSystems;

public class SquareImageButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
  [SerializeField] GameObject outline;
  [SerializeField] UnityEngine.UI.RawImage image;
  [SerializeField] GameObject pointerEnterFeedback;
  [SerializeField] GameObject workshopMarker;
  public System.Action onPointerDown;

  private ActorableSearchResult searchResult;

  public ActorableSearchResult GetSearchResult()
  {
    return searchResult;
  }

  public void SetSearchResult(ActorableSearchResult searchResult)
  {
    this.searchResult = searchResult;
    workshopMarker.SetActive(
      searchResult.actorPrefab != null && !searchResult.actorPrefab.GetWorkshopId().IsNullOrEmpty());
  }

  public void SetImage(Texture2D texture)
  {
    image.texture = texture;
  }

  public void SetSelected(bool selected)
  {
    outline.SetActive(selected);
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    onPointerDown?.Invoke();
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    pointerEnterFeedback.SetActive(true);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    pointerEnterFeedback.SetActive(false);
  }

  public void RequestDestroy()
  {
    Destroy(gameObject);
  }

}
