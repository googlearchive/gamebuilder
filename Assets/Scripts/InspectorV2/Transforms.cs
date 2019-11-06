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

public class Transforms
{
  public struct TransformUndoState
  {
    readonly Vector3 position;
    readonly Quaternion rotation;
    readonly string parentName;

    public TransformUndoState(Vector3 position, Quaternion rotation, string parentName)
    {
      this.position = position;
      this.rotation = rotation;
      this.parentName = parentName;
    }

    public void PushTo(VoosActor actor)
    {
      actor.SetPosition(position);
      actor.SetRotation(rotation);
      VoosActor parent = actor.GetEngine().GetActor(parentName);
      actor.SetTransformParent(IsValidParent(actor, parent) ? parentName : null);
      actor.SetSpawnPositionRotationOfEntireFamily();
      // NOTE: No need to call ApplyPropertiesToClones because that function
      // doesn't copy this anyway
    }

    bool IsValidParent(VoosActor actor, VoosActor parent)
    {
      // Check for self or cycles
      while (parent != null)
      {
        if (parent == actor)
        {
          Util.Log($"Cycle detected!");
          return false;
        }
        parent = actor.GetEngine().GetActor(parent.GetTransformParent());
      }
      return true;
    }
  }

  public struct SpawnTransformUndoState
  {
    readonly Vector3 position;
    readonly Quaternion rotation;
    readonly string parentName;

    public SpawnTransformUndoState(Vector3 position, Quaternion rotation, string parentName)
    {
      this.position = position;
      this.rotation = rotation;
      this.parentName = parentName;
    }

    public void PushTo(VoosActor actor)
    {
      actor.SetSpawnPosition(position);
      actor.SetSpawnRotation(rotation);
      VoosActor parent = actor.GetEngine().GetActor(parentName);
      actor.SetSpawnTransformParent(IsValidSpawnParent(actor, parent) ? parentName : null);
      // NOTE: No need to call ApplyPropertiesToClones because that function
      // doesn't copy this anyway
    }

    bool IsValidSpawnParent(VoosActor actor, VoosActor parent)
    {
      // Check for self or cycles
      while (parent != null)
      {
        if (parent == actor)
        {
          Util.Log($"Cycle detected!");
          return false;
        }
        parent = actor.GetEngine().GetActor(parent.GetSpawnTransformParent());
      }
      return true;
    }
  }

  public struct OffsetsUndoState
  {
    readonly Vector3 offset;
    readonly float yaw;

    public OffsetsUndoState(Vector3 offset, float yaw)
    {
      this.offset = offset;
      this.yaw = yaw;
    }

    public void PushTo(VoosActor actor)
    {
      Vector3 rotEuler = actor.GetRenderableRotation().eulerAngles;
      rotEuler.y = yaw;
      actor.SetRenderableRotation(Quaternion.Euler(rotEuler.x, rotEuler.y, rotEuler.z));
      actor.SetRenderableOffset(offset);
      // HACK FOR CLONES
      actor.ApplyPropertiesToClones();
    }
  }
}
