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
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using static TerrainManager;
using static LegacyTerrainDatabase;

// Handles the efficient storage and access of terrain data.
// Does NOT do networking - that is handled at the TerrainSystem level.
public class TerrainDatabase
{
  public const int CurrentVersion = 2;
  public const int FirstVersionWithMultipleStyles = 2;

  // LEGACY
  public static int cellArrayOffsetX = 100;
  public static int cellArrayOffsetY = 0;
  public static int cellArrayOffsetZ = 100;

  // Bits: 
  public struct BlockInfo
  {
    // Bits:
    // 2 direction
    // 2 shape - note that we do not store empty as a shape. 
    // 4 bits - unused.
    public byte b0;

    // 6 bits for block style.
    public byte b1;

    static byte DirectionMask = 3;
    static byte DirectionShift = 0;

    public void SetDirection(BlockDirection direction)
    {
      int intVal = (int)direction;
      Debug.Assert(intVal >= 0 && intVal < 4);

      b0 = (byte)((b0 & ~DirectionMask) | (intVal << DirectionShift));
    }

    public BlockDirection GetDirection()
    {
      return (BlockDirection)((b0 & DirectionMask) >> DirectionShift);
    }

    public void Copy(CellValue block)
    {
      this.SetShape(block.blockType);
      this.SetDirection(block.direction);
      this.SetStyle(block.style);
    }

    public CellValue ToBlock()
    {
      return new CellValue(GetShape(), GetDirection(), GetStyle());
    }

    static byte ShapeMask = 12;
    static byte ShapeShift = 2;

    public void SetShape(BlockShape type)
    {
      if (type == BlockShape.Empty)
      {
        throw new System.Exception("Should not be setting BlockShape of empty!");
      }
      int intVal = ((int)type) - 1;
      Debug.Assert(intVal >= 0 && intVal < 4);

      b0 = (byte)((b0 & ~ShapeMask) | (intVal << ShapeShift));
    }

    public BlockShape GetShape()
    {
      return (BlockShape)(((b0 & ShapeMask) >> ShapeShift) + 1);
    }

    public void SetStyle(BlockStyle style)
    {
      Debug.Assert((int)style < 64);
      this.b1 = (byte)style;
    }

    public BlockStyle GetStyle()
    {
      return (BlockStyle)this.b1;
    }
  }

  public struct SideInfo
  {
    public byte b0;

    public void SetStyle(BlockStyle style)
    {
      Debug.Assert((int)style < 64);
      this.b0 = (byte)style;
    }

    public BlockStyle GetStyle()
    {
      return (BlockStyle)this.b0;
    }
  }

  class CellComparer : IComparer<Cell>
  {
    public int Compare(Cell a, Cell b)
    {
      if (a.z > b.z) { return 1; }
      if (a.z < b.z) { return -1; }

      if (a.y > b.y) { return 1; }
      if (a.y < b.y) { return -1; }

      if (a.x > b.x) { return 1; }
      if (a.x < b.x) { return -1; }
      return 0;
    }
  }

  SortedDictionary<Cell, BlockInfo> blockMap;
  SortedDictionary<Cell, SideInfo> southSideMap;
  SortedDictionary<Cell, SideInfo> westSideMap;

  public TerrainDatabase()
  {
    blockMap = new SortedDictionary<Cell, BlockInfo>(new CellComparer());
    southSideMap = new SortedDictionary<Cell, SideInfo>(new CellComparer());
    westSideMap = new SortedDictionary<Cell, SideInfo>(new CellComparer());
  }

  public void SetCell(CellValue newBlock, Cell cell)
  {
    if (newBlock.blockType != BlockShape.Empty)
    {
      BlockInfo info = new BlockInfo();
      info.Copy(newBlock);
      blockMap[cell] = info;
    }
    else
    {
      blockMap.Remove(cell);
    }
  }

  public CellValue GetCellValue(Cell cell)
  {
    if (blockMap.ContainsKey(cell))
    {
      CellValue block = new CellValue();
      BlockInfo info = blockMap[cell];
      block.direction = info.GetDirection();
      block.blockType = info.GetShape();
      block.style = info.GetStyle();
      return block;
    }
    else
    {
      return new CellValue { blockType = BlockShape.Empty };
    }
  }

  public void SetSide(Side side, bool on, BlockStyle style)
  {
    var map = side.side == CellSide.South ? southSideMap : westSideMap;
    var cell = side.GetCell();

    if (on)
    {
      SideInfo info = new SideInfo();
      info.SetStyle(style);
      map[cell] = info;
    }
    else
    {
      map.Remove(cell);
    }
  }

  public bool IsSideSet(Side side)
  {
    var map = side.side == CellSide.South ? southSideMap : westSideMap;
    var cell = side.GetCell();
    return map.ContainsKey(cell);
  }

  // It's an error to call this for non-set sides
  public BlockStyle GetSideStyle(Side side)
  {
    var map = side.side == CellSide.South ? southSideMap : westSideMap;
    var cell = side.GetCell();
    return map[cell].GetStyle();
  }

  public int GetFilledCellCount()
  {
    return blockMap.Count;
  }

  void SerializeSideMap(SortedDictionary<Cell, SideInfo> map, UnityEngine.Networking.NetworkWriter writer)
  {
    writer.Write(map.Count);
    foreach (var pair in map)
    {
      writer.Write(pair.Value.b0);
      Debug.Assert(pair.Key.x + cellArrayOffsetX < 256);
      Debug.Assert(pair.Key.y + cellArrayOffsetY < 256);
      Debug.Assert(pair.Key.z + cellArrayOffsetZ < 256);
      writer.Write((byte)(pair.Key.x + cellArrayOffsetX));
      writer.Write((byte)(pair.Key.y + cellArrayOffsetY));
      writer.Write((byte)(pair.Key.z + cellArrayOffsetZ));
    }
  }

