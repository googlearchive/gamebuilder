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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameBuilder;
using System.Text;
using UNET = UnityEngine.Networking;

// Based off of Photon's utility script.
// TODO probably should rename this to just MainController...but also cordone off some of the networking-specific stuff into a new utility.
public class NetworkingController : Photon.PunBehaviour
{
  // From experiments, it seems like 490kb for that one RPC was a guaranteed
  // disconnect. So, let's stay under 400kb to be safe.
  static int MaximumPlayerInitKbs = 400;

  public static string SteamIdPlayerProperty = "SteamID";
  public static string SteamProfileNamePlayerProperty = "SteamProfileName";

  public static string GameDisplayNameRoomProperty = "gameName";
  public static string ThumbnailZippedJpegRoomProperty = "thumbnailZippedJpeg";
  static string[] PropertiesToShowInLobby = new string[] { GameDisplayNameRoomProperty, ThumbnailZippedJpegRoomProperty };

  static string BadJoinCodeMessage = "Sorry, it looks like you typed an invalid join code. It should look something like 1-123456.";
  static char JoinCodeSeparator = '-';

  public static string LastJoinedRoomPrefKey = "LAST_ROOM_JOINED";

  public interface LocalUserObjects
  {
    void Initialize(UserBody localBodyInstance);
  }


  // This prefab will be spawned for both the local and remote players.
  public UserBody userBodyPrefab;

  // This will be spawned when the world is ready for the player.
  // If it implements LocalObjects, Initialize will be called.
  public GameObject localPrefab;

  public GameObject disableOnJoinRoom;

  public VoosEngine voosEngine;
  public BehaviorSystem behaviorSystem;
  public SaveLoadController saveLoad;
  public GameBundleLibrary gameBundleLibrary;
  public GameBuilderSceneController scenes;
  public GameBuilderStage stage;
  public AutoSaveController autosaves;

  [SerializeField] AssetCache assetCache;
  TerrainManager terrain;
  BuiltinPrefabLibrary builtinPrefabLibrary;
  SojoSystem sojoSystem;

  DynamicPopup popups;
  WorkshopAssetSource workshop;

  private GameObject localUserInstance;

  public enum Mode
  {
    Online,
    Offline
  }

  public Mode mode = Mode.Online;

  string lastRoomNameAttempted = null;
  string lastRoomJoined = null;

  bool wasMasterWhenConnected = false;
  int numOthersWhenConnected = 0;
  string roomNameWhenConnected = null;

  bool receivedPlayerInitPayload = false;

  bool didSwitchMaps = false;

  float onlineSeconds = 0f;

  HashSet<string> kickedPlayerNickNames = new HashSet<string>();

  // The player init RPC is pretty big. So avoid sending it too often, if a
  // bunch of players log in at the same time, for example (which can def happen
  // on map switch!). So have a queue, and pop one off per second.
  Queue<PhotonPlayer> playerInitQueue = new Queue<PhotonPlayer>();

  LoadingScreen loadingScreen;

  // Internal state machine
  enum State
  {
    Uninited,
    Playing,
    SwitchingScene,
    Rejoining
  }
  State state = State.Uninited;

  [System.Serializable]
  struct PlayerLeftMessage
  {
    public int id;
    public string nickName;
  }

  public bool GetIsInMultiplayer()
  {
    return mode == Mode.Online && PhotonUtil.ActuallyConnected();
  }

  public string GetJoinedRoomName()
  {
    return PhotonNetwork.room.name;
  }

