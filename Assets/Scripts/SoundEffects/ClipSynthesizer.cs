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
using UnityEngine;

// Synthesizes an AudioClip from a SynthParams struct.
public class ClipSynthesizer
{
  const int SAMPLE_RATE = 44100;
  const int SAMPLES_PER_SLICE_MIN = 300;
  const int SAMPLES_PER_SLICE_MAX = 3000;
  const float MAX_CHANGE_PER_SAMPLE = 0.05f;
  const float MAX_VOLUME_CHANGE_PER_SAMPLE = 0.05f;

  // Natural logarithm of the frequency of a C4 (as in music).
  const float C4_LN = 5.56693129191f;
  // Natural logarithm step per semitone.
  const float SEMITONE_LN = 0.05776067245167f;

  private static LFSRNoiseGen noiseGen = new LFSRNoiseGen();

  // PCM samples (each between -1.0 and 1.0).
  private float[] samples;
  // The read position (Unity expects to treat this is as "stream", reading piece by piece)
  private int readPos = 0;
  // The synth params that we are synthesizing from.
  private SynthParams synthParams;
  // The AudioClip that we generated.
  private AudioClip audioClip;

  public ClipSynthesizer(SynthParams synthParams)
  {
    this.synthParams = synthParams;
    Synthesize();
  }

  // Reads samples starting at the current read position. This will advance the read position.
  public void ReadSamples(float[] data)
  {
    for (int i = 0; i < data.Length; i++)
    {
      data[i] = readPos < samples.Length ? samples[readPos] : 0;
      ++readPos;
    }
  }

  // Resets the read position from 0.
  public void ResetPos()
  {
    readPos = 0;
  }

  // Synthesizes the PCM samples based on synthParams.
  private void Synthesize()
  {
    // Number of PCM samples per slice.
    int samplesPerSlice = SAMPLES_PER_SLICE_MIN +
       (SynthParams.SPEED_MAX - synthParams.speed) * (SAMPLES_PER_SLICE_MAX - SAMPLES_PER_SLICE_MIN) / SynthParams.SPEED_MAX;
    samplesPerSlice = Mathf.Clamp(samplesPerSlice, SAMPLES_PER_SLICE_MIN, SAMPLES_PER_SLICE_MAX);
    // Total number of PCM samples.
    int sampleCount = SynthParams.NUM_SLICES * samplesPerSlice;
    // Allocate the samples.
    samples = new float[sampleCount];
    // Current volume. We keep track of this in order to make smooth changes in volume.
    float volume = synthParams.volume[0];
    // Are we at a good cut-off point to switch frequency?
    bool pitchChangeOk = true;
    // Pitch that we are currently emitting. This is the raw pitch index, not the frequency.
    int curPitch = 0;
    // Sample# on which we started to emit the current pitch.
    int curPitchStartSampleIndex = 0;

    for (int i = 0; i < sampleCount; i++)
    {
      int slice = i / samplesPerSlice;

      // Change pitch? Only if we are at a good cut-off point to change, meaning
      // we are not in the middle of a cycle.
      if (pitchChangeOk && synthParams.pitch[slice] != curPitch)
      {
        curPitch = synthParams.pitch[slice];
        curPitchStartSampleIndex = i;
        pitchChangeOk = false;
      }
      float thisSample = 0;
      // The center pitch is a C4, and each pitch step is a semitone.
      int semitonesAboveC4 = curPitch - SynthParams.PITCH_MAX / 2;
      // Calculate frequency in Hz based on the pitch number.
      float freqHz = Mathf.Exp(C4_LN + semitonesAboveC4 * SEMITONE_LN);
      // # of samples in one period of the wave
      float samplesPerPeriod = SAMPLE_RATE / freqHz;
      // Relative offset (0 to 1) of this sample in the current period.
      float offset = ((i - curPitchStartSampleIndex) % samplesPerPeriod) / samplesPerPeriod;
      float offsetNext = ((i + 1 - curPitchStartSampleIndex) % samplesPerPeriod) / samplesPerPeriod;
      switch (synthParams.waveShape)
      {
        case SynthWaveShape.SINE:
          thisSample = Mathf.Sin(offset * Mathf.PI);
          break;
        case SynthWaveShape.TRIANGLE:
          thisSample = offset < 0.25f ? offset * 4 :
              offset < 0.75f ? 1 - (offset - 0.25f) * 4 :
              -1 + (offset - 0.75f) * 4;
          break;
        case SynthWaveShape.SQUARE:
          thisSample = offset < 0.5 ? .4f : -.4f;
          break;
        case SynthWaveShape.NOISE:
          thisSample = -0.5f + noiseGen.NextValue(SynthParams.PITCH_MAX - curPitch);
          break;
        default:
          break;
      }
      // Modulate the sample by the current volume.
      thisSample *= (volume / (float)SynthParams.VOLUME_MAX);
      // Write the sampel.
      samples[i] = Mathf.Clamp(thisSample, -1, 1);
      // Change the volume soothly.
      volume = LimitedChange(volume, synthParams.volume[slice], MAX_VOLUME_CHANGE_PER_SAMPLE);
      // If the next offset is smaller than the current, it's the end of a cycle, so
      // we can signal that a pitch change is ok. Otherwise it's not ok.
      pitchChangeOk = offsetNext < offset;
    }
  }

  // Generates a Unity AudioClip. If one was already generated, returns the cached one.
  public AudioClip GetAudioClip()
  {
    if (audioClip != null)
    {
      return audioClip;
    }
    audioClip = AudioClip.Create("Synth", samples.Length, 1, SAMPLE_RATE, false, (float[] data) => ReadSamples(data));
    // Note that we can't delete samples[] here, because audioClip (at least in theory) expects to read them
    // asynchronously at some point in the future using our delegate above.
    return audioClip;
  }

  private static float LimitedChange(float init, float target, float maxDelta)
  {
    if (Mathf.Abs(init - target) <= maxDelta)
    {
      return target;
    }
    else if (init < target)
    {
      return init + maxDelta;
    }
    return init - maxDelta;
  }
}
