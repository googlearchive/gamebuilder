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
using UnityEngine.Networking;

public partial class VoosActor : MonoBehaviour
{
  public const string AVATAR_EXPLORER_URI = "builtin:BuiltinAssets/AvatarExplorer";
  public const string NOT_AVAILABLE_URI = "builtin:BuiltinAssets/BrokenConnection";
  public const string OFFSTAGE_GHOST_MAT = "OffstageGhostMaterial";

  public const string SYSTEM_ACTOR_NAME_PREFIX = "system:";
  public const string PLAYER_PLACEHOLDER_NAME_PREFIX = SYSTEM_ACTOR_NAME_PREFIX + "PlayerPlaceholder:";

  private const float LIGHT_LOCAL_Y_OFFSET = 1.0f;

  public static int Layer
  {
    get { return LayerMask.NameToLayer("VoosActor"); }
  }

  public static int LayerMaskValue
  {
    get { return 0.WithBit(Layer, true); }
  }

  static int FirstVersionWithIsSolid = 1;
  static int FirstVersionWithSpawnPositionRotation = 2;
  static int FirstVersionWithExplicitPlayerUpright = 3;
  static int FirstVersionWithAutoPropagateToClones = 4;
  static int FirstVersionWithPhysicsAttribs = 5;
  static int FirstVersionStopHidingPlayers = 6;
  static int FirstVersionWithUseConcaveCollider = 7;

  public static int CurrentVersionNumber = 8;

  // This is the data type for the cameraSettingsJson field (it's a JSON version of this).
  // This is only relevant for actors that are cameras.
  [System.Serializable]
  public struct CameraSettings
  {
    // If true, the mouse cursor is on (like in a top-down RTS camera);
    // if false, mouse cursor is off (like in an FPS camera).
    public bool cursorActive;
    // Field of view angle. 0 means "default" not 0.
    public float fov;
    // Camera aim origin point.
    public Vector3 aimOrigin;
    // Camera aim direction.
    public Vector3 aimDir;
    // Actors to that will NOT be rendered in this camera.
    public string[] dontRenderActors;
  }

  // Data type for lightSettingsJson field.
  [System.Serializable]
  public struct LightSettings
  {
    public static float DEFAULT_RANGE = 40;

    // Light's range. If 0 (which is the default if this struct is not filled),
    // this light is disabled.
    public float range;
    // Color of the light. If null, uses actor's color.
    public Color? color;
    // Offset of the light.
    public Vector3 offset;
  }

  // Move out into its own file..
  [System.Serializable]
  public struct PersistedState
  {
    // BE CAREFUL: When adding a new field, be aware that when loading older
    // save games, your new field will get its default value. So make sure the
    // default value preserves the old behavior. An example of a breaking new
    // field: isSolid. If it's true, then colliders are solid. If it's false,
    // colliders are triggers. When I tried this, loading all old scenes
    // (including minimal-scene) caused everything to be a trigger - so the
    // player would just fall through the floor. To fix this, I bumped up the
    // version number of the save game (IN THE SAME COMMIT) and upgraded all
    // previous versions to have isSolid = true.

    public int version;

    public string name;
    public string brainName;
    public string[] tags; // Ideally would be immutable..

    public string renderableUri;
    public string memoryJson;

    // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_PERSISTED_FIELDS_CSHARP_DECLS
    public string displayName;    // GENERATED
    public string description;    // GENERATED
    public Color tint;    // GENERATED
    public string transformParent;    // GENERATED
    public Vector3 position;    // GENERATED
    public Quaternion rotation;    // GENERATED
    public Vector3 localScale;    // GENERATED
    public Vector3 renderableOffset;    // GENERATED
    public Quaternion renderableRotation;    // GENERATED
    public string commentText;    // GENERATED
    public Vector3 spawnPosition;    // GENERATED
    public Quaternion spawnRotation;    // GENERATED
    public bool preferOffstage;    // GENERATED
    public bool isSolid;    // GENERATED
    public bool enablePhysics;    // GENERATED
    public bool enableGravity;    // GENERATED
    public float bounciness;    // GENERATED
    public float drag;    // GENERATED
    public float angularDrag;    // GENERATED
    public float mass;    // GENERATED
    public bool freezeRotations;    // GENERATED
    public bool freezeX;    // GENERATED
    public bool freezeY;    // GENERATED
    public bool freezeZ;    // GENERATED
    public bool enableAiming;    // GENERATED
    public bool hideInPlayMode;    // GENERATED
    public bool keepUpright;    // GENERATED
    public bool isPlayerControllable;    // GENERATED
    public string debugString;    // GENERATED
    public string cloneParent;    // GENERATED
    public Vector3 velocity;    // GENERATED
    public Vector3 angularVelocity;    // GENERATED
    public string cameraActor;    // GENERATED
    public string spawnTransformParent;    // GENERATED
    public bool wasClonedByScript;    // GENERATED
    public string loopingAnimation;    // GENERATED
    public string cameraSettingsJson;    // GENERATED
    public string lightSettingsJson;    // GENERATED
    public string pfxId;    // GENERATED
    public string sfxId;    // GENERATED
    public bool useConcaveCollider;    // GENERATED
    public bool speculativeColDet;    // GENERATED
    public bool useStickyDesiredVelocity;    // GENERATED
    public Vector3 stickyDesiredVelocity;    // GENERATED
    public Vector3 stickyForce;    // GENERATED
    // END_GAME_BUILDER_CODE_GEN

    public static PersistedState NewFrom(VoosActor actor, bool bypassMemory = false)
    {
      PersistedState rv;
      rv.version = CurrentVersionNumber;

      rv.name = actor.GetName();
      rv.tags = new string[actor.tags.Count];
      int i = 0;
      foreach (string tag in actor.tags)
      {
        rv.tags[i++] = tag;
      }

      rv.brainName = actor.GetBrainName();
      rv.memoryJson = bypassMemory ? "" : actor.GetMemoryJsonSlow();

      rv.renderableUri = actor.renderableUri;

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_PERSISTED_FIELDS_SERIALIZE
      rv.displayName = actor.GetDisplayName();    // GENERATED
      rv.description = actor.GetDescription();    // GENERATED
      rv.tint = actor.GetTint();    // GENERATED
      rv.transformParent = actor.GetTransformParent();    // GENERATED
      rv.position = actor.GetPosition();    // GENERATED
      rv.rotation = actor.GetRotation();    // GENERATED
      rv.localScale = actor.GetLocalScale();    // GENERATED
      rv.renderableOffset = actor.GetRenderableOffset();    // GENERATED
      rv.renderableRotation = actor.GetRenderableRotation();    // GENERATED
      rv.commentText = actor.GetCommentText();    // GENERATED
      rv.spawnPosition = actor.GetSpawnPosition();    // GENERATED
      rv.spawnRotation = actor.GetSpawnRotation();    // GENERATED
      rv.preferOffstage = actor.GetPreferOffstage();    // GENERATED
      rv.isSolid = actor.GetIsSolid();    // GENERATED
      rv.enablePhysics = actor.GetEnablePhysics();    // GENERATED
      rv.enableGravity = actor.GetEnableGravity();    // GENERATED
      rv.bounciness = actor.GetBounciness();    // GENERATED
      rv.drag = actor.GetDrag();    // GENERATED
      rv.angularDrag = actor.GetAngularDrag();    // GENERATED
      rv.mass = actor.GetMass();    // GENERATED
      rv.freezeRotations = actor.GetFreezeRotations();    // GENERATED
      rv.freezeX = actor.GetFreezeX();    // GENERATED
      rv.freezeY = actor.GetFreezeY();    // GENERATED
      rv.freezeZ = actor.GetFreezeZ();    // GENERATED
      rv.enableAiming = actor.GetEnableAiming();    // GENERATED
      rv.hideInPlayMode = actor.GetHideInPlayMode();    // GENERATED
      rv.keepUpright = actor.GetKeepUpright();    // GENERATED
      rv.isPlayerControllable = actor.GetIsPlayerControllable();    // GENERATED
      rv.debugString = actor.GetDebugString();    // GENERATED
      rv.cloneParent = actor.GetCloneParent();    // GENERATED
      rv.velocity = actor.GetVelocity();    // GENERATED
      rv.angularVelocity = actor.GetAngularVelocity();    // GENERATED
      rv.cameraActor = actor.GetCameraActor();    // GENERATED
      rv.spawnTransformParent = actor.GetSpawnTransformParent();    // GENERATED
      rv.wasClonedByScript = actor.GetWasClonedByScript();    // GENERATED
      rv.loopingAnimation = actor.GetLoopingAnimation();    // GENERATED
      rv.cameraSettingsJson = actor.GetCameraSettingsJson();    // GENERATED
      rv.lightSettingsJson = actor.GetLightSettingsJson();    // GENERATED
      rv.pfxId = actor.GetPfxId();    // GENERATED
      rv.sfxId = actor.GetSfxId();    // GENERATED
      rv.useConcaveCollider = actor.GetUseConcaveCollider();    // GENERATED
      rv.speculativeColDet = actor.GetSpeculativeColDet();    // GENERATED
      rv.useStickyDesiredVelocity = actor.GetUseStickyDesiredVelocity();    // GENERATED
      rv.stickyDesiredVelocity = actor.GetStickyDesiredVelocity();    // GENERATED
      rv.stickyForce = actor.GetStickyForce();    // GENERATED
      // END_GAME_BUILDER_CODE_GEN

      return rv;
    }

    public void Serialize(NetworkWriter writer)
    {
      writer.WriteVoosName(name);
      writer.WriteVoosName(brainName);
      writer.WriteUtf16(memoryJson);
      writer.WriteUtf16(renderableUri);

      Debug.Assert(tags.Length <= 256, "Actor has more than 256 tags.");
      writer.Write((byte)tags.Length);
      foreach (string tag in tags)
      {
        writer.WriteUtf16(tag);
      }

      // TODO use VoosName to write transformParent, cloneParent, etc. etc.

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_PERSISTED_FIELDS_BINARY_SERIALIZE
      writer.WriteUtf16(displayName);    // GENERATED
      writer.WriteUtf16(description);    // GENERATED
      writer.WriteColor(tint);    // GENERATED
      writer.WriteUtf16(transformParent);    // GENERATED
      writer.WriteVoosVector3(position);    // GENERATED
      writer.Write(rotation);    // GENERATED
      writer.WriteVoosVector3(localScale);    // GENERATED
      writer.WriteVoosVector3(renderableOffset);    // GENERATED
      writer.Write(renderableRotation);    // GENERATED
      writer.WriteUtf16(commentText);    // GENERATED
      writer.WriteVoosVector3(spawnPosition);    // GENERATED
      writer.Write(spawnRotation);    // GENERATED
      writer.WriteVoosBoolean(preferOffstage);    // GENERATED
      writer.WriteVoosBoolean(isSolid);    // GENERATED
      writer.WriteVoosBoolean(enablePhysics);    // GENERATED
      writer.WriteVoosBoolean(enableGravity);    // GENERATED
      writer.Write(bounciness);    // GENERATED
      writer.Write(drag);    // GENERATED
      writer.Write(angularDrag);    // GENERATED
      writer.Write(mass);    // GENERATED
      writer.WriteVoosBoolean(freezeRotations);    // GENERATED
      writer.WriteVoosBoolean(freezeX);    // GENERATED
      writer.WriteVoosBoolean(freezeY);    // GENERATED
      writer.WriteVoosBoolean(freezeZ);    // GENERATED
      writer.WriteVoosBoolean(enableAiming);    // GENERATED
      writer.WriteVoosBoolean(hideInPlayMode);    // GENERATED
      writer.WriteVoosBoolean(keepUpright);    // GENERATED
      writer.WriteVoosBoolean(isPlayerControllable);    // GENERATED
      writer.WriteUtf16(debugString);    // GENERATED
      writer.WriteUtf16(cloneParent);    // GENERATED
      writer.WriteVoosVector3(velocity);    // GENERATED
      writer.WriteVoosVector3(angularVelocity);    // GENERATED
      writer.WriteUtf16(cameraActor);    // GENERATED
      writer.WriteUtf16(spawnTransformParent);    // GENERATED
      writer.WriteVoosBoolean(wasClonedByScript);    // GENERATED
      writer.WriteUtf16(loopingAnimation);    // GENERATED
      writer.WriteUtf16(cameraSettingsJson);    // GENERATED
      writer.WriteUtf16(lightSettingsJson);    // GENERATED
      writer.WriteUtf16(pfxId);    // GENERATED
      writer.WriteUtf16(sfxId);    // GENERATED
      writer.WriteVoosBoolean(useConcaveCollider);    // GENERATED
      writer.WriteVoosBoolean(speculativeColDet);    // GENERATED
      writer.WriteVoosBoolean(useStickyDesiredVelocity);    // GENERATED
      writer.WriteVoosVector3(stickyDesiredVelocity);    // GENERATED
      writer.WriteVoosVector3(stickyForce);    // GENERATED
      // END_GAME_BUILDER_CODE_GEN
    }

