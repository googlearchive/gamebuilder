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

// MOSTLY GENERATED CODE! BE CAREFUL WHILE EDITING!
public partial class VoosActor
{
  // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_COMPONENT_CSHARP
  private string displayName;    // GENERATED

  public void SetDisplayName(string newDisplayName)    // GENERATED
  {
    if (displayName == newDisplayName)    // GENERATED
    {
      return;    // GENERATED
    }

    displayName = newDisplayName;    // GENERATED
    UpdateGameObjectName();    // GENERATED
  }

  public string GetDisplayName()    // GENERATED
  {
    return displayName;    // GENERATED
  }

  private string description;    // GENERATED

  public void SetDescription(string newDescription)    // GENERATED
  {
    description = newDescription;    // GENERATED
  }

  public string GetDescription()    // GENERATED
  {
    return description;    // GENERATED
  }

  private string transformParent;    // GENERATED

  public void SetTransformParent(string newTransformParent)    // GENERATED
  {
    if (transformParent == newTransformParent)    // GENERATED
    {
      return;    // GENERATED
    }

    var oldTransformParent = transformParent;    // GENERATED
    transformParent = newTransformParent;    // GENERATED
    UpdateTransformParent(oldTransformParent);    // GENERATED
    OnEffectivelyOffstageChanged();    // GENERATED
  }

  public string GetTransformParent()    // GENERATED
  {
    return transformParent;    // GENERATED
  }

  private Vector3 renderableOffset = Vector3.zero;    // GENERATED

  public void SetRenderableOffset(Vector3 newRenderableOffset)    // GENERATED
  {
    if (Vector3.Distance(renderableOffset, newRenderableOffset) < 1e-4f)    // GENERATED
    {
      return;    // GENERATED
    }

    renderableOffset = newRenderableOffset;    // GENERATED
    UpdateRenderableOffset();    // GENERATED
  }

  public Vector3 GetRenderableOffset()    // GENERATED
  {
    return renderableOffset;    // GENERATED
  }

  private Quaternion renderableRotation = Quaternion.identity;    // GENERATED

  public void SetRenderableRotation(Quaternion newRenderableRotation)    // GENERATED
  {
    newRenderableRotation = newRenderableRotation.normalized;    // GENERATED
    if (renderableRotation.ApproxEquals(newRenderableRotation))    // GENERATED
    {
      return;    // GENERATED
    }

    renderableRotation = newRenderableRotation;    // GENERATED
    UpdateRenderableRotation();    // GENERATED
  }

  public Quaternion GetRenderableRotation()    // GENERATED
  {
    return renderableRotation;    // GENERATED
  }

  private string commentText;    // GENERATED

  public void SetCommentText(string newCommentText)    // GENERATED
  {
    if (commentText == newCommentText)    // GENERATED
    {
      return;    // GENERATED
    }

    commentText = newCommentText;    // GENERATED
    UpdateCommentText();    // GENERATED
  }

  public string GetCommentText()    // GENERATED
  {
    return commentText;    // GENERATED
  }

  private Vector3 spawnPosition = Vector3.zero;    // GENERATED

  public void SetSpawnPosition(Vector3 newSpawnPosition)    // GENERATED
  {
    spawnPosition = newSpawnPosition;    // GENERATED
  }

  public Vector3 GetSpawnPosition()    // GENERATED
  {
    return spawnPosition;    // GENERATED
  }

  private Quaternion spawnRotation = Quaternion.identity;    // GENERATED

  public void SetSpawnRotation(Quaternion newSpawnRotation)    // GENERATED
  {
    newSpawnRotation = newSpawnRotation.normalized;    // GENERATED
    spawnRotation = newSpawnRotation;    // GENERATED
  }

  public Quaternion GetSpawnRotation()    // GENERATED
  {
    return spawnRotation;    // GENERATED
  }

  private bool preferOffstage = false;    // GENERATED

  public void SetPreferOffstage(bool newPreferOffstage)    // GENERATED
  {
    if (preferOffstage == newPreferOffstage)    // GENERATED
    {
      return;    // GENERATED
    }

    preferOffstage = newPreferOffstage;    // GENERATED
    OnEffectivelyOffstageChanged();    // GENERATED
  }

