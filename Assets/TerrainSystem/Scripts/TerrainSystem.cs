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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;
using UnityEngine;
using System.Threading;
using GameBuilder;

public class TerrainSystem : MonoBehaviour
{
  public static bool UseMips = true;

  public static readonly Int3 ChunkDims = new Int3(15, 15, 15);

  // Uses its enabled status as a "dirty" flag

  public Material material;
  public GameObject[] blocks;

  // Derived from block object measurements
  TileType[] types;
  Vector3 cell_size;
  Vector3 inv_cell_size;

  Int3 dimensions;

  // A slice is an entire XZ plane of tiles. Cached nx * nz for fast y index
  // traversal
  int TilesPerSlice;

  // Source tile data. Not sure what NativeArray buys us here..I guess it'll
  // help catch race conditions (ie. writing while jobs are reading).
  NativeArray<int> tile_shapes;
  NativeArray<int> tile_styles;
  NativeArray<int> tile_directions; // 0-3, multiplied by 90 to get degrees on y axis

  NativeArray<float> tile_colors;
  NativeArray<int>[] spans;
  NativeArray<int>[] span_lengths;

  byte[] bitmasks;
  Chunk[,,] chunks;
  GameObject chunk_holder;

#if TERRAIN_TEST_ENVIRONMENT
    List<DebugDraw.DebugDrawLine> debug_lines = new List<DebugDraw.DebugDrawLine>();
#endif

  class Style
  {
    public Color32[] base_tex;
    public Color32[] side_tex;
    public Color32[] border_tex;

    public bool base_tex_dirty;
    public bool side_tex_dirty;
    public bool border_tex_dirty;

    public int base_tex_index;
    public int side_tex_index;
    public int border_tex_index;
  }

  Dictionary<int, Style> styles = new Dictionary<int, Style>();

  Texture2DArray tex_array;
  Texture2DArray border_tex_array;
  bool tex_array_dirty = false;

  public const int tex_size = 512;
  const int border_tex_w = 128;
  const int border_tex_h = 32;

  // Chunk coord to importance.
  Dictionary<Int3, float> dirty_chunks = new Dictionary<Int3, float>(new Int3Comparer());
  HashSet<Int3> bitmask_dirty_chunks = new HashSet<Int3>(new Int3Comparer());
  HashSet<Int3> lighting_dirty_chunks = new HashSet<Int3>(new Int3Comparer());

  // Job handles for short-lived jobs.
  JobHandle? meshHandle = null;
  JobHandle? lightHandle = null;
  HashSet<JobHandle[]> encodeHandles = new HashSet<JobHandle[]>();

  // Call this before any code that changes the three tile data arrays.
  // Otherwise, Unity might crash, complaining that you're modifying an array
  // while a job is reading it.
  void CompleteTileDataJobs()
  {
    if (meshHandle != null) meshHandle.Value.Complete();
    if (lightHandle != null) lightHandle.Value.Complete();
    foreach (var handles in encodeHandles)
    {
      foreach (var h in handles)
      {
        h.Complete();
      }
    }
  }

  Int3 sky_dirty_min;
  Int3 sky_dirty_max;

  int max_spread = 4;
  float min_ambient = 0.03f;

  class Chunk
  {
    public Mesh mesh;
    public Mesh phys_mesh;
    public GameObject obj;
    public Int3 offset;
    public MeshRenderer renderer;

    public readonly SimpleMesh physics_mesh = new SimpleMesh();
    public readonly EditableMesh editable_mesh = new EditableMesh();
  }

  public const int serialization_version = 0;

  public int GetStyleTextureResolution()
  {
    return tex_size;
  }

  byte BitmaskIndex(int x, int y, int z, int border)
  {
    return bitmasks[(y * TilesPerSlice + z * dimensions[0] + x) * 6 + border];
  }

  // Mesh is composed entirely of some subset of quads on planes divided into 4 tris in X shape
  // There's a plane for each cardinal direction, and for diagonals
  // Cardinal direction planes are bitmasked together to eliminate hidden tris

  // We have grid of vertices and grid of edges
  // As we add tris, edges keep track of attached tris

  void CheckTextureSpecs(Color32[] tex)
  {
    Debug.Assert(tex.Length == tex_size * tex_size,
                 $"Texture has {tex.Length} pixels, but it needs to be {tex_size}x{tex_size}");
  }

  void CheckBorderTextureSpecs(Color32[] tex)
  {
    Debug.Assert(tex.Length == border_tex_w * border_tex_h,
                 $"Border texture has {tex.Length} pixels, but it needs to be {border_tex_w}x{border_tex_h}");
  }

  public void SetStyleTextures(int style, Color32[] floorTile, Color32[] wallTile = null, Color32[] overflowTrim = null)
  {
    CheckTextureSpecs(floorTile);
    if (wallTile != null)
    {
      CheckTextureSpecs(wallTile);
    }
    if (overflowTrim != null)
    {
      CheckBorderTextureSpecs(overflowTrim);
    }

    styles.TryGetValue(style, out Style new_style);

    bool chunks_need_rebuild = false;

    if (new_style == null)
    {
      new_style = new Style();
      new_style.base_tex = floorTile;
      new_style.base_tex_dirty = true;
      if (wallTile != null)
      {
        new_style.side_tex = wallTile;
        new_style.side_tex_dirty = true;
      }
      if (overflowTrim != null)
      {
        new_style.border_tex = overflowTrim;
        new_style.border_tex_dirty = true;
      }
      styles[style] = new_style;
    }
    else
    {
      if ((new_style.side_tex == null) ^ (wallTile == null))
      {
        chunks_need_rebuild = true; // We changed an atlas texture to a style texture or vice versa, need rebuild
      }
      if ((new_style.border_tex == null) ^ (overflowTrim == null))
      {
        chunks_need_rebuild = true; // We added or removed overflowTrim, need rebuild
      }
      if (new_style.base_tex != floorTile)
      {
        new_style.base_tex = floorTile;
        new_style.base_tex_dirty = true;
      }
      if (new_style.side_tex != wallTile)
      {
        new_style.side_tex = wallTile;
        new_style.side_tex_dirty = true;
      }
      if (new_style.border_tex != overflowTrim)
      {
        new_style.border_tex = overflowTrim;
        new_style.border_tex_dirty = true;
      }
    }

    if (chunks_need_rebuild)
    {
      Int3 dirty_min = Int3.zero();
      Int3 dirty_max = GetChunkCoord(dimensions - Int3.one());
      for (int x = dirty_min[0]; x <= dirty_max[0]; ++x)
      {
        for (int y = dirty_min[1]; y <= dirty_max[1]; ++y)
        {
          for (int z = dirty_min[2]; z <= dirty_max[2]; ++z)
          {
            var chunk_coord = new Int3(x, y, z);
            dirty_chunks.SetIfMissing(chunk_coord, 0f);
          }
        }
      }
    }

    tex_array_dirty = true;
    this.enabled = true;
  }

  public void SetMinAmbient(float val)
  {
    val = Mathf.Clamp(val, 0.0f, 1.0f);
    if (val != min_ambient)
    {
      min_ambient = val;
      Shader.SetGlobalFloat("_terrain_min_ambient", min_ambient);
    }
  }

  public float GetMinAmbient()
  {
    return min_ambient;
  }

  // Default: 4
  // The size of the area affected by a local edit is (n*2)^3, so beware of light recalculation time
  // if this is much higher
  public void SetMaxLightSpread(int val)
  {
    Debug.Assert(val >= 0);
    if (val != max_spread)
    {
      max_spread = val;
      sky_dirty_min = new Int3(0, 0, 0);
      sky_dirty_max = dimensions;
      this.enabled = true;
    }
  }