    public void Deserialize(NetworkReader reader)
    {
      this.name = reader.ReadVoosName();
      this.brainName = reader.ReadVoosName();
      this.memoryJson = reader.ReadUtf16();
      this.renderableUri = reader.ReadUtf16();

      byte numTags = reader.ReadByte();
      this.tags = new string[numTags];
      for (int i = 0; i < numTags; i++)
      {
        this.tags[i] = reader.ReadUtf16();
      }

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_PERSISTED_FIELDS_BINARY_DESERIALIZE
      this.displayName = reader.ReadUtf16();    // GENERATED
      this.description = reader.ReadUtf16();    // GENERATED
      this.tint = reader.ReadColor();    // GENERATED
      this.transformParent = reader.ReadUtf16();    // GENERATED
      this.position = reader.ReadVoosVector3();    // GENERATED
      this.rotation = reader.ReadQuaternion();    // GENERATED
      this.localScale = reader.ReadVoosVector3();    // GENERATED
      this.renderableOffset = reader.ReadVoosVector3();    // GENERATED
      this.renderableRotation = reader.ReadQuaternion();    // GENERATED
      this.commentText = reader.ReadUtf16();    // GENERATED
      this.spawnPosition = reader.ReadVoosVector3();    // GENERATED
      this.spawnRotation = reader.ReadQuaternion();    // GENERATED
      this.preferOffstage = reader.ReadVoosBoolean();    // GENERATED
      this.isSolid = reader.ReadVoosBoolean();    // GENERATED
      this.enablePhysics = reader.ReadVoosBoolean();    // GENERATED
      this.enableGravity = reader.ReadVoosBoolean();    // GENERATED
      this.bounciness = reader.ReadSingle();    // GENERATED
      this.drag = reader.ReadSingle();    // GENERATED
      this.angularDrag = reader.ReadSingle();    // GENERATED
      this.mass = reader.ReadSingle();    // GENERATED
      this.freezeRotations = reader.ReadVoosBoolean();    // GENERATED
      this.freezeX = reader.ReadVoosBoolean();    // GENERATED
      this.freezeY = reader.ReadVoosBoolean();    // GENERATED
      this.freezeZ = reader.ReadVoosBoolean();    // GENERATED
      this.enableAiming = reader.ReadVoosBoolean();    // GENERATED
      this.hideInPlayMode = reader.ReadVoosBoolean();    // GENERATED
      this.keepUpright = reader.ReadVoosBoolean();    // GENERATED
      this.isPlayerControllable = reader.ReadVoosBoolean();    // GENERATED
      this.debugString = reader.ReadUtf16();    // GENERATED
      this.cloneParent = reader.ReadUtf16();    // GENERATED
      this.velocity = reader.ReadVoosVector3();    // GENERATED
      this.angularVelocity = reader.ReadVoosVector3();    // GENERATED
      this.cameraActor = reader.ReadUtf16();    // GENERATED
      this.spawnTransformParent = reader.ReadUtf16();    // GENERATED
      this.wasClonedByScript = reader.ReadVoosBoolean();    // GENERATED
      this.loopingAnimation = reader.ReadUtf16();    // GENERATED
      this.cameraSettingsJson = reader.ReadUtf16();    // GENERATED
      this.lightSettingsJson = reader.ReadUtf16();    // GENERATED
      this.pfxId = reader.ReadUtf16();    // GENERATED
      this.sfxId = reader.ReadUtf16();    // GENERATED
      this.useConcaveCollider = reader.ReadVoosBoolean();    // GENERATED
      this.speculativeColDet = reader.ReadVoosBoolean();    // GENERATED
      this.useStickyDesiredVelocity = reader.ReadVoosBoolean();    // GENERATED
      this.stickyDesiredVelocity = reader.ReadVoosVector3();    // GENERATED
      this.stickyForce = reader.ReadVoosVector3();    // GENERATED
      // END_GAME_BUILDER_CODE_GEN
    }
  }

  // These are *expensive*! So only add this component if the behaviors actually
  // need one of them.
  class CollisionHandling : MonoBehaviour
  {
    public VoosEngine engine;
    public VoosActor actor;

    int frameAdded = 0;

    void Awake()
    {
      frameAdded = Time.frameCount;
    }

    void HandleCollision(Collision collision, bool isEnterMessage)
    {
      for (int i = 0; i < collision.contactCount; i++)
      {
        ContactPoint cp = collision.GetContact(i);
        var terrain = cp.otherCollider.GetComponentInParent<TerrainManager>();
        bool isTerrain = terrain != null;

        VoosActor collisionRecipient = actor;

        if (cp.thisCollider != null)
        {
          // NOTE: for childless actors, the collision recipient is always the actor itself,
          // but if the actor has children, then we have to explicitly query to see which of the
          // children was collided against, because Unity delivers collision only to the GameObject
          // where the RigidBody is, and in hierarchies only the top-level parent has a RB.
          // TODO: possible optimization: only execute this if statement if the actor indeed HAS
          // VoosActor children (I don't know of a cheap way to check...)
          collisionRecipient = cp.thisCollider.GetComponentInParent<VoosActor>() ?? actor;
        }

        if (!isTerrain)
        {
          engine.HandleTouch(collisionRecipient, cp.otherCollider, isEnterMessage);
        }
        else
        {
          engine.HandleTerrainCollision(collisionRecipient, terrain, cp, isEnterMessage);
        }
      }
    }

    private void OnCollisionEnter(Collision collision)
    {
      HandleCollision(collision, true);
    }

    private void OnTriggerEnter(Collider otherCollider)
    {
      if (otherCollider.GetComponentInParent<TerrainManager>() != null)
      {
        if (Time.frameCount - frameAdded <= 1)
        {
          // Ignore terrain collisions within 1 frame. For some reason, we tend
          // to get phantom ones here.
          return;
        }

        engine.HandleUnknownTerrainCollision(actor, true);
      }
      else
      {
        engine.HandleTouch(actor, otherCollider, true);
      }
    }

    private void OnCollisionStay(Collision collision)
    {
      HandleCollision(collision, false);
    }

    private void OnTriggerStay(Collider otherCollider)
    {
      if (otherCollider.GetComponentInParent<TerrainManager>() != null)
      {
        if (Time.frameCount - frameAdded <= 1)
        {
          // Ignore terrain collisions within 1 frame. For some reason, we tend
          // to get phantom ones here.
          return;
        }
        engine.HandleUnknownTerrainCollision(actor, false);
      }
      else
      {
        engine.HandleTouch(actor, otherCollider, false);
      }
    }
  }

  class CollisionStayTracker : MonoBehaviour
  {
    bool isColliding = false;

    public bool GetAndClear()
    {
      bool wasColliding = isColliding;
      isColliding = false;
      return wasColliding;
    }

    private void OnCollisionStay(Collision collision)
    {
      // Indicate that we are currently colliding against something.
      // This is checked on FixedUpdate() to throttle air controls.
      isColliding = true;
    }
  }

  internal void MakeOwnCopyOfBrain()
  {
    (new ActorBehaviorsEditor(this.GetName(), this.engine, null)).CreateOwnCopyOfBrain();
  }

  // All data that is relevant to runtime, serialized to script.
  // This includes persisted data, but also other fields that does not need to be persisted.
  [System.Serializable]
  public struct RuntimeState
  {
    public string name;

    // BEGIN_GAME_BUILDER_CODE_GEN RUNTIME_STATE_CSHARP_DECLS
    public bool isSprinting;    // GENERATED
    public Vector3 worldSpaceThrottle;    // GENERATED
    public Vector3 inputThrottle;    // GENERATED
    public Vector3 aimDirection;    // GENERATED
    public Vector3 aimOrigin;    // GENERATED
    public Vector3 lastAimHitPoint;    // GENERATED
    public string aimingAtName;    // GENERATED
    public Vector3 lookAxes;    // GENERATED
    // END_GAME_BUILDER_CODE_GEN

    public bool hasInputState;
    public PlayerBody.InputStateForScript inputState;

    public static RuntimeState NewFrom(VoosActor actor)
    {
      RuntimeState rv = new RuntimeState();

      rv.name = actor.GetName();

      PlayerBody body = actor.GetPlayerBody();
      rv.aimDirection = body != null ? body.GetAimDirection() : actor.transform.forward;
      rv.aimOrigin = body != null ? body.GetAimOrigin() : actor.transform.position + new Vector3(0f, 0.5f, 0f);
      rv.worldSpaceThrottle = body != null ? body.GetWorldSpaceThrottle() : Vector3.zero;
      rv.inputThrottle = body != null ? body.GetInputThrottle() : Vector3.zero;
      rv.isSprinting = body != null ? body.IsSprinting() : false;
      // Note: in Unity we use degrees, but JS uses radians:
      rv.lookAxes = body != null ? new Vector3(
        body.GetLookAxes().x * (float)Math.PI / 180.0f,
        body.GetLookAxes().y * (float)Math.PI / 180.0f, 0) : Vector3.zero;

      rv.aimingAtName = null;
      rv.lastAimHitPoint = Vector3.zero;
      if (body != null && body.HasInputStateForScript())
      {
        rv.hasInputState = true;
        rv.inputState = body.GetInputStateForScript();
      }

      if (actor.GetEnableAiming())
      {
        RaycastHit hit;

        // TODO make this an overlap so we can ignore self.
        if (Physics.Raycast(rv.aimOrigin, rv.aimDirection, out hit, 99f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
          VoosActor hitEnt = hit.transform.GetComponent<VoosActor>();
          if (hitEnt != null)
          {
            rv.aimingAtName = hitEnt.GetName();
          }

          rv.lastAimHitPoint = hit.point;
        }

        if (Application.isEditor)
        {
          if (rv.aimingAtName.IsNullOrEmpty())
          {
            Debug.DrawLine(rv.aimOrigin, rv.aimOrigin + 99f * rv.aimDirection, Color.grey, 0f);
          }
          else
          {
            Debug.DrawLine(rv.aimOrigin, hit.point, Color.green, 0f);
          }
        }
      }

      return rv;
    }
  }

  public interface TextRenderer
  {
    void SetText(string text);
  }

  // Persisted state. Non-derivable, often set by human, but could also be set by script.

  // TODO TODO this really should just use the PersistedState struct..? At least share some?

  HashSet<string> tags = new HashSet<string>();
  [SerializeField] string brainName = null;
  [SerializeField] Color tint;
  [SerializeField] string renderableUri;

  // Internal VoosEngine name/GUID. Do NOT rely on gameObject.name to be the same.
  string voosName = null;

  // Cache variables.

  VoosScene scene;
  VoosEngine engine;
  AssetCache assetCache;
  LinkedListNode<VoosActor> actorMemoryRPCNode;

  // Hmm..get rid of this dependency. Either don't make VoosActor know about behaviors, or make VoosEngine know about behaviors.
  BehaviorSystem behaviors;

  // This flag indicates that this actor has just teleported. This is used to communicate to the network peers
  // that they should NOT interpolate the position of this actor on their end but should rather simply place
  // the actor at its new position instantly.
  bool teleport = false;

  // Optimization for collision events.
  CollisionHandling collisionHandling = null;
  bool isHandlingCollisions { get { return collisionHandling != null; } }

  CollisionStayTracker collisionStayTrackerForPlayer = null;

