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

public class CreateToolPreview : MonoBehaviour
{
  [SerializeField] Material previewMaterial;
  GameObject resultRenderable;
  AssetSearch assetSearch;

  Material materialCopy;

  void Awake()
  {
    Util.FindIfNotSet(this, ref assetSearch);
    materialCopy = Instantiate(previewMaterial);
  }

  public GameObject GetResultRenderable()
  {
    return resultRenderable;
  }

  public void SetRenderableByReference(
    RenderableReference renderableReference,
    Quaternion addlRotation,
    Vector3 renderableOffset,
    Quaternion renderableRotation,
    PreferredScaleFunction scaleFunction,
    System.Func<bool> isStillValid)
  {
    if (resultRenderable != null)
    {
      Destroy(resultRenderable);
    }

    assetSearch.RequestRenderable(renderableReference,
      renderableObj =>
      {
        if (renderableObj == null) return; // Just to be safe..
        // Can be called async - so we could be gone already.
        if (this == null || this.gameObject == null) return;

        if (isStillValid())
        {
          renderableObj.transform.SetParent(transform);
          renderableObj.transform.rotation = renderableRotation * addlRotation;
          renderableObj.transform.localPosition = renderableOffset;
          renderableObj.transform.localScale = scaleFunction(renderableObj);
          MakeLookLikeGhost(renderableObj);

          if (resultRenderable != null) Destroy(resultRenderable);
          resultRenderable = renderableObj;
          renderableObj.SetActive(true);
        }
        else
        {
          Destroy(renderableObj);
          Debug.Log("renderable callback out of date");
        }
      });
  }

  public void SetTint(Color color)
  {
    materialCopy.SetColor("_MainTint", ArtUtil.GetHologramColor(color));
  }

  void MakeLookLikeGhost(GameObject newModelObject)
  {
    foreach (MeshRenderer _render in newModelObject.GetComponentsInChildren<MeshRenderer>(true))
    {
      _render.material = materialCopy;
    }

    foreach (SkinnedMeshRenderer _render in newModelObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    {
      _render.material = materialCopy;
    }

    foreach (Collider col in newModelObject.GetComponentsInChildren<Collider>(true))
    {
      GameObject.Destroy(col);
    }

    foreach (ParticleSystem ps in newModelObject.GetComponentsInChildren<ParticleSystem>(true))
    {
      GameObject.Destroy(ps);
    }
  }
}