  void Awake()
  {
    Util.Log($"NetworkingController.Awake t = {Time.realtimeSinceStartup}");

    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref saveLoad);
    Util.FindIfNotSet(this, ref scenes);
    Util.FindIfNotSet(this, ref gameBundleLibrary);
    Util.FindIfNotSet(this, ref builtinPrefabLibrary);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref stage);
    Util.FindIfNotSet(this, ref terrain);
    Util.FindIfNotSet(this, ref assetCache);
    Util.FindIfNotSet(this, ref autosaves);
    Util.FindIfNotSet(this, ref sojoSystem);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref workshop);

    // Maybe not the best place..? Should be close to OnApplicationFocus below.
    Application.targetFrameRate = -1;
  }

  void OnApplicationFocus(bool isFocused)
  {
    if (!isFocused)
    {
      Application.targetFrameRate = 30;
    }
    else
    {
      Application.targetFrameRate = -1;
    }
  }

  public void OnEnable()
  {
    scenes.OnBeforeReloadMainScene += OnBeforeReloadMainScene;
  }

  public void OnDisable()
  {
    scenes.OnBeforeReloadMainScene -= OnBeforeReloadMainScene;
  }

  string GetSteamID()
  {
#if USE_STEAMWORKS
    if (SteamManager.Initialized)
    {
      return Steamworks.SteamUser.GetSteamID().m_SteamID.ToString();
    }
    else
    {
      return null;
    }
#else
    return null;
#endif
  }

  void SetupPlayerProperties()
  {
#if USE_PUN
    var props = (ExitGames.Client.Photon.Hashtable)PhotonNetwork.player.CustomProperties;

    string nickName = null;

#if USE_STEAMWORKS
    if (SteamManager.Initialized)
    {
      nickName = Steamworks.SteamFriends.GetPersonaName();
      props[(object)SteamIdPlayerProperty] = GetSteamID();

      PlayerPrefs.SetString("PlayerNickName", nickName);
      PlayerPrefs.Save();
    }
    else
#endif
    {
      nickName = PlayerPrefs.GetString("PlayerNickName");
    }

    if (nickName.IsNullOrEmpty())
    {
      throw new System.Exception("No nick name set. Play multiplayer at least once while signed in to Steam.");
    }

    props[(object)SteamProfileNamePlayerProperty] = nickName;  // This may be redundant given we set the playerName/NickName.
    PhotonNetwork.player.SetCustomProperties(props);
    PhotonNetwork.playerName = nickName;
#endif
  }

  bool IsUserBanned(string steamId, out System.DateTime lastDate)
  {
    // (You can use this to implement user banning)
    lastDate = System.DateTime.Now;
    return false;
  }

  bool IsPlayerValid(PhotonPlayer p)
  {
    return p.ID >= 0 && PhotonPlayer.Find(p.ID) == p;
  }

  IEnumerator PumpPlayerInitCoroutine()
  {
    while (true)
    {
      if (this.gameObject == null) break;

      while (true)
      {
        if (playerInitQueue.Count == 0) break;
        PhotonPlayer p = playerInitQueue.Dequeue();
        if (!IsPlayerValid(p)) continue;
        StartCoroutine(InitializeNewPlayer(p));
        break;
      }

      yield return new WaitForSecondsRealtime(1f);
    }
  }

  void Start()
  {
#if USE_PUN
    StartCoroutine(PumpPlayerInitCoroutine());

    var diag = GetComponent<PhotonStatsGui>();
    if (diag != null)
    {
      diag.enabled = false;
    }

    try
    {
      // Don't destroy things created by clients. We will need to clean up the
      // player ghost object only. Even if we're not the master client, we still
      // need this. Because if the master quits, we might become the master, and
      // we need to prevent Photon from destroying all objects!
      if (!PhotonNetwork.inRoom)
      {
        PhotonNetwork.autoCleanUpPlayerObjects = false;
      }
      else
      {
        // Setting this causes an error if we're in a room, but we should make
        // sure it's false still.
        Debug.Assert(PhotonNetwork.autoCleanUpPlayerObjects == false);
      }

      PhotonNetwork.autoJoinLobby = false;
      bool isMultiplayer = GameBuilderApplication.CurrentGameOptions.playOptions.isMultiplayer;

      System.DateTime banLastDate;
      if (isMultiplayer && IsUserBanned(GetSteamID(), out banLastDate))
      {
        popups.Show($"You have been temporarily banned from multiplayer games for reported inappropriate or abusive behavior. You will be able to play again after {banLastDate.ToString("MMMM dd, yyyy")}.", "Back", () =>
        {
          scenes.LoadSplashScreen();
        }, 800f);
        return;
      }

      if (isMultiplayer)
      {
        Util.Log($"Multiplayer!");
        SetupPlayerProperties();
        mode = Mode.Online;
        PhotonNetwork.offlineMode = false;
      }
      else
      {
      }

      if (PhotonNetwork.connected && PhotonNetwork.inRoom)
      {
        // We are still connected and in a room. This is a map switch.
        mode = Mode.Online;
        Util.Log($"StayOnline mode. Pretending we just joined the room.");
        didSwitchMaps = true;
        OnJoinedRoom();
      }
      else if (PhotonNetwork.connected && PhotonNetwork.insideLobby && isMultiplayer)
      {
        // Joining or creating.

        string roomToJoin = GameBuilderApplication.CurrentGameOptions.joinCode;
        if (!roomToJoin.IsNullOrEmpty())
        {
          // We're joining a room from the lobby
          Util.Log($"Trying to join room {roomToJoin}..");
          // Try to join existing room
          PhotonNetwork.JoinRoom(roomToJoin.ToLower());
        }
        else
        {
          // We're creating a new game, and happen to be in a lobby already.
          TryCreateRoom();
        }
      }
      else
      {
        switch (mode)
        {
          case Mode.Online:
            string gameVersion = GetPhotonGameVersion();
            // If we're trying to join a game, make sure we connect to their region.
            string joinCode = GameBuilderApplication.CurrentGameOptions.joinCode;
            if (joinCode == "*")
            {
              // TEMP join random visible game in best region
              PhotonNetwork.ConnectToBestCloudServer(gameVersion);
            }
            else if (joinCode != null)
            {
              string regionCodeStr = joinCode.Split(JoinCodeSeparator)[0];
              try
              {
                CloudRegionCode regionCode = (CloudRegionCode)System.Int32.Parse(regionCodeStr);
                PhotonNetwork.ConnectToRegion(regionCode, gameVersion);
              }
              catch (System.OverflowException)
              {
                OnInvalidJoinCodeRegion();
              }
            }
            else
            {
              // Ok we're starting a new game, so just connect to the best.
              PhotonNetwork.ConnectToBestCloudServer(gameVersion);
            }
            break;
          case Mode.Offline:
            if (PhotonUtil.ActuallyConnected())
            {
              PhotonNetwork.Disconnect();
            }
            DestroyImmediate(GetComponent<ExitGames.UtilityScripts.PlayerRoomIndexing>());
            Util.Log($"Starting offline mode, t = {Time.realtimeSinceStartup}");
            PhotonNetwork.offlineMode = true;
            break;
        }
      }
    }
    catch (System.FormatException e)
    {
      OnFatalError(BadJoinCodeMessage);
    }
    catch (System.Exception e)
    {
      OnFatalError(e.ToString());
    }
#else

    // Non-PUN route
    mode = Mode.Offline;
    StartCoroutine(NonPunStartRoutine());

#endif
  }

  IEnumerator NonPunStartRoutine()
  {
    // Wait a few frames to be async...
    yield return null;
    yield return null;
    yield return null;
    MasterClientInit();
  }

  void OnFatalError(string error, string errorForAnalytics = null)
  {
    Util.LogError(error);

    popups.Show(error, "OK", () =>
    {
      scenes.LoadSplashScreen();
    });
  }

