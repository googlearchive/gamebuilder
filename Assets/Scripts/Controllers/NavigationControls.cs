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


// controls moving around and looking around for all four current camera views
public class NavigationControls : MonoBehaviour, PlayerBody.ControllerInput
{

  //lets get FPS working first
  CameraView cameraView = CameraView.FirstPerson;

  [SerializeField] UnityEngine.Audio.AudioMixerSnapshot closeAudioMixSnapshot;
  [SerializeField] UnityEngine.Audio.AudioMixerSnapshot distantAudioMixSnapshot;
  [SerializeField] RectTransform mainRect;
  [SerializeField] Transform topDownCamTransform;
  [SerializeField] Transform isoCamTransform;
  [SerializeField] Transform firstPersonCamTransform;
  [SerializeField] Transform firstPersonPhotoAnchor;
  [SerializeField] Transform thirdPersonPhotoAnchor;
  [SerializeField] Transform isoPhotoAnchor;

  //third person one is heavy
  [SerializeField] Transform thirdPersonPivotTransform;
  [SerializeField] Transform thirdPersonCamTransform;
  [SerializeField] Transform thirdPersonSphereCastOriginTransform;

  CameraViewController cameraViewController;

  public Camera targetCamera;
  public UserMain userMain;

  [SerializeField] EditMain editMain;
  [SerializeField] PlayMain playMain;
  [HideInInspector] public InputControl inputControl;

  ThirdPersonCameraView thirdPersonCameraViewController;
  FPSCameraView fpsCameraViewController;
  IsometricCameraView isometricCameraViewController;
  ActorDrivenCameraView actorDrivenCameraViewController;

  // public Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

  //this handles the animations - PlayerBody expects it ot be in this file
  public UserBody userBody;

  public static float MOVE_SPEED = 5;

  GameBuilderStage gbStage;
  private InputFieldOracle inputFieldOracle;
  float lastSwitchTime = -1f;

  // Temporary requested camera offset (for camera shake effects).
  Vector3 requestedTempOffset = Vector3.zero;

  public enum Mode
  {
    Grounded,
    Fly
  }
  const float ISO_ROTATE_MOD = 5f;

  int isoRotation = 0;
  int ISO_ROTATION_LENGTH = 8;

  int fixedUpdatesSinceLastUpdate;

  void Awake()
  {
    Util.FindIfNotSet(this, ref inputFieldOracle);
    Util.FindIfNotSet(this, ref gbStage);
  }

  public Transform GetPhotoAnchor()
  {
    switch (cameraView)
    {
      case CameraView.FirstPerson:
        return firstPersonPhotoAnchor;
      case CameraView.ThirdPerson:
        return thirdPersonPhotoAnchor;
      case CameraView.Isometric:
        return isoPhotoAnchor;
      default:
        return isoPhotoAnchor;
    }
  }

  public bool HasMoveInput()
  {
    return !inputFieldOracle.WasAnyFieldFocusedRecently() && inputControl.GetMoveAxes().sqrMagnitude > 0 && MoveActive();
  }

  public Vector3 GetPlayerScale()
  {
    return playMain.GetPlayerScale();
  }

  public LayerMask GetLayerMask()
  {
    return userMain.GetLayerMask();
  }
  //Called by user main
  public void Setup()
  {
    inputControl = userMain.GetInputControl();

    thirdPersonCameraViewController = new ThirdPersonCameraView();
    thirdPersonCameraViewController.Setup(this, thirdPersonCamTransform);
    thirdPersonCameraViewController.SetupThirdPersonTransforms(thirdPersonPivotTransform, thirdPersonSphereCastOriginTransform);

    fpsCameraViewController = new FPSCameraView();
    fpsCameraViewController.Setup(this, firstPersonCamTransform);

    fpsCameraViewController.SetTargetPositionAnchors(userBody.transform, editMain.lookTransform);

    isometricCameraViewController = new IsometricCameraView();
    isometricCameraViewController.Setup(this, isoCamTransform);

    actorDrivenCameraViewController = new ActorDrivenCameraView();
    GameObject actionDrivenCameraChild = new GameObject();
    actionDrivenCameraChild.transform.SetParent(gameObject.transform, false);
    actorDrivenCameraViewController.Setup(this, actionDrivenCameraChild.transform);

    cameraViewController = isometricCameraViewController;

    LoadCameraSettings();
  }

