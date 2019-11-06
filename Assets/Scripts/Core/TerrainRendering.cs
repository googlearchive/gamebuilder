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
using UnityEngine.Rendering;
using static TerrainManager;
using static GameBuilder.RenderUtil;
using static GameBuilder.TerrainUtil;
using GameBuilder;

// Creates the game objects to realize terrain data as controlled by the terrain database.
// Performance critical.
public class TerrainRendering : MonoBehaviour
{
  public class BlockTag : MonoBehaviour
  {
    public BlockStyle style;
  }

  [System.Serializable]
  public struct StylePrefabs
  {
    public GameObject fullPrefab;
    public GameObject halfPrefab;
    public GameObject rampPrefab;
    public GameObject cornerPrefab;
    public GameObject wallPrefab;

    public void ForEachPrefab(System.Action<GameObject> func)
    {
      func(fullPrefab);
      func(halfPrefab);
      func(rampPrefab);
      func(cornerPrefab);
      func(wallPrefab);
    }

    public GameObject GetBlockType(BlockShape type)
    {
      switch (type)
      {
        case BlockShape.Full:
          return fullPrefab;
        case BlockShape.Half:
          return halfPrefab;
        case BlockShape.Ramp:
          return rampPrefab;
        case BlockShape.Corner:
          return cornerPrefab;
        default:
          return null;
      }
    }
  }

  [SerializeField] StylePrefabs stoneStylePrefabs;
  [SerializeField] StylePrefabs grassStylePrefabs;
  [SerializeField] StylePrefabs metalStylePrefabs;
  [SerializeField] StylePrefabs snowRockStylePrefabs;
  [SerializeField] StylePrefabs solidColorStylePrefabs;

  // This is auto-generated from solidColorStylePrefabs.
  // The prefabs for each color share a single material, for perf.
  StylePrefabs[] perColorPrefabs;

  // we should probably move this into TerrainSystem.
  public Color[] blockColors;
  const string MATERIAL_COLOR_FIELD = "_Color";

  Transform gridParent;
  Dictionary<Cell, GameObject> cellInstances = new Dictionary<Cell, GameObject>();
  Dictionary<Side, GameObject> edgeInstances = new Dictionary<Side, GameObject>();

  struct InstanceGroupDef
  {
    public readonly BlockShape shape;
    public readonly BlockStyle style;

    public InstanceGroupDef(CellValue block)
    {
      this.shape = block.blockType;
      this.style = block.style;
    }
  }

  Dictionary<InstanceGroupDef, BlockManager> groupBlockManagers = new Dictionary<InstanceGroupDef, BlockManager>();

  void ForEachStylePrefabSet(System.Action<StylePrefabs> func)
  {
    func(stoneStylePrefabs);
    func(grassStylePrefabs);
    func(metalStylePrefabs);
    func(snowRockStylePrefabs);
    func(solidColorStylePrefabs);
    foreach (var set in perColorPrefabs)
    {
      func(set);
    }
  }

  void SanityCheckPrefab(GameObject prefab)
  {
    Debug.Assert(prefab.GetComponentsInChildren<Renderer>().Length == 1, $"Block prefab {prefab.name} has more than 1 renderer - is this really necessary?");
    if (!prefab.name.Contains("Wall"))
    {
      Debug.Assert(prefab.tag == "Ground", $"Block prefab {prefab.name} is not tagged as Ground");
    }
  }

  void Awake()
  {
    gridParent = (new GameObject("gridParent")).transform;
    gridParent.transform.parent = this.transform;

    InitializePerColorPrefabs();

    ForEachStylePrefabSet(set =>
    {
      set.ForEachPrefab(SanityCheckPrefab);
    });
  }

