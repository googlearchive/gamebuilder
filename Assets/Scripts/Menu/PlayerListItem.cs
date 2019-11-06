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

public class PlayerListItem : MonoBehaviour
{
  public TMPro.TextMeshProUGUI nameField;
  public UnityEngine.UI.Toggle editToggle;
  public ItemWithTooltipWithEventSystem editToggleTooltip;
  public UnityEngine.UI.Button reportButton;
  public GameObject reportButtonEmptyPlaceholder;
  public TMPro.TextMeshProUGUI reportButtonLabel;

  VirtualPlayerManager virtualPlayerManager;
  VirtualPlayerManager.VirtualPlayerInfo player;

  public System.Action<VirtualPlayerManager.VirtualPlayerInfo> onReportOrKick;

  bool canEditUpdated = false;
  bool desiredCanEdit;

  const string editToggleOnClient = "Building enabled\n(set by host)";
  const string editToggleOffClient = "Building disabled\n(set by host)";
  const string editToggleOnMaster = "Building enabled";
  const string editToggleOffMaster = "Building disabled";

  public void Setup(VirtualPlayerManager virtualPlayerManager)
  {
    this.virtualPlayerManager = virtualPlayerManager;
    editToggle.isOn = player.canEdit;
    desiredCanEdit = player.canEdit;
    editToggle.onValueChanged.AddListener(OnCanEditToggled);

    reportButton.onClick.AddListener(() => onReportOrKick?.Invoke(player));
  }

  private void OnCanEditToggled(bool value)
  {
    // Debug.Log("ON TOGG " + value);
    if (canEditUpdated) return;
    canEditUpdated = true;
    desiredCanEdit = value;
  }

  internal void SetVirtualPlayerInfo(VirtualPlayerManager.VirtualPlayerInfo player, bool isYou)
  {
    this.player = player;

    if (isYou)
    {
      nameField.text = player.slotNumber + ": " + PhotonPlayer.Find(player.photonPlayerId).NickName + "(you)";
      reportButton.gameObject.SetActive(false);
      reportButtonEmptyPlaceholder.SetActive(true);
    }
    else
    {
      reportButton.gameObject.SetActive(true);
      reportButtonEmptyPlaceholder.SetActive(false);
      nameField.text = player.slotNumber + ": " + PhotonPlayer.Find(player.photonPlayerId).NickName;
    }

    reportButtonLabel.text = PhotonNetwork.isMasterClient ? "Kick" : "Report";

  }

  internal void UpdateEditToggle()
  {
    editToggle.interactable = PhotonNetwork.isMasterClient;

    if (canEditUpdated)
    {
      editToggle.isOn = desiredCanEdit;
      virtualPlayerManager.SetPlayerCanEdit(player.virtualId, desiredCanEdit);
      canEditUpdated = false;
    }
    else
    {
      editToggle.isOn = player.canEdit;
    }

    if (PhotonNetwork.isMasterClient)
    {
      editToggleTooltip.SetDescription(editToggle.isOn ? editToggleOnMaster : editToggleOffMaster);
    }
    else
    {
      editToggleTooltip.SetDescription(editToggle.isOn ? editToggleOnClient : editToggleOffClient);
    }
  }

  internal void RequestDestroy()
  {
    Destroy(gameObject);
  }
}
