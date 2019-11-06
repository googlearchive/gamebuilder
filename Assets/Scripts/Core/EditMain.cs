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
using System.Linq;
using System;
// Manages movement and which tool is being selected, transitions between tools, everything up to the tool-specific stuff.
public class EditMain : AvatarMain
{
  public Transform lookTransform;
  [SerializeField] RectTransform reticleFeedbackRect;
  [SerializeField] TMPro.TextMeshProUGUI reticleLabel;
  [SerializeField] UnityEngine.UI.Image invalidSelectionImage;
  public AudioSource editAudioSource;

  [SerializeField] SidebarManager sidebarManager;
  public TMPro.TMP_InputField textToolInput;
  public GameObject textToolInputObject;
  [SerializeField] EditToolbar editToolbar;

  [SerializeField] SelectionFeedback hoverFeedback;
  public SelectionFeedback targetFeedbackPrefab;
  [SerializeField] GameObject selectionFeedbackParent;

  [SerializeField] Transform toolParent;
  [SerializeField] GameObject[] toolsToLoad;
  [SerializeField] RectTransform hudRect;
  public RectTransform centerAnchor;
  public RectTransform tooltipAnchor;
  public RectTransform topCenterAnchor;
  public RectTransform topLeftAnchor;
  public RectTransform bottomToolbarAnchor;
  [SerializeField] RectTransform tooltipAnchorIso;
  [SerializeField] RectTransform tooltipAnchor3D;
  [SerializeField] RectTransform reticleAnchor;
  [SerializeField] AudioClip toolSwitchAudioClip;
  [SerializeField] InspectorController inspectorController;
  [SerializeField] DragMultiselect dragMultiselectPrefab;

  [SerializeField] GameObject copyPasteToolPrefab;
  CopyPasteTool copyPasteTool;

  public GameObject modelLoadingObject;
  public GameObject actorModelLoadingFeedback;
  private InputFieldOracle inputFieldOracle;
  private VoosEngine engine;
  private OneOffEffects effects;
  private UndoStack undoStack;
  private GameBuilderStage gbStage;

  DynamicPopup popups;

  public const int INFO_TOOL_INDEX = 4;
  public const int LOGIC_TOOL_INDEX = 6;
  public const string GROUND_TAG = "Ground";
  public const string WALL_TAG = "Wall";
  float QUICK_ROTATE_AMOUNT = 45;

  const float MAX_SELECTION_DISTANCE = 1500f; //arbitrary
  bool sidebarActive = false;

  float hudHeight = 1;
  float percentSidebar = 0;

  bool cursorMode = true;

  public Transform emissionAnchor;
  public Transform mainAnchor;

  DragMultiselect dragMultiselect;

  VoosActor hoverActor;
  Vector3 hoverPosition;

  List<VoosActor> targetActors = new List<VoosActor>();
  VoosActor focusedTargetActor;
  List<SelectionFeedback> targetFeedbackList = new List<SelectionFeedback>();

  Vector3 currentVelocity = Vector3.zero;

  public event System.Action<IEnumerable<VoosActor>> targetActorsChanged;

  List<Tool> activeTools = new List<Tool>();

  bool active = false;

  bool cameraFollowingActor;
  bool moveCameraButtonWasPressedDownOverUI = false;

  // bool firstTimeInEditMode = true;

  internal void SetCameraFollowingActor(bool on)
  {

    if (!on || GetFocusedTargetActor() == null)
    {
      // LockCameraOnActor(null);
      cameraFollowingActor = false;
    }
    else
    {

      // LockCameraOnActor(GetFocusedTargetActor());
      cameraFollowingActor = true;
    }

  }

  public bool GetCameraFollowingActor()
  {
    if (cameraFollowingActor && GetFocusedTargetActor() == null)
    // if (cameraFollowingActor && navigationControls.GetCameraLockActor() == null)
    {
      cameraFollowingActor = false;
    }
    return cameraFollowingActor;
  }

  Tool currentToolCache = null;
  Tool currentTool
  {
    get { return currentToolCache; }
    set
    {
      currentToolCache = value;
      GetUserBody()?.SetActiveTool(value);
    }
  }

  public void SelectInfoTool()
  {
    SelectToolbarIndex(INFO_TOOL_INDEX);
  }

  public override void Activate(bool on)
  {
    //if turning on do at beginning
    if (on)
    {
      base.Activate(on);
      activationRoutine = StartCoroutine(ActivationRoutine());
    }

    active = on;
    reticleLabel.enabled = on;

    selectionFeedbackParent.SetActive(on);


    // RemoveOffstageTargetActors();

    if (on)
    {
      SelectToolbarIndex(curToolIndex);
    }
    else
    {
      ClearTools();
      moveCameraButtonWasPressedDownOverUI = false;
    }

    if (!on)
    {
      if (activationRoutine != null) StopCoroutine(activationRoutine);
      base.Activate(on);
    }
  }

