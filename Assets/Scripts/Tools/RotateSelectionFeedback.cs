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

public class RotateSelectionFeedback : SelectionFeedback
{
  [SerializeField] Renderer yawRenderer;
  [SerializeField] Renderer pitchRenderer;
  [SerializeField] Renderer rollRenderer;
  [SerializeField] Color yawColor;
  [SerializeField] Color pitchColor;
  [SerializeField] Color rollColor;

  Material yawMaterial;
  Material pitchMaterial;
  Material rollMaterial;

  const float gizmoScale = .2f;

  public void Setup()
  {
    yawMaterial = yawRenderer.material;
    pitchMaterial = pitchRenderer.material;
    rollMaterial = rollRenderer.material;


    yawMaterial.SetColor("_MainTint", yawColor);
    rollMaterial.SetColor("_MainTint", rollColor);
    pitchMaterial.SetColor("_MainTint", pitchColor);
  }

  public override void SetActor(VoosActor _actor)
  {
    base.SetActor(_actor);
    if (_actor == null) return;

    /*     Vector3 boundsSize = currentActor.ComputeWorldRenderBounds().size;
        float scale = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.y) * AXIS_SCALE;
        transform.localScale = Vector3.one * scale; */
    // transform.localScale = Vector3.one * 2.5f;

  }

  public override void UpdatePosition()
  {
    if (currentActor != null && gameObject.activeSelf)
    {
      transform.position = currentActor.GetPosition();
      // transform.position = currentActor.ComputeWorldRenderBounds().center;
      transform.rotation = currentActor.GetRotation();//currentActor.ComputeWorldRenderBounds().center;
    }
    else SetVisiblity(false);
  }

  bool IsYaw(RotateTool.RotationAxis rotationAxis)
  {
    return rotationAxis == RotateTool.RotationAxis.Yaw;
  }

  bool IsRoll(RotateTool.RotationAxis rotationAxis)
  {
    return rotationAxis == RotateTool.RotationAxis.Roll;
  }

  bool IsPitch(RotateTool.RotationAxis rotationAxis)
  {
    return rotationAxis == RotateTool.RotationAxis.Pitch;
  }

  public void SetPosition(Vector3 position)
  {
    transform.position = position;
  }


  public void SetRotation(Quaternion rotation)
  {
    transform.rotation = rotation;
  }

  public override void SetSelected(bool on)
  {
    // yawMaterial.SetColor("_MainTint", IsYaw(rotationAxis) && on ? selectColor : yawColor);
    // rollMaterial.SetColor("_MainTint", IsRoll(rotationAxis) && on ? selectColor : rollColor);
    // pitchMaterial.SetColor("_MainTint", IsPitch(rotationAxis) && on ? selectColor : pitchColor);
  }

  RotateTool.RotationAxis rotationAxis;

  internal void UpdateAxis(RotateTool.RotationAxis rotationAxis)
  {
    this.rotationAxis = rotationAxis;
    // yawRenderer.enabled = IsYaw(rotationAxis);
    // rollRenderer.enabled = IsRoll(rotationAxis);
    // pitchRenderer.enabled = IsPitch(rotationAxis);

    yawMaterial.SetColor("_MainTint", IsYaw(rotationAxis) ? selectColor : yawColor);
    rollMaterial.SetColor("_MainTint", IsRoll(rotationAxis) ? selectColor : rollColor);
    pitchMaterial.SetColor("_MainTint", IsPitch(rotationAxis) ? selectColor : pitchColor);
  }


  internal void UpdateScale(Vector3 viewPosition, float fov)
  {
    //   Debug.Log(fov);
    float dist = Vector3.Distance(transform.position, viewPosition);
    transform.localScale = Vector3.one * dist * Mathf.Sin(Mathf.Deg2Rad * fov / 2f) * gizmoScale;
  }
}
