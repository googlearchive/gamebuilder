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

[RequireComponent(typeof(PhotonView))]
public class UserBody : Photon.MonoBehaviour, PlayerBody.EventHandler, IPunObservable
{
  [System.Serializable]
  public struct CreatePreviewSettings
  {
    public RenderableReference renderable;
    public Vector3 renderableOffset;
    public Quaternion renderableRotation;
    public Quaternion addlRotation;
    public Vector3 scale;
  }

  public interface NetworkableToolEffect
  {
    void SetActive(bool active);
    void OnLateUpdate();
    void SetSpatialAudio(bool enabled);
    void SetRayOriginTransform(Transform origin);
    void SetTint(Color tint);
    GameObject GetGameObject();

    void SetTargetPosition(Vector3 position);
    void SetReceivedTargetActor(VoosActor actor);
  }

  public Transform toolEmissionAnchor;
  public Transform thirdPersonCameraPivotAnchor;
  [SerializeField] Animator animator;
  [SerializeField] Transform playAvatar;
  [SerializeField] Transform editAvatar;
  [SerializeField] AvatarAudioController avatarAudioController;
  [SerializeField] GameBuilderStage stage;
  [SerializeField] AvatarMaterialControl avatarMaterialControl;
  [SerializeField] CreateToolPreview createPreviewPrefab;

  public Transform playAvatarWheel;

  MeshRenderer[] playAvatarRenderers;

  [SerializeField] MeshRenderer[] renderersForTint;
  [SerializeField] MeshRenderer[] variableTransparencyRenderers;

  bool variableTransparencyRenderersVisible = true;
  bool playAvatarRendererEnabled = true;
  public Color currentTint;
  bool inPlayMode = true;
  // NOTE: Not really happy with tool effect networking..but it works.
  [SerializeField] GameObject[] toolEffectPrefabs;

  [System.Serializable]
  public struct GroundEffect
  {
    public GameBuilderStage.GroundType groundType;
    public GameObject effectObject;
  }
  [SerializeField] GroundEffect[] groundEffects;

  // Set non-null if this is a local user body and the local user is using this tool.
  Tool locallyActiveTool = null;

  // Only relevant to remote bodies
  NetworkableToolEffect toolEffectReplicant = null;
  string currentToolEffectName = null;
  int receivedToolTargetViewId;

  // Networking create preview
  string lastCreatePreviewSettingsJson = null;
  CreateToolPreview createPreviewInst = null;

  Vector3 lastReceivedTargetPos = Vector3.zero;
  Vector3 lerpedTargetPos = Vector3.positiveInfinity;

  bool isLocalPlayer = false;
  bool setupCalled = false;

  Vector3 velocityForMultiplayer = Vector3.zero;
  Vector3 lastPosForMultiplayer = Vector3.zero;

