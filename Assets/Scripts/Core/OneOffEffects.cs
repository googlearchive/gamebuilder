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

public class OneOffEffects : MonoBehaviour
{
  [SerializeField] GameObject[] effectPrefabs;
  [SerializeField] SpawnActor actorSpawner;

  PhotonView photonView;

  void Awake()
  {
    photonView = PhotonView.Get(this);
  }

  [PunRPC]
  void TriggerRPC(int i, Vector3 position, Quaternion rotation, bool isOffstage)
  {
    GameObject newEffect = GameObject.Instantiate(effectPrefabs[i], position, rotation);
    Util.SetLayerRecursively(newEffect, isOffstage ? LayerMask.NameToLayer("PrefabWorld") : LayerMask.NameToLayer("VoosActor"));

  }

  public void Trigger(string gameObjectName, Vector3 position, Quaternion rotation, bool isOffstage)
  {
    int i = System.Array.FindIndex(effectPrefabs, prefab => { return prefab.name == gameObjectName; });

    if (i == -1)
    {
      Util.LogError($"Could not find effect prefab named ${gameObjectName}");
      return;
    }

    // Hmm we could definitely do this unreliable!
    photonView.RPC("TriggerRPC", PhotonTargets.All, i, position, rotation, isOffstage);
  }

  [PunRPC]
  void TriggerActorSpawnEffectRPC(byte assetType, string uri, Vector3 position, Quaternion rotation, Vector3 scale, Color tint)
  {
    SpawnActor spawnActor = GameObject.Instantiate(actorSpawner, position, rotation).GetComponent<SpawnActor>();
    ActorableSearchResult dummyResult = new ActorableSearchResult
    {
      renderableReference = new RenderableReference { assetType = (AssetType)assetType, uri = uri }
    };
    spawnActor.SetTint(tint);
    spawnActor.Setup(
      dummyResult, position, rotation, scale, false,
      // This is just the preview, assume these offset are baked in. To save banwidth
      Vector3.zero, Quaternion.identity
    );
  }

  public void TriggerActorSpawn(
    ActorableSearchResult result,
    Vector3 position,
    Quaternion rotation,
    Vector3 scale,
    System.Action<VoosActor> onActorCallback,
    bool isOffstage,
    Color tint,
    Vector3 renderableOffset,
    Quaternion renderableRotation)
  {
    // Do it locally with the full setup.
    SpawnActor spawnActor = GameObject.Instantiate(actorSpawner, Vector3.zero, Quaternion.identity).GetComponent<SpawnActor>();
    spawnActor.onActorCreated = onActorCallback;
    spawnActor.isOffstage = isOffstage;
    spawnActor.SetTint(tint);
    spawnActor.Setup(
      result, position, rotation, scale, true,
      renderableOffset, renderableRotation);

    // Kick off RPC for others.
    photonView.RPC("TriggerActorSpawnEffectRPC", PhotonTargets.Others, (byte)result.renderableReference.assetType, result.renderableReference.uri,
    position + renderableOffset, renderableRotation * rotation, scale, tint);

  }

}
