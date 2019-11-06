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
using UnityEngine.Rendering;

public class GrassSpawn : MonoBehaviour
{
  [SerializeField] GameObject[] grassObjects;

  GrassBend bender;

  public int numberToSpawn;
  public float TerrainSizeNegativeRange = -50F;
  public float TerrainSizePositiveRange = 50F;

  GameBuilder.RenderUtil.TransformBatchesManager[] batcherByPrefab;

  // Mainly so we can modify these without affecting the assets.
  Material[] materialInstances;

  bool visible = true;

  float timeOfLastUnackedDistChange = -1f;
  float lastDistanceChangedTo = 0f;

  void Awake()
  {
    Util.FindIfNotSet(this, ref bender);

    materialInstances = new Material[grassObjects.Length];

    for (int i = 0; i < grassObjects.Length; i++)
    {
      var origMaterial = grassObjects[i].GetComponentInChildren<MeshRenderer>().sharedMaterial;
      materialInstances[i] = Material.Instantiate(origMaterial);
    }
  }

  void OnEnable()
  {
    bender.AddMaterials(materialInstances);
  }

  void OnDisable()
  {
    bender.RemoveMaterials(materialInstances);
  }

  public void SetIsVisible(bool on)
  {
    visible = on;
  }

  public void UpdateGrassDistance(float dist)
  {
    timeOfLastUnackedDistChange = Time.realtimeSinceStartup;
    lastDistanceChangedTo = dist;
  }

  void MaybeDoDistanceChange()
  {
    if (timeOfLastUnackedDistChange < 0f)
    {
      return;
    }

    if (timeOfLastUnackedDistChange + 0.1f < Time.realtimeSinceStartup)
    {
      // Do it.
      timeOfLastUnackedDistChange = -1f;
      ResetGrass(lastDistanceChangedTo);
    }
  }

  void DrawAll()
  {
    if (batcherByPrefab == null)
    {
      return;
    }
    for (int i = 0; i < grassObjects.Length; i++)
    {
      var mesh = grassObjects[i].GetComponentInChildren<MeshFilter>().sharedMesh;
      var mat = materialInstances[i];
      var layer = grassObjects[i].layer;
      bool receiveShadows = true;
      batcherByPrefab[i].ForEachBatch(batch =>
      {
        Graphics.DrawMeshInstanced(mesh, 0, mat, batch.transforms, batch.numInstances, null, ShadowCastingMode.Off, receiveShadows, layer, null, LightProbeUsage.Off, null);
      });
    }
  }

  void Update()
  {
    if (visible)
    {
      MaybeDoDistanceChange();
      DrawAll();
    }
  }

  void ResetGrass(float cullDistance)
  {
    if (grassObjects.Length < 1) return;
    int numPerPrefab = numberToSpawn / grassObjects.Length;
    if (batcherByPrefab == null)
    {
      batcherByPrefab = new GameBuilder.RenderUtil.TransformBatchesManager[grassObjects.Length];
    }

    for (int i = 0; i < grassObjects.Length; i++)
    {
      if (batcherByPrefab[i] == null)
      {
        batcherByPrefab[i] = new GameBuilder.RenderUtil.TransformBatchesManager();
      }
      var batcher = batcherByPrefab[i];
      batcher.Clear();

      for (int j = 0; j < numPerPrefab; j++)
      {
        Vector3 position = new Vector3(
          Random.Range(TerrainSizeNegativeRange, TerrainSizePositiveRange),
          0,
          Random.Range(TerrainSizeNegativeRange, TerrainSizePositiveRange));

        if (Mathf.Abs(position.x) < cullDistance && Mathf.Abs(position.z) < cullDistance)
        {
          batcher.Add(Matrix4x4.Translate(position));
        }
      }
    }
  }
}