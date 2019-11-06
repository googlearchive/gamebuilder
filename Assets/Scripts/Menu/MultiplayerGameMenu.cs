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

public class MultiplayerGameMenu : MonoBehaviour
{
  [SerializeField] MultiplayerMenuUI multiplayerMenuUI;
  [SerializeField] GameObject playerReportingObject;
  PlayerControlsManager playerControlsManager;
  NetworkingController networkingController;
  VoosEngine engine;
  VirtualPlayerManager virtualPlayerManager;

  // KickPlayer kickPlayer;
  ReportPlayer reportPlayer;

  bool showingJoinCode = true;
  string singleplayerNotice = "You are currently\nin single player";
  string multiplayerNotice = "You are currently\nin multiplayer";

  List<PlayerListItem> playerList = new List<PlayerListItem>();

  public void Setup()
  {
    Util.FindIfNotSet(this, ref playerControlsManager);
    Util.FindIfNotSet(this, ref networkingController);
    Util.FindIfNotSet(this, ref engine);

    virtualPlayerManager = engine.GetVirtualPlayerManager();

    multiplayerMenuUI.copyCodeButton.onClick.AddListener(OnCopyClicked);
    multiplayerMenuUI.closeButton.onClick.AddListener(Close);
    multiplayerMenuUI.joinCodeField.text = PhotonNetwork.room.Name;

    multiplayerMenuUI.hideCodeButton.onClick.AddListener(() => showingJoinCode = false);
    multiplayerMenuUI.showCodeButton.onClick.AddListener(() => showingJoinCode = true);
    multiplayerMenuUI.closeButton.onClick.AddListener(Close);

    GameObject reportingObject = Instantiate(playerReportingObject);
    // kickPlayer = reportingObject.GetComponentInChildren<KickPlayer>(true);
    reportPlayer = reportingObject.GetComponentInChildren<ReportPlayer>(true);

    // multiplayerMenuUI.reportPlayerButton.onClick.AddListener(reportPlayer.Open);
    // multiplayerMenuUI.kickPlayerButton.onClick.AddListener(kickPlayer.Open);
  }

  public bool Back()
  {
    // if (kickPlayer.IsOpen())
    // {
    //   kickPlayer.Close();
    //   return true;
    // }

    if (reportPlayer.IsOpen())
    {
      reportPlayer.Close();
      return true;
    }

    if (IsOpen())
    {
      Close();
      return true;
    }

    return false;

  }

  void OnCopyClicked()
  {
    StartCoroutine(OnCopyClickedRoutine());
  }

  public void Open()
  {
    gameObject.SetActive(true);
    Update();
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Toggle()
  {
    if (IsOpen()) Close();
    else Open();
  }

  public UnityEngine.UI.Button GetJoinMultiplayerButton()
  {
    return multiplayerMenuUI.createOrJoinMultiplayerButton;
  }


  void Update()
  {
    multiplayerMenuUI.hideCodeButton.gameObject.SetActive(networkingController.GetIsInMultiplayer() && showingJoinCode);
    multiplayerMenuUI.copyCodeButton.gameObject.SetActive(networkingController.GetIsInMultiplayer() && showingJoinCode);
    multiplayerMenuUI.showCodeButton.gameObject.SetActive(networkingController.GetIsInMultiplayer() && !showingJoinCode);
    multiplayerMenuUI.playerListObject.SetActive(networkingController.GetIsInMultiplayer());

    multiplayerMenuUI.singlePlayerGameObject.SetActive(!networkingController.GetIsInMultiplayer());
    multiplayerMenuUI.playerNumberGameObject.SetActive(networkingController.GetIsInMultiplayer());
    multiplayerMenuUI.joinCodeGameObject.SetActive(networkingController.GetIsInMultiplayer() && showingJoinCode);

    multiplayerMenuUI.newUserCanBuildObject.SetActive(networkingController.GetIsInMultiplayer() && PhotonNetwork.isMasterClient);


    // multiplayerMenuUI.reportPlayerButton.gameObject.SetActive(networkingController.GetIsInMultiplayer());
    // multiplayerMenuUI.kickPlayerButton.gameObject.SetActive(networkingController.GetIsInMultiplayer() && PhotonNetwork.isMasterClient);

    // multiplayerMenuUI.multiplayerStatusField.text = networkingController.GetIsInMultiplayer() ? multiplayerNotice : singleplayerNotice;

    if (!networkingController.GetIsInMultiplayer())
    {
      return;
    }

    RefreshUserList();

    if (playerControlsManager.GetMyPlayerNumber() == 0)
    {
      multiplayerMenuUI.playerNumberField.text = "";
    }
    else
    {
      multiplayerMenuUI.playerNumberField.text = "You are Player " + playerControlsManager.GetMyPlayerNumber();
    }
  }

  struct PlayerListInfo
  {
    public int slot;
    public string name;
    public bool canBuild;
  }

  internal bool GetNewUsersCanBuild()
  {
    return multiplayerMenuUI.newUsersCanBuild.isOn;
  }

  void RefreshUserList()
  {
    int counter = 0;
    foreach (VirtualPlayerManager.VirtualPlayerInfo player in virtualPlayerManager.EnumerateVirtualPlayers())
    {
      if (counter + 1 > playerList.Count)
      {
        PlayerListItem newItem = Instantiate(multiplayerMenuUI.playerListTemplate, multiplayerMenuUI.playerListObject.transform);
        newItem.Setup(virtualPlayerManager);
        newItem.onReportOrKick = OnReportOrKick;
        playerList.Add(newItem);
        newItem.gameObject.SetActive(true);
      }
      playerList[counter].SetVirtualPlayerInfo(player, /* isYou */playerControlsManager.GetMyPlayerNumber() == player.slotNumber);
      counter++;
    }

    if (counter < playerList.Count)
    {
      for (int i = counter; i < playerList.Count; i++)
      {
        playerList[i].RequestDestroy();
      }

      playerList.RemoveRange(counter, playerList.Count - counter);
    }

    foreach (PlayerListItem item in playerList)
    {
      item.UpdateEditToggle();
    }

    // if(counter )
  }

  void OnReportOrKick(VirtualPlayerManager.VirtualPlayerInfo player)
  {
    PhotonPlayer photonPlayer = PhotonPlayer.Find(player.photonPlayerId);
    reportPlayer.Open(photonPlayer, PhotonNetwork.isMasterClient);
  }

  IEnumerator OnCopyClickedRoutine()
  {
    Util.CopyToUserClipboard(PhotonNetwork.room.Name);
    multiplayerMenuUI.copyCodeButton.onClick.RemoveListener(OnCopyClicked);
    string orig = multiplayerMenuUI.copyButtonTextField.text;
    multiplayerMenuUI.copyButtonTextField.text = "Copied!";
    yield return new WaitForSecondsRealtime(1f);
    multiplayerMenuUI.copyButtonTextField.text = orig;
    multiplayerMenuUI.copyCodeButton.onClick.AddListener(OnCopyClicked);
  }

  public void SetOpen(bool on)
  {
    if (on) Open();
    else Close();
  }
}
