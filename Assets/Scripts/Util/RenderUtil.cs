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

using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

namespace GameBuilder
{
  public static class RenderUtil
  {
    // https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html
    public const int MaxInstancesPerDrawCall = 1023;

    public class TransformBatch
    {
      public int numInstances;
      public Matrix4x4[] transforms;

      public static TransformBatch Create()
      {
        return new TransformBatch
        {
          numInstances = 0,
          transforms = new Matrix4x4[MaxInstancesPerDrawCall]
        };
      }

      public void Reset()
      {
        numInstances = 0;
      }

      public bool IsFull()
      {
        return numInstances == transforms.Length;
      }

      public void Add(Matrix4x4 m)
      {
        if (IsFull())
        {
          throw new System.Exception("TransformBatch is full");
        }

        transforms[numInstances++] = m;
      }
    }

    // A garbage-mindful class to manage and reuse transform batches.
    public class TransformBatchesManager
    {
      List<TransformBatch> transformBatches = new List<TransformBatch>();
      int currentBatchIndex = 0;

      TransformBatch GetOrAddBatch(int batchId)
      {
        while (batchId >= transformBatches.Count)
        {
          transformBatches.Add(TransformBatch.Create());
        }
        return transformBatches[batchId];
      }

      public void Clear()
      {
        // Reset all batches
        foreach (var batch in transformBatches)
        {
          batch.Reset();
        }
        currentBatchIndex = 0;
      }

      public int GetNumBatchesUsed()
      {
        return currentBatchIndex + 1;
      }

      public void Add(Matrix4x4 m)
      {
        TransformBatch batch = GetOrAddBatch(currentBatchIndex);
        if (batch.IsFull())
        {
          currentBatchIndex++;
          batch = GetOrAddBatch(currentBatchIndex);
        }

        batch.Add(m);
      }

      public void ForEachBatch(System.Action<TransformBatch> func)
      {
        foreach (var batch in transformBatches)
        {
          if (batch.numInstances == 0)
          {
            // Assume all subsequent batches are also empty. They should be filled in order.
            break;
          }
          func(batch);
        }
      }
    }
  }
}