  public void SetWorldDimensions(Int3 new_dimensions)
  {
    CompleteTileDataJobs();

    dimensions = new_dimensions;
    TilesPerSlice = dimensions[0] * dimensions[2];
    int num_tiles = dimensions[0] * dimensions[1] * dimensions[2];
    Shader.SetGlobalVector("_map_dims", new Vector4(dimensions[0], dimensions[1], dimensions[2], 0.0f));
    if (tile_shapes.IsCreated)
    {
      DisposeNativeArrays();
    }
    tile_shapes = new NativeArray<int>(num_tiles, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    tile_colors = new NativeArray<float>(num_tiles, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    tile_styles = new NativeArray<int>(num_tiles, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    tile_directions = new NativeArray<int>(num_tiles, Allocator.Persistent, NativeArrayOptions.ClearMemory);
    spans = new NativeArray<int>[dimensions[1]];
    for (int i = 0; i < dimensions[1]; ++i)
    {
      spans[i] = new NativeArray<int>(TilesPerSlice, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }
    span_lengths = new NativeArray<int>[dimensions[1]];
    for (int i = 0; i < dimensions[1]; ++i)
    {
      span_lengths[i] = new NativeArray<int>(dimensions[2], Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }

    sky_job = new SkyLightJob();
    sky_job.tile_shapes = new NativeArray<int>(num_tiles, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    sky_job.sky_light_dist = new NativeArray<int>(num_tiles, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    sky_job.color = new NativeArray<float>(num_tiles, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    sky_job.write_bounds = new NativeArray<Int3>(2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

    bitmasks = new byte[dimensions[0] * dimensions[1] * dimensions[2] * 6];

    for (int i = 0; i < tile_shapes.Length; ++i)
    {
      tile_shapes[i] = -1;
    }
    for (int i = 0; i < tile_colors.Length; ++i)
    {
      tile_colors[i] = 1f;
    }

    if (chunk_holder != null)
    {
      Destroy(chunk_holder);
    }
    chunk_holder = new GameObject("Terrain Chunks");
    chunk_holder.transform.parent = transform;
    chunk_holder.transform.localPosition = Vector3.zero;
    Int3 chunk_dims = new Int3(Mathf.CeilToInt(dimensions[0] / (float)ChunkDims[0]),
                               Mathf.CeilToInt(dimensions[1] / (float)ChunkDims[1]),
                               Mathf.CeilToInt(dimensions[2] / (float)ChunkDims[2]));

    Util.Log($"Chunks count: {chunk_dims}");
    chunks = new Chunk[chunk_dims[0], chunk_dims[1], chunk_dims[2]];
    for (int x = 0; x < chunk_dims[0]; ++x)
    {
      for (int y = 0; y < chunk_dims[1]; ++y)
      {
        for (int z = 0; z < chunk_dims[2]; ++z)
        {
          var chunk = new Chunk();
          chunk.editable_mesh.Clear();
          chunk.obj = new GameObject($"Chunk {x},{y},{z}");
          chunk.obj.transform.parent = chunk_holder.transform;
          chunk.obj.tag = "Ground";
          var collider = chunk.obj.AddComponent<MeshCollider>();
          collider.cookingOptions = MeshColliderCookingOptions.None;
          var filter = chunk.obj.AddComponent<MeshFilter>();
          chunk.renderer = chunk.obj.AddComponent<MeshRenderer>();
          chunk.renderer.sharedMaterial = material;
          chunks[x, y, z] = chunk;
        }
      }
    }

    this.enabled = true; // Needs to refresh mesh before drawing

    // Always force a full recalc of sky light
    sky_dirty_min = new Int3(0, 0, 0);
    sky_dirty_max = dimensions;
  }

  void AddDirtyChunk(Int3 val)
  {
    if (val[0] >= 0 && val[0] < chunks.GetLength(0) &&
       val[1] >= 0 && val[1] < chunks.GetLength(1) &&
       val[2] >= 0 && val[2] < chunks.GetLength(2))
    {
      dirty_chunks.SetIfMissing(val, 0f);
      bitmask_dirty_chunks.Add(val);
      lighting_dirty_chunks.Add(val);
    }
  }

  int TileIndex(Int3 coord)
  {
    return coord[1] * TilesPerSlice + coord[2] * dimensions[0] + coord[0];
  }

  int TileIndex(int x, int y, int z)
  {
    return y * TilesPerSlice + z * dimensions[0] + x;
  }

  static int GenericIndex(Int3 coord, Int3 size)
  {
    return (coord[1] * size[2] + coord[2]) * size[0] + coord[0];
  }

  Chunk GetChunk(Int3 coord)
  {
    return chunks[coord[0], coord[1], coord[2]];
  }

  Int3 GetChunkCoord(Int3 coord)
  {
    return new Int3(coord[0] / ChunkDims[0],
                    coord[1] / ChunkDims[1],
                    coord[2] / ChunkDims[2]);
  }

  public bool IsInBound(Int3 coord)
  {
    return (coord[0] >= 0 && coord[0] < dimensions[0] &&
            coord[1] >= 0 && coord[1] < dimensions[1] &&
            coord[2] >= 0 && coord[2] < dimensions[2]);
  }

  void SetSlicesInternal(int[] sliceData, NativeArray<int> dest, int firstSliceY, int numSlices)
  {
    Debug.Assert(sliceData.Length == TilesPerSlice);
    for (int dy = 0; dy < numSlices; dy++)
    {
      int nativeStart = (firstSliceY + dy) * TilesPerSlice;
      Debug.Assert(nativeStart >= 0);
      Debug.Assert(nativeStart + TilesPerSlice <= dest.Length);
      NativeArray<int>.Copy(sliceData, 0, dest, nativeStart, TilesPerSlice);
    }
  }

  public void SetSlices(int firstSliceY, int numSlices, int style, int shape, int direction)
  {
    CompleteTileDataJobs();

    int[] temp = new int[TilesPerSlice];

    for (int i = 0; i < TilesPerSlice; i++) temp[i] = style;
    SetSlicesInternal(temp, tile_styles, firstSliceY, numSlices);

    for (int i = 0; i < TilesPerSlice; i++) temp[i] = shape;
    SetSlicesInternal(temp, tile_shapes, firstSliceY, numSlices);

    for (int i = 0; i < TilesPerSlice; i++) temp[i] = direction;
    SetSlicesInternal(temp, tile_directions, firstSliceY, numSlices);

    // Mark chunks dirty.

    // Hmm by some quirk, it's actually better for settling-latency to mark ALL
    // chunks dirty.

    /*
    Int3 first_coord = new Int3(0, firstSliceY, 0);
    Int3 last_coord = new Int3(dimensions[0], firstSliceY + numSlices, dimensions[2]) - Int3.one();
    Int3 first_chunk = GetChunkCoord(first_coord);
    Int3 last_chunk = GetChunkCoord(last_coord);
    */
    Int3 first_chunk = new Int3(0, 0, 0);
    Int3 last_chunk = GetChunkCoord(dimensions - Int3.one());

    for (int x = first_chunk[0]; x <= last_chunk[0]; ++x)
    {
      for (int y = first_chunk[1]; y <= last_chunk[1]; ++y)
      {
        for (int z = first_chunk[2]; z <= last_chunk[2]; ++z)
        {
          var chunk_coord = new Int3(x, y, z);
          dirty_chunks.SetIfMissing(chunk_coord, 0f);
          bitmask_dirty_chunks.Add(chunk_coord);
        }
      }
    }

    /*
    sky_dirty_min[0] = Mathf.Min(sky_dirty_min[0], first_coord[0]);
    sky_dirty_min[1] = Mathf.Min(sky_dirty_min[1], first_coord[1]);
    sky_dirty_min[2] = Mathf.Min(sky_dirty_min[2], first_coord[2]);
    sky_dirty_max[0] = Mathf.Max(sky_dirty_max[0], last_coord[0] + 1);
    sky_dirty_max[1] = Mathf.Max(sky_dirty_max[1], last_coord[1] + 1);
    sky_dirty_max[2] = Mathf.Max(sky_dirty_max[2], last_coord[2] + 1);
    */
    sky_dirty_min = new Int3(0, 0, 0);
    sky_dirty_max = dimensions;

    this.enabled = true; // Needs to refresh mesh before drawing
  }

  // TODO should really limit style to be ushort, since we only support 9 bits.
  public void SetCell(Int3 coord, int style, int shape, int direction)
  {
    if (!IsInBound(coord)) return;

    CompleteTileDataJobs();

    Debug.Assert(style >= 0, $"Style must not be negative: {dimensions}");
    Debug.Assert(direction >= 0, $"Direction must not be negative: {dimensions}");
    Debug.Assert(shape >= -1, $"Shape must be -1 or greater");
    int index = TileIndex(coord);
    if (tile_directions[index] == direction &&
       tile_shapes[index] == shape &&
       tile_styles[index] == style)
    {
      // No change needed
      return;
    }
    tile_directions[index] = direction;
    tile_shapes[index] = shape;
    tile_styles[index] = style;
    tile_shapes[index] = shape;

    Int3 dirty = Int3.zero();
    for (dirty[0] = coord[0] - 1; dirty[0] <= coord[0] + 1; ++dirty[0])
    {
      for (dirty[1] = coord[1] - 1; dirty[1] <= coord[1] + 1; ++dirty[1])
      {
        for (dirty[2] = coord[2] - 1; dirty[2] <= coord[2] + 1; ++dirty[2])
        {
          AddDirtyChunk(new Int3(dirty[0] / ChunkDims[0],
                                 dirty[1] / ChunkDims[1],
                                 dirty[2] / ChunkDims[2]));
        }
      }
    }
    sky_dirty_min[0] = Mathf.Min(sky_dirty_min[0], coord[0]);
    sky_dirty_min[1] = Mathf.Min(sky_dirty_min[1], coord[1]);
    sky_dirty_min[2] = Mathf.Min(sky_dirty_min[2], coord[2]);
    sky_dirty_max[0] = Mathf.Max(sky_dirty_max[0], coord[0] + 1);
    sky_dirty_max[1] = Mathf.Max(sky_dirty_max[1], coord[1] + 1);
    sky_dirty_max[2] = Mathf.Max(sky_dirty_max[2], coord[2] + 1);

    this.enabled = true; // Needs to refresh mesh before drawing
  }

  public (int style, int shape, int direction) GetCell(Int3 coord)
  {
    var index = TileIndex(coord);
    return (tile_styles[index], tile_shapes[index], tile_directions[index]);
  }

  public float GetCellLightAmount(Int3 coord)
  {
    return tile_colors[TileIndex(coord)];
  }

  public byte[] Serialize()
  {
    // If no params, just serialize all tiles
    return Serialize(Int3.zero(), dimensions);
  }

  // 0 - Block - occludes all sides fully
  // 1 - Half - fully occludes +x, +z, half occludes +y and -y (+x+z corner)
  // 2 - Ramp - fully occludes +x, -z, half occludes +z and -z (+x-y corner)
  // 3 - Corner - fully occludes -y, half occludes +z (+x-y corner), half occludes +x (+z-y corner)

  class TileType
  {
    public GameObject prefab;
    public Vector2[] border_uvs;
    public Vector2[] slope_uvs;
    public uint[] borders; // bitmask for which tris (in X configuration) exist on each side
                           // face order -x, +x, -y, +y, -z, +z
                           // tri order, -x, -y, -z, +x, +y, +z
    public uint[] slopes;
  }

  class EditableMesh
  {
    public readonly List<Vector3> vert_list = new List<Vector3>();
    public readonly List<int> tri_list = new List<int>();
    public readonly List<Vector2> uv_list = new List<Vector2>();
    public readonly List<Vector3> normal_list = new List<Vector3>();
    public readonly List<Color> color_list = new List<Color>();

    public void Clear()
    {
      vert_list.Clear();
      tri_list.Clear();
      uv_list.Clear();
      normal_list.Clear();
      color_list.Clear();
    }
  }

  class SimpleMesh
  {
    public readonly List<Vector3> vert_list = new List<Vector3>();
    public readonly List<int> tri_list = new List<int>();

    public void Clear()
    {
      vert_list.Clear();
      tri_list.Clear();
    }
  }

  static Vector3 GridPos((int x, int y, int z) coord, Vector3 cell_size)
  {
    // Get position of center of tile in world space
    var pos = new Vector3(coord.x, coord.y, coord.z);
    pos = Vector3.Scale(pos, cell_size);
    return pos;
  }

  static void InstantiateOnGrid(GameObject prefab, (int x, int y, int z) coord, int rotation, Transform parent, Vector3 cell_size)
  {
    var new_block = Instantiate(prefab);
    new_block.transform.parent = parent;
    new_block.transform.position = GridPos(coord, cell_size);
    new_block.transform.rotation = Quaternion.AngleAxis(90f * rotation, Vector3.up);
  }

  // basis vectors for each cube face
  // normal, right, up
  // border quadrants are +right, +up, -right, -up
  static Vector3[] cube_face_vec = new Vector3[] {
        -Vector3.right,   Vector3.forward,    -Vector3.up,
         Vector3.right,   Vector3.forward,    -Vector3.up,
        -Vector3.up,      Vector3.right, -Vector3.forward,
         Vector3.up,      Vector3.right, -Vector3.forward,
        -Vector3.forward, -Vector3.right, -Vector3.up,
         Vector3.forward, -Vector3.right, -Vector3.up,
    };

  Vector3[] slope_vec = new Vector3[24];

  void AnalyzeFace(Collider collider, Vector3 normal, Vector3 right, Vector3 up, float offset, out uint dirHitMask, out Vector2 uv_min, out Vector2 uv_max)
  {
    var unscaled_normal = normal;
    normal = Vector3.Scale(normal, cell_size);
    right = Vector3.Scale(right, cell_size);
    up = Vector3.Scale(up, cell_size);
    Vector3 origin = normal * offset;
    const float max_dist = 0.1f;
    var ray = new Ray();
    RaycastHit hit_info = new RaycastHit();
    dirHitMask = 0;
    uv_min = Vector2.zero;
    uv_max = Vector2.zero;
    for (int dir = 0; dir < 4; ++dir)
    {
      ray.origin = origin + unscaled_normal * max_dist * 0.5f;
      switch (dir)
      {
        case 0:
          ray.origin += 0.25f * right;
          break;
        case 1:
          ray.origin += 0.25f * up;
          break;
        case 2:
          ray.origin -= 0.25f * right;
          break;
        case 3:
          ray.origin -= 0.25f * up;
          break;
      }
      ray.direction = -normal;
      bool hit = collider.Raycast(ray, out hit_info, max_dist);
      if (hit)
      {
        dirHitMask |= 1U << dir;
        var tex = hit_info.textureCoord;
        var orig = ray.origin;
        ray.origin = orig + right * 0.01f;
        hit = collider.Raycast(ray, out hit_info, max_dist);
        if (!hit)
        {
          Debug.LogError("Didn't hit U");
        }
        var tex_u = (hit_info.textureCoord - tex) * 100f;
        ray.origin = orig + up * 0.01f;
        hit = collider.Raycast(ray, out hit_info, max_dist);
        if (!hit)
        {
          Debug.LogError("Didn't hit V");
        }
        var tex_v = (hit_info.textureCoord - tex) * 100f;
        //Debug.Log($"Border tex: {tex.ToString("F4")}  {tex_u.ToString("F4")}  {tex_v.ToString("F4")}");
        switch (dir)
        {
          case 0:
            uv_min = tex - tex_u * 0.75f - tex_v * 0.5f;
            uv_max = tex + tex_u * 0.25f + tex_v * 0.5f;
            break;
          case 1:
            uv_min = tex - tex_u * 0.5f - tex_v * 0.75f;
            uv_max = tex + tex_u * 0.5f + tex_v * 0.25f;
            break;
          case 2:
            uv_min = tex - tex_u * 0.25f - tex_v * 0.5f;
            uv_max = tex + tex_u * 0.75f + tex_v * 0.5f;
            break;
          case 3:
            uv_min = tex - tex_u * 0.5f - tex_v * 0.25f;
            uv_max = tex + tex_u * 0.5f + tex_v * 0.75f;
            break;
        }
        //Debug.Log($"Orig coords: {uv_min.ToString("F4")}  {uv_max.ToString("F4")}");
      }
    }
    // Debug.Log($"Result: {FromBinary(result)}");
  }

  // Use physics raycasts to get X bitmasks for each side and possible slope
  void AnalyzeBlock(TileType type)
  {
    // Spawn tile temporarily
    var temp = Instantiate(type.prefab);
    var collider = temp.GetComponent<Collider>();

    float max_dist = 0.1f;
    Ray ray = new Ray();
    RaycastHit hit_info = new RaycastHit();
    type.border_uvs = new Vector2[4 * 6 * 2];
    type.slope_uvs = new Vector2[4 * 8 * 2];
    type.borders = new uint[4];
    type.slopes = new uint[4];

    var back_face_hit = Physics.queriesHitBackfaces;
    Physics.queriesHitBackfaces = false;

    for (int rotation = 0; rotation < 4; ++rotation)
    {
      type.borders[rotation] = 0;
      type.slopes[rotation] = 0;
      Quaternion quat = Quaternion.AngleAxis(-90f * rotation, Vector3.up);
      // Detect what quadrants are filled on each face of this block
      for (int i = 0, index = 0; i < 18; i += 3)
      {
        AnalyzeFace(collider,
                    quat * cube_face_vec[i],
                    quat * cube_face_vec[i + 1],
                    quat * cube_face_vec[i + 2],
                    0.5f,
                    out uint result,
                    out Vector2 uv_min,
                    out Vector2 uv_max);

        type.borders[rotation] |= result << index;
        type.border_uvs[rotation * 12 + ((i / 3)) * 2 + 0] = uv_min;
        type.border_uvs[rotation * 12 + ((i / 3)) * 2 + 1] = uv_max;
        index += 4;
      }

      // Do the same for possible slopes
      // 0-3 = diagonal walls
      // 4-7 = ramps normal +x, -z, -x, +z (might have z sign backwards)
      // Faces (right, top, left, bottom)
      for (int i = 0, index = 0; i < 8; ++i)
      {
        AnalyzeFace(collider,
                    quat * slope_vec[i * 3 + 0],
                    quat * slope_vec[i * 3 + 1],
                    quat * slope_vec[i * 3 + 2],
                    0.0f,
                    out uint result,
                    out Vector2 uv_min,
                    out Vector2 uv_max);

        type.slopes[rotation] |= result << index;
        type.slope_uvs[rotation * 16 + i * 2 + 0] = uv_min;
        type.slope_uvs[rotation * 16 + i * 2 + 1] = uv_max;
        index += 4;
      }
    }

    Physics.queriesHitBackfaces = back_face_hit;
    Destroy(temp);
  }

  static void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector2 uv_a, Vector2 uv_b, Vector2 uv_c, Vector3 normal, bool flip, EditableMesh editable_mesh)
  {
    int vert_index = editable_mesh.vert_list.Count;
    editable_mesh.vert_list.Add(a);
    editable_mesh.vert_list.Add(flip ? b : c);
    editable_mesh.vert_list.Add(flip ? c : b);
    editable_mesh.tri_list.Add(vert_index);
    editable_mesh.tri_list.Add(vert_index + 1);
    editable_mesh.tri_list.Add(vert_index + 2);
    editable_mesh.normal_list.Add(normal);
    editable_mesh.normal_list.Add(normal);
    editable_mesh.normal_list.Add(normal);
    editable_mesh.uv_list.Add(uv_a);
    editable_mesh.uv_list.Add(flip ? uv_b : uv_c);
    editable_mesh.uv_list.Add(flip ? uv_c : uv_b);
    editable_mesh.color_list.Add(Color.red);
    editable_mesh.color_list.Add(Color.red);
    editable_mesh.color_list.Add(Color.red);
  }

  // Serialize all tiles from start (inclusive) to end (exclusive)
  public byte[] Serialize(Int3 start, Int3 end)
  {
    CompleteTileDataJobs();

    var size = end - start;
    int sliceSize = size[0] * size[2];
    int numJobs = size[1];
    var encodeJobs = new EncodeJob[numJobs];
    var jobHandles = new JobHandle[numJobs];

    using (Util.Profile("Setup jobs"))
      for (int i = 0, index = start[1] * TilesPerSlice; i < numJobs; ++i, index += TilesPerSlice)
      {
        ref var span_job = ref encodeJobs[i];
        span_job.num_spans = size[2];
        span_job.start_x = start[0];
        span_job.start_z = start[2];
        span_job.dimensions_x = dimensions[0];
        span_job.tile_directions = tile_directions.Slice(index, TilesPerSlice);
        span_job.tile_shapes = tile_shapes.Slice(index, TilesPerSlice);
        span_job.tile_styles = tile_styles.Slice(index, TilesPerSlice);
        span_job.span = spans[i].Slice(0, sliceSize);
        span_job.span_lengths = span_lengths[i].Slice(0, size[2]);
        encodeJobs[i] = span_job;
      }

    using (Util.Profile("Execute jobs"))
    {
      // Non-job-system
      // for (int i = 0; i < numJobs; ++i)
      // {
      //   encodeJobs[i].Execute();
      // }

      for (int i = 0; i < numJobs; ++i)
      {
        jobHandles[i] = encodeJobs[i].Schedule();
      }
      for (int i = 0; i < numJobs; ++i)
      {
        jobHandles[i].Complete();
      }
    }
    using (Util.Profile("WriteCompressed"))
    using (MemoryStream memStream = new MemoryStream())
    {
      using (DeflateStream dstream = new DeflateStream(memStream, System.IO.Compression.CompressionLevel.Fastest, true))
      {
        using (BinaryWriter writer = new BinaryWriter(dstream))
        {
          writer.Write(serialization_version);
          writer.Write(size[0]);
          writer.Write(size[1]);
          writer.Write(size[2]);
          for (int y = 0; y < size[1]; ++y)
          {
            for (int z = 0; z < size[2]; ++z)
            {
              int num_segments = span_lengths[y][z];
              writer.Write(num_segments);
              for (int x = 0; x < num_segments; ++x)
              {
                writer.Write(spans[y][z * size[0] + x]);
              }
            }
          }
        }
      }
      var data = memStream.ToArray();
      Debug.Log($"Serialized to: {data.Length} bytes");
      return data;
    }
  }

  public System.Collections.IEnumerator SerializeAsync(System.Action<byte[]> process)
  {
    CompleteTileDataJobs();

    Int3 start = Int3.zero();
    Int3 end = dimensions;
    var size = end - start;
    int sliceSize = size[0] * size[2];
    int numJobs = size[1];
    var encodeJobs = new EncodeJob[numJobs];
    var jobHandles = new JobHandle[numJobs];

    for (int i = 0, index = start[1] * TilesPerSlice; i < numJobs; ++i, index += TilesPerSlice)
    {
      encodeJobs[i] = new EncodeJob
      {
        num_spans = size[2],
        start_x = start[0],
        start_z = start[2],
        dimensions_x = dimensions[0],
        tile_directions = tile_directions.Slice(index, TilesPerSlice),
        tile_shapes = tile_shapes.Slice(index, TilesPerSlice),
        tile_styles = tile_styles.Slice(index, TilesPerSlice),
        span = spans[i].Slice(0, sliceSize),
        span_lengths = span_lengths[i].Slice(0, size[2])
      };
    }

    Util.Log($"Kicking off {numJobs} encode jobs");

    // Execute jobs and yield for them
    for (int i = 0; i < numJobs; ++i)
    {
      jobHandles[i] = encodeJobs[i].Schedule();
    }

    encodeHandles.Add(jobHandles);

    yield return new WaitWhile(() =>
    {
      for (int i = 0; i < numJobs; ++i)
      {
        if (!jobHandles[i].IsCompleted)
        {
          return true;
        }
      }
      return false;
    });

    // Not sure why I need to do this again..but OK
    for (int i = 0; i < numJobs; ++i)
    {
      jobHandles[i].Complete();
    }
    encodeHandles.Remove(jobHandles);

    // In case other jobs were also kicked off while we were waiting for ours..
    CompleteTileDataJobs();

    using (MemoryStream memStream = new MemoryStream())
    {
      using (DeflateStream dstream = new DeflateStream(memStream, System.IO.Compression.CompressionLevel.Fastest, true))
      {
        using (BinaryWriter writer = new BinaryWriter(dstream))
        {
          writer.Write(serialization_version);
          writer.Write(size[0]);
          writer.Write(size[1]);
          writer.Write(size[2]);
          for (int y = 0; y < size[1]; ++y)
          {
            for (int z = 0; z < size[2]; ++z)
            {
              int num_segments = span_lengths[y][z];
              writer.Write(num_segments);
              for (int x = 0; x < num_segments; ++x)
              {
                writer.Write(spans[y][z * size[0] + x]);
              }
            }
          }
        }
      }
      byte[] bytes = memStream.ToArray();
      Debug.Log($"Serialized to: {bytes.Length} bytes");
      process(bytes);
    }
  }

  public void Deserialize(byte[] data)
  {
    Deserialize(data, Int3.zero());
  }

  public void Deserialize(byte[] data, Int3 start)
  {
    CompleteTileDataJobs();

    Int3 size = Int3.zero();
    using (MemoryStream memStream = new MemoryStream(data))
    {
      using (MemoryStream decompressed_stream = new MemoryStream())
      {
        using (DeflateStream dstream = new DeflateStream(memStream, System.IO.Compression.CompressionMode.Decompress, true))
        {
          dstream.CopyTo(decompressed_stream);
        }
        Debug.Log($"Decompressed to: {decompressed_stream.Length}");
        decompressed_stream.Seek(0, SeekOrigin.Begin);
        using (BinaryReader reader = new BinaryReader(decompressed_stream))
        {
          int version = reader.ReadInt32();
          size[0] = reader.ReadInt32();
          size[1] = reader.ReadInt32();
          size[2] = reader.ReadInt32();
          for (int y = 0; y < size[1]; ++y)
          {
            for (int z = 0; z < size[2]; ++z)
            {
              int num_segments = reader.ReadInt32();
              //Debug.Assert(num_segments == span_lengths[y][z]);
              span_lengths[y][z] = num_segments;
              for (int x = 0; x < num_segments; ++x)
              {
                var val = reader.ReadInt32();
                //Debug.Assert(spans[y][z*dimensions[0]+x]==val);
                spans[y][z * size[0] + x] = val;
              }
            }
          }
        }
      }
    }

    int num_span_jobs = size[1];
    var job_handles = new JobHandle[num_span_jobs];

    var tile_shapes_slice = new NativeArray<int>[size[1]];
    var tile_styles_slice = new NativeArray<int>[size[1]];
    var tile_directions_slice = new NativeArray<int>[size[1]];

    int size_slice = size[0] * size[2];

    for (int i = 0; i < size[1]; ++i)
    {
      tile_shapes_slice[i] = new NativeArray<int>(size_slice, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
      tile_styles_slice[i] = new NativeArray<int>(size_slice, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
      tile_directions_slice[i] = new NativeArray<int>(size_slice, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
    }

    var from_span_jobs = new DecodeJob[num_span_jobs];
    for (int i = 0; i < num_span_jobs; ++i)
    {
      ref var span_job = ref from_span_jobs[i];
      span_job.num_spans = size[2];
      span_job.tile_directions = tile_directions_slice[i].Slice(0, size_slice);
      span_job.tile_shapes = tile_shapes_slice[i].Slice(0, size_slice);
      span_job.tile_styles = tile_styles_slice[i].Slice(0, size_slice);
      span_job.span = spans[i].Slice(0, size_slice);
      span_job.span_lengths = span_lengths[i].Slice(0, size[2]);
    }

    for (int i = 0; i < num_span_jobs; ++i)
    {
      job_handles[i] = from_span_jobs[i].Schedule();
    }
    for (int i = 0; i < num_span_jobs; ++i)
    {
      job_handles[i].Complete();
    }

    if (size == dimensions)
    {
      for (int i = 0; i < dimensions[1]; ++i)
      {
        tile_shapes.Slice(i * TilesPerSlice, TilesPerSlice).CopyFrom(tile_shapes_slice[i]);
        tile_styles.Slice(i * TilesPerSlice, TilesPerSlice).CopyFrom(tile_styles_slice[i]);
        tile_directions.Slice(i * TilesPerSlice, TilesPerSlice).CopyFrom(tile_directions_slice[i]);
      }
    }
    else
    {
      for (int y = 0; y < size[1]; ++y)
      {
        for (int z = 0; z < size[2]; ++z)
        {
          int dst = (y + start[1]) * TilesPerSlice + (z + start[2]) * dimensions[0] + start[0];
          int src = z * size[0];
          tile_shapes.Slice(dst, size[0]).CopyFrom(tile_shapes_slice[y].Slice(src, size[0]));
          tile_styles.Slice(dst, size[0]).CopyFrom(tile_styles_slice[y].Slice(src, size[0]));
          tile_directions.Slice(dst, size[0]).CopyFrom(tile_directions_slice[y].Slice(src, size[0]));
        }
      }
    }

    for (int i = 0; i < size[1]; ++i)
    {
      tile_shapes_slice[i].Dispose();
      tile_styles_slice[i].Dispose();
      tile_directions_slice[i].Dispose();
    }

    Int3 dirty_min = GetChunkCoord(start);
    Int3 dirty_max = GetChunkCoord(start + size - Int3.one());
    for (int x = dirty_min[0]; x <= dirty_max[0]; ++x)
    {
      for (int y = dirty_min[1]; y <= dirty_max[1]; ++y)
      {
        for (int z = dirty_min[2]; z <= dirty_max[2]; ++z)
        {
          var chunk_coord = new Int3(x, y, z);
          dirty_chunks.SetIfMissing(chunk_coord, 0f);
          bitmask_dirty_chunks.Add(chunk_coord);
        }
      }
    }

    sky_dirty_min = new Int3(0, 0, 0);
    sky_dirty_max = dimensions;

  }

  public struct SkyLightJob : IJob
  {
    [ReadOnly] public Int3 dimensions, p_min, p_max;
    [ReadOnly] public int max_spread;
    [ReadOnly] public NativeArray<int> tile_shapes;
    public NativeArray<int> sky_light_dist;
    public NativeArray<float> color;
    public NativeArray<Int3> write_bounds;

    public void Execute()
    {
      int dimension_slice = dimensions[0] * dimensions[2];
      var sky_queue = new Queue<Int3>();

      // Extend line segment to next blocking tile below, then expand by maximum fill distance to get possibly modified tiles

      // Start at tile above max tile so that corners and ramps can correctly shadow downwards
      Int3 max = p_max;
      max[1] = Mathf.Min(max[1] + 1, dimensions[1]);
      Int3 min = p_min;

      // Trace sky light downwards to see how far down it modifies
      for (int z = min[2]; z < max[2]; ++z)
      {
        int base_index = GenericIndex(new Int3(min[0], max[1] - 1, z), dimensions);
        for (int x = min[0]; x < max[0]; ++x, ++base_index)
        {
          int index = base_index;
          bool sky_visible = (sky_light_dist[index] == 0);
          if (max[1] == dimensions[1])
          {
            sky_visible = true;
          }
          for (int y = max[1] - 1; y >= 0; --y, index -= dimension_slice)
          {
            var tile_shape = tile_shapes[index];
            if (tile_shape == 0)
            {
              break;
            }
            var tile_sky_light = sky_light_dist[index];
            if (tile_sky_light != 0 && !sky_visible)
            {
              // We've reached tiles that are already in shadow, and we're not lighting them
              break;
            }
            else if ((tile_sky_light == 0 && !sky_visible) ||
                    (tile_sky_light != 0 && sky_visible))
            {
              // We're adding or removing direct light, so stretch bounds
              min[1] = Mathf.Min(min[1], y);
            }
            if (tile_shape == 2 || tile_shape == 3)
            { // Ramp and corner blocks light on tile below
              sky_visible = false;
            }
          }
        }
      }

      Int3 write_min = min - Int3.one() * max_spread;
      Int3 write_max = max + Int3.one() * max_spread;
      Int3 read_min = min - Int3.one() * max_spread * 2;
      Int3 read_max = max + Int3.one() * max_spread * 2;

      for (int i = 0; i < 3; ++i)
      {
        write_min[i] = Mathf.Max(write_min[i], 0);
        read_min[i] = Mathf.Max(read_min[i], 0);
        write_max[i] = Mathf.Min(write_max[i], dimensions[i]);
        read_max[i] = Mathf.Min(read_max[i], dimensions[i]);
      }

      Int3 read_size = read_max - read_min;
      int read_slice = read_size[0] * read_size[2];
      var temp_sky_dist = new int[read_size[0] * read_size[1] * read_size[2]];

      // Add skylight to tiles that have no cubes above them
      for (int z = read_min[2]; z < read_max[2]; ++z)
      {
        int base_index = GenericIndex(new Int3(read_min[0], read_max[1] - 1, z), dimensions);
        for (int x = read_min[0]; x < read_max[0]; ++x, ++base_index)
        {
          var index = base_index;
          bool sky_visible = (sky_light_dist[index] == 0);
          if (read_max[1] == dimensions[1])
          {
            sky_visible = true;
          }
          for (int y = read_max[1] - 1; y >= read_min[1]; --y, index -= dimension_slice)
          {
            var tile_shape = tile_shapes[index];
            if (tile_shape == 0)
            {
              sky_visible = false;
            }
            if (sky_visible)
            {
              sky_queue.Enqueue(new Int3(x, y, z));
            }
            int new_dist = sky_visible ? 0 : -1;
            if (sky_light_dist[index] != new_dist)
            {
              min[1] = Mathf.Min(min[1], y);
            }
            Int3 coord = new Int3(x, y, z) - read_min;
            temp_sky_dist[GenericIndex(coord, read_size)] = new_dist;
            if (tile_shape == 2 || tile_shape == 3)
            { // Ramp and corner blocks light on tile below
              sky_visible = false;
            }
          }
        }
      }

      Int3 read_max_minus_one = read_max - Int3.one();

      // Flood fill sky light
      while (sky_queue.Count > 0)
      {
        var coord = sky_queue.Dequeue();
        Int3 temp_coord = coord - read_min;
        var temp_index = GenericIndex(temp_coord, read_size);
        var tile_index = GenericIndex(coord, dimensions);
        int new_light_dist = temp_sky_dist[temp_index] + 1;
        if (coord[0] < read_max_minus_one[0] && tile_shapes[tile_index + 1] != 0)
        {
          int index = temp_index + 1;
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0] + 1, coord[1], coord[2]));
            }
          }
        }
        if (coord[0] > read_min[0] && tile_shapes[tile_index - 1] != 0)
        {
          int index = temp_index - 1;
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0] - 1, coord[1], coord[2]));
            }
          }
        }
        if (coord[1] < read_max_minus_one[1] && tile_shapes[tile_index + dimension_slice] != 0)
        {
          int index = temp_index + read_slice;
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0], coord[1] + 1, coord[2]));
            }
          }
        }
        if (coord[1] > read_min[1] && tile_shapes[tile_index - dimension_slice] != 0)
        {
          int index = temp_index - read_slice;
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0], coord[1] - 1, coord[2]));
            }
          }
        }
        if (coord[2] < read_max_minus_one[2] && tile_shapes[tile_index + dimensions[0]] != 0)
        {
          int index = temp_index + read_size[0];
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0], coord[1], coord[2] + 1));
            }
          }
        }
        if (coord[2] > read_min[2] && tile_shapes[tile_index - dimensions[0]] != 0)
        {
          int index = temp_index - read_size[0];
          if (temp_sky_dist[index] == -1)
          {
            temp_sky_dist[index] = new_light_dist;
            if (new_light_dist <= max_spread)
            {
              sky_queue.Enqueue(new Int3(coord[0], coord[1], coord[2] - 1));
            }
          }
        }
      }

      // Assign color based on sky light
      for (int y = write_min[1]; y < write_max[1]; ++y)
      {
        for (int z = write_min[2]; z < write_max[2]; ++z)
        {
          var index = GenericIndex(new Int3(write_min[0], y, z), dimensions);
          var temp_index = GenericIndex(new Int3(write_min[0], y, z) - read_min, read_size);
          for (int x = write_min[0]; x < write_max[0]; ++x, ++index, ++temp_index)
          {
            sky_light_dist[index] = temp_sky_dist[temp_index];
          }
        }
      }

      var light_lookup = new float[max_spread + 2];
      for (int i = 0; i < light_lookup.Length; ++i)
      {
        light_lookup[i] = Mathf.Pow(Mathf.Max(0.0f, 1.0f - i / (float)(max_spread + 1)), 1.5f);
      }

      // Assign color based on sky light
      for (int y = write_min[1]; y < write_max[1]; ++y)
      {
        for (int z = write_min[2]; z < write_max[2]; ++z)
        {
          var index = GenericIndex(new Int3(write_min[0], y, z), dimensions);
          for (int x = write_min[0]; x < write_max[0]; ++x, ++index)
          {
            if (sky_light_dist[index] != -1)
            {
              var light_amount = light_lookup[Mathf.Min(sky_light_dist[index], max_spread + 1)];//Mathf.Pow(0.9f, level[x,y,z))].sky_light_dist);
              color[index] = light_amount;
            }
            else
            {
              color[index] = 0.0f;
            }
          }
        }
      }
      write_bounds[0] = write_min;
      write_bounds[1] = write_max;
    }
  }

  //JobHandle sky_job_handle;
  Thread sky_job_thread;
  bool sky_job_running = false;
  SkyLightJob sky_job;

  void CalculateSkyLight(Int3 p_min, Int3 p_max)
  {
    sky_job.dimensions = dimensions;
    sky_job.p_min = p_min;
    sky_job.p_max = p_max;
    sky_job.max_spread = max_spread;

    // Only copy over tiles that have changed
    int size = p_max[0] - p_min[0];
    for (int y = p_min[1], y_base = p_min[1] * TilesPerSlice; y < p_max[1]; ++y, y_base += TilesPerSlice)
    {
      for (int z = p_min[2], z_base = y_base + p_min[2] * dimensions[0]; z < p_max[2]; ++z, z_base += dimensions[0])
      {
        var index = z_base + p_min[0];
        sky_job.tile_shapes.Slice(index, size).CopyFrom(tile_shapes.Slice(index, size));
      }
    }

    sky_job_thread = new Thread(new ThreadStart(sky_job.Execute));
    sky_job_thread.Start();
    //sky_job_handle = sky_job.Schedule();
    sky_job_running = true;
  }


  static string FromBinary(uint val)
  {
    return Regex.Replace(Convert.ToString(val, 2).PadLeft(32, '0'), ".{4}", "$0 ");
  }

  static string FromBinary(byte val)
  {
    return Regex.Replace(Convert.ToString(val, 2).PadLeft(8, '0'), ".{4}", "$0 ");
  }

  void AnalyzeBlocks()
  {
    // Get bounds of basic block
    var temp = Instantiate(blocks[0]);
    cell_size = temp.GetComponent<Collider>().bounds.extents * 2f;
    inv_cell_size = cell_size;
    for (int i = 0; i < 3; ++i)
    {
      inv_cell_size[i] = 1.0f / cell_size[i];
    }
    normal_offset_amount = 0.001f;
    droop_amount = cell_size[1] * 0.5f;
    Destroy(temp);

    // Create slope bases
    for (int i = 0; i < 8; ++i)
    {
      Vector3 normal, up, right;
      if (i < 4)
      {
        normal = Quaternion.AngleAxis(i * 90f, Vector3.up) * (Vector3.right + Vector3.forward);
        up = Vector3.up;
        right = Quaternion.AngleAxis(i * 90f, Vector3.up) * (Vector3.right - Vector3.forward);
      }
      else
      {
        normal = Quaternion.AngleAxis(i * 90f, Vector3.up) * (Vector3.right);
        up = Quaternion.AngleAxis(i * 90f, Vector3.up) * (-Vector3.right + Vector3.up);
        right = Quaternion.AngleAxis(i * 90f, Vector3.up) * (-Vector3.forward);
      }
      slope_vec[i * 3 + 0] = normal;
      slope_vec[i * 3 + 1] = right;
      slope_vec[i * 3 + 2] = up;
    }

    // Analyze all block types
    types = new TileType[blocks.Length];
    for (int i = 0; i < blocks.Length; ++i)
    {
      types[i] = new TileType();
      types[i].prefab = blocks[i];
      AnalyzeBlock(types[i]);
    }
  }

  public void CreateTextureArrays(bool force = false, int size = tex_size)
  {
    int num_array_textures = 0;
    int num_border_textures = 0;
    foreach (var entry in styles)
    {
      var style = entry.Value;
      if (style.base_tex != null)
      {
        style.base_tex_index = num_array_textures;
        ++num_array_textures;
      }
      if (style.side_tex != null)
      {
        style.side_tex_index = num_array_textures;
        ++num_array_textures;
      }
      if (style.border_tex != null)
      {
        style.border_tex_index = num_border_textures;
        ++num_border_textures;
      }
    }

    // Create texture arrays
    if (tex_array == null || num_array_textures != tex_array.depth || force)
    {
      tex_array = new Texture2DArray(size, size, Mathf.Max(1, num_array_textures), TextureFormat.RGBA32, UseMips);
      tex_array.filterMode = FilterMode.Point;
      Shader.SetGlobalTexture("_tex_array", tex_array);
    }

    if (border_tex_array == null || num_border_textures != border_tex_array.depth)
    {
      border_tex_array = new Texture2DArray(border_tex_w, border_tex_h, Mathf.Max(1, num_border_textures), TextureFormat.RGBA32, UseMips);
      border_tex_array.filterMode = FilterMode.Point;
      border_tex_array.wrapModeV = TextureWrapMode.Clamp;
      Shader.SetGlobalTexture("_border_tex_array", border_tex_array);
    }
    // Fill pixels

    using (Util.Profile("Set texarray pixels"))
    {
      foreach (var entry in styles)
      {
        var style = entry.Value;
        if (style.base_tex != null)
        {
          tex_array.SetPixels32(style.base_tex, style.base_tex_index);
          style.base_tex_dirty = false;
        }
        if (style.side_tex != null)
        {
          tex_array.SetPixels32(style.side_tex, style.side_tex_index);
          style.side_tex_dirty = false;
        }
        if (style.border_tex != null)
        {
          border_tex_array.SetPixels32(style.border_tex, style.border_tex_index);
          style.border_tex_dirty = false;
        }
      }
    }
    tex_array.Apply();
    border_tex_array.Apply();

    tex_array_dirty = false;
  }


  float normal_offset_amount;
  float droop_amount;

  Vector3[] add_overflow_verts = new Vector3[4];

  void AddOverflow(Vector3 a, Vector3 b, Vector3 n, bool flip, bool down, int border_tex_index, EditableMesh editable_mesh, int sides = 2)
  {
    Vector3 normal_offset = normal_offset_amount * n;
    var verts = add_overflow_verts;
    float u_0 = 0f;
    float u_1 = 1f;
    verts[0] = a;
    verts[1] = b;
    switch (sides)
    {
      case 0:
        verts[1] = (a + b) * 0.5f;
        u_1 = 0.5f;
        break;
      case 1:
        verts[0] = (a + b) * 0.5f;
        u_0 = 0.5f;
        break;
    }
    verts[2] = verts[1] + Vector3.up * droop_amount * (down ? -1f : 1f) + normal_offset;
    verts[3] = verts[0] + Vector3.up * droop_amount * (down ? -1f : 1f) + normal_offset;

    AddTri(verts[0], verts[1], verts[2],
            new Vector2(u_0, 1), new Vector2(u_1, 1), new Vector2(u_1, 0),
            Vector3.up,
            flip,
            editable_mesh);
    AddTri(verts[2], verts[3], verts[0],
            new Vector2(u_1, 0), new Vector2(u_0, 0), new Vector2(u_0, 1),
            Vector3.up,
            flip,
            editable_mesh);

    var color = Color.green;
    color.b = (border_tex_index + 0.5f) / 255f;
    for (int j = editable_mesh.color_list.Count - 6; j < editable_mesh.color_list.Count; ++j)
    {
      color.r = GetSmoothLightAmount(editable_mesh.vert_list[j] + Vector3.up * cell_size[1] * 0.35f);
      editable_mesh.color_list[j] = color;
    }
  }

  bool CheckEmpty(Int3 chunk_coords)
  {
    var chunk = chunks[chunk_coords[0], chunk_coords[1], chunk_coords[2]];
    Int3 start = new Int3(chunk_coords[0] * ChunkDims[0],
                          chunk_coords[1] * ChunkDims[1],
                          chunk_coords[2] * ChunkDims[2]);
    Int3 end = start + ChunkDims;
    end[0] = Mathf.Min(end[0], dimensions[0]);
    end[1] = Mathf.Min(end[1], dimensions[1]);
    end[2] = Mathf.Min(end[2], dimensions[2]);
    for (int y = start[1], y_index = TileIndex(new Int3(start[0], start[1], start[2])); y < end[1]; ++y, y_index += TilesPerSlice)
    {
      for (int z = start[2], z_index = y_index; z < end[2]; ++z, z_index += dimensions[0])
      {
        for (int x = start[0], index = z_index; x < end[0]; ++x, ++index)
        {
          if (tile_shapes[index] != -1)
          {
            chunk.obj.SetActive(true);
            return false;
          }
        }
      }
    }
    chunk.obj.SetActive(false);
    return true;
  }

  void UpdateBitmask(Int3 chunk_coords, NativeTileData td)
  {
    var tile_shapes = td.tile_shapes;
    var tile_styles = td.tile_styles;
    var tile_directions = td.tile_directions;

    var chunk = chunks[chunk_coords[0], chunk_coords[1], chunk_coords[2]];
    Int3 start = new Int3(chunk_coords[0] * ChunkDims[0],
                          chunk_coords[1] * ChunkDims[1],
                          chunk_coords[2] * ChunkDims[2]);
    Int3 end = start + ChunkDims;
    end[0] = Mathf.Min(end[0], dimensions[0]);
    end[1] = Mathf.Min(end[1], dimensions[1]);
    end[2] = Mathf.Min(end[2], dimensions[2]);
    for (int y = start[1], y_index = TileIndex(new Int3(start[0], start[1], start[2])); y < end[1]; ++y, y_index += TilesPerSlice)
    {
      for (int z = start[2], z_index = y_index; z < end[2]; ++z, z_index += dimensions[0])
      {
        for (int x = start[0], index = z_index; x < end[0]; ++x, ++index)
        {
          if (tile_shapes[index] != -1)
          {
            // Recreate tile from border/slope information
            uint borders = types[tile_shapes[index]].borders[tile_directions[index]];
            int compare_tile_type;
            // Make neighboring bitmasks to remove hidden faces
            for (int face = 0; face < 6; ++face)
            {
              int compare_tile_index = -1;
              switch (face)
              {
                case 0: compare_tile_index = x == 0 ? -1 : index - 1; break;
                case 1: compare_tile_index = (x == dimensions[0] - 1) ? -1 : index + 1; break;
                case 2: compare_tile_index = y == 0 ? -1 : index - TilesPerSlice; break;
                case 3: compare_tile_index = (y == dimensions[1] - 1) ? -1 : index + TilesPerSlice; break;
                case 4: compare_tile_index = z == 0 ? -1 : index - dimensions[0]; break;
                case 5: compare_tile_index = (z == dimensions[2] - 1) ? -1 : index + dimensions[0]; break;
                default: compare_tile_index = -1; break;
              }
              if (compare_tile_index != -1)
              {
                compare_tile_type = tile_shapes[compare_tile_index];
              }
              else
              {
                compare_tile_type = -1;
              }
              if (compare_tile_type != -1)
              {
                uint offset_mask = 0b1111U << (4 * face);
                int dir = tile_directions[compare_tile_index];
                var type = types[compare_tile_type];
                uint compare_borders = type.borders[dir];
                uint neg_y = borders & ~((face % 2 == 0) ? (compare_borders >> 4) : (compare_borders << 4));
                uint neg_y_masked = neg_y & offset_mask;
                borders &= ~offset_mask;
                borders |= neg_y_masked;
              }
            }

            int bitmask_index = index * 6;
            for (int i = 0; i < 6; ++i)
            {
              bitmasks[bitmask_index + i] = (byte)((borders >> (4 * i)) & 0b1111U);
            }
          }
          else
          {
            int bitmask_index = index * 6;
            for (int i = 0; i < 6; ++i)
            {
              bitmasks[bitmask_index + i] = 0;
            }
          }
        }
      }
    }
  }

  readonly int[] zero_length_int_array = new int[0];

  // dir == 0 -> side == 2
  readonly int[] top_half = new int[] {
                                            1,2,
                                            2,3,
                                            3,0,
                                            0,1,
                                        };
  readonly int[] wall_sides = new int[] { 0, 1, 4, 5 }; // Check -x, +x, -z, +z
  readonly int[] ceil_offset = new int[] {
                                -1, +1,  0,
                                +1, +1,  0,
                                 0, +1, -1,
                                 0, +1, +1
                            };
  readonly int[] ceil_tris = new int[] {
                                0, 2, 3, 1
                            };
  readonly int[] ramp_side = new int[] {
                                6, 4, 5, 7
                            };
  readonly int[] ramp2_side = new int[] {
                                4, 6, 7, 5
                            };
  readonly int[] floor_tris = new int[] {
                                2, 0, 1, 3
                            };

  // dir == 0 -> side == 2
  // flat tri: +x, -z, -x, +z
  readonly int[] top_half_2 = new int[] {
                                            0,3,
                                            1,0,
                                            2,1,
                                            3,2,
                                        };

  bool[] filled_tris = new bool[4];
  Vector3[] corners = new Vector3[4];
  Vector2[] corner_uv = new Vector2[4];

  struct NativeTileData
  {
    public NativeArray<int> tile_shapes;
    public NativeArray<int> tile_styles;
    public NativeArray<int> tile_directions;
  }

  NativeTileData GetTileData()
  {
    return new NativeTileData { tile_directions = tile_directions, tile_styles = tile_styles, tile_shapes = tile_shapes };
  }

  struct MeshJob : IJob
  {
    // See hack here: https://forum.unity.com/threads/solved-c-job-system-vs-managed-threaded-code.545360/
    public GCHandle terrainPtr;

    public Int3 chunkId;

    [ReadOnly] public NativeTileData tileData;

    public void Execute()
    {
      TerrainSystem terrain = (TerrainSystem)terrainPtr.Target;
      terrain.CreateChunkMesh(chunkId, tileData);
    }
  }

  struct LightJob : IJob
  {
    // See hack here: https://forum.unity.com/threads/solved-c-job-system-vs-managed-threaded-code.545360/
    public GCHandle terrainPtr;

    public Int3 chunkId;

    public void Execute()
    {
      TerrainSystem terrain = (TerrainSystem)terrainPtr.Target;
      terrain.UpdateChunkMeshColors(chunkId);
    }
  }

  void CreateChunkMesh(Int3 chunk_coords, NativeTileData td)
  {
    var tile_shapes = td.tile_shapes;
    var tile_styles = td.tile_styles;
    var tile_directions = td.tile_directions;

    // Make sure all neighboring bitmasks are up to date for proper edge decorations
    Int3 bitmask_coord = Int3.zero();
    for (bitmask_coord[0] = chunk_coords[0] - 1; bitmask_coord[0] <= chunk_coords[0] + 1; ++bitmask_coord[0])
    {
      for (bitmask_coord[1] = chunk_coords[1] - 1; bitmask_coord[1] <= chunk_coords[1] + 1; ++bitmask_coord[1])
      {
        for (bitmask_coord[2] = chunk_coords[2] - 1; bitmask_coord[2] <= chunk_coords[2] + 1; ++bitmask_coord[2])
        {
          if (bitmask_dirty_chunks.Contains(bitmask_coord))
          {
            UpdateBitmask(bitmask_coord, td);
            bitmask_dirty_chunks.Remove(bitmask_coord);
          }
        }
      }
    }

    var chunk = chunks[chunk_coords[0], chunk_coords[1], chunk_coords[2]];
    Int3 start = new Int3(chunk_coords[0] * ChunkDims[0],
                          chunk_coords[1] * ChunkDims[1],
                          chunk_coords[2] * ChunkDims[2]);
    Int3 end = start + ChunkDims;
    end[0] = Mathf.Min(end[0], dimensions[0]);
    end[1] = Mathf.Min(end[1], dimensions[1]);
    end[2] = Mathf.Min(end[2], dimensions[2]);

    // Create mesh
    chunk.editable_mesh.Clear();
    chunk.physics_mesh.Clear();
    var editable_mesh = chunk.editable_mesh;
    using (Util.Profile("Create triangles"))
    {
      for (int y = start[1], y_index = TileIndex(new Int3(start[0], start[1], start[2])); y < end[1]; ++y, y_index += TilesPerSlice)
      {
        for (int z = start[2], z_index = y_index; z < end[2]; ++z, z_index += dimensions[0])
        {
          for (int x = start[0], index = z_index; x < end[0]; ++x, ++index)
          {
            if (tile_shapes[index] != -1)
            {
              int color_start = editable_mesh.color_list.Count;
              int tri_start = editable_mesh.tri_list.Count;
              int bitmask_index = index * 6;
              var type = types[tile_shapes[index]];
              Vector3 offset = GridPos((x, y, z), cell_size);

              using (Util.Profile("compute filled tris"))
              {
                uint borders = 0;
                for (int i = 0; i < 6; ++i)
                {
                  borders |= (uint)bitmasks[bitmask_index + i] << (4 * i);
                }
                var slopes = type.slopes[tile_directions[index]];

                Array.Clear(filled_tris, 0, filled_tris.Length);

                // Iterate through each face
                for (int i = 0, bit_index = 0; i < 18; i += 3)
                {
                  bool topOrBottom = i == 9 || i == 6;

                  if ((borders & (0b1111 << bit_index)) == 0)
                  {
                    bit_index += 4;
                    continue;
                  }

                  // Get the corner UVs for the given rotation.
                  Vector2 uv_min = type.border_uvs[tile_directions[index] * 12 + ((i / 3)) * 2 + 0];
                  Vector2 uv_max = type.border_uvs[tile_directions[index] * 12 + ((i / 3)) * 2 + 1];

                  // For top/bottom, actually rotate the original UVs - don't
                  // just do the flip thing (though that's still needed for
                  // other faces)
                  if (topOrBottom)
                  {
                    uv_min = type.border_uvs[0 * 12 + ((i / 3)) * 2 + 0];
                    uv_max = type.border_uvs[0 * 12 + ((i / 3)) * 2 + 1];
                  }

                  Vector3 center = Vector3.Scale(cell_size * 0.5f, cube_face_vec[i]) + offset;
                  var mid_uv = (uv_min + uv_max) * 0.5f;
                  for (int j = 0; j < 4; ++j)
                  {
                    corners[j] = center +
                        Vector3.Scale(cell_size * 0.5f, cube_face_vec[i + 1]) * ((j == 0 || j == 1) ? 1f : -1f) +
                        Vector3.Scale(cell_size * 0.5f, cube_face_vec[i + 2]) * ((j == 1 || j == 2) ? 1f : -1f);

                    int selector = j;
                    if (topOrBottom)
                    {
                      // Permute the corners to effectively rotate them.
                      selector = (j + (4 - tile_directions[index])) % 4;
                    }
                    corner_uv[j] = new Vector2((selector == 0 || selector == 1) ? uv_max[0] : uv_min[0], (selector == 1 || selector == 2) ? uv_max[1] : uv_min[1]);
                  }

                  // Iterate through each triangle
                  for (int j = 0; j < 4; ++j)
                  {
                    filled_tris[j] = (borders & (1 << bit_index)) != 0;
                    ++bit_index;
                  }

                  // Merge faces if possible
                  for (int j = 0; j < 4; ++j)
                  {
                    if (filled_tris[j])
                    {
                      if (filled_tris[(j + 1) % 4])
                      {
                        AddTri(corners[j], corners[(j + 1) % 4], corners[(j + 2) % 4], corner_uv[j], corner_uv[(j + 1) % 4], corner_uv[(j + 2) % 4], cube_face_vec[i], (i / 3) % 2 != 0, editable_mesh);
                        filled_tris[(j + 1) % 4] = false;
                      }
                      else if (filled_tris[(j + 3) % 4])
                      {
                        AddTri(corners[j], corners[(j + 1) % 4], corners[(j + 3) % 4], corner_uv[j], corner_uv[(j + 1) % 4], corner_uv[(j + 3) % 4], cube_face_vec[i], (i / 3) % 2 != 0, editable_mesh);
                        filled_tris[(j + 3) % 4] = false;
                      }
                      else
                      {
                        AddTri(center, corners[j], corners[(j + 1) % 4], mid_uv, corner_uv[j], corner_uv[(j + 1) % 4], cube_face_vec[i], (i / 3) % 2 != 0, editable_mesh);
                      }
                      filled_tris[j] = false;
                    }
                  }
                }

                // Iterate through each slope
                for (int i = 0, bit_index = 0; i < 8; ++i)
                {
                  if ((slopes & (0b1111 << bit_index)) == 0)
                  {
                    bit_index += 4;
                    continue;
                  }
                  Vector3 center = offset;
                  var uv_min = type.slope_uvs[tile_directions[index] * 16 + i * 2 + 0];
                  var uv_max = type.slope_uvs[tile_directions[index] * 16 + i * 2 + 1];
                  var mid_uv = (uv_min + uv_max) * 0.5f;
                  for (int j = 0; j < 4; ++j)
                  {
                    corners[j] = center +
                        Vector3.Scale(cell_size * 0.5f, slope_vec[i * 3 + 1]) * ((j == 0 || j == 1) ? 1f : -1f) +
                        Vector3.Scale(cell_size * 0.5f, slope_vec[i * 3 + 2]) * ((j == 1 || j == 2) ? 1f : -1f);
                    corner_uv[j] = new Vector2((j == 0 || j == 1) ? uv_max[0] : uv_min[0], (j == 1 || j == 2) ? uv_max[1] : uv_min[1]);
                  }
                  for (int j = 0; j < 4; ++j)
                  {
                    filled_tris[j] = (slopes & (1 << bit_index)) != 0;
                    ++bit_index;
                  }
                  Vector3 normal = Vector3.Normalize(Vector3.Cross(corners[0] - center, corners[1] - center));

                  // Merge faces if possible
                  for (int j = 0; j < 4; ++j)
                  {
                    if (filled_tris[j])
                    {
                      if (filled_tris[(j + 1) % 4])
                      {
                        AddTri(corners[j], corners[(j + 1) % 4], corners[(j + 2) % 4], corner_uv[j], corner_uv[(j + 1) % 4], corner_uv[(j + 2) % 4], normal, true, editable_mesh);
                        filled_tris[(j + 1) % 4] = false;
                      }
                      else if (filled_tris[(j + 3) % 4])
                      {
                        AddTri(corners[j], corners[(j + 1) % 4], corners[(j + 3) % 4], corner_uv[j], corner_uv[(j + 1) % 4], corner_uv[(j + 3) % 4], normal, true, editable_mesh);
                        filled_tris[(j + 3) % 4] = false;
                      }
                      else
                      {
                        AddTri(center, corners[j], corners[(j + 1) % 4], mid_uv, corner_uv[j], corner_uv[(j + 1) % 4], normal, true, editable_mesh);
                      }
                      filled_tris[j] = false;
                    }
                  }
                }
              }
              int color_end = editable_mesh.color_list.Count;
              int tri_end = editable_mesh.tri_list.Count;

              using (Util.Profile("physics mesh"))
              {
                int phys_vert_start = chunk.physics_mesh.vert_list.Count;
                for (int i = color_start; i < color_end; ++i)
                {
                  chunk.physics_mesh.vert_list.Add(editable_mesh.vert_list[i]);
                }
                for (int i = tri_start; i < tri_end; ++i)
                {
                  chunk.physics_mesh.tri_list.Add(editable_mesh.tri_list[i] - color_start + phys_vert_start);
                }
              }

              using (Util.Profile("lighting"))
              {
                float style_color = (styles[tile_styles[index]].base_tex_index + 0.5f) / 255f;
                if (styles[tile_styles[index]].side_tex != null)
                {
                  for (int i = color_start; i < color_end; ++i)
                  {
                    var uv = editable_mesh.uv_list[i];
                    var world_normal = editable_mesh.normal_list[i];
                    var coord_scaled = Vector3.Scale(editable_mesh.vert_list[i], inv_cell_size) / 4.0f;


                    uv = Mathf.Abs(world_normal.y) > 0.1 ? new Vector2(coord_scaled.x, coord_scaled.z) :
                         Mathf.Abs(world_normal.x) < 0.3 ? new Vector2(coord_scaled.x, coord_scaled.y) :
                                                           new Vector2(coord_scaled.z, coord_scaled.y);

                    if (Mathf.Abs(Mathf.Abs(world_normal.x) - 0.7f) < 0.1f)
                    {
                      uv[0] *= 1.4f;
                    }
                    editable_mesh.uv_list[i] = uv;
                  }
                }
                for (int i = color_start; i < color_end; ++i)
                {
                  if (styles[tile_styles[index]].side_tex != null)
                  {
                    var world_normal = editable_mesh.normal_list[i];
                    if (world_normal.y <= 0.1)
                    {
                      style_color = (styles[tile_styles[index]].side_tex_index + 0.5f) / 255f;
                    }
                    else
                    {
                      style_color = (styles[tile_styles[index]].base_tex_index + 0.5f) / 255f;
                    }
                  }

                  var color = editable_mesh.color_list[i];
                  // Offset vertically slightly to eliminate "flat darkening" issue

                  // This alone is ~2ms (of 4)
                  using (Util.Profile("GetSmoothLightAmount"))
                    color.r = GetSmoothLightAmount(editable_mesh.vert_list[i] + Vector3.up * cell_size[1] * 0.35f);
                  //to debug cell lighting (not smooth)
                  //color.r = GetSmoothLightAmount(GridPos((x, y, z), cell_size) + Vector3.Scale(editable_mesh.normal_list[i], cell_size));
                  color.g = 0f;
                  color.b = style_color;
                  editable_mesh.color_list[i] = color;
                }
              }

              using (Util.Profile("deco"))
              {
                // border order -x, +x, -y, +y, -z, +z
                // tri order, -x, -y, -z, +x, +y, +z
                // flat tri: +x, -z, -x, +z
                // slopes:
                // 0-3 = diagonal walls
                // 4-7 = ramps normal +x, -z, -x, +z (might have z sign backwards)
                // Faces (right, top, left, bottom)
                bool tile_has_border = styles[tile_styles[index]].border_tex != null;
                var above_tile_index = (y == dimensions[1] - 1) ? -1 : index + TilesPerSlice;
                if (above_tile_index != -1 && tile_shapes[above_tile_index] == -1)
                {
                  above_tile_index = -1;
                }
                var below_tile_index = (y == 0) ? -1 : index - TilesPerSlice;
                if (below_tile_index != -1 && tile_shapes[below_tile_index] == -1)
                {
                  below_tile_index = -1;
                }
                for (int side = 0, ceil_index = 0; side < 4; ++side, ceil_index += 3)
                {
                  Int3 side_point = new Int3(x + ceil_offset[ceil_index + 0], y, z + ceil_offset[ceil_index + 2]);
                  int side_tile_index = -1;
                  if (side_point[0] >= 0 && side_point[0] < dimensions[0] &&
                     side_point[2] >= 0 && side_point[2] < dimensions[2])
                  {
                    side_tile_index = TileIndex(new Int3(side_point[0], side_point[1], side_point[2]));
                    if (tile_shapes[side_tile_index] == -1)
                    {
                      side_tile_index = -1;
                    }
                  }

                  // Check for top edges of walls
                  if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 3) != 0 && (above_tile_index == -1 || (bitmasks[bitmask_index + TilesPerSlice * 6 + wall_sides[side]] & (1 << 1)) == 0))
                  {
                    // Check for ceiling
                    bool ceiling = false;
                    int style = -1;
                    Int3 check_point = new Int3(x + ceil_offset[ceil_index + 0], y + 1, z + ceil_offset[ceil_index + 0]);
                    if (check_point[0] > 0 && check_point[0] < dimensions[0] - 1 &&
                       check_point[2] > 0 && check_point[2] < dimensions[2] - 1 && y != dimensions[1] - 1)
                    {
                      ceiling = (BitmaskIndex(x + ceil_offset[ceil_index + 0], y + ceil_offset[ceil_index + 1], z + ceil_offset[ceil_index + 2], 2) & 1 << ceil_tris[side]) != 0;
                      if (ceiling)
                      {
                        style = tile_styles[TileIndex(new Int3(x + ceil_offset[ceil_index + 0], y + ceil_offset[ceil_index + 1], z + ceil_offset[ceil_index + 2]))];
                      }
                    }
                    // Check for ramp
                    bool ramp = false;
                    if (!ceiling && y != dimensions[1] - 1)
                    {
                      var tile_check_index = TileIndex(new Int3(x, y + 1, z));
                      if (tile_shapes[tile_check_index] != -1)
                      {
                        var slopes = types[tile_shapes[tile_check_index]].slopes[tile_directions[tile_check_index]];
                        ramp = ((slopes >> (4 * ramp_side[side])) & (1 << 3)) != 0;
                        if (ramp)
                        {
                          style = tile_styles[tile_check_index];
                        }
                        //Debug.Log($"Ramp: {FromBinary(slopes)}");
                      }
                    }
                    // Check for top of same tile
                    bool top = !ceiling && !ramp;
                    if (top)
                    {
                      style = tile_styles[index];
                    }
                    Color color = Color.yellow;
                    if (ceiling)
                    {
                      color = Color.cyan;
                    }
                    if (ramp)
                    {
                      color = Color.green;
                    }
                    if (top)
                    {
                      color = Color.magenta;
                    }

                    if ((ramp || top) && styles[style].border_tex != null)
                    {
                      AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  cube_face_vec[wall_sides[side] * 3],
                                  wall_sides[side] % 2 != 0,
                                  true,
                                  styles[style].border_tex_index,
                                  editable_mesh);
                    }
                  }
                  // Check if top piece of wall is missing, but side pieces are not
                  // Check if top piece is missing and style has overflow
                  if (tile_has_border)
                  {
                    if (bitmasks[bitmask_index + wall_sides[side]] == 0b0110)
                    {
                      AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  cube_face_vec[wall_sides[side] * 3],
                                  wall_sides[side] % 2 == 0,
                                  true,
                                  styles[tile_styles[index]].border_tex_index,
                                  editable_mesh);
                    }
                    else if (bitmasks[bitmask_index + wall_sides[side]] == 0b0011)
                    {
                      AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  cube_face_vec[wall_sides[side] * 3],
                                  wall_sides[side] % 2 == 0,
                                  true,
                                  styles[tile_styles[index]].border_tex_index,
                                  editable_mesh);
                    }
                    else
                    {
                      if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 3) == 0)
                      {
                        // Check left side
                        for (int tri = 0; tri < 2; ++tri)
                        {
                          if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << (tri * 2)) != 0)
                          {
                            AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f * ((tri == 0) ? 1f : -1f), cell_size),
                                        offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f, cell_size),
                                        cube_face_vec[wall_sides[side] * 3],
                                        !(tri == 1 ^ wall_sides[side] % 2 != 0),
                                        true,
                                        styles[tile_styles[index]].border_tex_index,
                                        editable_mesh);
                          }
                        }
                      }

                      // Check if side piece of wall is missing, but bottom is not
                      // Basically copy/pasted from previous
                      if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 1) != 0)
                      {
                        // Check left side
                        for (int tri = 0; tri < 2; ++tri)
                        {
                          if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << (tri * 2)) == 0)
                          {
                            AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f * ((tri == 0) ? 1f : -1f), cell_size),
                                        offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f, cell_size),
                                        cube_face_vec[wall_sides[side] * 3],
                                        !(tri == 1 ^ wall_sides[side] % 2 != 0),
                                        true,
                                        styles[tile_styles[index]].border_tex_index,
                                        editable_mesh);
                          }
                        }
                      }
                    }
                  }
                  // Check for top of diagonal walls with nothing above them (either no tile above, or tile above has no bottom diagonal wall on either side)
                  if (tile_has_border && ((type.slopes[tile_directions[index]] >> side * 4) & (1 << 1)) != 0 && (above_tile_index == -1 || ((((types[tile_shapes[above_tile_index]].slopes[tile_directions[above_tile_index]] >> side * 4) & (1 << 3)) == 0) && (((types[tile_shapes[above_tile_index]].slopes[tile_directions[above_tile_index]] >> ((side + 2) % 4) * 4) & (1 << 3)) == 0))))
                  {
                    // Check if top X tris are filled, and either overflow half or all of this edge
                    var top_mask = bitmasks[bitmask_index + 3];
                    int num_top_tiles = 0;
                    if ((top_mask & 0b0001) != 0) ++num_top_tiles;
                    if ((top_mask & 0b0010) != 0) ++num_top_tiles;
                    if ((top_mask & 0b0100) != 0) ++num_top_tiles;
                    if ((top_mask & 0b1000) != 0) ++num_top_tiles;
                    // e.g. if two halves are on top of each other
                    if (num_top_tiles == 2)
                    {
                      AddOverflow(offset + Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f + slope_vec[side * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f + slope_vec[side * 3 + 1] * -0.5f, cell_size),
                                  slope_vec[side * 3 + 0],
                                  true,
                                  true,
                                  styles[tile_styles[index]].border_tex_index,
                                  editable_mesh);
                    }
                    else if (num_top_tiles == 1)
                    {
                      var a = offset + Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f + slope_vec[side * 3 + 1] * 0.5f, cell_size);// + Vector3.up * 0.1f + slope_vec[side * 3 + 0] * 0.1f;
                      var b = offset + Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f - slope_vec[side * 3 + 1] * 0.5f, cell_size);// + Vector3.up * 0.1f + slope_vec[side * 3 + 0] * 0.1f;

                      // Fudge to cover T-junction seam
                      a += slope_vec[side * 3 + 2] * 0.02f;
                      b += slope_vec[side * 3 + 2] * 0.02f;

                      for (int i = 0; i < 2; ++i)
                      {
                        if ((top_mask & (1 << top_half[side * 2 + i])) != 0)
                        {
                          // tri order, -x, -z, +x, +z

                          AddOverflow(a,
                                      b,
                                      slope_vec[side * 3 + 0],
                                      true,
                                      true,
                                      styles[tile_styles[index]].border_tex_index,
                                      editable_mesh,
                                      i);
                        }
                      }
                    }
                  }
                  // Check for borrom edges of walls
                  if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 1) != 0)
                  {
                    // Check for floor
                    Int3 check_point = new Int3(x + ceil_offset[ceil_index + 0], y, z + ceil_offset[ceil_index + 2]);
                    int style = -1;
                    Color color = Color.magenta;
                    if (check_point[0] >= 0 && check_point[0] < dimensions[0] &&
                       check_point[2] >= 0 && check_point[2] < dimensions[2])
                    {
                      // Check for ramp up
                      if (side_tile_index != -1)
                      {
                        var slopes = types[tile_shapes[side_tile_index]].slopes[tile_directions[side_tile_index]];
                        if (((slopes >> (4 * ramp2_side[side])) & (1 << 3)) != 0)
                        {
                          style = tile_styles[side_tile_index];
                          color = Color.cyan;
                        }
                      }
                      if (style == -1 && y != 0)
                      {
                        // Check for floor
                        if ((BitmaskIndex(x + ceil_offset[ceil_index + 0], y - 1, z + ceil_offset[ceil_index + 2], 3) & 1 << ceil_tris[side]) != 0)
                        {
                          style = tile_styles[TileIndex(x + ceil_offset[ceil_index + 0], y - 1, z + ceil_offset[ceil_index + 2])];
                        }
                        else
                        {
                          // Check for ramp down
                          var tile_check_index = TileIndex(new Int3(check_point[0], y - 1, check_point[2]));
                          if (tile_shapes[tile_check_index] != -1)
                          {
                            var slopes = types[tile_shapes[tile_check_index]].slopes[tile_directions[tile_check_index]];
                            if (((slopes >> (4 * ramp_side[side])) & (1 << 1)) != 0)
                            {
                              style = tile_styles[tile_check_index];
                              color = Color.yellow;
                            }
                          }
                        }
                      }
                    }
                    if (style != -1)
                    {
                      if (styles[style].border_tex != null)
                      {
                        AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                    offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                    cube_face_vec[wall_sides[side] * 3],
                                    wall_sides[side] % 2 == 0,
                                    false,
                                    styles[style].border_tex_index,
                                    editable_mesh);
                      }
                    }
                  }
                  if (side_tile_index != -1 && styles[tile_styles[side_tile_index]].border_tex != null)
                  {
                    if (bitmasks[bitmask_index + wall_sides[side]] == 0b1001)
                    {
                      AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  cube_face_vec[wall_sides[side] * 3],
                                  wall_sides[side] % 2 != 0,
                                  false,
                                  styles[tile_styles[side_tile_index]].border_tex_index,
                                  editable_mesh);
                    }
                    else if (bitmasks[bitmask_index + wall_sides[side]] == 0b1100)
                    {
                      AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f, cell_size),
                                  cube_face_vec[wall_sides[side] * 3],
                                  wall_sides[side] % 2 != 0,
                                  false,
                                  styles[tile_styles[side_tile_index]].border_tex_index,
                                  editable_mesh);
                    }
                    else
                    {
                      // Check if bottom piece of wall is missing, but side pieces are not
                      if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 1) == 0)
                      {
                        // Check left side
                        for (int tri = 0; tri < 2; ++tri)
                        {
                          if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << (tri * 2)) != 0)
                          {
                            AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f * ((tri == 0) ? 1f : -1f), cell_size),
                                        offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f, cell_size),
                                        cube_face_vec[wall_sides[side] * 3],
                                        (tri == 1 ^ wall_sides[side] % 2 != 0),
                                        false,
                                        styles[tile_styles[side_tile_index]].border_tex_index,
                                        editable_mesh);
                          }
                        }
                      }
                      // Check if side piece of wall is missing, but top is not
                      // Basically copy/pasted from previous
                      if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << 3) != 0)
                      {
                        // Check left side
                        for (int tri = 0; tri < 2; ++tri)
                        {
                          if ((bitmasks[bitmask_index + wall_sides[side]] & 1 << (tri * 2)) == 0)
                          {
                            AddOverflow(offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f - cube_face_vec[wall_sides[side] * 3 + 2] * 0.5f + cube_face_vec[wall_sides[side] * 3 + 1] * 0.5f * ((tri == 0) ? 1f : -1f), cell_size),
                                        offset + Vector3.Scale(cube_face_vec[wall_sides[side] * 3] * 0.5f, cell_size),
                                        cube_face_vec[wall_sides[side] * 3],
                                        (tri == 1 ^ wall_sides[side] % 2 != 0),
                                        false,
                                        styles[tile_styles[side_tile_index]].border_tex_index,
                                        editable_mesh);
                          }
                        }
                      }
                    }
                  }

                  // Check for bottom diagonals with nothing below them
                  if (below_tile_index != -1 && styles[tile_styles[below_tile_index]].border_tex != null && ((type.slopes[tile_directions[index]] >> side * 4) & (1 << 3)) != 0 && ((((types[tile_shapes[below_tile_index]].slopes[tile_directions[below_tile_index]] >> side * 4) & (1 << 1)) == 0) && (((types[tile_shapes[below_tile_index]].slopes[tile_directions[below_tile_index]] >> ((side + 2) % 4) * 4) & (1 << 1)) == 0)))
                  {
                    // Check if bottom X tris are filled, and either overflow half or all of this edge
                    var top_mask = bitmasks[bitmask_index - TilesPerSlice * 6 + 3];
                    int num_top_tiles = 0;
                    if ((top_mask & 0b0001) != 0) ++num_top_tiles;
                    if ((top_mask & 0b0010) != 0) ++num_top_tiles;
                    if ((top_mask & 0b0100) != 0) ++num_top_tiles;
                    if ((top_mask & 0b1000) != 0) ++num_top_tiles;
                    // e.g. if two halves are on top of each other
                    if (num_top_tiles == 2)
                    {
                      AddOverflow(offset - Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f + slope_vec[side * 3 + 1] * 0.5f, cell_size),
                                  offset - Vector3.Scale(slope_vec[side * 3 + 2] * 0.5f + slope_vec[side * 3 + 1] * -0.5f, cell_size),
                                  slope_vec[side * 3 + 0],
                                  true,
                                  false,
                                  styles[tile_styles[below_tile_index]].border_tex_index,
                                  editable_mesh);
                    }
                    else if (num_top_tiles == 1)
                    {
                      // dir == 0 -> side == 2
                      // flat tri: +x, -z, -x, +z
                      var a = offset + Vector3.Scale(slope_vec[side * 3 + 2] * -0.5f + slope_vec[side * 3 + 1] * 0.5f, cell_size);// + Vector3.up * 0.1f + slope_vec[side * 3 + 0] * 0.1f;
                      var b = offset + Vector3.Scale(slope_vec[side * 3 + 2] * -0.5f - slope_vec[side * 3 + 1] * 0.5f, cell_size);// + Vector3.up * 0.1f + slope_vec[side * 3 + 0] * 0.1f;

                      // Fudge to cover T-junction seam
                      a += slope_vec[side * 3 + 0] * 0.01f;
                      b += slope_vec[side * 3 + 0] * 0.01f;

                      for (int i = 0; i < 2; ++i)
                      {
                        if ((top_mask & (1 << top_half_2[side * 2 + i])) != 0)
                        {
                          // tri order, -x, -z, +x, +z

                          AddOverflow(a,
                                      b,
                                      slope_vec[side * 3 + 0],
                                      false,
                                      false,
                                      styles[tile_styles[below_tile_index]].border_tex_index,
                                      editable_mesh,
                                      i);
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }

  void UpdateChunkMeshColors(Int3 chunkId)
  {
    var chunk = chunks[chunkId[0], chunkId[1], chunkId[2]];
    for (int i = 0; i < chunk.editable_mesh.color_list.Count; ++i)
    {
      var color = chunk.editable_mesh.color_list[i];
      // Offset vertically slightly to eliminate "flat darkening" issue
      color.r = GetSmoothLightAmount(chunk.editable_mesh.vert_list[i] + Vector3.up * cell_size[1] * 0.35f);
      chunk.editable_mesh.color_list[i] = color;
    }
  }

  void UpdateUnityMesh(Int3 chunkId)
  {
    using (Util.Profile("UpdateUnityMesh"))
    {
      var chunk = chunks[chunkId[0], chunkId[1], chunkId[2]];
      ref Mesh mesh = ref chunk.mesh;

      // Create terrain mesh object
      if (mesh == null)
      {
        mesh = new Mesh();
        mesh.name = "Terrain Display Mesh";
      }
      mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      mesh.triangles = zero_length_int_array; // To avoid error when changing vertices

      var editable_mesh = chunk.editable_mesh;
      mesh.SetVertices(editable_mesh.vert_list);
      mesh.SetUVs(0, editable_mesh.uv_list);
      mesh.SetNormals(editable_mesh.normal_list);
      mesh.SetTriangles(editable_mesh.tri_list, 0);
      mesh.SetColors(editable_mesh.color_list);

      ref var phys_mesh = ref chunk.phys_mesh;
      if (phys_mesh == null)
      {
        phys_mesh = new Mesh();
        phys_mesh.name = "Terrain Phys Mesh";
      }
      phys_mesh.triangles = zero_length_int_array; // To avoid error when changing vertices
      phys_mesh.SetVertices(chunk.physics_mesh.vert_list);
      phys_mesh.SetTriangles(chunk.physics_mesh.tri_list, 0);

      chunk.obj.GetComponent<MeshFilter>().sharedMesh = mesh;
      using (Util.Profile("Change shared_mesh"))
      {
        chunk.obj.GetComponent<MeshCollider>().sharedMesh = phys_mesh;
      }
    }
  }

  public void SetTempMaterial(Material mat)
  {
    foreach (Chunk chunk in chunks)
    {
      chunk.renderer.sharedMaterial = mat ?? material;
    }
  }

  void Awake()
  {
    material = new Material(material); // Make material instance in case something else uses this material also
    Shader.SetGlobalFloat("_terrain_min_ambient", min_ambient);
    AnalyzeBlocks();
    CreateTextureArrays();

    StartCoroutine(ApplyChangesRoutine());
  }

  public Int3 TileFromWorld(Vector3 coord)
  {
    Vector3 local_coord = transform.InverseTransformPoint(coord);
    return new Int3(Mathf.FloorToInt((local_coord[0] / cell_size[0]) + 0.5f),
                    Mathf.FloorToInt((local_coord[1] / cell_size[1]) + 0.5f),
                    Mathf.FloorToInt((local_coord[2] / cell_size[2]) + 0.5f));
  }

  public static Int3 TileFromLocal(Vector3 coord, Vector3 cell_size)
  {
    return new Int3(Mathf.FloorToInt((coord[0] / cell_size[0]) + 0.5f),
                    Mathf.FloorToInt((coord[1] / cell_size[1]) + 0.5f),
                    Mathf.FloorToInt((coord[2] / cell_size[2]) + 0.5f));
  }

  float GetClampedColor(int x, int y, int z)
  {
    return tile_colors[TileIndex(new Int3(Mathf.Clamp(x, 0, dimensions[0] - 1),
                                          Mathf.Clamp(y, 0, dimensions[1] - 1),
                                          Mathf.Clamp(z, 0, dimensions[2] - 1)))];
  }

  float[,,] gsla_corners = new float[2, 2, 2];
  float[,] flat_corners = new float[2, 2];
  float[] line_corners = new float[2];

  // In terrain system space (world space - terrain offset)
  public float GetSmoothLightAmount(Vector3 pos)
  {
    var tile_vert = Vector3.Scale(pos, inv_cell_size);

    Int3 val = new Int3(Mathf.FloorToInt(tile_vert[0]),
                        Mathf.FloorToInt(tile_vert[1]),
                        Mathf.FloorToInt(tile_vert[2]));
    Vector3 gradient = new Vector3(tile_vert[0] - val[0],
                                 tile_vert[1] - val[1],
                                 tile_vert[2] - val[2]);
    gsla_corners[0, 0, 0] = GetClampedColor(val[0], val[1], val[2]);
    gsla_corners[0, 0, 1] = GetClampedColor(val[0], val[1], val[2] + 1);
    gsla_corners[0, 1, 0] = GetClampedColor(val[0], val[1] + 1, val[2]);
    gsla_corners[0, 1, 1] = GetClampedColor(val[0], val[1] + 1, val[2] + 1);
    gsla_corners[1, 0, 0] = GetClampedColor(val[0] + 1, val[1], val[2]);
    gsla_corners[1, 0, 1] = GetClampedColor(val[0] + 1, val[1], val[2] + 1);
    gsla_corners[1, 1, 0] = GetClampedColor(val[0] + 1, val[1] + 1, val[2]);
    gsla_corners[1, 1, 1] = GetClampedColor(val[0] + 1, val[1] + 1, val[2] + 1);

    flat_corners[0, 0] = Mathf.Lerp(gsla_corners[0, 0, 0], gsla_corners[0, 0, 1], gradient[2]);
    flat_corners[0, 1] = Mathf.Lerp(gsla_corners[0, 1, 0], gsla_corners[0, 1, 1], gradient[2]);
    flat_corners[1, 0] = Mathf.Lerp(gsla_corners[1, 0, 0], gsla_corners[1, 0, 1], gradient[2]);
    flat_corners[1, 1] = Mathf.Lerp(gsla_corners[1, 1, 0], gsla_corners[1, 1, 1], gradient[2]);

    line_corners[0] = Mathf.Lerp(flat_corners[0, 0], flat_corners[0, 1], gradient[1]);
    line_corners[1] = Mathf.Lerp(flat_corners[1, 0], flat_corners[1, 1], gradient[1]);

    return Mathf.Lerp(line_corners[0], line_corners[1], gradient[0]);
  }


  static int Encode(int len, int shape, int style, int dir)
  {
    return (len << (9 + 9 + 4)) + (style << (9 + 4)) + ((shape + 1) << 4) + dir;
  }

  static void Decode(int val, out int len, out int shape, out int style, out int dir)
  {
    len = val >> (9 + 9 + 4);
    style = (val >> (9 + 4)) & 0b1111_1111_1;
    shape = ((val >> (4)) & 0b1111) - 1;
    dir = val & 0b11;
  }

  // Input a X-Z slice of tile data and convert it to a list of segments on the X axis
  public struct EncodeJob : IJob
  {
    [ReadOnly] public int num_spans;
    [ReadOnly] public int start_x, start_z, dimensions_x;
    [ReadOnly] public NativeSlice<int> tile_shapes;
    [ReadOnly] public NativeSlice<int> tile_styles;
    [ReadOnly] public NativeSlice<int> tile_directions;
    public NativeSlice<int> span;
    public NativeSlice<int> span_lengths;
    // len 9, style 9, shape 3, dir 2

    public void Execute()
    {
      int x_length = span.Length / span_lengths.Length;
      for (int z = 0; z < num_spans; ++z)
      {
        int seg_index = z * x_length; // Where to store segment info
        int tile_index = (start_z + z) * dimensions_x + start_x;
        int last_tile_index = tile_index + x_length; // Last tile in span
                                                     // Set up first segment
        int seg_len = 1;
        int seg_shape = tile_shapes[tile_index];
        int seg_dir = tile_directions[tile_index];
        int seg_style = tile_styles[tile_index];
        // Walk through remaining tiles
        ++tile_index;
        for (; tile_index < last_tile_index; ++tile_index)
        {
          // Add new tiles, adding to previous span if same, new if different
          int shape = tile_shapes[tile_index];
          int dir = tile_directions[tile_index];
          int style = tile_styles[tile_index];
          if (shape == seg_shape && (shape == -1 || (style == seg_style && dir == seg_dir)))
          {
            // Tile is same as previous, just increment segment length
            ++seg_len;
          }
          else
          {
            // Tile is different, save prev segment and start new one
            span[seg_index] = Encode(seg_len, seg_shape, seg_style, seg_dir);
            ++seg_index;
            seg_len = 1;
            seg_shape = shape;
            seg_dir = dir;
            seg_style = style;
          }
        }
        // We're out of tiles, so encode final segment
        span[seg_index] = Encode(seg_len, seg_shape, seg_style, seg_dir);
        // Record number of segments in span
        span_lengths[z] = seg_index - (z * x_length) + 1;
      }
    }
  }

  // Decompress span segments into full tile array
  public struct DecodeJob : IJob
  {
    [ReadOnly] public int num_spans;
    public NativeSlice<int> tile_shapes;
    public NativeSlice<int> tile_styles;
    public NativeSlice<int> tile_directions;
    [ReadOnly] public NativeSlice<int> span;
    [ReadOnly] public NativeSlice<int> span_lengths;

    public void Execute()
    {
      int tile_index = 0;
      int x_length = tile_shapes.Length / num_spans;
      for (int z = 0; z < num_spans; ++z)
      {
        int span_index = z * x_length;
        int num_segments = span_lengths[z];
        Debug.Assert(tile_index == span_index);
        int row_end = span_index + x_length;
        // Keep decoding segments until we fill up the row
        while (tile_index < row_end)
        {
          // Decode segment
          int span_val = span[span_index];
          Decode(span_val, out int span_len, out int span_shape, out int span_style, out int span_dir);
          // Apply segment to tiles
          for (int i = 0; i < span_len; ++i, ++tile_index)
          {
            tile_shapes[tile_index] = span_shape;
            tile_directions[tile_index] = span_dir;
            tile_styles[tile_index] = span_style;
          }
          ++span_index;
        }
        //Debug.Assert(tile_index == row_end);
      }
    }
  }

  void DisposeNativeArrays()
  {
    CompleteTileDataJobs();

    tile_shapes.Dispose();
    tile_colors.Dispose();
    tile_styles.Dispose();
    tile_directions.Dispose();
    for (int i = 0; i < spans.Length; ++i)
    {
      spans[i].Dispose();
    }
    for (int i = 0; i < span_lengths.Length; ++i)
    {
      span_lengths[i].Dispose();
    }

    if (sky_job_running)
    {
      sky_job_thread.Abort();
      //sky_job_handle.Complete();
    }
    sky_job.tile_shapes.Dispose();
    sky_job.sky_light_dist.Dispose();
    sky_job.color.Dispose();
    sky_job.write_bounds.Dispose();
  }

  void OnDestroy()
  {
    DisposeNativeArrays();
  }

  void CheckOnSkyJob()
  {
    if (!sky_job_running && sky_dirty_max != Int3.zero())
    {
      using (Util.Profile("CalculateSkyLight"))
        CalculateSkyLight(sky_dirty_min, sky_dirty_max);
      sky_dirty_min = dimensions;
      sky_dirty_max = Int3.zero();
    }

    if (sky_job_running && sky_job_thread != null && !sky_job_thread.IsAlive)
    {
      using (Util.Profile("Join Sky thread"))
        sky_job_thread.Join();
      sky_job_running = false;

      Int3 write_min = sky_job.write_bounds[0];
      Int3 write_max = sky_job.write_bounds[1];

      using (Util.Profile("Copy to tile_colors"))
      {
        int size = write_max[0] - write_min[0];
        for (int y = write_min[1], y_base = write_min[1] * TilesPerSlice; y < write_max[1]; ++y, y_base += TilesPerSlice)
        {
          for (int z = write_min[2], z_base = y_base + write_min[2] * dimensions[0]; z < write_max[2]; ++z, z_base += dimensions[0])
          {
            var index = z_base + write_min[0];
            tile_colors.Slice(index, size).CopyFrom(sky_job.color.Slice(index, size));
          }
        }
      }

      Int3 minDirtyChunk = GetChunkCoord(write_min);
      Int3 maxDirtyChunk = GetChunkCoord(write_max - Int3.one());
      using (Util.Profile($"Mark dirty chunks {minDirtyChunk} to {maxDirtyChunk}"))
      {
        for (int x = minDirtyChunk[0]; x <= maxDirtyChunk[0]; ++x)
        {
          for (int y = minDirtyChunk[1]; y <= maxDirtyChunk[1]; ++y)
          {
            for (int z = minDirtyChunk[2]; z <= maxDirtyChunk[2]; ++z)
            {
              // TODO this causes a lot of garbage...probably because of boxing..
              lighting_dirty_chunks.Add(new Int3(x, y, z));
            }
          }
        }
      }
    }
  }

  System.Collections.IEnumerator ApplyChangesRoutine()
  {
    while (true)
    {
      yield return ApplyChanges();
    }
  }

  System.Collections.IEnumerator ApplyChanges()
  {
    if (!this.enabled)
    {
      yield break;
    }

    if (tex_array_dirty)
    {
      using (Util.Profile("CreateTextureArrays"))
        CreateTextureArrays();
    }

    using (Util.Profile("CheckOnSkyJob"))
      CheckOnSkyJob();

    Int3 closest = Int3.zero();
    float highestImportance = System.Single.NegativeInfinity;
    int closest_distance = int.MaxValue;
    Int3 curr_chunk = GetChunkCoord(TileFromWorld(Camera.main.transform.position));

    var to_delete = new List<Int3>();

    using (Util.Profile("GetClosestDirtyChunk"))
    {
      foreach (var pair in dirty_chunks)
      {
        var dirty_chunk = pair.Key;
        var importance = pair.Value;

        int dist = Mathf.Abs(dirty_chunk[0] - curr_chunk[0]) +
                   Mathf.Abs(dirty_chunk[1] - curr_chunk[1]) +
                   Mathf.Abs(dirty_chunk[2] - curr_chunk[2]);
        if (dist < closest_distance || importance > highestImportance)
        {
          bool empty = CheckEmpty(dirty_chunk);
          if (!empty)
          {
            closest = dirty_chunk;
            closest_distance = dist;
            highestImportance = importance;
          }
          else
          {
            to_delete.Add(dirty_chunk);
          }
        }
      }
      foreach (var val in to_delete)
      {
        dirty_chunks.Remove(val);
      }
      to_delete.Clear();
    }


    // Figure out a working chunk for lighting, so we can kick off both jobs at
    // once. To reduce latency.

    MeshJob? meshJob = null;
    LightJob? lightJob = null;

    Debug.Assert(meshHandle == null, "meshHandle not null??");
    Debug.Assert(lightHandle == null, "lightHandle not null??");

    if (closest_distance != int.MaxValue)
    {
      meshJob = new MeshJob { chunkId = closest, terrainPtr = GCHandle.Alloc(this), tileData = GetTileData() };
      meshHandle = meshJob.Value.Schedule();
      dirty_chunks.Remove(closest);
    }

    using (Util.Profile("GetClosestLightingDirtyChunk"))
    {
      to_delete.Clear();
      closest = Int3.zero();
      closest_distance = int.MaxValue;

      foreach (var dirty_chunk in lighting_dirty_chunks)
      {
        int dist = Mathf.Abs(dirty_chunk[0] - curr_chunk[0]) +
                   Mathf.Abs(dirty_chunk[1] - curr_chunk[1]) +
                   Mathf.Abs(dirty_chunk[2] - curr_chunk[2]);
        if (dist < closest_distance)
        {
          var chunk = chunks[dirty_chunk[0], dirty_chunk[1], dirty_chunk[2]];
          if (chunk.obj.activeSelf)
          {
            if (!dirty_chunks.ContainsKey(dirty_chunk) && chunk.mesh != null)
            {
              closest = dirty_chunk;
              closest_distance = dist;
            }
          }
          else
          {
            to_delete.Add(dirty_chunk);
          }
        }
      }

      foreach (var val in to_delete)
      {
        lighting_dirty_chunks.Remove(val);
      }
    }

    if (closest_distance != int.MaxValue)
    {
      var chunk = chunks[closest[0], closest[1], closest[2]];
      Debug.Assert(chunk.mesh != null, "Chunk had no mesh after find-best block");

      lightJob = new LightJob { chunkId = closest, terrainPtr = GCHandle.Alloc(this) };
      lightHandle = meshHandle != null
        // Need to wait for mesh job to finish - may be the same chunk.
        ? lightJob.Value.Schedule(meshHandle.Value)
        : lightJob.Value.Schedule();
      lighting_dirty_chunks.Remove(closest);
    }

    // Wait for both, to maximize utilization
    yield return new WaitWhile(() =>
      (meshHandle != null && !meshHandle.Value.IsCompleted)
      ||
      (lightHandle != null && !lightHandle.Value.IsCompleted));

    if (meshHandle != null)
    {
      meshHandle.Value.Complete();
      meshHandle = null;

      UpdateUnityMesh(meshJob.Value.chunkId);
      meshJob.Value.terrainPtr.Free();
    }

    if (lightHandle != null)
    {
      lightHandle.Value.Complete();
      lightHandle = null;

      var chunk = GetChunk(lightJob.Value.chunkId);
      chunk.mesh.SetColors(chunk.editable_mesh.color_list);
      lightJob.Value.terrainPtr.Free();
    }

    if (dirty_chunks.Count == 0 &&
       lighting_dirty_chunks.Count == 0 &&
       !sky_job_running)
    {

      // All done!
      this.enabled = false;
    }
  }

  public void SetRootOffset(Vector3 pos)
  {
    transform.position = pos;
  }

  public Int3 GetWorldDimensions()
  {
    return this.dimensions;
  }

  public int GetNumDirtyChunks()
  {
    return dirty_chunks.Count;
  }

  public float GetTotalDirtyChunkImportance()
  {
    float t = 0f;
    foreach (var pair in dirty_chunks)
    {
      t += pair.Value;
    }
    return t;
  }

  public int GetNumDirtyLightChunks()
  {
    return lighting_dirty_chunks.Count;
  }

  public void MarkDirtyChunkImportance(Int3 coord, float importance)
  {
    CompleteTileDataJobs();
    if (dirty_chunks.ContainsKey(coord))
    {
      dirty_chunks[coord] = importance;
    }
    else
    {
      // If the chunk is not dirty, then it's already built and importance is
      // irrelevant. Do nothing!
      Util.LogWarning($"Ignoring chunk importance mark for {coord}");
    }
  }

  internal void ReportRigidbodyAt(Int3 cell)
  {
    CompleteTileDataJobs();
    // Mark the entire column of chunks as important.
    Int3 chunk = GetChunkCoord(cell);
    for (int y = 0; y < chunks.GetLength(1); y++)
    {
      chunk.y = y;
      MarkDirtyChunkImportance(chunk, 1f);
    }
  }

  public float GetMipBias()
  {
    return tex_array.mipMapBias;
  }

  public void SetMipBias(float bias)
  {
    tex_array.mipMapBias = bias;
    border_tex_array.mipMapBias = bias;
  }

  public int GetAnisoLevel()
  {
    return tex_array.anisoLevel;
  }

  public void SetAnisoLevel(int level)
  {
    tex_array.anisoLevel = level;
    border_tex_array.anisoLevel = level;
  }

  public void FindReplaceStyle(int find, int replace)
  {
    CompleteTileDataJobs();
    for (int i = 0; i < tile_styles.Length; i++)
    {
      if (tile_styles[i] == find)
      {
        tile_styles[i] = replace;
      }
    }
    // TODO actually mark dirty..
  }
}
