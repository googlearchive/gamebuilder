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

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Manages everything related to particle-effects: storing them, playing them, etc.
// Delegates the actual data storage to SojoSystem, but that's an implementation
// detail of this class; outside code should always interact with ParticleEffectSystem,
// never directly with SojoSystem for particle effects.
public class ParticleEffectSystem : MonoBehaviour
{
  SojoSystem sojoSystem;
  UnreliableMessageSystem unreliableMessageSystem;
  UndoStack undoStack;
  ClaimKeeper claimKeeper;
  [SerializeField] Material particleMaterial;

  public event System.Action<string> onParticleEffectChanged;
  public event System.Action<string> onParticleEffectRemoved;
  public static string PFX_CLAIM_PREFIX = "PFX:";

  [System.Serializable]
  struct ParticleSpawnRequest
  {
    public string pfxId;
    public Vector3 position;
    public Vector3 euler;
    public float scale;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref sojoSystem);
    Util.FindIfNotSet(this, ref unreliableMessageSystem);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref claimKeeper);

    sojoSystem.onSojoPut += (sojo) =>
    {
      if (sojo.contentType == SojoType.ParticleEffect)
      {
        onParticleEffectChanged?.Invoke(sojo.id);
      }
    };

    sojoSystem.onSojoDelete += (sojo) =>
    {
      if (sojo.contentType == SojoType.ParticleEffect)
      {
        onParticleEffectRemoved?.Invoke(sojo.id);
      }
    };

    unreliableMessageSystem.AddHandler<ParticleSpawnRequest>(
      NetworkingEventCodes.PARTICLE_EFFECT, OnNetworkSpawnParticleRequest);
  }

  // Returns a listing of all available particle effects (IDs and names).
  public List<ParticleEffectListing> ListAll()
  {
    List<ParticleEffectListing> result = new List<ParticleEffectListing>();
    foreach (Sojo sojo in sojoSystem.GetAllSojosOfType(SojoType.ParticleEffect))
    {
      result.Add(new ParticleEffectListing(sojo.id, sojo.name));
    }
    return result;
  }

  // Gets a particle effect by ID.
  public ParticleEffect GetParticleEffect(string id)
  {
    Sojo sojo = sojoSystem.GetSojoById(id);
    if (sojo == null)
    {
      return null;
    }
    if (sojo.contentType != SojoType.ParticleEffect)
    {
      throw new System.Exception("SOJO is not a particle effect: " + sojo);
    }
    return new ParticleEffect(id, sojo.name, JsonUtility.FromJson<ParticleEffectContent>(sojo.content));
  }

  // Gets a particle effect by name (not unique).
  public ParticleEffect GetParticleEffectByName(string name)
  {
    Sojo sojo = sojoSystem.GetSojoByName(name);
    if (sojo == null)
    {
      return null;
    }
    if (sojo.contentType != SojoType.ParticleEffect)
    {
      throw new System.Exception("SOJO is not a particle effect: " + sojo);
    }
    return new ParticleEffect(sojo.id, sojo.name, JsonUtility.FromJson<ParticleEffectContent>(sojo.content));
  }

  // Saves a particle effect. If the ID corresponds to an existing particle effect, that effect will
  // be overwritten; if not, a new particle effect will be created.
  public void PutParticleEffect(ParticleEffect particleEffect)
  {
    ParticleEffect prevEffect = GetParticleEffect(particleEffect.id);

    ClaimableUndoUtil.ClaimableUndoItem undoItem = new ClaimableUndoUtil.ClaimableUndoItem();
    undoItem.resourceId = undoItem.resourceId = ParticleEffectSystem.PFX_CLAIM_PREFIX + particleEffect.id;
    undoItem.resourceName = particleEffect.name;
    if (prevEffect != null)
    {
      undoItem.label = $"Change particle effect {prevEffect.name}";
    }
    else
    {
      undoItem.label = $"Add particle effect {particleEffect.name}";
    }
    undoItem.doIt = () =>
    {
      sojoSystem.PutSojo(new Sojo(particleEffect.id, particleEffect.name,
      SojoType.ParticleEffect, JsonUtility.ToJson(particleEffect.content)));
    };
    undoItem.undo = () =>
    {
      if (prevEffect != null)
      {
        sojoSystem.PutSojo(new Sojo(prevEffect.id, prevEffect.name,
        SojoType.ParticleEffect, JsonUtility.ToJson(prevEffect.content)));
      }
      else
      {
        sojoSystem.DeleteSojo(particleEffect.id);
      }
    };
    undoItem.cannotDoReason = () =>
    {
      return null;
    };
    ClaimableUndoUtil.PushUndoForResource(undoStack, claimKeeper, undoItem);
  }

  // Deletes the particle effect with that ID.
  public void DeleteParticleEffect(string particleEffectId)
  {
    Sojo sojo = sojoSystem.GetSojoById(particleEffectId);
    if (sojo == null) return;

    ClaimableUndoUtil.ClaimableUndoItem undoItem = new ClaimableUndoUtil.ClaimableUndoItem();
    undoItem.resourceId = PFX_CLAIM_PREFIX + particleEffectId;
    undoItem.resourceName = sojo.name;
    undoItem.label = $"Deleting particle effect {sojo.name}";
    undoItem.cannotDoReason = () => { return null; };
    undoItem.cannotUndoReason = () => { return null; };
    undoItem.doIt = () =>
    {
      sojoSystem.DeleteSojo(particleEffectId);
    };
    undoItem.undo = () =>
    {
      sojoSystem.PutSojo(sojo);
    };

    ClaimableUndoUtil.PushUndoForResource(undoStack, claimKeeper, undoItem);
  }

  public ParticleSystem SpawnParticleEffect(
    ParticleEffect particleFx, Vector3 position, Vector3 eulerDeg, float scale, bool stream = false)
  {
    unreliableMessageSystem.Send<ParticleSpawnRequest>(NetworkingEventCodes.PARTICLE_EFFECT, new ParticleSpawnRequest
    {
      pfxId = particleFx.id,
      position = position,
      euler = eulerDeg,
      scale = scale,
    });
    return SpawnParticleEffectLocal(particleFx, position, eulerDeg, scale, stream);
  }

  // Another player requested that a particle be spawned.
  private void OnNetworkSpawnParticleRequest(ParticleSpawnRequest request)
  {
    ParticleEffect effect = GetParticleEffect(request.pfxId);
    if (effect == null)
    {
      return;
    }
    SpawnParticleEffectLocal(effect, request.position, request.euler, request.scale);
  }

  public ParticleSystem SpawnParticleEffectLocal(ParticleEffect particleFx, Vector3 position, Vector3 eulerDeg, float scale, bool stream = false)
  {
    Quaternion rotation = Quaternion.Euler(eulerDeg);
    // Particle system defaults to facing left, so make it point upward before rotation
    rotation = rotation * Quaternion.AngleAxis(90, Vector3.left);
    GameObject go = new GameObject(particleFx.name);
    go.transform.position = position;
    go.transform.rotation = rotation;
    go.transform.localScale = Vector3.one * scale;

    ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
    particleSystem.Stop();
    ParticleSystem.MainModule main = particleSystem.main;
    SetParticleSystemProps(particleFx.content, go, stream);
    if (!stream) main.loop = false;
    particleSystem.Play();

    if (!stream) StartCoroutine(DestroyParticleEffectWhenDone(particleSystem));
    return particleSystem;
  }

  public ParticleSystem SpawnParticleEffectForSimulation(
    ParticleEffect particleFx, Vector3 position, Quaternion rotation, Transform parent, bool stream = false)
  {
    GameObject go = new GameObject(particleFx.name);
    go.transform.position = position;
    go.transform.rotation = rotation;
    go.transform.parent = parent;

    ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
    particleSystem.Stop();
    SetParticleSystemProps(particleFx.content, go, stream);
    particleSystem.Play();

    return particleSystem;
  }

  private void SetParticleSystemProps(ParticleEffectContent content, GameObject go, bool stream)
  {
    ParticleSystem particleSystem = go.GetComponent<ParticleSystem>();
    ParticleSystem.MainModule main = particleSystem.main;
    main.simulationSpace = content.isLocal ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
    main.duration = content.duration;
    main.startLifetime = content.duration;
    main.gravityModifier = content.gravityModifier;

    ParticleSystem.ShapeModule shape = particleSystem.shape;
    switch (content.shapeType)
    {
      case ParticleEffectContent.ShapeType.Cone:
        shape.shapeType = ParticleSystemShapeType.Cone;
        break;
      case ParticleEffectContent.ShapeType.Sphere:
        shape.shapeType = ParticleSystemShapeType.Sphere;
        break;
      case ParticleEffectContent.ShapeType.Box:
        shape.shapeType = ParticleSystemShapeType.Box;
        break;
      case ParticleEffectContent.ShapeType.Circle:
        shape.shapeType = ParticleSystemShapeType.Circle;
        break;
      case ParticleEffectContent.ShapeType.Rectangle:
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        break;
    }
    shape.scale = Vector3.one * content.shapeSize;

    ParticleSystem.EmissionModule emission = particleSystem.emission;
    emission.enabled = true;
    if (stream)
    {
      emission.rateOverTime = content.burstCount;
    }
    else
    {
      emission.rateOverTime = 0;
      emission.SetBursts(
        new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, content.burstCount) });
    }

    ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
    velocityOverLifetime.enabled = true;
    velocityOverLifetime.speedModifier = content.speed;

    ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = particleSystem.rotationOverLifetime;
    rotationOverLifetime.enabled = true;
    SetRotationOverLifetime(rotationOverLifetime, content.rotationOverLifetime);

    ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
    sizeOverLifetime.enabled = true;
    SetSizeOverLifetime(sizeOverLifetime, content.startSize, content.endSize);

    ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
    colorOverLifetime.enabled = true;
    colorOverLifetime.color = ColorTuplesToGradient(content.colorOverLifetime);

    ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
    renderer.material = particleMaterial;
  }

  private IEnumerator DestroyParticleEffectWhenDone(ParticleSystem effect)
  {
    ParticleSystem.MainModule main = effect.main;
    yield return new WaitForSeconds(main.duration);
    Destroy(effect.gameObject);
  }

  private static void SetRotationOverLifetime(
    ParticleSystem.RotationOverLifetimeModule rotation, List<ParticleEffectContent.FloatStop> stops)
  {
    AnimationCurve rotationCurve = new AnimationCurve();
    foreach (ParticleEffectContent.FloatStop stop in stops)
    {
      rotationCurve.AddKey(stop.position, stop.value);
    }
    rotation.z = new ParticleSystem.MinMaxCurve(1f, rotationCurve);
  }

  private static void SetSizeOverLifetime(
    ParticleSystem.SizeOverLifetimeModule size, float startSize, float endSize)
  {
    AnimationCurve scaleCurve = new AnimationCurve();
    scaleCurve.AddKey(0, startSize);
    scaleCurve.AddKey(1, endSize);
    size.size = new ParticleSystem.MinMaxCurve(1f, scaleCurve);
  }

  private static Gradient ColorTuplesToGradient(List<ParticleEffectContent.ColorStop> colors)
  {
    Gradient colorGradient = new Gradient();
    GradientColorKey[] gradientColorKeys = new GradientColorKey[colors.Count];
    GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[colors.Count];
    for (int i = 0; i < colors.Count; i++)
    {
      ParticleEffectContent.ColorStop stop = colors[i];
      gradientColorKeys[i] = new GradientColorKey(stop.value, stop.position);
      gradientAlphaKeys[i] = new GradientAlphaKey(stop.value.a, stop.position);
    }
    colorGradient.SetKeys(gradientColorKeys, gradientAlphaKeys);
    return colorGradient;
  }
}

