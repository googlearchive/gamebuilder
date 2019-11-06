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

// Keeps track of what actor the player controls, changing it as needed.
public class PlayerControlsManager : MonoBehaviour
{
  UserMain userMain;
  VirtualPlayerManager virtualPlayerManager;
  VoosEngine engine;
  string virtualPlayerId;
  float requestOwnershipCooldown;
  bool registeredVirtualPlayerId;
  DynamicPopup popups;
  List<VoosActor> temp = new List<VoosActor>();
  // As last reported to virtualPlayerManager, are we in edit mode?
  bool reportedWeAreInEditMode;

  // My virtual player IDs. This will normally be a list of just 1, and it will be equal to virtualPlayerId.
  // More IDs will be added if the user enters the simulated multiplayer commands like "vpjoin".
  List<string> myVirtualPlayerIds = new List<string>();

  // Lazily queried because it's not available on Awake()
  PersistentMessagePanel messagePanelLazy;

  enum WarnState
  {
    // Warning not shown.
    NO_WARNING,
    // We've noticed an incorrect number of player controlled actors, and are counting down to warn.
    COUNTING_DOWN,
    // Showing warning.
    SHOWING_WARNING
  }
  WarnState warnState = WarnState.NO_WARNING;
  float countdownToWarn;  // only valid if warnState == WarnState.COUNTING_DOWN

  const float DELAY_BEFORE_WARNING = 2;

  const string WARN_NO_ACTOR_TO_CONTROL_FMT = "You can't play because there is nothing in the scene for you to control.\nCreate an actor with a Player Controls panel set to Player {0}.";
  const string WARN_MORE_THAN_ONE_ACTOR_TO_CONTROL_FMT = "There is more than one actor set to be controlled by Player {0}.\nMake sure there is only one. The actors are:\n";

