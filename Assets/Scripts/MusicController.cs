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

public class MusicController : MonoBehaviour
{
  [SerializeField] AudioSource[] audioSources;

  [SerializeField] AudioClip[] clipsA;
  [SerializeField] AudioClip[] clipsB;
  [SerializeField] AudioClip[] clipsC;
  [SerializeField] AudioClip[] clipsD;
  [SerializeField] AudioClip[] clipsE;
  [SerializeField] AudioClip[] clipsF;

  AudioClip lastClip;
  Stem lastStem;

  //alternating between two audio sources with Unity recommended technique
  int audioSourceIndex = 0;

  //using AudioSettings.dspTime and PlayScheduled for high accuracy
  double nextClipTime = 0;

  //amount of time we want to give a clip to load
  //shortest clip we have is 9 seconds so I figure 5 is fine right?
  const float BUFFER_TIME = 5f;

  enum Stem
  {
    A,
    B,
    C,
    D,
    E,
    F
  }

  void Awake()
  {
    if (FindObjectsOfType<MusicController>().Length > 1)
    {
      Destroy(gameObject);
    }
  }

  void Start()
  {
    DontDestroyOnLoad(this.gameObject);
    nextClipTime = AudioSettings.dspTime + 1f;

    //one off for start
    if (Random.value < 0.5f)
    { //A clip
      lastStem = Stem.A;
    }
    else
    { //D clip
      lastStem = Stem.D;
    }

    int index; //not used for this first one...
    lastClip = GetClipByStem(lastStem, out index);
    audioSources[audioSourceIndex].clip = lastClip;
    audioSources[audioSourceIndex].PlayScheduled(nextClipTime);
    nextClipTime += lastClip.length;

  }

  AudioClip GetClipByStem(Stem stem, out int index)
  {
    switch (stem)
    {
      case Stem.A:
        index = Random.Range(0, clipsA.Length);
        return clipsA[index];
      case Stem.B:
        index = Random.Range(0, clipsB.Length);
        return clipsB[index];
      case Stem.C:
        index = Random.Range(0, clipsC.Length);
        return clipsC[index];
      case Stem.D:
        index = Random.Range(0, clipsD.Length);
        return clipsD[index];
      case Stem.E:
        index = Random.Range(0, clipsE.Length);
        return clipsE[index];
      case Stem.F:
        index = Random.Range(0, clipsF.Length);
        return clipsF[index];
      default:
        index = -1;
        return null;
    }
  }

  void FlipAudioSource()
  {
    audioSourceIndex = 1 - audioSourceIndex;
  }

  void QueueNextClip()
  {
    Stem newStem;
    AudioClip newClip;

    switch (lastStem)
    {
      case Stem.A:
        QueueFromA(out newStem, out newClip);
        break;
      case Stem.B:
        QueueFromB(out newStem, out newClip);
        break;
      case Stem.C:
        QueueFromC(out newStem, out newClip);
        break;
      case Stem.D:
        QueueFromD(out newStem, out newClip);
        break;
      case Stem.E:
        QueueFromE(out newStem, out newClip);
        break;
      case Stem.F:
        QueueFromF(out newStem, out newClip);
        break;
      default:
        newStem = Stem.A;
        newClip = null;
        break;
    }

    FlipAudioSource();
    audioSources[audioSourceIndex].clip = newClip;
    audioSources[audioSourceIndex].PlayScheduled(nextClipTime);
    nextClipTime += newClip.length;

    // Debug.Log($"QUEUEING NEXT CLIP: {newStem},{newClip}");

    lastStem = newStem;
    lastClip = newClip;
  }

  void QueueFromA(out Stem newStem, out AudioClip newClip)
  {
    int index;
    if (Random.value < .25f)
    {
      newStem = Stem.A;
      newClip = GetClipByStem(newStem, out index);
      if (newClip == lastClip)
      {
        index = (index + 1) % clipsA.Length;
        newClip = clipsA[index];
      }
    }
    else
    {
      newStem = Stem.B;
      newClip = GetClipByStem(newStem, out index);
    }
  }

  void QueueFromB(out Stem newStem, out AudioClip newClip)
  {
    int index;
    float rand = Random.value;
    if (rand < .25f)
    {
      newStem = Stem.A;
      newClip = GetClipByStem(newStem, out index);
    }
    else if (rand < .75f)
    {
      newStem = Stem.B;
      newClip = GetClipByStem(newStem, out index);
      if (newClip == lastClip)
      {
        index = (index + 1) % clipsB.Length;
        newClip = clipsB[index];
      }
    }
    else
    {
      newStem = Stem.C;
      newClip = GetClipByStem(newStem, out index);
    }
  }

  void QueueFromC(out Stem newStem, out AudioClip newClip)
  {
    int index;
    if (Random.value < .25f)
    {
      newStem = Stem.B;
      newClip = GetClipByStem(newStem, out index);
    }
    else
    {
      newStem = Stem.D;
      newClip = GetClipByStem(newStem, out index);
    }
  }

  void QueueFromD(out Stem newStem, out AudioClip newClip)
  {
    int index;
    newStem = Stem.E;
    newClip = GetClipByStem(newStem, out index);
  }

  void QueueFromE(out Stem newStem, out AudioClip newClip)
  {
    int index;
    if (Random.value < .75f)
    {
      newStem = Stem.E;
      newClip = GetClipByStem(newStem, out index);
      if (newClip == lastClip)
      {
        index = (index + 1) % clipsE.Length;
        newClip = clipsE[index];
      }
    }
    else
    {
      newStem = Stem.F;
      newClip = GetClipByStem(newStem, out index);
    }
  }


  void QueueFromF(out Stem newStem, out AudioClip newClip)
  {
    int index;
    newStem = Stem.A;
    newClip = GetClipByStem(newStem, out index);
  }


  void Update()
  {
    double currentDspTime = AudioSettings.dspTime;
    if (currentDspTime + BUFFER_TIME > nextClipTime)
    {
      QueueNextClip();
    }
  }

}
