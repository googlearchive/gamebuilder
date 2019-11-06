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

public class UtilTest
{

  [Test]
  public void TestRuneUtils()
  {
    Assert.AreEqual(6, Util.CountRunes("123<sprite=4>56"));
    Assert.AreEqual(6, Util.CountRunes("12 <sprite=4> 6"));

    Assert.AreEqual(2, Util.ToNextTMProRuneStart("12<sprite=3>45", 1));
    Assert.AreEqual(12, Util.ToNextTMProRuneStart("12<sprite=3>45", 2));
  }

  [Test]
  public void TestClamp()
  {
    int x = 5;
    Assert.AreEqual(5, x.Clamp(1, 10));
    Assert.AreEqual(4, x.Clamp(1, 4));
    Assert.AreEqual(6, x.Clamp(6, 10));
    Assert.AreEqual(5, x.Clamp(5, 10));
    Assert.AreEqual(5, x.Clamp(1, 5));
  }

  enum FooEnum
  {
    Foo,
    Bar,
    Baz
  }

  [Test]
  public void TestEnumUtils()
  {
    Assert.AreEqual(3, Util.CountEnumValues<FooEnum>());

    HashSet<FooEnum> yieldValueSet = new HashSet<FooEnum>();
    int numYielded = 0;
    foreach (FooEnum value in Util.ValuesOf<FooEnum>())
    {
      numYielded++;
      yieldValueSet.Add(value);
    }
    Assert.AreEqual(3, numYielded);
    Assert.AreEqual(3, yieldValueSet.Count);

    Assert.AreEqual(FooEnum.Foo, Util.ParseEnum<FooEnum>("Foo"));
    Assert.AreEqual(FooEnum.Bar, Util.ParseEnum<FooEnum>("Bar"));
    Assert.AreEqual(FooEnum.Baz, Util.ParseEnum<FooEnum>("Baz"));
  }

  class TestDatabase
  {
    public Util.Table<Vector3> vectors = new Util.Table<Vector3>();

    // NOTE: This is gross, and it's mainly because I couldn't figure out how to make JsonUtility play nice with generics :(

    [System.Serializable]
    public struct Jsonable
    {
      public Vector3[] vectorValues;
      public string[] vectorIds;
    }

    public Jsonable Save()
    {
      Jsonable rv = new Jsonable();
      vectors.GetJsonables(ref rv.vectorIds, ref rv.vectorValues);
      return rv;
    }

    public void Load(Jsonable saved)
    {
      vectors.LoadJsonables(saved.vectorIds, saved.vectorValues);
    }
  }

  [Test]
  public void TestJsonTables()
  {
    TestDatabase before = new TestDatabase();
    before.vectors.Set("up", Vector3.up);
    before.vectors.Set("right", Vector3.right);
    before.vectors.Set("forward", Vector3.forward);

    string json = JsonUtility.ToJson(before.Save());
    Debug.Log(json);

    TestDatabase after = new TestDatabase();
    after.Load(JsonUtility.FromJson<TestDatabase.Jsonable>(json));
    Assert.AreEqual(Vector3.up, after.vectors.Get("up"));
    Assert.AreEqual(Vector3.right, after.vectors.Get("right"));
    Assert.AreEqual(Vector3.forward, after.vectors.Get("forward"));
  }

  [Test]
  public void TestWithBitBasic()
  {
    Assert.AreEqual(5, 1.WithBit(2, true));
    Assert.AreEqual(1, 1.WithBit(0, true));
    Assert.AreEqual(1, 5.WithBit(2, false));
    Assert.AreEqual(1, 1.WithBit(2, false));
  }

  [Test]
  public void TestUniquePass()
  {
    int[] vals = { 1, 2, 3 };
    Util.AssertAllUnique(vals);
  }

  [Test]
  public void TestUniqueFail()
  {
    int[] vals = { 1, 2, 3, 2 };
    bool threw = false;
    try
    {
      Util.AssertAllUnique(vals);
    }
    catch (System.Exception e)
    {
      threw = true;
    }
    Assert.IsTrue(threw);
  }

