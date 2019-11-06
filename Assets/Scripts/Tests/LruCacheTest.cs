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
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class LruCacheTest
{
  [Test]
  public void TestBasics()
  {
    LruCache<string> cache = new LruCache<string>(3);

    Assert.IsNull(cache.Get("one"));
    // Start out with Latin (unum, duo, tria):
    cache.Put("one", "unum");
    cache.Put("two", "duo");
    cache.Put("three", "tria");
    Assert.AreEqual("duo", cache.Get("two"));
    Assert.AreEqual("unum", cache.Get("one"));
    Assert.AreEqual("tria", cache.Get("three"));

    // Overwrite with Spanish (uno, dos, tres):
    cache.Put("one", "uno");
    cache.Put("two", "dos");
    cache.Put("three", "tres");
    Assert.AreEqual("dos", cache.Get("two"));
    Assert.AreEqual("tres", cache.Get("three"));
    Assert.AreEqual("uno", cache.Get("one"));

    // Should overflow and evict "two", as it's the least recently used.
    cache.Put("four", "cuatro");
    Assert.AreEqual("uno", cache.Get("one"));
    Assert.IsNull(cache.Get("two"));
    Assert.AreEqual("tres", cache.Get("three"));
    Assert.AreEqual("cuatro", cache.Get("four"));

    // Let's try to get "tres" evicted.
    cache.Get("one");  // "one" should no longer be a candidate for eviction.
    cache.Put("five", "cinco");
    Assert.AreEqual("uno", cache.Get("one"));
    Assert.IsNull(cache.Get("two"));
    Assert.IsNull(cache.Get("three"));
    Assert.AreEqual("cuatro", cache.Get("four"));
    Assert.AreEqual("cinco", cache.Get("five"));
  }

  [Test]
  public void TestEvictionDelegate()
  {
    string evictions = "";
    LruCache<string> cache = new LruCache<string>(3, (evictKey, evictValue) => evictions += evictKey + evictValue);
    cache.Put("Blue", "Bleu");
    cache.Put("Red", "Rouge");
    cache.Put("Green", "Vert");
    cache.Get("Blue");
    cache.Put("Yellow", "Jaune");
    cache.Put("White", "Blanc");
    cache.Put("Black", "Noir");
    Assert.AreEqual("RedRougeGreenVertBlueBleu", evictions);
  }
}