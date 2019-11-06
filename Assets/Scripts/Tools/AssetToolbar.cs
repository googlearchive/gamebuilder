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

public class AssetToolbar : Toolbar
{
  [SerializeField] EditMain editMain;
  CreationLibrarySidebar creationLibrary;
  [SerializeField] ToolbarItem moreButton;
  [HideInInspector] public Transform assetObjectsParent;
  [SerializeField] Material previewMaterial;
  [SerializeField] GameObject[] toolbarTooltips;
  AssetSearch assetSearch;

  const int ASSET_COUNT = 4;

  [HideInInspector] public ActorableSearchResult[] assetToolbarAssets;
  [HideInInspector] public GameObject[] assetToolbarObjects;
  [HideInInspector] public GameObject[] assetToolbarRenderables;

  public void Setup(CreationLibrarySidebar creationLibrary)
  {
    this.creationLibrary = creationLibrary;
    for (int i = 0; i < ASSET_COUNT; i++)
    {
      int index = i;
      toolbarItems[i].SetSelect(false);
      toolbarItems[i].GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => OnMenuItemClick?.Invoke(index));
    }

    assetObjectsParent = new GameObject("assetObjects").transform;
    assetToolbarAssets = new ActorableSearchResult[ASSET_COUNT];

    moreButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
    {
      if (creationLibrary.IsOpenedOrOpening()) creationLibrary.RequestClose();
      else creationLibrary.RequestOpen();
    });
    Util.FindIfNotSet(this, ref assetSearch);

    OnMenuItemClick = SelectIndex;

    assetToolbarRenderables = new GameObject[ASSET_COUNT];
    assetToolbarObjects = new GameObject[ASSET_COUNT];
    for (int i = 0; i < assetToolbarObjects.Length; i++)
    {
      assetToolbarObjects[i] = new GameObject("new tool item");
      assetToolbarObjects[i].transform.SetParent(assetObjectsParent);
      assetToolbarObjects[i].transform.localPosition = Vector3.zero;
      assetToolbarObjects[i].transform.localRotation = Quaternion.identity;
      assetToolbarObjects[i].transform.localScale = new Vector3(1f, 1f, 1f);
      assetToolbarObjects[i].SetActive(false);
    }

    //load the toolbar with goodstuff by hand for now
    LoadDefaultAssets();
  }

  void Update()
  {
    if (moreButton.IsSelected() != creationLibrary.IsOpenedOrOpening())
    {
      moreButton.SetSelect(creationLibrary.IsOpenedOrOpening());
    }

    //shutting off the Q/E being tied to tooltips
    /*  if (editMain.ShowTooltips() != toolbarTooltips[0].activeSelf)
     {
       foreach (GameObject go in toolbarTooltips)
       {
         go.SetActive(editMain.ShowTooltips());
       }
     } */
  }

  public bool IsCurrentAssetRenderableEmpty()
  {
    return assetToolbarRenderables[currentIndex] == null;
  }

  public void UpdateAsset(ActorableSearchResult newResult)
  {
    UpdateAssetAtIndex(newResult, currentIndex);
  }

  public void UpdateAssetAtIndex(ActorableSearchResult newResult, int index)
  {
    assetToolbarAssets[index] = newResult;
    toolbarItems[index].SetTexture(newResult.thumbnail);
    EmptyAssetToolbarRenderable(index);
    assetSearch.RequestRenderable(assetToolbarAssets[index].renderableReference, renderableObj => SetAssetToolbarRenderable(renderableObj, newResult, index));
  }

  public void ResetAsset()
  {
    ActorableSearchResult newResult = assetSearch.GetBuiltInSearchResult(defaultAssets[currentIndex]);
    UpdateAssetAtIndex(newResult, currentIndex);
  }

  public override void SelectIndex(int n)
  {
    base.SelectIndex(n);
    for (int i = 0; i < assetToolbarObjects.Length; i++)
    {
      assetToolbarObjects[i].SetActive(n == i);
    }
  }

  public GameObject GetCurrentAsset()
  {
    return assetToolbarObjects[currentIndex];
  }

  void SetAssetToolbarRenderable(GameObject _gameobject, ActorableSearchResult _requestedResult, int index)
  {
    if (_requestedResult.renderableReference.uri == assetToolbarAssets[index].renderableReference.uri)
    {
      _gameobject.transform.rotation = _requestedResult.preferredRotation;
      _gameobject.transform.localScale = _requestedResult.preferredScaleFunction(_gameobject);
      SetAssetToolbarObject(index, _gameobject);
    }
    else
    {
      Destroy(_gameobject);
      Debug.Log("renderable callback out of date");
    }
  }

  void EmptyAssetToolbarRenderable(int index)
  {
    if (assetToolbarRenderables[index] != null) Destroy(assetToolbarRenderables[index]);
  }

  public ActorableSearchResult GetSelected()
  {
    return assetToolbarAssets[currentIndex];
  }

  public void SetAssetToolbarObject(int index, GameObject newModelObject)
  {
    if (assetToolbarRenderables[index] != null) Destroy(assetToolbarRenderables[index]);

    if (newModelObject != null)
    {
      assetToolbarRenderables[index] = newModelObject;
      newModelObject.transform.SetParent(assetToolbarObjects[index].transform, false);
      SetupGhostModel(newModelObject);
    }
  }

  void SetupGhostModel(GameObject newModelObject)
  {
    // Always override local position so it shows up under our frame, but leave rotation and scale alone.
    newModelObject.transform.localPosition = Vector3.zero;
    newModelObject.SetActive(true);

    foreach (MeshRenderer _render in newModelObject.GetComponentsInChildren<MeshRenderer>())
    {
      _render.material = previewMaterial;
      // Texture tex = _render.material.mainTexture;
      // _render.material.SetTexture("_MainTex", tex);
    }

    foreach (SkinnedMeshRenderer _render in newModelObject.GetComponentsInChildren<SkinnedMeshRenderer>())
    {
      _render.material = previewMaterial;
      // Texture tex = _render.material.mainTexture;
      // _render.material.SetTexture("_MainTex", tex);
    }

    foreach (Collider col in newModelObject.GetComponentsInChildren<Collider>())
    {
      GameObject.Destroy(col);
    }

    foreach (ParticleSystem ps in newModelObject.GetComponentsInChildren<ParticleSystem>())
    {
      GameObject.Destroy(ps);
    }
  }

  string[] defaultAssets = {
   "Forest/Slime",
   "Forest/Tree_01",
   "Forest/Axe",
   "Forest/Collectable_Star"
  };

  void LoadDefaultAssets()
  {
    for (int i = 0; i < 4; i++)
    {
      ActorableSearchResult newResult = assetSearch.GetBuiltInSearchResult(defaultAssets[i]);
      UpdateAssetAtIndex(newResult, i);
    }
  }

}
