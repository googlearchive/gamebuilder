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

public class DrawHologramPyramid : MonoBehaviour
{
  public Transform origin;
  public Transform[] corners;
  [SerializeField] LineRenderer[] lines;

  [SerializeField] GameObject pyramidMeshObject;
  [SerializeField] Material hologramMaterial;

  Mesh mesh;
  MeshRenderer renderer;
  Vector3 GetOrigin()
  {
    return transform.InverseTransformPoint(origin.position);
  }

  Vector3 GetCorner(int index)
  {
    return transform.InverseTransformPoint(corners[index].position);
  }

  void Awake()
  {
    mesh = pyramidMeshObject.AddComponent<MeshFilter>().mesh;
    renderer = pyramidMeshObject.AddComponent<MeshRenderer>();
    renderer.material = hologramMaterial;

    mesh.Clear();

    mesh.vertices = new Vector3[] {
      GetOrigin(), GetCorner(0), GetCorner(1), GetCorner(2), GetCorner(3),
    };
    mesh.uv = new Vector2[] {
      new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(1, 1)
      };
    mesh.triangles = new int[] {
      0, 1, 2,
      0, 2, 3,
      0, 3, 4,
      0, 4, 1
      };
    mesh.RecalculateBounds();
  }

  void LateUpdate()
  {
    mesh.vertices = new Vector3[] {
      GetOrigin(), GetCorner(0), GetCorner(1), GetCorner(2), GetCorner(3),
    };
    for (int i = 0; i < 4; i++)
    {
      //bit of a hack to not call getcorner more
      lines[i].SetPositions(new Vector3[] { mesh.vertices[0], mesh.vertices[i + 1] });
    }
    mesh.RecalculateBounds();
  }

  public void SetTint(Color color)
  {
    foreach (LineRenderer line in lines)
    {
      line.material.SetColor("_MainTint", color);
    }

    renderer.material.SetColor("_MainTint", color);
  }
}