  public void SetActiveTool(Tool tool)
  {
    locallyActiveTool = tool;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref stage);
    playAvatarRenderers = playAvatar.GetComponentsInChildren<MeshRenderer>(true /* include inactive children */);
    OnUpdateGroundType();
  }

  void OnEnable()
  {
    stage.OnUpdateGroundType += OnUpdateGroundType;
  }

  void OnDisable()
  {
    stage.OnUpdateGroundType -= OnUpdateGroundType;
  }

  void OnUpdateGroundType()
  {
    foreach (var effect in groundEffects)
    {
      effect.effectObject.SetActive(effect.groundType == stage.GetGroundType());
    }
  }

  public void UpdateVelocity(Vector2 velocity)
  {
    if (isLocalPlayer)
    {
      animator.SetFloat("VelX", velocity.x, 0.5f, Time.unscaledDeltaTime * 7.5f);
      animator.SetFloat("VelY", velocity.y, 0.5f, Time.unscaledDeltaTime * 7.5f);
    }
    avatarAudioController.UpdateVelocity(velocity.x, velocity.y);
  }

  public void SetIsPlayingAsRobot(bool val)
  {
    avatarAudioController.SetIsPlayingAsRobot(val);
  }

  public void SetInPlayMode(bool value)
  {
    if (inPlayMode != value)
    {
      inPlayMode = value;
      OnInPlayModeChanged();
    }
  }


  void UpdateReplicantToolEffectTargetPos()
  {
    if (Single.IsInfinity(lastReceivedTargetPos.x)) return;

    if (Single.IsInfinity(lerpedTargetPos.x))
    {
      lerpedTargetPos = lastReceivedTargetPos;
    }
    else
    {
      lerpedTargetPos = Vector3.Lerp(lerpedTargetPos, lastReceivedTargetPos, 0.5f);
    }

    toolEffectReplicant?.SetTargetPosition(lerpedTargetPos);

    if (createPreviewInst != null)
    {
      createPreviewInst.transform.position = lerpedTargetPos;
    }
  }

  void Update()
  {
    if (!isLocalPlayer)
    {
      //find velocity
      //Vector3 velocityForMultiplayer = Vector3.Lerp( Vector3.zero;
      velocityForMultiplayer = Vector3.Lerp(velocityForMultiplayer, (transform.position - lastPosForMultiplayer) / Time.unscaledDeltaTime, .1f);
      Vector3 relativeVelocity = Quaternion.Inverse(Quaternion.LookRotation(transform.forward)) * velocityForMultiplayer;

      //makes it smaller for the animator
      float x = relativeVelocity.x / NavigationControls.MOVE_SPEED;
      float y = relativeVelocity.z / NavigationControls.MOVE_SPEED;
      UpdateVelocity(new Vector2(x, y));

      lastPosForMultiplayer = transform.position;

      UpdateReplicantToolEffectTargetPos();
    }
  }

  void SetGroundEffectsEmission(bool enabled)
  {
    foreach (var effect in groundEffects)
    {
      var particles = effect.effectObject.GetComponentInChildren<ParticleSystem>(true);
      if (particles != null)
      {
        var em = particles.emission;
        em.enabled = enabled;
      }
    }
  }

  void OnInPlayModeChanged()
  {
    if (this == null
      || animator == null
      || avatarMaterialControl == null
      || avatarAudioController == null)
    {
      return;
    }

    if (inPlayMode)
    {
      avatarMaterialControl.SetToStandardMaterial();
      animator.ResetTrigger("TransformToEditor");
      animator.SetTrigger("TransformToExplorer");
      inPlayMode = true;
      avatarAudioController.OnTransformToExplorer();
      SetGroundEffectsEmission(true && animator.GetBool("IsGrounded"));
      DestroyToolEffectReplicant();
    }
    else
    {
      avatarMaterialControl.SetToHologramMaterial();
      animator.ResetTrigger("TransformToExplorer");
      animator.SetTrigger("TransformToEditor");
      inPlayMode = false;
      avatarAudioController.OnTransformToEditor();
      SetGroundEffectsEmission(false);
    }
  }

  public void SetHologramMenu(bool on)
  {
    // Silence what is probably an on-exit only crash.
    // DO NOT USE ?. !!!
    if (animator == null) return;
    animator.SetBool("EditorHolograms", on);
  }

  public void SetCrouch(bool on)
  {
    if (on != animator.GetBool("IsCrouching"))
    {
      animator.SetBool("IsCrouching", on);
    }
  }

  public void SetGrounded(bool on)
  {
    if (on != animator.GetBool("IsGrounded"))
    {
      animator.SetBool("IsGrounded", on);
      avatarAudioController.UpdateGrounded(on);

      SetGroundEffectsEmission(on && inPlayMode);
    }
  }

  public void SetSprint(bool on)
  {
    if (on != animator.GetBool("IsSprinting"))
    {
      animator.SetBool("IsSprinting", on);
    }
  }

  public void SetVariableTransparency(float value)
  {
    if (variableTransparencyRenderersVisible == (value != 0)) return;

    variableTransparencyRenderersVisible = value != 0;

    for (int i = 0; i < variableTransparencyRenderers.Length; i++)
    {
      variableTransparencyRenderers[i].enabled = variableTransparencyRenderersVisible && playAvatarRendererEnabled;
    }
  }

  public void SetPlayerVisible(bool on)
  {
    if (playAvatarRendererEnabled == on) return;

    playAvatarRendererEnabled = on;

    for (int i = 0; i < playAvatarRenderers.Length; i++)
    {
      //only set it if its not in the variable transparency thing - those are done in the second for loop
      if (!variableTransparencyRenderers.Contains(playAvatarRenderers[i]))
        playAvatarRenderers[i].enabled = on;
    }

    for (int i = 0; i < variableTransparencyRenderers.Length; i++)
    {
      variableTransparencyRenderers[i].enabled = on && variableTransparencyRenderersVisible;
    }
  }

  public void OnDamaged()
  {
    animator.SetTrigger("IsDamaged");
    avatarAudioController.OnDamage();
  }

  public void OnDied()
  {
    animator.SetTrigger("IsDead");
    avatarAudioController.OnDeath();

  }

  public void OnJumpDenied()
  {
    //animator.SetTrigger("IsJumping");
  }

  public void OnJumped()
  {
    avatarAudioController.OnJump();
    //animator.SetTrigger("IsJumping");
  }

  public void OnLanded()
  {
    //animator.SetBool("IsGrounded", true);
  }

  public void OnRespawned()
  {
    animator.SetTrigger("IsRespawning");
    avatarAudioController.OnRespawn();
  }

  public void SetTint(Color newTint)
  {
    if (currentTint != newTint)
    {
      currentTint = newTint;
      OnTintChanged();
    }
  }

  public void OnTintChanged()
  {
    foreach (MeshRenderer _renderer in renderersForTint)
    {
      _renderer.material.SetColor("_MainTint", currentTint);
    }
    avatarMaterialControl.SetTint(currentTint);

    toolEffectReplicant?.SetTint(currentTint);
    createPreviewInst?.SetTint(currentTint);
  }

  void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
