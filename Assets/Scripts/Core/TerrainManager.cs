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

using System;
using System.Collections.Generic;
using UnityEngine;
using GameBuilder;
using CommandTerminal;
#if TEXTURE_ADJUSTMENTS
using VacuumShaders.TextureExtensions;
#endif

public class TerrainManager : MonoBehaviour
{
  public static int OVERRIDE_STYLE = -1;
  public static int NetworkingVersion = 1;
  public static bool TEST_V2 = true;
  public static Vector3 BLOCK_SIZE = new Vector3(2.5f, 1.5f, 2.5f);

  public static float BlockHeight { get { return BLOCK_SIZE.y; } }

  public static int BlocksYStart = -20;
  public static int BlocksYCount = 150;

  static float HORIZONTAL_SNAP_UNIT { get { return BLOCK_SIZE.x / 2f; } }
  static float VERTICAL_SNAP_UNIT { get { return BLOCK_SIZE.y / 2f; } }

  static float HORIZONTAL_CELL_UNIT { get { return BLOCK_SIZE.x; } }
  static float VERTICAL_CELL_UNIT { get { return BLOCK_SIZE.y; } }

  static Vector3 CELL_OFFSET = new Vector3(0, .75f, 0);

  public int cellDimensionsY { get { return terrainV2 == null ? 50 : terrainV2.GetWorldDimensions().y; } }
  public int cellArrayOffsetX { get { return terrainV2 == null ? 100 : terrainV2.GetWorldDimensions().x / 2; } }
  public int cellArrayOffsetZ { get { return terrainV2 == null ? 100 : terrainV2.GetWorldDimensions().z / 2; } }

  // Triggered when a custom style has just been successfully downloaded from the workshop. Use GetCustomStyleIds and GetCustomStyleTexture to query 
  public event System.Action onCustomStyleTextureChange;

  PhotonView photonView;
  TerrainDatabase database = new TerrainDatabase();
  TerrainRendering rendering;
  VoosEngine engine;
  GameBuilderStage stage;
  WorkshopAssetSource workshopAssets;
  Color32[] fallbackTexture = null;

  List<ulong> customStyleWorkshopIds = new List<ulong>();
  Dictionary<BlockStyle, Texture2D> customStyleTextures = new Dictionary<BlockStyle, Texture2D>();

  Dictionary<ulong, System.Action> workshopDownloadCallbacks = new Dictionary<ulong, Action>();

  [SerializeField] TerrainSystem terrainV2Prefab;
  TerrainSystem terrainV2;

  [SerializeField] List<Texture2D> terrainV2Textures;

  public float minAmbient;

  public enum BlockShape : byte
  {
    // THE INTEGER VALUES MATTER!
    Empty = 0,
    Full = 1,
    Half = 2,
    Ramp = 3,
    Corner = 4
  }

  public enum BlockDirection : byte
  {
    // THE INTEGER VALUES MATTER!
    North = 0,
    East = 1,
    South = 2,
    West = 3
  }

  public enum BlockStyle : ushort
  {
    // THE INTEGER VALUES MATTER!
    // The underlying supports a maximum of 500
    SolidColor0 = 0,
    SolidColor1,
    SolidColor2,
    SolidColor3,
    SolidColor4,
    SolidColor5,
    SolidColor6,
    SolidColor7,
    SolidColor8,
    SolidColor9,
    SolidColor10,
    SolidColor11,
    SolidColor12,
    SolidColor13,
    SolidColor14,
    SolidColor15 = 15,
    Stone = 16,
    Space = 17,
    Grass = 18,
    SnowRock = 19,
    Dirt = 20,
    GrassStone = 21,
    GreyCraters = 22,
    Ice = 23,
    Lava = 24,
    Sand = 25,
    Water = 26,
    Wooden = 27,
    RedCraters = 28,
    IndustrialGreen = 29,
    IndustrialRed = 30,
    GreyBricks = 31,
    MetalBeige = 32,
    RedBricks = 33,
    Road = 34,
    RoadCrossing = 35,
    RoadWhiteBrokenLine = 36,
    RoadYellowUnbrokenLine = 37,
    Pavement = 38,
    PavementConcaveCorner = 39,
    PavementConvexCorner = 40,
    // PUT ALL BUILTINS ABOVE
    FirstCustomStyle = 256
  };

  internal bool IsSettledForPerfMeasurement()
  {
    return terrainV2 != null
    && terrainV2.GetNumDirtyChunks() == 0
    && terrainV2.GetNumDirtyLightChunks() == 0;
  }

  public static BlockStyle LastSolidColorStyle = BlockStyle.SolidColor15;
  public static int NumSolidColorStyles = (int)(LastSolidColorStyle + 1);
  public static int NumTotalStyles = 41;

  [System.Serializable]
  public struct CellValue
  {
    public BlockShape blockType;
    public BlockDirection direction;
    public BlockStyle style;

    public bool IsOccupied()
    {
      return blockType != BlockShape.Empty;
    }

    public CellValue(BlockShape _blocktype, BlockDirection _direction, BlockStyle _style)
    {
      blockType = _blocktype;
      direction = _direction;
      style = _style;
    }

    public override string ToString()
    {
      return $"cell block: {blockType},{style},{direction}";
    }
  }

  [System.Serializable]
  public struct SideValue
  {
    public bool on;
    public BlockStyle style;
  }

