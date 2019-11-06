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

#pragma once

#ifdef _WIN32

#include <Windows.h>
#include <iostream>
#include <string>

class CpuTimer
{
public:
  CpuTimer(const std::string &label) : hadError_(false), label_(label)
  {
    if (QueryPerformanceCounter(&t0_) == 0)
    {
      hadError_ = true;
    }
    if (QueryPerformanceFrequency(&frequency_) == 0)
    {
      hadError_ = true;
    }
  }

  double GetElapsedMilliSeconds()
  {
    LARGE_INTEGER t1;
    if (QueryPerformanceCounter(&t1) == 0)
    {
      hadError_ = true;
      return -1.0;
    }

    LARGE_INTEGER deltaTicks;
    deltaTicks.QuadPart = t1.QuadPart - t0_.QuadPart;

    return ((double)(deltaTicks.QuadPart * 1e3)) / frequency_.QuadPart;
  }

  ~CpuTimer()
  {
    if (hadError_)
    {
      std::cerr << "ERROR: Errors encountered while calling QueryPerformanceCounter. Do not trust timings!" << std::endl;
      return;
    }
    double dms = GetElapsedMilliSeconds();
    if (dms > 1e3)
    {
      std::cout << label_ << " " << (dms / 1e3) << " seconds" << std::endl;
    }
    else
    {
      std::cout << label_ << " " << (dms) << " milliseconds" << std::endl;
    }
  }

private:
  LARGE_INTEGER t0_;
  LARGE_INTEGER frequency_;
  bool hadError_;
  std::string label_;
};

#else

// TODO TODO
#include <iostream>
#include <string>

class CpuTimer
{
public:
  CpuTimer(const std::string &label) : hadError_(false), label_(label)
  {
  }

  double GetElapsedMilliSeconds()
  {
    return 0.0;
  }

  ~CpuTimer()
  {
  }

private:
  bool hadError_;
  std::string label_;
};

#endif
