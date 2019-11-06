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

// At the game level (inside of the VOOS bubble), there are only *players* --
// nothing is "virtual". Players are the entities that play the game. That's it.
//
// However, at the engine level, the game is actually played by "virtual players".
// They normally correspond to a flesh-and-bone human player, but not always -- the same
// human player might set up two virtual players to debug a multiplayer game, for instance.
//
// In that case the game thinks of that as 2 players, but they correspond to the
// same user.
//
// Virtual Players are assigned slot numbers ("Player 1", "Player 2", etc) to make it
// easier for scripts to identify them -- especially friendly for cards/panels.
public class VirtualPlayerManager : Photon.PunBehaviour
{
  Dictionary<string, VirtualPlayerInfo> virtualPlayers = new Dictionary<string, VirtualPlayerInfo>();

  public delegate void OnVirtualPlayerJoined(string virtualId);
  public event OnVirtualPlayerJoined onVirtualPlayerJoined;

  public delegate void OnVirtualPlayerLeft(string virtualId);
  public event OnVirtualPlayerLeft onVirtualPlayerLeft;

  public void RegisterVirtualPlayer(string virtualId)
  {
    photonView.RPC("RegisterVirtualPlayerRPC", PhotonTargets.MasterClient, virtualId, PhotonNetwork.player.ID);
  }

  public void SetPlayerIsInEditMode(string virtualId, bool isInEditMode)
  {
    SetPlayerIsInEditModeLocal(virtualId, isInEditMode);
    photonView.RPC("SetPlayerIsInEditModeRPC", PhotonTargets.AllViaServer, virtualId, isInEditMode);
  }

  public void SetPlayerCanEdit(string virtualId, bool canEdit)
  {
    SetPlayerCanEditLocal(virtualId, canEdit);
    photonView.RPC("SetPlayerCanEditRPC", PhotonTargets.AllViaServer, virtualId, canEdit);
  }

