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

// Manages the player body, ie. "play mode" physical representation of the
// player, responding to a Controller. Things like jumping physics, ground
// checks, enforcing throttle, rotating transforms. TODO I think we're gonna end
// up merging this with VoosActor...since these things should be doable from
// script. Such as, setting desired velocity for NPCs.
public class PlayerBody : MonoBehaviour, IBipedDriver
{
  // All key names must be kept in sync with keyboard.js.txt
  // Special "virtual" keys:
  public const string VKEY_JUMP = "@jump";
  public const string VKEY_PRIMARY_ACTION = "@pri";
  public const string VKEY_SECONDARY_ACTION = "@sec";
  static string[] ScriptableKeyNames = new string[] {
    "1", "2", "3", "4", "5", "6", "7", "8", "9", "0",
    "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
    "space", "enter", "return", "backspace",
    "up", "down", "left", "right",
    "[1]", "[2]", "[3]", "[4]", "[5]", "[6]", "[7]", "[8]", "[9]", "[0]",
    "mouse 0",
    VKEY_JUMP, VKEY_PRIMARY_ACTION, VKEY_SECONDARY_ACTION
  };

  public interface EventHandler
  {
    void OnJumped();
    void OnLanded();

    // Probably because we're not grounded.
    void OnJumpDenied();

    void OnDamaged();
    void OnRespawned();
    void OnDied();

    void OnTintChanged(Color tint);
  }

  // The actual input signals, like move forward, jump, etc.
  public interface ControllerInput
  {
    Quaternion GetAim();
    Vector3 GetAimOrigin();
    Vector3 GetInputThrottle();
    Vector3 GetWorldSpaceThrottle();
    Vector2 GetLookAxes();
    bool GetKeyDown(string keyName);
    bool GetKeyHeld(string keyName);
    bool IsSprinting();
  }

  // An exclusive user/controller of this player body.
  public interface Controller
  {
    // Just for debugging. Doesn't need to be unique.
    string GetName();

    // This is called every frame, and you can safely return null as you wish to
    // effectively disable controls.
    ControllerInput GetInput();

    // You can return null if you don't want to handle events.
    EventHandler GetEventHandler();
  }

  [SerializeField] PlayerBodyParts parts;

  // Whether or not the mouse button is down, as reported to scripting.
  private bool reportedMouseDown = false;

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
  const float MOUSEWHEEL_FACTOR = 0.033f;
#else
  const float MOUSEWHEEL_FACTOR = 1.000f;
#endif

  [System.Serializable]
  public struct InputStateForScript
  {
    // Position of mouse in UI-space coordinates.
    public float mouseX, mouseY;
    // True if mouse button is currently being held.
    public bool mouseButtonIsPressed;
    // True if mouse button was just pressed on this frame.
    public bool mouseButtonJustPressed;
    // True if mouse button was just released on this frame.
    public bool mouseButtonJustReleased;
    // Ray going from the camera through the mouse position.
    public Vector3 mouseRayOrigin;
    public Vector3 mouseRayDirection;
    // Mouse wheel movement this frame, raw (not adjusted by sensitivity setting).
    public float mouseWheelRaw;
    // Mouse wheel movement this frame (adjusted by sensitivity setting)
    public float mouseWheel;
    // Keys that were just pressed on this frame.
    public string[] keysJustPressed;
    // Keys that are being held in this frame.
    public string[] keysHeld;
    // Keys that were just released in this frame.
    public string[] keysJustReleased;
  }

  VoosActor voosActor;
  Quaternion lastAimRotation = Quaternion.identity;
  GameUiMain gameUiMain;

  Rigidbody rigidbodyCache;
  Rigidbody rb
  {
    get { return Util.GetAndCache(this, ref rigidbodyCache); }
  }

  // The local controller that is controlling this, if any.
  Controller controller = null;

  Rigidbody groundRigidbody = null;

  float isGroundedUpdateTime = 0f;
  bool isGrounded = false;

