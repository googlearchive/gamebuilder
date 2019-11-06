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
using ExitGames.UtilityScripts;

public class DebugNetworkingGUI : MonoBehaviour
{
  public GUISkin Skin;
  [SerializeField] VoosEngine engine;
  [SerializeField] GlobalUnreliableData unreliable;
#if USE_PUN
  void Awake()
  {
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref unreliable);
  }

  void Start()
  {
    if (PhotonNetwork.offlineMode)
    {
      MonoBehaviour.Destroy(this);
    }
  }

  public void DrawActorDebugGUI(VoosActor actor)
  {
    using (new Util.GUILayoutFrobArea(actor.transform.position, 100, 500))
    {
      PhotonView photonView = PhotonView.Get(actor);
      if (photonView == null)
      {
        return;
      }
      PlayerBody playerBody = actor.GetPlayerBody();
      //string pbodyInfo = playerBody == null ? "" : $"Claimed? {playerBody.IsClaimed()} ..play mode? {playerBody.GetIsClaimerPlaying()}";
      string color = photonView.isMine ? "yellow" : "grey";
      string hash = actor.GetName().Substring(0, 9);
      bool locked = actor.IsLockedByAnother() || actor.IsLockWantedLocally();
      string lockingString = locked ? " LOCKED" : "";
      string lastPos = actor.unrel == null ? "" : actor.unrel.lastPosition.x.ToFourDecimalPlaces();
      GUILayout.Label($@"<color={color}>{actor.GetDisplayName()}
rot: {actor.GetRotation().ToFourDecimalPlaces()}
last unrel: {actor.lastUnreliableUpdateTime}
lastPX: {lastPos}</color>".Trim());
      GUILayout.Toggle(actor.GetReplicantCatchUpMode(), "Catchup?");
      actor.debug = GUILayout.Toggle(actor.debug, "Debug");
      // owner: {photonView.ownerId}{lockingString}
      // {hash} view {actor.reliablePhotonView.viewID}
      // X: {actor.transform.position.x.ToFourDecimalPlaces()}
      // lastRBMPX: {actor.lastRBMovedPos.x.ToFourDecimalPlaces()}
    }
  }

  public void OnGUI()
  {
    if (DebugLevel % 3 == 0 || PhotonNetwork.offlineMode)
    {
      return;
    }

    GUI.skin = this.Skin;

    int height = 600;
    GUILayout.BeginArea(new Rect(0, Screen.height / 2 - height / 2, 300, height));

    string stateColorString = PhotonNetwork.inRoom ? "#00ff88" : "yellow";
    GUILayout.Label($"<color={stateColorString}>{PhotonNetwork.connectionStateDetailed.ToString()}</color>");

    if (PhotonNetwork.inRoom)
    {
      GUILayout.Label($"Room: {PhotonNetwork.room.name}");

      System.Action<PhotonPlayer> emitLabel = (PhotonPlayer player) =>
      {
        int localPlayerId = player.ID;
        string playerIsMaster = player.IsMasterClient ? "(master) " : "";
        string you = player == PhotonNetwork.player ? "(you)" : "";
        string playerLabel = Util.GetPlayerName(player.GetRoomIndex());
        GUILayout.Label(string.Format("P{3} ({0}), {1} {2}{4}", localPlayerId, playerLabel, playerIsMaster, player.GetRoomIndex(), you));
      };

      emitLabel(PhotonNetwork.player);
      foreach (PhotonPlayer otherPlayer in PhotonNetwork.otherPlayers)
      {
        emitLabel(otherPlayer);
      }

      GUILayout.Label($"{PhotonNetwork.otherPlayers.Length} other players in room");
    }

    if (unreliable != null)
    {
      var diag = unreliable.GetDiagnostics();
      GUILayout.Label($"Unreliable diag:\n{JsonUtility.ToJson(diag, true)}");
    }
    else
    {
      GUILayout.Label($"Unreliable DNE");
    }

    GUILayout.EndArea();

    if (DebugLevel % 3 == 2)
    {
      foreach (VoosActor actor in engine.EnumerateActors())
      {
        DrawActorDebugGUI(actor);
      }
    }
  }

  int DebugLevel = 0;

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.F1) && Util.IsControlOrCommandHeld())
    {
      DebugLevel++;
    }
  }
#endif
}