  // NOTE: local draft objects do NOT have a reliableView (this will be null).
  [SerializeField] PhotonView reliableView;
  public PhotonView reliablePhotonView
  {
    get { return reliableView; }
  }

  ActorNetworking actorNetworking;

  Rigidbody cachedRigidbody;

  // Please don't access unless you're VoosActor or PlayerBody.
  public PlayerBody playerBody = null;

  // Set when non-JS sources need to modify the memory, such as loading from persisted or networking.
  // We can't immediately set it in JS land, since the actor may not exist in the actor database.
  [System.NonSerialized] string authoritativeMemoryJson = null;

  // State that only this component cares about.
  [SerializeField] GameObject currentRenderable;
  int outstandingCacheRequests = 0;
  bool isCurrentRenderableShown = true;
  MeshRenderer[] meshRenderers;
  Renderer[] allRenderers;
  Renderer[] tintableRenderers;
  Collider[] renderableColliders;
  Animation renderableAnimation;

  // To solve the quirk that kinematic solids don't register collisions with
  // *eachother*. Each collider here is a component added to some object in the
  // renderable's hierarchy. Specifically, the same object that has the original
  // non-trigger collider.
  List<Collider> ghostColliders = new List<Collider>();

  AssetCache.Value __cachedRenderable;
  AssetCache.Value cachedRenderable
  {
    get { return __cachedRenderable; }
    set
    {
      if (__cachedRenderable != null)
      {
        __cachedRenderable.onAssetChanged -= OnCachedRenderableChanged;
      }

      __cachedRenderable = value;

      if (__cachedRenderable != null)
      {
        __cachedRenderable.onAssetChanged += OnCachedRenderableChanged;
      }
    }
  }

  // Deltas are requested during Update, we want to apply them during the next FixedUpdate.
  Vector3 pendingVelocityDelta = Vector3.zero;
  List<Vector3> pendingTorques = new List<Vector3>();

  // This is because we shouldn't use MovePosition outside of FixedUpdate.
  // Still need to think more about what the scripting API looks like for physics stuff..
  bool moveRequestPending = false;
  Vector3 moveRequestTargetPosition = Vector3.zero;

  // NOTE: This is *not* where locks are enforced. That's done by VoosEngine.
  // This is only a signal to others that this is *probably* locked, so don't
  // even try to request ownership. And it's only meaningful if the actor is not
  // locally owned.
  bool lockedIfNotOwned = false;

  // Set to true on OnCollisionStay to indicate that this actor just collided against something.
  bool isColliding = false;

  bool resetInertiaTensorPending = false;

  public event System.Action offstageChanged;

  // TODO Just use event's for these
  public delegate void DisplayNameChanged();

  private DisplayNameChanged onDisplayNameChanged = () => { };

  public event System.Action onLockChanged;

  // Cached: are we effectively offstage?
  // This cache is updated every time our preferOffstage field changes, and every time
  // it changes for one of our ancestors. See ComputeIsOffstageEffective().
  private bool cachedIsOffstageEffective;

  // Lazily created AudioSource for SFX, used when we need to play SFX coming from this actor.
  private AudioSource loopingAudioSource;
  private ParticleSystem renderableParticleSystem;

  // For VoosEngine's use only
  [NonSerialized]
  public ushort lastTempId = 0;
  [NonSerialized]
  public float realTimeOfLastCollisionWithLocalPlayer = 0f;

  // For diagnostics
  public float lastUnreliableUpdateTime = 0f;
  public bool debug = false;
  public GlobalUnreliableData.NetworkedActor unrel = null;
  public Vector3 lastRBMovedPos;

  // Passed 'true' if it is an undo
  public event System.Action<bool> onBrainChanged;

  // If not null, this is this actor's point light (parented to this actor).
  private Light actorLight;

  // If this is not a player controllable actor, this is the IBipedDriver implementation
  // that we use so that characters like the Fox can work as an NPC.
  private NonPlayerBipedDriver nonPlayerBipedDriver;

  // Because isLocal can be changed by a lot of things, best to just detect
  // change in an observational way.
  bool lastSyncedIsLocalValue = false;

  void MarkForScriptSync()
  {
    engine.MarkActorForScriptSync(this);
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref actorNetworking);
  }

  // Because we have no control over Awake...
  // The alternative is to use FindIfNotSet in Awake. But they actually add a good amount of overhead. Avoid them.
  public void Initialize(VoosScene scene, VoosEngine engine, AssetCache assetCache, BehaviorSystem behaviorSystem)
  {
    if (this.scene != null)
    {
      throw new System.Exception("Initialize called twice on VoosActor?");
    }

    Debug.Assert(engine != null, "VoosActor.Initialize: null engine");
    Debug.Assert(scene != null, "VoosActor.Initialize: null scene");
    Debug.Assert(behaviorSystem != null, "VoosActor.Initialize: null behavior sys");
    Debug.Assert(assetCache != null, "VoosActor.Initialize: null asset cache");

    this.scene = scene;
    this.engine = engine;
    this.behaviors = behaviorSystem;
    this.assetCache = assetCache;

    // Should not be needed, but in case
    spawnPosition = this.transform.position;
    spawnRotation = this.transform.rotation;

    transform.parent = scene.transform;
    actorMemoryRPCNode = new LinkedListNode<VoosActor>(this);
    OnRenderableChanged();
    UpdateRigidbodyComponent();
    UpdateCollisionStayTracker();
    // Actors that get created need to teleport into place, not interpolate in.
    teleport = true;

    MarkForScriptSync();

    engine.GetParticleEffectSystem().onParticleEffectChanged += OnParticleEffectUpdated;
    engine.GetParticleEffectSystem().onParticleEffectRemoved += OnParticleEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectChanged += OnSoundEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectRemoved += OnSoundEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectLoaded += OnSoundEffectUpdated;
  }

  private void OnParticleEffectUpdated(string id)
  {
    if (this == null)
    {
      return;
    }
    if (id == pfxId) UpdatePfxId();
  }

  private void OnSoundEffectUpdated(string id)
  {
    if (this == null)
    {
      return;
    }
    if (id == sfxId) UpdateSfxId();
  }

  void MaybeCorrectRotation()
  {
    if (GetKeepUpright())
    {
      Vector3 forward = transform.forward;
      if (Vector3.Angle(transform.forward, Vector3.up) < 1f ||
      Vector3.Angle(transform.forward, Vector3.down) < 1f)
      {
        forward = -transform.up;
      }
      transform.rotation = Quaternion.LookRotation(forward.WithY(0f).normalized, Vector3.up);

      Debug.Assert(Vector3.Angle(transform.up, Vector3.up) < 2f, "MaybeCorrectRotation: Keep upright not working");
    }
  }

  void UpdateRigidbodyComponent()
  {
    var rb = GetRigidBody();
    if (rb == null)
    {
      return;
    }

    bool kinematicForCatchup = !IsLocallyOwned() && replicantCatchUpMode;
    rb.isKinematic = kinematicForCatchup || !GetEnablePhysics() || GetIsOffstageEffective();
    rb.useGravity = GetEnableGravity();
    rb.mass = GetMass();
    rb.drag = GetDrag();
    rb.angularDrag = GetAngularDrag();

    RigidbodyConstraints constraints = RigidbodyConstraints.None;

    if (GetFreezeRotations())
    {
      constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }
    else if (GetKeepUpright())
    {
      constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    if (GetFreezeX()) constraints |= RigidbodyConstraints.FreezePositionX;
    if (GetFreezeY()) constraints |= RigidbodyConstraints.FreezePositionY;
    if (GetFreezeZ()) constraints |= RigidbodyConstraints.FreezePositionZ;

    rb.constraints = constraints;

    rb.collisionDetectionMode = GetSpeculativeColDet() ? CollisionDetectionMode.ContinuousSpeculative : CollisionDetectionMode.Discrete;
  }

  public bool IsParentedToAnotherActor()
  {
    return !this.GetTransformParent().IsNullOrEmpty();
  }

  public bool IsMemoryDirty
  {
    get
    {
      return actorMemoryRPCNode.List != null;
    }
  }

  void UpdateTransformParent(string oldTransformParent)
  {
    if (this.GetTransformParent() == this.GetName())
    {
      throw new System.Exception("Transform parent set to self. Not allowed.");
    }

    VoosActor newParent = engine.GetActor(GetTransformParent());

    if (newParent != null)
    {
      this.transform.parent = newParent.transform;

      if (GetRigidBody() != null)
      {
        // We need to destroy the RB component so weird physics things don't happen.
        Destroy(GetRigidBody());
        cachedRigidbody = null; // Don't trust Unity nullness anymore..
      }

      // If we own the new parent, try to own this actor too.
      if (newParent != null && newParent.IsLocallyOwned() && !this.IsLocallyOwned())
      {
        this.RequestOwnership();
      }
    }
    else
    {
      // No parent - parent to the scene.
      this.transform.parent = scene.transform;

      if (GetRigidBody() == null)
      {
        this.gameObject.AddComponent<Rigidbody>();
        UpdateRigidbodyComponent();
      }
    }

    resetInertiaTensorPending = false;

    // Notify about change.
    VoosActor oldParent = oldTransformParent != null ? engine.GetActor(oldTransformParent) : null;
    if (oldParent != null)
    {
      NotifyHierarchyChangedFor(oldParent);
    }
    NotifyHierarchyChangedFor(this);
  }

  private static void NotifyHierarchyChangedFor(VoosActor actor)
  {
    foreach (VoosActor descendant in actor.GetRootActor().GetComponentsInChildren<VoosActor>())
    {
      descendant.OnHierarchyChanged();
    }
  }

  public void OnHierarchyChanged()
  {
    UpdateCollisionHandling();
    resetInertiaTensorPending = true;
  }

  public static bool IsValidParent(VoosActor child, VoosActor parent)
  {
    // Check for self or cycles
    while (parent != null)
    {
      if (parent == child)
      {
        Util.Log($"Cycle detected!");
        return false;
      }
      parent = child.GetEngine().GetActor(parent.GetTransformParent());
    }
    return true;
  }

  public static bool IsValidSpawnParent(VoosActor child, VoosActor parent)
  {
    // Check for self or cycles
    while (parent != null)
    {
      if (parent == child)
      {
        Util.Log($"Cycle detected!");
        return false;
      }
      parent = child.GetEngine().GetActor(parent.GetSpawnTransformParent());
    }
    return true;
  }

  public bool GetIsRunning()
  {
    return engine.GetIsRunning();
  }


  void FixedUpdate()
  {
    // Important to call this always, up here, since we do need to clear the "isColliding" state no matter what.
    bool wasCollidingAndIsPlayer = collisionStayTrackerForPlayer != null ? collisionStayTrackerForPlayer.GetAndClear() : false;

    // IMPORTANT: FixedUpdate is called even if Physics.autoSimulation is
    // disabled, so we need to guard ourselves.
    if (!engine.GetIsRunning())
    {
      return;
    }

    Rigidbody rb = GetRigidBody();

    // TODO Guard most of this code with IsLocallyOwned!!

    // Apply velocity deltas here.
    if (rb != null)
    {
      if (pendingVelocityDelta.magnitude > 0f)
      {
        lastImpulseTime = Time.fixedTime;
        rb.AddForce(pendingVelocityDelta, ForceMode.VelocityChange);
        pendingVelocityDelta = Vector3.zero;
      }

      if (pendingTorques.Count > 0)
      {
        foreach (var torque in pendingTorques)
        {
          rb.AddTorque(torque);
        }
        pendingTorques.Clear();
      }

      if (GetStickyForce().magnitude > 0f)
      {
        rb.AddForce(GetStickyForce());
      }

      // Enforce a speed limit..
      float overSpeed = rb.velocity.magnitude - 100f;
      if (overSpeed > 0f)
      {
        Vector3 correction = rb.velocity * -1 * overSpeed;
        // But for stability..limit the magnitude of the correction! Otherwise we're likely to explode.
        correction = Vector3.ClampMagnitude(correction, 100f);
        rb.AddForce(correction, ForceMode.VelocityChange);
      }
    }

    if (GetUseDesiredVelocity() || GetUseStickyDesiredVelocity())
    {
      Vector3 netDesiredVelocity = Vector3.zero;
      if (GetUseDesiredVelocity()) netDesiredVelocity += GetDesiredVelocity();
      if (GetUseStickyDesiredVelocity()) netDesiredVelocity += GetStickyDesiredVelocity();

      if (this.GetEnablePhysics() && rb != null)
      {
        // Dynamic

        if (ignoreVerticalDesiredVelocity)
        {
          // If we're grounded on a rigidbody, allow it to carry us. So add its velocity to our desired velocity.
          // Mostly for moving platforms.
          Rigidbody groundBody = GetPlayerBody()?.GetGroundRigidbody();
          Vector3 groundVelocity = groundBody != null ? groundBody.velocity : Vector3.zero;

          Vector3 desiredAccel = ((netDesiredVelocity + groundVelocity) - rb.velocity).WithY(0f) / Time.fixedDeltaTime;

          Debug.DrawRay(transform.position + 1.6f * Vector3.up, desiredAccel, Color.red, 0f);

          // Cap our maximum force if we're not grounded.
          // TODO push this to the JavaScript for walk/run controls.
          if (GetPlayerBody() != null && !GetPlayerBody().GetIsTouchingGround())
          {
            // Limit horizontal motor force if we are colliding against
            // something. Prevent climbing/launching off steep ramps and
            // slightly-non-vertical walls.
            if (wasCollidingAndIsPlayer)
            {
              desiredAccel = Vector3.ClampMagnitude(desiredAccel, 10f);
            }
          }

          if (GetEnableGravity() && GetPlayerBody() != null)
          {
            if (!GetPlayerBody().GetIsTouchingGround())
            {
              // In air, some reasonable amount.
              rb.AddForce(Vector3.up * -50f, ForceMode.Acceleration);
            }
            else
            {
              // Grounded - do an extra "stick to the ground" force to avoid hopping.

              RaycastHit hit;
              bool rayHit = Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                Vector3.down,
                out hit,
                0.3f,
                -1, QueryTriggerInteraction.Ignore);

              // Don't do stick-force if we're on a dynamic body, like a rolling ball
              bool hitDynamicBody = hit.rigidbody != null && !hit.rigidbody.isKinematic;

              if (rayHit && !hitDynamicBody)
              {
                Debug.DrawRay(hit.point, hit.normal, Color.red, 0.1f);

                // If the ramp angle is above 45f, don't do anything.
                float rampAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (
                  // Too steep - we don't wanna be..pushing ourselves up walls or up into ceilings!
                  rampAngle > 45f
                  // An impulse could be..a JUMP, or a launch pad. Hold off for a bit to let them do their thing.
                  || (Time.fixedTime - lastImpulseTime) < 0.5f
                )
                {
                  // Don't do anything.
                }
                else
                {
                  Vector3 groundingAccel = hit.normal * -400f;
                  rb.AddForce(groundingAccel, ForceMode.Acceleration);
                }
              }
            }
          }

          rb.AddForce(desiredAccel, ForceMode.Acceleration);
        }
        else
        {
          Util.RealizeTargetVelocityXYZ(rb, netDesiredVelocity);
        }
      }
      else
      {
        // Kinematic - just move it by the desired velocity.
        Vector3 velocity = netDesiredVelocity;
        if (ignoreVerticalDesiredVelocity)
        {
          velocity.y = 0f;
        }
        Vector3 newPos = transform.position + Time.fixedDeltaTime * velocity;
        if (rb != null)
        {
          rb.MovePosition(newPos);
        }
        else
        {
          transform.position = newPos;
        }
      }
    }

    if (moveRequestPending)
    {
      moveRequestPending = false;
      if (rb != null)
      {
        // NOTE: For non-kinematics, this functions as a teleport.
        // For kinematics, this simulates a sweep.
        rb.MovePosition(moveRequestTargetPosition);
      }
      else
      {
        transform.position = moveRequestTargetPosition;
      }
    }

    if (rb != null && resetInertiaTensorPending)
    {
      rb.ResetInertiaTensor();
      resetInertiaTensorPending = false;
    }
  }

