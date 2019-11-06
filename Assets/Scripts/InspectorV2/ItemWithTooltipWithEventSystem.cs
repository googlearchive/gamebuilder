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
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemWithTooltipWithEventSystem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] string description;
  [SerializeField] bool controlledByTooltipOption = false;

  UserMain userMain;

  bool tooltipShowing = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref userMain);
  }

  // Allow setting userMain manually since FindIfNotSet is costly in bulk.
  public void SetupWithUserMain(UserMain userMain)
  {
    this.userMain = userMain;
  }

  public void SetDescription(string description)
  {
    this.description = description;
    if (tooltipShowing)
    {
      userMain.SetMouseoverTooltipText(description);
    }
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (controlledByTooltipOption)
    {
      if (userMain.playerOptions.showTooltips)
      {
        ShowTooltip();
      }
    }
    else
    {
      ShowTooltip();
    }
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    HideTooltip();
  }

  void ShowTooltip()
  {
    userMain.SetMouseoverTooltipText(description);
    tooltipShowing = true;
  }

  void HideTooltip()
  {
    userMain.SetMouseoverTooltipText("");
    tooltipShowing = false;
  }

  void OnDisable()
  {
    if (tooltipShowing)
    {
      HideTooltip();
    }

  }


}
