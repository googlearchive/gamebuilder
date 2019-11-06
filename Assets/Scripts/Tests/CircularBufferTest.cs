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
using System.Collections.Generic;
using GameBuilder;

public class CircularBufferTest
{
  [Test]
  public void TestBasic()
  {
    var cb = new CircularBuffer<int>(3);
    cb.Add(1);
    Assert.AreEqual(1, cb.Count);
    cb.Add(2);
    Assert.AreEqual(2, cb.Count);
    cb.Add(3);
    Assert.AreEqual(3, cb.Count);

    Assert.IsTrue(cb.Contains(1));
    Assert.IsTrue(cb.Contains(2));
    Assert.IsTrue(cb.Contains(3));

    cb.Add(4);
    Assert.AreEqual(3, cb.Count);
    Assert.IsFalse(cb.Contains(1));
    Assert.IsTrue(cb.Contains(2));
    Assert.IsTrue(cb.Contains(3));
    Assert.IsTrue(cb.Contains(4));

    cb.Clear();
    Assert.AreEqual(0, cb.Count);
    Assert.IsFalse(cb.Contains(1));
    Assert.IsFalse(cb.Contains(2));
    Assert.IsFalse(cb.Contains(3));
    Assert.IsFalse(cb.Contains(4));
  }

  [Test]
  public void TestContainsAfterClear()
  {
    var cb = new CircularBuffer<int>(3);
    cb.Add(1);
    cb.Add(2);
    cb.Add(3);
    cb.Clear();
    Assert.IsFalse(cb.Contains(1));
    Assert.IsFalse(cb.Contains(2));
    Assert.IsFalse(cb.Contains(3));

    cb.Add(1);
    Assert.IsTrue(cb.Contains(1));
    Assert.IsFalse(cb.Contains(2));
    Assert.IsFalse(cb.Contains(3));

    cb.Add(2);
    cb.Add(3);
    Assert.IsTrue(cb.Contains(1));
    Assert.IsTrue(cb.Contains(2));
    Assert.IsTrue(cb.Contains(3));

    cb.Add(4);
    Assert.IsFalse(cb.Contains(1));
    Assert.IsTrue(cb.Contains(2));
    Assert.IsTrue(cb.Contains(3));
    Assert.IsTrue(cb.Contains(4));
  }

  [Test]
  public void TestForEach()
  {
    var cb = new CircularBuffer<int>(3);
    cb.Add(1);
    cb.Add(2);
    cb.Add(3);

    int sum = 0;
    foreach (int x in cb)
    {
      sum += x;
    }
    Assert.AreEqual(6, sum);

    cb.Add(4);
    sum = 0;
    foreach (int x in cb)
    {
      sum += x;
    }
    Assert.AreEqual(9, sum);
  }

}

