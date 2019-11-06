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

public class DotNetTests
{

  [Test]
  public void DotNetTestsSimplePasses()
  {
    // Use the Assert class to test conditions.
  }

  // A UnityTest behaves like a coroutine in PlayMode
  // and allows you to yield null to skip a frame in EditMode
  [UnityTest]
  public IEnumerator DotNetTestsWithEnumeratorPasses()
  {
    // Use the Assert class to test conditions.
    // yield to skip a frame
    yield return null;
  }

  [Test]
  public void TestGuidSerialize()
  {
    var guid = System.Guid.NewGuid();
    byte[] bytes = guid.ToByteArray();
    Assert.AreEqual(16, bytes.Length);
    Debug.Log(guid.ToString("N"));
    Debug.Log(guid.ToString("N").Length);

    var guid2 = System.Guid.NewGuid();
    Assert.AreNotEqual(guid, guid2);

    var deserGuid = new System.Guid(bytes);
    Assert.AreEqual(guid, deserGuid);
  }

  [Test]
  public void TestUnderscoreOrder()
  {
    string guid = "5879dd67214e439ab1229c534cced49e";
    string gameRules = "__GameRules__";
    Assert.Less(gameRules.ToLower(), guid.ToLower());
  }

  [Test]
  public void ArrayOfVecs()
  {
    Vector3[] v = new Vector3[]
    {
      Vector3.zero, Vector3.zero
    };

    v[0].x = 5;
    Assert.AreEqual(5, v[0].x);
  }

  struct TwoVecs
  {
    public Vector3 a;
    public Vector3 b;
  }

  [Test]
  public void TwoVecsInStruct()
  {
    TwoVecs foo = new TwoVecs { a = Vector3.zero, b = Vector3.zero };
    foo.a.x = 5;
    Assert.AreEqual(5, foo.a.x);
  }

  [Test]
  public void Uris()
  {
    System.Uri uri = new System.Uri("poly:abc123");
    Assert.AreEqual("poly", uri.Scheme);
    Assert.AreEqual("abc123", uri.PathAndQuery);
  }

  [Test]
  public void ImageUris()
  {
    System.Uri uri = new System.Uri("http://www.foo.com/bar.png");
    Assert.AreEqual("http", uri.Scheme);
    Assert.AreEqual("http://www.foo.com/bar.png", uri.ToString());
  }

  [System.Serializable]
  class BaseJsonable
  {
    public int x;
  }

  [System.Serializable]
  class SubclassJsonable : BaseJsonable
  {
    public int y;
  }

  [Test]
  public void JsonWithInheritance()
  {
    SubclassJsonable foo = new SubclassJsonable();
    foo.x = 12;
    foo.y = 34;

    string json = JsonUtility.ToJson(foo);

    SubclassJsonable result = JsonUtility.FromJson<SubclassJsonable>(json);
    Assert.AreEqual(12, result.x);
    Assert.AreEqual(34, result.y);
  }

  [System.Serializable]
  class XYStruct
  {
    public int x;
    public int y;
  }

  [Test]
  public void JsonStructMisMatchTest()
  {
    XYStruct xy = new XYStruct { x = 12, y = 34 };
    string json = JsonUtility.ToJson(xy);
    Debug.Log(json);

    BaseJsonable result = JsonUtility.FromJson<BaseJsonable>(json);
    Assert.AreEqual(12, result.x);
  }

  [Test]
  public void ListStruct()
  {
    List<XYStruct> vecs = new List<XYStruct>();
    vecs.Add(new XYStruct { x = 2, y = 3 });
    vecs[0].x = 42;
    Assert.AreEqual(42, vecs[0].x);
  }
}
