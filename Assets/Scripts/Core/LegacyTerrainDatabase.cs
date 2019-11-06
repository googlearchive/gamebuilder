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

using static TerrainManager;

public class LegacyTerrainDatabase
{
  public enum LegacyEdgesState : byte
  {
    None = 0,
    West = 1,
    South = 2,
    Both = 3
  }

  public struct LegacyBlockInfo
  {
    /// [2 -- bits Block Direction][2 bits -- Edges State][4 bits ---- Block Shape]
    /// 0 is an empty block.
    byte data;

    public LegacyBlockInfo(BlockDirection direction, LegacyEdgesState edgesState, BlockShape shape)
    {
      data = (byte)((int)direction | ((int)edgesState << 2) | ((int)shape << 4));
    }
    public byte GetData()
    {
      return data;
    }
    public void SetData(byte d)
    {
      data = d;
    }
    public void SetBlockDirection(BlockDirection direction)
    {
      data = (byte)((data & ~3) | (int)direction);
    }
    public BlockDirection GetBlockDirection()
    {
      return (BlockDirection)(data & 3);
    }

    public void SetBlockType(BlockShape type)
    {
      data = (byte)((data & 15) | ((int)type << 4));
    }
    public BlockShape GetBlockType()
    {
      return (BlockShape)(data >> 4);
    }

    public void SetBlockWall(LegacyEdgesState walls)
    {
      data = (byte)((data & ~12) | ((int)walls << 2));
    }
    public LegacyEdgesState GetBlockWall()
    {
      return (LegacyEdgesState)((data >> 2) & 3);
    }
    public bool WestWall()
    {
      return ((LegacyEdgesState)((data >> 2) & 3) & LegacyEdgesState.West) > 0;
    }
    public bool SouthWall()
    {
      return ((LegacyEdgesState)((data >> 2) & 3) & LegacyEdgesState.South) > 0;
    }
  }

}