#if USE_PUN
  static int AlreadyJoinedCode = 32746;

  public override void OnPhotonJoinRoomFailed(object[] codeAndMsg)
  {
    if (codeAndMsg[0].ToString() == AlreadyJoinedCode.ToString())
    {
      Util.Log($"Ignoring 'already joined..' error, since Photon will just auto retry anyway.");
      return;
    }
    string roomToJoin = GameBuilderApplication.CurrentGameOptions.joinCode;
    string displayError = $"Sorry, couldn't join room '{roomToJoin}' :(\nAre you sure you typed it correctly?\nMaybe one of you is on EXPERIMENTAL and the other is not?\nError: {codeAndMsg[1]} (code {codeAndMsg[0]})";
    OnFatalError(displayError, $"OnPhotonJoinRoomFailed. Error: {codeAndMsg[1]} (code {codeAndMsg[0]})");

    // Clear the last joined room, if it's the same one
    if (PlayerPrefs.GetString(LastJoinedRoomPrefKey, "") == roomToJoin)
    {
      PlayerPrefs.DeleteKey(LastJoinedRoomPrefKey);
      PlayerPrefs.Save();
    }
  }

  public static string GenerateUniqueRoomName()
  {
#if UNITY_EDITOR
    return $"dev-{System.Net.Dns.GetHostName().ToLowerInvariant()}";
#else
    return ((int)UnityEngine.Random.Range(0, 999999)).ToString();
#endif
  }

  public void TryCreateRoom()
  {
    var props = new ExitGames.Client.Photon.Hashtable();
    props.Add(GameDisplayNameRoomProperty, GameBuilderApplication.CurrentGameOptions.displayName);
    RoomOptions roomOptions = new RoomOptions() { MaxPlayers = (byte)GetMaxPlayers(), IsVisible = false, CustomRoomProperties = props, CustomRoomPropertiesForLobby = PropertiesToShowInLobby };

    // Bake the region code into the code so others know..Ideally we'd let you
    // pick the region, in case your friends are far..
    int regionNumber = (int)PhotonNetwork.CloudRegion;
    string roomName = $"{regionNumber}{JoinCodeSeparator}{GenerateUniqueRoomName()}";
    Util.Log($"Trying to create room {roomName}, game name = {GameBuilderApplication.CurrentGameOptions.displayName}");

    PhotonNetwork.CreateRoom(roomName, roomOptions, null);
    lastRoomNameAttempted = roomName;
  }

  public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
  {
    Util.LogWarning($"Failed to create room '{lastRoomNameAttempted}' :( Error: {codeAndMsg.ToString()}. Trying again..");
    TryCreateRoom();
  }

  public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
  {
    TryCreateRoom();
  }

  public override void OnConnectedToMaster()
  {
    string roomToJoin = GameBuilderApplication.CurrentGameOptions.joinCode;
    if (roomToJoin == "*")
    {
      PhotonNetwork.JoinLobby();
    }
    else if (roomToJoin != null)
    {
      Util.Log($"Trying to join room {roomToJoin}..");
      // Try to join existing room
      PhotonNetwork.JoinRoom(roomToJoin.ToLower());
    }
    else
    {
      TryCreateRoom();
    }
  }

  public override void OnFailedToConnectToPhoton(DisconnectCause cause)
  {
    OnFatalError($"Woops! Could not connect to the master server. Cause:\n{cause.ToString()}");
  }

  IEnumerator OnJoinedRoomCoroutine()
  {
    // Always wait 1 frame. This is because Photon, in offline mode, can calls
    // this synchronously within TryCreateRoom - bad. So, we'll deal with it.
    yield return null;

    // For some reason..this happens sometimes.
    while (PhotonNetwork.room == null) yield return null;

    lastRoomJoined = PhotonNetwork.room.Name;
    Debug.Log($"Joined room '{lastRoomJoined}'");

    PlayerPrefs.SetString(LastJoinedRoomPrefKey, lastRoomJoined);
    PlayerPrefs.Save();

    if (PhotonNetwork.isMasterClient)
    {
      if (!PhotonNetwork.offlineMode)
      {

        // Setup room properties for the in-lobby game browser.
        var gameParams = GameBuilderApplication.CurrentGameOptions;
        var props = (ExitGames.Client.Photon.Hashtable)PhotonNetwork.room.CustomProperties;
        props[(object)GameDisplayNameRoomProperty] = (object)gameParams.displayName;
        props[(object)ThumbnailZippedJpegRoomProperty] = (object)gameParams.thumbnailZippedJpegBytes;
        PhotonNetwork.room.SetCustomProperties(props);

        PhotonNetwork.room.IsVisible = gameParams.playOptions.startAsPublic;
      }
      MasterClientInit();

      didSwitchMaps = false;
    }
    else
    {
      // This should never happen in offline mode...but just in case
      if (!PhotonNetwork.offlineMode)
      {
      }
      StartCoroutine(NonMasterClientInit());
    }
  }

  public override void OnJoinedRoom()
  {
    StartCoroutine(OnJoinedRoomCoroutine());
  }
