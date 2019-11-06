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

public class ResetPositionFeedback : MonoBehaviour
{
  [SerializeField] Transform currentPositionTransform;
  [SerializeField] Transform resetPositionTransform;
  [SerializeField] LineRenderer connectingLine;
  [SerializeField] Material previewMaterial;

  Vector3 currentPosition = Vector3.zero;
  Vector3 resetPosition = Vector3.forward;

  float CIRCLE_RADIUS = .5f;
  VoosActor targetActor;

  GameObject previewObject;

  public void SetActor(VoosActor _actor)
  {
    targetActor = _actor;

    if (targetActor != null)
    {
      UpdateFeedback(true);
    }

    gameObject.SetActive(targetActor != null);
  }

  void Update()
  {
    UpdateFeedback();
  }

  void UpdateFeedback(bool updateAll = false)
  {
    if (targetActor == null)
    {
      SetActor(null);
      return;
    }

    bool updateCurrentPosition = false;
    bool updateResetPosition = false;

    if (currentPosition != targetActor.transform.position)
    {
      updateCurrentPosition = true;
    }

    if (resetPosition != targetActor.GetSpawnPosition())
    {
      updateResetPosition = true;
    }

    if (!updateAll && !updateResetPosition && !updateCurrentPosition)
    {
      return;
    }

    Bounds bounds = targetActor.ComputeWorldRenderBounds();
    Vector3 bottomCenter = bounds.center - Vector3.up * bounds.extents.y;
    Vector3 bottomDelta = bottomCenter - targetActor.transform.position;

    if (updateCurrentPosition || updateAll)
    {
      currentPosition = targetActor.transform.position;
      Vector3 currentPositionWithBoundsOffset = currentPosition + bottomDelta;
      if (currentPositionWithBoundsOffset.y < .01f)
      {
        currentPositionWithBoundsOffset.y = .01f;
      }
      currentPositionTransform.position = currentPositionWithBoundsOffset;
    }

    if (updateResetPosition || updateAll)
    {
      resetPosition = targetActor.GetSpawnPosition();
      Vector3 resetPositionWithBoundsOffset = currentPosition + bottomDelta;
      if (resetPositionWithBoundsOffset.y < .01f)
      {
        resetPositionWithBoundsOffset.y = .01f;
      }
      resetPositionTransform.position = resetPositionWithBoundsOffset;
    }

    Vector3 circleDelta = (currentPositionTransform.position - resetPositionTransform.position).normalized * CIRCLE_RADIUS;
    connectingLine.SetPosition(0, currentPositionTransform.position - circleDelta);
    connectingLine.SetPosition(1, resetPositionTransform.position + circleDelta);

  }
}