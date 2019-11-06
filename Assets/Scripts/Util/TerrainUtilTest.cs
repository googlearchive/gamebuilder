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
using UnityEngine.TestTools;
using NUnit.Framework;
using static GameBuilder.TerrainUtil;
using GameBuilder;

public class TerrainUtilTest
{

  [Test]
  public void TestRenderingBlockBasic()
  {
    var b = new RenderingBlock();
    Int3 u = new Int3(1, 2, 3);
    Int3 v = new Int3(4, 5, 6);
    Int3 w = new Int3(0, 0, 0);
    Assert.AreEqual(0, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Set(u, Matrix4x4.identity);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(v);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(u);
    Assert.AreEqual(0, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Set(u, Matrix4x4.identity);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
    b.Set(v, Matrix4x4.identity);
    Assert.AreEqual(2, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(w);
    Assert.AreEqual(2, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(u);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
    b.Set(w, Matrix4x4.identity);
    Assert.AreEqual(2, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
    b.Clear(v);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(u);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(w);
    Assert.AreEqual(0, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
  }

  [Test]
  public void TestRemoveInMiddle()
  {
    var b = new RenderingBlock();
    Int3 u = new Int3(1, 2, 3);
    Int3 v = new Int3(4, 5, 6);
    Int3 w = new Int3(0, 0, 0);

    b.Set(u, Matrix4x4.identity);
    b.Set(v, Matrix4x4.identity);
    b.Set(w, Matrix4x4.identity);
    // u, v, w
    Assert.AreEqual(3, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(v);
    // u, w
    Assert.AreEqual(2, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Set(v, Matrix4x4.identity);
    // u, w, v
    Assert.AreEqual(3, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(u);
    // w, v
    Assert.AreEqual(2, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Set(u, Matrix4x4.identity);
    // w, v, u
    Assert.AreEqual(3, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());

    b.Clear(u);
    b.Clear(v);
    b.Clear(w);
    Assert.AreEqual(0, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
  }

  [Test]
  public void TestReplace()
  {
    var b = new RenderingBlock();
    Int3 u = new Int3(1, 2, 3);

    b.Set(u, Matrix4x4.identity);
    b.Set(u, Matrix4x4.identity);
    b.Set(u, Matrix4x4.identity);
    Assert.AreEqual(1, b.GetNumOccupied());
    Assert.IsTrue(b.CheckInvariants());
  }
}