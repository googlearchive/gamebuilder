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

public class EditToolbar : Toolbar
{
  [SerializeField] Color[] primaryToolColors;
  [SerializeField] Color[] secondaryToolColors;
  [SerializeField] CanvasGroup canvasGroup;
  /*   [SerializeField] ToolbarItem optionsItem;

    [SerializeField] Color primaryOptionColor;
    [SerializeField] Color secondaryOptionColor;

    public System.Action OnOptionsClick; */

  const float ANIMATION_TIME_OFFSET = .03f;

  Vector2 onPosition = new Vector2(0, 0);
  Vector2 offPosition = new Vector2(0, 0);


  /*  public void SetOptionsSelect(bool on)
   {
     optionsItem.SetSelect(on);
   } */

  public void Setup()
  {

    for (int i = 0; i < toolbarItems.Length; i++)
    {
      int index = i;
      toolbarItems[i].SetColors(primaryToolColors[i], secondaryToolColors[i]);
      toolbarItems[i].SetSelect(false);
      toolbarItems[i].GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnMenuItemClick?.Invoke(index));
    }

    /*    optionsItem.SetColors(primaryOptionColor, secondaryOptionColor);
       optionsItem.SetSelect(false);
       optionsItem.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnOptionsClick?.Invoke());
    */
  }

  public void Open()
  {
    for (int i = 0; i < toolbarItems.Length; i++)
    {
      toolbarItems[i].Open(i * ANIMATION_TIME_OFFSET);
    }
    canvasGroup.interactable = true;
    // optionsItem.Open(TOOL_COUNT * ANIMATION_TIME_OFFSET);
  }

  public void Close()
  {
    canvasGroup.interactable = false;
    for (int i = 0; i < toolbarItems.Length; i++)
    {
      toolbarItems[i].Close(i * ANIMATION_TIME_OFFSET);
    }
    // optionsItem.Close(TOOL_COUNT * ANIMATION_TIME_OFFSET);
  }
}
