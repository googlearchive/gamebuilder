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

public class PositionSelectionFeedback : SelectionFeedback
{
  [SerializeField] Renderer xRenderer;
  [SerializeField] Renderer yRenderer;
  [SerializeField] Renderer zRenderer;
  [SerializeField] Color xColor;
  [SerializeField] Color yColor;
  [SerializeField] Color zColor;
  const float gizmoScale = .3f;

  Material xMaterial;
  Material yMaterial;
  Material zMaterial;

  [SerializeField] MoveTool moveTool;


  public void Setup()
  {
    xMaterial = xRenderer.material;
    yMaterial = yRenderer.material;
    zMaterial = zRenderer.material;
  }

  public override void SetActor(VoosActor _actor)
  {
    base.SetActor(_actor);
    if (_actor == null) return;

    /* Vector3 boundsSize = currentActor.ComputeWorldRenderBounds().size;
    float scale = Mathf.Max(boundsSize.x, boundsSize.y, boundsSize.y) * AXIS_SCALE;
    transform.localScale = Vector3.one * scale; */
    //transform.localScale = Vector3.one * 4f;
  }

  public override void UpdatePosition()
  {
    if (currentActor != null && gameObject.activeSelf)
    {
      transform.position = currentActor.ComputeWorldRenderBounds().center;
      transform.rotation = moveTool.coordinateSystem == MoveTool.CoordinateSystem.Global ?
        Quaternion.identity : currentActor.GetRotation();

    }
    else SetVisiblity(false);
  }

  internal void UpdateAxis(MoveTool.GizmoAxis gizmoAxis, bool allAxis)
  {
    if (gameObject.activeSelf == false) return;

    if (gizmoAxis == MoveTool.GizmoAxis.None)
    {
      xMaterial.SetColor("_MainTint", xColor);
      zMaterial.SetColor("_MainTint", zColor);
      yMaterial.SetColor("_MainTint", yColor);
      return;
    }
    else if (gizmoAxis == MoveTool.GizmoAxis.Center)
    {
      xMaterial.SetColor("_MainTint", selectColor);
      zMaterial.SetColor("_MainTint", selectColor);
      yMaterial.SetColor("_MainTint", allAxis ? selectColor : yColor);
      return;
    }
    else
    {
      xMaterial.SetColor("_MainTint", gizmoAxis == MoveTool.GizmoAxis.X ? selectColor : xColor);
      zMaterial.SetColor("_MainTint", gizmoAxis == MoveTool.GizmoAxis.Z ? selectColor : zColor);
      yMaterial.SetColor("_MainTint", gizmoAxis == MoveTool.GizmoAxis.Y ? selectColor : yColor);
    }
  }

  internal void UpdateScale(Vector3 viewPosition, float fov)
  {
    //   Debug.Log(fov);
    float dist = Vector3.Distance(transform.position, viewPosition);
    transform.localScale = Vector3.one * dist * Mathf.Sin(Mathf.Deg2Rad * fov / 2f) * gizmoScale;
  }
}
