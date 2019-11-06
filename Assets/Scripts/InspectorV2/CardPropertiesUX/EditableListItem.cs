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
using UnityEngine.EventSystems;

public class EditableListItem : MonoBehaviour, IDragHandler
{
  [SerializeField] RectTransform container;
  [SerializeField] RectTransform contentContainer;
  [SerializeField] UnityEngine.UI.Button deleteButton;
  [SerializeField] RectTransform disabledOverlay;

  public event System.Action<EditableListItem> onRequestDelete;
  public event System.Action<EditableListItem> onDrag;

  void Awake()
  {
    deleteButton.onClick.AddListener(() =>
    {
      onRequestDelete?.Invoke(this);
    });
  }

  public RectTransform GetContainer()
  {
    return container;
  }

  public RectTransform GetContentContainer()
  {
    return contentContainer;
  }

  public void RequestDestroy()
  {
    Destroy(gameObject);
  }

  public void OnDrag(PointerEventData data)
  {
    onDrag?.Invoke(this);
  }

  public void SetDraggingFeedback(bool visible)
  {
    disabledOverlay.gameObject.SetActive(visible);
  }

}