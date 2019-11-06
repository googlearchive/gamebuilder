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

using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class AssetCacheTest
{

  [Test]
  public void TestFitPolyImport()
  {
    GameObject importedObject = new GameObject("importedObject");

    GameObject meshChild = CreateBoxMeshObject(Vector3.one, Vector3.zero);
    meshChild.transform.SetParent(importedObject.transform);

    AssetCache.DownloadVisitor.FitImportedModel(importedObject, "");
    Assert.AreEqual(Vector3.one, meshChild.transform.localScale);
    Assert.AreEqual(new Vector3(0, 0.5f, 0), meshChild.transform.localPosition);
  }

  [Test]
  public void TestFitPolyImportUncenteredMesh()
  {
    GameObject importedObject = new GameObject("importedObject");

    GameObject meshChild = CreateBoxMeshObject(Vector3.one, new Vector3(9, 3, 5));
    meshChild.transform.SetParent(importedObject.transform);

    AssetCache.DownloadVisitor.FitImportedModel(importedObject, "");
    Assert.AreEqual(Vector3.one, meshChild.transform.localScale);
    Assert.AreEqual(new Vector3(-9, -2.5f, -5), meshChild.transform.localPosition);
  }

  [Test]
  public void TestFitPolyImportScaledMesh()
  {
    GameObject importedObject = new GameObject("importedObject");

    GameObject meshChild = CreateBoxMeshObject(new Vector3(4, 2, 8), Vector3.zero);
    meshChild.transform.SetParent(importedObject.transform);

    AssetCache.DownloadVisitor.FitImportedModel(importedObject, "");
    Assert.AreEqual(new Vector3(0.125f, 0.125f, 0.125f), meshChild.transform.localScale);
    Assert.AreEqual(new Vector3(0, 0.125f, 0), meshChild.transform.localPosition);
  }

  [Test]
  public void TestFitPolyImporMultipleMeshes()
  {
    GameObject importedObject = new GameObject("importedObject");

    GameObject meshChild1 = CreateBoxMeshObject(new Vector3(2, 2, 2), new Vector3(2, 0, 0));
    meshChild1.transform.SetParent(importedObject.transform);
    GameObject meshChild2 = CreateBoxMeshObject(new Vector3(2, 2, 2), new Vector3(0, 2, 0));
    meshChild2.transform.SetParent(importedObject.transform);

    AssetCache.DownloadVisitor.FitImportedModel(importedObject, "");
    Assert.AreEqual(new Vector3(0.25f, 0.25f, 0.25f), meshChild1.transform.localScale);
    Assert.AreEqual(new Vector3(-0.25f, 0.25f, 0), meshChild1.transform.localPosition);
    Assert.AreEqual(new Vector3(0.25f, 0.25f, 0.25f), meshChild2.transform.localScale);
    Assert.AreEqual(new Vector3(-0.25f, 0.25f, 0), meshChild2.transform.localPosition);
  }

  private GameObject CreateBoxMeshObject(Vector3 meshScale, Vector3 meshOffset)
  {
    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    Mesh mesh = cube.GetComponent<MeshFilter>().mesh;
    Vector3[] vertices = mesh.vertices;
    int i = 0;
    while (i < vertices.Length)
    {
      vertices[i] = Vector3.Scale(vertices[i], meshScale);
      vertices[i] += meshOffset;
      i++;
    }
    mesh.vertices = vertices;
    mesh.RecalculateBounds();
    return cube;
  }
}