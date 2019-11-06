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

// Linear Feedback Shift Register noise generator.
// This is a Galois LFSR based on the theory (but NOT the
// code) presented in:
// https://en.wikipedia.org/wiki/Linear-feedback_shift_register
public class LFSRNoiseGen
{
  private const uint XOR_MASK = ~0x07ffffffu; // FFFFFFU!
  // Generation register.
  private uint reg = 123; // Whatever
  // Counts up every time we generate a value.
  private uint clk = 0;

  // Generates a noise value (from 0 to 1). The divisor indicates how many times
  // samples of the noise will be repeated (the higher the divisor, the lower
  // the "perceived pitch" of the noise when heard as audio).
  public float NextValue(int divisor)
  {
    // Division by 0 will not be tolerated here.
    divisor = divisor < 1 ? 1 : divisor;
    clk++;
    // Only advance the generation when the clock is divisible by
    // the divisor -- this creates different "frequencies" of noise.
    if (clk % divisor == 0)
    {
      bool feedback = 1 == reg % 2;
      // All bits shift right, so we lose entropy here,
      // but we add it back in the next step.
      reg >>= 1;
      if (feedback)
      {
        // Invert a few of the high bits to "add entropy".
        // Note that the highest bit will always be 1,
        // since we just right-shifted.
        reg ^= XOR_MASK;
      }
      // Note that we don't care about the generator looping around here
      // after a complete period of generation -- we just continue to
      // generate numbers as if there's no tomorrow.
    }    
    return (float)((reg & 0xff) / 256.0f);
  }
}


