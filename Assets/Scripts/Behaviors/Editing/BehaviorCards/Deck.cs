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

public partial class BehaviorCards
{
  public static string GetDefaultCodeForCategory(string category)
  {
    if (category == "Action")
    {
      return CodeTemplates.ACTION;
    }
    else if (category == "Event")
    {
      return CodeTemplates.EVENT;
    }
    else
    {
      return CodeTemplates.MISC;
    }
  }

  class Deck : IDeckModel, Unassigner
  {
    readonly PropEditor deckEditor;
    readonly ActorBehaviorsEditor actorBehaviorsEditor;
    readonly BehaviorCards.ManagerImpl manager;

    public Deck(PropEditor deckEditor, ActorBehaviorsEditor actorBehaviorsEditor, BehaviorCards.ManagerImpl manager)
    {
      this.deckEditor = deckEditor;
      this.actorBehaviorsEditor = actorBehaviorsEditor;
      this.manager = manager;
    }

    public string GetPropertyName()
    {
      return deckEditor.variableName;
    }

    public ICardAssignmentModel CreateAndAssignNewCard()
    {
      // Make sure the category is for this slot.
      CardMetadata md = CardMetadata.DefaultCardMetadata;
      md.cardSystemCardData.categories = new string[] { deckEditor.cardCategory };
      string metadataJson = JsonUtility.ToJson(md);
      UnassignedBehavior newBehavior = this.actorBehaviorsEditor.CreateNewBehavior(GetDefaultCodeForCategory(deckEditor.cardCategory), metadataJson);
      return this.OnAssignCard(new UnassignedCard(newBehavior));
    }

    public IEnumerable<ICardAssignmentModel> GetAssignedCards()
    {
      HashSet<string> myUseIds = new HashSet<string>();
      foreach (string useId in (string[])deckEditor.data)
      {
        myUseIds.Add(useId);
      }

      foreach (var editor in this.actorBehaviorsEditor.GetAssignedBehaviors())
      {
        if (myUseIds.Contains(editor.useId))
        {
          yield return new CardAssignment(editor, this);
        }
      }
    }

    public string GetCardCategory() { return deckEditor.cardCategory; }

    public string GetPrompt() { return deckEditor.comment; }

    public ICardAssignmentModel OnAssignCard(ICardModel card, int index = -1)
    {
      Debug.Assert(card != null, "Given card was null");
      Debug.Assert(this.actorBehaviorsEditor != null, "Null actorBehaviorsEditor?");
      using (this.actorBehaviorsEditor.StartUndo($"Add {this.GetCardCategory()} card {card.GetTitle()}"))
      {
        UnassignedBehavior behaviorEditor = ((UnassignedCard)card).GetUnassignedBehaviorItem();
        AssignedBehavior assigned = this.actorBehaviorsEditor.AddBehavior(behaviorEditor);
        List<string> deckUseIds = new List<string>((string[])deckEditor.data);
        if (index >= 0)
        {
          if (index > deckUseIds.Count) throw new System.Exception("OnAssignCard: Index greater than deckUseIds count!");
          deckUseIds.Insert(index, assigned.useId);
        }
        else
        {
          deckUseIds.Add(assigned.useId);
        }
        deckEditor.SetData(deckUseIds.ToArray());
        return new CardAssignment(assigned, this);
      }
    }

    public void Unassign(CardAssignment card)
    {
      using (this.actorBehaviorsEditor.StartUndo($"Remove card {card.GetCard().GetTitle()}"))
      {
        AssignedBehavior cardUse = card.GetAssignedBehavior();

        // Remove use ID from deck property.
        List<string> deckUseIds = new List<string>((string[])deckEditor.data);
        bool didExist = deckUseIds.Remove(cardUse.useId);
        if (!didExist)
        {
          Util.LogError($"Programmer error? The removed card did not actually exist in our deck..");
        }
        deckEditor.SetData(deckUseIds.ToArray());

        // Remove from actor brain
        cardUse.RemoveSelfFromActor();
      }
    }

    public Sprite GetIcon()
    {
      if (deckEditor.deckOptions.iconResPath.IsNullOrEmpty())
      {
        return null;
      }
      else
      {
        return Resources.Load(deckEditor.deckOptions.iconResPath, typeof(Sprite)) as Sprite;
      }
    }

    public int GetNumAssignedCards()
    {
      return deckEditor.data == null ? 0 : ((string[])deckEditor.data).Length;
    }

    public IEnumerable<ICardModel> GetDefaultCards()
    {
      if (deckEditor.deckOptions.defaultCardURIs == null)
      {
        yield break;
      }
      foreach (string cardUri in deckEditor.deckOptions.defaultCardURIs)
      {
        yield return new UnassignedCard(new UnassignedBehavior(cardUri, this.actorBehaviorsEditor.GetBehaviorSystem()));
      }
    }

    public int GetIndexOf(ICardAssignmentModel assignedModel)
    {
      List<string> deckUseIds = new List<string>((string[])deckEditor.data);
      return deckUseIds.IndexOf(assignedModel.GetId());
    }
  }
}