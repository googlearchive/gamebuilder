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

public class JointFeedback : MonoBehaviour
{
  [SerializeField] Transform startTransform;
  [SerializeField] Transform endTransform;
  [SerializeField] LineRenderer lineRenderer;

  [SerializeField] Color color;

  Vector3[] posArray = new Vector3[] { Vector3.zero, Vector3.zero };

  public void SetPosition(int index, Vector3 position)
  {
    posArray[index] = position;
  }

  void Awake()
  {
    foreach (Renderer rend in GetComponentsInChildren<Renderer>())
    {
      rend.material.color = color;
    }


  }

  void LateUpdate()
  {
    transform.position = Vector3.Lerp(posArray[0], posArray[1], 0.5f);
    transform.LookAt(posArray[0]);

    startTransform.position = posArray[0];
    endTransform.position = posArray[1];
    startTransform.LookAt(posArray[1]);
    endTransform.LookAt(posArray[0]);


    lineRenderer.SetPositions(posArray);
  }

  public void Start()
  {
    gameObject.SetActive(true);
  }

  public void End()
  {
    gameObject.SetActive(false);
  }

  public void DumbStart()
  {
    if (!gameObject.activeSelf) Start();
  }

  public void DumbEnd()
  {
    if (gameObject.activeSelf) End();
  }
}
