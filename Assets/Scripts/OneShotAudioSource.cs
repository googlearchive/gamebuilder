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

public class OneShotAudioSource : MonoBehaviour
{
  AudioSource source;

  public static GameObject Play(GameObject parent, AudioClip clip, float volume, bool spatialized)
  {
    GameObject obj = new GameObject("OneShotAudioSource " + clip.name);
    obj.transform.SetParent(parent.transform, false);
    obj.AddComponent<OneShotAudioSource>().Setup(clip, volume, spatialized);
    return obj;
  }

  void Setup(AudioClip clip, float volume, bool spatialized)
  {
    source = gameObject.AddComponent<AudioSource>();
    source.spatialize = spatialized;
    source.spatialBlend = spatialized ? 1 : 0; // 1 = Full 3D
    source.PlayOneShot(clip, volume);
  }

  void Update()
  {
    if (!source.isPlaying)
    {
      GameObject.Destroy(gameObject);
    }
  }
}