  class GetMemoryJsonRequest
  {
    public string operation = "getActorMemoryJson";
    public string actorId;
  }

  class GetMemoryJsonResponse
  {
    public string memoryJson;
  }

  public string GetMemoryJsonSlow()
  {
    using (InGameProfiler.Section("GetMemoryJsonSlow"))
    {
      if (authoritativeMemoryJson != null)
      {
        return authoritativeMemoryJson;
      }

      var request = new GetMemoryJsonRequest { actorId = this.GetName() };
      var response = engine.CommunicateWithAgent<GetMemoryJsonRequest, GetMemoryJsonResponse>(request);

      if (response.IsEmpty())
      {
        throw new System.Exception($"Could not retrieve memory for {this.GetDebugString()}");
      }

      return response.Get().memoryJson;
    }
  }

  public void SetMemoryJson(string newMemoryJson)
  {
    if (authoritativeMemoryJson == newMemoryJson) return;
    authoritativeMemoryJson = newMemoryJson;
    MarkForScriptSync();
    MarkMemoryAsDirty();
  }

  // Marks this actor's memory as being dirty, that is, mark it as needing to broadcast an update.
  public void MarkMemoryAsDirty()
  {
    if (actorMemoryRPCNode.List == null)
    {
      engine.memoryUpdateQueue.AddLast(actorMemoryRPCNode);
    }
  }

  // This is used by script to do resets. Ie. put the actor back to its spawn
  // pos, and zero out velocity. We shouldn't let the script modify velocities
  // directly..longer term, we should probably just expose a function like
  // "ClearVelocity" instead of setting it directly.
  public void SetVelocity(Vector3 newVelocity)
  {
    if (GetRigidBody() != null && Vector3.Distance(GetRigidBody().velocity, newVelocity) > 1e-6)
    {
      GetRigidBody().velocity = newVelocity;
    }
  }

  public Vector3 GetVelocity()
  {
    if (GetRigidBody() == null)
    {
      return Vector3.zero;
    }
    else
    {
      return GetRigidBody().velocity;
    }
  }

  public void SetAngularVelocity(Vector3 newAngularVelocity)
  {
    if (GetRigidBody() != null)
    {
      GetRigidBody().angularVelocity = newAngularVelocity;
    }
  }

  public Vector3 GetAngularVelocity()
  {
    if (GetRigidBody() != null)
    {
      return GetRigidBody().angularVelocity;
    }
    else
    {
      return Vector3.zero;
    }
  }

  bool replicantCatchUpMode = false;

  public void SetReplicantCatchUpMode(bool val)
  {
    if (this.replicantCatchUpMode != val)
    {
      this.replicantCatchUpMode = val;
      UpdateRigidbodyComponent();
    }
  }

  public bool GetReplicantCatchUpMode()
  {
    return this.replicantCatchUpMode;
  }

  public void OnOwnershipTransfered()
  {
    MarkForScriptSync();
    UpdateRigidbodyComponent();
    if (this.debug)
    {
      Log($"ownership xfered. Local? {this.IsLocallyOwned()}");
    }
  }

  // This is a superset of DeserializeNonGenerated. Some of this is scriptable,
  // some of it is just given to scripting as read-only info.
  public void SerializeForScriptSync(NetworkWriter writer)
  {
    // These values here are frequently accessed for every actor, so it's best
    // to NOT use cross-domain accessors for them.
    writer.WriteUtf16(this.GetBrainName());
    writer.WriteVoosBoolean(this.IsLocallyOwned());
    lastSyncedIsLocalValue = this.IsLocallyOwned();
    writer.WriteVoosBoolean(this.GetIsOffstageEffective());
    writer.WriteVoosBoolean(this.authoritativeMemoryJson != null);
    if (this.authoritativeMemoryJson != null)
    {
      writer.WriteUtf16(this.authoritativeMemoryJson);
      this.authoritativeMemoryJson = null;
    }
  }

  public PersistedState Save()
  {
    return PersistedState.NewFrom(this);
  }

  public void UpdateFrom(PersistedState serialized)
  {
    try
    {
      // TODO is this assert kinda irrelevant..?
      Debug.Assert(serialized.name == this.GetName(), "Serialized name did not match expected name");

      this.SetBrainName(serialized.brainName);
      this.SetTags(serialized.tags);
      SetMemoryJson(serialized.memoryJson);
      SetRenderableUri(serialized.renderableUri);

      if (serialized.version < FirstVersionWithUseConcaveCollider)
      {
        // Enable this by default.
        serialized.useConcaveCollider = true;
      }

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_PERSISTED_FIELDS_DESERIALIZE
      SetDisplayName(serialized.displayName);    // GENERATED
      SetDescription(serialized.description);    // GENERATED
      SetTint(serialized.tint);    // GENERATED
      SetTransformParent(serialized.transformParent);    // GENERATED
      SetPosition(serialized.position);    // GENERATED
      SetRotation(serialized.rotation);    // GENERATED
      SetLocalScale(serialized.localScale);    // GENERATED
      SetRenderableOffset(serialized.renderableOffset);    // GENERATED
      SetRenderableRotation(serialized.renderableRotation);    // GENERATED
      SetCommentText(serialized.commentText);    // GENERATED
      SetSpawnPosition(serialized.spawnPosition);    // GENERATED
      SetSpawnRotation(serialized.spawnRotation);    // GENERATED
      SetPreferOffstage(serialized.preferOffstage);    // GENERATED
      SetIsSolid(serialized.isSolid);    // GENERATED
      SetEnablePhysics(serialized.enablePhysics);    // GENERATED
      SetEnableGravity(serialized.enableGravity);    // GENERATED
      SetBounciness(serialized.bounciness);    // GENERATED
      SetDrag(serialized.drag);    // GENERATED
      SetAngularDrag(serialized.angularDrag);    // GENERATED
      SetMass(serialized.mass);    // GENERATED
      SetFreezeRotations(serialized.freezeRotations);    // GENERATED
      SetFreezeX(serialized.freezeX);    // GENERATED
      SetFreezeY(serialized.freezeY);    // GENERATED
      SetFreezeZ(serialized.freezeZ);    // GENERATED
      SetEnableAiming(serialized.enableAiming);    // GENERATED
      SetHideInPlayMode(serialized.hideInPlayMode);    // GENERATED
      SetKeepUpright(serialized.keepUpright);    // GENERATED
      SetIsPlayerControllable(serialized.isPlayerControllable);    // GENERATED
      SetDebugString(serialized.debugString);    // GENERATED
      SetCloneParent(serialized.cloneParent);    // GENERATED
      SetVelocity(serialized.velocity);    // GENERATED
      SetAngularVelocity(serialized.angularVelocity);    // GENERATED
      SetCameraActor(serialized.cameraActor);    // GENERATED
      SetSpawnTransformParent(serialized.spawnTransformParent);    // GENERATED
      SetWasClonedByScript(serialized.wasClonedByScript);    // GENERATED
      SetLoopingAnimation(serialized.loopingAnimation);    // GENERATED
      SetCameraSettingsJson(serialized.cameraSettingsJson);    // GENERATED
      SetLightSettingsJson(serialized.lightSettingsJson);    // GENERATED
      SetPfxId(serialized.pfxId);    // GENERATED
      SetSfxId(serialized.sfxId);    // GENERATED
      SetUseConcaveCollider(serialized.useConcaveCollider);    // GENERATED
      SetSpeculativeColDet(serialized.speculativeColDet);    // GENERATED
      SetUseStickyDesiredVelocity(serialized.useStickyDesiredVelocity);    // GENERATED
      SetStickyDesiredVelocity(serialized.stickyDesiredVelocity);    // GENERATED
      SetStickyForce(serialized.stickyForce);    // GENERATED
                                                 // END_GAME_BUILDER_CODE_GEN

      // TODO we should have code gen behave better with version numbers...I
      // guess it could generate version numbers for us, and we provide default
      // values in the field data!
      SetSpawnRotation(serialized.version >= FirstVersionWithSpawnPositionRotation ? serialized.spawnRotation : serialized.rotation);
      SetSpawnPosition(serialized.version >= FirstVersionWithSpawnPositionRotation ? serialized.spawnPosition : serialized.position);
      SetIsSolid(serialized.version >= FirstVersionWithIsSolid ? serialized.isSolid : true);

      // Before we had this explicit, assume that all controllable actors should be upright.
      SetKeepUpright(serialized.version >= FirstVersionWithExplicitPlayerUpright ? serialized.keepUpright : serialized.isPlayerControllable);

      if (serialized.version < FirstVersionWithAutoPropagateToClones)
      {
        // This is not ideal, but it's better to break the link than to assume
        // users actually want auto-propagation. Users may have made copies, but
        // they then independently edited those copies. It would be pretty bad
        // if they loaded it up now, edited the original, and then the copy's
        // edits were wiped!
        SetCloneParent(null);

        // Bit of a hack: This is also about when I added 'wasClonedByScript',
        // so upgrade that.
        if (serialized.memoryJson.Contains("isClone"))
        {
          SetWasClonedByScript(true);
        }
      }

      if (serialized.version < FirstVersionWithPhysicsAttribs)
      {
        SetMass(1f);
        SetDrag(0f);
        SetAngularDrag(0.05f);
        SetBounciness(0f);
      }

      if (serialized.version < FirstVersionStopHidingPlayers)
      {
        if (GetIsPlayerControllable())
        {
          SetHideInPlayMode(false);
        }
      }
    }
    catch (System.Exception e)
    {
      Debug.LogError("Error while merging for entity " + serialized.name);
      Debug.LogException(e);
      throw e;
    }
  }

