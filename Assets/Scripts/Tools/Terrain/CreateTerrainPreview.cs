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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TerrainManager;
using GameBuilder;

public class CreateTerrainPreview : MonoBehaviour
{
  [SerializeField] Material previewMaterial;

  Material previewMaterialInstance;

  static string CLEAR_MESSAGE = "CLEAR";

  TerrainRendering terrainRendering;
  GameObject previewObject;
  BlockShape previewShape;
  BlockDirection previewDirection;
  Cell previewCell;
  Color previewTint;
  UnreliableMessageSystem unreliableMessaging;
  VoosEngine engine;

  HashSet<GameObject> creationPreview = new HashSet<GameObject>();

  Dictionary<int, HashSet<GameObject>> networkedPreviews = new Dictionary<int, HashSet<GameObject>>();

  [System.Serializable]
  struct PreviewData
  {
    public Int3 cell;
    public ushort shape;
    public ushort direction;
    public Color tint;
    public bool isHoverPreview;
  }

  public void Awake()
  {
    Util.FindIfNotSet(this, ref terrainRendering);
    Util.FindIfNotSet(this, ref unreliableMessaging);
    Util.FindIfNotSet(this, ref engine);
    previewMaterialInstance = Instantiate(previewMaterial);

    unreliableMessaging.AddRawHandler(NetworkingEventCodes.TERRAIN_PREVIEW, HandlePreviewMessage);
  }

  List<VoosActor> controlledActorsWork = new List<VoosActor>();

  void HandlePreviewMessage(object rawData, int senderId)
  {
    string rawString = (string)rawData;

    if (rawString == CLEAR_MESSAGE)
    {
      // Signal to clear.
      if (networkedPreviews.ContainsKey(senderId))
      {
        networkedPreviews[senderId].DestroyAllAndClear();
      }
      return;
    }

    PreviewData data = JsonUtility.FromJson<PreviewData>(rawString);
    if (!networkedPreviews.ContainsKey(senderId))
    {
      networkedPreviews[senderId] = new HashSet<GameObject>();
    }

    if (data.isHoverPreview)
    {
      // Clear any existing, since the hover is just a single preview ghost
      networkedPreviews[senderId].DestroyAllAndClear();
    }

    BlockShape shape = (BlockShape)data.shape;
    BlockDirection dir = (BlockDirection)data.direction;
    var preview = InstantiatePreview(shape);
    preview.transform.parent = null;
    preview.transform.position = GetCellCenter(new Cell(data.cell));
    preview.transform.rotation = GetBlockDirectionAsQuaternion(dir);
    preview.GetComponentInChildren<Renderer>().material.SetColor("_MainTint", data.tint);
    networkedPreviews[senderId].Add(preview);
  }

  public void SetVisibility(bool on)
  {
    previewObject?.SetActive(on);
    if (!on)
    {
      creationPreview.DestroyAllAndClear();
      unreliableMessaging.SendRaw(NetworkingEventCodes.TERRAIN_PREVIEW, CLEAR_MESSAGE);
    }
  }

  public void AddCellToCreationPreview(Cell cell)
  {
    creationPreview.Add(Instantiate(previewObject, GetCellCenter(cell), transform.rotation));

    // Network it.
    var netData = new PreviewData
    {
      isHoverPreview = false,
      cell = cell.ToInt3(),
      shape = (ushort)previewShape,
      direction = (ushort)previewDirection,
      tint = previewTint
    };
    unreliableMessaging.SendRaw(NetworkingEventCodes.TERRAIN_PREVIEW, JsonUtility.ToJson(netData));
  }

  public void ClearCreationPreview()
  {
    creationPreview.DestroyAllAndClear();
    unreliableMessaging.SendRaw(NetworkingEventCodes.TERRAIN_PREVIEW, CLEAR_MESSAGE);
  }


  public void SetTint(Color tint)
  {
    previewMaterialInstance.SetColor("_MainTint", ArtUtil.GetHologramColor(tint));
    previewTint = tint;
  }

  GameObject InstantiatePreview(BlockShape blockShape)
  {
    var rv = Instantiate(
      terrainRendering.GetPrefabForBlockType(blockShape, BlockStyle.SolidColor0),
      transform);
    rv.SetActive(true);
    rv.transform.localPosition = Vector3.zero;
    rv.transform.localRotation = Quaternion.identity;
    rv.GetComponentInChildren<Renderer>().material = previewMaterialInstance;
    foreach (Collider col in rv.GetComponentsInChildren<Collider>())
    {
      GameObject.Destroy(col);
    }
    return rv;
  }

  void NetworkHoverPreview()
  {
    if (creationPreview.Count > 0)
    {
      // We're dragging - don't worry about the single hover block.
      return;
    }

    var netData = new PreviewData
    {
      isHoverPreview = true, // Important!
      cell = previewCell.ToInt3(),
      shape = (ushort)previewShape,
      direction = (ushort)previewDirection,
      tint = previewTint
    };
    unreliableMessaging.SendRaw(NetworkingEventCodes.TERRAIN_PREVIEW, JsonUtility.ToJson(netData));
  }

  public void UpdatePreviewBlock(BlockShape blockShape, BlockDirection blockDirection)
  {
    if (previewObject != null)
    {
      Destroy(previewObject);
    }

    previewObject = InstantiatePreview(blockShape);
    previewShape = blockShape;
    previewDirection = blockDirection;
    transform.rotation = GetBlockDirectionAsQuaternion(blockDirection);

    NetworkHoverPreview();
  }

  internal void UpdatePreviewDirection(BlockDirection blockDirection)
  {
    transform.rotation = GetBlockDirectionAsQuaternion(blockDirection);
    previewDirection = blockDirection;

    foreach (GameObject go in creationPreview)
    {
      go.transform.rotation = transform.rotation;
    }

    NetworkHoverPreview();
  }

  public void UpdatePreviewCell(Cell cell)
  {
    if (previewObject == null)
    {
      return;
    }

    previewObject.transform.position = GetCellCenter(cell);
    previewCell = cell;

    NetworkHoverPreview();
  }

  void Update()
  {
    foreach (var pair in networkedPreviews)
    {
      int otherId = pair.Key;
      if (Array.FindIndex(PhotonNetwork.otherPlayers, player => player.ID == otherId) == -1)
      {
        // Player is disconnected.
        pair.Value.DestroyAllAndClear();
      }
    }

    // TODO autoclear previews if we haven't received anything in like 5s - probably dropped the "clear" message.
  }
}
