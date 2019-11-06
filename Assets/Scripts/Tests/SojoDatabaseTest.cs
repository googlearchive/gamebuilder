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
public class SojoDatabaseTest
{
  SojoDatabase database = new SojoDatabase();

  [Test]
  public void GetById()
  {
    database.PutSojo(new Sojo("abc", "First one", SojoType.SoundEffect, "Content1"));
    database.PutSojo(new Sojo("def", "Second one", SojoType.SoundEffect, "Content2"));
    database.PutSojo(new Sojo("ghi", "Third one", SojoType.SoundEffect, "Content3"));
    Assert.NotNull(database.GetSojoById("abc"));
    Assert.AreEqual("Second one", database.GetSojoById("def").name);
    Assert.IsNull(database.GetSojoById("foo"));
  }

  [Test]
  public void GetByName()
  {
    database.PutSojo(new Sojo("abc", "First one", SojoType.SoundEffect, "Content1"));
    database.PutSojo(new Sojo("def", "Second one", SojoType.SoundEffect, "Content2"));
    database.PutSojo(new Sojo("ghi", "Third one", SojoType.SoundEffect, "Content3"));
    Assert.NotNull(database.GetSojoByName("First one"));
    Assert.AreEqual("Content2", database.GetSojoByName("Second one").content);
  }

  [Test]
  public void MultipleWithSameName()
  {
    Assert.IsNull(database.GetSojoByName("Name"));
    database.PutSojo(new Sojo("id1", "Name", SojoType.SoundEffect, "Content"));
    database.PutSojo(new Sojo("id2", "Name", SojoType.SoundEffect, "Content"));
    database.PutSojo(new Sojo("id3", "Name", SojoType.SoundEffect, "Content"));
    Assert.AreEqual("Content", database.GetSojoByName("Name").content);
    database.DeleteSojo("id2");
    Assert.AreEqual("Content", database.GetSojoByName("Name").content);
    database.DeleteSojo("id1");
    Assert.AreEqual("Content", database.GetSojoByName("Name").content);
    Assert.AreEqual("id3", database.GetSojoByName("Name").id);
  }

  [Test]
  public void SaveAndLoad()
  {
    database.PutSojo(new Sojo("id1", "Name1", SojoType.SoundEffect, "Content1"));
    database.PutSojo(new Sojo("id2", "Name2", SojoType.ParticleEffect, "Content2"));
    database.PutSojo(new Sojo("id3", "Name3", SojoType.SoundEffect, "Content3"));
    SojoDatabase.Saved saved = database.Save();
    database = new SojoDatabase();
    database.Load(saved);
    Assert.AreEqual("Name1", database.GetSojoById("id1").name);
    Assert.AreEqual("Name2", database.GetSojoById("id2").name);
    Assert.AreEqual("Name3", database.GetSojoById("id3").name);
    Assert.AreEqual(SojoType.SoundEffect, database.GetSojoById("id1").contentType);
    Assert.AreEqual(SojoType.ParticleEffect, database.GetSojoById("id2").contentType);
  }
}