#endif

  private void OnDestroy()
  {
    // Important to do this for scene reload.
    switch (state)
    {
      case State.SwitchingScene:
        // Do NOT disconnect or leave the room.
        return;
      default:
        PhotonNetwork.Disconnect();
        return;
    }
  }

  public string lastLoadedBundleId { get; private set; }

  void MasterClientInit()
  {
    SaveGameToLoad saveGameToLoad = FindObjectOfType<SaveGameToLoad>();
    string bundleId = GameBuilderApplication.CurrentGameOptions.bundleIdToLoad;

    if (saveGameToLoad)
    {
      Debug.Log("Loading saved game!");
      saveLoad.Load(saveGameToLoad.saved, saveGameToLoad.voosFilePath);
      GameObject.Destroy(saveGameToLoad.gameObject);
    }
    else if (!bundleId.IsNullOrEmpty())
    {
      Debug.Log($"Loading game bundle {bundleId}");
      string voosPath = gameBundleLibrary.GetBundle(bundleId).GetVoosPath();
      saveLoad.Load(SaveLoadController.ReadSaveGame(voosPath), voosPath);
#if !USE_STEAMWORKS
      workshop.Load(gameBundleLibrary.GetBundle(bundleId).GetAssetsPath());
#endif
      lastLoadedBundleId = bundleId;
    }
    else
    {
      SaveLoadController.SaveGame save = SaveLoadController.ReadSaveGame(GameBuilderSceneController.GetMinimalScenePath(mode == Mode.Online));
      saveLoad.Load(save);
    }

    using (Util.Profile("SpawnLocalobjects"))
      SpawnLocalObjects();
    StartCoroutine(LoadingSequence());
  }

#if USE_PUN
  IEnumerator NonMasterClientInit()
  {
    // Wait for VoosEngine to be init'd.
    while (!receivedPlayerInitPayload)
    {
      Util.Log($"Waiting for network initialization from master..");
      yield return new WaitForSecondsRealtime(1f);
    }

    if (!PhotonNetwork.connected || !PhotonNetwork.inRoom)
    {
      // We got disconnected after the init message...very possible if it's too
      // big. The player should be seeing the popup.
      yield break;
    }

    SpawnLocalObjects();
    StartCoroutine(LoadingSequence());
  }