  internal bool ToggleTargetActor(VoosActor actor)
  {
    if (IsActorInTargetActors(actor))
    {
      RemoveTargetActor(actor);
      return false;
    }
    else
    {
      AddTargetActor(actor);
      return true;
    }
  }

  internal bool AddSetOrRemoveTargetActor(VoosActor actor)
  {
    bool present = IsActorInTargetActors(actor);

    if (present)
    {
      return true;
    }
    else if (Input.GetKey(KeyCode.LeftShift))
    {
      AddTargetActor(actor);
      return true;
    }
    else
    {
      SetTargetActor(actor);
      return true;
    }
  }

  internal LogicSidebar GetLogicSidebar()
  {
    return sidebarManager.logicSidebar;
  }


  internal TerrainToolSettings GetTerrainSidebar()
  {
    return sidebarManager.terrainSidebar;
  }

  void ClearTools()
  {
    for (int i = activeTools.Count - 1; i >= 0; i--)
    {
      activeTools[i].Close();
    }

    activeTools.Clear();
    currentTool = null;
  }

  public RectTransform GetReticleAnchor()
  {
    return reticleAnchor;
  }

  public Tool GetCurrentTool()
  {
    return currentTool;
  }

  Vector3 transformationMovementVector = new Vector3(0, 1, 0f);

  Coroutine activationRoutine;
  IEnumerator ActivationRoutine()
  {
    float t = 0;
    while (t < 1)
    {
      t = Mathf.Clamp01(t + Time.unscaledDeltaTime * 2f);
      Vector3 _aim = navigationControls.GetAim() * Vector3.forward;
      _aim.y = 0;

      avatarTransform.Translate(Quaternion.LookRotation(_aim) * transformationMovementVector * Mathf.Sin(Mathf.PI * t) * Time.unscaledDeltaTime * 7f);
      yield return null;
    }
  }

  public override bool MouseLookActive()
  {
    if (currentTool != null)
    {
      return currentTool.MouseLookActive();
    }
    return true;
  }

  // public void LockCameraOnActor(VoosActor actor)
  // {
  //   navigationControls.SetCameraLockActor(actor);
  // }

  // public void MoveCameraToActor(VoosActor actor)
  // {
  //   navigationControls.MoveCameraToActor(actor);
  // }

  public bool ShowTooltips()
  {
    return userMain.playerOptions.showTooltips;
  }

  private void UpdateHoverActor(VoosActor newActor)
  {
    hoverActor = newActor;

    hoverFeedback.SetActor(newActor);
    currentTool?.SetHoverActor(hoverActor);
    UpdatetReticleLabel();
  }