  public byte[] Serialize()
  {
    byte[] buffer = new byte[
      4 // version
      + 4 // zero for block count
      + 4 // south count
      + southSideMap.Count * 4
      + 4 // west count
      + westSideMap.Count * 4
      + 1 // ending sanity check number
      + 1 // extra byte cuz NetworkWriter is buggy:
          // https://bitbucket.org/Unity-Technologies/networking/src/78ca8544bbf4e87c310ce2a9a3fc33cdad2f9bb1/Runtime/NetworkBuffer.cs?at=5.3&fileviewer=file-view-default#NetworkBuffer.cs-160
    ];
    var writer = new UnityEngine.Networking.NetworkWriter(buffer);
    writer.Write(CurrentVersion);

    // Write dummy block count
    writer.Write(0);

    SerializeSideMap(southSideMap, writer);
    SerializeSideMap(westSideMap, writer);

    // Sanity check
    writer.Write((byte)42);

    // Util.Log($"Wrote {blockMap.Count} blocks, {southSideMap.Count} south walls, and {westSideMap.Count} west walls to {buffer.Length} bytes");
    return buffer;
  }

  public void Clear()
  {
    blockMap.Clear();
    southSideMap.Clear();
    westSideMap.Clear();
  }

  void LoadLegacy(UnityEngine.Networking.NetworkReader reader)
  {
    int count = reader.ReadInt32();
    Util.Log($"loading {count} legacy cells");
    for (int i = 0; i < count; i++)
    {
      Cell c = new Cell(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
      LegacyBlockInfo info = new LegacyBlockInfo();
      info.SetData(reader.ReadByte());

      CellValue block = new CellValue(info.GetBlockType(), info.GetBlockDirection(), BlockStyle.Stone);
      SetCell(block, c);
      Side westEdge = new Side(c.x, c.y, c.z, CellSide.West);
      SetSide(westEdge, info.WestWall(), BlockStyle.Stone);
      Side southEdge = new Side(c.x, c.y, c.z, CellSide.South);
      SetSide(southEdge, info.SouthWall(), BlockStyle.Stone);
    }
  }

  Cell ReadCell(UnityEngine.Networking.NetworkReader reader)
  {
    int x = reader.ReadByte() - cellArrayOffsetX;
    int y = reader.ReadByte() - cellArrayOffsetY;
    int z = reader.ReadByte() - cellArrayOffsetZ;
    Cell c = new Cell(x, y, z);
    return c;
  }

  public void Deserialize(byte[] cellBytes)
  {
    Clear();

    // Util.Log($"loading terrain data of {cellBytes.Length} bytes");
    var reader = new UnityEngine.Networking.NetworkReader(cellBytes);
    // Again - kinda awkward to have the deserialize code here.
    int version = reader.ReadInt32();

    if (version == 0)
    {
      Util.LogWarning($"Ignoring terrain version 0");
    }
    else if (version == 1)
    {
      LoadLegacy(reader);
    }
    else if (version == TerrainDatabase.CurrentVersion)
    {
      int blockCount = reader.ReadInt32();
      // Util.Log($"reading {blockCount} blocks");
      for (int i = 0; i < blockCount; i++)
      {
        TerrainDatabase.BlockInfo info = new TerrainDatabase.BlockInfo();
        info.b0 = reader.ReadByte();
        info.b1 = reader.ReadByte();
        Cell c = ReadCell(reader);
        Debug.Assert(info.GetShape() != BlockShape.Empty);
        SetCell(info.ToBlock(), c);
      }

      System.Action<CellSide> LoadSideMap = (CellSide cellSide) =>
      {
        int count = reader.ReadInt32();
        // Util.Log($"Reading {count} {cellSide} walls");
        for (int i = 0; i < count; i++)
        {
          TerrainDatabase.SideInfo info = new TerrainDatabase.SideInfo();
          info.b0 = reader.ReadByte();
          Cell c = ReadCell(reader);
          Side side = new Side
          {
            side = cellSide,
            x = c.x,
            y = c.y,
            z = c.z
          };
          SetSide(side, true, info.GetStyle());
        }
      };

      LoadSideMap.Invoke(CellSide.South);
      LoadSideMap.Invoke(CellSide.West);

      Debug.Assert(reader.ReadByte() == 42, "End-of-buffer sanity check failed!");
    }
    else
    {
      throw new System.Exception($"Given terrain of unknown version: {version}.");
    }
  }

  public IEnumerable<SetCellRpcJsonable> EnumerateBlocks()
  {
    foreach (var pair in blockMap)
    {
      yield return new SetCellRpcJsonable
      {
        cell = pair.Key,
        value = pair.Value.ToBlock()
      };
    }
  }

  public IEnumerable<SetSideRpcJsonable> EnumerateSides()
  {
    foreach (var pair in southSideMap)
    {
      Cell c = pair.Key;
      yield return new SetSideRpcJsonable
      {
        side = new Side { side = CellSide.South, x = c.x, y = c.y, z = c.z },
        value = new SideValue { on = true, style = pair.Value.GetStyle() }
      };
    }
    foreach (var pair in westSideMap)
    {
      Cell c = pair.Key;
      yield return new SetSideRpcJsonable
      {
        side = new Side { side = CellSide.West, x = c.x, y = c.y, z = c.z },
        value = new SideValue { on = true, style = pair.Value.GetStyle() }
      };
    }
  }
}
