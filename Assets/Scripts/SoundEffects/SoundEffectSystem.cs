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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

// Manages everything related to sound-effects: storing them, playing them, etc.
// Delegates the actual data storage to SojoSystem, but that's an implementation
// detail of this class; outside code should always interact with SoundEffectSystem,
// never directly with SojoSystem for sound effects.
public class SoundEffectSystem : MonoBehaviour
{
  SojoSystem sojoSystem;
  Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();
  UserMain userMain;
  UnreliableMessageSystem unreliableMessageSystem;
  VoosEngine engine;
  UndoStack undoStack;
  ClaimKeeper claimKeeper;

  private ConcurrentQueue<Util.Tuple<SoundEffect, AudioClip>> clipsToProcess =
    new ConcurrentQueue<Util.Tuple<SoundEffect, AudioClip>>();

  public event System.Action<string> onSoundEffectChanged;
  public event System.Action<string> onSoundEffectRemoved;
  public event System.Action<string> onSoundEffectLoaded;
  public static string SFX_CLAIM_PREFIX = "SFX:";

  WorkshopAssetSource workshopAssetSource;

  [System.Serializable]
  struct SfxPlayRequest
  {
    public string sfxId;
    public string actorName;
    public Vector3 position;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref sojoSystem);
    Util.FindIfNotSet(this, ref unreliableMessageSystem);
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref claimKeeper);
    Util.FindIfNotSet(this, ref workshopAssetSource);
    unreliableMessageSystem.AddHandler<SfxPlayRequest>(NetworkingEventCodes.SFX, OnNetworkSfxRequest);
    sojoSystem.onSojoPut += OnSojoPut;
    sojoSystem.onSojoDelete += OnSojoDelete;
  }

  void OnDestroy()
  {
    if (sojoSystem != null)
    {
      sojoSystem.onSojoPut -= OnSojoPut;
      sojoSystem.onSojoDelete -= OnSojoDelete;
    }
    if (unreliableMessageSystem != null)
    {
      unreliableMessageSystem.RemoveHandler(NetworkingEventCodes.SFX);
    }
  }

  // Returns a listing of all available sound effects (IDs and names).
  public List<SoundEffectListing> ListAll()
  {
    List<SoundEffectListing> result = new List<SoundEffectListing>();
    foreach (Sojo sojo in sojoSystem.GetAllSojosOfType(SojoType.SoundEffect))
    {
      result.Add(new SoundEffectListing(sojo.id, sojo.name));
    }
    return result;
  }

  // Gets a sound effect by ID.
  public SoundEffect GetSoundEffect(string id)
  {
    Sojo sojo = sojoSystem.GetSojoById(id);
    if (sojo == null)
    {
      return null;
    }
    if (sojo.contentType != SojoType.SoundEffect)
    {
      throw new System.Exception("SOJO is not a sound effect: " + sojo);
    }
    return new SoundEffect(id, sojo.name, JsonUtility.FromJson<SoundEffectContent>(sojo.content));
  }

  // Gets a sound effect by name (not unique).
  public SoundEffect GetSoundEffectByName(string name)
  {
    Sojo sojo = sojoSystem.GetSojoByName(name);
    if (sojo == null)
    {
      return null;
    }
    if (sojo.contentType != SojoType.SoundEffect)
    {
      throw new System.Exception("SOJO is not a sound effect: " + sojo);
    }
    return new SoundEffect(sojo.id, sojo.name, JsonUtility.FromJson<SoundEffectContent>(sojo.content));
  }

  // Saves a sound effect. If the ID corresponds to an existing sound effect, that effect will
  // be overwritten; if not, a new sound effect will be created.
  public void PutSoundEffect(SoundEffect soundEffect)
  {
    SoundEffect prevEffect = GetSoundEffect(soundEffect.id);

    ClaimableUndoUtil.ClaimableUndoItem undoItem = new ClaimableUndoUtil.ClaimableUndoItem();
    undoItem.resourceId = undoItem.resourceId = SoundEffectSystem.SFX_CLAIM_PREFIX + soundEffect.id;
    undoItem.resourceName = soundEffect.name;
    if (prevEffect != null)
    {
      undoItem.label = $"Change sound effect {prevEffect.name}";
    }
    else
    {
      undoItem.label = $"Add sound effect {soundEffect.name}";
    }
    undoItem.doIt = () =>
    {
      sojoSystem.PutSojo(new Sojo(soundEffect.id, soundEffect.name,
      SojoType.SoundEffect, JsonUtility.ToJson(soundEffect.content)));
    };
    undoItem.undo = () =>
    {
      if (prevEffect != null)
      {
        sojoSystem.PutSojo(new Sojo(prevEffect.id, prevEffect.name,
        SojoType.SoundEffect, JsonUtility.ToJson(prevEffect.content)));
      }
      else
      {
        sojoSystem.DeleteSojo(soundEffect.id);
      }
    };
    undoItem.cannotDoReason = () =>
    {
      return null;
    };
    ClaimableUndoUtil.PushUndoForResource(undoStack, claimKeeper, undoItem);
  }

  // Deletes the sound effect with that ID.
  public void DeleteSoundEffect(string soundEffectId)
  {
    Sojo sojo = sojoSystem.GetSojoById(soundEffectId);
    if (sojo == null) return;

    ClaimableUndoUtil.ClaimableUndoItem undoItem = new ClaimableUndoUtil.ClaimableUndoItem();
    undoItem.resourceId = SFX_CLAIM_PREFIX + soundEffectId;
    undoItem.resourceName = sojo.name;
    undoItem.label = $"Deleting particle effect {sojo.name}";
    undoItem.cannotDoReason = () => { return null; };
    undoItem.cannotUndoReason = () => { return null; };
    undoItem.doIt = () =>
    {
      sojoSystem.DeleteSojo(soundEffectId);
    };
    undoItem.undo = () =>
    {
      sojoSystem.PutSojo(sojo);
    };

    ClaimableUndoUtil.PushUndoForResource(undoStack, claimKeeper, undoItem);
  }

  public void PlaySoundEffect(SoundEffect sfx, VoosActor actor, Vector3 position)
  {
    PlaySoundEffectLocal(sfx, actor, position);
    unreliableMessageSystem.Send<SfxPlayRequest>(NetworkingEventCodes.SFX, new SfxPlayRequest
    {
      actorName = actor != null ? actor.GetName() : null,
      sfxId = sfx.id,
      position = position
    });
  }

  // Note: EITHER actor or position must be specified.\
  // Note that this already respects the user's SFX volume setting. The volumeScale
  // arg is an ADDITIONAL scale to apply, if desired.
  public void PlaySoundEffectLocal(SoundEffect sfx, VoosActor actor, Vector3 position, float volumeScale = 1)
  {
    // This can fail when just joining a multiplayer game..so just silently fail.
    try
    {
      Util.FindIfNotSet(this, ref userMain);
    }
    catch (System.Exception e)
    {
      return;
    }

    float sfxVolume = userMain.playerOptions.sfxVolume * volumeScale;

    AudioClip clip;
    // If it's in the cache, play it. If it's not, it's because it isn't loaded yet
    // (we proactively cache all sounds).
    if (audioClipCache.TryGetValue(sfx.id, out clip))
    {
      if (actor != null)
      {
        actor.PlayClip(clip, sfxVolume, sfx.content.spatialized);
      }
      else
      {
        AudioSource.PlayClipAtPoint(clip, position, sfxVolume);
      }
    }
    else
    {
      Debug.LogWarning("Sound " + sfx.id + " (" + sfx.name + ") not loaded yet. Ignoring.");
    }
  }

  public AudioClip GetAudioClip(string sfxId)
  {
    AudioClip value = null;
    audioClipCache.TryGetValue(sfxId, out value);
    return value;
  }

  private void PreloadAndCacheSoundEffect(SoundEffect sfx)
  {
    if (sfx.content.effectType == SoundEffectType.Synthesized)
    {
      ClipSynthesizer synth = new ClipSynthesizer(sfx.content.synthParams);
      audioClipCache[sfx.id] = synth.GetAudioClip();
    }
    else if (sfx.content.effectType == SoundEffectType.SteamWorkshop)
    {
      workshopAssetSource.Get(sfx.content.steamWorkshopId, result => OnWorkshopAssetLoaded(sfx, result));
    }
    else
    {
      Debug.LogWarning("Unknown SFX type: " + sfx.content.effectType + " for id " + sfx.id);
    }
  }

  private void OnWorkshopAssetLoaded(SoundEffect sfx, Util.Maybe<string> result)
  {
    if (result.IsEmpty())
    {
      Debug.LogErrorFormat("Failed to load SFX {0} ({1}) from steam workshop {2}: {3}",
          sfx.id, sfx.name, sfx.content.steamWorkshopId, result.GetErrorMessage() ?? "(no error message)");
      // TODO: Retry? Warn user?
      return;
    }
    string directory = result.Value;
    string wavFilePath = Path.Combine(directory, "audio.wav");
    string oggFilePath = Path.Combine(directory, "audio.ogg");
    if (File.Exists(wavFilePath))
    {
      StartCoroutine(LoadAudioClip(sfx, wavFilePath, AudioType.WAV));
    }
    else if (File.Exists(oggFilePath))
    {
      StartCoroutine(LoadAudioClip(sfx, oggFilePath, AudioType.OGGVORBIS));
    }
    else
    {
      Debug.LogError($"Could not find WAV or OGG file inside {directory}");
    }
  }

  IEnumerator LoadAudioClip(SoundEffect sfx, string filePath, AudioType audioType)
  {
    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, audioType))
    {
      yield return www.SendWebRequest();

      if (www.isNetworkError)
      {
        Debug.LogError($"Failed to load sound at {filePath}. Error: {www.error}");
      }
      else
      {
        var clip = DownloadHandlerAudioClip.GetContent(www);
        clipsToProcess.Enqueue(new Util.Tuple<SoundEffect, AudioClip>(sfx, clip));
      }
    }

  }

  void Update()
  {
    Util.Tuple<SoundEffect, AudioClip> tuple;
    while (clipsToProcess.TryDequeue(out tuple))
    {
      OnAudioClipLoaderDone(tuple.first, tuple.second);
    }
  }

  private void OnAudioClipLoaderDone(SoundEffect sfx, AudioClip clip)
  {
    audioClipCache[sfx.id] = clip;
    Debug.LogFormat("Successfully loaded and cached audio clip {0} ({1})", sfx.id, sfx.name);
    onSoundEffectLoaded?.Invoke(sfx.id);
  }

  // Another player requested that a sound be played.
  private void OnNetworkSfxRequest(SfxPlayRequest request)
  {
    SoundEffect effect = GetSoundEffect(request.sfxId);
    if (effect == null)
    {
      return;
    }
    VoosActor actor = string.IsNullOrEmpty(request.actorName) ? null : engine.GetActor(request.actorName);
    PlaySoundEffectLocal(effect, actor, request.position);
  }

  private void OnSojoPut(Sojo sojo)
  {
    if (sojo.contentType != SojoType.SoundEffect) return;
    audioClipCache.Remove(sojo.id);
    SoundEffect effect = GetSoundEffect(sojo.id);
    if (effect != null)
    {
      PreloadAndCacheSoundEffect(effect);
    }
    onSoundEffectChanged?.Invoke(sojo.id);
  }

  private void OnSojoDelete(Sojo sojo)
  {
    if (sojo.contentType != SojoType.SoundEffect) return;
    audioClipCache.Remove(sojo.id);
    onSoundEffectRemoved?.Invoke(sojo.id);
  }
}

