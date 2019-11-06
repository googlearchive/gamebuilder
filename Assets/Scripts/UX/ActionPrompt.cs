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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionPrompt : MonoBehaviour
{
  [SerializeField] GameObject[] actionTooltipObjects;
  [SerializeField] TMPro.TextMeshProUGUI[] actionTooltipTextfields;

  enum ActionIndex
  {
    None = -1,
    Action1 = 0,
    Action2 = 1
  }

  void SetActionTooltip(ActionIndex actionIndex, string content)
  {
    if (actionTooltipObjects[(int)actionIndex].activeSelf != (content != ""))
    {
      actionTooltipObjects[(int)actionIndex].SetActive(content != "");
    }
    actionTooltipTextfields[(int)actionIndex].text = content;
  }

  public void UpdatePrompts(VoosEngine.PlayerToolTip[] newtips)
  {
    if (newtips.Length == 0)
    {
      SetActionTooltip(ActionIndex.Action1, "");
      SetActionTooltip(ActionIndex.Action2, "");
    }
    else
    {
      bool action1Active = false;
      bool action2Active = false;
      for (int i = 0; i < newtips.Length; i++)
      {
        if (newtips[i].keyCode == "action2")
        {
          action2Active = true;
          SetActionTooltip(ActionIndex.Action2, newtips[i].text);
        }
        else if (newtips[i].keyCode == "action1")
        {
          action1Active = true;
          SetActionTooltip(ActionIndex.Action1, newtips[i].text);
        }
      }

      if (!action1Active) SetActionTooltip(ActionIndex.Action1, "");
      if (!action2Active) SetActionTooltip(ActionIndex.Action2, "");
    }
  }
}
