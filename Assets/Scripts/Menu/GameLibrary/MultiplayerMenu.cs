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
using System.Linq;
using UnityEngine;

public class MultiplayerMenu : ThumbnailMenu
{
  [SerializeField] GameObject gameThumbnailPrefab;
  [SerializeField] GameDetail gameDetailPrefab;
  [SerializeField] Texture2D spoofGameTexture;
  [SerializeField] Texture2D newGameTexture;
  [SerializeField] UnityEngine.UI.Button joinCodeButton;
  [SerializeField] UnityEngine.UI.Button refreshButton;
  [SerializeField] GameObject joinMultiplayerPopupBackground;
  [SerializeField] TextPopup joinMultiplayerPopup;

  [SerializeField] MonoBehaviour[] stuffToTempDisable;
  [SerializeField] GameObject[] objectsToTempDisable;

#if USE_PUN

  GameBuilderSceneController scenes;
  LoadingScreen loadingScreen;
  DynamicPopup dynamicPopup;
  GameDetail gameDetail;

  public static string LastJoinedRoomPrefsKey = "LAST_ROOM_TRIED_TO_JOIN";

  // HACK HACK (for debug).
  public static bool debugShortcutToMpJoin = false;

  public enum MultiplayerSort
  {
    Name,
    PlayerCount
  };

  MultiplayerSort multiplayerSort;