  // Depends on: isSolid, enablePhysics, renderable.
  void UpdateTriggerGhost()
  {
    using (Util.Profile("UpdateTriggerGhost"))
    {
      // IMPORTANT: Note that we don't check "isHandlingCollisions" here! That's
      // because even if *this* actor isn't handling collisions, we still
      // actually need the ghost so OTHER actors can collide with it! See
      // 'kinematic-solid-collide-test.voos'
      bool needGhost = GetIsSolid() && !GetEnablePhysics() && !GetIsOffstageEffective();

      foreach (var collider in ghostColliders)
      {
        if (collider != null)
        {
          MonoBehaviour.Destroy(collider);
        }
      }
      ghostColliders.Clear();

      if (!needGhost)
      {
        return;
      }

      Util.AssertUniformScale(this.transform.localScale);

      // Very important that we iterate through the colliders found in the
      // original renderable! Some of our ghosts may still show up, due to
      // delayed destroy.
      foreach (Collider collider in renderableColliders)
      {
        MeshCollider mesh = collider as MeshCollider;
        if (mesh != null)
        {
          Util.AssertUniformScale(mesh.transform.localScale);
          var ghost = mesh.gameObject.AddComponent<MeshCollider>();
          ghostColliders.Add(ghost);
          ghost.convex = true;
          ghost.isTrigger = true; // Must be set after convex
          ghost.sharedMesh = mesh.sharedMesh;
          continue;
        }

        BoxCollider box = collider as BoxCollider;
        if (box != null)
        {
          var ghost = box.gameObject.AddComponent<BoxCollider>();
          ghostColliders.Add(ghost);
          ghost.isTrigger = true;
          ghost.center = box.center;
          ghost.size = box.size;
          continue;
        }

        SphereCollider sphere = collider as SphereCollider;
        if (sphere != null)
        {
          var ghost = sphere.gameObject.AddComponent<SphereCollider>();
          ghostColliders.Add(ghost);
          ghost.isTrigger = true;
          ghost.center = sphere.center;
          ghost.radius = sphere.radius;
          continue;
        }

        CapsuleCollider capsule = collider as CapsuleCollider;
        if (capsule != null)
        {
          var ghost = capsule.gameObject.AddComponent<CapsuleCollider>();
          ghostColliders.Add(ghost);
          ghost.isTrigger = true;
          ghost.center = capsule.center;
          ghost.direction = capsule.direction;
          ghost.height = capsule.height;
          ghost.radius = capsule.radius;
          continue;
        }

        Util.LogError($"Unknown collider type: {collider.GetType().FullName}");
      }
    }
  }

  public void SetRunning(bool running)
  {
    foreach (Animator animator in currentRenderable.GetComponentsInChildren<Animator>())
    {
      animator.speed = running ? 1 : 0;
    }
    foreach (var render in allRenderers)
    {
      if (render.material.HasProperty(Shader.PropertyToID("_Running")))
      {
        render.material.SetInt(Shader.PropertyToID("_Running"), running ? 1 : 0);
      }
    }
    foreach (var particleSystem in currentRenderable.GetComponentsInChildren<ParticleSystem>())
    {
      if (running)
      {
        particleSystem.Play();
      }
      else
      {
        particleSystem.Pause();
      }
    }
  }

  // Called when the effective offstage state might have changed, as a result of the
  // root actor in our hierarchy having changed.
  void OnEffectivelyOffstageChanged()
  {
    // If we can't be offstage and we were put offstage, RESIST!
    // TODO: be smarter about this (change codegen to have a "validation" function?)
    // TODO: technically if we are parented to another actor, we could end up going offstage
    // effectively even though we have preferOffstage = false. We don't detect that case. Maybe we should.
    if (!CanGoOffstage() && preferOffstage)
    {
      // Note: this will cause a recursive call to this function, but it should be ok because on the
      // recursive call preferOffstage will be false, so we won't hit this case again.
      // Let's hope I'm right.
      // If you see infinite recursion here, it's because I'm wrong.
      SetPreferOffstage(false);
      return;
    }

    UpdateIsOffstageEffective();
    UpdateRigidbodyComponent();
    UpdateActorLayer();
    UpdateOffstageGhostRenderable();
    UpdateTriggerGhost();
    offstageChanged?.Invoke();

    // We need to tell all our children to update, because their effective
    // on-stage state will have changed as well.
    foreach (Transform child in transform)
    {
      var actor = child.GetComponent<VoosActor>();
      if (actor != null)
      {
        actor.OnEffectivelyOffstageChanged();
      }
    }
  }

  public bool Raycast(Vector3 origin, out RaycastHit hit, float maxDistance)
  {
    hit = new RaycastHit();
    if (currentRenderable != null)
    {
      Vector3 targetPos = this.ComputeWorldRenderBounds().center;
      Ray ray = new Ray(origin, (targetPos - origin).normalized);
      foreach (Collider collider in renderableColliders)
      {
        RaycastHit currHit;
        if (collider.Raycast(ray, out currHit, maxDistance))
        {
          if (hit.collider == null || currHit.distance < hit.distance)
          {
            hit = currHit;
          }
        }
      }
    }
    return hit.collider != null;
  }

  private void UpdateCommentText()
  {
    if (currentRenderable == null)
    {
      return;
    }
    TextRenderer text = this.currentRenderable.GetComponentInChildren<TextRenderer>();
    if (text != null)
    {
      text.SetText(GetCommentText());
    }
  }

  private void UpdatePfxId()
  {
    if (renderableParticleSystem != null) Destroy(renderableParticleSystem);
    if (!pfxId.IsNullOrEmpty())
    {
      ParticleEffect effect = engine.GetParticleEffectSystem().GetParticleEffect(pfxId);
      if (effect != null)
      {
        renderableParticleSystem = engine.GetParticleEffectSystem().SpawnParticleEffectLocal(
          effect, transform.position, transform.rotation.eulerAngles, 1, true);
        renderableParticleSystem.transform.parent = transform;
        renderableParticleSystem.gameObject.layer = gameObject.layer;
        if (!ShouldShowRenderable())
        {
          renderableParticleSystem.gameObject.SetActive(false);
        }
      }
    }
  }

  private void UpdateSfxId()
  {
    if (loopingAudioSource == null)
    {
      loopingAudioSource = gameObject.AddComponent<AudioSource>();
      loopingAudioSource.loop = true;
    }
    if (sfxId.IsNullOrEmpty())
    {
      loopingAudioSource.clip = null;
    }
    else
    {
      SoundEffect soundEffect = engine.GetSoundEffectSystem()?.GetSoundEffect(sfxId);
      loopingAudioSource.clip = engine.GetSoundEffectSystem()?.GetAudioClip(sfxId);

      if (soundEffect != null && soundEffect.content.spatialized)
      {
        loopingAudioSource.spatialize = true;
        loopingAudioSource.spatialBlend = 1; // 1 = Full 3D
      }
      else
      {
        loopingAudioSource.spatialize = false;
        loopingAudioSource.spatialBlend = 0;
      }
      loopingAudioSource.Play();
    }
  }

  bool ShouldShowRenderable()
  {
    return engine.ShouldRenderActor(this) && (engine.GetIsInEditMode() || !hideInPlayMode);
  }

  // Cheap enough to call each frame.
  void UpdateRenderableHiddenState()
  {
    if (currentRenderable == null)
    {
      return;
    }

    bool shouldShow = ShouldShowRenderable();

    if (isCurrentRenderableShown != shouldShow)
    {
      isCurrentRenderableShown = shouldShow;

      foreach (Renderer r in allRenderers)
      {
        r.enabled = shouldShow;
      }
      if (renderableParticleSystem != null)
      {
        renderableParticleSystem.gameObject.SetActive(shouldShow);
      }
    }
  }

  public Color GetTint()
  {
    return tint;
  }

  public void SetTint(Color newTint)
  {
    // CLAMP! We don't network >1 colors well yet.
    newTint = new Color(newTint.r.Clamp01(), newTint.g.Clamp01(), newTint.b.Clamp01(), newTint.a.Clamp01());

    if (newTint != tint)
    {
      tint = newTint;
      UpdateRenderableTint();
    }
  }

  static int[] PossibleRenderableTintShaderProperties = new int[] {
    Shader.PropertyToID("_Color"),
    Shader.PropertyToID("_MainTint"),
    Shader.PropertyToID("_BaseColorFactor")
  };

  void UpdateRenderableTint()
  {
    if (currentRenderable == null)
    {
      return;
    }

    foreach (var render in tintableRenderers)
    {
      foreach (int prop in PossibleRenderableTintShaderProperties)
      {
        if (render.material.HasProperty(prop))
        {
          render.material.SetColor(prop, tint);
        }
      }
    }

    GetPlayerBody()?.OnTintChanged(tint);
  }

  public void UpdatePhysicsMaterial()
  {
    foreach (Collider collider in renderableColliders)
    {
      collider.material.bounciness = GetBounciness();
      // collider.material.staticFriction = staticFrictionTemp;
      // collider.material.dynamicFriction = dynamicFrictionTemp;
    }
  }

  public void NotifyBrainUndoRedo()
  {
    this.onBrainChanged?.Invoke(true);
  }

  public void SetBrainName(string name)
  {
    if (brainName == name) return;
    brainName = name;
    UpdateCollisionHandling();
    this.onBrainChanged?.Invoke(false);
    MarkForScriptSync();
  }

  public VoosScene GetScene() { return scene; }

  public string GetName() { return voosName; }

  void UpdateGameObjectName()
  {
    this.name = $"{this.GetDisplayName()} ({voosName.Substring(0, 9)})";
    if (onDisplayNameChanged != null)
    {
      onDisplayNameChanged();
    }
  }

  public void SetName(string newName)
  {
    if (voosName != newName)
    {
      string oldName = voosName;
      voosName = newName;
      engine.NotifyNameChange(this, oldName);
      UpdateGameObjectName();
    }
  }

  public void RequestVelocityChange(Vector3 delta)
  {
    if (GetRigidBody() != null)
    {
      pendingVelocityDelta += delta;
    }
    else
    {
      Debug.LogError("Tried to apply velocity change, but this entity has no rigidbody.");
    }
  }

  public void Log(string msg)
  {
    Util.Log($"[{this.name}] {msg}");
  }

  public void UpdateIsPlayerControllable()
  {
    if (GetIsPlayerControllable())
    {
      PlayerBody.MakeActorControllable(this, engine.GetPlayerBodyPartsPrefab());
    }
    else
    {
      engine.NotifyActorBecomingUncontrollable(this);
      PlayerBody.MakeActorNotControllable(this);
    }
    FixTags();
    UpdateHumanoid();

    AssertPlayerBodyInvariant();
  }

  public void AssertPlayerBodyInvariant()
  {
    if (GetIsPlayerControllable())
    {
      Debug.Assert(playerBody != null, "AssetPlayerBodyInvariant: playerBody != null failed");
    }
    else
    {
      Debug.Assert(playerBody == null, "AssetPlayerBodyInvariant: playerBody == null failed");
    }
  }

