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
  class AssignedPanel : PanelCommon, CardPanel.IAssignedPanel
  {
    readonly AssignedBehavior behavior;
    readonly BehaviorCards.ManagerImpl manager;

    public AssignedPanel(AssignedBehavior behavior, BehaviorCards.ManagerImpl manager)
    : base(behavior.GetUnassigned())
    {
      this.behavior = behavior;
      this.manager = manager;

      AssertInvariants();
    }

    public AssignedBehavior GetBehavior() { return behavior; }

    IEnumerable<PropEditor> GetDeckProps()
    {
      return behavior.GetProperties().Where(f => f.propType == PropType.CardDeck);
    }

    public bool IsValid()
    {
      return behavior.IsValid();
    }

    public void AssertInvariants()
    {
      if (!IsValid()) { return; }

      var brain = behavior.assignedBrain.GetBrain();

      // Every deck slot should have a valid use ID.
      foreach (PropEditor fieldEditor in GetDeckProps())
      {
        foreach (var useId in (string[])fieldEditor.data)
        {
          Debug.Assert(brain.HasUse(useId));
        }
      }
    }

    public CardPanel.IAssignedPanel Duplicate()
    {
      AssertInvariants();
      return this.manager.DuplicatePanel(this);
    }

    public void Remove()
    {
      AssertInvariants();

      using (this.manager.GetActorEditor().StartUndo($"Remove {this.GetTitle()} panel"))
      {
        // Remove all cards in all our decks
        HashSet<string> cardUseIds = new HashSet<string>();
        foreach (PropEditor deck in GetDeckProps())
        {
          cardUseIds.AddRange((string[])deck.data);
        }

        ActorBehaviorsEditor brain = behavior.assignedBrain;
        List<AssignedBehavior> toRemove = new List<AssignedBehavior>();
        toRemove.AddRange(brain.GetAssignedBehaviors().Where(use => cardUseIds.Contains(use.useId)));
        foreach (var cardAssign in toRemove)
        {
          cardAssign.RemoveSelfFromActor();
          Debug.Assert(!cardAssign.IsValid());
        }

        // Remove self
        this.behavior.RemoveSelfFromActor();
      }
      Debug.Assert(!behavior.IsValid());
    }

    // TODO rename to GetDecks
    public IEnumerable<IDeckModel> GetDecks()
    {
      foreach (PropEditor fieldEditor in GetDeckProps())
      {
        var slot = new Deck(fieldEditor, behavior.assignedBrain, this.manager);
        yield return slot;
      }
    }

    public IDeckModel GetSlotByName(string variableName)
    {
      foreach (PropEditor fieldEditor in GetDeckProps())
      {
        if (fieldEditor.propDef.variableName == variableName)
        {
          var slot = new Deck(fieldEditor, behavior.assignedBrain, this.manager);
          return slot;
        }
      }
      return null;
    }

    public string GetId()
    {
      return behavior.useId;
    }

    public bool IsFake()
    {
      return false;
    }

    public CardPanel.PanelUse GetUse()
    {
      return Util.FromJsonSafe<CardPanel.PanelUse>(behavior.GetUseMetaJson());
    }

    public void SetUse(CardPanel.PanelUse data, string undoLabel)
    {
      AssertInvariants();
      using (undoLabel != null ?
        this.manager.GetActorEditor().StartUndo(undoLabel) :
        new Util.DummyDisposable())
      {
        // Util.Log($"setting use data for {GetTitle()}: {data.position}");
        string newJson = JsonUtility.ToJson(data);
        behavior.SetUseMetaJson(newJson);
      }
      AssertInvariants();
    }
  }
}