  public void Awake()
  {
    Util.FindIfNotSet(this, ref virtualPlayerManager);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref popups);
    virtualPlayerId = System.Guid.NewGuid().ToString();
    myVirtualPlayerIds.Add(virtualPlayerId);
    engine.onBeforeActorDestroy += OnActorDestroyedOrUncontrollable;
    engine.onActorBecomingUncontrollable += OnActorDestroyedOrUncontrollable;
  }

  void OnDestroy()
  {
    if (engine != null)
    {
      engine.onBeforeActorDestroy -= OnActorDestroyedOrUncontrollable;
      engine.onActorBecomingUncontrollable -= OnActorDestroyedOrUncontrollable;
    }
  }

  public string GetVirtualPlayerId()
  {
    return virtualPlayerId;
  }

  void Update()
  {
    if (!registeredVirtualPlayerId && (PhotonNetwork.offlineMode || PhotonNetwork.inRoom))
    {
      virtualPlayerManager.RegisterVirtualPlayer(virtualPlayerId);
      registeredVirtualPlayerId = true;
    }

    if (null == (userMain = userMain ?? GameObject.FindObjectOfType<UserMain>()))
    {
      // Not ready.
      return;
    }

    if (reportedWeAreInEditMode != userMain.InEditMode())
    {
      virtualPlayerManager.SetPlayerIsInEditMode(virtualPlayerId, userMain.InEditMode());
      reportedWeAreInEditMode = userMain.InEditMode();
    }

    TransferPlayerAsNeeded();
  }

  private void TransferPlayerAsNeeded()
  {
    // What actors want to be controlled by the player?
    List<VoosActor> actorsToControl = temp;
    engine.GetActorsControlledByVirtualPlayerId(virtualPlayerId, actorsToControl);
    // What actor is the player is currently controlling?
    // (use null to mean a placeholder actor)
    VoosActor currentActor = userMain.GetPlayerActor();
    // What actor should the player be controlling?
    // It's the one match. If there is not exactly one match, then none (conflict).
    VoosActor actorToControl = actorsToControl.Count == 1 ? actorsToControl[0] : null;
    // Transfer control if needed.
    if (actorToControl != currentActor)
    {
      TransferUserTo(actorToControl);
    }
    // Request ownership periodically, if we don't own ourselves.
    requestOwnershipCooldown -= Time.unscaledDeltaTime;
    if (actorToControl != null && !actorToControl.IsLocallyOwned() && requestOwnershipCooldown <= 0)
    {
      requestOwnershipCooldown = 2;
      actorToControl.RequestOwnership();
    }
    // Show error message if applicable.
    UpdateWarningState(actorsToControl);
  }

  private void UpdateWarningState(List<VoosActor> actorsToControl)
  {
    messagePanelLazy = messagePanelLazy ?? GameObject.FindObjectOfType<PersistentMessagePanel>();
    if (messagePanelLazy == null) return;
    // If we haven't been assigned a player number yet, don't warn.
    if (GetMyPlayerNumber() <= 0) return;
    // Whenever we go into edit mode, we reset to "not warned state" to warn the user again if they
    // go into play mode.
    if (userMain.InEditMode())
    {
      warnState = WarnState.NO_WARNING;
      messagePanelLazy.Hide();
      return;
    }
    switch (warnState)
    {
      case WarnState.NO_WARNING:
        if (actorsToControl.Count != 1)
        {
          warnState = WarnState.COUNTING_DOWN;
          countdownToWarn = DELAY_BEFORE_WARNING;
        }
        break;
      case WarnState.COUNTING_DOWN:
        if (actorsToControl.Count == 1)
        {
          warnState = WarnState.NO_WARNING;
        }
        if (engine.GetIsRunning() && 0 > (countdownToWarn -= Time.unscaledDeltaTime))
        {
          warnState = WarnState.SHOWING_WARNING;
          ShowControlsWarning(actorsToControl);
        }
        break;
      case WarnState.SHOWING_WARNING:
        if (actorsToControl.Count == 1)
        {
          warnState = WarnState.NO_WARNING;
          messagePanelLazy.Hide();
        }
        break;
      default:
        throw new System.Exception("Invalid warn state " + warnState);
    }
  }

  private void ShowControlsWarning(List<VoosActor> actorsToControl)
  {
    string message;
    if (actorsToControl.Count == 0)
    {
      message = string.Format(WARN_NO_ACTOR_TO_CONTROL_FMT, GetMyPlayerNumber());
    }
    else
    {
      message = string.Format(WARN_MORE_THAN_ONE_ACTOR_TO_CONTROL_FMT, GetMyPlayerNumber());
      int count = 0;
      foreach (VoosActor actor in actorsToControl)
      {
        if (count >= 3)
        {
          message += " (...)";
          break;
        }
        ++count;
        message += "\n * " + actor.GetDisplayName();
      }
    }
    messagePanelLazy.Show(message);
  }

  private void OnActorDestroyedOrUncontrollable(VoosActor actor)
  {
    if (userMain != null && userMain.GetPlayerActor() == actor)
    {
      // Our current actor is destroyed, so migrate over to a placeholder for now,
      // until Update() figures out where we should move next.
      TransferUserTo(null);
    }
  }

  // Returns my player# or 0 if not yet assigned.
  public int GetMyPlayerNumber()
  {
    VirtualPlayerManager.VirtualPlayerInfo? playerInfo = virtualPlayerManager.GetVirtualPlayerById(virtualPlayerId);
    return playerInfo != null ? playerInfo.Value.slotNumber : 0;
  }

  void TransferUserTo(VoosActor newActor)
  {
    if (newActor != null && !newActor.IsLocallyOwned())
    {
      newActor.RequestOwnership();
    }

    if (newActor != null && !newActor.GetIsPlayerControllable())
    {
      newActor.SetIsPlayerControllable(true);
    }

    Debug.Log("Migrating user to actor " + newActor);
    userMain.MigrateUserTo(newActor);
    Debug.Assert(userMain.GetPlayerActor() == newActor, "Actor migration failed?");
  }

  void SwitchToNewVirtualPlayer()
  {
    virtualPlayerId = System.Guid.NewGuid().ToString();
    myVirtualPlayerIds.Add(virtualPlayerId);
    virtualPlayerManager.RegisterVirtualPlayer(virtualPlayerId);
    SwitchToVirtualPlayerId(virtualPlayerId);
  }

  bool SwitchToVirtualPlayerNumber(int playerNumber)
  {
    VirtualPlayerManager.VirtualPlayerInfo? info = virtualPlayerManager.GetVirtualPlayerBySlotNumber(playerNumber);
    if (info == null)
    {
      Debug.LogError($"Player {playerNumber} does not exist.");
      return false;
    }
    SwitchToVirtualPlayerId(info.Value.virtualId);
    return true;
  }

  bool LeaveAsCurrentVirtualPlayer()
  {
    if (myVirtualPlayerIds.Count < 2)
    {
      Debug.LogError($"You can't leave because you're the only virtual player.");
      return false;
    }
    virtualPlayerManager.UnregisterVirtualPlayer_ForSinglePlayerOnly(virtualPlayerId);
    myVirtualPlayerIds.Remove(virtualPlayerId);
    SwitchToVirtualPlayerId(myVirtualPlayerIds[0]);
    return true;
  }

  void SwitchToVirtualPlayerId(string virtualPlayerId)
  {
    Debug.Assert(myVirtualPlayerIds.Contains(virtualPlayerId), "VP ID not created by me: " + virtualPlayerId);
    string oldVirtualPlayerId = this.virtualPlayerId;
    this.virtualPlayerId = virtualPlayerId;
    // Pretend that the abandoned player is now in play mode.
    virtualPlayerManager.SetPlayerIsInEditMode(oldVirtualPlayerId, false);
    virtualPlayerManager.SetPlayerIsInEditMode(virtualPlayerId, userMain.InEditMode());
  }

  void ChangeCurrentNickname(string newNickName)
  {
    virtualPlayerManager.SetNickName(virtualPlayerId, newNickName);
  }

  [CommandTerminal.RegisterCommand(Help = "Joins game as a new virtual player")]
  public static void VpJoin(CommandTerminal.CommandArg[] args)
  {
    if (GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer)
    {
      CommandTerminal.HeadlessTerminal.Log("*** Not available in multiplayer.");
      return;
    }
    GameObject.FindObjectOfType<PlayerControlsManager>().SwitchToNewVirtualPlayer();
    CommandTerminal.HeadlessTerminal.Log("Switched to new virtual player.");
  }

  [CommandTerminal.RegisterCommand(Help = "Leaves game as current virtual player (switches to initial virtual player)")]
  public static void VpLeave(CommandTerminal.CommandArg[] args)
  {
    if (GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer)
    {
      CommandTerminal.HeadlessTerminal.Log("*** Not available in multiplayer.");
      return;
    }
    if (GameObject.FindObjectOfType<PlayerControlsManager>().LeaveAsCurrentVirtualPlayer())
    {
      CommandTerminal.HeadlessTerminal.Log("Left.");
    }
  }

  [CommandTerminal.RegisterCommand(Help = "vpswitch n: switches to virtual player #n", MinArgCount = 1, MaxArgCount = 1)]
  public static void VpSwitch(CommandTerminal.CommandArg[] args)
  {
    if (GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer)
    {
      CommandTerminal.HeadlessTerminal.Log("*** Not available in multiplayer.");
      return;
    }
    if (GameObject.FindObjectOfType<PlayerControlsManager>().SwitchToVirtualPlayerNumber(args[0].Int))
    {
      CommandTerminal.HeadlessTerminal.Log("Switched.");
    }
    else
    {
      CommandTerminal.HeadlessTerminal.Log("*** Failed.");
    }
  }

  [CommandTerminal.RegisterCommand(Help = "vpnick new_name: changes the nickname of current virtual player", MinArgCount = 1, MaxArgCount = 1)]
  public static void VpNick(CommandTerminal.CommandArg[] args)
  {
    if (GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer)
    {
      CommandTerminal.HeadlessTerminal.Log("*** Not available in multiplayer.");
      return;
    }
    GameObject.FindObjectOfType<PlayerControlsManager>().ChangeCurrentNickname(args[0].String);
    CommandTerminal.HeadlessTerminal.Log("Done.");
  }
}
