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

public class CreateToolRay : MonoBehaviour, UserBody.NetworkableToolEffect
{
  [SerializeField] GameObject rayObject;
  [SerializeField] DrawHologramPyramid hologramPyramid;

  Transform networkedRayOrigin;
  Vector3 networkedRayTargetPos = Vector3.zero;

  public void TurnOn()
  {
    rayObject.SetActive(true);
  }

  public void TurnOff()
  {
    rayObject.SetActive(false);
  }

  public void SetTint(Color color)
  {
    hologramPyramid.SetTint(ArtUtil.GetHologramColor(color));
  }

  void SetRayEndPoint(Vector3 p)
  {
    hologramPyramid.corners[0].position = p - Vector3.right;
    hologramPyramid.corners[1].position = p + Vector3.up;
    hologramPyramid.corners[2].position = p + Vector3.left;
    hologramPyramid.corners[3].position = p + Vector3.down;
  }

  public void UpdateRayWithObject(GameObject go)
  {
    if (go == null)
    {
      TurnOff();
      return;
    }

    TurnOn();

    if (go.GetComponentsInChildren<Renderer>().Length == 0)
    {
      SetRayEndPoint(go.transform.position);
      return;
    }

    Bounds bounds = Util.ComputeWorldRenderBounds(go);
    SetRayEndPoint(bounds.center);
  }

  void UserBody.NetworkableToolEffect.SetActive(bool active)
  {
    if (active) TurnOn();
    else TurnOff();
  }

  void UserBody.NetworkableToolEffect.SetReceivedTargetActor(VoosActor actor) { }
  void UserBody.NetworkableToolEffect.SetTargetPosition(Vector3 position)
  {
    networkedRayTargetPos = position;
  }

  void UserBody.NetworkableToolEffect.OnLateUpdate()
  {
    if (networkedRayOrigin != null && networkedRayTargetPos.magnitude > 0f)
    {
      transform.position = networkedRayOrigin.position;
      SetRayEndPoint(networkedRayTargetPos);
    }
  }

  void UserBody.NetworkableToolEffect.SetSpatialAudio(bool enabled) { }

  void UserBody.NetworkableToolEffect.SetRayOriginTransform(Transform origin)
  {
    networkedRayOrigin = origin;
  }

  Transform localRayOrigin = null;
  public void SetLocalRayOriginTransform(Transform transform)
  {
    localRayOrigin = transform;
  }

  void LateUpdate()
  {
    if (localRayOrigin != null) transform.position = localRayOrigin.position;
  }

  GameObject UserBody.NetworkableToolEffect.GetGameObject()
  {
    // IMPORTANT: Do NOT use ?. - we need to use (this == null) explicitly because Unity's null stuff is BAD.
    if (this == null) return null;
    return this.gameObject;
  }
}