// A particle effect listing (ID and name).
public struct ParticleEffectListing
{
  public string id;
  public string name;
  public ParticleEffectListing(string id, string name)
  {
    this.id = id;
    this.name = name;
  }
}

// A particle effect.
public class ParticleEffect
{
  public string id;
  public string name;
  public ParticleEffectContent content;
  public ParticleEffect(string id, string name, ParticleEffectContent content)
  {
    this.id = id;
    this.name = name;
    this.content = content;
  }
}

// The content of a particle effect, that is, the part of it that actually indicate
// how it looks (as opposed to ID and name, which are just metadata). This is
// what gets serialized as the content of the particle effect in SojoSystem.
[System.Serializable]
public class ParticleEffectContent
{
  public bool isLocal;
  public float duration;
  public float speed;
  public float startSize = 1;
  public float endSize = 1;
  public ShapeType shapeType;
  public float shapeSize = 1;
  public List<Vector3Stop> velocityOverLifetimeDEPRECATED = new List<Vector3Stop>();
  public List<FloatStop> rotationOverLifetime = new List<FloatStop>();
  public List<FloatStop> sizeOverLifetimeDEPRECATED = new List<FloatStop>();
  public List<ColorStop> colorOverLifetime = new List<ColorStop>();
  public int burstCount;
  public float gravityModifier;

