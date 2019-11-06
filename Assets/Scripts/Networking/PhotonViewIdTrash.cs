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
using UnityEngine.Networking;

public class PhotonViewIdTrash : MonoBehaviour
{
  List<int> viewIdsToUnallocate = new List<int>(100);

  // We should wait at least 1 frame before unallocating a view ID. So use
  // this to track the last frame.
  int frameOfLastPut = -1;

  public void Clear()
  {
    // OK trash em all!
    foreach (int viewId in viewIdsToUnallocate)
    {
      PhotonNetwork.UnAllocateViewID(viewId);
    }
    viewIdsToUnallocate.Clear();
  }

  void Update()
  {
    if (Time.frameCount >= frameOfLastPut + 1)
    {
      Clear();
    }
  }
  void OnDestroy()
  {
    Clear();
  }
  public void Put(int viewId)
  {
    viewIdsToUnallocate.Add(viewId);
    frameOfLastPut = Time.frameCount;
  }
}