  // [Test]
  // public void KnownFailureTestJsonArray()
  // {
  //   Vector3[] vecs = new Vector3[] { Vector3.up, Vector3.right };
  //   string json = JsonUtility.ToJson(vecs);
  //   Vector3[] outVecs = JsonUtility.FromJson<Vector3[]>(json);
  //   Debug.Log(json);
  //   Assert.AreEqual(vecs.Length, outVecs.Length);
  // }

  struct IntString
  {
    public int x;
    public string s;
  }

  [Test]
  public void TestForSortedGroups()
  {
    IntString[] items = new IntString[] {
      new IntString{x = 0},
      new IntString{x = 1},
      new IntString{x = 1},
      new IntString{x = 2},
      new IntString{x = 2},
      new IntString{x = 2},
      new IntString{x = 3},
    };

    int expectedGroup = 0;
    int numItems = 0;

    Util.ForSortedGroups(items, x => x.x, (num, group) =>
    {
      Assert.AreEqual(expectedGroup, num);
      foreach (IntString item in group)
      {
        Assert.AreEqual(expectedGroup, item.x);
        numItems++;
      }
      expectedGroup++;
    });

    Assert.AreEqual(4, expectedGroup);
    Assert.AreEqual(items.Length, numItems);
  }

  [Test]
  public void TestForSortedGroups2()
  {
    IntString[] items = new IntString[] {
      new IntString{x = 0},
      new IntString{x = 0},
      new IntString{x = 1},
      new IntString{x = 2},
      new IntString{x = 3},
      new IntString{x = 3},
      new IntString{x = 3},
    };

    int expectedGroup = 0;
    int numItems = 0;

    Util.ForSortedGroups(items, x => x.x, (num, group) =>
    {
      Assert.AreEqual(expectedGroup, num);
      foreach (IntString item in group)
      {
        Assert.AreEqual(expectedGroup, item.x);
        numItems++;
      }
      expectedGroup++;
    });

    Assert.AreEqual(4, expectedGroup);
    Assert.AreEqual(items.Length, numItems);
  }

  [Test]
  public void TestGzip()
  {
    byte[] unzipped = new byte[1024];
    for (int i = 0; i < unzipped.Length; i++)
    {
      unzipped[i] = (byte)(i % 128);
    }
    byte[] zipped = Util.GZip(unzipped);
    Assert.Greater(zipped.Length, 0);
    Assert.LessOrEqual(zipped.Length, unzipped.Length);
    byte[] actualUnzipped = Util.UnGZip(zipped);
    Assert.AreEqual(unzipped.Length, actualUnzipped.Length);

    for (int i = 0; i < unzipped.Length; i++)
    {
      Assert.AreEqual(unzipped[i], actualUnzipped[i]);
    }
  }

  [Test]
  public void SanityCheckEnumerateLines()
  {
    string s = @"333
";
    int lines = 0;
    foreach (string l in s.EnumerateLines())
    {
      Debug.Log($"got line: <<{l}>>");
      lines++;
    }
    Assert.AreEqual(1, lines);
  }

  [Test]
  public void TestAtFractionalPosition()
  {
    int[] nums = new int[200];
    for (int i = 0; i < 200; i++)
    {
      nums[i] = i;
    }

    Assert.AreEqual(0, nums.AtFractionalPosition(0f));
    Assert.AreEqual(199, nums.AtFractionalPosition(1f));
    Assert.AreEqual(180, nums.AtFractionalPosition(0.9f));
    Assert.AreEqual(198, nums.AtFractionalPosition(0.99f));
    Assert.AreEqual(40, nums.AtFractionalPosition(0.2f));
  }

  [Test]
  public void TestExtractIdFromWorkshopUrl()
  {
    Assert.AreEqual(
      1835323700,
      Util.ExtractIdFromWorkshopUrl("https://steamcommunity.com/sharedfiles/filedetails/?id=1835323700&searchtext="));
    Assert.AreEqual(
      1835323700,
      Util.ExtractIdFromWorkshopUrl("https://steamcommunity.com/sharedfiles/filedetails/?id=1835323700"));
    Assert.AreEqual(
      1835323700,
      Util.ExtractIdFromWorkshopUrl("https://steamcommunity.com/sharedfiles/filedetails/?foo=bar&id=1835323700"));
    Assert.AreEqual(
      0,
      Util.ExtractIdFromWorkshopUrl("https://fooooo.com/sharedfiles/filedetails/?id=1835323700"));
  }


}
