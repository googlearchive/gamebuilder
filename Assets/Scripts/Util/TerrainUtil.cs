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
using UnityEngine;

namespace GameBuilder
{
  // Some utilities for terrain.
  public static class TerrainUtil
  {
    public const int BlockEdgeLength = 10;
    public const int InstancesPerBlock = BlockEdgeLength * BlockEdgeLength * BlockEdgeLength;

    const int L = BlockEdgeLength;
    const float Lf = (float)BlockEdgeLength;

    public class RenderingBlock
    {
      short nextFreeSlot = 0;
      public Matrix4x4[] transforms = new Matrix4x4[InstancesPerBlock];
      // If it's -1, the cell is unoccupied. Otherwise, it's an index into the 'transforms' array.
      short[] cellToSlot = new short[InstancesPerBlock];
      short[] slotToCell = new short[InstancesPerBlock];

      public RenderingBlock()
      {
        for (int i = 0; i < InstancesPerBlock; i++)
        {
          cellToSlot[i] = -1;
          slotToCell[i] = -1;
        }
      }

      public void Set(Int3 u, Matrix4x4 transform)
      {
        short cell = (short)(u.x * L * L + u.y * L + u.z);
        if (cellToSlot[cell] != -1)
        {
          // Replacing.
          // Use the same transform slot.
          short slot = cellToSlot[cell];
          transforms[slot] = transform;
        }
        else
        {
          // New.
          short slot = nextFreeSlot++;
          cellToSlot[cell] = slot;
          slotToCell[slot] = cell;
          transforms[slot] = transform;
        }
      }

      public short GetNumOccupied()
      {
        return nextFreeSlot;
      }

      public Matrix4x4[] GetTransforms()
      {
        return transforms;
      }

      public void Clear(Int3 u)
      {
        if (GetNumOccupied() == 0)
        {
          return;
        }
        short cell = (short)(u.x * L * L + u.y * L + u.z);
        short slot = cellToSlot[cell];
        if (slot != -1)
        {
          // Move the last slot to this slot.
          short lastSlot = (short)(nextFreeSlot - 1);
          short lastCell = slotToCell[lastSlot];
          transforms[slot] = transforms[lastSlot];
          cellToSlot[lastCell] = slot;
          slotToCell[slot] = lastCell;
          slotToCell[lastSlot] = -1;
          cellToSlot[cell] = -1;
          nextFreeSlot = lastSlot;
        }
      }

      public bool CheckInvariants()
      {
        for (int slot = 0; slot < InstancesPerBlock; slot++)
        {
          if (slot < nextFreeSlot)
          {
            // Expect some valid value
            if (slotToCell[slot] == -1)
            {
              return false;
            }

            int cell = slotToCell[slot];
            if (cellToSlot[cell] != slot)
            {
              return false;
            }
          }
          else
          {
            // Expect nothing set
            if (slotToCell[slot] != -1)
            {
              return false;
            }
          }
        }

        // Expect an exact number of valid cellToSlot entries.
        int usedCells = 0;
        for (int i = 0; i < InstancesPerBlock; i++)
        {
          if (cellToSlot[i] != -1)
          {
            usedCells++;
          }
        }
        if (usedCells != nextFreeSlot)
        {
          return false;
        }

        return true;
      }
    }

    // Provides a set/clear(int3, matrix) interface, backed by an efficiently
    // managed set of blocks. Each block is below a max size and is spatially
    // coherent. Only addressed blocks are actually allocated.
    public class BlockManager
    {
      public Dictionary<Int3, RenderingBlock> blocksTable = new Dictionary<Int3, RenderingBlock>();

      public void ClearAll()
      {
        blocksTable.Clear();
      }

      public void Set(Int3 u, Matrix4x4 transform)
      {
        Int3 blockNum = Int3.Floor(u / Lf);

        RenderingBlock block = null;
        if (!blocksTable.TryGetValue(blockNum, out block))
        {
          block = new RenderingBlock();
          blocksTable.Add(blockNum, block);
        }

        Int3 blockCell = u - (blockNum * L);
        Debug.Assert(blockCell.x >= 0);
        Debug.Assert(blockCell.x < L);
        Debug.Assert(blockCell.y >= 0);
        Debug.Assert(blockCell.y < L);
        Debug.Assert(blockCell.z >= 0);
        Debug.Assert(blockCell.z < L);
        block.Set(blockCell, transform);
      }

      // TODO possible optimization: keep a pool of rendering blocks, so if we
      // fully clear a block, we don't just toss it to garbage or leave the
      // memory wasted.
      public void Clear(Int3 u)
      {
        Int3 blockNum = Int3.Floor(u / Lf);

        RenderingBlock block = null;
        if (!blocksTable.TryGetValue(blockNum, out block))
        {
          return;
        }

        Int3 blockCell = u - (blockNum * L);
        Debug.Assert(blockCell.x >= 0);
        Debug.Assert(blockCell.x < L);
        Debug.Assert(blockCell.y >= 0);
        Debug.Assert(blockCell.y < L);
        Debug.Assert(blockCell.z >= 0);
        Debug.Assert(blockCell.z < L);
        block.Clear(blockCell);

        // TODO if a block is totally cleared, free it (back into a pool!)
      }

      public void ForEachBlock(System.Action<RenderingBlock> process)
      {
        foreach (var pair in blocksTable)
        {
          process(pair.Value);
        }
      }
    }
  }
}
