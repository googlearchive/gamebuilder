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
using static TerrainManager;

public class CopyPasteTerrainPreview : MonoBehaviour
{
  TerrainRendering terrainRendering;
  // TerrainManager terrain;
  [SerializeField] GameObject previewSelectionObject;
  [SerializeField] Material previewMaterial;

  HashSet<Preview> previewSet = new HashSet<Preview>();

  struct Preview
  {
    public Cell cell;
    public CellValue value;
    public GameObject selectionObject;
    public GameObject pasteObject;
  }


  public void Setup()
  {
    Util.FindIfNotSet(this, ref terrainRendering);
    // Util.FindIfNotSet(this, ref terrain);
    // CreateBasicPreviewBlock();
  }

  public void SetVisibility(bool on)
  {
    gameObject.SetActive(on);
  }

  public void UpdateOffset(Vector3 offset)
  {
    foreach (Preview preview in previewSet)
    {
      preview.pasteObject.transform.position = GetCellCenter(preview.cell) + offset;
    }
  }

  public void SelectCellGroup(IEnumerable<Util.Tuple<Cell, CellValue>> cellsAndValues)
  {
    foreach (Util.Tuple<Cell, CellValue> cv in cellsAndValues)
    {

      Preview preview = new Preview
      {
        cell = cv.first,
        value = cv.second,
        selectionObject = Instantiate(previewSelectionObject, GetCellCenter(cv.first), Quaternion.identity),
        pasteObject = GetPreviewBlock(cv.second, GetCellCenter(cv.first))
      };

      previewSet.Add(preview);
    }
  }

  public void Clear()
  {
    foreach (Preview preview in previewSet)
    {
      Destroy(preview.selectionObject);
      Destroy(preview.pasteObject);
    }
    previewSet.Clear();
  }

  GameObject GetPreviewBlock(CellValue value, Vector3 pos)
  {

    GameObject previewObject = Instantiate(
        terrainRendering.GetPrefabForBlockType(value.blockType, BlockStyle.SolidColor0));
    previewObject.SetActive(true);
    previewObject.transform.localPosition = pos;
    previewObject.transform.localRotation = Quaternion.identity;
    previewObject.GetComponentInChildren<Renderer>().material = previewMaterial;
    foreach (Collider col in previewObject.GetComponentsInChildren<Collider>())
    {
      GameObject.Destroy(col);
    }

    previewObject.transform.rotation = GetBlockDirectionAsQuaternion(value.direction);
    return previewObject;
  }
}
