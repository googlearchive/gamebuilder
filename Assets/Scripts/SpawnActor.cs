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

public class SpawnActor : MonoBehaviour
{
  AssetSearch assetSearch;
  [SerializeField] Material spawnMaterial;
  [SerializeField] AudioSource audioSource;

  ActorableSearchResult result;
  Vector3 spawnPosition;
  Quaternion spawnRotation;
  Vector3 spawnScale;
  Material spawnMaterialInstance;
  bool isLocal;
  Vector3 renderableOffset;
  Quaternion renderableRotation;

  const string spawnDissolveVariable = "_DissolveProgress";

  GameObject previewGameobject;

  public System.Action<VoosActor> onActorCreated;
  public bool isOffstage;
  private ActorWizard newActorWizard;

  void Awake()
  {
    Util.FindIfNotSet(this, ref assetSearch);
    Util.FindIfNotSet(this, ref newActorWizard);
    spawnMaterialInstance = Instantiate(spawnMaterial);
    audioSource.pitch = Random.Range(.95f, 1.1f);
  }

  public void Setup(
    ActorableSearchResult result,
    Vector3 spawnPosition,
    Quaternion spawnRotation,
    Vector3 spawnScale,
    bool isLocal,
    Vector3 renderableOffset,
    Quaternion renderableRotation
  )
  {
    // NOTE: Bit of a hack. When we're not local, 'result' only has the renderable reference set. None of the other data is relevant.
    this.result = result;

    this.spawnPosition = spawnPosition;
    this.spawnRotation = spawnRotation;
    this.spawnScale = spawnScale;
    this.isLocal = isLocal;
    this.renderableOffset = renderableOffset;
    this.renderableRotation = renderableRotation;

    assetSearch.RequestRenderable(result.renderableReference, OnRenderable);
  }

  public void SetTint(Color color)
  {
    spawnMaterialInstance.SetColor("_Color0", ArtUtil.GetHologramColor(color));
  }

  MeshRenderer[] renderers;

  void OnRenderable(GameObject _gameobject)
  {
    // Always override local position so it shows up under our frame, but leave rotation and scale alone.
    _gameobject.transform.position = spawnPosition + renderableOffset;
    _gameobject.transform.rotation = renderableRotation * spawnRotation;
    _gameobject.transform.localScale = spawnScale;
    _gameobject.SetActive(true);

    renderers = _gameobject.GetComponentsInChildren<MeshRenderer>();

    foreach (MeshRenderer _render in renderers)
    {
      _render.material = spawnMaterialInstance;
      _render.material.SetFloat(spawnDissolveVariable, 0);
    }

    foreach (Collider col in _gameobject.GetComponentsInChildren<Collider>())
    {
      GameObject.Destroy(col);
    }

    _gameobject.transform.parent = transform;
    previewGameobject = _gameobject;
    StartCoroutine(EffectRoutine());
  }

  IEnumerator EffectRoutine()
  {
    float lerpVal = 0;
    while (lerpVal < 1)
    {
      lerpVal = Mathf.Clamp01(lerpVal + Time.unscaledDeltaTime * 2);

      foreach (MeshRenderer _render in renderers)
      {
        //_render.material = spawnMaterialInstance;
        _render.material.SetFloat(spawnDissolveVariable, lerpVal);

      }

      yield return null;
    }

    SpawnEvent();
  }

  bool destructionDesired = false;

  void SpawnEvent()
  {
    // Only actually create the actor for local. For remotes, this is purely visual.
    if (isLocal)
    {
      VoosActor _newactor = assetSearch.RequestActor(result, spawnPosition, spawnRotation, spawnScale);
      _newactor.SetSpawnPosition(_newactor.transform.position);
      _newactor.SetSpawnRotation(_newactor.transform.rotation);

      if (isOffstage) _newactor.SetPreferOffstage(true);

      onActorCreated?.Invoke(_newactor);

      newActorWizard.MaybeShow(_newactor);
    }

    destructionDesired = true;
    Destroy(previewGameobject);
  }

  void Update()
  {
    if (destructionDesired && !audioSource.isPlaying)
    {
      Destroy(gameObject);
    }
  }
}