  public void SetNickName(string virtualId, string nickName)
  {
    VirtualPlayerInfo? maybeInfo = GetVirtualPlayerById(virtualId);
    if (maybeInfo != null)
    {
      VirtualPlayerInfo info = maybeInfo.Value;
      info.nickName = nickName;
      // This doesn't happen often (it's a debug command), so let's piggy back on an existing RPC:
      photonView.RPC("PutVirtualPlayerRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(info));
    }
  }

  private void SetPlayerCanEditLocal(string virtualId, bool canEdit)
  {
    VirtualPlayerInfo info;
    if (virtualPlayers.TryGetValue(virtualId, out info))
    {
      info.canEdit = canEdit;
      // We must write it back because VirtualPlayerInfo is a struct.
      virtualPlayers[virtualId] = info;
    }
  }

  void SetPlayerIsInEditModeLocal(string virtualId, bool isInEditMode)
  {
    VirtualPlayerInfo info;
    if (virtualPlayers.TryGetValue(virtualId, out info))
    {
      info.isInEditMode = isInEditMode;
      // We must write it back because VirtualPlayerInfo is a struct.
      virtualPlayers[virtualId] = info;
    }
  }

  [PunRPC]
  void SetPlayerIsInEditModeRPC(string virtualId, bool isInEditMode)
  {
    SetPlayerIsInEditModeLocal(virtualId, isInEditMode);
  }


  [PunRPC]
  void SetPlayerCanEditRPC(string virtualId, bool canEdit)
  {
    SetPlayerCanEditLocal(virtualId, canEdit);
  }

  public VirtualPlayerInfo? GetVirtualPlayerById(string virtualId)
  {
    VirtualPlayerInfo info;
    return !string.IsNullOrEmpty(virtualId) && virtualPlayers.TryGetValue(virtualId, out info) ? (VirtualPlayerInfo?)info : null;
  }

  public VirtualPlayerInfo? GetVirtualPlayerBySlotNumber(int slotNumber)
  {
    foreach (KeyValuePair<string, VirtualPlayerInfo> pair in virtualPlayers)
    {
      if (pair.Value.slotNumber == slotNumber) return pair.Value;
    }
    return null;
  }

  public IEnumerable<VirtualPlayerInfo> EnumerateVirtualPlayers()
  {
    return virtualPlayers.Values;
  }

  public VirtualPlayerInfo? GetInfoForPhotonPlayerId(int photonPlayerId)
  {
    foreach (var info in EnumerateVirtualPlayers())
    {
      if (info.photonPlayerId == photonPlayerId)
      {
        return info;
      }
    }
    return null;
  }

  public int GetVirtualPlayerCount()
  {
    return virtualPlayers.Count;
  }

  // Handled by master client.
  [PunRPC]
  private void RegisterVirtualPlayerRPC(string virtualId, int photonPlayerId)
  {
    if (!PhotonNetwork.isMasterClient)
    {
      return;
    }
    if (virtualPlayers.ContainsKey(virtualId))
    {
      // Already registered.
      return;
    }
    // Allocate the lowest free slot#
    int slotNumber = GetLowestFreeSlotNumber();
    string nickName = "Player" + slotNumber;
    foreach (PhotonPlayer player in PhotonNetwork.playerList)
    {
      if (player.ID == photonPlayerId && !string.IsNullOrEmpty(player.NickName))
      {
        nickName = player.NickName;
      }
    }
    VirtualPlayerInfo info = new VirtualPlayerInfo
    {
      virtualId = virtualId,
      photonPlayerId = photonPlayerId,
      slotNumber = slotNumber,
      nickName = nickName
    };
    PutVirtualPlayerLocal(info);
    photonView.RPC("PutVirtualPlayerRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(info));
  }

  [PunRPC]
  private void PutVirtualPlayerRPC(string virtualPlayerInfoJson)
  {
    PutVirtualPlayerLocal(JsonUtility.FromJson<VirtualPlayerInfo>(virtualPlayerInfoJson));
  }

  private void PutVirtualPlayerLocal(VirtualPlayerInfo info)
  {
    bool isNew = !virtualPlayers.ContainsKey(info.virtualId);
    virtualPlayers[info.virtualId] = info;
    if (isNew)
    {
      onVirtualPlayerJoined?.Invoke(info.virtualId);
    }
  }

  public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
  {
    // Unregister all virtual players belonging to that player.
    HashSet<string> virtualIdsToDelete = new HashSet<string>();
    foreach (KeyValuePair<string, VirtualPlayerInfo> pair in virtualPlayers)
    {
      if (otherPlayer.ID == pair.Value.photonPlayerId)
      {
        virtualIdsToDelete.Add(pair.Key);
      }
    }
    foreach (string virtualIdToDelete in virtualIdsToDelete)
    {
      HandleVirtualPlayerLeft(virtualIdToDelete);
    }
  }

  public void UnregisterVirtualPlayer_ForSinglePlayerOnly(string virtualId)
  {
    Debug.Assert(!GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer, "Can only be called in single-player games.");
    // This is a debug utility for simulated multiplayer games that allows the user to simulate
    // that the given virtual player has left.
    HandleVirtualPlayerLeft(virtualId);
  }

  private void HandleVirtualPlayerLeft(string virtualId)
  {
    if (virtualPlayers.ContainsKey(virtualId))
    {
      onVirtualPlayerLeft?.Invoke(virtualId);
      virtualPlayers.Remove(virtualId);
    }
  }

  public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
  {
    if (!PhotonNetwork.isMasterClient)
    {
      return;
    }
    foreach (KeyValuePair<string, VirtualPlayerInfo> pair in virtualPlayers)
    {
      photonView.RPC("PutVirtualPlayerRPC", newPlayer, JsonUtility.ToJson(pair.Value));
    }
  }

  private int GetLowestFreeSlotNumber()
  {
    HashSet<int> takenSlots = new HashSet<int>();
    // This is an O(n) implementation but virtualPlayers is a small dictionary
    foreach (VirtualPlayerInfo info in virtualPlayers.Values)
    {
      takenSlots.Add(info.slotNumber);
    }
    for (int i = 1; ; i++)
    {
      if (!takenSlots.Contains(i)) return i;
    }
  }

  [System.Serializable]
  public struct VirtualPlayerInfo
  {
    // ID of this virtual player
    public string virtualId;
    // The real-world player who owns this virtual player.
    public int photonPlayerId;
    // Slot number associated with this virtual player.
    public int slotNumber;
    // Is this player in play mode? (as opposed to edit mode)
    public bool isInEditMode;
    // Can this player edit
    public bool canEdit;
    // Nickname
    public string nickName;

    public override string ToString()
    {
      return $"PLAYER #{slotNumber}, nickName={nickName}, vid={virtualId}, photonPlayerId={photonPlayerId}, isInEditMode={isInEditMode}";
    }
  }

  [CommandTerminal.RegisterCommand(Help = "Prints all virtual players")]
  public static void CommandVpList(CommandTerminal.CommandArg[] args)
  {
    CommandTerminal.HeadlessTerminal.Log("VIRTUAL PLAYERS");
    VirtualPlayerManager instance = GameObject.FindObjectOfType<VirtualPlayerManager>();
    foreach (KeyValuePair<string, VirtualPlayerInfo> pair in instance.virtualPlayers)
    {
      CommandTerminal.HeadlessTerminal.Log(pair.Value.ToString());
    }
  }
}