  Vector3 lastPosition = Vector3.zero;
  Vector3 inferredVelocity = Vector3.zero;
  UserMain userMain;
  bool hasInputForScript;
  InputStateForScript inputStateForScript;
  HashSet<string> keysHeldAsReportedToScript = new HashSet<string>();

  //  Just for landing event
  bool wasTouchingGround = false;

  public void SetParts(PlayerBodyParts parts)
  {
    this.parts = parts;

    // We only use the collider as a reference for our box overlap test.
    parts.groundCheckCollider.gameObject.SetActive(false);
  }

  public Transform GetThirdPersonPivot()
  {
    return parts.thirdPersonPivot;
  }

  public Transform GetAvatarTransform()
  {
    return parts.avatarTransform;
  }

  public Vector3 GetVelocity()
  {
    if (rb == null)
    {
      return Vector3.zero;
    }
    else
    {
      return rb.velocity;
    }
  }

  public void StartControlling(Controller newController)
  {
    GetVoosActor().RequestOwnership();

    // TODO technically we need to confirm that we have locking ownership...

    Debug.Assert(parts != null, $"{newController.GetName()} tried to start controlling PlayerBody {this.name}, but we didn't have body parts set.");
    this.controller = newController;
    Debug.Log("OK Controller " + newController.GetName() + " now controls PlayerBody " + this.name);

    // Repropagate stateful events for the benefit of the new controller.
    OnTintChanged(GetVoosActor().GetTint());
  }

  public void StopControlling(Controller supposedController)
  {
    if (this.controller == supposedController)
    {
      this.controller = null;
    }
    else
    {
      Debug.LogError("Controller " + supposedController.GetName() + " tried to stop controlling, but only the current controller, " + this.controller.GetName() + ", can stop controlling PlayerBody " + this.name);
    }
  }

  private void Awake()
  {
    voosActor = GetComponent<VoosActor>();
    Debug.Assert(voosActor != null);
    Util.FindIfNotSet(this, ref gameUiMain);
  }

  void OnEnable()
  {
    voosActor.GetEngine().onBeforeVoosUpdate += OnBeforeVoosUpdate;
  }

  void OnDisable()
  {
    if (voosActor != null && voosActor.GetEngine() != null)
    {
      voosActor.GetEngine().onBeforeVoosUpdate -= OnBeforeVoosUpdate;
    }
  }

  void OnBeforeVoosUpdate()
  {
    if (this == null) return;

    // Queue input messages ASAP!!
    if (controller != null && controller.GetInput() != null)
    {
      hasInputForScript = true;
      PrepareInputForScript(controller.GetInput());
    }
    else
    {
      hasInputForScript = false;
    }
  }

  public VoosActor GetVoosActor()
  {
    return voosActor;
  }

  private static Collider[] QueryGroundResultsTemp = new Collider[10];

  bool ComputeIsTouchingGround(out Rigidbody groundRigidbody)
  {
    Vector3 worldCenter = parts.groundCheckCollider.transform.TransformPoint(parts.groundCheckCollider.center);
    Vector3 worldLossyScale = parts.groundCheckCollider.transform.lossyScale;
    Util.AssertUniformScale(worldLossyScale);
    float worldRadius = worldLossyScale.x * parts.groundCheckCollider.radius;
    int numHits = Physics.OverlapSphereNonAlloc(worldCenter, worldRadius, QueryGroundResultsTemp, -1, QueryTriggerInteraction.Ignore);

    for (int i = 0; i < numHits; i++)
    {
      Collider col = QueryGroundResultsTemp[i];
      if (col != null && !col.transform.IsChildOf(this.transform))
      {
        groundRigidbody = col.attachedRigidbody;
        // DebugLabel.Draw(transform.position, $"Ground touching {col.name}");
        return true;
      }
    }

    groundRigidbody = null;
    return false;
  }

  void UpdateGroundIfOutDated()
  {
    float updateOrFixedUpdateStartTime = Mathf.Max(Time.time, Time.fixedTime);
    float eps = 1e-3f;
    if (isGroundedUpdateTime < updateOrFixedUpdateStartTime - eps)
    {
      isGrounded = ComputeIsTouchingGround(out groundRigidbody);
      isGroundedUpdateTime = updateOrFixedUpdateStartTime;
    }
  }