  [System.Serializable]
  public struct Cell
  {
    public int x;
    public int y;
    public int z;

    public Cell(int _x, int _y, int _z)
    {
      x = _x;
      y = _y;
      z = _z;
    }

    public Cell(Int3 u)
    {
      x = u.x;
      y = u.y;
      z = u.z;
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Cell))
      {
        return false;
      }

      var cell = (Cell)obj;
      return x == cell.x &&
             y == cell.y &&
             z == cell.z;
    }

    public override int GetHashCode()
    {
      var hashCode = 373119288;
      hashCode = hashCode * -1521134295 + x.GetHashCode();
      hashCode = hashCode * -1521134295 + y.GetHashCode();
      hashCode = hashCode * -1521134295 + z.GetHashCode();
      return hashCode;
    }

    public override string ToString()
    {
      return $"cell: {x},{y},{z}";
    }

    public Int3 ToInt3()
    {
      return new Int3(x, y, z);
    }
  }

  public enum CellSide
  {
    West = 0,
    South = 1
  }

  [System.Serializable]
  public struct Side
  {
    public int x;
    public int y;
    public int z;
    public CellSide side;

    public Side(int _x, int _y, int _z, CellSide _side)
    {
      x = _x;
      y = _y;
      z = _z;
      side = _side;
    }

    public Cell GetCell()
    {
      return new Cell(x, y, z);
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Side))
      {
        return false;
      }

      var side = (Side)obj;
      return x == side.x &&
             y == side.y &&
             z == side.z &&
             this.side == side.side;
    }

    public override int GetHashCode()
    {
      var hashCode = -1111146410;
      hashCode = hashCode * -1521134295 + x.GetHashCode();
      hashCode = hashCode * -1521134295 + y.GetHashCode();
      hashCode = hashCode * -1521134295 + z.GetHashCode();
      hashCode = hashCode * -1521134295 + side.GetHashCode();
      return hashCode;
    }

    public override string ToString()
    {
      return $"side: {x},{y},{z},{side}";
    }
  }

  Int3 GetV2Offset()
  {
    if (terrainV2 == null) return Int3.zero();
    var d = terrainV2.GetWorldDimensions();
    return new Int3(d.x / 2, -BlocksYStart, d.z / 2);
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref rendering);
    Util.FindIfNotSet(this, ref stage);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref workshopAssets);
    photonView = PhotonView.Get(this);
  }

  public int GetNumImportantChunksRemaining()
  {
    return terrainV2 == null ? 1 : Mathf.CeilToInt(terrainV2.GetTotalDirtyChunkImportance());
  }

  public bool HasReachedSteadyState()
  {
    return terrainV2 != null && terrainV2.GetTotalDirtyChunkImportance() == 0f;
  }

  [System.Serializable]
  public struct SetCellRpcJsonable
  {
    public Cell cell;
    public CellValue value;
  }

  [PunRPC]
  void SetCellValueRPC(string jsonString)
  {
    SetCellRpcJsonable args = JsonUtility.FromJson<SetCellRpcJsonable>(jsonString);
    SetCellLocally(args.value, args.cell);
  }

  void SetCellLocally(CellValue val, Cell cell)
  {
    if (terrainV2 == null) return;

    BlockStyle style = val.style;
    if (OVERRIDE_STYLE >= 0)
    {
      Util.LogWarning($"Overriding block style to {OVERRIDE_STYLE}");
      style = (BlockStyle)OVERRIDE_STYLE;
    }

    terrainV2.SetCell(cell.ToInt3() + GetV2Offset(), (int)style, (int)val.blockType - 1 /* offset because we use 0 == empty */, (int)val.direction);
  }

  public void SetCellValue(Cell cell, CellValue cellBlock)
  {
    if (terrainV2 == null) return;
    var v2Id = cell.ToInt3() + GetV2Offset();
    if (!terrainV2.IsInBound(v2Id)) return;
    var args = new SetCellRpcJsonable { cell = cell, value = cellBlock };
    SetCellLocally(args.value, args.cell);
    photonView.RPC("SetCellValueRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(args));
  }

  [System.Serializable]
  public struct SetSideRpcJsonable
  {
    public Side side;
    public SideValue value;
  }

  void SetSideLocally(Side side, SideValue value)
  {
    // TEMP DISABLED!! For 2019-05-29 contractor session..
    return;
    database.SetSide(side, value.on, value.style);
    rendering.OnSetSide(side, value.on, value.style);
  }

  [PunRPC]
  void SetSideValueRPC(string jsonString)
  {
    SetSideRpcJsonable args = JsonUtility.FromJson<SetSideRpcJsonable>(jsonString);
    SetSideLocally(args.side, args.value);
  }

  public void ClearSide(Side side)
  {
    SetSideValue(side, new SideValue { on = false, style = BlockStyle.SolidColor0 });
  }

  public SideValue GetSideValue(Side side)
  {
    bool on = database.IsSideSet(side);
    BlockStyle style = on ? database.GetSideStyle(side) : BlockStyle.SolidColor0;
    return new SideValue { on = on, style = style };
  }

  public void SetSideValue(Side side, SideValue value)
  {
    SetSideLocally(side, value);
    var args = new SetSideRpcJsonable { side = side, value = value };
    photonView.RPC("SetSideValueRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(args));
  }

  public static Vector3 SnapHorizontalPosition(Vector3 rawPosition)
  {
    Vector3 snapPosition = rawPosition;
    snapPosition.x = Mathf.Round(snapPosition.x / HORIZONTAL_SNAP_UNIT) * HORIZONTAL_SNAP_UNIT;
    snapPosition.z = Mathf.Round(snapPosition.z / HORIZONTAL_SNAP_UNIT) * HORIZONTAL_SNAP_UNIT;
    return snapPosition;
  }

  public static Vector3 SnapVerticalPosition(Vector3 rawPosition)
  {
    Vector3 snapPosition = rawPosition;
    snapPosition.y = Mathf.Round(snapPosition.y / VERTICAL_SNAP_UNIT) * VERTICAL_SNAP_UNIT;
    return snapPosition;
  }

  public static Vector3 SnapPosition(Vector3 rawPosition)
  {
    Vector3 snapPosition = rawPosition;
    snapPosition.x = Mathf.Round(snapPosition.x / HORIZONTAL_SNAP_UNIT) * HORIZONTAL_SNAP_UNIT;
    snapPosition.y = Mathf.Round(snapPosition.y / VERTICAL_SNAP_UNIT) * VERTICAL_SNAP_UNIT;
    snapPosition.z = Mathf.Round(snapPosition.z / HORIZONTAL_SNAP_UNIT) * HORIZONTAL_SNAP_UNIT;
    return snapPosition;
  }

  public static Vector3 GetCellCenter(Cell cell)
  {
    return new Vector3(
      cell.x * HORIZONTAL_CELL_UNIT,
      cell.y * VERTICAL_CELL_UNIT,
     cell.z * HORIZONTAL_CELL_UNIT
    ) + CELL_OFFSET;
  }

  public static Cell GetContainingCell(Vector3 vec)
  {
    vec -= CELL_OFFSET;
    return new Cell(Mathf.RoundToInt(vec.x / HORIZONTAL_CELL_UNIT), Mathf.RoundToInt(vec.y / VERTICAL_CELL_UNIT), Mathf.RoundToInt(vec.z / HORIZONTAL_CELL_UNIT));
  }

  public static Side GetSide(Vector3 vec, CellSide cellside)
  {
    Vector3 edgeOffset = (cellside == CellSide.West) ?
      new Vector3(HORIZONTAL_CELL_UNIT / 2f, 0, 0) :
      new Vector3(0, 0, HORIZONTAL_CELL_UNIT / 2f);

    vec = vec - CELL_OFFSET + edgeOffset;

    return new Side(Mathf.RoundToInt(vec.x / HORIZONTAL_CELL_UNIT), Mathf.RoundToInt(vec.y / VERTICAL_CELL_UNIT), Mathf.RoundToInt(vec.z / HORIZONTAL_CELL_UNIT), cellside);
  }

  public static void MoveCellWithVector(Vector3 direction, ref Cell cell)
  {
    direction.Normalize();
    Vector3 absoluteVector = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));

    if (absoluteVector.x > absoluteVector.y)
    {
      if (absoluteVector.x > absoluteVector.z)
      {
        cell.x += (int)Mathf.Sign(direction.x);
      }
      else
      {
        cell.z += (int)Mathf.Sign(direction.z);
      }
    }
    else
    {
      if (absoluteVector.y > absoluteVector.z)
      {
        cell.y += (int)Mathf.Sign(direction.y);
      }
      else
      {
        cell.z += (int)Mathf.Sign(direction.z);
      }
    }
  }

  //returns delta to snap
  public Vector3 SnapToCell(Vector3 rawPosition, out Cell cell, out Vector3 snapPosition)
  {
    // NOTE we really should be clamping on celldimension - cellarrayoffset but taking advantage of the fact that we know
    // horizontal offset is half and vertical offset is none
    cell = new Cell(
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.x / HORIZONTAL_CELL_UNIT), -cellArrayOffsetX, cellArrayOffsetX - 1)),
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.y / VERTICAL_CELL_UNIT), BlocksYStart, BlocksYStart + BlocksYCount - 1)),
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.z / HORIZONTAL_CELL_UNIT), -cellArrayOffsetZ, cellArrayOffsetZ - 1))
    );

    Vector3 deltaVec = new Vector3(
      rawPosition.x - cell.x * HORIZONTAL_CELL_UNIT,
      rawPosition.y - cell.y * VERTICAL_CELL_UNIT,
      rawPosition.z - cell.z * HORIZONTAL_CELL_UNIT
    );
    snapPosition = GetCellCenter(cell);

    return deltaVec;
  }

  public CellValue GetCellValue(Cell cell)
  {
    if (terrainV2 == null)
    {
      return new CellValue { blockType = BlockShape.Empty };
    }

    var v2Id = cell.ToInt3() + GetV2Offset();
    if (!terrainV2.IsInBound(v2Id))
    {
      return new CellValue { blockType = BlockShape.Empty };
    }

    (int style, int shape, int direction) = terrainV2.GetCell(v2Id);
    if (shape == -1)
    {
      return new CellValue { blockType = BlockShape.Empty };
    }
    else
    {
      return new CellValue { blockType = (BlockShape)(shape + 1), style = (BlockStyle)style, direction = (BlockDirection)direction };
    }
  }


  public bool SnapToEmptyCell(Vector3 rawPosition, Vector3 hitNormal, out Cell cell, out Vector3 snapPosition)
  {
    if (terrainV2 == null)
    {
      cell = new Cell();
      snapPosition = rawPosition;
      return false;
    }

    Vector3 deltaVec = SnapToCell(rawPosition, out cell, out snapPosition);
    if (hitNormal != Vector3.zero) deltaVec = hitNormal;

    if (GetCellValue(cell).IsOccupied())
    {
      MoveCellWithVector(deltaVec, ref cell);
      snapPosition = GetCellCenter(cell);
      if (GetCellValue(cell).IsOccupied())
      {
        return false;
      }
    }

    return true;
  }

  public bool SnapToEmptyEdge(Vector3 rawPosition, out Side side, out Vector3 snapPosition)
  {
    Vector3 deltaVec = SnapToEdge(rawPosition, out side, out snapPosition);

    // Debug.Log($"{cell.x},{cell.y},{cell.z}");

    if (database.IsSideSet(side))
    {
      //just look one up
      side.y++;
      snapPosition = EdgeToVector(side);
      if (database.IsSideSet(side))
      {
        return false;
      }
    }

    return true;
  }


  //returns delta to snap
  public Vector3 SnapToEdge(Vector3 rawPosition, out Side side, out Vector3 snapPosition)
  {
    // NOTE we really should be clamping on celldimension - cellarrayoffset but taking advantage of the fact that we know
    // horizontal offset is half and vertical offset is none
    //find the cell
    Cell cell = new Cell(
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.x / HORIZONTAL_CELL_UNIT), -cellArrayOffsetX, cellArrayOffsetX - 1)),
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.y / VERTICAL_CELL_UNIT), 0, cellDimensionsY - 1)),
    Mathf.RoundToInt(Mathf.Clamp(Mathf.Round(rawPosition.z / HORIZONTAL_CELL_UNIT), -cellArrayOffsetZ, cellArrayOffsetZ - 1))
    );

    Vector3 deltaVec = new Vector3(
      rawPosition.x - cell.x * HORIZONTAL_CELL_UNIT,
      rawPosition.y - cell.y * VERTICAL_CELL_UNIT,
      rawPosition.z - cell.z * HORIZONTAL_CELL_UNIT
    );

    side = FindEdgeWithCellAndDeltaVector(cell, deltaVec);

    snapPosition = EdgeToVector(side);

    return deltaVec;
  }

  public static Vector3 EdgeToVector(Side side)
  {
    Vector3 edgeOffset = (side.side == CellSide.West) ?
      new Vector3(HORIZONTAL_CELL_UNIT / 2f, 0, 0) :
      new Vector3(0, 0, HORIZONTAL_CELL_UNIT / 2f);

    return new Vector3(
      side.x * HORIZONTAL_CELL_UNIT,
      side.y * VERTICAL_CELL_UNIT,
     side.z * HORIZONTAL_CELL_UNIT
    ) + CELL_OFFSET - edgeOffset;
  }

  public static Side FindEdgeWithCellAndDeltaVector(Cell cell, Vector3 deltaVec)
  {
    Side side = new Side(cell.x, cell.y, cell.z, CellSide.West);
    Vector3 cardinalVector = GetCardinalDirection(deltaVec);

    if (cardinalVector.x != 0)
    {
      if (cardinalVector.x > 0)
      {
        side.x++;
      }
    }
    else
    {
      if (cardinalVector.z <= 0)
      {
        side.side = CellSide.South;
      }
      else
      {
        side.z++;
        side.side = CellSide.South;
      }
    }
    return side;
  }

  public byte[] SerializeTerrainV2()
  {
    Debug.Assert(terrainV2 != null, "SerializeTerrainV2 called before Reset was called");
    return terrainV2.Serialize();
  }

  public static Vector3 GetCardinalDirection(Vector3 rawVector)
  {
    rawVector.y = 0;
    rawVector.Normalize();
    if (Mathf.Abs(rawVector.x) > Mathf.Abs(rawVector.z))
    {
      return new Vector3(Mathf.Sign(rawVector.x), 0, 0);
    }
    else
    {
      return new Vector3(0, 0, Mathf.Sign(rawVector.z));
    }
  }
  public static Quaternion GetBlockDirectionAsQuaternion(BlockDirection blockDirection)
  {
    switch (blockDirection)
    {
      case TerrainManager.BlockDirection.North:
        return Quaternion.identity;
      case TerrainManager.BlockDirection.East:
        return Quaternion.Euler(0, 90, 0);
      case TerrainManager.BlockDirection.South:
        return Quaternion.Euler(0, 180, 0);
      case TerrainManager.BlockDirection.West:
        return Quaternion.Euler(0, 270, 0);
      default:
        return Quaternion.identity;
    }
  }

  public Metadata GetMetadata()
  {
    return new Metadata
    {
      customStyleWorkshopIds = customStyleWorkshopIds.ToArray(),
    };
  }

  public System.Collections.IEnumerator GetPersistedStateAsync(System.Action<PersistedState> process)
  {
    PersistedState state = new PersistedState();
    state.version = PersistedState.CurrentVersion;
    state.cellString = null;
    Debug.Assert(terrainV2 != null, "terrainV2 was null?");
    yield return terrainV2.SerializeAsync(bytes => state.v2Data = ToBase64(bytes));
    state.metadata = GetMetadata();
    process(state);
  }

  public PersistedState GetPersistedState()
  {
    PersistedState state = new PersistedState();
    state.version = PersistedState.CurrentVersion;
    state.cellString = null;
    using (new Util.ProfileBlock("v2TerrainSerialize"))
    {
      if (terrainV2 != null)
      {
        state.v2Data = ToBase64(terrainV2.Serialize());
      }
    }
    state.metadata = GetMetadata();
    return state;
  }

  void LoadLegacyTerrainData(byte[] terrainDatabaseBytes)
  {
    database.Deserialize(terrainDatabaseBytes);
    rendering.Clear();

    // For legacy, we will only show walls. This is so maps like de_dust2
    // continue to work well. Of course..they won't be able to edit the walls..
    foreach (var args in database.EnumerateSides())
    {
      rendering.OnSetSide(args.side, args.value.on, args.value.style);
    }
  }

  public void SetPersistedState(Vector2 worldSize, PersistedState state)
  {
    byte[] legacyData = null;
    if (state.cellString != null && state.cellString.Length > 0)
    {
      legacyData = Unzip(state.cellString);
    }

    byte[] v2Data = null;
    if (!state.v2Data.IsNullOrEmpty())
    {
      v2Data = Unzip(state.v2Data);
    }

    bool loadingDataFromBeforeDigging = state.version < PersistedState.FirstVersionWithDigging;
    Reset(worldSize, v2Data, loadingDataFromBeforeDigging, legacyData, state.metadata.customStyleWorkshopIds, state.simpleData);
  }

  // Saved to disk and included in the player init payload.
  [System.Serializable]
  public struct Metadata
  {
    public ulong[] customStyleWorkshopIds;
  }

  // What is saved to disk
  [System.Serializable]
  public struct PersistedState
  {
    public static int FirstVersionWithDigging = 1;
    public static int CurrentVersion = 1;

    public int version;
    public string cellString;
    public string v2Data;

    // Only read, not written. This is intended as a simple way to enable
    // external programs to generate terrain data that GB can load. This format
    // should be simple to write and stable over time.
    public string simpleData;

    public Metadata metadata;
  }

  public static string ToBase64(byte[] data)
  {
    return System.Convert.ToBase64String(data);
  }

  public static byte[] Unzip(string gZipString)
  {
    byte[] data = System.Convert.FromBase64String(gZipString);
    return data;
  }

  public void Reset(
    Vector2 worldSize, byte[] terrainV2Data,
    bool loadingDataFromBeforeDigging,
    byte[] legacyData,
    ulong[] customStyleWorkshopIds = null,
    string simpleData = null)
  {
    Debug.Assert(worldSize.magnitude > 0f);
    Util.Log($"Resetting terrain!");

    float groundSizeX = worldSize.x;
    float groundSizeZ = worldSize.y;

    var newDims = new Int3(
      Mathf.CeilToInt(groundSizeX / BLOCK_SIZE.x),
      BlocksYCount,
      Mathf.CeilToInt(groundSizeZ / BLOCK_SIZE.z));

    Debug.Assert(newDims >= new Int3(10, 10, 10));

    Int3 newBotCenter = new Int3(newDims.x / 2, 0, newDims.z / 2);

    // If old data exists, make sure we restore it. This is the resize use case.
    byte[] restoreData = null;
    Int3 restoreDims = Int3.zero();
    if (terrainV2 != null)
    {
      var oldDims = terrainV2.GetWorldDimensions();
      restoreDims = Int3.Min(oldDims, newDims);
      Int3 start = oldDims / 2 - restoreDims / 2;
      Int3 end = start + restoreDims;
      Debug.Assert(start >= Int3.zero());
      Debug.Assert(end <= oldDims);
      restoreData = terrainV2.Serialize(start, end);

      Destroy(terrainV2.gameObject);
      terrainV2 = null;
    }

    using (Util.Profile("terrainV2 instantiate"))
      terrainV2 = Instantiate(terrainV2Prefab, Vector3.zero, Quaternion.identity, this.transform);

    using (Util.Profile("terrainV2 SetWorldDimensions"))
      terrainV2.SetWorldDimensions(newDims);

    terrainV2.SetRootOffset(new Vector3(
      -newDims.x / 2 * 2.5f,
      BlocksYStart * BlockHeight + BlockHeight / 2f,
      -newDims.z / 2 * 2.5f));

    using (Util.Profile("Create color terrain textures"))
    {
      int texSize = terrainV2.GetStyleTextureResolution();
      // Color32 way faster in general than Color.
      for (int i = 0; i < NumSolidColorStyles; i++)
      {
        Color32[] pixels = new Color32[texSize * texSize];
        Color32 color32 = rendering.blockColors[i];
        for (int j = 0; j < pixels.Length; j++)
        {
          pixels[j] = color32;
        }
        terrainV2.SetStyleTextures(i, pixels); // color

        // 10 is orange
        if (i == (int)BlockStyle.SolidColor10)
        {
          fallbackTexture = pixels;
        }
      }
    }

    Debug.Assert(fallbackTexture != null, "Could not find fallbackTexture?");

    // TODO why do we do these specifically? Are they not read in via the loop below?
    terrainV2.SetStyleTextures((int)BlockStyle.Stone, terrainV2Textures[4].GetPixels32()); // stone
    terrainV2.SetStyleTextures((int)BlockStyle.Space, terrainV2Textures[1].GetPixels32()); // metal
    terrainV2.SetStyleTextures((int)BlockStyle.Grass, terrainV2Textures[8].GetPixels32(), terrainV2Textures[7].GetPixels32(), terrainV2Textures[6].GetPixels32()); // grass
    terrainV2.SetStyleTextures((int)BlockStyle.SnowRock, terrainV2Textures[11].GetPixels32(), terrainV2Textures[10].GetPixels32(), terrainV2Textures[9].GetPixels32()); // snow

    foreach (object obj in Enum.GetValues(typeof(BlockStyle)))
    {
      BlockStyle style = (BlockStyle)obj;
      if ((int)style <= (int)BlockStyle.SnowRock)
      {
        // We hard code this above for now.
        continue;
      }
      Color32[] topOrAtlas = null;
      Color32[] side = null;
      Color32[] overflow = null;
      foreach (var tex in terrainV2Textures)
      {
        if (tex == null) continue;
        if (!tex.name.StartsWith(style.ToString().ToLowerInvariant()))
        {
          continue;
        }

        if (tex.name.EndsWith("-top") || tex.name == style.ToString().ToLowerInvariant())
        {
          topOrAtlas = tex.GetPixels32();
        }
        else if (tex.name.EndsWith("-side-ceiling"))
        {
          side = tex.GetPixels32();
        }
        else if (tex.name.EndsWith("-overflow"))
        {
          overflow = tex.GetPixels32();
        }
      }

      if (topOrAtlas == null)
      {
        Util.LogWarning($"Had to use fallback texture for terrain style {style}. side={side}, overflow={overflow}");
        topOrAtlas = fallbackTexture;
      }

      if (side != null)
      {
#if UNITY_EDITOR
        Debug.Assert(overflow != null, $"{style.ToString()} style has a side texture but not an overflow?");
#endif
      }
      else
      {
        if (overflow != null)
        {
          Util.LogWarning($"Style {style} had an overflow texture but not a side? IGNORING overflow.");
          overflow = null;
        }
      }

      terrainV2.SetStyleTextures((int)style, topOrAtlas, side, overflow);
    }

    // Custom styles
    this.customStyleWorkshopIds.Clear();
    if (customStyleWorkshopIds != null)
    {
      this.customStyleWorkshopIds.AddRange(customStyleWorkshopIds);
    }
    UpdateCustomStyleWorkshopIds();

    if (restoreData != null)
    {
      terrainV2.Deserialize(restoreData, (newDims / 2 - restoreDims / 2));
    }

    if (legacyData != null)
    {
      LoadLegacyTerrainData(legacyData);

      // But move all the blocks to our new system.
      using (Util.Profile("legacySync"))
      {
        foreach (var args in database.EnumerateBlocks())
        {
          terrainV2.SetCell(args.cell.ToInt3() + GetV2Offset(), (int)args.value.style, (int)args.value.blockType - 1, (int)args.value.direction);
        }
      }
    }

    if (terrainV2Data != null)
    {
      Util.Log($"loading v2 data of {terrainV2Data.Length} bytes");
      using (Util.Profile("terrainV2 Deserialize"))
        terrainV2.Deserialize(terrainV2Data);

      // Legacy upgrade
      if (loadingDataFromBeforeDigging)
      {
        // The serialized data was before digging. We need to move it up, effectively.
        Debug.Assert(BlocksYStart < 0);
        // Copy...
        byte[] temp = terrainV2.Serialize(
            Int3.zero(),
            newDims.WithY(newDims.y + BlocksYStart));
        // Move up..
        terrainV2.Deserialize(
          temp,
          Int3.zero().WithY(-BlocksYStart));

        // At this point, we actually have 2 copies of the terrain, offset by
        // some Y! heh. But the SetSlices call below will deal with that.
      }
    }

    if (loadingDataFromBeforeDigging)
    {
      // Fill in the ground.
      BlockStyle style = BlockStyle.Grass;
      switch (stage.GetGroundType())
      {
        case GameBuilderStage.GroundType.Snow:
          style = BlockStyle.SnowRock;
          break;
        case GameBuilderStage.GroundType.SolidColor:
        case GameBuilderStage.GroundType.Space:
        case GameBuilderStage.GroundType.Grass:
        default:
          style = BlockStyle.Grass;
          break;
      }
      terrainV2.SetSlices(0, (0 - BlocksYStart), (int)style, 0, 0);
    }

    if (!simpleData.IsNullOrEmpty())
    {
      byte[] zippedBytes = System.Convert.FromBase64String(simpleData);
      using (var zippedStream = new System.IO.MemoryStream(zippedBytes, 0, zippedBytes.Length))
      using (var unzipped = new System.IO.Compression.GZipStream(zippedStream, System.IO.Compression.CompressionMode.Decompress))
      using (System.IO.BinaryReader reader = new System.IO.BinaryReader(unzipped))
      {
        int version = reader.ReadUInt16(); // Unused.
        Debug.Assert(version == 0, $"Unknown simpleData version: {version}");
        uint numBlocks = reader.ReadUInt32();
        Util.Log($"reading in {numBlocks} from simpleData");
        for (int i = 0; i < numBlocks; i++)
        {
          short x = reader.ReadInt16();
          short y = reader.ReadInt16();
          short z = reader.ReadInt16();
          byte shape = reader.ReadByte();
          byte direction = reader.ReadByte();
          ushort style = reader.ReadUInt16();
          this.SetCellValue(
            new Cell(x, y, z),
            new CellValue
            {
              blockType = (BlockShape)shape,
              direction = (BlockDirection)direction,
              style = (BlockStyle)style
            });
        }
      }
    }

    // Now mark chunks with actors in them as important
    foreach (var actor in engine.EnumerateActors())
    {
      // Non-dynamic-physics actors don't need terrain to exist...well, less so.
      if (!actor.GetEnablePhysics()) { continue; }
      var pos = actor.GetSpawnPosition();
      Int3 cell = GetContainingCell(pos).ToInt3();
      terrainV2.ReportRigidbodyAt((cell + GetV2Offset()));
    }
  }

  void UpdateCustomStyleWorkshopIds()
  {
    if (customStyleWorkshopIds.Count > 256)
    {
      throw new System.Exception("We do not support more than 256 custom terrain block styles");
    }

    for (int i = 0; i < customStyleWorkshopIds.Count; i++)
    {
      ulong workshopId = customStyleWorkshopIds[i];
      BlockStyle style = GetCustomStyle(i);

      if (!customStyleTextures.ContainsKey(style))
      {
        // New style - use fallback until we download it.
        Debug.Assert(fallbackTexture != null);
        terrainV2.SetStyleTextures((int)style, fallbackTexture);
      }

      // Kick off download. Why do we do this even if we have the style entry in
      // customStyleTextures? For multiplayer. It's possible that the style's
      // workshop ID is different, ie. someone else just added this at the same
      // time. It's a race condition, but we need to make sure we're consistent.
      // So always kick off a new download.
      workshopAssets.Get(workshopId, maybePath =>
      {
        if (maybePath.IsEmpty())
        {
          Util.LogError($"Failed to download custom terrain block style, workshop ID = {workshopId}");
          return;
        }

        Texture2D texture = ReadCustomStyle(maybePath.Value);
#if TEXTURE_ADJUSTMENTS
        texture.ResizePro(TerrainSystem.tex_size, TerrainSystem.tex_size, false);
#else
        if (texture.width != TerrainSystem.tex_size || texture.height != TerrainSystem.tex_size)
        {
          // Simple bilinear resample. Not great for downsampling.
          Texture2D old = texture;
          texture = new Texture2D(TerrainSystem.tex_size, TerrainSystem.tex_size, TextureFormat.RGBA32, false, false);
          for (int x = 0; x < texture.width; x++)
          {
            for (int y = 0; y < texture.width; y++)
            {
              float u = x * 1f / (texture.width - 1);
              float v = y * 1f / (texture.height - 1);
              Color sampled = old.GetPixelBilinear(u, v);
              texture.SetPixel(x, y, sampled);
            }
          }
          texture.Apply();
        }
#endif

        // Remember texture for use as UI thumbnail
        Debug.Assert(texture != null);
        customStyleTextures[style] = texture;

        // Feed it to terrain system
        Color32[] pixels = texture.GetPixels32();
        terrainV2.SetStyleTextures((int)style, pixels, pixels);

        onCustomStyleTextureChange?.Invoke();

        if (workshopDownloadCallbacks.ContainsKey(workshopId))
        {
          workshopDownloadCallbacks[workshopId]?.Invoke();
          workshopDownloadCallbacks.Remove(workshopId);
        }
      });
    }
  }

  [PunRPC]
  void SetMetadataRPC(string metaJson)
  {
    Metadata meta = JsonUtility.FromJson<Metadata>(metaJson);

    if (Util.AreListsEqual(this.customStyleWorkshopIds, meta.customStyleWorkshopIds))
    {
      return;
    }

    customStyleWorkshopIds.Clear();
    if (meta.customStyleWorkshopIds != null)
    {
      customStyleWorkshopIds.AddRange(meta.customStyleWorkshopIds);
    }
    UpdateCustomStyleWorkshopIds();
  }

  public void AddCustomStyle(ulong workshopId, System.Action onReadyToUse)
  {
    customStyleWorkshopIds.Add(workshopId);
    UpdateCustomStyleWorkshopIds();
    photonView.RPC("SetMetadataRPC", PhotonTargets.AllViaServer, JsonUtility.ToJson(GetMetadata()));

    if (onReadyToUse != null)
    {
      workshopDownloadCallbacks[workshopId] = onReadyToUse;
    }
  }

  BlockStyle GetCustomStyle(int index)
  {
    Debug.Assert(index < customStyleWorkshopIds.Count, "GetCustomStyleId: index exceeded custom style count");
    int styleId = (int)BlockStyle.FirstCustomStyle + index;
    Debug.Assert(styleId < 512, "custom style ID not less than 512?");
    return (BlockStyle)styleId;
  }

  public ulong GetCustomStyleWorkshopId(int index)
  {
    return customStyleWorkshopIds[index];
  }

  public int GetNumCustomStyles()
  {
    return customStyleWorkshopIds.Count;
  }

  public IEnumerable<BlockStyle> GetCustomStyles()
  {
    for (int i = 0; i < customStyleWorkshopIds.Count; i++)
    {
      yield return GetCustomStyle(i);
    }
  }

  // This returns null if the texture is not yet loaded
  public Texture2D GetCustomStyleTexture(BlockStyle style)
  {
    Texture2D rv = null;
    customStyleTextures.TryGetValue(style, out rv);
    return rv;
  }

  // TODO put this code together with TerrainToolSettings.OnPreImportClosed
  public static Texture2D ReadCustomStyle(string dirPath)
  {
    // Find the PNG or JPEG
    string foundImageFile = null;
    foreach (string filePath in System.IO.Directory.EnumerateFiles(dirPath))
    {
      string fileName = System.IO.Path.GetFileName(filePath);
      if (fileName == "image.jpg" || fileName == "image.png")
      {
        foundImageFile = filePath;
        break;
      }
    }

    if (foundImageFile == null)
    {
      throw new System.Exception($"Could not find a JPG or PNG file in {dirPath}");
    }

    Util.Log($"Loading custom terrain texture: {foundImageFile}");
    byte[] bytes = System.IO.File.ReadAllBytes(foundImageFile);
    Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
    tex.LoadImage(bytes);
    return tex;
  }

  public bool IsTerrainCollider(Collider collider)
  {
    return collider.GetComponentInParent<TerrainSystem>() != null;
  }

  public void EmptyAllButOne()
  {
    terrainV2.SetSlices(0, BlocksYCount, 0, -1, 0);
    SetCellValue(new Cell(0, -1, 0), new CellValue(BlockShape.Full, BlockDirection.East, BlockStyle.SolidColor0));
  }

  public void DoStylesTest()
  {
    int texSize = terrainV2.GetStyleTextureResolution();
    int N = 200;
    for (int i = 0; i < N; i++)
    {
      Texture2D texture = new Texture2D(texSize, texSize);
      var color = new Color(
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f),
            UnityEngine.Random.Range(0f, 1f)
          );
      for (int j = 0; j < texSize; j++)
      {
        for (int k = 0; k < texSize; k++)
        {
          texture.SetPixel(j, k, color);
        }
      }
      texture.Apply();
      terrainV2.SetStyleTextures(i, texture.GetPixels32()); // color
    }
    for (int z = 0; z < N; z++)
    {
      terrainV2.SetCell(new Int3(100, -BlocksYStart + 5, z), z, 0, 0);
    }
  }

  public static Cell GetCellForRayHit(Vector3 pos, Vector3 normal)
  {
    // If the hit position is probably right on a face, push it down along the normal. Otherwise, assume it's ready as is.
    Vector3 uvw = (pos - CELL_OFFSET).DivideComponents(BLOCK_SIZE).Remainder(1f);
    // If any component is near half-points (ie. *.5), we're on a face.
    if (Math.Abs(uvw.x).ApproxEquals(0.5f, 1e-4f)
      || Math.Abs(uvw.y).ApproxEquals(0.5f, 1e-4f)
      || Math.Abs(uvw.z).ApproxEquals(0.5f, 1e-4f))
    {
      // It's on a face, push it to the middle along the normal.
      pos -= normal * BLOCK_SIZE.MinAbsComponent() * 0.5f;
    }

    return GetContainingCell(pos);
  }

  void Update()
  {
    if (terrainV2 != null)
    {
      terrainV2.SetMinAmbient(minAmbient);
      // terrainV2.SetMaxLightSpread(4);
    }
  }

  // Mat can be null to go back to non-temp material.
  public void SetTempMaterial(Material mat)
  {
    terrainV2?.SetTempMaterial(mat);
  }

  [RegisterCommand(Help = "Set ground", MinArgCount = 1, MaxArgCount = 1)]
  public static void CommandSetGround(CommandArg[] args)
  {
    TerrainSystem terrain = FindObjectOfType<TerrainSystem>();
    BlockStyle style;
    int shape = 0;

    // Special argument 'Empty' means empty shape.
    if (args[0].ToString() == "Empty")
    {
      style = BlockStyle.SolidColor0;
      shape = -1;
    }
    else if (!Util.TryParseEnum<BlockStyle>(args[0].ToString(), out style, true))
    {
      HeadlessTerminal.Log("Invalid block style: " + args[0].ToString() +
          ". Valid values are: " + string.Join("\n", Util.ValuesOf<BlockStyle>()));
      return;
    }
    TerrainSystem ts = FindObjectOfType<TerrainSystem>();
    ts.SetSlices(0, (0 - BlocksYStart), (int)style, shape, 0);
  }

  public void FindReplace(BlockStyle find, BlockStyle replace)
  {
    terrainV2?.FindReplaceStyle((int)find, (int)replace);
  }

  public Util.Tuple<Cell, CellValue> GetCellValueTuple(Cell cell)
  {
    return new Util.Tuple<Cell, CellValue>(cell, GetCellValue(cell));
  }

  public IEnumerable<Util.Tuple<Cell, CellValue>> GetFilledCells(Cell cellA, Cell cellB)
  {
    Int3 cellIntA = cellA.ToInt3();
    Int3 cellIntB = cellB.ToInt3();

    Int3 min = Int3.Min(cellIntA, cellIntB);
    Int3 max = Int3.Max(cellIntA, cellIntB) + Int3.one();

    // Int3 mins3 = mins.ToInt3();
    // Int3 maxs = inclusiveMaxs.ToInt3() + Int3.one();
    foreach (Int3 u in Int3.Enumerate(min, max))
    {
      CellValue val = GetCellValue(new Cell(u));
      if (val.blockType != BlockShape.Empty)
      {
        yield return new Util.Tuple<Cell, CellValue>(new Cell(u), val);
      }

    }
  }
}