  void InitializePerColorPrefabs()
  {
    Debug.Assert(blockColors.Length == NumSolidColorStyles);

    // We're assuming all shapes use the same material.
    Material solidColorMaterial = solidColorStylePrefabs.cornerPrefab.GetComponentInChildren<Renderer>().sharedMaterial;

    perColorPrefabs = new StylePrefabs[NumSolidColorStyles];

    // Create prefabs for each color so we can just instantiate them, and they'll share the same materials.
    for (int style = 0; style < NumSolidColorStyles; style++)
    {
      Material sharedColorMaterial = Material.Instantiate(solidColorMaterial);
      sharedColorMaterial.SetColor(MATERIAL_COLOR_FIELD, blockColors[style]);

      StylePrefabs prefabs = new StylePrefabs();

      prefabs.fullPrefab = GameObject.Instantiate(solidColorStylePrefabs.fullPrefab);
      prefabs.halfPrefab = GameObject.Instantiate(solidColorStylePrefabs.halfPrefab);
      prefabs.rampPrefab = GameObject.Instantiate(solidColorStylePrefabs.rampPrefab);
      prefabs.cornerPrefab = GameObject.Instantiate(solidColorStylePrefabs.cornerPrefab);
      prefabs.wallPrefab = GameObject.Instantiate(solidColorStylePrefabs.wallPrefab);

      prefabs.ForEachPrefab(prefab =>
      {
        prefab.transform.SetParent(this.transform);
        prefab.SetActive(false);
        prefab.SetAllMaterialsToShared(sharedColorMaterial);
      });

      perColorPrefabs[style] = prefabs;
    }
  }

  void SpawnCellBlock(BlockShape blockType, Cell cell, BlockDirection direction, BlockStyle style)
  {
    GameObject newBlock = Instantiate(
      GetPrefabForBlockType(blockType, style),
      TerrainManager.GetCellCenter(cell),
      GetBlockDirectionAsQuaternion(direction),
      gridParent);

    var tag = newBlock.AddComponent<BlockTag>();
    tag.style = style;

    // Hide here instead of for the prefab...the prefab is still used for
    // preview - blah!
    foreach (var r in newBlock.GetComponentsInChildren<MeshRenderer>())
    {
      MonoBehaviour.Destroy(r);
    }

    newBlock.SetActive(true);
    cellInstances.Add(cell, newBlock);
  }

  Vector3 GetWallOrigin(Side edge, BlockStyle style)
  {
    bool isWest = edge.side == CellSide.West;

    Vector3 offsetBlocks;

    if (style == BlockStyle.Stone)
    {
      offsetBlocks = isWest ?
       new Vector3(-0.5f, -0.5f, -0.5f)
       : new Vector3(0.5f, -0.5f, -0.5f);
    }
    else
    {
      offsetBlocks = isWest ?
      new Vector3(-0.5f, -0.5f, 0.5f)
      : new Vector3(-0.5f, -0.5f, -0.5f);
    }

    var cellCenter = GetCellCenter(new Cell(edge.x, edge.y, edge.z));
    return cellCenter + Vector3.Scale(BLOCK_SIZE, offsetBlocks);
  }

  void SpawnEdgeAt(Side edge, BlockStyle style)
  {
    GameObject newWall = Instantiate(
      GetPrefabForWallStyle(style),
      GetWallOrigin(edge, style),
      (edge.side == CellSide.West) ? Quaternion.Euler(0, 90, 0) : Quaternion.identity,
      gridParent);

    newWall.SetActive(true);
    edgeInstances.Add(edge, newWall);
  }

  // TODO unify these two methods below with a single "GetStylePrefabs(BlockStyle)"

  public GameObject GetPrefabForBlockType(BlockShape type, BlockStyle style)
  {
    switch (style)
    {
      case BlockStyle.Stone:
        return stoneStylePrefabs.GetBlockType(type);
      case BlockStyle.Grass:
        return grassStylePrefabs.GetBlockType(type);
      case BlockStyle.Space:
        return metalStylePrefabs.GetBlockType(type);
      case BlockStyle.SnowRock:
        return snowRockStylePrefabs.GetBlockType(type);
      default:
        int colorId = (int)style;
        if (colorId < 0 || colorId > (int)LastSolidColorStyle)
        {
          Util.LogError($"Bad style given: {style}. Snapping to color 0.");
          colorId = 0;
        }
        return perColorPrefabs[colorId].GetBlockType(type);
    }
  }