#if USE_PUN
    if (stream.isWriting)
    {
      stream.SendNext(inPlayMode);
      stream.SendColor(currentTint);

      bool toolActive = locallyActiveTool != null;
      stream.SendNext(toolActive);
      if (toolActive)
      {
        stream.SendNext(locallyActiveTool.GetToolEffectName());
        stream.SendNext(locallyActiveTool.GetToolEffectActive());
        stream.SendNext(locallyActiveTool.GetToolEffectTargetViewId());
        stream.SendNext(locallyActiveTool.GetToolEffectTargetPosition());
        stream.SendNext(locallyActiveTool.GetCreatePreviewSettingsJson());
      }
      stream.SendNext(playAvatarRendererEnabled);
    }
    else
    {
      if (!setupCalled)
      {
        Setup(false);
      }

      SetInPlayMode((bool)stream.ReceiveNext());
      SetTint(stream.ReceiveColor());
      if ((bool)stream.ReceiveNext())
      {
        // Tool is active.
        SetToolEffectName((string)stream.ReceiveNext());
        bool isActive = (bool)stream.ReceiveNext(); // IMPORTANT keep a local var, since the ?. may not call receive
        toolEffectReplicant?.SetActive(isActive);
        receivedToolTargetViewId = (int)stream.ReceiveNext();
        lastReceivedTargetPos = (Vector3)stream.ReceiveNext(); // IMPORTANT keep a local var, since the ?. may not call receive
        SetCreatePreviewSettingsJson((string)stream.ReceiveNext());
      }
      else
      {
        DestroyToolEffectReplicant();
        SetCreatePreviewSettingsJson(null);
        lerpedTargetPos = Vector3.positiveInfinity;
        lastReceivedTargetPos = Vector3.positiveInfinity;
      }
      // "Render enabled" field not present in earlier versions, defaults to true:
      SetPlayerVisible((bool)stream.ReceiveNext());
    }