#endif

  bool IsLoadingResources()
  {
    return assetCache.GetNumActiveDownloads() > 0
    || !terrain.HasReachedSteadyState();
  }

  IEnumerator LoadingSequence()
  {
    float t0 = Time.realtimeSinceStartup;
    loadingScreen.Show();
    yield return new WaitForSecondsRealtime(0.5f);

    System.Action startPlaying = () =>
    {
      float t1 = Time.realtimeSinceStartup;
      loadingScreen.FadeAndHide();
      voosEngine.SetIsRunning(true);

      GameObject.FindObjectOfType<UserMain>().FadeInAudioAfterLoading();
    };

    if (!IsLoadingResources())
    {
      startPlaying();
      yield break;
    }

    loadingScreen.SetCancelButton("Start Playing Anyway", () => startPlaying());

    int totalDownloads = 1;
    int totalChunks = 1;

    while (IsLoadingResources())
    {
      // We compute the # of total downloads in this weird and very approximate way,
      // but it's not like progress bars ever tell the truth anyway.
      // This is necessary because not all downloads may be immediately queueud.
      int downloadsRemaining = assetCache.GetNumActiveDownloads();
      totalDownloads = Mathf.Max(downloadsRemaining, totalDownloads);

      int chunksRemaining = terrain.GetNumImportantChunksRemaining();
      totalChunks = Mathf.Max(chunksRemaining, totalChunks);

      float progress = 1 - ((downloadsRemaining + chunksRemaining) * 1f / (totalDownloads + totalChunks));

      string text = "";

      if (downloadsRemaining > 0)
      {
        text += $"Loading models, {downloadsRemaining} remaining.";
      }

      if (chunksRemaining > 0)
      {
        text += $"\nGenerating terrain, {chunksRemaining} chunks remaining.";
      }

      loadingScreen.SetProgress(progress);
      loadingScreen.SetStatusText(text);

      yield return new WaitForSecondsRealtime(0.1f);
    }

    // One last little bit to reduce chances of people falling through the
    // ground..
    yield return new WaitForSecondsRealtime(0.5f);
    startPlaying();
  }

  private void SpawnLocalObjects()
  {
    using (Util.Profile("disableOnJoinRoom"))
    {
      if (disableOnJoinRoom != null)
      {
        disableOnJoinRoom.SetActive(false);
      }
    }

    using (Util.Profile("localUserBody"))
    {
      Debug.Assert(localUserInstance == null);
      Vector3 pos = new Vector3(0, 1.5f, 0);
#if USE_PUN
      localUserInstance = PhotonNetwork.Instantiate(userBodyPrefab.name, pos, Quaternion.identity, 0);
#else
      localUserInstance = Instantiate(userBodyPrefab.gameObject, pos, Quaternion.identity);
#endif
      UserBody localUserBody = localUserInstance.GetComponent<UserBody>();
      localUserBody.Setup(true);

      using (Util.Profile("localPrefab"))
      {
        if (localPrefab != null)
        {
          var localInstance = GameObject.Instantiate(localPrefab, null);
          LocalUserObjects localInterface = localInstance.GetComponent<LocalUserObjects>();
          if (localInterface != null)
          {
            localInterface.Initialize(localUserBody);
          }
        }
      }
    }
  }

  public string GetPlayerSteamID(PhotonPlayer player)
  {
    object steamId = "(N/A)";
#if USE_PUN
    player.CustomProperties.TryGetValue((object)NetworkingController.SteamIdPlayerProperty, out steamId);
#endif
    return (string)steamId;
  }

#if USE_PUN
  public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
  {
    // HACK HACK. Only because CMS is tied to player panels, when it really should be global.
    var cms = FindObjectOfType<HudNotifications>();
    if (cms != null)
    {
      Util.Log($"'{newPlayer.NickName}' joined! SteamID: {GetPlayerSteamID(newPlayer)}");
      cms.AddMessage($"'{newPlayer.NickName}' joined!");
    }

    if (PhotonNetwork.isMasterClient)
    {
      System.DateTime banLastDate;
      if (kickedPlayerNickNames.Contains(newPlayer.NickName)
        || IsUserBanned(GetPlayerSteamID(newPlayer), out banLastDate)
        )
      {
        // RE-KICK!
        KickPlayer(newPlayer);
      }
      else
      {
        // NOTE: Ideally we'd be leveraging the rate-limit system and enqueueing
        // this player into playerInitQueue. However, that is currently broken.
        // Upon a new player joining, Photon resets all ReliableDeltaCompressed
        // views so the new player gets the full view data, then it can resume
        // sending deltas only. However, it does this IMMEDIATELY. So if we
        // don't send the init payload ALSO immediately, the new player won't
        // create those views, and the full view data messages will just get
        // dropped. This means, subsequent delta-messages will get dropped, and
        // RDC views are effectively non-functional for the new player. Bad.
        StartCoroutine(InitializeNewPlayer(newPlayer));
      }
    }
  }

  public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
  {
    Util.Log($"Player {otherPlayer.ID} disconnected.");

    var cms = FindObjectOfType<HudNotifications>();
    if (cms != null)
    {
      cms.AddMessage($"'{otherPlayer.NickName}' left");
    }

    if (!PhotonNetwork.isMasterClient)
    {
      return;
    }

    // Inefficient and global, but whatever...
    foreach (UserBody body in FindObjectsOfType<UserBody>())
    {
      if (body.GetComponent<PhotonView>().OwnerActorNr == otherPlayer.ID)
      {
        PhotonNetwork.Destroy(body.gameObject);
      }
    }

    // Reclaim all actors owned by this player. A bit surprised photon doesn't
    // do this for us..
    foreach (VoosActor actor in voosEngine.EnumerateActors())
    {
      if (actor.reliablePhotonView != null && actor.reliablePhotonView.OwnerActorNr == otherPlayer.ID)
      {
        // Why not call actor.RequestOwnership here? That would result in
        // nothing happening, because the other player is definitely gone. But
        // calling view.RequestOwnership() directly will go through the normal
        // Photon flow, bypassing all our logic, and Photon should just grant us
        // ownership.
        actor.reliablePhotonView.RequestOwnership();
      }
    }

    // Broadcast a message saying the player disconnected.
    PlayerLeftMessage playerLeftMessage = new PlayerLeftMessage();
    playerLeftMessage.id = otherPlayer.ID;
    playerLeftMessage.nickName = otherPlayer.NickName;
    voosEngine.BroadcastMessage("PlayerLeft", playerLeftMessage);
  }

  void OnInvalidJoinCodeRegion()
  {
    int maxRegionVal = 0;
    foreach (CloudRegionCode code in Enum.GetValues(typeof(CloudRegionCode)))
    {
      maxRegionVal = Math.Max((int)code, maxRegionVal);
    }
    OnFatalError($"Sorry, it looks like you typed an invalid join code.\nThe first number before the hyphen should be between 0 and {maxRegionVal}.");
  }

  public override void OnConnectionFail(DisconnectCause cause)
  {
    if (cause == DisconnectCause.InvalidRegion)
    {
      OnInvalidJoinCodeRegion();
    }
    else
    {
      voosEngine.HackyForceSetIsRunning(false);

      if (Time.unscaledTime > 10f)
      {
        autosaves.SetPaused(true);
        autosaves.TriggerAutosave(autosaveBundleId =>
        {
          if (numOthersWhenConnected == 0)
          {
            string userMessage = $"Woops! You were disconnected. Reason:\n{cause.ToString()}\nWe've auto-saved the project, so you can recover it and restart the multiplayer session.";
            Util.LogError(userMessage);
            popups.ShowTwoButtons(userMessage,
              "Recover Autosave",
              () =>
              {
                scenes.RestartAndLoadLibraryBundle(gameBundleLibrary.GetBundleEntry(autosaveBundleId), new GameBuilderApplication.PlayOptions());
              },
              "Go to Main Menu",
              () => scenes.LoadSplashScreen(),
              800f
            );
          }
          else
          {
            string userMessage = $"Woops! You were disconnected. Reason:\n{cause.ToString()}. Please reconnect.\n<size=80%><color=#888888>NOTE: If the multiplayer game is gone, you can recover the autosave in your Game Library.</color></size>";
            Util.LogError(userMessage);
            popups.ShowTwoButtons(
              userMessage,
              $"Reconnect to {roomNameWhenConnected}",
              () => scenes.JoinMultiplayerGameByCode(roomNameWhenConnected),
              "Go to Main Menu",
              () => scenes.LoadSplashScreen(),
              800f);
          }
        });
      }
      else
      {
        string userMessage = $"Woops! You were disconnected. Reason:\n{cause.ToString()}.";
        Util.LogError(userMessage);
        popups.Show(userMessage, "Back to Main Menu", () =>
        {
          scenes.LoadSplashScreen();
        });
      }
    }
  }
