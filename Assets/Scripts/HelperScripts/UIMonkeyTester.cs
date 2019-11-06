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

public class UIMonkeyTester : MonoBehaviour
{

  private UndoStack undoStack;
  private System.Random random = new System.Random();

  public void Awake()
  {
    Util.FindIfNotSet(this, ref undoStack);
  }

  private enum UIType
  {
    UNDO,
    BUTTON,
    SLIDER,
    CHECKBOX,
    DROPDOWN
  }

  // Start a monkey test by manipulating random UI components / randomly undoing.
  public IEnumerator MonkeyTestUI(int iterations)
  {
    UndoStack undoStack = UnityEngine.Object.FindObjectOfType<UndoStack>();
    System.Random random = new System.Random();
    List<UIType> uiTypes = new List<UIType>();
    foreach (UIType uiType in (UIType[])System.Enum.GetValues(typeof(UIType)))
    {
      uiTypes.Add(uiType);
    }

    for (int i = 0; i < iterations; i++)
    {
      // Shuffle the ui types to try, then go down the list until we find an available one.
      Util.ShuffleList(uiTypes, random);
      for (int j = 0; j < uiTypes.Count; j++)
      {
        bool processed = false;
        switch (uiTypes[j])
        {
          case UIType.UNDO:
            if (!undoStack.IsEmpty())
            {
              processed = true;
              undoStack.TriggerUndo();
            }
            break;
          case UIType.BUTTON:
            UnityEngine.UI.Button[] buttons = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
            if (buttons.Length > 0)
            {
              processed = true;
              UnityEngine.UI.Button button = buttons[random.Next(0, buttons.Length)];
              Debug.Log("Button " + button.gameObject.name, button);
              button.onClick?.Invoke();
            }
            break;
          case UIType.CHECKBOX:
            UnityEngine.UI.Toggle[] checkboxes = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Toggle>();
            if (checkboxes.Length > 0)
            {
              processed = true;
              UnityEngine.UI.Toggle checkbox = checkboxes[random.Next(0, checkboxes.Length)];
              Debug.Log("Checkbox " + checkbox.gameObject.name, checkbox);
              checkbox.onValueChanged?.Invoke(random.Next(0, 2) > 0);
            }
            break;
          case UIType.SLIDER:
            UnityEngine.UI.Slider[] sliders = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Slider>();
            if (sliders.Length > 0)
            {
              processed = true;
              UnityEngine.UI.Slider slider = sliders[random.Next(0, sliders.Length)];
              Debug.Log("Slider " + slider.gameObject.name, slider);
              slider.onValueChanged?.Invoke((float)random.NextDouble() * (slider.maxValue - slider.minValue) + slider.minValue);
            }
            break;
          case UIType.DROPDOWN:
            TMPro.TMP_Dropdown[] dropdowns = UnityEngine.Object.FindObjectsOfType<TMPro.TMP_Dropdown>();
            if (dropdowns.Length > 0)
            {
              processed = true;
              TMPro.TMP_Dropdown dropdown = dropdowns[random.Next(0, dropdowns.Length)];
              Debug.Log("Dropdown " + dropdown.gameObject.name, dropdown);
              dropdown.onValueChanged?.Invoke(random.Next(0, dropdown.options.Count));
            }
            break;
        }
        if (processed) break;
      }

      yield return new WaitForSeconds(0.5f);
    }
  }
}