  public ActorNetworking GetNetworking()
  {
    return actorNetworking;
  }

  public void EnqueueMessage(string name, string argsJson = null)
  {
    engine.EnqueueMessage(new VoosEngine.ActorMessage { targetActor = this.GetName(), name = name, argsJson = argsJson });
  }

  public string GetBrainName()
  {
    return brainName;
  }

  // Returns true if the current model is or will be the given URI.
  public void SetRenderableUri(string newRenderableUri)
  {
    if (this.renderableUri == newRenderableUri)
    {
      return;
    }

    // Immediately set it. Even though it's not currently the renderable, it IS
    // the intended one. This prevents some multiplayer errors.
    this.renderableUri = newRenderableUri;

    if (newRenderableUri.IsNullOrEmpty())
    {
      // Just don't change anything. This is probably temporary, and soon we'll
      // get an update. This can happen in multiplayer.
      return;
    }

    // Get it from the cache.
    outstandingCacheRequests++;
    assetCache.Get(newRenderableUri, cached =>
    {
      outstandingCacheRequests--;
      if (this.renderableUri != newRenderableUri)
      {
        // There must of been another call to SetRenderableUri while we were downloading.
        // We're stale - ignore.
        return;
      }
      this.cachedRenderable = cached;
      SetRenderable(cached.GetAssetClone());
    });
  }

  void OnCachedRenderableChanged()
  {
    SetRenderable(this.cachedRenderable.GetAssetClone());
  }

  public string GetRenderableUri()
  {
    return this.renderableUri;
  }

  public void AddDisplayNameChangedListener(DisplayNameChanged listener)
  {
    onDisplayNameChanged += listener;
  }

  public void RemoveDisplayNameChangedListener(DisplayNameChanged listener)
  {
    onDisplayNameChanged -= listener;
  }

  void SetRenderable(GameObject newRenderable)
  {
    // It's possible the actor was destroyed before SetRenderable was called (it could've been async).
    if (this == null)
    {
      return;
    }

    if (currentRenderable != null)
    {
      Destroy(currentRenderable);
      currentRenderable = null;
    }

    currentRenderable = newRenderable;
    isCurrentRenderableShown = true;

    // We ran into an issue where reparenting -> setting the renderable localScale to 1
    // would cause mesh collision stuttering issues if done during scene load.
    // Now we set the scale to the parent's scale first and then reparent so that the
    // new localScale will resolve to 1. This seems to fix the stuttering issues.
    newRenderable.transform.localScale = this.transform.localScale;
    newRenderable.transform.SetParent(this.transform, true);
    newRenderable.transform.localScale = Vector3.one;
    newRenderable.transform.localRotation = GetRenderableRotation();
    newRenderable.transform.localPosition = GetRenderableOffset();

    // Activate it after we've setup the transform and hierarchy. This is when the physics mesh bake collider stuff will happen.
    // If we did this before setting up the hierarchy, it would incur double costs!
    newRenderable.SetActive(true);

    OnRenderableChanged();
  }

  // TODO the "offstage" or "PrefabWorld" isn't really needed anymore. We could
  // probably clean this up by just leverage SetActive to hide offstage things.
  // We still need the offstage ghost in its own layer so selection-rays work
  // without it interacting with on-stage physics.

  // Deps: isOffstage; parent; renderable; 
  void UpdateActorLayer()
  {
    if (Layer == -1)
    {
      throw new System.Exception("Could not find layer named 'VoosActor' - dev error!");
    }

    // We don't want to set fully recursive from gameObject, since other things
    // are childed to us (like UserBody for players).
    var layer = GetIsOffstageEffective() ? LayerMask.NameToLayer("PrefabWorld") : Layer;
    Util.SetLayerRecursively(currentRenderable, layer);
    if (renderableParticleSystem != null)
    {
      renderableParticleSystem.gameObject.layer = layer;
    }
    gameObject.layer = layer;
  }


  GameObject offstageGhostRenderable;
  private void UpdateOffstageGhostRenderable()
  {
    if (offstageGhostRenderable != null) Destroy(offstageGhostRenderable);

    if (!GetIsOffstageEffective()) return;

    offstageGhostRenderable = Instantiate(currentRenderable);

    Material mat = Resources.Load(OFFSTAGE_GHOST_MAT) as Material;

    foreach (MeshRenderer _render in offstageGhostRenderable.GetComponentsInChildren<MeshRenderer>())
    {
      _render.material = mat;
    }

    //copying SetRenderable function
    offstageGhostRenderable.transform.localScale = this.transform.localScale;
    offstageGhostRenderable.transform.SetParent(this.transform, true);
    offstageGhostRenderable.transform.localScale = Vector3.one;
    offstageGhostRenderable.transform.localRotation = GetRenderableRotation();
    offstageGhostRenderable.transform.localPosition = GetRenderableOffset();

    Util.SetLayerRecursively(offstageGhostRenderable, LayerMask.NameToLayer("PrefabWorld"));
  }

  public void SetOffstageGhostRenderableVisibleAndInteractive(bool on)
  {
    if (offstageGhostRenderable == null) return;
    Util.SetLayerRecursively(offstageGhostRenderable, on ? LayerMask.NameToLayer("OffstageGhost") : LayerMask.NameToLayer("PrefabWorld"));
  }

  public void GetThumbnail(System.Action<Texture2D> callback)
  {
    if (renderableUri == null || renderableUri == "")
    {
      callback(null);
    }
    else
    {
      assetCache.Get(renderableUri, (entry) =>
      {
        callback(entry.GetThumbnail());
      });
    }
  }

  Rigidbody GetRigidBody()
  {
    if (cachedRigidbody == null)
    {
      cachedRigidbody = GetComponent<Rigidbody>();
    }

    return cachedRigidbody;
  }

  public void SetTeleport(bool value)
  {
    teleport = value;
  }

  public bool GetTeleport()
  {
    return teleport;
  }

  void UpdateHumanoid()
  {
    if (GetIsPlayerControllable())
    {
      // TODO: make this an interface instead of trying to find concrete implementations:
      var biped = this.currentRenderable.GetComponentInChildren<NPCBiped>();
      var robot = this.currentRenderable.GetComponentInChildren<RobotCharacter>();

      var body = GetPlayerBody();
      if (biped != null && body != null)
      {
        biped.SetDriver(body);
      }
      if (robot != null && body != null)
      {
        robot.SetDriver(body);
      }
      if (nonPlayerBipedDriver != null)
      {
        MonoBehaviour.Destroy(nonPlayerBipedDriver);
        nonPlayerBipedDriver = null;
      }
    }
    else
    {
      // Not player controllable, so create a non player biped driver.
      if (nonPlayerBipedDriver == null)
      {
        nonPlayerBipedDriver = gameObject.AddComponent<NonPlayerBipedDriver>();
        nonPlayerBipedDriver.Setup(this);
      }
    }
  }

  void OnRenderableChanged()
  {
    meshRenderers = this.currentRenderable.GetComponentsInChildren<MeshRenderer>();
    allRenderers = this.currentRenderable.GetComponentsInChildren<Renderer>();
    List<Renderer> tintable = new List<Renderer>();
    foreach (Renderer r in allRenderers)
    {
      RenderableOptions options = r.GetComponent<RenderableOptions>();
      if (options != null && options.dontApplyTint)
      {
        // This renderable is specifically marked as not receiving tint. Skip it.
        continue;
      }
      tintable.Add(r);
    }
    tintableRenderers = tintable.ToArray();
    renderableColliders = this.currentRenderable.GetComponentsInChildren<Collider>();
    renderableAnimation = this.currentRenderable.GetComponent<Animation>();

    // TEMP force disable instancing for now. Most of our materials get
    // instanced anyway, due to setting tint, so TRYING to instance actually
    // introduces HUGE CPU rendering overhead.
    foreach (Renderer r in allRenderers)
    {
      if (r.sharedMaterial == null) continue;
      r.sharedMaterial.enableInstancing = false;
    }

    // Bit of a hack: if the renderable has animation, and no looping animation
    // has been set, then set it to the default clip.
    if (renderableAnimation != null
      && renderableAnimation.clip != null
      && GetLoopingAnimation().IsNullOrEmpty())
    {
      SetLoopingAnimation(renderableAnimation.clip.name);
    }

    UpdateActorLayer();
    UpdateOffstageGhostRenderable();
    UpdateRenderableTint();
    UpdateCommentText();
    UpdateColliders();
    UpdateRenderableHiddenState();
    UpdateRenderableOffset();
    UpdateRenderableRotation();
    UpdateTriggerGhost();
    UpdateAnimation();
    UpdateHumanoid();
    UpdatePhysicsMaterial();
    UpdatePfxId();

    resetInertiaTensorPending = true;
  }

  // This handles MeshCollider.convex and Collider.isTrigger. This must be in
  // the same update function, since !convex && isTrigger is invalid.
  void UpdateColliders()
  {
    if (currentRenderable == null)
    {
      return;
    }

    // Concave colliders only works for static, non-triggers.
    bool concave = GetUseConcaveCollider() && GetIsSolid() && !GetEnablePhysics();
    bool isTrigger = !GetIsSolid();

    foreach (var collider in renderableColliders)
    {
      var meshCol = collider as MeshCollider;
      if (meshCol != null)
      {
        // To be safe, first make it non-trigger. This is OK for convex or
        // concave. Otherwise, Unity will just log a warning and ignore your
        // sets..
        meshCol.isTrigger = false;
        meshCol.convex = !concave;

        Debug.Assert(!(isTrigger && concave), "isTrigger AND concave is invalid! Should not happen.");
        // Now whatever convex is should be perfectly compatible with isTrigger, as checked by the assert.
        meshCol.isTrigger = isTrigger;
      }
      else
      {
        collider.isTrigger = isTrigger;
      }
    }
  }

  public PlayerBody GetPlayerBody()
  {
    AssertPlayerBodyInvariant();
    return playerBody;
  }

  // foreach(string label in actor.EnumerateBehaviorLabels()) { ... }
  public IEnumerable<string> EnumerateBehaviorLabels()
  {
    var brain = behaviors.GetBrain(GetBrainName());
    foreach (var use in brain.behaviorUses)
    {
      yield return behaviors.GetBehaviorData(use.behaviorUri).GetInlineCommentLabel();
    }
  }

  float lastImpulseTime = 0f;

  public void HandleMessageFromScript(VoosEngine.ActorMessage message)
  {
    switch (message.name)
    {
      case "Damaged":
        GetPlayerBody()?.OnDamagedMessage();
        break;
      case "Died":
        GetPlayerBody()?.OnDiedMessage();
        break;
      case "Respawned":
        GetPlayerBody()?.OnRespawnedMessage();
        break;
      case "Jumped":
        GetPlayerBody()?.OnJumpedMessage();
        break;
    }
  }

  public BehaviorSystem GetBehaviorSystem()
  {
    return behaviors;
  }

  public void SetLocalScale(Vector3 scale)
  {
    this.transform.localScale = scale;
  }

  public Vector3 GetLocalScale()
  {
    return this.transform.localScale;
  }

  void CheckMeshRenderers()
  {
    foreach (var renderer in meshRenderers)
    {
      if (renderer == null)
      {
        Util.LogError($"Actor {this.GetDebugString()} had a null mesh renderer. RenderableURI: {this.GetRenderableUri()}");
      }
    }
  }

  public Bounds ComputeWorldRenderBounds()
  {
    if (meshRenderers.Length > 0)
    {
      // To help diagnose an NPE.
      CheckMeshRenderers();

      // Only use mesh renderers..things like particles, etc. can get big and
      // usually not what you want.
      return Util.ComputeWorldRenderBounds(meshRenderers);
    }
    else
    {
      return new Bounds(this.transform.position, new Vector3(1, 1, 1));
    }
  }

  public Vector3 GetWorldRenderBoundsSize()
  {
    return ComputeWorldRenderBounds().size;
  }

  public Vector3 GetWorldRenderBoundsCenter()
  {
    return ComputeWorldRenderBounds().center;
  }

  public bool IsWaitingForRenderable()
  {
    return outstandingCacheRequests > 0;
  }

