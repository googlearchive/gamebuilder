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

public static class DotNetExtensions
{
  public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
  {
    foreach (T item in items)
    {
      set.Add(item);
    }
  }

  public static bool ApproxEquals(this float x, float y, float eps)
  {
    return System.Math.Abs(x - y) < eps;
  }

  public static void SetIfMissing<K, V>(this Dictionary<K, V> dict, K key, V valueIfMissing)
  {
    if (!dict.ContainsKey(key))
    {
      dict[key] = valueIfMissing;
    }

  }
}