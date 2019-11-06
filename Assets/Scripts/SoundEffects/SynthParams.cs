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

// Sound synthesis parameters.
// This is all that is needed to synthesize a sound effect.
[System.Serializable]
public class SynthParams
{
  // Number of "slices" of the effect. Each slice can have its own pitch
  // and volume.
  public const int NUM_SLICES = 32;
  // Maximum value for volume.
  public const int VOLUME_MAX = 16;
  // Maximum value for pitch. Each pitch increment corresponds to a semitone,
  // and the middle pitch number (32) corresponds to C4 on a piano.
  public const int PITCH_MAX = 64;
  // Maximum value for speed.
  public const int SPEED_MAX = 16;

  // Wave shape.
  public SynthWaveShape waveShape;
  // Speed setting. The higher the faster.
  public int speed;
  // Volume of each slice. From 0 to VOLUME_MAX.
  public int[] volume;
  // Pitch of each slice. From 0 to PITCH_MAX.
  public int[] pitch;

  public static SynthParams GetDefaults()
  {
    SynthParams synthParams = new SynthParams();
    synthParams.waveShape = SynthWaveShape.SQUARE;
    synthParams.speed = SPEED_MAX / 2;
    synthParams.volume = new int[NUM_SLICES];
    synthParams.pitch = new int[NUM_SLICES];
    for (int i = 0; i < NUM_SLICES; i++)
    {
      synthParams.volume[i] = VOLUME_MAX;
      synthParams.pitch[i] = PITCH_MAX / 2;
    }
    return synthParams;
  }
}

public enum SynthWaveShape
{
  // Because these are serialized as numbers, DO NOT CHANGE the enum values:
  SQUARE = 0,
  TRIANGLE = 1,
  SINE = 2,
  NOISE = 3
}
