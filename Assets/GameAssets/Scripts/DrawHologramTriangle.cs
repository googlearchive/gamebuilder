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

public class DrawHologramTriangle : MonoBehaviour
{
    public Transform ToolOrigin;
    public Transform Line_EndPoint1;
    public Transform Line_EndPoint2;
    public Material HologramMaterial;

    Mesh mesh;

    // Use this for initialization
    void Start()
    {
        Vector3 TrianglePoint1 = ToolOrigin.position;
        Vector3 TrianglePoint2 = Line_EndPoint1.position;
        Vector3 TrianglePoint3 = Line_EndPoint2.position;

        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        mesh = GetComponent<MeshFilter>().mesh;

        mesh.Clear();

        // make changes to the Mesh by creating arrays which contain the new values
        mesh.vertices = new Vector3[] { TrianglePoint1, TrianglePoint2, TrianglePoint3 };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new int[] { 0, 1, 2 };

        if (HologramMaterial != null)
            this.GetComponent<Renderer>().material = HologramMaterial;
    }

    void LateUpdate(){
        mesh.vertices = new Vector3[] { ToolOrigin.position,  Line_EndPoint1.position, Line_EndPoint2.position };
        mesh.RecalculateBounds();
    }
}