  [PunRPC]
  private void SetMemoryRPC(string memory)
  {
    this.SetMemoryJson(memory);
  }

  bool TagsAreDifferent(string[] newTags)
  {
    newTags = newTags ?? new string[0];

    if (newTags.Length != this.tags.Count)
    {
      return true;
    }

    foreach (string newTag in newTags)
    {
      if (!this.tags.Contains(newTag))
      {
        return true;
      }
    }

    return false;
  }

  public void SetTags(string[] newTags)
  {
    if (!TagsAreDifferent(newTags))
    {
      return;
    }

    this.tags.Clear();
    foreach (string tag in newTags)
    {
      if (!this.tags.Contains(tag))
      {
        this.tags.Add(tag);
      }
    }
    FixTags();
  }

  void FixTags()
  {
    // Players always have the "player" tag.
    // We need to set this here because (1) needs to be set when loading an old file;
    // (2) the player might accidentally delete the tag and then nothing would work.
    if (isPlayerControllable && !this.tags.Contains("player"))
    {
      this.tags.Add("player");
    }
  }

  public IReadOnlyCollection<string> GetTags()
  {
    return this.tags;
  }

  public bool HasTag(string tag)
  {
    return this.tags.Contains(tag);
  }

  public int GetPrimaryPhotonViewId()
  {
    return reliablePhotonView != null ? reliablePhotonView.viewID : 0;
  }

  public int GetPhotonOwnerId()
  {
    return reliablePhotonView != null ? reliablePhotonView.ownerId : 0;
  }

  public PhotonPlayer GetPhotonOwner()
  {
    return reliablePhotonView != null ? reliablePhotonView.owner : null;
  }

  public string GetOwnerNickName()
  {
    if (this == null || reliablePhotonView == null || reliablePhotonView.owner == null)
    {
      return "(View or owner is null)";
    }
    else
    {
      return reliablePhotonView.owner.NickName;
    }
  }

  public bool CanShowCommentText()
  {
    return GetComponentInChildren<TextRenderer>() != null;
  }

  public Vector3 GetPosition()
  {
    if (transform == null)
    {
      Util.LogError($"VoosActor {this.name} has a null transform..?");
    }
    return transform.position;
  }

  public Vector3 GetLocalPosition()
  {
    if (transform == null)
    {
      Util.LogError($"VoosActor {this.name} has a null transform..?");
    }
    return transform.localPosition;
  }

  public void SweepTo(Vector3 newPosition)
  {
    var rb = GetRigidBody();

    if (rb != null && rb.isKinematic && engine.GetIsRunning())
    {
      // For kinematic RBs, we want to use moveRequest stuff so they collide
      // through other dynamic RBs. Test case: place a bunch of boulders on the
      // ground. Move a box through them. The box should push the boulders
      // around.
      moveRequestPending = true;
      moveRequestTargetPosition = newPosition;
    }
    else
    {
      // In all other cases, just set position directly. However, do NOT set
      // teleport to true, so other clients see this as a smooth motion.
      transform.position = newPosition;
    }
  }

  public void SetPosition(Vector3 newPosition)
  {
    SetPositionImpl(false, newPosition);
  }

  public void SetLocalPosition(Vector3 newLocalPosition)
  {
    SetPositionImpl(true, newLocalPosition);
  }

  private void SetPositionImpl(bool isLocal, Vector3 newPosition)
  {
    if (isLocal)
    {
      transform.localPosition = newPosition;
    }
    else
    {
      transform.position = newPosition;
    }

    // Make sure we clear any moves. Otherwise, the next FixedUpdate could try
    // to move it back to some stale position.
    moveRequestPending = false;

    // Make sure other peers know their replicants should get teleported
    // (communicated through GlobalUnreliableData). SetPosition is ALWAYS a
    // teleport.
    this.teleport = true;
  }

  public Quaternion GetRotation()
  {
    return transform.rotation;
  }

  public Quaternion GetLocalRotation()
  {
    return transform.localRotation;
  }

  public void SetRotation(Quaternion newRotation, bool hackAdjustPlayerBodyLastAimRotation = false)
  {
    SetRotationImpl(false, newRotation);
    if (hackAdjustPlayerBodyLastAimRotation && GetPlayerBody() != null)
    {
      GetPlayerBody().HackAdjustLastAimRotation(newRotation);
    }
  }

  public void SetLocalRotation(Quaternion newRotation)
  {
    SetRotationImpl(true, newRotation);
  }

  private void SetRotationImpl(bool isLocal, Quaternion newRotation)
  {
    if (isLocal)
    {
      transform.localRotation = newRotation;
    }
    else
    {
      transform.rotation = newRotation;
    }
    MaybeCorrectRotation();
  }

  private void UpdateRenderableRotation()
  {
    if (currentRenderable)
    {
      currentRenderable.transform.localRotation = GetRenderableRotation();
    }

    if (offstageGhostRenderable)
    {
      offstageGhostRenderable.transform.localRotation = GetRenderableRotation();
    }
  }

  private void UpdateRenderableOffset()
  {
    if (currentRenderable)
    {
      currentRenderable.transform.localPosition = GetRenderableOffset();
    }


    if (offstageGhostRenderable)
    {
      offstageGhostRenderable.transform.localPosition = GetRenderableOffset();
    }
  }

  void Update()
  {
    UpdateRenderableHiddenState();
    UpdateLightState();

    if (IsLocallyOwned() != lastSyncedIsLocalValue)
    {
      MarkForScriptSync();
    }
  }

  void OnDestroy()
  {
    this.cachedRenderable = null;

    if (reliablePhotonView != null)
    {
      int viewID = reliablePhotonView.viewID;
      PhotonNetwork.ReleaseIdOfView(reliablePhotonView); // This suppresses the "viewID still in use!" warnings.
      PhotonNetwork.UnAllocateViewID(viewID);
    }

    engine.GetParticleEffectSystem().onParticleEffectChanged -= OnParticleEffectUpdated;
    engine.GetParticleEffectSystem().onParticleEffectRemoved -= OnParticleEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectChanged -= OnSoundEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectRemoved -= OnSoundEffectUpdated;
    engine.GetSoundEffectSystem().onSoundEffectLoaded -= OnSoundEffectUpdated;

    engine.NotifyActorDestroyed(this);
  }

  public void PrepareToBeDestroyed()
  {
    // Unparent one-shot audio sources so they will continue to play.
    foreach (OneShotAudioSource source in GetComponentsInChildren<OneShotAudioSource>())
    {
      source.gameObject.transform.SetParent(null, true);
    }
  }

  internal void NotifyEditModeToggled()
  {
    // Technically don't need this, but it prevents a flicker when going into play mode.
    UpdateRenderableHiddenState();
  }

  // Convenience function to apply a world-space rotation. Mostly because we
  // don't want to rely on accessing .transform
  public void Rotate(float xAngle, float yAngle, float zAngle)
  {
    // Heh heh!
    transform.Rotate(xAngle, yAngle, zAngle, Space.Self);
    SetRotation(transform.rotation);
  }

  public void RotateAround(Vector3 point, Vector3 axis, float angleDegrees, bool hackAdjustPlayerBodyLastAimRotation = false)
  {
    transform.RotateAround(point, axis, angleDegrees);
    SetRotation(transform.rotation, hackAdjustPlayerBodyLastAimRotation);
  }

  public bool IsLocallyOwned()
  {
    return this.GetNetworking().IsLocal();
  }

  public void WantLock()
  {
    engine.WantLockForActor(this);
  }

  public void UnwantLock()
  {
    engine.UnwantLockForActor(this);
  }

  // If this actor is locked by another, this will do nothing. If this actor is
  // already owned by us, this does nothing. Otherwise, it will send an
  // ownership request to the current owner. It could take many frames for the
  // request to be granted, or maybe the owner locked it just as we were
  // requesting, and it will be denied.
  public void RequestOwnership(VoosEngine.OwnRequestReason reason = VoosEngine.OwnRequestReason.Default)
  {
    if (reliablePhotonView == null || reliablePhotonView.isMine)
    {
      return;
    }

    if (IsLockedByAnother())
    {
      return;
    }

    engine.RequestOwnership(this, reason);
  }

  // NOTE: We should probably just have actor networking be an object that we
  // own, and we return networking.GetIsLockedByOwner here.
  public void SetLockingOwnership_ONLY_FOR_ACTOR_NETWORKING(bool val)
  {
    if (lockedIfNotOwned != val)
    {
      lockedIfNotOwned = val;
      onLockChanged?.Invoke();
    }
  }

  public bool IsLockWantedLocally()
  {
    return engine.IsLockWantedForActor(this);
  }

  public void ApplyPropertiesToClones()
  {
    VoosActor actor = this;
    VoosActor.PersistedState props = VoosActor.PersistedState.NewFrom(actor);

    foreach (VoosActor copy in engine.EnumerateCopiesOf(actor))
    {
      if (copy.IsLockedByAnother())
      {
        Util.LogError($"We tried to ApplyActorPropertiesToClones to an actor locked by another..");
        continue;
      }

      copy.RequestOwnership();

      // Overwrite all properties, with some sensible exceptions.
      props.name = copy.GetName();
      props.localScale = copy.GetLocalScale(); // TODO do we want to keep this or let it get overwritten?
      props.cloneParent = copy.GetCloneParent();
      props.displayName = copy.GetDisplayName();
      props.position = copy.GetPosition();
      props.rotation = copy.GetRotation();
      props.transformParent = copy.GetTransformParent();
      props.spawnPosition = copy.GetSpawnPosition();
      props.spawnRotation = copy.GetSpawnRotation();
      props.spawnTransformParent = copy.GetSpawnTransformParent();
      props.preferOffstage = copy.GetPreferOffstage();
      props.wasClonedByScript = copy.GetWasClonedByScript();
      copy.UpdateFrom(props);
    }

  }

  public void ApplyBrainNameToClones()
  {
    foreach (VoosActor clone in engine.EnumerateCopiesOf(this))
    {
      if (clone.IsLockedByAnother())
      {
        Util.LogError($"We tried to ApplyBrainNameToClonesApplyBrainNameToClones to an actor locked by another..");
        continue;
      }

      clone.RequestOwnership();
      clone.SetBrainName(GetBrainName());
    }
  }

  public bool IsLockedByAnother()
  {
    if (reliableView == null)
    {
      return false;
    }
    return (!IsLocallyOwned() && lockedIfNotOwned) ||
      (IsCloneParentLockedByAnother() && !GetWasClonedByScript());
  }

  public bool IsCloneParentLockedByAnother()
  {
    VoosActor parent = this.GetCloneParentActor();
    return parent != null && parent.IsLockedByAnother();
  }

  public string GetLockingOwnerNickName()
  {
    if (this.GetCloneParentActor() != null)
    {
      return this.GetCloneParentActor().GetOwnerNickName();
    }
    else
    {
      return this.GetOwnerNickName();
    }
  }

  public VoosActor GetCloneParentActor()
  {
    if (GetCloneParent().IsNullOrEmpty())
    {
      return null;
    }
    return engine.GetActor(GetCloneParent());
  }

  public string GetJoinedTags()
  {
    return string.Join(",", this.GetTags());
  }

  public void SetJoinedTags(string newValue)
  {
    this.SetTags(newValue.Split(','));
  }

  public override string ToString()
  {
    return $"{displayName} ({voosName.Substring(0, 9)})";
  }

  public bool GetIsOffstageEffective()
  {
    return cachedIsOffstageEffective;
  }

  public void UpdateIsOffstageEffective()
  {
    // Our effective on/off stage state depends on the root of our hierarchy.
    bool newValue = GetRootActor().GetPreferOffstage();
    if (cachedIsOffstageEffective == newValue) return;

    cachedIsOffstageEffective = newValue;
    MarkForScriptSync();
  }

  public VoosActor GetParentActor()
  {
    return GetTransformParent().IsNullOrEmpty() ? null : engine.GetActor(GetTransformParent());
  }

  public bool IsParented()
  {
    return GetParentActor() != null;
  }