  // Effectively a cache, valid since the last FixedUpdate. Would rather not expose the expensive function (above).
  public bool GetIsTouchingGround()
  {
    UpdateGroundIfOutDated();
    return isGrounded;
  }

  public Rigidbody GetGroundRigidbody()
  {
    UpdateGroundIfOutDated();
    return groundRigidbody;
  }

  private void UpdateLandedEvent(bool isTouchingGround)
  {
    if (!wasTouchingGround && isTouchingGround)
    {
      controller?.GetEventHandler()?.OnLanded();
    }

    // Infer jump for biped animation..
    if (!voosActor.IsLocallyOwned())
    {
      if (wasTouchingGround && !isTouchingGround)
      {
        onJump?.Invoke();
      }
    }

    wasTouchingGround = isTouchingGround;
  }

  private void Update()
  {
    userMain = userMain ?? GameObject.FindObjectOfType<UserMain>();
    if (controller != null && controller.GetInput() != null)
    {
      lastAimRotation = controller.GetInput().GetAim();
    }

    // Update inferred velocity
    inferredVelocity = (transform.position - lastPosition) / Time.deltaTime;
    lastPosition = transform.position;
  }

  // This is here in order to allow the user to rotate a player controllable actor in edit mode
  // without it fighting back to reset the rotation. You'd think that's a simple problem, but...
  // it's not. See, the camera drives the aim direction; the player controller script gets the
  // aim direction and sets its rotation to match the aim, so as you rotate it, you are setting
  // its rotation but not its aim, so it will fight back and reset the rotation to match the aim.
  // We could make the aim match the rotation but that leads to CHAOS, because scripts
  // may be doing math with the aim to get to the rotation, and if you try to go the other way
  // it will create destructive feedback with that logic, causing the actor to spin uncontrollably.
  // SO... the only right way to do this would be for us to rotate the actor AND the thing that
  // sets its aim at the same time (NavigationControls camera or the actor-based camera), but
  // that would require us to tell JavaScript "hey, adjust the aim because we're rotating
  // this in edit mode" which might be another terrible box of surprises...
  public void HackAdjustLastAimRotation(Quaternion newAimRotation)
  {
    lastAimRotation = newAimRotation;
  }

  private void FixedUpdate()
  {
    if (GetVoosActor()?.GetIsRunning() == false)
    {
      return;
    }

    bool isTouchingGround = ComputeIsTouchingGround(out groundRigidbody);

    UpdateLandedEvent(isTouchingGround);
  }

