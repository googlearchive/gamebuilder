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
using UnityEngine.EventSystems;

//setup the canvas to hand full or split screen

//it also manages the top layer of player state:
//1. am I in edit? 
//2. am i in menu?
//3. cursor position and visiblity

public class UserMain : MonoBehaviour
{
  bool editMode = false;
  Vector2 mousePos = Vector2.zero;

  [SerializeField] UnityEngine.Audio.AudioMixer audioMixer;
  [SerializeField] CanvasGroup canvasGroup;
  [SerializeField] CanvasGroup hudCanvasGroup;
  [SerializeField] InputControl inputControl;
  [SerializeField] RectTransform mainRect;
  [SerializeField] RectTransform playRect;
  [SerializeField] RectTransform editRect;
  [SerializeField] RectTransform pacmanTutorialRect;
  [SerializeField] RectTransform consoleAnchor;
  [SerializeField] CodeErrorNotification codeErrorNotification;

  [SerializeField] AvatarMain playMain;
  [SerializeField] SidebarManager sidebarManager;
  [SerializeField] EditMain editMain;
  [SerializeField] GameObject mainMenuPrefab;
  [SerializeField] VoosEngine voosEngine;

  [SerializeField] NavigationControls navigationControls;
  [SerializeField] HudTransitionControl hudTransitionControl;
  [SerializeField] GameObject reticleObject;
  [SerializeField] GameObject pauseFeedbackObjectPlaying;
  [SerializeField] GameObject pauseFeedbackObjectEditing;
  [SerializeField] GameObject recoveryModeText;
  [SerializeField] CommandConsoleUI commandConsolePrefab;
  [SerializeField] HeaderMenu headerMenu;
  [SerializeField] SystemMenu systemMenuPrefab;

  AvatarMain[] avatars;

  public PlayerOptions playerOptions;

  MouseoverTooltip mouseoverTooltip;
  UserBody userBody;

  AvatarMain currentAvatar;
  CommandConsoleUI commandConsole;
  HudNotifications consoleMessages;
  NetworkingController networkingController;
  GameBuilderSceneController sceneController;
  VirtualPlayerManager virtualPlayerManager;
  PlayerControlsManager playerControlsManager;
  GameBuilderStage gbStage;
  DynamicPopup dynamicPopup;
  HudManager hudManager;
  private InputFieldOracle inputFieldOracle;
  GlobalExceptionHandler globalExceptionHandler;
  UndoStack undoStack;
  GlobalExceptionHandler bsod;
  GameBuilderLogHandler logHandler;

  [HideInInspector] public SystemMenu systemMenu;

  Color? lastTintPushedToUserBody = null;

  const float MIN_MOUSEWHEEL_SENSITIVITY_WIN = .1f;
  const float MAX_MOUSEWHEEL_SENSITIVITY_WIN = 1f;
  const float DEFAULT_MOUSEWHEEL_SENSITIVITY_WIN = .5f;

  const float MIN_MOUSEWHEEL_SENSITIVITY_MAC = .003f;
  const float MAX_MOUSEWHEEL_SENSITIVITY_MAC = .03f;
  const float DEFAULT_MOUSEWHEEL_SENSITIVITY_MAC = .020f;

  // This constant is no longer used but it's funny.
  const float PACMAN_WIDTH = 750;

  public const float DEFAULT_MOUSE_SENSITIVITY = 1;
  const float DEFAULT_SFX_VOLUME = .5f;
  const float DEFAULT_MUSIC_VOLUME = 1f;


  Vector2 CONSOLE_ANCHOR_POSITION_PLAY = new Vector2(-10, -10);
  Vector2 CONSOLE_ANCHOR_POSITION_EDIT = new Vector2(-10, -120);

  // public bool playModeOnly { get; private set; }

  public RectTransform GetMainRect()
  {
    return mainRect;
  }

  public bool CursorActive()
  {
    return IsHeaderMenuActive() ||
    !navigationControls.MouseLookActive() ||
    (currentAvatar != null ? currentAvatar.CursorActive() : false) ||
    dynamicPopup.IsOpen() ||
    globalExceptionHandler.IsShowing();
  }

  public bool KeyLock()
  {
    return (inputFieldOracle.WasAnyFieldFocusedRecently())
        || IsHeaderMenuActive()
        || playMain.KeyLock()
        || editMain.KeyLock()
        || commandConsole.IsConsoleInputActive();
    // return IsGameMenuActive() || playMain.KeyLock() || editMain.KeyLock() || (consoleBrowser.KeyboardHasFocus && (IsConsoleActive() || AnyTutorialActive()));
  }

  internal bool CameraCapturedCursor()
  {
    return navigationControls.CameraCapturedCursor();
  }

  public bool ShouldAvatarKeyLock()
  {
    return IsHeaderMenuActive() || commandConsole.IsConsoleInputActive();
  }

