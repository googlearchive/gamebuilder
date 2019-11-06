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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Behaviors;
using System.IO;
using BehaviorProperties;

public partial class BehaviorCards
{
  public static string AddDefaultCardPref = "AddDefaultCards";

  class UnassignedPanel : PanelCommon, PanelLibraryItem.IModel
  {
    UnassignedBehavior item;
    BehaviorCards.ManagerImpl manager;

    public UnassignedPanel(UnassignedBehavior item, BehaviorCards.ManagerImpl manager)
    : base(item)
    {
      this.item = item;
      this.manager = manager;
    }

    public CardPanel.IAssignedPanel Assign()
    {
      using (this.manager.GetActorEditor().StartUndo($"Add {this.GetTitle()} panel"))
      {
        AssignedBehavior behavior = manager.GetActorEditor().AddBehavior(item);
        Util.Log($"DO NOT REMOVE - added panel, use ID: {behavior.useId}");

        AssignedPanel panel = new AssignedPanel(behavior, manager);

        // Now setup default cards
        if (PlayerPrefs.GetInt(AddDefaultCardPref, 1) == 1)
        {
          foreach (var slot in panel.GetDecks())
          {
            if (slot.GetNumAssignedCards() == 0)
            {
              foreach (ICardModel defaultCard in slot.GetDefaultCards())
              {
                slot.OnAssignCard(defaultCard);
              }
            }
          }
        }

        return panel;
      }
    }

    public UnassignedBehavior GetItem()
    {
      return item;
    }
  }
}