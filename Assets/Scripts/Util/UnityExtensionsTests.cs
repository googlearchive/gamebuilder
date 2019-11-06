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

public class UnityExtensionsTests
{
  [Test]
  public void TestUtf16Serialize()
  {
    byte[] buffer = new byte[100];
    var writer = new UnityEngine.Networking.NetworkWriter(buffer);

    string sample = "japanese いろはに";
    writer.WriteUtf16(sample);

    var reader = new UnityEngine.Networking.NetworkReader(buffer);
    string actual = reader.ReadUtf16();

    Assert.AreEqual(sample, actual);
  }

  [Test]
  public void TestNetworkWriterSize()
  {
    var writer = new UnityEngine.Networking.NetworkWriter();
    writer.Write((byte)42);
    byte[] bytes = writer.ToArray();
    Assert.AreEqual(1, bytes.Length);
  }

  [Test]
  public void TestNetworkWriterBug()
  {
    byte[] buffer = new byte[1];
    var writer = new UnityEngine.Networking.NetworkWriter(buffer);
    writer.Write((byte)42);
    // You'd expect this to be 42...but actually it's 0 cuz NetworkWriter wrongly resized into a new buffer...
    Assert.AreEqual((byte)0, buffer[0]);
  }

  [Test]
  public void TestNetworkWriterWorkaround()
  {
    // Work around is to add 1 more byte to the end to avoid the resize...
    byte[] buffer = new byte[2];
    var writer = new UnityEngine.Networking.NetworkWriter(buffer);
    writer.Write((byte)42);
    Assert.AreEqual((byte)42, buffer[0]);
  }

  [Test]
  public void TestQuantize()
  {
    Assert.AreEqual(50f, 51f.Quantize(50f));
    Assert.AreEqual(50f, 49f.Quantize(50f));
    Assert.AreEqual(50f, 74f.Quantize(50f));
    Assert.AreEqual(50f, 26f.Quantize(50f));
    Assert.AreEqual(100f, 76f.Quantize(50f));
    Assert.AreEqual(0f, 24f.Quantize(50f));
  }
}