  public override void Setup(UserMain _usermain)
  {
    base.Setup(_usermain);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref effects);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref gbStage);
    Util.FindIfNotSet(this, ref popups);

    dragMultiselect = Instantiate(dragMultiselectPrefab);
    dragMultiselect.Setup(this);

    editToolbar.Setup();
    Util.FindIfNotSet(this, ref inputFieldOracle);

    editToolbar.OnSelectIndex = SelectToolbarIndex;

    List<Sprite> sprites = new List<Sprite>();
    List<string> texts = new List<string>();

    editToolbar.OnMenuItemClick = ClickToolSlotIndex;
    SelectToolbarIndex(1);

    inspectorController.Setup();
  }

  public InspectorController GetInspectorController()
  {
    return inspectorController;
  }

  public CreationLibrarySidebar GetCreationLibrarySidebar()
  {
    return sidebarManager.creationLibrary;
  }

  void ClickToolSlotIndex(int n)
  {
    SelectToolbarIndex(n);
  }

  void TriggerUpdate()
  {
    Debug.Assert(dragMultiselect != null, "Trigger: Drag multiselect is null");

    if (!userMain.CursorOverUI())
    {
      if (inputControl.GetButtonDown("Action1"))
      {
        if (!TryClickIntoCameraView()) //using LMB to click "into" camera view
        {
          Debug.Assert(currentTool != null);

          if (!currentTool.Trigger(true))
          {
            dragMultiselect.StartDrag();
          }
        }
      }
    }

    if (inputControl.GetButtonUp("Action1"))
    {
      currentTool.Trigger(false);
      dragMultiselect.StopDrag();
    }
  }

  bool TryClickIntoCameraView()
  {
    if (!Using3DCamera() || MoveCameraButtonModifyingCamera()) return false;
    return navigationControls.TryCaptureCursor();
  }

  public bool IsCameraClickedIn()
  {
    if (MoveCameraButtonModifyingCamera()) return false;
    return navigationControls.CameraCapturedCursor();
  }

  public bool Using3DCamera()
  {
    return userMain.GetCameraView() == CameraView.FirstPerson || userMain.GetCameraView() == CameraView.ThirdPerson;
  }

  public bool UsingFirstPersonCamera()
  {
    return userMain.GetCameraView() == CameraView.FirstPerson;
  }

  public void SelectLastTool()
  {
    SelectToolbarIndex(lastToolIndex);
  }


  public Transform GetPhotoAnchor()
  {
    return navigationControls.GetPhotoAnchor();
  }

  public CameraInfo GetCameraInfo()
  {
    return navigationControls.GetCameraInfo();
  }

  public void ReselectCurrentTool()
  {
    SelectToolbarIndex(curToolIndex);
  }

  public void TryCopy()
  {
    if (currentTool.GetName() == "Copy" || GetTargetActorsCount() == 0)
    {
      return;
    }

    AppendTool<CopyPasteTool>(copyPasteToolPrefab);
  }

  public T AppendTool<T>(GameObject toolfab) where T : Tool
  {
    T newTool = Instantiate(toolfab, toolParent).GetComponent<T>();
    currentTool = newTool;
    activeTools.Add(currentTool);
    currentTool.Launch(this);
    UpdateHoverActor(null);
    return newTool;
  }

  public void RemoveToolFromList(Tool toolToRemove)
  {
    int index = activeTools.IndexOf(toolToRemove);
    activeTools[index].Close();
    activeTools.RemoveAt(index);
    currentTool = activeTools.Last();
    UpdateHoverActor(null);
  }

  public bool TryEscapeOutOfCameraView()
  {
    return navigationControls.TryReleaseCursor();
  }

  //when you hit escape
  // cancel tool stuff -> deselect actors 
  public override bool OnEscape()
  {
    if (currentTool.OnEscape())
    {
      return true;
    }

    if (GetTargetActorsCount() > 0)
    {
      ClearTargetActors();
      return true;
    }

    return false;
  }

  int lastToolIndex = 0;
  int curToolIndex = 0;
  public void SelectToolbarIndex(int n)
  {
    if (n != curToolIndex)
    {
      lastToolIndex = curToolIndex;
      curToolIndex = n;
      editAudioSource.PlayOneShot(toolSwitchAudioClip, .4f);
    }

    editToolbar.SelectIndex(n);
    SetTool<Tool>(toolsToLoad[n]);
  }

  public T SetTool<T>(GameObject toolfab) where T : Tool
  {
    ClearTools();
    T newTool = Instantiate(toolfab, toolParent).GetComponent<T>();
    currentTool = newTool;
    activeTools.Add(currentTool);
    currentTool.Launch(this);
    UpdateHoverActor(null);
    return newTool;
  }

  void CheckToolSlotInput()
  {
    if (Util.IsControlOrCommandHeld()) return;

    int n = inputControl.GetNumberKeyDown();
    if (n >= 0 && n < toolsToLoad.Length)
    {
      SelectToolbarIndex(n);
    }
  }

  bool GetCursorMode()
  {
    //mousebuttondown check makes sure we only check this after first frame of button down
    // this ensures the rightMouseButtonWasPressedDownOverUI is set to the right value before doing the check (race condition b/w two Update functions)
    // TODO: Test left mouse button draggging as RMB replacement - if that works, this goes away, and if not, we should clean this up

    if (Using3DCamera()) return GetCursorModeWithMouseLookCamera();
    else return GetCursorModeInIso();
  }

  internal bool ActorsEditable()
  {
    if (currentTool == null) return GetTargetActorsCount() > 0;

    return GetTargetActorsCount() > 0 && currentTool.CanEditTargetActors();
  }

  bool GetCursorModeInIso()
  {
    if (MoveCameraButtonModifyingCamera()) return !cursorMode || base.CursorActive();
    return cursorMode || base.CursorActive();
  }

  bool GetCursorModeWithMouseLookCamera()
  {
    if (MoveCameraButtonModifyingCamera()) return !base.CursorActive();
    return base.CursorActive();
  }

  bool MoveCameraButtonModifyingCamera()
  {
    return !inputControl.GetButtonDown("MoveCamera") && inputControl.GetButton("MoveCamera") && !moveCameraButtonWasPressedDownOverUI;
  }

  public override bool CursorActive()
  {
    if (currentTool == null) return GetCursorMode();
    else
    {
      return currentTool.CursorActive() || GetCursorMode();
    }
  }


  public override bool KeyLock()
  {
    if (inputFieldOracle.WasAnyFieldFocusedRecently())
    {
      return true;
    }
    if (currentTool == null)
    {
      return false;
    }
    return currentTool.KeyLock();
  }

  public bool UserMainKeyLock()
  {
    return userMain.KeyLock();
  }

  public Ray GetCursorRay()
  {
    if (userMain.CursorActive())
    {
      return GetCamera().ScreenPointToRay(Input.mousePosition);
    }
    else
    {
      return GetCamera().ViewportPointToRay(navigationControls.GetDefaultSelectionPoint());
    }
  }

  float GetMaxDistance()
  {
    if (Using3DCamera())
    {
      return currentTool.GetMaxDistance();
    }
    else
    {
      return MAX_SELECTION_DISTANCE;
    }
  }

  float GetMaxGroundDistance()
  {
    if (Using3DCamera())
    {
      return currentTool.GetMaxGroundDistance();
    }
    else
    {
      return MAX_SELECTION_DISTANCE;
    }
  }

  float GetMaxThingsDistance()
  {
    if (Using3DCamera())
    {
      return currentTool.GetMaxThingsDistance();
    }
    else
    {
      return MAX_SELECTION_DISTANCE;
    }
  }


  float moveSpeed = 8;


  void MoveUpdate()
  {
    if (!userMain.CursorOverUI() || (currentTool != null && currentTool.isTriggerDown))
    {
      lookTransform.rotation = navigationControls.GetAim();
    }

    float sprintScale = (navigationControls.IsSprinting() ? 2f : 1);
    currentVelocity = navigationControls.GetVelocity() * moveSpeed * sprintScale;
    avatarTransform.Translate(currentVelocity * Time.unscaledDeltaTime);

    Vector3 avPos = avatarTransform.position;

    bool avatarPositionModified = false;

    Vector3 worldMin = gbStage.GetWorldMin();
    Vector3 worldMax = gbStage.GetWorldMax();

    if (avPos.x < worldMin.x || avPos.x > worldMax.x)
    {
      avPos.x = Mathf.Clamp(avPos.x, worldMin.x, worldMax.x);
      avatarPositionModified = true;
    }

    if (avPos.y < worldMin.y || avPos.y > worldMax.y)
    {
      avPos.y = Mathf.Clamp(avPos.y, worldMin.y, worldMax.y);
      avatarPositionModified = true;
    }

    if (avPos.z < worldMin.z || avPos.z > worldMax.z)
    {
      avPos.z = Mathf.Clamp(avPos.z, worldMin.z, worldMax.z);
      avatarPositionModified = true;
    }

    if (avatarPositionModified)
    {
      avatarTransform.position = avPos;
    }
  }

  void NextTool()
  {
    SelectToolbarIndex((curToolIndex + 1) % toolsToLoad.Length);
  }

  void PreviousTool()
  {
    int newToolIndex = curToolIndex - 1;
    if (newToolIndex < 0)
    {
      newToolIndex = toolsToLoad.Length - 1;
    }
    SelectToolbarIndex(newToolIndex);
  }

  Ray selectionRay;

  public Ray GetSelectionRay()
  {
    return selectionRay;
  }

  public Vector3 GetAzimuth()
  {
    return lookTransform.forward;
  }

  public void UpdatetReticleLabel()
  {
    string _labelText = "";
    if (hoverActor != null)
    {
      if (hoverActor)

        _labelText = lockedSelectionString + hoverActor.GetDisplayName();
      if (currentTool != null)
      {
        if (currentTool.GetReticleText() != "")
        {
          _labelText = lockedSelectionString + currentTool.GetReticleText();
        }
      }
    }
    else
    {
      if (lockedSelectionString != null) _labelText = lockedSelectionString;
    }
    reticleLabel.text = _labelText;
  }

  public override void OnCameraViewUpdate(CameraView cv)
  {
    // tooltipAnchor.SetParent(Using3DCamera() ? tooltipAnchor3D : tooltipAnchorIso);
    // tooltipAnchor.anchoredPosition = Vector2.zero;
    if (currentTool != null && currentTool.isTriggerDown)
    {
      currentTool.Trigger(false);
    }
  }

  public UserBody GetUserBody()
  {
    return navigationControls.userBody;
  }

  public Color GetAvatarTint()
  {
    return GetUserBody().currentTint;
  }

  public Transform groundHitTransform;
  public Vector3 groundHitNormal;
  string lockedSelectionString = null;

  void UpdateInvalidFeedback()
  {
    lockedSelectionString = hoverActor == null ? null : currentTool.GetInvalidActorReason(hoverActor);
    if (lockedSelectionString != null)
    {
      invalidSelectionImage.enabled = true;
      reticleAnchor.gameObject.SetActive(false);
    }
    else
    {
      invalidSelectionImage.enabled = false;
      reticleAnchor.gameObject.SetActive(true);
    }
    UpdatetReticleLabel();
  }

  bool ShouldUseCursorForSelectionRay()
  {
    return userMain.CursorActive();// && navigationControls.ShouldUseCursorForSelectionRay();
  }


  void UpdateSelectionRayAndReticle()
  {
    Vector2 reticleVec;

    if (ShouldUseCursorForSelectionRay())
    {
      selectionRay = GetCamera().ScreenPointToRay(Input.mousePosition);
      RectTransformUtility.ScreenPointToLocalPointInRectangle(hudRect, Input.mousePosition, null, out reticleVec);
      reticleFeedbackRect.anchoredPosition = reticleVec;
      reticleLabel.alignment = TMPro.TextAlignmentOptions.Left;
      reticleLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(40, 0);
    }
    else
    {
      reticleFeedbackRect.anchoredPosition = Vector2.zero;
      reticleLabel.alignment = TMPro.TextAlignmentOptions.Center;

      reticleLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
      selectionRay = GetCamera().ViewportPointToRay(navigationControls.GetDefaultSelectionPoint());
    }
  }

  void NoToolUpdate()
  {
    RaycastHit[] hits;
    float curDist = Mathf.Infinity;
    VoosActor _tempSelected = null;
    Vector3 _selectedHitPoint = Vector3.zero;
    float thingSelectionDistance = 1000.0f;
    hits = Physics.RaycastAll(selectionRay, thingSelectionDistance);
    for (int i = 0; i < hits.Length; i++)
    {
      if (hits[i].transform.GetComponent<VoosActor>() != null)
      {
        if (hits[i].distance < curDist)
        {
          curDist = hits[i].distance;
          _selectedHitPoint = hits[i].point;
          _tempSelected = hits[i].transform.GetComponent<VoosActor>();
        }

      }
    }

    if (_tempSelected != hoverActor)
    {
      UpdateHoverActor(_tempSelected);
    }
  }

  bool GroundTargetUpdate(out float dist, out Vector3 hitpoint)
  {
    Transform newGroundTransform = null;
    RaycastHit[] hits;
    Vector3 _selectedHitPoint = Vector3.zero;
    Vector3 _selectedHitNormal = Vector3.zero;
    bool groundTargetFound = false;
    dist = Mathf.Infinity;

    hits = Physics.RaycastAll(selectionRay, currentTool.GetMaxGroundDistance(), userMain.GetLayerMask());

    for (int i = 0; i < hits.Length; i++)
    {
      if (hits[i].transform.tag == GROUND_TAG || hits[i].transform.tag == WALL_TAG)
      {
        if (hits[i].distance < dist)
        {

          groundTargetFound = true;
          dist = hits[i].distance;
          _selectedHitPoint = hits[i].point;
          _selectedHitNormal = hits[i].normal;
          newGroundTransform = hits[i].transform;
        }

      }
    }

    if (groundTargetFound)
    {
      hitpoint = _selectedHitPoint;
      groundHitTransform = newGroundTransform;
      groundHitNormal = _selectedHitNormal;

      // Debug.DrawRay(hitpoint, groundHitNormal, Color.red, .2f);
    }
    else
    {
      hitpoint = Vector3.zero;
      groundHitNormal = Vector3.zero;
      groundHitTransform = null;
    }

    return groundHitTransform != null;
  }

  bool ActorTargetUpdate(out float closestDist, out Vector3 closestHitPos, out VoosActor closestActor)
  {
    RaycastHit[] hits;
    Vector3 _selectedHitPoint = Vector3.zero;
    closestDist = Mathf.Infinity;
    float thingSelectionDistance = currentTool.TargetsSpace() ? (GetMaxThingsDistance() - navigationControls.GetDistanceModifer()) : 1500.0f;

    closestActor = null;
    closestHitPos = Vector3.zero;

    hits = Physics.RaycastAll(selectionRay, thingSelectionDistance, userMain.GetLayerMask());

    // Find closest hit actor.
    for (int i = 0; i < hits.Length; i++)
    {
      // If not closer than current best, don't worry about it.
      if (hits[i].distance > closestDist) continue;

      VoosActor maybeActor = hits[i].collider.GetComponentInParent<VoosActor>();
      if (maybeActor != null && maybeActor.IsRenderableCollider(hits[i].collider))
      {
        closestDist = hits[i].distance;
        closestHitPos = hits[i].point;
        closestActor = maybeActor;
      }

    }

    return closestActor != null;
  }

  Vector3 spaceTargetUpdate()
  {
    if (Using3DCamera())
    {

      Vector3 spacepoint;
      Vector3 v1 = selectionRay.origin;
      Vector3 v2 = selectionRay.direction;
      v2.Normalize();

      //hack for third person
      float maxDistance = GetMaxDistance() - navigationControls.GetDistanceModifer();
      //

      spacepoint = v1 + v2 * maxDistance;
      return spacepoint;
    }
    else
    {
      return navigationControls.GetHorizontalPlanePoint(avatarTransform.position.y);
    }
  }

  private void Update()
  {
    if (!active) return;

    if (userMain.IsHeaderMenuActive()) return;

    RemoveDestroyedOrLockedTargets();
    UpdateOffstageTargets();
    // i wish i new why i put these here...
    navigationControls.userBody.SetPlayerVisible(true);

    if (GetTargetActorsCount() == 0 && cameraFollowingActor)
    {
      SetCameraFollowingActor(false);
    }

    if (inputControl.GetButtonDown("MoveCamera") && userMain.CursorOverUI())
    {
      moveCameraButtonWasPressedDownOverUI = true;
    }

    if (inputControl.GetButtonUp("MoveCamera"))
    {
      moveCameraButtonWasPressedDownOverUI = false;
    }

    if (!KeyLock() && !userMain.ShouldAvatarKeyLock())
    {
      MoveUpdate();
      CheckToolSlotInput();
      TriggerUpdate();

      if (inputControl.GetButtonDown("ListActors"))
      {
        userMain.ToggleActorList();
      }

      if (ActorsEditable())
      {
        ParentCheck();
        CopyCheck();

        if (inputControl.GetButtonDown("Focus"))
        {
          SetCameraFollowingActor(!GetCameraFollowingActor());
        }

        if (DeleteCheck()) return;
      }
    }

    navigationControls.SetUserBodyVelocity(currentVelocity);

    if (userMain.CursorOverUI() && !currentTool.isTriggerDown)
    {
      return;
    }

    UpdateSelectionRayAndReticle();

    if (currentTool == null)
    {
      NoToolUpdate();
      return;
    }

    if (currentTool.IsSelectionLocked()) return;

    bool groundTargetFound = false;
    float groundDist = Mathf.Infinity;
    Vector3 groundHitPoint = Vector3.zero;

    bool actorTargetFound = false;
    float actorDist = Mathf.Infinity;
    Vector3 actorHitPoint = Vector3.zero;
    VoosActor foundActor = null;

    //get actor and ground stuff
    if (currentTool.TargetsGround())
    {
      groundTargetFound = GroundTargetUpdate(out groundDist, out groundHitPoint);
    }

    if (currentTool.TargetsActors())
    {
      actorTargetFound = ActorTargetUpdate(out actorDist, out actorHitPoint, out foundActor);
    }

    // Debug.Log(groundTargetFound);

    // decide which to use if both presnet
    if (groundTargetFound && actorTargetFound)
    {
      if (actorDist <= groundDist)
      {
        groundTargetFound = false;
        actorTargetFound = true;
      }
      else
      {
        actorTargetFound = false;
        groundTargetFound = true;
      }
    }

    VoosActor actorCandidate = null;
    if (actorTargetFound)
    {
      actorCandidate = foundActor;
      hoverPosition = actorHitPoint;
    }
    else if (groundTargetFound)
    {
      actorCandidate = null;
      hoverPosition = groundHitPoint;
    }
    else if (currentTool.TargetsSpace())
    {
      hoverPosition = spaceTargetUpdate();
      actorCandidate = null;
    }

    if (actorCandidate != hoverActor)
    {
      UpdateHoverActor(actorCandidate);
    }
    currentTool.UpdatePosition(hoverPosition);

    // hack to force refresh of reticle in edge cases
    if (hoverActor == null && reticleLabel.text != "") UpdatetReticleLabel();

    UpdateInvalidFeedback();
  }

  private void UpdateOffstageTargets()
  {
    foreach (VoosActor actor in targetActors)
    {
      if (actor.GetIsOffstageEffective())
      {

        actor.SetOffstageGhostRenderableVisibleAndInteractive(true);
      }
    }
  }

  private void ParentCheck()
  {
    VoosActor actor = GetSingleTargetActor();
    if (inputControl.GetButtonDown("Parent") && actor != null)
    {
      Debug.Log(
        "parenting " + actor.GetDisplayName() + " to " +
        (hoverActor != null ? hoverActor.GetDisplayName() : "(nothing)"));

      bool autosetParent = PlayerPrefs.GetInt("moveTool-autosetSpawn", 1) == 1 ? true : false;
      MoveToolSettings.SetCurrentParentForActor(actor, hoverActor, undoStack, autosetParent);
    }
  }

  private void CopyCheck()
  {
    if (inputControl.GetButtonDown("Copy"))
    {
      TryCopy();
    }
  }

  public void TryForceSelectingActor(VoosActor actor)
  {
    if (actor == null)
    {
      SetCameraFollowingActor(false);
    }

    if (currentTool == null)
    {
      SetTargetActor(actor);
    }
    else
    {
      SetTargetActor(actor);
      currentTool.ForceUpdateTargetActor();
    }
  }


  public void TryForceAddingActor(VoosActor actor)
  {
    if (currentTool == null)
    {
      AddTargetActor(actor);
    }
    else
    {
      AddTargetActor(actor);
      currentTool.ForceUpdateTargetActor();
    }
  }

  void LateUpdate()
  {
    if (hoverActor != null)
    {
      hoverFeedback.SetVisiblity(!IsActorInTargetActors(hoverActor));


      // if (currentTool.ShowSelectedTargetFeedback())
      // {
      //   hoverFeedback.SetVisiblity(!IsActorInTargetActors(hoverActor));
      // }
      // else
      // {
      //   hoverFeedback.SetVisiblity(currentTool.ShowHoverTargetFeedback());
      // }
    }

    // if (targetFeedback != null)
    // {
    //   if (currentTool != null)
    //   {
    //     targetFeedback.SetVisiblity(currentTool.ShowSelectedTargetFeedback());
    //   }
    // }

    hoverFeedback.UpdatePosition();
    foreach (SelectionFeedback feedback in targetFeedbackList)
    {
      feedback.SetVisiblity(currentTool.ShowSelectedTargetFeedback());
      feedback.UpdatePosition();
    }
  }

  public IEnumerable<VoosActor> GetTargetActors()
  {
    return targetActors;
  }

  // public void SetTargetActors(IEnumerable<VoosActor> newActors)
  // {
  //   targetActors.Clear();
  //   OnTargetActorsUpdated?.Invoke();
  //   targetActors.AddRange(newActors);
  // }

  public int GetTargetActorsCount()
  {
    return targetActors.Count;
  }

  public bool IsActorInTargetActors(VoosActor actor)
  {
    return targetActors.Contains(actor);
  }

  void RemoveDestroyedOrLockedTargets()
  {
    if (GetTargetActorsCount() == 0) return;

    for (int i = targetActors.Count - 1; i >= 0; i--)
    {
      if (targetActors[i] == null || targetActors[i].IsLockedByAnother())
      {
        RemoveTargetActor(targetActors[i]);
      }
    }
  }

  public VoosActor GetFocusedTargetActor()
  {
    if (focusedTargetActor == null && targetActors.Count > 0)
    {
      RefreshFocusedTargetActor();
    }

    return focusedTargetActor;
  }

  private void RefreshFocusedTargetActor()
  {
    //clear null
    if (targetActors.Count > 0)
    {
      for (int i = targetActors.Count - 1; i >= 0; i--)
      {
        if (targetActors[i] == null)
        {
          targetActors.RemoveAt(i);
        }
      }
    }

    if (targetActors.Count > 0)
    {
      focusedTargetActor = targetActors[targetActors.Count - 1];
    }
    else
    {
      focusedTargetActor = null;
    }
    targetActorsChanged?.Invoke(targetActors);
  }

  public void SetFocusedTargetActor(VoosActor actor)
  {
    focusedTargetActor = actor;
  }

  public void SetTargetActor(VoosActor actor)
  {
    ClearTargetActors();

    //to support older uses of this function to "clear" the target actor with null
    if (actor == null) return;

    AddTargetActor(actor);
  }

  public void RemoveTargetActor(VoosActor actor)
  {
    if (!IsActorInTargetActors(actor)) return;

    if (actor != null && actor.GetIsOffstageEffective()) actor.SetOffstageGhostRenderableVisibleAndInteractive(false);
    targetActors.Remove(actor);
    RemoveTargetFeedbackForActor(actor);
    if (focusedTargetActor == actor)
    {
      if (targetActors.Count > 0)
      {
        focusedTargetActor = targetActors[targetActors.Count - 1];
      }
      else
      {
        focusedTargetActor = null;
      }
    }
    targetActorsChanged?.Invoke(targetActors);
  }

  void AddTargetFeedbackForActor(VoosActor actor)
  {
    SelectionFeedback feedback = Instantiate(targetFeedbackPrefab, selectionFeedbackParent.transform);
    feedback.SetActor(actor);
    targetFeedbackList.Add(feedback);
  }

  void RemoveTargetFeedbackForActor(VoosActor actor)
  {
    for (int i = 0; i < targetFeedbackList.Count; i++)
    {
      if (targetFeedbackList[i].GetActor() == actor)
      {
        targetFeedbackList[i].RequestDestroy();
        targetFeedbackList.RemoveAt(i);
        return;
      }
    }
  }

  void RemoveAllTargetFeedback()
  {
    for (int i = 0; i < targetFeedbackList.Count; i++)
    {
      targetFeedbackList[i].RequestDestroy();
    }

    targetFeedbackList.Clear();
  }

  public void AddTargetActor(VoosActor actor)
  {
    if (IsActorInTargetActors(actor)) return;

    targetActors.Add(actor);
    SetFocusedTargetActor(actor);
    AddTargetFeedbackForActor(actor);
    targetActorsChanged?.Invoke(targetActors);
  }

  public void ClearTargetActors()
  {
    foreach (VoosActor actor in targetActors)
    {
      if (actor != null && actor.GetIsOffstageEffective()) actor.SetOffstageGhostRenderableVisibleAndInteractive(false);
    }
    targetActors.Clear();
    SetFocusedTargetActor(null);
    RemoveAllTargetFeedback();
    targetActorsChanged?.Invoke(targetActors);
  }

  public VoosActor GetSingleTargetActor()
  {
    if (GetTargetActorsCount() != 1) return null;
    return GetFocusedTargetActor();
  }

  public VoosActor GetFirstTargetActor()
  {
    if (GetTargetActorsCount() == 0) return null;
    return targetActors[0];
  }

  void RemoveOffscreenTargetActors()
  {
    RemoveDestroyedOrLockedTargets();

    if (GetTargetActorsCount() == 0) return;

    for (int i = targetActors.Count - 1; i >= 0; i--)
    {
      if (!targetActors[i].IsCenterOnScreen(Camera.main))
      {
        RemoveTargetActor(targetActors[i]);
      }
    }
  }

  public void DeleteTargetActors()
  {
    VoosActor builtin = targetActors.FirstOrDefault(a => a.IsBuiltinActor());
    if (builtin != null)
    {
      popups.Show($"Sorry, built-in actors such as {builtin.GetDisplayName()} cannot be deleted.", "OK");
    }
    var deletableActors = targetActors.Where(a => a != null && !a.IsLockedByAnother() && !a.IsBuiltinActor()).ToList();
    if (deletableActors.Count == 0) return;
    var actorStates = engine.SaveActorHierarchy(deletableActors);
    var label = deletableActors.Count > 1 ? $"Delete {deletableActors.Count} actors" : $"Delete {deletableActors[0].GetDisplayName()}";

    undoStack.Push(new UndoStack.Item
    {
      actionLabel = label,
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () =>
      {
        foreach (var state in actorStates)
        {
          ActorUndoUtil.GetValidActorThen(
            engine, state.name,
            validActor => this.DeleteActor(validActor));
        }
      },
      undo = () => engine.RestoreActorHierarchy(actorStates)
    });

    SetCameraFollowingActor(false);
  }

  bool DeleteCheck()
  {
    if (inputControl.GetButtonDown("Delete") && GetTargetActorsCount() != 0)
    {
      DeleteTargetActors();
      return true;
    }

    return false;
  }

  public void DeleteActor(VoosActor actor)
  {
    bool cannotDelete = actor == null || actor.IsLockedByAnother() || actor.IsBuiltinActor();

    if (cannotDelete) return;

    Vector3 pos = actor.transform.position;
    effects.Trigger("Explosion_FX", pos, Quaternion.identity, actor.GetIsOffstageEffective());
    RemoveTargetActor(actor);
    engine.DestroyActor(actor);
  }


  public bool IsCodeViewOpen()
  {
    if (!GetLogicSidebar().IsOpenedOrOpening())
    {
      return false;
    }
    return GetLogicSidebar().IsCodeViewOpen();
  }

  public void QuickRotateTargetActors()
  {
    IEnumerable<VoosActor> actors = GetTargetActors();
    if (GetTargetActorsCount() == 0) return;

    Vector3 averagePosition = Vector3.zero;
    foreach (VoosActor actor in GetTargetActors())
    {
      averagePosition += actor.GetPosition();
    }
    averagePosition /= GetTargetActorsCount();

    Dictionary<string, Transforms.TransformUndoState> undoTransformStates =
        new Dictionary<string, Transforms.TransformUndoState>();
    Dictionary<string, Transforms.TransformUndoState> redoTransformStates =
        new Dictionary<string, Transforms.TransformUndoState>();
    foreach (VoosActor actor in GetTargetActors())
    {
      undoTransformStates[actor.GetName()] =
          new Transforms.TransformUndoState(actor.GetPosition(), actor.GetRotation(), actor.GetTransformParent());
      redoTransformStates[actor.GetName()] = GetQuickRotateTransformState(actor, averagePosition);
    }

    undoStack.PushUndoForMany(engine,
        actors,
        $"Rotate",
        redoActor =>
        {
          redoTransformStates[redoActor.GetName()].PushTo(redoActor);
        },
        undoActor =>
        {
          undoTransformStates[undoActor.GetName()].PushTo(undoActor);
        });
  }

  private Transforms.TransformUndoState GetQuickRotateTransformState(VoosActor actor, Vector3 pivot)
  {
    Rigidbody targetRigidbody = actor.GetComponent<Rigidbody>();
    if (targetRigidbody != null)
    {
      targetRigidbody.angularVelocity = Vector3.zero;
    }

    Quaternion newRotation = actor.GetRotation() * Quaternion.Euler(0, QUICK_ROTATE_AMOUNT, 0);
    Vector3 diff = actor.GetPosition() - pivot;
    Vector3 newPosition = Quaternion.AngleAxis(QUICK_ROTATE_AMOUNT, Vector3.up) * diff + pivot;

    return new Transforms.TransformUndoState(newPosition, newRotation, actor.GetTransformParent());
  }

  public void ShowCodeEditor(string behaviorUri, VoosEngine.BehaviorLogItem? error = null)
  {
    SelectToolbarIndex(LOGIC_TOOL_INDEX);
    GetLogicSidebar().SetToCodeView(behaviorUri, error);
  }
}
