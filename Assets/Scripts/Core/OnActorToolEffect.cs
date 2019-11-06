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

// Effectively, the logic/edit tool ring effects
class OnActorToolEffect : UserBody.NetworkableToolEffect
{
  GameObject effectInstance;
  VoosActor targetActor;

  public OnActorToolEffect(GameObject instance)
  {
    this.effectInstance = instance;
  }

  public GameObject GetGameObject()
  {
    // IMPORTANT: Do NOT use ?. - we need to use (this == null) explicitly because Unity's null stuff is BAD.
    if (this == null) return null;
    return this.effectInstance;
  }

  public void OnLateUpdate()
  {
    if (targetActor == null)
    {
      return;
    }
    // Copy/pasted from BehaviorTool for now.
    effectInstance.transform.position = targetActor.ComputeWorldRenderBounds().center;
    float scale = Mathf.Sqrt(Mathf.Pow(targetActor.ComputeWorldRenderBounds().size.x, 2) + Mathf.Pow(targetActor.ComputeWorldRenderBounds().size.z, 2));//* 1.5f;

    effectInstance.transform.localScale = Vector3.one * (scale + .5f);// currentFocusedActor.GetWorldRenderBounds().size;

    // Also make sure we match the layer, ie for offstage.
    Util.SetLayerRecursively(effectInstance, targetActor.gameObject.layer);
  }

  public void SetActive(bool active)
  {
    effectInstance.SetActive(active);
  }

  public void SetRayOriginTransform(Transform origin) { }

  public void SetSpatialAudio(bool enabled) { }

  public void SetReceivedTargetActor(VoosActor actor)
  {
    this.targetActor = actor;
  }

  public void SetTint(Color tint)
  {
    var setter = effectInstance.GetComponent<ToolRingFXColor>();
    setter?.SetTint(tint);
  }

  public void SetTargetPosition(Vector3 position) { }
}