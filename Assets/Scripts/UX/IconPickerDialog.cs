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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IconPickerDialog : MonoBehaviour
{
  private const string PREFAB_PATH = "IconPicker";

  [SerializeField] Button closeButton;
  [SerializeField] TMPro.TMP_Dropdown categoryDropdown;
  [SerializeField] GameObject iconRowTemplate;

  private IconLoader iconLoader;
  private List<string> iconCategories;

  // Called when icon picker is closed.
  // pickedIconName: the picked icon name (null if canceled).
  public delegate void OnIconPickerResult(string pickedIconName);

  OnIconPickerResult callback;

  public static IconPickerDialog Launch(OnIconPickerResult callback)
  {
    GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(PREFAB_PATH));
    IconPickerDialog dialog = obj.GetComponent<IconPickerDialog>();
    dialog.Setup(callback);
    return dialog;
  }

  private void Setup(OnIconPickerResult callback)
  {
    Util.FindIfNotSet(this, ref iconLoader);
    this.callback = callback;
    closeButton.onClick.AddListener(OnCloseButtonClicked);
    iconRowTemplate.SetActive(false);
    PopulateCategories();
  }

  private void PopulateCategories()
  {
    iconCategories = new List<string>(iconLoader.EnumerateCategories());
    categoryDropdown.AddOptions(iconCategories);
    categoryDropdown.value = 0;
    PopulateGrid();
    categoryDropdown.onValueChanged.AddListener(i => PopulateGrid());
  }

  private void PopulateGrid()
  {
    ClearGrid();
    string categoryName = iconCategories[categoryDropdown.value];
    IconPickerIconRow thisRow = null;
    foreach (string iconName in iconLoader.EnumerateIcons(categoryName))
    {
      if (thisRow == null || thisRow.IsFull())
      {
        // Start a new row.
        thisRow = CreateNewIconRow();
      }
      Image newImage;
      Button newButton;
      thisRow.AddCell(out newImage, out newButton);
      iconLoader.LoadIconSprite(iconName, (name, sprite) =>
      {
        newImage.sprite = sprite;
        newImage.color = Color.white;
      });
      newButton.onClick.AddListener(() =>
      {
        HandleIconClicked(iconName);
      });
    }
  }

  private void HandleIconClicked(string iconName)
  {
    CloseAndReturn(iconName);
  }

  private IconPickerIconRow CreateNewIconRow()
  {
    GameObject newRow = GameObject.Instantiate(iconRowTemplate);
    newRow.SetActive(true);
    newRow.transform.SetParent(iconRowTemplate.transform.parent, worldPositionStays: false);
    newRow.transform.SetAsLastSibling();
    IconPickerIconRow result = newRow.GetComponent<IconPickerIconRow>();
    Debug.Assert(result != null, "Icon picker row template must have IconPickerIconRow script");
    return result;
  }

  private void ClearGrid()
  {
    Transform listParent = iconRowTemplate.transform.parent;
    for (int i = 0; i < listParent.childCount; i++)
    {
      Transform child = listParent.GetChild(i);
      if (child != iconRowTemplate.transform)
      {
        GameObject.Destroy(child.gameObject);
      }
    }
  }

  public void Close()
  {
    CloseAndReturn(null);
  }

  private void OnCloseButtonClicked()
  {
    CloseAndReturn(null);
  }

  private void CloseAndReturn(string result)
  {
    GameObject.Destroy(gameObject);
    if (callback != null)
    {
      callback(result);
    }
  }
}