  void LoadCameraSettings()
  {
    SetCameraView((CameraView)gbStage.GetInitialCameraMode());
    SetViewRotation(gbStage.GetIsoCamRotationIndex());
  }

  public void SetMode(Mode _newmode)
  {
    thirdPersonCameraViewController.SetMode(_newmode);
    fpsCameraViewController.SetMode(_newmode);
  }

  public void UpdateTargetThirdPersonPivot(Transform newTargetPivot)
  {
    thirdPersonCameraViewController.SetTargetPivotAnchor(newTargetPivot);
  }

  public bool hidingPlayerLayer;
  public void ToggleCullingMask(bool on)
  {
    hidingPlayerLayer = on;
    userMain.UpdateCameraMask();
  }

  public void ToggleEditCullingMask(bool on)
  {
    int curMask = targetCamera.cullingMask;
    curMask = curMask.WithBit(LayerMask.NameToLayer("OffstageGhost"), on);
    targetCamera.cullingMask = curMask.WithBit(LayerMask.NameToLayer("EditModeOnly"), on);
  }

  public float GetDistanceModifer()
  {
    if (cameraView != CameraView.ThirdPerson) return 0;
    else
    {
      return thirdPersonCameraViewController.GetZOffset();
    }
  }

  public Vector3 GetHorizontalPlanePoint(float height, bool useCenter = false)
  {
    Ray ray = useCenter ? targetCamera.ViewportPointToRay(new Vector2(.5f, .5f)) : userMain.GetCursorRay();


    float enter;
    Plane tempPlane = new Plane(Vector3.up, Vector3.up * height);
    if (tempPlane.Raycast(ray, out enter))
    {
      return ray.GetPoint(enter);
    }
    else
    {
      //can't find ground
      return Vector3.zero;
    }


  }

  public VoosActor GetCameraLockActor()
  {
    if (userMain.InEditMode()) return editMain.GetFocusedTargetActor();
    else return null;
  }

  public CameraView GetCameraView()
  {
    return cameraView;
  }

  public virtual bool CursorActive()
  {
    return cameraViewController.CursorActive();
  }

  public void UpdateRotationValues(Quaternion _newrotation)
  {
    if (cameraView == CameraView.FirstPerson) fpsCameraViewController.UpdateRotationValues(_newrotation);
    else if (cameraView == CameraView.ThirdPerson) thirdPersonCameraViewController.UpdateRotationValues(_newrotation);
  }

  public Vector2 GetDefaultSelectionPoint()
  {
    return cameraViewController.GetDefaultSelectionPoint();
  }

  void FixedUpdate()
  {
    fixedUpdatesSinceLastUpdate++;
  }

  public bool GetCameraFollowingActor()
  {
    return userMain.InEditMode() ? editMain.GetCameraFollowingActor() : false;
  }

  public void SetCameraFollowingActor(bool on)
  {
    editMain.SetCameraFollowingActor(on);
  }

  void Update()
  {
    //userMain.ToggleReticles(true)

    if (!userMain.CursorOverUI())
    {
      if (inputControl.GetZoom() != 0
         && cameraView == CameraView.Isometric)
      {
        float updatedZoom = cameraViewController.GetZoom() - inputControl.GetZoom();
        cameraViewController.SetZoom(Mathf.Max(0, updatedZoom));
      }

      //if(userMain.KeyLock)
      if (inputControl.GetButtonDown("MoveCamera") && editMain.GetCameraFollowingActor())
      {
        editMain.SetCameraFollowingActor(false);
      }

      if (inputControl.GetButton("RotateCamera") && cameraView == CameraView.Isometric)
      {
        isometricCameraViewController.RotateView(inputControl.GetMouseAxes().x * ISO_ROTATE_MOD);
      }
    }

    cameraViewController.ControllerUpdate(fixedUpdatesSinceLastUpdate);
    fixedUpdatesSinceLastUpdate = 0;

    // Temporary band-aid guard, since this userBody assumption is pretty wide-spread.
    if (userBody != null)
    {
      if (MoveActive())
      {
        userBody?.SetSprint(IsSprinting());
        // userBody?.SetCrouch(inputControl.GetButton("Crouch"));
      }
      else
      {
        userBody?.SetSprint(false);
        userBody?.SetCrouch(false);
      }
    }
  }