  public bool GetPreferOffstage()    // GENERATED
  {
    return preferOffstage;    // GENERATED
  }

  private bool isSolid = true;    // GENERATED

  public void SetIsSolid(bool newIsSolid)    // GENERATED
  {
    if (isSolid == newIsSolid)    // GENERATED
    {
      return;    // GENERATED
    }

    isSolid = newIsSolid;    // GENERATED
    UpdateTriggerGhost();    // GENERATED
    UpdateColliders();    // GENERATED
  }

  public bool GetIsSolid()    // GENERATED
  {
    return isSolid;    // GENERATED
  }

  private bool enablePhysics;    // GENERATED

  public void SetEnablePhysics(bool newEnablePhysics)    // GENERATED
  {
    if (enablePhysics == newEnablePhysics)    // GENERATED
    {
      return;    // GENERATED
    }

    enablePhysics = newEnablePhysics;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
    UpdateTriggerGhost();    // GENERATED
    UpdateColliders();    // GENERATED
  }

  public bool GetEnablePhysics()    // GENERATED
  {
    return enablePhysics;    // GENERATED
  }

  private bool enableGravity;    // GENERATED

  public void SetEnableGravity(bool newEnableGravity)    // GENERATED
  {
    if (enableGravity == newEnableGravity)    // GENERATED
    {
      return;    // GENERATED
    }

    enableGravity = newEnableGravity;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetEnableGravity()    // GENERATED
  {
    return enableGravity;    // GENERATED
  }

  private float bounciness;    // GENERATED

  public void SetBounciness(float newBounciness)    // GENERATED
  {
    if (Mathf.Abs(bounciness - newBounciness) < 1e-4f)    // GENERATED
    {
      return;    // GENERATED
    }

    bounciness = newBounciness;    // GENERATED
    UpdatePhysicsMaterial();    // GENERATED
  }

  public float GetBounciness()    // GENERATED
  {
    return bounciness;    // GENERATED
  }

  private float drag;    // GENERATED

  public void SetDrag(float newDrag)    // GENERATED
  {
    if (Mathf.Abs(drag - newDrag) < 1e-4f)    // GENERATED
    {
      return;    // GENERATED
    }

    drag = newDrag;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public float GetDrag()    // GENERATED
  {
    return drag;    // GENERATED
  }

  private float angularDrag;    // GENERATED

  public void SetAngularDrag(float newAngularDrag)    // GENERATED
  {
    if (Mathf.Abs(angularDrag - newAngularDrag) < 1e-4f)    // GENERATED
    {
      return;    // GENERATED
    }

    angularDrag = newAngularDrag;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public float GetAngularDrag()    // GENERATED
  {
    return angularDrag;    // GENERATED
  }

  private float mass;    // GENERATED

  public void SetMass(float newMass)    // GENERATED
  {
    if (Mathf.Abs(mass - newMass) < 1e-4f)    // GENERATED
    {
      return;    // GENERATED
    }

    mass = newMass;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public float GetMass()    // GENERATED
  {
    return mass;    // GENERATED
  }

  private bool freezeRotations;    // GENERATED

  public void SetFreezeRotations(bool newFreezeRotations)    // GENERATED
  {
    if (freezeRotations == newFreezeRotations)    // GENERATED
    {
      return;    // GENERATED
    }

    freezeRotations = newFreezeRotations;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetFreezeRotations()    // GENERATED
  {
    return freezeRotations;    // GENERATED
  }

  private bool freezeX;    // GENERATED

  public void SetFreezeX(bool newFreezeX)    // GENERATED
  {
    if (freezeX == newFreezeX)    // GENERATED
    {
      return;    // GENERATED
    }

    freezeX = newFreezeX;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetFreezeX()    // GENERATED
  {
    return freezeX;    // GENERATED
  }

  private bool freezeY;    // GENERATED

  public void SetFreezeY(bool newFreezeY)    // GENERATED
  {
    if (freezeY == newFreezeY)    // GENERATED
    {
      return;    // GENERATED
    }

    freezeY = newFreezeY;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetFreezeY()    // GENERATED
  {
    return freezeY;    // GENERATED
  }

  private bool freezeZ;    // GENERATED

  public void SetFreezeZ(bool newFreezeZ)    // GENERATED
  {
    if (freezeZ == newFreezeZ)    // GENERATED
    {
      return;    // GENERATED
    }

    freezeZ = newFreezeZ;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetFreezeZ()    // GENERATED
  {
    return freezeZ;    // GENERATED
  }

  private bool enableAiming;    // GENERATED

  public void SetEnableAiming(bool newEnableAiming)    // GENERATED
  {
    enableAiming = newEnableAiming;    // GENERATED
  }

  public bool GetEnableAiming()    // GENERATED
  {
    return enableAiming;    // GENERATED
  }

  private bool hideInPlayMode;    // GENERATED

  public void SetHideInPlayMode(bool newHideInPlayMode)    // GENERATED
  {
    if (hideInPlayMode == newHideInPlayMode)    // GENERATED
    {
      return;    // GENERATED
    }

    hideInPlayMode = newHideInPlayMode;    // GENERATED
    UpdateRenderableHiddenState();    // GENERATED
  }

  public bool GetHideInPlayMode()    // GENERATED
  {
    return hideInPlayMode;    // GENERATED
  }

  private bool keepUpright;    // GENERATED

  public void SetKeepUpright(bool newKeepUpright)    // GENERATED
  {
    if (keepUpright == newKeepUpright)    // GENERATED
    {
      return;    // GENERATED
    }

    keepUpright = newKeepUpright;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
    MaybeCorrectRotation();    // GENERATED
  }

  public bool GetKeepUpright()    // GENERATED
  {
    return keepUpright;    // GENERATED
  }

  private bool useDesiredVelocity;    // GENERATED

  public void SetUseDesiredVelocity(bool newUseDesiredVelocity)    // GENERATED
  {
    useDesiredVelocity = newUseDesiredVelocity;    // GENERATED
  }

  public bool GetUseDesiredVelocity()    // GENERATED
  {
    return useDesiredVelocity;    // GENERATED
  }

  private bool ignoreVerticalDesiredVelocity;    // GENERATED

  public void SetIgnoreVerticalDesiredVelocity(bool newIgnoreVerticalDesiredVelocity)    // GENERATED
  {
    ignoreVerticalDesiredVelocity = newIgnoreVerticalDesiredVelocity;    // GENERATED
  }

  public bool GetIgnoreVerticalDesiredVelocity()    // GENERATED
  {
    return ignoreVerticalDesiredVelocity;    // GENERATED
  }

  private Vector3 desiredVelocity = Vector3.zero;    // GENERATED

  public void SetDesiredVelocity(Vector3 newDesiredVelocity)    // GENERATED
  {
    desiredVelocity = newDesiredVelocity;    // GENERATED
  }

  public Vector3 GetDesiredVelocity()    // GENERATED
  {
    return desiredVelocity;    // GENERATED
  }

  private bool isPlayerControllable;    // GENERATED

  public void SetIsPlayerControllable(bool newIsPlayerControllable)    // GENERATED
  {
    if (isPlayerControllable == newIsPlayerControllable)    // GENERATED
    {
      return;    // GENERATED
    }

    isPlayerControllable = newIsPlayerControllable;    // GENERATED
    UpdateIsPlayerControllable();    // GENERATED
    UpdateCollisionStayTracker();    // GENERATED
  }

  public bool GetIsPlayerControllable()    // GENERATED
  {
    return isPlayerControllable;    // GENERATED
  }

  private string debugString;    // GENERATED

  public void SetDebugString(string newDebugString)    // GENERATED
  {
    debugString = newDebugString;    // GENERATED
  }

  public string GetDebugString()    // GENERATED
  {
    return debugString;    // GENERATED
  }

  private string cloneParent;    // GENERATED

  public void SetCloneParent(string newCloneParent)    // GENERATED
  {
    cloneParent = newCloneParent;    // GENERATED
  }

  public string GetCloneParent()    // GENERATED
  {
    return cloneParent;    // GENERATED
  }

  private string cameraActor;    // GENERATED

  public void SetCameraActor(string newCameraActor)    // GENERATED
  {
    cameraActor = newCameraActor;    // GENERATED
  }

  public string GetCameraActor()    // GENERATED
  {
    return cameraActor;    // GENERATED
  }

  private string spawnTransformParent;    // GENERATED

  public void SetSpawnTransformParent(string newSpawnTransformParent)    // GENERATED
  {
    spawnTransformParent = newSpawnTransformParent;    // GENERATED
  }

  public string GetSpawnTransformParent()    // GENERATED
  {
    return spawnTransformParent;    // GENERATED
  }

  private bool wasClonedByScript = false;    // GENERATED

  public void SetWasClonedByScript(bool newWasClonedByScript)    // GENERATED
  {
    wasClonedByScript = newWasClonedByScript;    // GENERATED
  }

  public bool GetWasClonedByScript()    // GENERATED
  {
    return wasClonedByScript;    // GENERATED
  }

  private string loopingAnimation;    // GENERATED

  public void SetLoopingAnimation(string newLoopingAnimation)    // GENERATED
  {
    if (loopingAnimation == newLoopingAnimation)    // GENERATED
    {
      return;    // GENERATED
    }

    loopingAnimation = newLoopingAnimation;    // GENERATED
    UpdateAnimation();    // GENERATED
  }

  public string GetLoopingAnimation()    // GENERATED
  {
    return loopingAnimation;    // GENERATED
  }

  private string controllingVirtualPlayerId;    // GENERATED

  public void SetControllingVirtualPlayerId(string newControllingVirtualPlayerId)    // GENERATED
  {
    if (controllingVirtualPlayerId == newControllingVirtualPlayerId)    // GENERATED
    {
      return;    // GENERATED
    }

    controllingVirtualPlayerId = newControllingVirtualPlayerId;    // GENERATED
    UpdateControllingVirtualPlayerId();    // GENERATED
  }

  public string GetControllingVirtualPlayerId()    // GENERATED
  {
    return controllingVirtualPlayerId;    // GENERATED
  }

  private string cameraSettingsJson;    // GENERATED

  public void SetCameraSettingsJson(string newCameraSettingsJson)    // GENERATED
  {
    if (cameraSettingsJson == newCameraSettingsJson)    // GENERATED
    {
      return;    // GENERATED
    }

    cameraSettingsJson = newCameraSettingsJson;    // GENERATED
    UpdateCameraSettingsJson();    // GENERATED
  }

  public string GetCameraSettingsJson()    // GENERATED
  {
    return cameraSettingsJson;    // GENERATED
  }

  private string lightSettingsJson;    // GENERATED

  public void SetLightSettingsJson(string newLightSettingsJson)    // GENERATED
  {
    if (lightSettingsJson == newLightSettingsJson)    // GENERATED
    {
      return;    // GENERATED
    }

    lightSettingsJson = newLightSettingsJson;    // GENERATED
    UpdateLightSettingsJson();    // GENERATED
  }

  public string GetLightSettingsJson()    // GENERATED
  {
    return lightSettingsJson;    // GENERATED
  }

  private string pfxId;    // GENERATED

  public void SetPfxId(string newPfxId)    // GENERATED
  {
    if (pfxId == newPfxId)    // GENERATED
    {
      return;    // GENERATED
    }

    pfxId = newPfxId;    // GENERATED
    UpdatePfxId();    // GENERATED
  }

  public string GetPfxId()    // GENERATED
  {
    return pfxId;    // GENERATED
  }

  private string sfxId;    // GENERATED

  public void SetSfxId(string newSfxId)    // GENERATED
  {
    if (sfxId == newSfxId)    // GENERATED
    {
      return;    // GENERATED
    }

    sfxId = newSfxId;    // GENERATED
    UpdateSfxId();    // GENERATED
  }

  public string GetSfxId()    // GENERATED
  {
    return sfxId;    // GENERATED
  }

  private bool useConcaveCollider;    // GENERATED

  public void SetUseConcaveCollider(bool newUseConcaveCollider)    // GENERATED
  {
    if (useConcaveCollider == newUseConcaveCollider)    // GENERATED
    {
      return;    // GENERATED
    }

    useConcaveCollider = newUseConcaveCollider;    // GENERATED
    UpdateColliders();    // GENERATED
  }

  public bool GetUseConcaveCollider()    // GENERATED
  {
    return useConcaveCollider;    // GENERATED
  }

  private bool speculativeColDet;    // GENERATED

  public void SetSpeculativeColDet(bool newSpeculativeColDet)    // GENERATED
  {
    if (speculativeColDet == newSpeculativeColDet)    // GENERATED
    {
      return;    // GENERATED
    }

    speculativeColDet = newSpeculativeColDet;    // GENERATED
    UpdateRigidbodyComponent();    // GENERATED
  }

  public bool GetSpeculativeColDet()    // GENERATED
  {
    return speculativeColDet;    // GENERATED
  }

  private bool useStickyDesiredVelocity;    // GENERATED

  public void SetUseStickyDesiredVelocity(bool newUseStickyDesiredVelocity)    // GENERATED
  {
    useStickyDesiredVelocity = newUseStickyDesiredVelocity;    // GENERATED
  }

  public bool GetUseStickyDesiredVelocity()    // GENERATED
  {
    return useStickyDesiredVelocity;    // GENERATED
  }

  private Vector3 stickyDesiredVelocity = Vector3.zero;    // GENERATED

  public void SetStickyDesiredVelocity(Vector3 newStickyDesiredVelocity)    // GENERATED
  {
    stickyDesiredVelocity = newStickyDesiredVelocity;    // GENERATED
  }

  public Vector3 GetStickyDesiredVelocity()    // GENERATED
  {
    return stickyDesiredVelocity;    // GENERATED
  }

  private Vector3 stickyForce = Vector3.zero;    // GENERATED

  public void SetStickyForce(Vector3 newStickyForce)    // GENERATED
  {
    stickyForce = newStickyForce;    // GENERATED
  }

  public Vector3 GetStickyForce()    // GENERATED
  {
    return stickyForce;    // GENERATED
  }

  // END_GAME_BUILDER_CODE_GEN

  public Vector3 GetVector3Field(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_VECTOR3_FIELD_SWITCH
      case 0: return this.GetPosition();    // GENERATED
      case 1: return this.GetLocalPosition();    // GENERATED
      case 2: return this.GetLocalScale();    // GENERATED
      case 3: return this.GetRenderableOffset();    // GENERATED
      case 4: return this.GetSpawnPosition();    // GENERATED
      case 5: return this.GetDesiredVelocity();    // GENERATED
      case 6: return this.GetWorldRenderBoundsSize();    // GENERATED
      case 7: return this.GetWorldRenderBoundsCenter();    // GENERATED
      case 8: return this.GetVelocity();    // GENERATED
      case 9: return this.GetAngularVelocity();    // GENERATED
      case 10: return this.GetStickyDesiredVelocity();    // GENERATED
      case 11: return this.GetStickyForce();    // GENERATED
                                                // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad Vector3 field ID: {fieldId}. Returning zero.");
        return Vector3.zero;
    }
  }

  public void SetVector3Field(ushort fieldId, Vector3 newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_VECTOR3_FIELD_SWITCH
      case 0: this.SetPosition(newValue); return;    // GENERATED
      case 1: this.SetLocalPosition(newValue); return;    // GENERATED
      case 2: this.SetLocalScale(newValue); return;    // GENERATED
      case 3: this.SetRenderableOffset(newValue); return;    // GENERATED
      case 4: this.SetSpawnPosition(newValue); return;    // GENERATED
      case 5: this.SetDesiredVelocity(newValue); return;    // GENERATED
      case 6: this.SetVelocity(newValue); return;    // GENERATED
      case 7: this.SetAngularVelocity(newValue); return;    // GENERATED
      case 8: this.SetStickyDesiredVelocity(newValue); return;    // GENERATED
      case 9: this.SetStickyForce(newValue); return;    // GENERATED
                                                        // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad Vector3 field ID: {fieldId}. Ignoring set.");
        return;
    }
  }

  public bool GetBooleanField(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_BOOLEAN_FIELD_SWITCH
      case 0: return this.GetPreferOffstage();    // GENERATED
      case 1: return this.GetIsSolid();    // GENERATED
      case 2: return this.GetEnablePhysics();    // GENERATED
      case 3: return this.GetEnableGravity();    // GENERATED
      case 4: return this.GetFreezeRotations();    // GENERATED
      case 5: return this.GetFreezeX();    // GENERATED
      case 6: return this.GetFreezeY();    // GENERATED
      case 7: return this.GetFreezeZ();    // GENERATED
      case 8: return this.GetEnableAiming();    // GENERATED
      case 9: return this.GetHideInPlayMode();    // GENERATED
      case 10: return this.GetKeepUpright();    // GENERATED
      case 11: return this.GetUseDesiredVelocity();    // GENERATED
      case 12: return this.GetIgnoreVerticalDesiredVelocity();    // GENERATED
      case 13: return this.GetIsPlayerControllable();    // GENERATED
      case 14: return this.GetWasClonedByScript();    // GENERATED
      case 15: return this.GetUseConcaveCollider();    // GENERATED
      case 16: return this.GetIsGrounded();    // GENERATED
      case 17: return this.GetSpeculativeColDet();    // GENERATED
      case 18: return this.GetUseStickyDesiredVelocity();    // GENERATED
                                                             // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad boolean field ID: {fieldId}. Returning false.");
        return false;
    }
  }

  public void SetBooleanField(ushort fieldId, bool newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_BOOLEAN_FIELD_SWITCH
      case 0: this.SetPreferOffstage(newValue); return;    // GENERATED
      case 1: this.SetIsSolid(newValue); return;    // GENERATED
      case 2: this.SetEnablePhysics(newValue); return;    // GENERATED
      case 3: this.SetEnableGravity(newValue); return;    // GENERATED
      case 4: this.SetFreezeRotations(newValue); return;    // GENERATED
      case 5: this.SetFreezeX(newValue); return;    // GENERATED
      case 6: this.SetFreezeY(newValue); return;    // GENERATED
      case 7: this.SetFreezeZ(newValue); return;    // GENERATED
      case 8: this.SetEnableAiming(newValue); return;    // GENERATED
      case 9: this.SetHideInPlayMode(newValue); return;    // GENERATED
      case 10: this.SetKeepUpright(newValue); return;    // GENERATED
      case 11: this.SetUseDesiredVelocity(newValue); return;    // GENERATED
      case 12: this.SetIgnoreVerticalDesiredVelocity(newValue); return;    // GENERATED
      case 13: this.SetIsPlayerControllable(newValue); return;    // GENERATED
      case 14: this.SetWasClonedByScript(newValue); return;    // GENERATED
      case 15: this.SetUseConcaveCollider(newValue); return;    // GENERATED
      case 16: this.SetSpeculativeColDet(newValue); return;    // GENERATED
      case 17: this.SetUseStickyDesiredVelocity(newValue); return;    // GENERATED
                                                                      // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad boolean field ID: {fieldId}. Ignoring set.");
        return;
    }
  }

  public float GetFloatField(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_FLOAT_FIELD_SWITCH
      case 0: return this.GetBounciness();    // GENERATED
      case 1: return this.GetDrag();    // GENERATED
      case 2: return this.GetAngularDrag();    // GENERATED
      case 3: return this.GetMass();    // GENERATED
      // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad float field ID: {fieldId}. Returning 0.");
        return 0f;
    }
  }

  public void SetFloatField(ushort fieldId, float newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_FLOAT_FIELD_SWITCH
      case 0: this.SetBounciness(newValue); return;    // GENERATED
      case 1: this.SetDrag(newValue); return;    // GENERATED
      case 2: this.SetAngularDrag(newValue); return;    // GENERATED
      case 3: this.SetMass(newValue); return;    // GENERATED
      // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad float field ID: {fieldId}. Ignoring set.");
        return;
    }
  }
  public string GetStringField(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_STRING_FIELD_SWITCH
      case 0: return this.GetDisplayName();    // GENERATED
      case 1: return this.GetDescription();    // GENERATED
      case 2: return this.GetTransformParent();    // GENERATED
      case 3: return this.GetCommentText();    // GENERATED
      case 4: return this.GetDebugString();    // GENERATED
      case 5: return this.GetCloneParent();    // GENERATED
      case 6: return this.GetJoinedTags();    // GENERATED
      case 7: return this.GetCameraActor();    // GENERATED
      case 8: return this.GetSpawnTransformParent();    // GENERATED
      case 9: return this.GetLoopingAnimation();    // GENERATED
      case 10: return this.GetControllingVirtualPlayerId();    // GENERATED
      case 11: return this.GetCameraSettingsJson();    // GENERATED
      case 12: return this.GetLightSettingsJson();    // GENERATED
      case 13: return this.GetPfxId();    // GENERATED
      case 14: return this.GetSfxId();    // GENERATED
                                          // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad string field ID: {fieldId}. Returning null.");
        return null;
    }
  }

  public void SetStringField(ushort fieldId, string newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_STRING_FIELD_SWITCH
      case 0: this.SetDisplayName(newValue); return;    // GENERATED
      case 1: this.SetDescription(newValue); return;    // GENERATED
      case 2: this.SetTransformParent(newValue); return;    // GENERATED
      case 3: this.SetCommentText(newValue); return;    // GENERATED
      case 4: this.SetDebugString(newValue); return;    // GENERATED
      case 5: this.SetJoinedTags(newValue); return;    // GENERATED
      case 6: this.SetCameraActor(newValue); return;    // GENERATED
      case 7: this.SetSpawnTransformParent(newValue); return;    // GENERATED
      case 8: this.SetLoopingAnimation(newValue); return;    // GENERATED
      case 9: this.SetControllingVirtualPlayerId(newValue); return;    // GENERATED
      case 10: this.SetCameraSettingsJson(newValue); return;    // GENERATED
      case 11: this.SetLightSettingsJson(newValue); return;    // GENERATED
      case 12: this.SetPfxId(newValue); return;    // GENERATED
      case 13: this.SetSfxId(newValue); return;    // GENERATED
                                                   // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad string field ID: {fieldId}. Ignoring set.");
        return;
    }
  }

