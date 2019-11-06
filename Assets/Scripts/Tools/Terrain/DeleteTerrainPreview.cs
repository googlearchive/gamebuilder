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

public class DeleteTerrainPreview : MonoBehaviour
{
  [SerializeField] GameObject previewObject;

  HashSet<GameObject> previewObjects = new HashSet<GameObject>();

  void Start()
  {
    previewObject.GetComponentInChildren<Renderer>().material.SetColor("_MainTint", Color.red);
  }


  public void SetVisibility(bool on)
  {
    if (gameObject != null)
    {
      gameObject.SetActive(on);
    }
  }

  public void AddCellToPreview(Cell cell)
  {
    previewObjects.Add(Instantiate(previewObject, GetCellCenter(cell), Quaternion.identity));
  }

  public void ClearPreview()
  {
    foreach (GameObject go in previewObjects)
    {
      Destroy(go);
    }
    previewObjects.Clear();
  }
}
