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

public class ToolEffectController : MonoBehaviour, UserBody.NetworkableToolEffect
{

  [SerializeField] ToolEffect targetEffect;
  [SerializeField] ToolEffect originEffect;
  [SerializeField] LineRenderer lineRenderer;
  [SerializeField] GameObject particleSystemObject;

  [SerializeField] Renderer[] renderers;
  [SerializeField] Renderer particleRenderer;

  public Transform originTransform;

  public float startDuration = .1f;
  public float endDuration = .1f;

  [SerializeField] AudioSource audioSource;
  [SerializeField] AudioClip startClip;
  [SerializeField] AudioClip endClip;

  Vector3 originPosition;
  Vector3 targetPosition;
  float lineRendererPercent = 0;

  bool toolActivate = false;
  bool toolRoutineActive = false;

  public void ToolActivate(bool on)
  {
    toolActivate = on;
    if (on)
    {
      if (toolRoutine != null) StopCoroutine(toolRoutine);
      toolRoutine = StartCoroutine(ToolRoutine());
    }
  }

  public bool IsActive()
  {
    return toolActivate;
  }

  public void UpdateTargetPosition(Vector3 newpos)
  {
    targetPosition = newpos;
  }

  public Vector3 GetTargetPosition()
  {
    return targetPosition;
  }

  public void SetSpatialAudio(bool on)
  {
    if (audioSource != null) audioSource.spatialBlend = on ? 1 : 0;
  }

  Coroutine toolRoutine;
  IEnumerator ToolRoutine()
  {
    toolRoutineActive = true;
    lineRenderer.enabled = true;
    lineRenderer.SetPositions(new Vector3[] { Vector3.zero, Vector3.zero });

    particleSystemObject.SetActive(true);

    if (audioSource != null)
    {
      audioSource.PlayOneShot(startClip);
      audioSource.Play();
    }


    //starts at origin
    originEffect.StartEffect();

    //line renderer extend
    float timer = 0;
    while (timer < startDuration)
    {
      timer += Time.unscaledDeltaTime;
      lineRendererPercent = Mathf.Clamp01(timer / startDuration);
      yield return null;
    }

    //starts at target
    targetEffect.StartEffect();

    //loop just sits here
    while (toolActivate)
    {
      yield return null;
    }

    //shutdown on target
    targetEffect.EndEffect();


    if (audioSource != null)
    {
      audioSource.Stop();
      audioSource.PlayOneShot(endClip);
    }


    //line renderer sucks back up
    timer = 0;
    while (timer < endDuration)
    {
      timer += Time.unscaledDeltaTime;
      lineRendererPercent = 1 - Mathf.Clamp01(timer / startDuration);
      yield return null;
    }

    //sthudown on origin
    originEffect.EndEffect();
    lineRenderer.enabled = false;
    particleSystemObject.SetActive(false);
    toolRoutineActive = false;
  }



  public void ExplicitLateUpdate()
  {
    if (!toolRoutineActive) return;

    originPosition = originTransform.position;
    targetEffect.SetPositionAndRotation(targetPosition, Quaternion.LookRotation(originPosition - targetPosition));
    originEffect.SetPositionAndRotation(originPosition, Quaternion.LookRotation(targetPosition - originPosition));

    Vector3[] lineVecs = new Vector3[] { originPosition, Vector3.Lerp(originPosition, targetPosition, lineRendererPercent) };
    lineRenderer.SetPositions(lineVecs);


  }

  void UserBody.NetworkableToolEffect.SetActive(bool active)
  {
    if (active != this.IsActive())
    {
      this.ToolActivate(active);
    }
  }

  void UserBody.NetworkableToolEffect.OnLateUpdate()
  {
    this.ExplicitLateUpdate();
  }

  void UserBody.NetworkableToolEffect.SetRayOriginTransform(Transform origin)
  {
    this.originTransform = origin;
  }

  GameObject UserBody.NetworkableToolEffect.GetGameObject()
  {
    // IMPORTANT: Do NOT use ?. - we need to use (this == null) explicitly because Unity's null stuff is BAD.
    if (this == null) return null;
    return this.gameObject;
  }

  void UserBody.NetworkableToolEffect.SetReceivedTargetActor(VoosActor targetActor)
  {
    if (targetActor != null)
    {
      Util.SetLayerRecursively(this.gameObject, targetActor.gameObject.layer);

      RaycastHit hit;
      if (targetActor.Raycast(this.originTransform.position, out hit, 100.0f))
      {
        UpdateTargetPosition(hit.point);
      }
      else
      {
        UpdateTargetPosition(targetActor.transform.position);
      }
    }

  }

  public void SetTint(Color tint)
  {
    Color color = ArtUtil.GetHologramColor(tint);
    foreach (Renderer renderer in renderers)
    {
      renderer.material.SetColor("_MainTint", color);
    }
    particleRenderer.material.SetColor("_Color", color);
  }

  void UserBody.NetworkableToolEffect.SetTargetPosition(Vector3 position)
  {
    // Ignore - we use target actor.
  }
}

public abstract class ToolEffect : MonoBehaviour
{
  public abstract void StartEffect();
  public abstract void SetPositionAndRotation(Vector3 position, Quaternion rotation);
  public abstract void EndEffect(); //not used by fail
}