  public Quaternion GetQuaternionField(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_QUATERNION_FIELD_SWITCH
      case 0: return this.GetRotation();    // GENERATED
      case 1: return this.GetLocalRotation();    // GENERATED
      case 2: return this.GetRenderableRotation();    // GENERATED
      case 3: return this.GetSpawnRotation();    // GENERATED
                                                 // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad quaternion field ID: {fieldId}. Returning identity.");
        return Quaternion.identity;
    }
  }

  public void SetQuaternionField(ushort fieldId, Quaternion newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_QUATERNION_FIELD_SWITCH
      case 0: this.SetRotation(newValue); return;    // GENERATED
      case 1: this.SetLocalRotation(newValue); return;    // GENERATED
      case 2: this.SetRenderableRotation(newValue); return;    // GENERATED
      case 3: this.SetSpawnRotation(newValue); return;    // GENERATED
                                                          // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad quaternion field ID: {fieldId}. Ignoring set.");
        return;
    }
  }

  public Color GetColorField(ushort fieldId)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_GET_COLOR_FIELD_SWITCH
      case 0: return this.GetTint();    // GENERATED
                                        // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad color field ID: {fieldId}. Returning white.");
        return Color.white;
    }
  }

  public void SetColorField(ushort fieldId, Color newValue)
  {
    switch (fieldId)
    {
      // BEGIN_GAME_BUILDER_CODE_GEN CS_ACTOR_SET_COLOR_FIELD_SWITCH
      case 0: this.SetTint(newValue); return;    // GENERATED
                                                 // END_GAME_BUILDER_CODE_GEN
      default:
        Util.LogError($"Bad color field ID: {fieldId}. Ignoring set.");
        return;
    }
  }
}