  void Awake()
  {
    Util.FindIfNotSet(this, ref scenes);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref dynamicPopup);
  }

  public override void Setup()
  {
    base.Setup();

    joinCodeButton.onClick.AddListener(JoinMultiplayerPopup);
    refreshButton.onClick.AddListener(Refresh);

    joinMultiplayerPopup.confirmWithInputEvent = JoinMultiplayer;
    joinMultiplayerPopup.cancelEvent = CancelMultiplayerPopup;

    searchField.onValueChanged.AddListener((searchString) => SetSearchString(searchString));
    searchClear.onClick.AddListener(() => searchField.text = "");

    SetOpenEvent(CreateThumbnails);
    AddCategory(/* name */ null, /* placeholderText */ "");

    gameDetail = Instantiate(gameDetailPrefab).GetComponent<GameDetail>();
  }

  void Start()
  {
    if (debugShortcutToMpJoin)
    {
      JoinMultiplayerPopup();
      debugShortcutToMpJoin = false;
    }
  }

  IEnumerator OpenRoutine()
  {
    if (!InLobby())
    {
      if (PhotonNetwork.connected)
      {
        PhotonNetwork.Disconnect();
        yield return null;
      }
      PhotonNetwork.autoJoinLobby = true;
      PhotonNetwork.ConnectToBestCloudServer(NetworkingController.GetPhotonGameVersion());
      while (!InLobby())
      {
        // TODO some connecting feedback..
        yield return null;
      }
    }

    Refresh();

    // Keep auto-refreshing until we get SOMETHING
    while (PhotonNetwork.GetRoomList() == null || PhotonNetwork.GetRoomList().Length == 0)
    {
      yield return new WaitForSecondsRealtime(1f);
      Refresh();
    }
  }

  private void Refresh()
  {
    ClearThumbnails();
    CreateThumbnails();
  }

  bool InLobby()
  {
    return PhotonNetwork.connected && !PhotonNetwork.offlineMode && PhotonNetwork.insideLobby;
  }

  public override void Open()
  {
    Util.Log($"Photon state: {PhotonUtil.GetPhotonStateString()}");
    sortText.text = multiplayerSort.ToString();
    base.Open();
    StartCoroutine(OpenRoutine());
  }

  protected override void ChangeSort(Direction direction)
  {
    multiplayerSort = (MultiplayerSort)(1 - (int)multiplayerSort);
    sortText.text = multiplayerSort.ToString();
    UpdateSort();
  }

  bool IsRoomHidden(string roomName)
  {
    // (we can use this to hide rooms known to be broken/incompatible)
    return false;
  }

  protected void CreateThumbnails()
  {
    AddNewGameThumbnail();

    if (!InLobby())
    {
      return;
    }

    foreach (var room in PhotonNetwork.GetRoomList())
    {
      // Some games are broken and crash/disconnect on join..so hide these.
      if (IsRoomHidden(room.Name))
      {
        continue;
      }

      AddRoomThumbnail(room);
    }
  }


  void AddNewGameThumbnail()
  {
    string gameName = "New game";

    GameThumbnail thumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
    thumbnail.SetThumbnail(newGameTexture);
    thumbnail.SetGameSource(GameDetail.GameSource.Local);
    thumbnail.SetName(gameName);
    thumbnail.OnClick = OpenNew;
    thumbnail.GetWriteTime = () => { return System.DateTime.Today; }; // Not relevant
    thumbnail.GetDescription = () => { return "Start a new multiplayer game"; };
    thumbnail.GetPlayerCount = () => { return 0; };
    AddThumbnail(thumbnail);
  }

  void OpenNew()
  {
    dynamicPopup.AskHowToPlayMultiplayer(playOpts =>
    {
      var gameOpts = new GameBuilderApplication.GameOptions { playOptions = playOpts };
      loadingScreen.ShowAndDo(() => sceneController.RestartAndLoadMinimalScene(gameOpts));
    });
  }

  void AddRoomThumbnail(RoomInfo room)
  {
#if USE_PUN
    string gameName = "<no name>";
    Texture2D tnTexture = spoofGameTexture;

    if (room.CustomProperties.ContainsKey(NetworkingController.GameDisplayNameRoomProperty))
    {
      gameName = (string)room.CustomProperties[NetworkingController.GameDisplayNameRoomProperty];
    }

    if (room.CustomProperties.ContainsKey(NetworkingController.ThumbnailZippedJpegRoomProperty))
    {
      try
      {
        byte[] tnBytes = (byte[])room.CustomProperties[NetworkingController.ThumbnailZippedJpegRoomProperty];
        if (tnBytes != null)
        {
          tnTexture = Util.ZippedJpegToTexture2D(tnBytes);
        }
      }
      catch (System.Exception)
      {
        // Wah wah.
      }
    }

    GameThumbnail thumbnail = Instantiate(gameThumbnailPrefab).GetComponent<GameThumbnail>();
    thumbnail.SetThumbnail(tnTexture);
    thumbnail.SetGameSource(GameDetail.GameSource.Multiplayer);
    thumbnail.SetName(gameName);
    thumbnail.OnClick = () => ShowRoomDetails(thumbnail, room.Name);
    thumbnail.GetWriteTime = () => { return System.DateTime.Today; }; // Not relevant
    thumbnail.GetDescription = () => { return "no description"; };
    thumbnail.GetPlayerCount = () => { return room.PlayerCount; };
    AddThumbnail(thumbnail);
#endif
  }

  void ShowRoomDetails(GameThumbnail newThumbnail, string roomCode)
  {
    string detailCopy = $"<b>{newThumbnail.GetName()}</b> - {newThumbnail.GetPlayerCount()} player(s)";
    SelectThumbnail(newThumbnail, (rect) =>
    {
      gameDetail.FitTo(rect);
      gameDetail.OpenSpecial(detailCopy, newThumbnail.GetTexture(), playOpts =>
      {
        // TODO loading screen here?
        scenes.LoadMainSceneAsync(new GameBuilderApplication.GameOptions
        {
          playOptions = new GameBuilderApplication.PlayOptions { isMultiplayer = true },
          joinCode = roomCode
        });
      }, true);
    });
  }

  protected void UpdateSort()
  {
    if (multiplayerSort == MultiplayerSort.Name)
    {
      SetSorting((t1, t2) =>
      {
        return t1.GetName().CompareTo(t2.GetName());
      });
    }
    else
    {
      SetSorting((t1, t2) =>
      {
        return t2.GetPlayerCount().CompareTo(t1.GetPlayerCount());
      });
    }
  }

  public override void Close()
  {
    if (joinMultiplayerPopup.IsActive())
    {
      CancelMultiplayerPopup();
    }
    else
    {
      base.Close();
    }
  }

  public void JoinMultiplayerPopup()
  {
    joinMultiplayerPopup.Activate();
    joinMultiplayerPopupBackground.SetActive(true);
    if (PlayerPrefs.HasKey(LastJoinedRoomPrefsKey))
    {
      joinMultiplayerPopup.SetInputFieldText(PlayerPrefs.GetString(LastJoinedRoomPrefsKey));
    }
    else
    {
#if UNITY_EDITOR
      joinMultiplayerPopup.SetInputFieldText(NetworkingController.GenerateUniqueRoomName());
#endif
    }
  }

  void JoinMultiplayer(string joinCode)
  {
    joinCode = joinCode.Trim('\n', '\r', ' ', '\t');
    Util.Log($"Trying to join room {joinCode}..");
    PlayerPrefs.SetString(LastJoinedRoomPrefsKey, joinCode);
    PlayerPrefs.Save();
    loadingScreen.ShowAndDo(() =>
    {
      sceneController.JoinMultiplayerGameByCode(joinCode);
    });
  }

  public void CancelMultiplayerPopup()
  {
    joinMultiplayerPopupBackground.SetActive(false);

    joinMultiplayerPopup.Deactivate();
  }
#else
  protected override void ChangeSort(Direction direction)
  {
  }
#endif
}
