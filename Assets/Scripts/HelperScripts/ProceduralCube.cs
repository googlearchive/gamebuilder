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

public class ProceduralCube : MonoBehaviour
{

  void Awake()
  {
    ConstructMesh();
    // GetComponent<Renderer>().material.color = Color.yellow;
  }

  void ConstructMesh()
  {
    Mesh mesh;
    GetComponent<MeshFilter>().mesh = mesh = new Mesh();
    mesh.name = "Procedural cube";

    Vector3[] verts = new Vector3[] {
      new Vector3(-1,1,1),
      new Vector3(1,1,1),
      new Vector3(1,-1,1),
      new Vector3(-1,-1,1),
            new Vector3(-1,1,-1),
      new Vector3(1,1,-1),
      new Vector3(1,-1,-1),
      new Vector3(-1,-1,-1)
     };
    int[] lines = new int[] {
      0,1,1,2,2,3,3,0,
      4,5,5,6,6,7,7,4,
      0,4,1,5,2,6,3,7
     };

    mesh.vertices = verts;
    mesh.RecalculateBounds();
    mesh.SetIndices(lines, MeshTopology.Lines, 0);
  }
}
