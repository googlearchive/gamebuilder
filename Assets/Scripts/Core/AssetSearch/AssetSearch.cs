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

public class AssetSearch : MonoBehaviour
{
  [SerializeField] PolySearchManager polySearchManager;
  [SerializeField] GisSearchManager gisSearchManager;
  [SerializeField] PrefabSearchManager prefabSearchManager;

  BuiltinPrefabLibrary prefabLibrary;
  VoosEngine voosEngine;
  BehaviorSystem behaviorSystem;
  SoundEffectSystem soundEffectSystem;
  ParticleEffectSystem particleEffectSystem;

  void Awake()
  {
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref prefabLibrary);
    Util.FindIfNotSet(this, ref soundEffectSystem);
    Util.FindIfNotSet(this, ref particleEffectSystem);
  }

  public void DefaultSearch(OnActorableSearchResult resultCallback)
  {
    polySearchManager.DefaultSearch(resultCallback);
    gisSearchManager.Search("dog", resultCallback, null);
  }

  public void AddPrefabsProcessor(OnActorableSearchResult resultCallback)
  {
    prefabSearchManager.AddPrefabsProcessor(resultCallback);
  }

  public void Search(
    string _searchString,
    OnActorableSearchResult resultCallback)
  {
    //TODO: Cancel current search if still going?
    polySearchManager.Search(_searchString, resultCallback, (found) => { });
    gisSearchManager.Search(_searchString, resultCallback, (found) => { });
  }

  public void PrefabSearch(
  string _searchString,
  OnActorableSearchResult resultCallback)
  {

    prefabSearchManager.Search(_searchString, resultCallback);
  }

  public ActorableSearchResult TurnPrefabIntoSearchResult(ActorPrefab prefab)
  {
    return prefabSearchManager.TurnPrefabIntoSearchResult(prefab, AssetType.Actor);
  }

  public void RequestPolySearchResult(string polyID, OnActorableSearchResult resultCallback)
  {
    polySearchManager.RequestResultByID(polyID, resultCallback);
  }

  public void RequestRenderable(RenderableReference reference, RenderableRequestEventHandler requestCallback)
  {
    if (reference.assetType == AssetType.Poly)
    {
      polySearchManager.RequestRenderable(reference.uri, requestCallback);
    }
    else if (reference.assetType == AssetType.Image)
    {
      gisSearchManager.RequestRenderable(reference.uri, requestCallback);
    }
    else if (reference.assetType == AssetType.Actor || reference.assetType == AssetType.AssetPack)
    {
      prefabSearchManager.RequestRenderable(reference.uri, requestCallback);
    }

    else
    {
      Debug.LogError("Request renderable did not know how to handle this asset type");
    }
  }

  public VoosActor RequestActor(ActorableSearchResult _requestedResult, Vector3 rootSpawnPosition, Quaternion additionalRotation, Vector3 spawnScale)
  {
    Quaternion spawnRotation = additionalRotation * _requestedResult.preferredRotation;

    // NOTE: We could also have additionalScale here, in which case we'd want to multiply it with preferredLocalScale and apply it.
    // If, for example, the new tool gave you the ability to scale (like it lets you rotate now).

    if (_requestedResult.renderableReference.assetType == AssetType.Actor || _requestedResult.renderableReference.assetType == AssetType.AssetPack)
    {
      return _requestedResult.actorPrefab.Instantiate(voosEngine, behaviorSystem, rootSpawnPosition, spawnRotation,
      setupActor =>
      {
        // IMPORTANT! The setupActor could be a child of the hierarchy! So the
        // position is not necessarily rootSpawnPosition.

        setupActor.SetSpawnPosition(setupActor.transform.position);
        setupActor.SetSpawnRotation(setupActor.transform.rotation);

        // Post-setup for effect results
        string pfxId = _requestedResult.pfxId;
        if (pfxId != null)
        {
          setupActor.SetPfxId(pfxId);
          ParticleEffect pfx = particleEffectSystem.GetParticleEffect(pfxId);
          if (pfx != null) setupActor.SetDisplayName(pfx.name);
        }
        string sfxId = _requestedResult.sfxId;
        if (sfxId != null)
        {
          setupActor.SetSfxId(sfxId);
          SoundEffect sfx = soundEffectSystem.GetSoundEffect(sfxId);
          if (sfx != null) setupActor.SetDisplayName(sfx.name);
        }
      });
    }
    else
    {
      return voosEngine.CreateActor(rootSpawnPosition, spawnRotation, actor =>
      {
        if (_requestedResult.forceConcave) actor.SetUseConcaveCollider(true);
        actor.SetLocalScale(spawnScale);
        actor.SetDisplayName(_requestedResult.name);
        actor.SetRenderableUri(_requestedResult.renderableReference.uri);
      });
    }

  }

  internal ActorableSearchResult GetBuiltInSearchResult(string v)
  {
    return prefabSearchManager.TurnPrefabIntoSearchResult(prefabLibrary.Get(v), AssetType.AssetPack);
  }
}

public delegate void OnActorableSearchResult(ActorableSearchResult newResult);

public delegate VoosActor ActorEventHandler();
public delegate void RenderableRequestEventHandler(GameObject _gameobject);
public delegate Vector3 PreferredScaleFunction(GameObject renderable);

// All info you need to retrieve a renderable for a search result.
[System.Serializable]
public struct RenderableReference
{
  public AssetType assetType;
  public string uri;
}

public struct ActorableSearchResult
{
  public RenderableReference renderableReference;
  public ActorPrefab actorPrefab;
  public string name;
  public bool forceConcave;
  public Texture2D thumbnail;

  public string pfxId;
  public string sfxId;

  // This is used to set the ACTOR scale. NOT the renderable scale.
  public PreferredScaleFunction preferredScaleFunction;

  public Quaternion preferredRotation;

  public override string ToString()
  {
    return $"name: {name}, uri: {renderableReference.uri}";
  }

  public Vector3 GetRenderableOffset()
  {
    return actorPrefab != null ? actorPrefab.GetRenderableOffset() : Vector3.zero;
  }
}

public enum AssetType
{
  Actor,
  Image,
  Poly,
  AssetPack,
  None
}