  public VoosActor GetRootActor()
  {
    VoosActor actor = this;
    VoosActor parent;
    int iters = 0;
    while (null != (parent = actor.GetParentActor()))
    {
      actor = parent;
      if (++iters > 20)
      {
        // Something weird is going on.
        throw new Exception("Hierarchy too deep. Is there a cycle in the hierarchy?? " + actor.name);
      }
    }
    return actor;
  }

  public bool IsBuiltinActor()
  {
    if (voosName.IsNullOrEmpty())
    {
      return false;
    }
    return voosName.StartsWith("builtin:", StringComparison.InvariantCulture);
  }

  public bool CanGoOffstage()
  {
    if (GetIsPlayerControllable())
    {
      return false;
    }
    if (GetName() == "__GameRules__")
    {
      return false;
    }
    return true;
  }

  // Depends on: loopingAnimation, currentRenderable
  private void UpdateAnimation()
  {
    if (currentRenderable == null || renderableAnimation == null)
    {
      return;
    }

    var anim = renderableAnimation;
    string loopName = GetLoopingAnimation();

    if (loopName.IsNullOrEmpty())
    {
      anim.Stop();
      return;
    }

    AnimationState loopState = anim[loopName];
    if (loopState == null)
    {
      Debug.LogWarning($"looping animation '{loopName}' could not be found in renderable {GetRenderableUri()} for actor {GetDisplayName()}");
      return;
    }

    if (anim.IsPlaying(loopName) && loopState.wrapMode == WrapMode.Loop)
    {
      return;
    }

    loopState.wrapMode = WrapMode.Loop;
    anim.CrossFade(loopName, 0.5f, PlayMode.StopAll);
  }

  // Will play the given one-shot animation, then return to the looping
  // animation (if any) upon completion.
  public void PlayOneShotAnimation(string animName)
  {
    var anim = renderableAnimation;
    if (anim == null)
    {
      Debug.LogWarning($"Renderable with URI {GetRenderableUri()} does not have any animations");
      return;
    }

    if (animName == GetLoopingAnimation())
    {
      // Ignore this request. It's already playing and always will.
      return;
    }

    var state = anim[animName];
    if (state == null)
    {
      Debug.LogWarning($"Could not find requested animation name: {animName} for renderable URI {GetRenderableUri()}");
      return;
    }

    state.wrapMode = WrapMode.Once;
    anim.Rewind(animName);
    anim.CrossFade(animName, 0.5f, PlayMode.StopAll);

    if (!GetLoopingAnimation().IsNullOrEmpty())
    {
      // Also queue up the looping animation, so it resumes once this one shot
      // is done.
      anim.CrossFadeQueued(GetLoopingAnimation(), 0.5f, QueueMode.CompleteOthers, PlayMode.StopAll);
    }
  }

  public void PlayClip(AudioClip clip, float volume, bool spatialized)
  {
    OneShotAudioSource.Play(gameObject, clip, volume, spatialized);
  }

  public bool IsRenderableDefaultPlayerAvatar()
  {
    return AVATAR_EXPLORER_URI == renderableUri;
  }

  private void UpdateControllingVirtualPlayerId()
  {
    SetIsPlayerControllable(!controllingVirtualPlayerId.IsNullOrEmpty());
  }

  public bool IsSystemActor()
  {
    return voosName != null && voosName.StartsWith(SYSTEM_ACTOR_NAME_PREFIX);
  }

  public bool IsPlayerPlaceholder()
  {
    return voosName != null && voosName.StartsWith(PLAYER_PLACEHOLDER_NAME_PREFIX);
  }

  public bool IsCenterOnScreen(Camera camera)
  {
    Vector3 viewportPosition = camera.WorldToViewportPoint(this.GetWorldRenderBoundsCenter());
    // Debug.Log(actor.GetDisplayName() + ":" + viewportPosition);
    return viewportPosition.x >= 0 &&
      viewportPosition.x <= 1 &&
      viewportPosition.y >= 0 &&
      viewportPosition.y <= 1 &&
      viewportPosition.z > 0;
  }

  public bool ShouldPersist()
  {
    return !IsPlayerPlaceholder();
  }

  public VoosEngine GetEngine() { return engine; }

  IEnumerator RequestOwnershipThenCoroutine(System.Action then)
  {
    float t0 = Time.realtimeSinceStartup;
    RequestOwnership();
    while (true)
    {
      if (this == null || this.gameObject == null || this.reliablePhotonView == null)
      {
        // We've been destroyed - terminate all requests.
        yield break;
      }

      if (this.IsLockedByAnother())
      {
        // Eh, no can do.
        yield break;
      }

      if (IsLocallyOwned())
      {
        // Ready to go!
        break;
      }

      if (Time.realtimeSinceStartup - t0 > 5f)
      {
        // Time out fail. Oh well.
        yield break;
      }

      // Keep waiting..
      yield return null;
    }
    then();
  }

  public void RequestOwnershipThen(System.Action then)
  {
    if (this.IsLockedByAnother())
    {
      return;
    }
    StartCoroutine(RequestOwnershipThenCoroutine(then));
  }

  public string GetDebugName()
  {
    return this.gameObject.name;
  }

  public void OnPreVoosUpdate()
  {
    // These settings are temporary (must be set on every VOOS update):
    SetUseDesiredVelocity(false);
    SetDesiredVelocity(Vector3.zero);
    SetIgnoreVerticalDesiredVelocity(false);
  }

  private void UpdateLightState()
  {
    LightSettings lightSettings = GetLightSettings();

    // Create or destroy light as appropriate.
    bool wantLight = lightSettings.range > 0 && !GetIsOffstageEffective() && !hideInPlayMode;
    if (wantLight && null == actorLight)
    {
      // I don't have a light but should have. So create it.
      GameObject lightObj = new GameObject("LIGHT:" + GetName());
      actorLight = lightObj.AddComponent<Light>();
      actorLight.type = LightType.Point;
      actorLight.intensity = 1;
      lightObj.transform.SetParent(gameObject.transform, false);
    }
    else if (!wantLight && null != actorLight)
    {
      // I have a light and shouldn't have. So destroy it.
      GameObject.Destroy(actorLight.gameObject);
      actorLight = null;
    }

    // Update light settings.
    if (actorLight != null)
    {
      actorLight.range = lightSettings.range;
      actorLight.color = lightSettings.color ?? GetTint();
      // Add an offset the light position so the light doesn't
      // disappear into the ground when the actor is standing on the ground. A bit hacky...
      actorLight.transform.localPosition = lightSettings.offset + LIGHT_LOCAL_Y_OFFSET * Vector3.up;
      actorLight.renderMode =
        GameBuilderApplication.GetQuality() == GameBuilderApplication.Quality.Low
        ? LightRenderMode.ForceVertex
        : LightRenderMode.Auto;
    }
  }

  public void OnQualitySettingsChanged()
  {
    UpdateLightState();
  }

  private CameraSettings? cachedCameraSettings = new CameraSettings();
  public CameraSettings GetCameraSettings()
  {
    if (cachedCameraSettings == null)
    {
      string json = GetCameraSettingsJson();
      cachedCameraSettings = json.IsNullOrEmpty() ? new CameraSettings() : JsonUtility.FromJson<CameraSettings>(json);
    }
    return cachedCameraSettings.Value;
  }

  public void UpdateCameraSettingsJson()
  {
    cachedCameraSettings = null;
  }

  private LightSettings? cachedLightSettings = new LightSettings();
  public LightSettings GetLightSettings()
  {
    if (cachedLightSettings == null)
    {
      string json = GetLightSettingsJson();
      cachedLightSettings = json.IsNullOrEmpty() ? new LightSettings() : JsonUtility.FromJson<LightSettings>(json);
    }
    return cachedLightSettings.Value;
  }

  public void UpdateLightSettingsJson()
  {
    cachedLightSettings = null;
  }

  private bool HasChildrenActors()
  {
    for (int i = 0; i < transform.childCount; i++)
    {
      if (transform.GetChild(i).GetComponent<VoosActor>() != null)
      {
        return true;
      }
    }
    return false;
  }

  public void UpdateCollisionHandling()
  {

    bool handling =
      // Only actors whose brains handle collisions need this. But if we have
      // children, then we need to handle collisions because one of our
      // descendants might want to.
      // TODO: Optimize this. We could do all hierarchy-iteration on the JS side
      // to make it fast.
      (HasChildrenActors() || behaviors.DoesBrainHandleCollisions(GetBrainName()));

    if (isHandlingCollisions == handling)
    {
      return;
    }

    if (handling)
    {
      collisionHandling = gameObject.AddComponent<CollisionHandling>();
      collisionHandling.engine = engine;
      collisionHandling.actor = this;
    }
    else
    {
      Debug.Assert(collisionHandling != null, "Actor was handling collisions before, but did not have valid collisionHandling reference?");
      Destroy(collisionHandling);
    }
  }

  void UpdateCollisionStayTracker()
  {
    if (GetIsPlayerControllable())
    {
      if (collisionStayTrackerForPlayer == null)
      {
        collisionStayTrackerForPlayer = this.gameObject.AddComponent<CollisionStayTracker>();
      }
    }
    else
    {
      if (collisionStayTrackerForPlayer != null)
      {
        Destroy(collisionStayTrackerForPlayer);
        collisionStayTrackerForPlayer = null;
      }
    }
  }

  public void ForceReplicantPosition(Vector3 pos)
  {
    Debug.Assert(!IsLocallyOwned(), "ForceReplicantPosition on local actor");

    var rb = GetRigidBody();
    if (rb != null)
    {
      rb.MovePosition(pos);
    }
    else
    {
      transform.position = pos;
    }
  }

  public void ForceReplicantRotation(Quaternion rot)
  {
    Debug.Assert(!IsLocallyOwned(), "ForceReplicantRotation on local actor");

    var rb = GetRigidBody();
    if (rb != null)
    {
      rb.MoveRotation(rot);
    }
    else
    {
      transform.rotation = rot;
    }
  }

  public bool GetIsGrounded()
  {
    if (this.GetPlayerBody() == null)
    {
      return false;
    }

    return this.GetPlayerBody().GetIsTouchingGround();
  }

  // Sets the spawn position and rotation of the entire actor family (ancestors and descendants)
  // to their current position.
  //
  // Q: Why set it for the entire family rather than just the descendants?
  // A: Because when the user moves a child to a new position, they expect its position relative
  //    to the parent to be preserved on reset. And this is only possible if we also set the
  //    parent's spawn position too. If we didn't, it would look completely broken.
  public void SetSpawnPositionRotationOfEntireFamily()
  {
    Debug.Assert(!moveRequestPending, "SetSpawnPositionRotationOfEntireFamily can't be called when there's a move request pending, because it would yield wrong results. Wait for a FixedUpdate before calling.");
    foreach (VoosActor familyMember in GetRootActor().GetComponentsInChildren<VoosActor>())
    {
      familyMember.SetSpawnPosition(familyMember.GetPosition());
      familyMember.SetSpawnRotation(familyMember.GetRotation());
    }
  }

  void OnEnable()
  {
    GameBuilderApplication.onQualityLevelChanged += UpdateLightState;
  }

  void OnDisable()
  {
    GameBuilderApplication.onQualityLevelChanged -= UpdateLightState;
  }

  // Returns the *immediate* children actors of this actor.
  public IEnumerable<VoosActor> GetChildActors()
  {
    for (int i = 0; i < transform.childCount; i++)
    {
      VoosActor child = transform.GetChild(i).GetComponent<VoosActor>();
      if (child != null)
      {
        yield return child;
      }
    }
  }

  public IEnumerable<VoosActor> DepthFirstSearch()
  {
    foreach (VoosActor child in GetChildActors())
    {
      foreach (VoosActor descendent in child.DepthFirstSearch())
      {
        yield return descendent;
      }
    }
    yield return this;
  }

  // True for all colliders except trigger-ghosts. This is usually not a
  // problem, except for the extremely specific case of concave-mesh-colliders
  // with trigger ghosts..in that case, it's important to ignore trigger ghosts
  // for picking, as they MUST be convex. Burger-on-kitchen-table problem.
  public bool IsRenderableCollider(Collider collider)
  {
    return Array.IndexOf(renderableColliders, collider) >= 0;
  }

  internal void RequestTorque(Vector3 torque)
  {
    pendingTorques.Add(torque);
  }
}
