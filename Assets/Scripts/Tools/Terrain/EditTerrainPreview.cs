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

public class EditTerrainPreview : MonoBehaviour
{
  [SerializeField] GameObject previewObject;

  List<GameObject> previewObjects = new List<GameObject>();

  void Start()
  {
    previewObject.GetComponentInChildren<Renderer>().material.SetColor("_MainTint", Color.white);
  }

  public void SetVisibility(bool on)
  {
    if (gameObject != null)
    {
      gameObject.SetActive(on);
    }
  }

  public void ClearPreview()
  {
    foreach (GameObject go in previewObjects)
    {
      go.SetActive(false);
    }
  }

  internal void UpdateCells(HashSet<Util.Tuple<Cell, CellValue>> operationCandidates)
  {
    int counter = 0;
    foreach (Util.Tuple<Cell, CellValue> cv in operationCandidates)
    {
      counter++;
      if (counter > previewObjects.Count)
      {
        previewObjects.Add(Instantiate(previewObject, GetCellCenter(cv.first), Quaternion.identity));
      }
      else
      {
        previewObjects[counter - 1].SetActive(true);
        previewObjects[counter - 1].transform.position = GetCellCenter(cv.first);
      }
    }

    if (previewObjects.Count > counter)
    {
      for (int i = counter; i < previewObjects.Count; i++)
      {
        previewObjects[i].SetActive(false);
      }
    }

  }

  internal void DestroyPreviewObjects()
  {
    foreach (GameObject go in previewObjects)
    {
      Destroy(go);
    }
    previewObjects.Clear();
  }
}