  public void SetUserBodyVelocity(Vector3 _velocity)
  {
    // Temporary band-aid guard, since this userBody assumption is pretty wide-spread.
    if (userBody == null)
    {
      return;
    }

    Vector3 relativeVelocity = Quaternion.Inverse(Quaternion.LookRotation(userBody.transform.forward)) * _velocity; //avatarMain.avatarTransform.InverseTransformDirection(realVelocity);

    //makes it smaller for the animator
    float x = relativeVelocity.x / MOVE_SPEED;
    float y = relativeVelocity.z / MOVE_SPEED;
    userBody?.UpdateVelocity(new Vector2(x, y));
  }

  internal bool TryCaptureCursor()
  {

    if (userMain.InEditMode()) return false;
    else
    {
      return cameraViewController.TryCaptureCursor();
    }

    // if (cameraView == CameraView.ActorDriven)
    // {
    //   return cameraViewController.TryCaptureCursor();
    // }
    // else
    // {
    //   return false;
    // }
  }

  public bool CameraCapturedCursor()
  {
    if (userMain.InEditMode()) return false;
    else
    {
      return cameraViewController.IsCursorCaptured();
    }
    // if (cameraView == CameraView.ActorDriven)
    // {
    // }
    // else
    // {
    //   return false;
    // }
  }

  internal bool TryReleaseCursor()
  {
    if (userMain.InEditMode()) return false;
    else
    {
      return cameraViewController.TryReleaseCursor();
    }
  }

  public void SetGrounded(bool on)
  {
    userBody?.SetGrounded(on);
  }

  public void SetCameraView(CameraView newView)
  {
    // Debug.Log("Setting camera view: " + cameraView + " -> " + newView);
    /*  Debug.Log("SETTING " + cameraView + " " + newView);
     if (cameraView == newView)
     {
       return;
     }
     Debug.Log("continuing..."); */

    // Guard against switching to actor-driven camera when there is no camera actor.
    // Shouldn't happen, but just to be safe.

    if (newView == CameraView.ActorDriven && actorDrivenCameraViewController.GetCameraActor() == null)
    {
      Debug.LogWarning("Can't switch to actor-driven camera. No camera actor.");
      return;
    }

    CameraView oldView = cameraView;
    CameraViewController oldController = cameraViewController;
    cameraView = newView;

    gbStage.SetInitialCameraMode((GameBuilderStage.CameraMode)cameraView);

    // Log how long user spent in previous mode
    if (lastSwitchTime > 0f)
    {
      float secondsSpent = Time.realtimeSinceStartup - lastSwitchTime;
      long msSpent = (long)(Mathf.FloorToInt(secondsSpent * 1000f));
      if (msSpent > 100)
      {
        Util.Log($"Spent {msSpent}ms in {oldView.ToString()}");
      }
    }
    lastSwitchTime = Time.realtimeSinceStartup;

    switch (cameraView)
    {
      case CameraView.ThirdPerson:
        cameraViewController = thirdPersonCameraViewController;
        break;
      case CameraView.FirstPerson:
        cameraViewController = fpsCameraViewController;
        break;
      case CameraView.Isometric:
        cameraViewController = isometricCameraViewController;
        break;
      case CameraView.ActorDriven:
        cameraViewController = actorDrivenCameraViewController;
        break;
      default:
        throw new System.Exception($"Unsupported CameraView: {cameraView}");
    }
    cameraViewController.SetCamera();

    if ((cameraView == CameraView.FirstPerson || cameraView == CameraView.ThirdPerson)
    && (oldView == CameraView.FirstPerson || oldView == CameraView.ThirdPerson)
    && oldController != null)
    {
      cameraViewController.SetLookRotation(oldController.GetLookRotation());
    }


    if (cameraView == CameraView.Isometric)
    {

      distantAudioMixSnapshot.TransitionTo(.1f);
    }
    else
    {
      closeAudioMixSnapshot.TransitionTo(.1f);
    }

  }