  void PrepareInputForScript(ControllerInput input)
  {
    if (input == null)
    {
      return;
    }

    if (userMain == null)
    {
      // Not ready yet.
      return;
    }

    inputStateForScript = new InputStateForScript();

    if (input.GetKeyDown(PlayerBody.VKEY_JUMP))
    {
      // V1 name:
      voosActor.EnqueueMessage("JumpTriggered");
      // V2 name:
      voosActor.EnqueueMessage("Jump");
    }

    if (input.GetKeyDown(PlayerBody.VKEY_PRIMARY_ACTION))
    {
      // V1 name:
      voosActor.EnqueueMessage("Action1Triggered");
      // V2 name:
      voosActor.EnqueueMessage("PrimaryAction");
    }

    if (input.GetKeyDown(PlayerBody.VKEY_SECONDARY_ACTION))
    {
      // V1 name:
      voosActor.EnqueueMessage("Action2Triggered");
      // V2 name:
      voosActor.EnqueueMessage("SecondaryAction");
    }

    List<string> keysHeld = new List<string>();
    List<string> keysJustPressed = new List<string>();
    List<string> keysJustReleased = new List<string>();

    foreach (string key in ScriptableKeyNames)
    {
      bool isDown = input.GetKeyDown(key) && !keysHeldAsReportedToScript.Contains(key);
      bool isUp = !input.GetKeyHeld(key) && keysHeldAsReportedToScript.Contains(key);
      bool isHeld = input.GetKeyHeld(key);

      if (isHeld)
      {
        keysHeldAsReportedToScript.Add(key);
      }
      else
      {
        keysHeldAsReportedToScript.Remove(key);
      }

      if (isDown)
      {
        voosActor.EnqueueMessage("KeyDown", $"{{\"keyName\": \"{key}\"}}");
        keysJustPressed.Add(key);
      }
      if (isHeld)
      {
        voosActor.EnqueueMessage("KeyHeld", $"{{\"keyName\": \"{key}\"}}");
        keysHeld.Add(key);
      }
      if (isUp)
      {
        voosActor.EnqueueMessage("KeyUp", $"{{\"keyName\": \"{key}\"}}");
        keysJustReleased.Add(key);
      }
    }

    bool mouseIsDown = Input.GetMouseButton(0) && !userMain.CursorOverUI() && !userMain.InEditMode();
    if (mouseIsDown && !reportedMouseDown)
    {
      voosActor.EnqueueMessage("MouseDown");
      reportedMouseDown = true;
      inputStateForScript.mouseButtonJustPressed = true;
    }
    if (reportedMouseDown)
    {
      voosActor.EnqueueMessage("MouseHeld");
      inputStateForScript.mouseButtonIsPressed = true;
    }
    if (!mouseIsDown && reportedMouseDown)
    {
      voosActor.EnqueueMessage("MouseUp");
      reportedMouseDown = false;
      inputStateForScript.mouseButtonJustReleased = true;
    }

    Vector2 mousePosUiCoords;
    Vector3 mousePosRaw = Input.mousePosition;
    mousePosUiCoords = gameUiMain.UnityScreenPointToGameUiPoint(mousePosRaw);

    inputStateForScript.mouseX = mousePosUiCoords.x;
    inputStateForScript.mouseY = mousePosUiCoords.y;
    inputStateForScript.keysHeld = keysHeld.ToArray();
    inputStateForScript.keysJustPressed = keysJustPressed.ToArray();
    inputStateForScript.keysJustReleased = keysJustReleased.ToArray();

    Ray mouseRay = userMain.GetCamera().ScreenPointToRay(mousePosRaw);
    inputStateForScript.mouseRayOrigin = mouseRay.origin;
    inputStateForScript.mouseRayDirection = mouseRay.direction;

    if (inputStateForScript.mouseButtonJustPressed)
    {
      RaycastHit hit;
      if (Physics.Raycast(mouseRay.origin, mouseRay.direction, out hit, 1000, VoosActor.LayerMaskValue, QueryTriggerInteraction.Collide))
      {
        VoosActor clickedActor = hit.collider.GetComponentInParent<VoosActor>();
        if (clickedActor != null)
        {
          clickedActor.GetEngine().EnqueueMessage(new VoosEngine.ActorMessage
          {
            name = "ActorClicked",
            targetActor = clickedActor.GetName(),
            argsJson = "{}"
          });
        }
      }
    }

    inputStateForScript.mouseWheelRaw = Input.GetAxis("Mouse ScrollWheel");
    inputStateForScript.mouseWheel = inputStateForScript.mouseWheelRaw * MOUSEWHEEL_FACTOR;
  }

  public bool HasInputStateForScript()
  {
    return hasInputForScript;
  }

  public InputStateForScript GetInputStateForScript()
  {
    return inputStateForScript;
  }

  internal bool IsSprinting()
  {
    if (controller == null || controller.GetInput() == null)
    {
      return false;
    }
    return controller.GetInput().IsSprinting();
  }

  public Vector3 GetWorldSpaceThrottle()
  {
    if (controller == null || controller.GetInput() == null)
    {
      return Vector3.zero;
    }
    else
    {
      return controller.GetInput().GetWorldSpaceThrottle();
    }
  }

  public Vector3 GetInputThrottle()
  {
    if (controller == null || controller.GetInput() == null)
    {
      return Vector3.zero;
    }
    else
    {
      return controller.GetInput().GetInputThrottle();
    }
  }

  public Vector2 GetLookAxes()
  {
    if (controller != null && controller.GetInput() != null)
    {
      return controller.GetInput().GetLookAxes();
    }
    return Vector2.zero;
  }