#endif
  }

  void DestroyToolEffectReplicant()
  {
    if (toolEffectReplicant != null)
    {
      GameObject.Destroy(toolEffectReplicant.GetGameObject());
      toolEffectReplicant = null;
    }
  }

  private void SetToolEffectName(string name)
  {
    Debug.Assert(name != null);

    if (currentToolEffectName != name)
    {
      // Need to change the instance.
      DestroyToolEffectReplicant();
      currentToolEffectName = name;

      Util.Log($"new tool effect name: {name}");

      foreach (var prefab in toolEffectPrefabs)
      {
        if (currentToolEffectName.Contains(prefab.name))
        {
          GameObject inst = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
          toolEffectReplicant = inst.GetComponent<NetworkableToolEffect>();

          // YOLO. Assume it's an on-actor effect.
          if (toolEffectReplicant == null)
          {
            toolEffectReplicant = new OnActorToolEffect(inst);
          }

          toolEffectReplicant.SetSpatialAudio(true);
          toolEffectReplicant.SetRayOriginTransform(toolEmissionAnchor);
          toolEffectReplicant.SetTint(currentTint);
          break;
        }
      }

      // Note that it is OK if at this point, currentToolEffectInstance is null. That just means we don't support networking that effect.
    }
  }

  void LateUpdate()
  {
#if USE_PUN
    if (!isLocalPlayer && toolEffectReplicant != null)
    {
      NetworkableToolEffect effect = toolEffectReplicant;
      if (receivedToolTargetViewId >= 0)
      {
        // Find the target actor, shoot a ray at it, and have the tool effect end there.
        PhotonView view = PhotonView.Find(receivedToolTargetViewId);
        VoosActor targetActor = view?.GetComponent<VoosActor>();
        effect.SetReceivedTargetActor(targetActor);
      }
      else
      {
        effect.SetReceivedTargetActor(null);
      }
      effect.OnLateUpdate();
    }
#endif
  }

  public void Setup(bool isLocalPlayer)
  {
    Debug.Assert(!setupCalled);
    setupCalled = true;
    this.isLocalPlayer = isLocalPlayer;
    if (!this.isLocalPlayer)
    {
      avatarAudioController.SetSpatial(true);
      MoveToDefaultLayer(transform);
      Util.SetLayerRecursively(playAvatar.gameObject, LayerMask.NameToLayer("VoosActor"));
    }
    if (inPlayMode)
    {
      avatarAudioController.LaunchAsExplorer();
    }
    else
    {
      avatarAudioController.LaunchAsEditor();
    }
  }

  public bool GetIsLocalPlayer()
  {
    return this.isLocalPlayer;
  }

  void PlayerBody.EventHandler.OnTintChanged(Color tint)
  {
    SetTint(tint);
  }

  void MoveToDefaultLayer(Transform parentTransform)
  {
    parentTransform.gameObject.layer = LayerMask.NameToLayer("Default");
    foreach (Transform t in parentTransform)
    {
      if (t != parentTransform) MoveToDefaultLayer(t);
    }
  }

  void SetCreatePreviewSettingsJson(string createPreviewSettingsJson)
  {
    if (lastCreatePreviewSettingsJson == createPreviewSettingsJson) return;

    lastCreatePreviewSettingsJson = createPreviewSettingsJson;
    if (createPreviewSettingsJson.IsNullOrEmpty())
    {
      if (createPreviewInst != null)
      {
        GameObject.Destroy(createPreviewInst.gameObject);
        createPreviewInst = null;
      }
    }
    else
    {
      // Settings changed, create new preview
      var settings = JsonUtility.FromJson<CreatePreviewSettings>(createPreviewSettingsJson);

      if (createPreviewInst != null)
      {
        GameObject.Destroy(createPreviewInst.gameObject);
      }

      createPreviewInst = Instantiate(createPreviewPrefab);
      createPreviewInst.SetRenderableByReference(
        settings.renderable,
        settings.addlRotation,
        settings.renderableOffset,
        settings.renderableRotation,
        _ => settings.scale,
        () => true);

      createPreviewInst.SetTint(currentTint);
    }
  }

  void OnDestroy()
  {
    if (createPreviewInst != null)
    {
      GameObject.Destroy(createPreviewInst.gameObject);
    }

    if (toolEffectReplicant != null)
    {
      GameObject.Destroy(toolEffectReplicant.GetGameObject());
    }
  }
}
