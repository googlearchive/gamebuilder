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

// This is meant to be used with a relaible, delta-compressed view. This is for
// rarely changing fields, such as the renderable URI.
public class ActorNetworking : MonoBehaviour, IPunObservable
{
  VoosActor actorCache;
  VoosActor actor
  {
    get { return Util.GetAndCache(this, ref actorCache); }
  }

  PhotonView photonViewCache;
  PhotonView photonView
  {
    get { return Util.GetAndCache(this, ref photonViewCache); }
  }

  public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
  {
#if USE_PUN
    Debug.Assert(actor != null, $"No {typeof(VoosActor).Name} component on {gameObject.name}");

    // All fields here should be the only things needed to turn an actor into itself working off of Resources/Actor.prefab!

    // NOTE: when changing this format in a backwards-incompatible way, 
    // also bump NetworkingController.NewPlayerInitPayload.CurrentVersion.

    if (stream.isWriting)
    {
      Debug.Assert(actor.IsLocallyOwned());

      stream.SendNext(actor.GetName());
      stream.SendNext(actor.GetBrainName());

      // IMPORTANT: URIs can be local paths, and we *never* want to send those over the network for privacy. So obscure it.
      string renderableUri = actor.GetRenderableUri();
      if (!renderableUri.IsNullOrEmpty() && VoosAssetUtil.IsLocalAsset(renderableUri))
      {
        renderableUri = VoosActor.NOT_AVAILABLE_URI;
      }
      stream.SendNext(renderableUri);

      // TODO we really should pack all booleans into bit fields..

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_RELIABLE_STREAM_WRITE
      stream.SendNext(Util.EmptyIfNull(actor.GetDisplayName()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetDescription()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetTransformParent()));    // GENERATED
      stream.SendNext(actor.GetLocalScale());    // GENERATED
      stream.SendNext(actor.GetRenderableOffset());    // GENERATED
      stream.SendNext(actor.GetRenderableRotation());    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetCommentText()));    // GENERATED
      stream.SendNext(actor.GetSpawnPosition());    // GENERATED
      stream.SendNext(actor.GetSpawnRotation());    // GENERATED
      stream.SendNext(actor.GetPreferOffstage());    // GENERATED
      stream.SendNext(actor.GetIsSolid());    // GENERATED
      stream.SendNext(actor.GetEnablePhysics());    // GENERATED
      stream.SendNext(actor.GetEnableGravity());    // GENERATED
      stream.SendNext(actor.GetBounciness());    // GENERATED
      stream.SendNext(actor.GetDrag());    // GENERATED
      stream.SendNext(actor.GetAngularDrag());    // GENERATED
      stream.SendNext(actor.GetMass());    // GENERATED
      stream.SendNext(actor.GetFreezeRotations());    // GENERATED
      stream.SendNext(actor.GetFreezeX());    // GENERATED
      stream.SendNext(actor.GetFreezeY());    // GENERATED
      stream.SendNext(actor.GetFreezeZ());    // GENERATED
      stream.SendNext(actor.GetEnableAiming());    // GENERATED
      stream.SendNext(actor.GetHideInPlayMode());    // GENERATED
      stream.SendNext(actor.GetKeepUpright());    // GENERATED
      stream.SendNext(actor.GetIsPlayerControllable());    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetDebugString()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetCloneParent()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetJoinedTags()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetSpawnTransformParent()));    // GENERATED
      stream.SendNext(actor.GetWasClonedByScript());    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetLoopingAnimation()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetControllingVirtualPlayerId()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetLightSettingsJson()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetPfxId()));    // GENERATED
      stream.SendNext(Util.EmptyIfNull(actor.GetSfxId()));    // GENERATED
      stream.SendNext(actor.GetUseConcaveCollider());    // GENERATED
      stream.SendNext(actor.GetSpeculativeColDet());    // GENERATED
      stream.SendNext(actor.GetUseStickyDesiredVelocity());    // GENERATED
      stream.SendNext(actor.GetStickyDesiredVelocity());    // GENERATED
      stream.SendNext(actor.GetStickyForce());    // GENERATED
                                                            // END_GAME_BUILDER_CODE_GEN

      // If we are parented, send over the local position just once.
      if (actor.GetParentActor() != null)
      {
        stream.SendNext(actor.transform.localPosition);
        stream.SendNext(actor.transform.localRotation);
        // Also send tint, which is part of GlobalUnreliableData channel
        stream.SendNext(actor.GetTint());
      }

      stream.SendNext(actor.IsLockWantedLocally());
    }
    else
    {
      actor.SetName((string)stream.ReceiveNext());
      actor.SetBrainName((string)stream.ReceiveNext());
      actor.SetRenderableUri((string)stream.ReceiveNext());

      // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_RELIABLE_STREAM_READ
      actor.SetDisplayName((string)stream.ReceiveNext());    // GENERATED
      actor.SetDescription((string)stream.ReceiveNext());    // GENERATED
      actor.SetTransformParent((string)stream.ReceiveNext());    // GENERATED
      actor.SetLocalScale((Vector3)stream.ReceiveNext());    // GENERATED
      actor.SetRenderableOffset((Vector3)stream.ReceiveNext());    // GENERATED
      actor.SetRenderableRotation((Quaternion)stream.ReceiveNext());    // GENERATED
      actor.SetCommentText((string)stream.ReceiveNext());    // GENERATED
      actor.SetSpawnPosition((Vector3)stream.ReceiveNext());    // GENERATED
      actor.SetSpawnRotation((Quaternion)stream.ReceiveNext());    // GENERATED
      actor.SetPreferOffstage((bool)stream.ReceiveNext());    // GENERATED
      actor.SetIsSolid((bool)stream.ReceiveNext());    // GENERATED
      actor.SetEnablePhysics((bool)stream.ReceiveNext());    // GENERATED
      actor.SetEnableGravity((bool)stream.ReceiveNext());    // GENERATED
      actor.SetBounciness((float)stream.ReceiveNext());    // GENERATED
      actor.SetDrag((float)stream.ReceiveNext());    // GENERATED
      actor.SetAngularDrag((float)stream.ReceiveNext());    // GENERATED
      actor.SetMass((float)stream.ReceiveNext());    // GENERATED
      actor.SetFreezeRotations((bool)stream.ReceiveNext());    // GENERATED
      actor.SetFreezeX((bool)stream.ReceiveNext());    // GENERATED
      actor.SetFreezeY((bool)stream.ReceiveNext());    // GENERATED
      actor.SetFreezeZ((bool)stream.ReceiveNext());    // GENERATED
      actor.SetEnableAiming((bool)stream.ReceiveNext());    // GENERATED
      actor.SetHideInPlayMode((bool)stream.ReceiveNext());    // GENERATED
      actor.SetKeepUpright((bool)stream.ReceiveNext());    // GENERATED
      actor.SetIsPlayerControllable((bool)stream.ReceiveNext());    // GENERATED
      actor.SetDebugString((string)stream.ReceiveNext());    // GENERATED
      actor.SetCloneParent((string)stream.ReceiveNext());    // GENERATED
      actor.SetJoinedTags((string)stream.ReceiveNext());    // GENERATED
      actor.SetSpawnTransformParent((string)stream.ReceiveNext());    // GENERATED
      actor.SetWasClonedByScript((bool)stream.ReceiveNext());    // GENERATED
      actor.SetLoopingAnimation((string)stream.ReceiveNext());    // GENERATED
      actor.SetControllingVirtualPlayerId((string)stream.ReceiveNext());    // GENERATED
      actor.SetLightSettingsJson((string)stream.ReceiveNext());    // GENERATED
      actor.SetPfxId((string)stream.ReceiveNext());    // GENERATED
      actor.SetSfxId((string)stream.ReceiveNext());    // GENERATED
      actor.SetUseConcaveCollider((bool)stream.ReceiveNext());    // GENERATED
      actor.SetSpeculativeColDet((bool)stream.ReceiveNext());    // GENERATED
      actor.SetUseStickyDesiredVelocity((bool)stream.ReceiveNext());    // GENERATED
      actor.SetStickyDesiredVelocity((Vector3)stream.ReceiveNext());    // GENERATED
      actor.SetStickyForce((Vector3)stream.ReceiveNext());    // GENERATED
      // END_GAME_BUILDER_CODE_GEN

      if (actor.GetParentActor() != null)
      {
        actor.SetLocalPosition((Vector3)stream.ReceiveNext());
        actor.SetLocalRotation((Quaternion)stream.ReceiveNext());
        actor.SetTint((Color)stream.ReceiveNext());
      }

      actor.SetLockingOwnership_ONLY_FOR_ACTOR_NETWORKING((bool)stream.ReceiveNext());
    }
