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

public class ScaleSelectionFeedback : SelectionFeedback
{
  const float gizmoScale = .15f;

  public override void UpdatePosition()
  {
    if (currentActor != null && gameObject.activeSelf)
    {
      transform.position = currentActor.ComputeWorldRenderBounds().center;
    }
    else SetVisiblity(false);
  }

  internal void UpdateScale(Vector3 viewPosition, float fov)
  {
    float dist = Vector3.Distance(transform.position, viewPosition);
    transform.localScale = Vector3.one * dist * Mathf.Sin(Mathf.Deg2Rad * fov / 2f) * gizmoScale;
  }
}