// A sound effect listing (ID and name).
public struct SoundEffectListing
{
  public string id;
  public string name;
  public SoundEffectListing(string id, string name)
  {
    this.id = id;
    this.name = name;
  }
}

// A sound effect.
public class SoundEffect
{
  public string id;
  public string name;
  public SoundEffectContent content;
  public SoundEffect(string id, string name, SoundEffectContent content)
  {
    this.id = id;
    this.name = name;
    this.content = content;
  }
}

// Types of sound effect.
public enum SoundEffectType
{
  // Synth sound made in the built-in UI.
  Synthesized,
  // Sound file from a Steam Workshop ID.
  SteamWorkshop
}

// The content of a sound effect, that is, the part of it that actually indicate
// how it sounds (as opposed to ID and name, which are just metadata). This is
// what gets serialized as the content of the sound in SojoSystem.
[System.Serializable]
public class SoundEffectContent
{
  // Type of sound effect.
  public SoundEffectType effectType;
  // Not-null if and only if effectType == EffectType.Synthesized
  public SynthParams synthParams;
  // Only valid if effectType == SoundEffectType.SteamWorkshop
  public ulong steamWorkshopId;
  // If true, this is a "3D sound".
  public bool spatialized;

  // Other types of content can be added here in the future.

  // Creates a SoundEffectContent based on synth parameters.
  public static SoundEffectContent NewWithSynthParams(SynthParams synthParams)
  {
    SoundEffectContent content = new SoundEffectContent();
    content.effectType = SoundEffectType.Synthesized;
    content.synthParams = synthParams;
    content.spatialized = true;
    return content;
  }

  public static SoundEffectContent NewWithSteamWorkshopId(ulong steamWorkshopId)
  {
    SoundEffectContent content = new SoundEffectContent();
    content.effectType = SoundEffectType.SteamWorkshop;
    content.steamWorkshopId = steamWorkshopId;
    content.spatialized = true;
    return content;
  }

  private SoundEffectContent() { }
}

