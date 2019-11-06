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

public class PlayerControllableWizard : Photon.PunBehaviour
{
  // This must match the file name of the Player Controls Panel script.
  const string PLAYER_CONTROLS_PANEL_URI = "builtin:Player Controls Panel v2";
  // This must match the property name in the Player Controls Panel script.
  const string PLAYER_NUMBER_PROP_NAME = "PlayerNumber";
  // This must match the default value of the player# prop in the Player Controls Panel script.
  const int PLAYER_NUMBER_PROP_DEFAULT_VALUE = 1;

  DynamicPopup popups;
  VirtualPlayerManager virtualPlayerManager;
  PlayerControlsManager playerControlsManager;
  VoosEngine engine;

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref virtualPlayerManager);
    Util.FindIfNotSet(this, ref playerControlsManager);
    Util.FindIfNotSet(this, ref engine);
  }

  public bool WantToShow(VoosActor actor)
  {
    return null != TryGetPlayerControlsPanel(actor);
  }

  public void Show(VoosActor actor)
  {
    // Start with player 0 to avoid warnings/etc.
    AssignPlayerToActor(0, actor.GetName());

    string name = actor.GetDisplayName();
    List<PopupButton.Params> buttons = new List<PopupButton.Params>();

    int myPlayerNumber = playerControlsManager.GetMyPlayerNumber();

    int maxPlayerSlotNumber = 4;  // Show at least player 1-4, unless there are more players.
    foreach (VirtualPlayerManager.VirtualPlayerInfo player in virtualPlayerManager.EnumerateVirtualPlayers())
    {
      maxPlayerSlotNumber = Mathf.Max(player.slotNumber, maxPlayerSlotNumber);
    }
    for (int i = 1; i <= maxPlayerSlotNumber; i++)
    {
      int thisNumber = i; // For closures below
      buttons.Add(new PopupButton.Params
      {
        getLabel = () => $"Player {thisNumber}" + ((thisNumber == myPlayerNumber) ? " (myself)" : ""),
        onClick = () => OnClickedPlayerNumber(actor, thisNumber)
      });
    }
    buttons.Add(new PopupButton.Params { getLabel = () => "Nobody for now", onClick = () => OnClickedPlayerNumber(actor, 0) });
    buttons.Add(new PopupButton.Params { getLabel = () => "It's an NPC", onClick = () => OnClickedIsNpc(actor) });
    popups.Show(new DynamicPopup.Popup
    {
      getMessage = () => $"Who will control this {name}?",
      buttons = buttons,
    });
  }

  void OnClickedPlayerNumber(VoosActor actor, int playerNumber)
  {
    AssignPlayerToActor(playerNumber, actor.GetName());
  }

  void OnClickedIsNpc(VoosActor actor)
  {
    AssignPlayerToActor(ASSIGN_ACTOR_AS_NPC, actor.GetName());
  }

  private static Behaviors.BehaviorUse TryGetPlayerControlsPanel(VoosActor actor)
  {
    Behaviors.Brain brain = actor.GetBehaviorSystem().GetBrain(actor.GetBrainName());
    if (brain == null)
    {
      return null;
    }
    foreach (Behaviors.BehaviorUse use in actor.GetBehaviorSystem().GetBrain(actor.GetBrainName()).GetUses())
    {
      if (use.behaviorUri == PLAYER_CONTROLS_PANEL_URI) return use;
    }
    return null;
  }

  const int ASSIGN_ACTOR_AS_NPC = -1;

  // This is saying "player #playerNumber wants to control ONLY actor actorName, and relinquish control of all others".
  // As a special case, if playerNumber == ASSIGN_PLAYER_NPC, this means "actorName wants to be an NPC".
  void AssignPlayerToActor(int playerNumber, string actorName)
  {
    AssignPlayerToActorLocal(playerNumber, actorName);
    if (photonView != null)
    {
      photonView.RPC("AssignPlayerToActorRPC", PhotonTargets.AllViaServer, playerNumber, actorName);
    }
  }

  [PunRPC]
  void AssignPlayerToActorRPC(int playerNumber, string actorName)
  {
    AssignPlayerToActorLocal(playerNumber, actorName);
  }

  private void AssignPlayerToActorLocal(int playerNumber, string actorName)
  {
    foreach (VoosActor actor in engine.EnumerateActors())
    {
      // Only act on locally owned actors (all clients are running this method by the miracle of RPCs,
      // so each client will act on the actors they own).
      if (!actor.IsLocallyOwned()) continue;

      Behaviors.BehaviorUse playerControlsPanel = TryGetPlayerControlsPanel(actor);
      if (playerControlsPanel == null) continue;

      if (playerNumber != ASSIGN_ACTOR_AS_NPC)
      {
        // If this actor is set to this player#, set it 0 unless it's the desired actor.
        // If this actor is the desired actor, set its player#.
        int thisPlayerNumber = playerControlsPanel.GetPropertyValue<int>(PLAYER_NUMBER_PROP_NAME, PLAYER_NUMBER_PROP_DEFAULT_VALUE);
        if (thisPlayerNumber == playerNumber && actor.GetName() != actorName)
        {
          SetPlayerNumberOnPlayerControlsPanel(actor, playerControlsPanel, 0);
        }
        else if (thisPlayerNumber != playerNumber && actor.GetName() == actorName)
        {
          SetPlayerNumberOnPlayerControlsPanel(actor, playerControlsPanel, playerNumber);
        }
      }
      else
      {
        // Our mission is just to make the desired actor an NPC.
        if (actor.GetName() == actorName)
        {
          DeletePlayerControlsPanel(actor);
        }
      }
    }
  }

  private void SetPlayerNumberOnPlayerControlsPanel(VoosActor actor, Behaviors.BehaviorUse use, int playerNumber)
  {
    use = use.DeepClone();
    use.SetPropertyValue(PLAYER_NUMBER_PROP_NAME, playerNumber);
    Behaviors.Brain brain = actor.GetBehaviorSystem().GetBrain(actor.GetBrainName());
    if (brain == null)
    {
      Debug.LogErrorFormat("Could not set player# on actor {0} ({1}). No brain.", actor.GetName(), actor.GetDisplayName());
      return;
    }
    brain.SetUse(use);
    actor.GetBehaviorSystem().PutBrain(actor.GetBrainName(), brain);
  }

  private void DeletePlayerControlsPanel(VoosActor actor)
  {
    Debug.Assert(actor.IsLocallyOwned(), "Actor should be locally owned");
    Behaviors.BehaviorUse playerControlsPanel = TryGetPlayerControlsPanel(actor);
    if (playerControlsPanel == null) return;
    Behaviors.Brain brain = actor.GetBehaviorSystem().GetBrain(actor.GetBrainName());
    if (brain == null)
    {
      Debug.LogErrorFormat("Could not set player# on actor {0} ({1}). No brain.", actor.GetName(), actor.GetDisplayName());
      return;
    }
    // PLAYER_CONTROLS_PANEL_HAS_NO_DECKS_ASSUMPTION
    // WARNING: this is a naive delete that doesn't recursively look for behavior uses mentioned
    // in any decks used by the Player Controls panel, so if in the future we do add decks to it,
    // we need to update this logic to remove the panel properly.
    brain.DeleteUse(playerControlsPanel.id);
    actor.GetBehaviorSystem().PutBrain(actor.GetBrainName(), brain);
  }
}
