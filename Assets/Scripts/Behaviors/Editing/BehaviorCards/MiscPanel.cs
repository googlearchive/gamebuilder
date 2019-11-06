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
  class MiscPanel : CardPanel.IAssignedPanel, IDeckModel, Unassigner
  {
    ActorBehaviorsEditor actorEditor;
    BehaviorCards.ManagerImpl parent;

    public MiscPanel(ActorBehaviorsEditor actorEditor, BehaviorCards.ManagerImpl parent)
    {
      this.actorEditor = actorEditor;
      this.parent = parent;
    }

    public string GetDescription() { return ""; }
    public Color GetColor() { return CUSTOM_PANEL_COLOR; }
    public string GetPropertyName() { return null; } // For CardSlot.IModel
    public Sprite GetIcon()
    {
      return Resources.Load("BuiltinAssets/PanelIcons/gear-icon", typeof(Sprite)) as Sprite;
    }
    public string GetTitle() { return "Custom"; }

    public IEnumerable<IDeckModel> GetDecks()
    {
      // We just have one slot, that we implement ourselves.
      yield return this;
    }

    public IEnumerable<ICardAssignmentModel> GetAssignedCards()
    {
      var bs = actorEditor.GetBehaviorSystem();
      foreach (var useEditor in actorEditor.GetAssignedBehaviors())
      {
        var behaviorData = useEditor.GetBehaviorData();
        if (CardMetadata.IsCardOfCategory(behaviorData, CUSTOM_CATEGORY))
        {
          // TEMP HACK - it's really important to not show the default card
          if (useEditor.GetBehaviorUri() == "builtin:Default Behavior")
          {
            continue;
          }
          yield return new CardAssignment(useEditor, this);
        }
      }
    }

    // The one slot we have
    public string GetCardCategory() { return CUSTOM_CATEGORY; }
    public string GetPrompt() { return ""; }

    public ICardAssignmentModel OnAssignCard(ICardModel card, int index = -1)
    {
      using (this.actorEditor.StartUndo($"Add Custom card {card.GetTitle()}"))
      {
        var assigned = actorEditor.AddBehavior(((UnassignedCard)card).GetUnassignedBehaviorItem());
        return new CardAssignment(assigned, this);
      }
    }

    public void Remove()
    {
      Util.LogError($"The default panel cannot be removed.");
    }

    public void Unassign(CardAssignment card)
    {
      using (this.actorEditor.StartUndo($"Remove card {card.GetCard().GetTitle()}"))
      {
        AssignedBehavior editor = card.GetAssignedBehavior();
        editor.RemoveSelfFromActor();
      }
    }

    public ICardAssignmentModel CreateAndAssignNewCard()
    {
      string metaJson = JsonUtility.ToJson(new CardMetadata
      {
        cardSystemCardData = new CardMetadata.Data
        {
          description = "Describe what your card does here!",
          isCard = true,
          title = "Name your card",
          categories = CUSTOM_CATEGORIES
        }
      });
      UnassignedBehavior newBehavior = this.actorEditor.CreateNewBehavior(CodeTemplates.MISC, metaJson);
      return this.OnAssignCard(new UnassignedCard(newBehavior));
    }

    public int GetNumAssignedCards()
    {
      throw new NotImplementedException();
    }

    public IEnumerable<ICardModel> GetDefaultCards()
    {
      throw new NotImplementedException();
    }

    public AssignedBehavior GetBehavior()
    {
      return null;
    }

    public int GetIndexOf(ICardAssignmentModel assignedModel)
    {
      return 0;
    }
    public string GetId()
    {
      return BehaviorCards.GetMiscPanelId();
    }

    public bool IsFake()
    {
      return true;
    }

    public CardPanel.PanelUse GetUse()
    {
      return Util.FromJsonSafe<BrainMetadata>(actorEditor.GetMetadataJson()).miscPanelUseMetadata;
    }

    public void SetUse(CardPanel.PanelUse miscMeta, string undoLabel)
    {
      using (undoLabel != null ?
        actorEditor.StartUndo(undoLabel) :
        new Util.DummyDisposable())
      {
        BrainMetadata md = Util.FromJsonSafe<BrainMetadata>(actorEditor.GetMetadataJson());
        md.miscPanelUseMetadata = miscMeta;
        actorEditor.SetMetadataJson(JsonUtility.ToJson(md));
      }
    }

    public CardPanel.IAssignedPanel Duplicate()
    {
      throw new NotImplementedException();
    }
  }

  public static string CUSTOM_CATEGORY = "Custom";
  public static string[] CUSTOM_CATEGORIES = new string[] { CUSTOM_CATEGORY };
  public static Color CUSTOM_PANEL_COLOR = Color.grey;

  public static string GetMiscPanelId()
  {
    return "__MISC__";
  }

  public static string GetMiscMetadataJson()
  {
    return JsonUtility.ToJson(new CardMetadata
    {
      cardSystemCardData = new CardMetadata.Data
      {
        description = "Describe what your card does here!",
        isCard = true,
        title = "Name your card",
        categories = CUSTOM_CATEGORIES
      }
    });
  }
}