  private GameObject GetPrefabForWallStyle(BlockStyle style)
  {
    switch (style)
    {
      case BlockStyle.Stone:
        return stoneStylePrefabs.wallPrefab;
      case BlockStyle.Grass:
        return grassStylePrefabs.wallPrefab;
      case BlockStyle.Space:
        return metalStylePrefabs.wallPrefab;
      case BlockStyle.SnowRock:
        return snowRockStylePrefabs.wallPrefab;
      default:
        int colorId = (int)style;
        if (colorId < 0 || colorId > (int)LastSolidColorStyle)
        {
          Util.LogError($"Bad style given: {style}. Snapping to color 0.");
          colorId = 0;
        }
        return perColorPrefabs[colorId].wallPrefab;
    }
  }

  BlockManager GetOrCreateBlockManager(InstanceGroupDef groupDef)
  {
    BlockManager manager = null;
    if (groupBlockManagers.TryGetValue(groupDef, out manager))
    {
      return manager;
    }
    else
    {
      manager = new BlockManager();
      groupBlockManagers[groupDef] = manager;
      return manager;
    }
  }

  BlockManager GetBlockManager(CellValue block)
  {
    if (block.blockType == BlockShape.Empty)
    {
      throw new System.Exception("Should not be getting block manager for empty..");
    }
    return GetOrCreateBlockManager(new InstanceGroupDef(block));
  }

  public void OnSetCell(CellValue cellValue, Cell cell)
  {
    Int3 u = new Int3(cell.x, cell.y, cell.z);
    GameObject inst = null;
    if (cellInstances.TryGetValue(cell, out inst))
    {
      Destroy(inst);
      cellInstances.Remove(cell);

      // Clear this cell in ALL block managers. Since we don't readily know
      // which one was there before.
      foreach (var manager in groupBlockManagers.Values)
      {
        manager.Clear(u);
      }
    }

    if (cellValue.blockType != BlockShape.Empty)
    {
      // NOTE: Could be slightly smarter by seeing if current style/shape/direction is same..
      SpawnCellBlock(cellValue.blockType, cell, cellValue.direction, cellValue.style);
      // NOTE: We only use game objects for collision now. Eventually, we should
      // stop doing that as well, and create per-block combined collision
      // meshes.

      Matrix4x4 cellTransform =
        Matrix4x4.Translate(TerrainManager.GetCellCenter(cell))
      * Matrix4x4.Rotate(GetBlockDirectionAsQuaternion(cellValue.direction));
      GetBlockManager(cellValue).Set(u, cellTransform);
    }
  }

  public void OnSetSide(Side edge, bool on, BlockStyle style)
  {
    GameObject existing = null;
    if (edgeInstances.TryGetValue(edge, out existing))
    {
      Destroy(existing);
      edgeInstances.Remove(edge);
    }

    if (on)
    {
      SpawnEdgeAt(edge, style);
    }
  }

  public void Clear()
  {
    foreach (Transform child in gridParent)
    {
      Destroy(child);
    }

    foreach (var blockManager in groupBlockManagers.Values)
    {
      blockManager.ClearAll();
    }
  }

  void DrawInstancedCells()
  {
    foreach (var pair in groupBlockManagers)
    {
      GameObject prefab = GetPrefabForBlockType(pair.Key.shape, pair.Key.style);
      Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
      Material mat = prefab.GetComponent<MeshRenderer>().sharedMaterial;
      int layer = prefab.layer;

      pair.Value.ForEachBlock(block =>
      {
        var xforms = block.GetTransforms();
        var count = block.GetNumOccupied();
        Graphics.DrawMeshInstanced(mesh, 0, mat, xforms, count, null, ShadowCastingMode.On, true, layer, null, LightProbeUsage.Off, null);
      });
    }
  }

  void Update()
  {
    DrawInstancedCells();
  }
}
