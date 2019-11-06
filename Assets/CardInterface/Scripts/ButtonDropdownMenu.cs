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
using System.Collections;
using System.Collections.Generic;

public class ButtonDropdownMenu : MonoBehaviour
{

  [SerializeField] RectTransform mouseoverRect;
  [SerializeField] RectTransform dropdownRect;
  [SerializeField] UnityEngine.UI.Button optionPrefab;
  [SerializeField] List<string> options;

  public event System.Action<string> onOptionClicked;
  private List<UnityEngine.UI.Button> optionButtons = new List<UnityEngine.UI.Button>();

  void Awake()
  {
    SetOptions(options);
  }

  public void SetOptions(List<string> options)
  {
    this.options = options;

    foreach (UnityEngine.UI.Button button in optionButtons)
    {
      GameObject.Destroy(button.gameObject);
    }
    optionButtons.Clear();

    foreach (string option in options)
    {
      UnityEngine.UI.Button optionButton = Instantiate(optionPrefab, dropdownRect.transform);
      optionButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = option;
      optionButton.onClick.AddListener(() =>
      {
        dropdownRect.gameObject.SetActive(false);
        onOptionClicked?.Invoke(option);
      });
      optionButton.gameObject.SetActive(true);
      optionButtons.Add(optionButton);
    }
  }

  void Update()
  {
    dropdownRect.gameObject.SetActive(
      RectTransformUtility.RectangleContainsScreenPoint(mouseoverRect, Input.mousePosition) ||
      (dropdownRect.gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(dropdownRect, Input.mousePosition)));
  }

}