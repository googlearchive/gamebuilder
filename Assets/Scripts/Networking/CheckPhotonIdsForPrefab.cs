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

public class CheckPhotonIdsForPrefab : MonoBehaviour
{
#if USE_PUN
  void Awake()
  {
    var view = GetComponent<PhotonView>();

    if (view == null)
    {
      StartCoroutine(DeferredThrow($"Expected a PhotonView component on {this.name}. Developer needs to do File -> Save Project."));
    }

    if (view.instantiationId != -1)
    {
      StartCoroutine(DeferredThrow($"Expected -1 view.instantiationId. Prefabs should get these assigned at run time. Developer needs to do File -> Save Project."));
    }

    if (view.viewID != 0)
    {
      StartCoroutine(DeferredThrow($"Expected 0 viewID! Prefabs should get these assigned at run time. Developer needs to do File -> Save Project."));
    }
  }

  IEnumerator DeferredThrow(string msg)
  {
    yield return new WaitForSecondsRealtime(0.5f);
  }
#endif
}