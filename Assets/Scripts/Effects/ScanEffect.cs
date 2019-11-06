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

public class ScanEffect : MonoBehaviour
{
  public Transform originTransform;
  [SerializeField] AudioSource audioSource;

  public System.Action<VoosActor> OnScanComplete;
  public void Scan(VoosActor actor)
  {
    Debug.Assert(actor != null);

    if (scanRoutine != null) StopCoroutine(scanRoutine);
    scanRoutine = StartCoroutine(ScanRoutine(actor));
    audioSource.Play();
  }

  Coroutine scanRoutine;
  IEnumerator ScanRoutine(VoosActor actor)
  {
    float timeMod = 3;

    Quaternion arcBegin = Quaternion.Euler(0, -Mathf.PI / 2f, 0);
    Quaternion arcEnd = Quaternion.Euler(0, Mathf.PI / 2f, 0);

    transform.localScale = Vector3.zero;

    float lerpVal = 0;
    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * timeMod);
      Bounds actorBounds = actor.ComputeWorldRenderBounds();
      Vector3 actorCentroid = actorBounds.center;

      Quaternion rot = Quaternion.Lerp(arcBegin, arcEnd, lerpVal) * Quaternion.LookRotation(actorCentroid - transform.position);
      // transform.LookAt(actorCentroid);
      transform.rotation = rot;

      float dist = Vector3.Distance(transform.position, actorCentroid);
      transform.localScale = new Vector3(.2f, actorBounds.size.y, dist);
      yield return null;
    }
    transform.localScale = Vector3.zero;
    OnScanComplete?.Invoke(actor);
  }


  void Update()
  {
    transform.position = originTransform.position;
  }
}