  public void RotateViewByIncrement()
  {
    SetViewRotation((isoRotation + 1) % ISO_ROTATION_LENGTH);
    gbStage.SetIsoCamRotationIndex(isoRotation);
  }

  void SetViewRotation(int n)
  {
    isoRotation = n;
    isometricCameraViewController.RotateViewByIncrement(isoRotation);
  }

  public float GetViewZoom()
  {
    return cameraViewController.GetZoom();
  }

  public void SetViewZoom(float f)
  {
    cameraViewController.SetZoom(f);
  }

  public bool MouseLookActive()
  {
    return CameraCapturedCursor() ||
      (inputControl.GetButton("MoveCamera") && !userMain.CursorOverUI());
    //return inputControl.GetButton("MoveCamera") && !userMain.CursorOverUI();
  }

  // public bool CursorMouseLookActive()
  // {
  //   return userMain.IsAvatarMouseLookActive();
  // }

  bool MoveActive()
  {
    return !userMain.ShouldAvatarKeyLock() && !userMain.KeyLock() && !userMain.IsAvatarKeyLock();
  }

  public Quaternion GetAim()
  {
    return cameraViewController.GetRotation();
  }

  public bool IsSprinting()
  {
    return inputControl.GetButton("Sprint");
  }

  Vector3 PlayerBody.ControllerInput.GetWorldSpaceThrottle()
  {
    return this.GetVelocity();
  }

  Vector3 PlayerBody.ControllerInput.GetInputThrottle()
  {
    return inputControl.GetMoveAxes();
  }

  Vector2 PlayerBody.ControllerInput.GetLookAxes()
  {
    // If the cursor is active, the camera should not pan around as the user
    // moves the mouse, hence the look axes should be 0.
    return CursorActive() ? Vector2.zero : inputControl.GetLookAxes();
  }

  public Vector3 GetVelocity()
  {
    if (!MoveActive()) return Vector3.zero;
    return cameraViewController.GetVelocity();
  }

  public bool requestGrounding = false;

  public Vector3 GetAvatarMoveVector()
  {
    return cameraViewController.GetAvatarMoveVector();
  }

  public void SetUserBody(UserBody _userbody)
  {
    userBody = _userbody;
  }

  public void RequestGrounding()
  {
    requestGrounding = true;
  }


  public CameraInfo GetCameraInfo()
  {
    return new CameraInfo(targetCamera.fieldOfView);
  }


  public bool IsGroundingRequested()
  {
    if (requestGrounding)
    {
      requestGrounding = false;
      return true;
    }
    return false;
  }

  public Vector3 GetAimOrigin()
  {
    // If user body is missing just get through it
    if (userBody == null) return Vector3.zero;

    return cameraViewController.GetAimOrigin();
  }

  void LateUpdate()
  {
    // Temporary band-aid guard, since this userBody assumption is pretty wide-spread.
    if (userBody != null)
    {
      cameraViewController.ControllerLateUpdate();
    }

    // Apply the requested offset, if any.
    if (requestedTempOffset != Vector3.zero)
    {
      cameraViewController.GetMainTransform().transform.Translate(requestedTempOffset, Space.Self);
      requestedTempOffset = Vector3.zero;
    }

    // If we're using an actor-based camera and it no longer has an actor
    // (for example, it was deleted), switch view.
    if (cameraViewController == actorDrivenCameraViewController &&
        actorDrivenCameraViewController.GetCameraActor() == null)
    {
      SetCameraView(CameraView.Isometric);
    }
  }

