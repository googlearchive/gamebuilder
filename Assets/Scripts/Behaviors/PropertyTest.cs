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
using PropertyAssignment = Behaviors.PropertyAssignment;

public class PropertyTest
{
  [Test]
  public void PropertyValueJson()
  {
    PropertyAssignment before = new PropertyAssignment();
    before.propertyName = "health";
    before.SetValue<int>(42);

    string json = JsonUtility.ToJson(before);
    PropertyAssignment after = JsonUtility.FromJson<PropertyAssignment>(json);

    Assert.AreEqual(42, after.GetValue<int>());
    Assert.AreEqual("health", after.propertyName);
  }

  // Purely for testing validity of BuildPropertyBlockJson.
  // Ie. what it generates should be Json-parsable into these trivial structs.
  [System.Serializable]
  struct IntProperty
  {
    public int value;
  }

  [System.Serializable]
  struct StringProperty
  {
    public string value;
  }

  [System.Serializable]
  struct TestPropertyBlock
  {
    public IntProperty health;
    public StringProperty friend;
  }


  [Test]
  public void BuildPropertyBlockJson()
  {
    PropertyAssignment health = new PropertyAssignment();
    health.propertyName = "health";
    health.SetValue<int>(42);

    PropertyAssignment friend = new PropertyAssignment();
    friend.propertyName = "friend";
    friend.SetValue<string>("alice");

    PropertyAssignment[] all = new PropertyAssignment[] { health, friend };

    string json = PropertyAssignment.BuildPropertyBlockJson(all);
    Debug.Log(json);

    TestPropertyBlock after = JsonUtility.FromJson<TestPropertyBlock>(json);
    Assert.AreEqual(42, after.health.value);
    Assert.AreEqual("alice", after.friend.value);
  }
}
