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

public partial class BehaviorCards : MonoBehaviour
{
  class ManagerImpl : ICardManagerModel
  {
    readonly ActorBehaviorsEditor actorEditor;

    public ManagerImpl(ActorBehaviorsEditor actorEditor)
    {
      this.actorEditor = actorEditor;
    }

    // NOTE: This is not used anymore, but keep it here for the sorting code,
    // which we should soon port to other code paths.
    public IEnumerable<ICardModel> GetCards(string category = null)
    {
      var bs = actorEditor.GetBehaviorSystem();

      List<UnassignedCard> entries = new List<UnassignedCard>();
      foreach (string uri in bs.LoadBehaviorLibrary())
      {
        Behavior data = bs.GetBehaviorData(uri);
        if (!CardMetadata.GetMetaDataFor(data).isCard) continue;
        if (category == null || CardMetadata.IsCardOfCategory(data, category))
        {
          // TEMP HACK: don't show builtin customs in card view - they're just
          // there for legacy support.
          if (BehaviorSystem.IsBuiltinBehaviorUri(uri) && category == CUSTOM_CATEGORY)
          {
            continue;
          }
          entries.Add(new UnassignedCard(new UnassignedBehavior(uri, bs)));
        }
      }

      // Sort by priority first, then by title.
      entries.Sort((UnassignedCard a, UnassignedCard b) =>
      {
        var mdA = a.GetMetadata();
        var mdB = b.GetMetadata();
        int prioCompare = -mdA.listPriority.CompareTo(mdB.listPriority);
        return prioCompare != 0 ? prioCompare : mdA.title.CompareTo(mdB.title);
      });

      return entries;
    }

    public IEnumerable<PanelLibraryItem.IModel> GetPanelLibrary()
    {
      var bs = actorEditor.GetBehaviorSystem();
      foreach (string uri in bs.LoadBehaviorLibrary())
      {
        var data = bs.GetBehaviorData(uri);
        if (PanelMetadata.Get(data).hidden)
        {
          continue;
        }
        if (IsPanel(data))
        {
          yield return new UnassignedPanel(new UnassignedBehavior(uri, bs), this);
        }
      }
    }

    public IEnumerable<CardPanel.IAssignedPanel> GetAssignedPanels()
    {
      var bs = actorEditor.GetBehaviorSystem();
      foreach (var assignment in actorEditor.GetAssignedBehaviors())
      {
        var data = assignment.GetBehaviorData();
        if (IsPanel(data))
        {
          yield return new AssignedPanel(assignment, this);
        }
      }
      var customPanel = new MiscPanel(this.actorEditor, this);
      yield return customPanel;
    }

    public AssignedPanel DuplicatePanel(AssignedPanel origPanel)
    {
      using (this.actorEditor.StartUndo($"Duplicate '{origPanel.GetTitle()}' panel"))
      {
        AssignedPanel panelClone = new AssignedPanel(this.actorEditor.AddBehavior(origPanel.GetBehavior().GetUnassigned()), this);
        panelClone.SetUse(origPanel.GetUse(), null);
        var use = origPanel.GetUse();
        use.position += new Vector2(80, -80);
        panelClone.SetUse(use, null);

        // Copy properties...extra work needed for deck properties.
        foreach (PropEditor origProp in origPanel.GetBehavior().GetProperties())
        {
          if (origProp.propType == PropType.CardDeck)
          {
            var origDeck = origPanel.GetSlotByName(origProp.variableName);
            var deckClone = panelClone.GetSlotByName(origProp.variableName);

            // Make sure to insert in sorted order..even with the index option,
            // that'll crash if out of order.
            var origDeckCards = new List<ICardAssignmentModel>(origDeck.GetAssignedCards());
            origDeckCards.Sort((a, b) => origDeck.GetIndexOf(a) - origDeck.GetIndexOf(b));
            foreach (var origCard in origDeckCards)
            {
              var cardClone = deckClone.OnAssignCard(origCard.GetCard());
              cardClone.SetProperties(origCard.GetProperties());
            }
          }
          else
          {
            // Normal - just copy the value
            panelClone.GetBehavior().SetProperty(origProp.variableName, origProp);
          }
        }

        return panelClone;
      }
    }

    public void Dispose()
    {
      // TEMP TEMP  uncomment once refactor done
      // this.actorEditor.Dispose();
      // this.actorEditor = null;
    }

    public ICardAssignmentModel FindAssignedCard(string id)
    {
      foreach (var panel in GetAssignedPanels())
      {
        foreach (var slot in panel.GetDecks())
        {
          foreach (var assigned in slot.GetAssignedCards())
          {
            if (assigned.GetId() == id)
            {
              return assigned;
            }
          }
        }
      }
      return null;
    }

    public string GetId()
    {
      return this.actorEditor.GetActorName();
    }

    public ActorBehaviorsEditor GetActorEditor() { return this.actorEditor; }

    public bool IsValid()
    {
      return this.actorEditor.IsValid();
    }

    BrainMetadata GetBrainMetadata()
    {
      BrainMetadata md = Util.FromJsonSafe<BrainMetadata>(actorEditor.GetMetadataJson());
      return md;
    }

    void SetBrainMetadata(BrainMetadata md)
    {
      actorEditor.SetMetadataJson(JsonUtility.ToJson(md));
    }

    public PanelManager.ViewState GetViewState()
    {
      return GetBrainMetadata().panelViewState;
    }

    public void SetViewState(PanelManager.ViewState viewState)
    {
      BrainMetadata md = GetBrainMetadata();
      md.panelViewState = viewState;
      // Do NOT undo view state changes.
      SetBrainMetadata(md);
    }

    public PanelNote.Data[] GetNotes()
    {
      return GetBrainMetadata().panelNotes;
    }

    public void SetNotes(PanelNote.Data[] notesData)
    {
      BrainMetadata md = GetBrainMetadata();
      md.panelNotes = notesData;

      using (this.actorEditor.StartUndo($"Edit {this.actorEditor.GetActorDisplayName()} panel notes"))
      {
        SetBrainMetadata(md);
      }
    }

    public bool CanWrite()
    {
      return this.actorEditor.CanWrite();
    }
  }

  VoosEngine engine;
  UndoStack undo;

  void Awake()
  {
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref undo);
  }

  public ICardManagerModel GetCardManager(VoosActor actor)
  {
    // TODO we should think about, is it possible to return the same instance of
    // this object for the same actor? Then users could use reference equality,
    // hold on to old instances, etc...what if the brain changes? There is
    // implicit state in ABE..
    return new ManagerImpl(new ActorBehaviorsEditor(actor.GetName(), engine, undo));
  }

}