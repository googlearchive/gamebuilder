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
using System.Collections.Generic;

public class EditableList<C, A> where A : IEditableListAdapter<C>
{
  private A adapter;
  private EditableListUI ui;

  private List<EditableListItem> listItems = new List<EditableListItem>();
  private Dictionary<EditableListItem, C> contents = new Dictionary<EditableListItem, C>();
  private EditableListItem draggingItem;

  public event System.Action<int> onRequestAddItem;
  public event System.Action<int, int> onRequestMoveItem;
  public event System.Action<int> onRequestDeleteItem;

  public EditableList(A adapter, EditableListUI ui)
  {
    this.ui = ui;
    ui.addToBottomButton.onClick.AddListener(() =>
    {
      onRequestAddItem(listItems.Count);
    });
    ui.onDisable += () =>
    {
      SetDraggingItem(null);
    };
    this.adapter = adapter;
  }

  public void Refresh()
  {
    SetDraggingItem(null);

    for (int i = 0; i < Mathf.Min(adapter.GetCount(), listItems.Count); i++)
    {
      adapter.Populate(i, contents[listItems[i]]);
    }

    for (int i = listItems.Count; i < adapter.GetCount(); i++)
    {
      EditableListItem newItem = CreateListItem();

      C content = adapter.Inflate(newItem.GetContentContainer());
      contents[newItem] = content;
      adapter.Populate(i, content);

      newItem.transform.SetSiblingIndex(i);
      listItems.Insert(i, newItem);
    }

    for (int i = listItems.Count - 1; i >= 0 && i >= adapter.GetCount(); i--)
    {
      EditableListItem item = listItems[i];
      listItems.RemoveAt(i);
      contents.Remove(item);
      item.RequestDestroy();
    }
  }

  private EditableListItem CreateListItem()
  {
    EditableListItem newItem = Object.Instantiate(ui.itemPrefab, ui.listContainer.transform);
    newItem.onRequestDelete += (item) =>
    {
      onRequestDeleteItem?.Invoke(listItems.IndexOf(item));
    };
    newItem.onDrag += (item) =>
    {
      if (draggingItem == null && listItems.Count > 1) SetDraggingItem(item);
    };
    newItem.gameObject.SetActive(true);
    return newItem;
  }

  private void SetDraggingItem(EditableListItem item)
  {
    if (draggingItem == item) return;

    if (draggingItem != null)
    {
      draggingItem.SetDraggingFeedback(false);
      foreach (Transform component in ui.dragItemHint)
      {
        Object.Destroy(component.gameObject);
      }
    }

    draggingItem = item;
    if (draggingItem != null)
    {
      ui.dragItemHint.gameObject.SetActive(true);
      int index = listItems.IndexOf(draggingItem);
      draggingItem.SetDraggingFeedback(true);
      RectTransform content = Object.Instantiate(draggingItem.GetContentContainer(), ui.dragItemHint);
      Object.Destroy(content.GetComponent<EditableListItem>());
    }
    else
    {
      ui.dragItemHint.gameObject.SetActive(false);
      ui.dragTargetHint.gameObject.SetActive(false);
    }
  }

  public void Update()
  {
    if (draggingItem != null)
    {
      int draggingItemIndex = listItems.IndexOf(draggingItem);
      Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(null, Input.mousePosition);

      Vector2 dragItemPosition;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(
        ui.container, screenPosition, null, out dragItemPosition);
      ui.dragItemHint.anchoredPosition = dragItemPosition;

      ui.dragTargetHint.gameObject.SetActive(false);
      Vector2 rectPosition;

      int selectedIndex = -1;
      for (int i = 0; i < listItems.Count; i++)
      {
        EditableListItem item = listItems[i];

        RectTransform container = item.GetContainer();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
          container, screenPosition, null, out rectPosition);

        // Current mouse target is above this item?
        if (rectPosition.y >= 0)
        {
          ui.dragTargetHint.gameObject.SetActive(true);
          ui.dragTargetHint.anchoredPosition = new Vector2(
            ui.dragTargetHint.anchoredPosition.x,
            container.anchoredPosition.y + ui.listContainer.spacing / 2.0f + container.rect.height / 2.0f);
          selectedIndex = (i <= draggingItemIndex ? i : i - 1);
          break;
        }

        // Current mouse target is below this item?
        else if (rectPosition.y < 0 &&
                 (rectPosition.y > -container.rect.height / 2.0f || i == listItems.Count - 1))
        {
          ui.dragTargetHint.gameObject.SetActive(true);
          ui.dragTargetHint.anchoredPosition = new Vector2(
            ui.dragTargetHint.anchoredPosition.x,
            container.anchoredPosition.y - ui.listContainer.spacing / 2.0f - container.rect.height / 2.0f);
          selectedIndex = (i < draggingItemIndex ? i + 1 : i);
          break;
        }
      }

      if (Input.GetMouseButtonUp(0))
      {
        EditableListItem item = draggingItem;
        SetDraggingItem(null);
        if (selectedIndex == -1 || draggingItemIndex == selectedIndex) return;
        onRequestMoveItem?.Invoke(draggingItemIndex, selectedIndex);
      }
    }
  }

}