  [System.Serializable]
  public enum ShapeType
  {
    Cone,
    Sphere,
    Box,
    Circle,
    Rectangle
  }

  [System.Serializable]
  public class Vector3Stop : ISerializationCallbackReceiver
  {
    public Vector3 value;
    public float position;
    public string id;

    public Vector3Stop(Vector3 value, float position) : this(System.Guid.NewGuid().ToString(), value, position) { }

    public Vector3Stop(string id, Vector3 value, float position)
    {
      this.id = id;
      this.value = value;
      this.position = position;
    }

    public void OnBeforeSerialize()
    { }

    public void OnAfterDeserialize()
    {
      if (id == null)
      {
        id = System.Guid.NewGuid().ToString();
      }
    }
  }

  [System.Serializable]
  public class FloatStop : ISerializationCallbackReceiver
  {
    public float value;
    public float position;
    public string id;

    public FloatStop(float value, float position) : this(System.Guid.NewGuid().ToString(), value, position) { }

    public FloatStop(string id, float value, float position)
    {
      this.id = id;
      this.value = value;
      this.position = position;
    }

    public void OnBeforeSerialize()
    { }

    public void OnAfterDeserialize()
    {
      if (id == null)
      {
        id = System.Guid.NewGuid().ToString();
      }
    }
  }

  [System.Serializable]
  public class ColorStop : ISerializationCallbackReceiver
  {
    public Color value;
    public float position;
    public string id;

    public ColorStop(Color value, float position) : this(System.Guid.NewGuid().ToString(), value, position) { }

    public ColorStop(string id, Color value, float position)
    {
      this.id = id;
      this.value = value;
      this.position = position;
    }

    public void OnBeforeSerialize()
    { }

    public void OnAfterDeserialize()
    {
      if (id == null)
      {
        id = System.Guid.NewGuid().ToString();
      }
    }
  }
}