  internal Vector3 GetAimDirection()
  {
    return lastAimRotation * Vector3.forward;
  }

  internal Vector3 GetAimOrigin()
  {
    if (controller == null || controller.GetInput() == null)
    {
      return transform.position + new Vector3(0f, 0.5f, 0f);
    }
    else
    {
      return controller.GetInput().GetAimOrigin();
    }
  }

  public void Teleport(Vector3 newPos, Quaternion newRot)
  {
    transform.position = newPos;
    parts.headNode.rotation = newRot;
  }

  public Transform GetHeadTransform()
  {
    return parts.headNode;
  }

  public static void MakeActorControllable(VoosActor actor, PlayerBodyParts bodyPartsPrefab)
  {
    Debug.Assert(actor.playerBody == null, actor.name);
    PlayerBodyParts parts = GameObject.Instantiate(bodyPartsPrefab.gameObject).GetComponent<PlayerBodyParts>();
    parts.transform.parent = actor.transform;
    parts.transform.localPosition = Vector3.zero;
    parts.transform.localRotation = Quaternion.identity;
    actor.playerBody = actor.gameObject.AddComponent<PlayerBody>();
    actor.playerBody.SetParts(parts);
    // Util.Log($"OK added plaeyrBody to {actor.name}");
  }

  public static void MakeActorNotControllable(VoosActor actor)
  {
    Debug.Assert(actor.playerBody != null, actor.name);
    PlayerBody body = actor.playerBody;
    Debug.Assert(body != null, $"expected actor {actor.name} to be controlled, but body was null");
    Debug.Assert(body.gameObject != null, $"expected actor {actor.name} to be controlled, but body GO was null");
    Debug.Assert(body.parts != null, $"expected actor {actor.name} to be controlled, but body PARTS was null");
    Debug.Assert(body.parts.gameObject != null, $"expected actor {actor.name} to be controlled, but body PARTS GAME OBJECT was null");
    // Util.Log($"MakeActorNotControl destroy body on actor {actor.GetComponent<VoosActor>().GetDebugName()}");
    GameObject.Destroy(body.parts.gameObject);
    MonoBehaviour.Destroy(body);
    actor.playerBody = null;
  }

  public void OnDamagedMessage()
  {
    controller?.GetEventHandler()?.OnDamaged();
  }

  public void OnDiedMessage()
  {
    controller?.GetEventHandler()?.OnDied();
  }

  public void OnRespawnedMessage()
  {
    controller?.GetEventHandler()?.OnRespawned();
  }

  public void OnTintChanged(Color tint)
  {
    controller?.GetEventHandler()?.OnTintChanged(tint);
  }

  public void OnJumpedMessage()
  {
    controller?.GetEventHandler()?.OnJumped();
    onJump?.Invoke();
  }

  public event Action onJump;

  public Vector3 GetMoveThrottle()
  {
    if (this == null || this.gameObject == null) return Vector3.zero;

    if (voosActor != null && voosActor.IsLocallyOwned())
    {
      return voosActor.GetDesiredVelocity();
    }
    else
    {
      // TEMP TEMP - can be noisy, thus the thresholding.
      var groundVel = inferredVelocity.WithY(0f);
      return groundVel.magnitude > 0.4f ? groundVel : Vector3.zero;
    }
  }

  public Vector3 GetLookDirection()
  {
    if (this == null || this.gameObject == null) return Vector3.forward;

    if (voosActor == null || !voosActor.IsLocallyOwned())
    {
      // Make this up for now
      return transform.forward;
    }

    if (controller == null || controller.GetInput() == null)
    {
      return Vector3.zero;
    }
    return controller.GetInput().GetAim() * Vector3.forward;
  }

  public bool IsGrounded()
  {
    return wasTouchingGround;
  }

  public bool IsValid()
  {
    return this != null && this.gameObject != null && voosActor != null;
  }

  void OnDestroy()
  {
    Util.Log($"PlayerBody on actor {voosActor.GetDebugName()} destroyed");
  }
}