  public bool GetKeyDown(string keyName)
  {
    bool keysActive = !userMain.KeyLock();
    bool moveActive = MoveActive();
    switch (keyName)
    {
      case PlayerBody.VKEY_JUMP:
        return moveActive && inputControl.GetButtonDown("Jump");
      case PlayerBody.VKEY_PRIMARY_ACTION:
        return keysActive && inputControl.GetButtonDown("Action1");
      case PlayerBody.VKEY_SECONDARY_ACTION:
        return keysActive && inputControl.GetButtonDown("Action2");
      default:
        return keysActive && Input.GetKeyDown(keyName);
    }
  }

  public bool GetKeyHeld(string keyName)
  {
    bool keysActive = !userMain.KeyLock();
    bool moveActive = MoveActive();
    switch (keyName)
    {
      case PlayerBody.VKEY_JUMP:
        return moveActive && inputControl.GetButton("Jump");
      case PlayerBody.VKEY_PRIMARY_ACTION:
        return keysActive && inputControl.GetButton("Action1");
      case PlayerBody.VKEY_SECONDARY_ACTION:
        return keysActive && inputControl.GetButton("Action2");
      default:
        return keysActive && Input.GetKey(keyName);
    }
  }

  public void SwitchToActorDrivenCamera(VoosActor actor)
  {
    actorDrivenCameraViewController.SetCameraActor(actor);
    SetCameraView(CameraView.ActorDriven);
  }

  public VoosActor GetActorDrivenCameraActor()
  {
    return actorDrivenCameraViewController.GetCameraActor();
  }

  public void RequestTemporaryCameraOffset(Vector3 offset)
  {
    requestedTempOffset = offset;
  }

  // internal bool ShouldUseCursorForSelectionRay()
  // {
  //   return cameraViewController.UsingClickInCameraView();
  // }
}

public abstract class CameraViewController
{
  protected NavigationControls navigationControls;
  protected Transform mainTransform;
  protected Vector3 velocity;
  protected Quaternion rotation;

  public Vector3 GetVelocity() { return velocity; }
  public Quaternion GetRotation() { return rotation; }
  public Transform GetMainTransform() { return mainTransform; }

  public abstract Quaternion GetAimRotation();
  public abstract Vector3 GetAimOrigin();

  public abstract void ControllerUpdate(int fixedUpdatesSinceLastUpdate);
  public abstract void SetZoom(float f);
  public abstract float GetZoom();
  public abstract void MoveCameraToActor(VoosActor actor);

  public virtual void Setup(NavigationControls _nav, Transform _mainT)
  {
    navigationControls = _nav;
    mainTransform = _mainT;
  }

  public virtual Vector2 GetLookRotation()
  {
    return Vector2.zero; ;
  }


  public virtual void SetLookRotation(Vector2 newRotation)
  {

  }

  public virtual void ControllerLateUpdate() { }

  public abstract void SetCamera();

  public abstract Vector2 GetDefaultSelectionPoint();


  public abstract Vector3 GetAvatarMoveVector();
  protected bool cursorActive = false;

  public virtual bool CursorActive()
  {
    return cursorActive;
  }

  // public void SetCursorActive(bool value)
  // {
  //   cursorActive = value;
  // }

  // public bool GetCursorActive()
  // {
  //   return cursorActive;
  // }

  public virtual bool TryCaptureCursor()
  {
    if (cursorActive)
    {
      cursorActive = false;
      return true;
    }
    else
    {
      return false;
    }
  }

  public virtual bool IsCursorCaptured()
  {
    return !cursorActive;
  }

  public virtual bool TryReleaseCursor()
  {
    if (!cursorActive)
    {
      cursorActive = true;
      return true;
    }
    else
    {
      return false;
    }
  }
}

public enum CameraView
{
  ThirdPerson,
  Isometric,
  FirstPerson,
  TopDown,
  ActorDriven,
};

public struct CameraInfo
{
  public float zoom;

  public CameraInfo(float _zoom)
  {
    zoom = _zoom;
  }




}