  public InputControl GetInputControl()
  {
    return inputControl;
  }

  public bool InEditMode()
  {
    return editMain.IsActive();
  }

  public Vector2 GetMousePos()
  {
    return mousePos;
  }

  public void ToggleMouseInvert(bool on)
  {
    playerOptions.invertMouselook = on;
    PlayerPrefs.SetInt("invertMouselook", playerOptions.invertMouselook ? 1 : 0);
  }

  public void SetMouseSensitivity(float value)
  {
    playerOptions.mouseLookSensitivity = value;
    PlayerPrefs.SetFloat("mouseLookSensitivity", playerOptions.mouseLookSensitivity);
  }

  public void SetMouseWheelSensitivity(float value)
  {
    float realValue;
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    realValue = Mathf.Lerp(MIN_MOUSEWHEEL_SENSITIVITY_WIN, MAX_MOUSEWHEEL_SENSITIVITY_WIN, value);
#else
    realValue = Mathf.Lerp(MIN_MOUSEWHEEL_SENSITIVITY_MAC, MAX_MOUSEWHEEL_SENSITIVITY_MAC, value);
#endif

    playerOptions.mouseWheelSensitivity = realValue;
    PlayerPrefs.SetFloat("mouseWheelSensitivity", playerOptions.mouseWheelSensitivity);
  }

  public float GetMouseWheelSensitivity()
  {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    float val = Mathf.InverseLerp(MIN_MOUSEWHEEL_SENSITIVITY_WIN, MAX_MOUSEWHEEL_SENSITIVITY_WIN, playerOptions.mouseWheelSensitivity);
    return val;
#else
    return Mathf.InverseLerp(MIN_MOUSEWHEEL_SENSITIVITY_MAC, MAX_MOUSEWHEEL_SENSITIVITY_MAC, playerOptions.mouseWheelSensitivity);
#endif
  }

  internal void ResetMouseWheelSensitivity()
  {
    float value;

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
    value = DEFAULT_MOUSEWHEEL_SENSITIVITY_WIN;
#else
    value = DEFAULT_MOUSEWHEEL_SENSITIVITY_MAC;
#endif
    playerOptions.mouseWheelSensitivity = value;
    PlayerPrefs.SetFloat("mouseWheelSensitivity", playerOptions.mouseWheelSensitivity);
  }

  public void ToggleAutoPause(bool on)
  {
    playerOptions.autoPause = on;
    PlayerPrefs.SetInt("autoPause", playerOptions.autoPause ? 1 : 0);
  }

  // public void ToggleShowAdvanced(bool on)
  // {
  //   playerOptions.showAdvanced = on;
  //   PlayerPrefs.SetInt("showAdvanced", playerOptions.showAdvanced ? 1 : 0);
  // }

