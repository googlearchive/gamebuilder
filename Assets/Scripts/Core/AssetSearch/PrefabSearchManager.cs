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

public class PrefabSearchManager : MonoBehaviour
{
  [SerializeField] Sprite defaultThumbnail;

  const int MAX_ASSETS_RETURNED = 10;

  AssetCache assetCache;
  SceneActorLibrary sceneActorLibrary;
  BuiltinPrefabLibrary builtinPrefabLibrary;

  void Awake()
  {
    Util.FindIfNotSet(this, ref assetCache);
    Util.FindIfNotSet(this, ref sceneActorLibrary);
    Util.FindIfNotSet(this, ref builtinPrefabLibrary);
  }

  public void RequestRenderable(string uri, RenderableRequestEventHandler requestCallback)
  {
    assetCache.Get(uri, (entry) => requestCallback(entry.GetAssetClone()));
  }

  public void AddPrefabsProcessor(OnActorableSearchResult resultCallback)
  {
    foreach (ActorPrefab prefab in sceneActorLibrary.GetAll())
    {
      resultCallback(TurnPrefabIntoSearchResult(prefab, AssetType.Actor));
    }

    builtinPrefabLibrary.AddPrefabsProcessor((ActorPrefab prefab) =>
    {
      resultCallback(TurnPrefabIntoSearchResult(prefab, AssetType.AssetPack));
    });
  }

  public ActorableSearchResult TurnPrefabIntoSearchResult(ActorPrefab prefab, AssetType assetType)
  {

    ActorableSearchResult _newresult = new ActorableSearchResult();
    _newresult.preferredRotation = prefab.GetRenderableRotation();
    _newresult.preferredScaleFunction = (go) => prefab.GetLocalScale();
    _newresult.renderableReference.assetType = assetType;
    _newresult.name = prefab.GetLabel();
    _newresult.renderableReference.uri = prefab.GetRenderableUri();

    if (prefab.GetThumbnail() != null)
    {
      _newresult.thumbnail = prefab.GetThumbnail();
    }
    else
    {
      // TODO just take a texture
      _newresult.thumbnail = defaultThumbnail.texture;
    }
    _newresult.actorPrefab = prefab;

    return _newresult;
  }

  public void Search(string searchstring, OnActorableSearchResult resultCallback)
  {

    foreach (ActorPrefab prefab in sceneActorLibrary.GetAll())
    {
      string stringForSearch = prefab.GetLabel().ToLower();

      if (stringForSearch.Contains(searchstring.ToLower()))
      {
        resultCallback(TurnPrefabIntoSearchResult(prefab, AssetType.Actor));
      }
    }


    foreach (ActorPrefab prefab in builtinPrefabLibrary.GetAll())
    {
      string stringForSearch = prefab.GetLabel().ToLower();

      if (stringForSearch.Contains(searchstring.ToLower()))
      {
        resultCallback(TurnPrefabIntoSearchResult(prefab, AssetType.AssetPack));
      }
    }
  }
}