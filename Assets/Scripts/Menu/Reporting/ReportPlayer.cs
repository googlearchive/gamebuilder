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

public class ReportPlayer : ReportBase
{
  [SerializeField] UnityEngine.UI.Button reportButton;
  [SerializeField] UnityEngine.UI.Button kickButton;
  [SerializeField] TMPro.TextMeshProUGUI reportButtonText;
  [SerializeField] TMPro.TextMeshProUGUI descriptionInputText;
  [SerializeField] TMPro.TextMeshProUGUI questionText;

  DynamicPopup popups;
  NetworkingController networking;
  GameBuilderSceneController scenes;

  bool kicking = false;

  protected override void Awake()
  {
    Util.FindIfNotSet(this, ref networking);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref scenes);

    reportButton.onClick.AddListener(Report);
    kickButton.onClick.AddListener(Kick);
    base.Awake();
  }

  public override void Open(PhotonPlayer reportee, bool kicking = false)
  {
    base.Open(reportee, kicking);
    this.kicking = kicking;
    questionText.text = $"What is {reportee.NickName} doing?";
    reportButtonText.text = kicking ? "KICK AND REPORT" : "REPORT";
    kickButton.gameObject.SetActive(kicking);
  }

  void Kick()
  {
    networking.KickPlayer(reportee);
    Close();
  }

  void Report()
  {
    string description = descriptionInputText.text;
    if (description.IsNullOrEmpty())
    {
      return;
    }

    if (!PhotonNetwork.isMasterClient && reportee != PhotonNetwork.masterClient)
    {
      networking.SendReportToMasterClient(reportee.NickName, description);
    }

#if USE_PUN
    object steamId = "(N/A)";
    reportee.CustomProperties.TryGetValue((object)NetworkingController.SteamIdPlayerProperty, out steamId);
#endif

    // Feedback
    if (reportee != PhotonNetwork.masterClient)
    {
      popups.Show("Thank you for reporting the player. We will review your message and take appropriate action if necessary. The Game Master has also been notified and may kick the player from the game.", "OK", () => { }, 800f);
    }
    else
    {
      popups.Show(new DynamicPopup.Popup
      {
        fullWidthButtons = true,
        textWrapWidth = 800f,
        getMessage = () => $"Thank you for reporting the player. We will review your message and take appropriate action if necessary. Would you like to leave the current game or continue playing?",
        buttons = new List<PopupButton.Params>() {
          new PopupButton.Params{ getLabel = () => "Leave Game", onClick = () => scenes.LoadSplashScreen()},
          new PopupButton.Params { getLabel = () => "Continue Playing", onClick = () => { } }
        }
      });
    }

    if (kicking) networking.KickPlayer(reportee);
    Close();
  }
}