  public void SetSFXVolume(float newVolume)
  {
    playerOptions.sfxVolume = newVolume;
    PlayerPrefs.SetFloat("sfxVolume", playerOptions.sfxVolume);
    if (playerOptions.sfxVolume != 0)
    {
      audioMixer.SetFloat("sfxVolume", Mathf.Log10(playerOptions.sfxVolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("sfxVolume", -80);
    }
  }

  public void SetMusicVolume(float newVolume)
  {
    playerOptions.musicVolume = newVolume;
    PlayerPrefs.SetFloat("musicVolume", playerOptions.musicVolume);
    if (playerOptions.musicVolume != 0)
    {
      audioMixer.SetFloat("musicVolume", Mathf.Log10(playerOptions.musicVolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("musicVolume", -80);
    }
  }

  public void TemporarilyKillMusicAndSfx()
  {
    audioMixer.SetFloat("musicVolume", -80);
    audioMixer.SetFloat("sfxVolume", -80);
  }

  // public bool IsAvatarMouseLookActive()
  // {
  //   if (currentAvatar != null)
  //   {
  //     return currentAvatar.MouseLookActive();
  //   }
  //   else
  //   {
  //     return false;
  //   }
  // }

  public Ray GetCursorRay()
  {
    if (currentAvatar == editMain)
    {
      return editMain.GetCursorRay();
    }
    else
    {
      return navigationControls.targetCamera.ViewportPointToRay(navigationControls.GetDefaultSelectionPoint());
    }
  }

  public bool IsAvatarKeyLock()
  {
    if (currentAvatar != null)
    {
      return currentAvatar.KeyLock();
    }
    else
    {
      return false;
    }
  }

  public void ToggleReticles(bool on)
  {
    reticleObject.SetActive(on);
  }

  void ToggleConsole()
  {
    commandConsole.ToggleConsole();
  }

  void LoadPlayerOptions()
  {
    playerOptions = new PlayerOptions();
    playerOptions.showTooltips = PlayerPrefs.GetInt("showTooltips", 1) == 1 ? true : false;
    playerOptions.invertMouselook = PlayerPrefs.GetInt("invertMouselook", 0) == 1 ? true : false;
    playerOptions.autoPause = PlayerPrefs.GetInt("autoPause", 0) == 1 ? true : false;
    playerOptions.hideAvatarInTopDown = PlayerPrefs.GetInt("hideAvatarInTopDown", 0) == 1 ? true : false;
    playerOptions.sfxVolume = PlayerPrefs.GetFloat("sfxVolume", DEFAULT_SFX_VOLUME);
    playerOptions.musicVolume = PlayerPrefs.GetFloat("musicVolume", DEFAULT_MUSIC_VOLUME);
    playerOptions.mouseLookSensitivity = PlayerPrefs.GetFloat("mouseLookSensitivity", DEFAULT_MOUSE_SENSITIVITY);
    playerOptions.mouseWheelSensitivity = PlayerPrefs.GetFloat("mouseWheelSensitivity", DEFAULT_MOUSEWHEEL_SENSITIVITY_WIN);

    systemMenu.GetGraphicsMenu().Setup();

    audioMixer.SetFloat("musicVolume", -80);
    audioMixer.SetFloat("sfxVolume", -80);
  }

  public void FadeInAudioAfterLoading()
  {
    float sfxvolume = PlayerPrefs.GetFloat("sfxVolume", .5f);
    float musicvolume = PlayerPrefs.GetFloat("musicVolume", 1f);
    AudioMixerFadeIn(sfxvolume, musicvolume);
  }

  public void AudioMixerFadeIn(float sfxvolume, float musicvolume)
  {
    if (musicvolume != 0)
    {
      audioMixer.SetFloat("musicVolume", Mathf.Log10(musicvolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("musicVolume", -80);
    }

    if (sfxvolume != 0)
    {
      audioMixer.SetFloat("sfxVolume", Mathf.Log10(sfxvolume) * 20);
    }
    else
    {
      audioMixer.SetFloat("sfxVolume", -80);
    }
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref inputFieldOracle);
  }

  public void Setup()
  {
    transform.SetParent(null, false);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref networkingController);
    Util.FindIfNotSet(this, ref consoleMessages);
    Util.FindIfNotSet(this, ref dynamicPopup);
    Util.FindIfNotSet(this, ref hudManager);
    Util.FindIfNotSet(this, ref mouseoverTooltip);
    Util.FindIfNotSet(this, ref globalExceptionHandler);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref sceneController);
    Util.FindIfNotSet(this, ref gbStage);
    Util.FindIfNotSet(this, ref virtualPlayerManager);
    Util.FindIfNotSet(this, ref playerControlsManager);
    Util.FindIfNotSet(this, ref bsod);
    Util.FindIfNotSet(this, ref logHandler);

    codeErrorNotification.Setup();
    logHandler.onDisplayCodeError = codeErrorNotification.Display;

    inputControl.Setup();
    undoStack.onUndone += item => AddDebugMessage($"Undoing \"{item.actionLabel}.\"");
    undoStack.onRedone += item => AddDebugMessage($"Re-doing \"{item.actionLabel}.\"");
    systemMenu = GameObject.Instantiate(systemMenuPrefab, editMain.topLeftAnchor);
    systemMenu.Setup();

    mouseoverTooltip.Setup(this);
    mouseoverTooltip.SetText("");

    commandConsole = Instantiate(commandConsolePrefab);
    commandConsole.SetHudManager(hudManager);

    consoleMessages.GetComponent<RectTransform>().SetParent(consoleAnchor, false);
    consoleMessages.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

    LoadPlayerOptions();

    sidebarManager.Setup();

    //change layer for player
    Transform[] transforms = gameObject.GetComponentsInChildren<Transform>();
    foreach (Transform t in transforms)
    {
      t.gameObject.layer = LayerMask.NameToLayer("Player");
    }

    avatars = new AvatarMain[] { playMain, editMain };
    foreach (AvatarMain av in avatars)
    {
      av.Setup(this);
      av.transform.SetParent(null);
    }

    navigationControls.UpdateTargetThirdPersonPivot(userBody.thirdPersonCameraPivotAnchor);

    SetEditMode(CanEdit() ? GameBuilderApplication.CurrentGameOptions.playOptions.startInBuildMode : false);

    Application.wantsToQuit += WantsToQuit;

    if (networkingController.GetIsInMultiplayer())
    {
      headerMenu.OpenMultiplayerMenu();
    }
  }

  public bool CanEdit()
  {
    if (GameBuilderApplication.IsStandaloneExport) return false;

    if (networkingController.GetIsInMultiplayer())
    {
      VirtualPlayerManager.VirtualPlayerInfo? playerInfo = virtualPlayerManager.GetVirtualPlayerById(playerControlsManager.GetVirtualPlayerId());
      return playerInfo != null ? playerInfo.Value.canEdit : true;
    }
    else
    {
      return singlePlayerCanEdit;
    }
  }

  void OnDestroy()
  {
    Application.wantsToQuit -= WantsToQuit;
  }

  public SaveMenu GetSaveMenu()
  {
    return headerMenu.GetSaveMenu();
  }

  public void ToggleActorList()
  {
    headerMenu.ToggleActorList();
  }


  public void SetupUserBody(UserBody _userbody)
  {
    userBody = _userbody;
    editMain.emissionAnchor = userBody.toolEmissionAnchor;

    userBody.gameObject.AddComponent<AudioListener>();

    navigationControls.SetUserBody(_userbody);
  }

  public LayerMask GetLayerMask()
  {
    int defaultMask = 1 << LayerMask.NameToLayer("Default");
    // defaultMask = defaultMask.WithBit(LayerMask.NameToLayer("PrefabWorld"), false);
    defaultMask = defaultMask.WithBit(LayerMask.NameToLayer("VoosActor"), true);
    defaultMask = defaultMask.WithBit(LayerMask.NameToLayer("OffstageGhost"), true);
    return defaultMask;
  }



  public void UpdateCameraMask()
  {
    int curMask = GetCamera().cullingMask;

    // curMask = curMask.WithBit(LayerMask.NameToLayer("PrefabWorld"), false);
    // curMask = curMask.WithBit(LayerMask.NameToLayer("OffstageGhost"), false);
    curMask = curMask.WithBit(LayerMask.NameToLayer("Player"), true && !navigationControls.hidingPlayerLayer);
    curMask = curMask.WithBit(LayerMask.NameToLayer("PlayerEditMode"), !navigationControls.hidingPlayerLayer && !HidingAvatarInTopDown());
    GetCamera().cullingMask = curMask.WithBit(LayerMask.NameToLayer("VoosActor"), true);

  }

  bool HidingAvatarInTopDown()
  {
    return playerOptions.hideAvatarInTopDown && InEditMode() && !editMain.Using3DCamera();
  }

  public bool CursorOverUI()
  {
    return EventSystem.current.IsPointerOverGameObject();
  }

  public void SetMouseoverTooltipText(string newtext)
  {
    if (mouseoverTooltip != null)
    {
      mouseoverTooltip.SetText(newtext);
    }
  }

  public Vector2 GetScreenSize()
  {
    return new Vector2(Screen.width, Screen.height);
  }

  public void AddDebugMessage(string s)
  {
    consoleMessages.AddMessage(s);
  }

  public void ToggleTooltips(bool on)
  {
    playerOptions.showTooltips = on;
    PlayerPrefs.SetInt("showTooltips", playerOptions.showTooltips ? 1 : 0);
  }


  public bool IsShowingTooltips()
  {
    return playerOptions.showTooltips;
  }

  public bool CursorOverObject(GameObject g)
  {
    PointerEventData pointer = new PointerEventData(EventSystem.current)
    {
      position = Input.mousePosition
    };

    List<RaycastResult> results = new List<RaycastResult>();
    EventSystem.current.RaycastAll(pointer, results);

    foreach (RaycastResult r in results)
    {
      if (r.gameObject == g) return true;
    }
    return false;
  }

  // public void SetCameraHeight(float percentage)
  // {
  //   navigationControls.targetCamera.rect = new Rect(0, 0, 1, percentage);
  // }

  public Camera GetCamera()
  {
    return navigationControls.targetCamera;
  }

  public void SetEditMode(bool on)
  {
    if (on && !CanEdit())
    {
      AddDebugMessage("Play mode only");
      return;
    }

    SetMouseoverTooltipText("");


    if (on) navigationControls.TryReleaseCursor();

    editMode = on;

    if (editMode)
    {
    }
    else
    {
    }

    consoleAnchor.anchoredPosition = editMode ? CONSOLE_ANCHOR_POSITION_EDIT : CONSOLE_ANCHOR_POSITION_PLAY;

    userBody.SetInPlayMode(!editMode);

    AvatarMain lastAvatar = currentAvatar;
    currentAvatar = on ? editMain : playMain;

    navigationControls.SetMode(editMode ? NavigationControls.Mode.Fly : NavigationControls.Mode.Grounded);

    // Only do the teleport if:
    // 1. Going from play to edit mode for the first time
    // 2. Going from edit to play mode if there is no player actor, then DO teleport to prevent
    // us from returning to a meaningless play-mode position.
    // if (lastAvatar != null && lastAvatar != currentAvatar && ((on && firstTimeInEditMode) || GetPlayerActor() == null))
    // {
    //   currentAvatar.Teleport(lastAvatar.GetAvatarPosition(), lastAvatar.GetAim());
    // }

    bool shouldTeleport = on || Util.IsControlOrCommandHeld();

    if (lastAvatar != null && lastAvatar != currentAvatar && (shouldTeleport || GetPlayerActor() == null))
    {
      currentAvatar.Teleport(lastAvatar.GetAvatarPosition(), lastAvatar.GetAim());
    }

    //this is a hack for the conditional above to only teleport from play->edit the first time
    // if (on && firstTimeInEditMode) firstTimeInEditMode = false;

    //making user body have no parent to prevent momentary setactive(false) (which messes w/ animation)
    userBody.transform.SetParent(null);

    AddDebugMessage(editMode ? "Switching to EDIT mode" : "Switching to PLAY mode");

    playMain.Activate(!editMode);
    editMain.Activate(editMode);

    voosEngine.NotifyEditModeToggled(on);

    //putting user body on its new home
    userBody.transform.SetParent(currentAvatar.bodyParent);
    userBody.transform.localPosition = Vector3.zero;
    userBody.transform.localRotation = Quaternion.identity;

    navigationControls.ToggleEditCullingMask(on);

    if (!editMode) navigationControls.TryCaptureCursor();

    if (editMode)
    {
      hudTransitionControl.SetEditMode();
    }
    else
    {
      hudTransitionControl.SetPlayMode();
    }

    if (playerOptions.autoPause)
    {
      if (editMode)
      {
        voosEngine.SetIsRunning(false);
      }
      else
      {
        voosEngine.SetIsRunning(true);
      }
    }
  }

  // Tries to migrate the user to the target actor.
  // If targetActor == null, the user will be actorless.
  public void MigrateUserTo(VoosActor targetActor)
  {
    PlayMain pm = playMain.GetComponent<PlayMain>();
    PlayerBody targetPlayerBody = targetActor?.GetPlayerBody();

    if (targetActor != null && (!targetActor.GetIsPlayerControllable() || null == targetPlayerBody))
    {
      targetActor.SetIsPlayerControllable(true);
      targetPlayerBody = targetActor.GetPlayerBody();
      Debug.Assert(targetPlayerBody != null, "MigrateUserTo could not add a PlayerBody to target actor.");
    }
    pm.SetPlayerBody(targetPlayerBody);
    // If we are in play mode, we need to reparent the user body.
    // Otherwise this will be done later, when we enter play mode.
    if (!InEditMode())
    {
      userBody.transform.SetParent(playMain.bodyParent);
      userBody.transform.localPosition = Vector3.zero;
      userBody.transform.localRotation = Quaternion.identity;
    }
    if (targetActor != null)
    {
      targetActor.RequestOwnership();
    }
    VoosActor playMainActor = pm.GetPlayerActor();
    Debug.Assert(targetActor == playMainActor, "MigrateUserTo didn't succeed, we wanted targetActor to be " + targetActor + " but PlayMain still has " + playMain);
  }

  public VoosActor GetPlayerActor()
  {
    return playMain.GetComponent<PlayMain>().GetPlayerActor();
  }

  public bool IsHeaderMenuActive()
  {
    return false;//mainMenu.IsOpen();
  }


  public Vector2 GetCursorPosition()
  {
    return Input.mousePosition;
  }

  public bool SetCameraView(CameraView cv, bool byUserRequest)
  {
    // The user is not allowed to change camera view in play mode if
    // the camera is in actor-drive mode.
    if (byUserRequest && !editMode && navigationControls.GetCameraView() == CameraView.ActorDriven)
    {
      // TODO: give UI feedback.
      Debug.Log("Can't change camera in play mode because there is a custom camera on.");
      return false;
    }

    foreach (AvatarMain av in avatars)
    {
      av.OnCameraViewUpdate(cv);
    }
    navigationControls.SetCameraView(cv);

    return true;
  }

  public CameraView GetCameraView()
  {
    return navigationControls.GetCameraView();
  }

  public void RotateCameraView()
  {
    navigationControls.RotateViewByIncrement();
  }

  const float DISCRETE_CAMERA_ZOOM_AMOUNT = .2f;
  public void DiscreteCameraZoomIn()
  {
    SetCameraViewZoom(GetCameraViewZoom() + DISCRETE_CAMERA_ZOOM_AMOUNT);
  }

  public void DiscreteCameraZoomOut()
  {
    SetCameraViewZoom(GetCameraViewZoom() - DISCRETE_CAMERA_ZOOM_AMOUNT);
  }

  public float GetCameraViewZoom()
  {
    return navigationControls.GetViewZoom();
  }

  public void SetCameraViewZoom(float newzoom)
  {
    navigationControls.SetViewZoom(newzoom);
  }

  public void AddCameraEffect(string effectName)
  {
    navigationControls.targetCamera.gameObject.AddComponent(Type.GetType(effectName));
  }

  public void RemoveCameraEffect(string effectName)
  {
    var temp = navigationControls.targetCamera.gameObject.GetComponent(Type.GetType(effectName));
    if (temp != null) Destroy(temp);
  }

  // Sets the actor that will serve as the camera. If null, returns to the default camera.
  public void SetCameraActor(VoosActor cameraActor)
  {
    if (cameraActor != null)
    {
      navigationControls.SwitchToActorDrivenCamera(cameraActor);
    }
    else
    {
      navigationControls.SetCameraView(CameraView.ThirdPerson);
    }
  }

  bool IsCursorVisible()
  {
    return Cursor.lockState != CursorLockMode.Locked;
  }

  void OnMenuButtonDown()
  {
    if (inputFieldOracle.WasAnyFieldFocusedRecently())
    {
    }
    else if (dynamicPopup.IsOpen() && dynamicPopup.IsCancellable())
    {
      dynamicPopup.Cancel();
    }
    else if (navigationControls.TryReleaseCursor())
    {

    }
    else if (currentAvatar.OnEscape())
    {
    }
    else if (headerMenu.Back())
    {
    }

  }

  CameraView editCameraView = CameraView.Isometric;
  public void NextCameraView()
  {
    CameraView curView = GetCameraView();

    switch (curView)
    {
      case CameraView.FirstPerson:
        editCameraView = CameraView.ThirdPerson;
        SetCameraView(CameraView.ThirdPerson, true);
        break;
      case CameraView.ThirdPerson:
        editCameraView = CameraView.Isometric;
        SetCameraView(CameraView.Isometric, true);
        break;
      case CameraView.Isometric:
        editCameraView = CameraView.FirstPerson;
        SetCameraView(CameraView.FirstPerson, true);
        break;
      case CameraView.ActorDriven:
        dynamicPopup.Show("You can't change the camera because this game uses a custom camera.\n" +
          "To go back to the normal camera, go into EDIT MODE and delete your camera.", "OK", () => { });
        break;
      default:
        return;
    }
  }

  void UndoCheck()
  {
    bool editorUndoRequest = false;
    bool editorRedoRequest = false;

#if UNITY_EDITOR
    editorUndoRequest = Input.GetKeyDown(KeyCode.F8);
    editorRedoRequest = Input.GetKeyDown(KeyCode.F9);
#endif

    if (inputControl.GetButtonDown("Redo") || editorRedoRequest)
    {
      TryRedo();
    }
    // Else if is pretty important - since the keys for undo is subset of redo.
    else if (inputControl.GetButtonDown("Undo") || editorUndoRequest)
    {
      TryUndo();
    }
  }

  public bool UndoAvailable()
  {
    return !undoStack.IsEmpty();
  }

  public bool RedoAvailable()
  {
    return !undoStack.IsRedoEmpty();
  }

  public void TryUndo()
  {
    if (undoStack.IsEmpty())
    {
      AddDebugMessage("Nothing to undo.");
    }
    else
    {
      undoStack.TriggerUndo();
    }
  }

  public void TryRedo()
  {
    if (undoStack.IsRedoEmpty())
    {
      AddDebugMessage("Nothing to redo.");
    }
    else
    {
      undoStack.TriggerRedo();
    }
  }

  bool IsMultiplayerHost()
  {
    if (networkingController.GetIsInMultiplayer())
    {
      return PhotonNetwork.isMasterClient;
    }

    return false;
  }



  void Update()
  {

    if (IsMultiplayerHost())
    {
      UpdateBuildPermissionsForNewUsers();
    }

    CheckResetByHotkey();
    CheckPauseByHotkey();

    AudioListener.volume = Application.isFocused ? 1 : 0;

    bool playingAndPaused = !editMode && !voosEngine.GetIsRunning();
    if (pauseFeedbackObjectPlaying.activeSelf != playingAndPaused)
    {
      pauseFeedbackObjectPlaying.SetActive(playingAndPaused);
    }
    bool editingAndPaused = editMode && !voosEngine.GetIsRunning();
    if (pauseFeedbackObjectEditing.activeSelf != editingAndPaused)
    {
      pauseFeedbackObjectEditing.SetActive(editingAndPaused);
    }


    ToggleReticles(!CursorActive());

    recoveryModeText.SetActive(GameBuilderApplication.IsRecoveryMode);


    if (!KeyLock())
    {
      if (inputControl.GetButtonDown("View"))
      {
        NextCameraView();
      }

      if (!GameBuilderApplication.IsStandaloneExport)
      {

        if (inputControl.GetButtonDown("Save"))
        {
          SaveFromShortcut();
        }


        if (inputControl.GetButtonDown("Console"))
        {
          ToggleConsole();
        }

        UndoCheck();
      }
    }

    if (inputControl.GetButtonDown("ToggleBuildPlay") && (!KeyLock() || Input.GetKey(KeyCode.LeftControl)) && !IsHeaderMenuActive() && CanEdit())
    {
      SetEditMode(!editMode);
    }

    if (editMode && !CanEdit())
    {
      SetEditMode(false);
    }

    if (Input.GetButtonDown("Cancel"))
    {
      OnMenuButtonDown();
    }

    if (CursorActive() != IsCursorVisible())
    {
      Cursor.lockState = CursorActive() ? CursorLockMode.None : CursorLockMode.Locked;
      Cursor.visible = Cursor.lockState == CursorLockMode.Locked ? false : true;
    }

    //update mouse position
    if (CursorActive() && !CursorOverUI())
    {
      Vector2 pos = Input.mousePosition;
      pos.x = Mathf.Clamp01(pos.x / Screen.width);
      pos.y = Mathf.Clamp01(pos.y / Screen.height);
      mousePos = pos;
    }

    // Check if we are in the right camera mode for the current player actor.
    UpdateCameraActor();

    // HACK: to avoid jarring camera changes, if there is no player actor and we're in play mode, then set
    // the UserBody to be where the last play mode avatar position was. Otherwise it would just sit there
    // at Vector3.zero, and the camera would warp all the way there, then all the way back, and that's ugly.
    if (GetPlayerActor() == null && !InEditMode()
      // Fix NPE on scene switch
      && userBody != null && userBody.transform != null)
    {
      userBody.transform.position = playMain.GetAvatarPosition();
    }

    UpdateUserBodyTint();
    UpdateIsPlayingAsRobot();
  }

  internal void SaveFromShortcut()
  {
    if (headerMenu.ShowWorkshopInfo())
    {
      headerMenu.SetOpenSaveOrWorkshop(true);
    }
    else
    {
      headerMenu.GetSaveMenu().SaveFromShortcut();
    }
  }

  private void CheckPauseByHotkey()
  {
    if (inputControl.GetButtonDown("Pause"))
    {
      voosEngine.SetIsRunning(!voosEngine.GetIsRunning());
    }

#if UNITY_EDITOR
    if (Input.GetKeyDown(KeyCode.F7))
    {
      voosEngine.SetIsRunning(!voosEngine.GetIsRunning());
    }
#endif

  }

  private void CheckResetByHotkey()
  {
    if (CanEdit() && inputControl.GetButtonDown("Reset"))
    {
      Debug.Log("Game reset triggered - frame " + Time.frameCount);
      voosEngine.ResetGame();
    }
  }

  HashSet<string> VirtualIDsWithSetBuildPermissions = new HashSet<string>();
  private void UpdateBuildPermissionsForNewUsers()
  {
    HashSet<string> newIds = new HashSet<string>();
    foreach (VirtualPlayerManager.VirtualPlayerInfo player in virtualPlayerManager.EnumerateVirtualPlayers())
    {
      if (VirtualIDsWithSetBuildPermissions.Contains(player.virtualId)) continue;
      newIds.Add(player.virtualId);
    }

    foreach (string id in newIds)
    {
      VirtualIDsWithSetBuildPermissions.Add(id);
      virtualPlayerManager.SetPlayerCanEdit(id, headerMenu.multiplayerGameMenu.GetNewUsersCanBuild());
    }
  }

  private void UpdateIsPlayingAsRobot()
  {
    VoosActor playerActor = GetPlayerActor();
    if (playerActor == null || userBody == null)
    {
      return;
    }
    userBody.SetIsPlayingAsRobot(playerActor.GetRenderableUri() == VoosActor.AVATAR_EXPLORER_URI);
  }

  internal MouseoverTooltip GetMouseoverTooltip()
  {
    return mouseoverTooltip;
  }

  private void UpdateCameraActor()
  {
    VoosActor curCameraActor = navigationControls.GetCameraView() == CameraView.ActorDriven ?
        navigationControls.GetActorDrivenCameraActor() : null;
    VoosActor requiredCameraActor = (editMode || GetPlayerActor() == null) ? null :
        string.IsNullOrEmpty(GetPlayerActor().GetCameraActor()) ? null :
        voosEngine.GetActor(GetPlayerActor().GetCameraActor());
    // Are things already as they should be?
    if (curCameraActor == requiredCameraActor)
    {
      // Things are already as they should be.
      return;
    }
    if (requiredCameraActor != null)
    {
      navigationControls.SwitchToActorDrivenCamera(requiredCameraActor);
    }
    else
    {
      // TODO: preserve previous camera view so we can come back to the same one
      // when returning to edit mode?

      // navigationControls.SetCameraView(CameraView.Isometric);
      navigationControls.SetCameraView(editCameraView);
    }
  }

  public NavigationControls GetNavigationControls()
  {
    return navigationControls;
  }

  public RectTransform getEditRect()
  {
    return editRect;
  }

  private void UpdateUserBodyTint()
  {
    VoosActor playerActor = GetPlayerActor();
    if (playerActor == null || userBody == null)
    {
      return;
    }
    if (lastTintPushedToUserBody != null && playerActor.GetTint() == lastTintPushedToUserBody.Value)
    {
      // Already up to date.
      return;
    }
    Color newTint = playerActor.GetTint();
    userBody.SetTint(newTint);
    lastTintPushedToUserBody = newTint;
  }

  public VoosActor GetCameraActor()
  {
    return navigationControls.GetCameraView() == CameraView.ActorDriven ?
      navigationControls.GetActorDrivenCameraActor() : null;
  }

  public void ToggleUI()
  {
    if (canvasGroup.alpha == 1)
    {
      sidebarManager.HideAllSidebars(true);
      canvasGroup.alpha = 0;
    }
    else
    {
      sidebarManager.HideAllSidebars(false);
      canvasGroup.alpha = 1;
    }
  }

  public void ToggleEditAvatar()
  {
    // showEditAvatar = !showEditAvatar;
    SetHideAvatarInTopDown(!playerOptions.hideAvatarInTopDown);
  }

  public void SetHideAvatarInTopDown(bool on)
  {
    playerOptions.hideAvatarInTopDown = on;
    PlayerPrefs.SetInt("hideAvatarInTopDown", playerOptions.hideAvatarInTopDown ? 1 : 0);
    UpdateCameraMask();
  }

  public void ToggleHUD()
  {
    if (hudCanvasGroup.alpha == 1)
    {
      // sidebarManager.HideAllSidebars(true);
      hudCanvasGroup.alpha = 0;
    }
    else
    {
      // sidebarManager.HideAllSidebars(false);
      hudCanvasGroup.alpha = 1;
    }
  }

  public void ShowExitPopup(string exitPrompt, string exitButtonPrompt, System.Action callback)
  {
    if (GameBuilderApplication.IsTutorialMode)
    {
      ExitPopupWithoutSave(exitPrompt, exitButtonPrompt, callback);
    }
    else
    {
      SavePopup(() => sceneController.LoadSplashScreen());
    }
  }

  public void ShowCodeEditor(string behaviorUri, VoosEngine.BehaviorLogItem? error = null)
  {
    if (CanEdit())
    {
      if (!InEditMode())
      {
        SetEditMode(true);
      }
      editMain.ShowCodeEditor(behaviorUri, error);
    }
  }

  bool allowQuitIfWanted = false;

  public bool WantsToQuit()
  {
    if (allowQuitIfWanted)
    {
      bsod.NotifySceneClosing();
      return true;
    }

    if (GameBuilderApplication.IsTutorialMode || GameBuilderApplication.IsStandaloneExport)
    {
      ExitPopupWithoutSave("Quit to desktop?", "Quit", OnQuitConfirmed);
    }
    else
    {
      SavePopup(OnQuitConfirmed);
    }
    return false;
  }

  void OnQuitConfirmed()
  {
    allowQuitIfWanted = true;
    Application.Quit();
  }


  void ExitPopupWithoutSave(string exitPrompt, string exitButtonPrompt, System.Action callback)
  {
    System.Action onCancel = () => { allowQuitIfWanted = false; };
    allowQuitIfWanted = true;
    dynamicPopup.Show(new DynamicPopup.Popup
    {
      forceImmediateTakeover = true,
      getMessage = () => exitPrompt,
      isCancellable = true,
      onCancel = onCancel,
      buttons = new List<PopupButton.Params>()
          {
            new PopupButton.Params
            {
              getLabel = () => exitButtonPrompt,
              onClick = callback //sceneController.LoadSplashScreen(),
            },
            new PopupButton.Params
            {
              getLabel = () => "Cancel",
              onClick = onCancel
            }
          },
      fullWidthButtons = true
    });
  }

  public void SavePopup(System.Action callback)
  {
    System.Action onCancel = () => { allowQuitIfWanted = false; };
    allowQuitIfWanted = true;
    dynamicPopup.Show(new DynamicPopup.Popup
    {
      forceImmediateTakeover = true,
      getMessage = () => "Do you want to save your project?",
      isCancellable = true,
      onCancel = onCancel,
      buttons = new List<PopupButton.Params>()
          {
            new PopupButton.Params
            {
              getLabel = () => "Save",
              onClick = () =>  GetSaveMenu().SaveBeforeExit(callback)
            },
                        new PopupButton.Params
            {
              getLabel = () => "Don't save",
              onClick = callback
            },
            new PopupButton.Params
            {
              getLabel = () => "Cancel",
              onClick = onCancel
            }
          },
      fullWidthButtons = true
    });
  }

  bool singlePlayerCanEdit = true;
  public void SetPlayModeOnly(bool value)
  {
    singlePlayerCanEdit = !value;
    if (!singlePlayerCanEdit && InEditMode())
    {
      SetEditMode(false);
    }
  }
}

[System.Serializable]
public struct PlayerOptions
{
  public bool showTooltips;
  public bool invertMouselook;
  public bool autoPause;
  public bool hideAvatarInTopDown;
  public float sfxVolume;
  public float musicVolume;
  public float mouseLookSensitivity;
  public float mouseWheelSensitivity;
}