#endif
  }

  public bool IsRemoteReplicant()
  {
    return photonView != null && !photonView.isMine;
  }

  public bool IsLocal()
  {
    return !IsRemoteReplicant();
  }

  // Ownership on collision logic. We only request ownership when the local
  // player's body hits another actor. Before, the logic was when *any* local
  // actor (ie. actor owned by us) hit another actor, we infected ownership. But
  // this resulted in excessive RPCs (ownership requests), like in the "ball of
  // pugs" example.
  private void OnCollisionEnter(Collision collision)
  {
    bool isLocalPlayerBody = photonView != null && photonView.isMine && this.actor.GetPlayerBody() != null;

    if (!isLocalPlayerBody)
    {
      return;
    }

    // Of course, only worry about other actors.
    VoosActor otherActor = collision.gameObject.GetComponent<VoosActor>();
    if (otherActor == null)
    {
      return;
    }

    // Record
    otherActor.realTimeOfLastCollisionWithLocalPlayer = Time.realtimeSinceStartup;

    // Also, never take over other player bodies.
    PlayerBody otherPlayerBody = otherActor.GetPlayerBody();
    if (otherPlayerBody != null)
    {
      return;
    }

    // Try to request ownership, since it's better for the player to own objects
    // it's colliding with, like a soccer ball. But if it's locked by another,
    // then don't bother.
    if (!otherActor.IsLocallyOwned())
    {
      otherActor.RequestOwnership(VoosEngine.OwnRequestReason.Collision);
    }
  }
}