#endif

  public void KickPlayer(PhotonPlayer player)
  {
#if USE_PUN
    // Courtesy RPC.
    photonView.RPC("KickRPC", player);
    PhotonNetwork.CloseConnection(player);
    kickedPlayerNickNames.Add(player.NickName);
#endif
  }

  [PunRPC]
  void KickRPC()
  {
    PhotonNetwork.Disconnect();
    // Always go back to main menu.
    popups.Show("You were kicked.", "Back to Main Menu", () =>
    {
      scenes.LoadSplashScreen();
    });
  }

  [PunRPC]
  void JoinRoomRPC(string joinCode)
  {
    // If we don't explicitly leave, there's a chance the room will still think
    // we're in when we try to reconnect..thus saying "you're already in here!!"
    PhotonNetwork.LeaveRoom();

    // Trigger main scene reload. This will end up reconnecting to the region server, which is a bit wasteful, but OK.
    scenes.LoadMainSceneAsync(new GameBuilderApplication.GameOptions { playOptions = new GameBuilderApplication.PlayOptions { isMultiplayer = true }, joinCode = joinCode });
  }

  void OnBeforeReloadMainScene()
  {
    if (mode == Mode.Online && PhotonNetwork.connected)
    {
      // If the master client is loading a new map and we're in multiplayer,
      // just always keep the room and consider this a map change for the room.

      string newJoinCode = GameBuilderApplication.CurrentGameOptions.joinCode;
      bool tryingToJoinNewRoom = !newJoinCode.IsNullOrEmpty() && newJoinCode != PhotonNetwork.room.Name;
      bool keepRoom = PhotonNetwork.isMasterClient && !tryingToJoinNewRoom;

      if (!keepRoom)
      {
        Util.Log("Disconnecting, and probably reconnecting into a new game.");
        PhotonNetwork.Disconnect();
      }
      else
      {
        Util.Log("Staying connected, just switching scenes. Telling other players to reconnect to the same room..");
        photonView.RPC("JoinRoomRPC", PhotonTargets.Others, GetJoinedRoomName());

        // This DestroyAll is crucial. If we don't call it, the UserBody's of
        // non-master players seem to stick around in a weird way..where the
        // master doesn't see them, but new players (and reconnects) do..
        PhotonNetwork.DestroyAll();

        state = State.SwitchingScene;
      }
    }
  }

  void Update()
  {
    if (PhotonNetwork.connected)
    {
      if (PhotonNetwork.isMasterClient && !wasMasterWhenConnected && onlineSeconds > 10f)
      {
        popups.Show($"You are now the master client! The previous master was disconnected. If you keep the game running, they can probably reconnect and resume normally.", "Continue", null, 800f);
      }
      wasMasterWhenConnected = PhotonNetwork.isMasterClient;
      numOthersWhenConnected = PhotonNetwork.otherPlayers.Length;
      roomNameWhenConnected = PhotonNetwork.room?.Name;
    }

    if (PhotonUtil.ActuallyConnected())
    {
      onlineSeconds += Time.unscaledDeltaTime;
    }

    // TEMP TEMP
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F2))
    {
      PhotonNetwork.room.IsVisible = !PhotonNetwork.room.IsVisible;
      Util.Log($"toggled room visible to {PhotonNetwork.room.IsVisible}");
    }
  }

  public int GetNumOtherPlayers()
  {
    return PhotonNetwork.otherPlayers.Length;
  }

  public int GetMaxPlayers()
  {
    return 8;
  }

  // TODO TODO umm is this the same as a save game? Should part of that code be shared?
  [System.Serializable]
  struct NewPlayerInitPayload
  {
    // Hmm...I wonder if we can make this a hash of all the sub-parts' versions.
    // 9 is first version with diggable ground.
    public static int CurrentVersion = 11;
    public int version;
    public Behaviors.Database.Jsonable behaviorDatabase;
    public GameBuilderStage.Persisted stage;
    public SojoDatabase.Saved sojoDatabase;
    public TerrainManager.Metadata terrainMeta;
  }

  public static string GetPhotonGameVersion()
  {
    // I've thought about just using the build commit here...but that would
    // preclude the pretty useful case of testing by connecting to a build with
    // an editor.
    return NewPlayerInitPayload.CurrentVersion.ToString()
    + "."
    + VoosActor.CurrentVersionNumber.ToString()
    + "."
    + TerrainSystem.serialization_version.ToString()
    + "."
    + TerrainManager.NetworkingVersion.ToString();
  }

  public static void LogJsonSizeStats<T>(T jsonable)
  {
    string json = JsonUtility.ToJson(jsonable);
    byte[] bytes = Encoding.UTF8.GetBytes(json);
    byte[] zipped = Util.GZip(bytes);
    Util.Log($"{typeof(T).FullName}: {bytes.Length / 1024} kb raw, {zipped.Length / 1024} kb zipped");
  }

  public void LogInitPlayerPayloadStats()
  {
    NewPlayerInitPayload payload;
    payload.version = NewPlayerInitPayload.CurrentVersion;
    payload.behaviorDatabase = behaviorSystem.SaveDatabase();
    payload.sojoDatabase = sojoSystem.SaveDatabase();
    payload.terrainMeta = terrain.GetMetadata();
    payload.stage = stage.Save();

    // Garbage-happy size profiling
    LogJsonSizeStats(payload.behaviorDatabase);
    LogJsonSizeStats(payload.sojoDatabase);
    LogJsonSizeStats(payload.terrainMeta);
    LogJsonSizeStats(payload.stage);

    byte[] terrainBytes = terrain.SerializeTerrainV2();
    Util.Log($"terrain is {terrainBytes.Length / 1024} zipped");
  }

  byte[] VOOS_INIT_BYTES_REUSED = new byte[MaximumPlayerInitKbs * 1024];

  public byte[] GetVoosInitBytes()
  {
    var voosBuffer = VOOS_INIT_BYTES_REUSED;
    Array.Clear(voosBuffer, 0, voosBuffer.Length);
    var writer = new UNET.NetworkWriter(voosBuffer);
    voosEngine.SerializePlayerInitPayloadV2(writer);
    return Util.GZip(writer.ToArray());
  }

  IEnumerator InitializeNewPlayer(PhotonPlayer newPlayer)
  {
    NewPlayerInitPayload payload;
    payload.version = NewPlayerInitPayload.CurrentVersion;
    payload.behaviorDatabase = behaviorSystem.SaveDatabase();
    payload.sojoDatabase = sojoSystem.SaveDatabase();
    payload.terrainMeta = terrain.GetMetadata();
    payload.stage = stage.Save();

    string metaJson = JsonUtility.ToJson(payload);
    byte[] zippedMetaBytes = Util.GZip(Encoding.UTF8.GetBytes(metaJson));
    byte[] zippedVoosBytes = GetVoosInitBytes();
    byte[] terrainV2Bytes = terrain.SerializeTerrainV2();

    if (((zippedMetaBytes.Length + zippedVoosBytes.Length + terrainV2Bytes.Length) / 1024) > MaximumPlayerInitKbs)
    {
      throw new System.Exception($"Sorry, your game has become too large for multiplayer. We recommend loading it in single player and removing some unnecessary actors, assets, or cards. If you need more help, please post to the Steam discussion forums.");
    }

    Util.Log($"Sending {voosEngine.GetNumActors()} actors and to new player {newPlayer.ID}, zippedMetaBytes: {zippedMetaBytes.Length / 1024} KB, zippedVoosBytes: {zippedVoosBytes.Length / 1024} KB");
    photonView.RPC("InitNewPlayerRPC", newPlayer, zippedMetaBytes, zippedVoosBytes);

    // Send terrain on its own. Avoid the JSON stuff.
    Util.Log($"Sending terrain init msg, {terrainV2Bytes.Length / 1024} kb zipped");
    photonView.RPC("InitNewPlayerTerrainRPC", newPlayer, terrainV2Bytes);

    // IMPORTANT: Also tell ALL players that, after the prior RPCs, the new
    // player should be ready to participate normally. This will trigger
    // everyone to, for example, broadcast fresh snapshots for actors they own
    // to the new player. This was done anyway, by Photon's builtin behavior,
    // but because we manage actor views on our own, the new player wasn't ready
    // to receive those yet. Wasteful, yes, but we need this.
    yield return new WaitForSecondsRealtime(3f);
    photonView.RPC("NewPlayerWasInitializedRPC", PhotonTargets.AllViaServer);

    // NOTE FOR FUTURE: I think the real solution here is to stop relying on
    // Photon's RDC at all. It's not efficient for us (we can do change
    // detection at the setter level..), and having PhotonView coupled to
    // VoosActor's life time is unnecessarily restrictive. Ideally, all we would
    // do is this: VoosEngine would not be allowed to run on the new client
    // until all other players have sent over their local actors' full state.
    // But while VOOS is paused, we still can receive delta updates via RPC.
    // Then when we get all state from all other players, we can start running
    // like normal. This means we don't waste bandwidth by sending all actor
    // state TWICE to the same client, as we do now.

  }

  [PunRPC]
  void NewPlayerWasInitializedRPC()
  {
    // See comment at RPC call site.
    foreach (var actor in voosEngine.EnumerateActors())
    {
      if (actor.reliablePhotonView != null)
      {
        actor.reliablePhotonView.ForceResendToAllPlayers();
      }
    }
  }

  TerrainManager.Metadata lastReceivedTerrainMeta;

  [PunRPC]
  void InitNewPlayerRPC(byte[] zippedMetaBytes, byte[] zippedVoosBytes)
  {
    Util.Log($"InitNewPlayerRPC, {zippedMetaBytes.Length / 1024} KB zippedMetaBytes, {zippedVoosBytes.Length / 1024} KB zippedVoosBytes");
    byte[] unzippedMetaBytes = Util.UnGZip(zippedMetaBytes);
    string payloadJson = Encoding.UTF8.GetString(unzippedMetaBytes, 0, unzippedMetaBytes.Length);
    NewPlayerInitPayload payload = JsonUtility.FromJson<NewPlayerInitPayload>(payloadJson);

    if (payload.version != NewPlayerInitPayload.CurrentVersion)
    {
      OnFatalError($"The game you're trying to join seems to be running an incompatible version of the game.\nQuit the game and make sure Steam has no pending updates.");
      return;
    }

    byte[] unzippedVoosBytes = Util.UnGZip(zippedVoosBytes);
    var voosReader = new UNET.NetworkReader(unzippedVoosBytes);

    behaviorSystem.LoadDatabaseForNetworkInit(payload.behaviorDatabase);
    voosEngine.DeserializePlayerInitV2(voosReader);
    sojoSystem.LoadDatabase(payload.sojoDatabase);
    stage.Load(payload.stage);

    receivedPlayerInitPayload = true;

    lastReceivedTerrainMeta = payload.terrainMeta;
  }

  [PunRPC]
  void InitNewPlayerTerrainRPC(byte[] bytes)
  {
    Util.Log($"InitNewPlayerTerrainRPC, {bytes.Length} bytes");
    Debug.Assert(receivedPlayerInitPayload, "Got InitNewPlayerTerrainRPC before InitNewPlayerRPC");
    terrain.Reset(stage.GetGroundSize(), bytes, false, null, lastReceivedTerrainMeta.customStyleWorkshopIds);
  }

  [PunRPC]
  void TriggerTerrainResetRPC()
  {
    // Passing null for data (and for custom styles) will cause it to copy from what's currently there.
    terrain.Reset(stage.GetGroundSize(), null, false, null);
  }

  public void TriggerTerrainReset()
  {
#if USE_PUN
    photonView.RPC("TriggerTerrainResetRPC", PhotonTargets.AllViaServer);
#else
    TriggerTerrainResetRPC();
#endif
  }

  public static bool CanDoMultiplayerMapSwitch()
  {
    return PhotonUtil.ActuallyConnected() && PhotonNetwork.inRoom && PhotonNetwork.isMasterClient;
  }

  internal void SendReportToMasterClient(string nickName, string description)
  {
    if (PhotonNetwork.isMasterClient)
    {
      return;
    }

    photonView.RPC("SendReportToMasterClientRPC", PhotonNetwork.masterClient, nickName, description);
  }

  void KickByNickName(string nickName)
  {
    foreach (var player in PhotonNetwork.playerList)
    {
      if (player.NickName == nickName)
      {
        KickPlayer(player);
        break;
      }
    }
  }

  [PunRPC]
  void SendReportToMasterClientRPC(string nickName, string description)
  {
    popups.Show(new DynamicPopup.Popup
    {
      fullWidthButtons = true,
      getMessage = () => $"Player {nickName} has been reported.",
      buttons = new List<PopupButton.Params>(){
        new PopupButton.Params{ getLabel = () => $"Kick {nickName}", onClick = () => KickByNickName(nickName) },
        new PopupButton.Params{ getLabel = () => $"OK", onClick = () => {}}
      }
    });
  }
}
