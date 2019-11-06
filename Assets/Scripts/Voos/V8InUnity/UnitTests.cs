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

using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace V8InUnity
{
  public class UnitTests
  {
    [System.Serializable]
    struct TestAgentRequest
    {
      public float[] floats;
    }

    [System.Serializable]
    struct TestAgentResponse
    {
      public float[] floatsOut;
    }

    [Test]
    public void TestSort()
    {
      int[] a = new int[] { 2, 3, 1 };
      Native.TestSort(a, a.Length);
      Debug.Assert(a[0] == 1);
      Debug.Assert(a[1] == 2);
      Debug.Assert(a[2] == 3);
    }

    [Test]
    public void TestAgentUpdateOnce()
    {
      TestAgentUpdate();
    }

    [Test]
    public void TestAgentUpdateTwice()
    {
      TestAgentUpdate();
      // Call again to confirm multiple brains work.
      TestAgentUpdate();
    }

    [Test]
    public void TestModules()
    {
      // This is a repeat of the C++ test, just for good measure.

      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
function updateAgent(state) {
  state.floats[1] = getVoosModule('FooMath')['transform'](state.floats[0]);
  state.floats[2] = getVoosModule('BarMath')['transform'](state.floats[0]);
}
";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

      Debug.Assert(V8InUnity.Native.SetModule(brainUid, "FooMath", @"
export function transform(x) { return 2*x; }
"));

      Debug.Assert(V8InUnity.Native.SetModule(brainUid, "BarMath", @"
export function transform(x) { return 3*x; }
"));
      TestAgentRequest req;
      req.floats = new float[] { 4f };
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentRequest>(brainUid, agentUid, req, new Native.UpdateCallbacks());
      Debug.Assert(!maybeResponse.IsEmpty());
      TestAgentRequest res = maybeResponse.Get();
      Debug.Assert(res.floats[1] == 8f);
      Debug.Assert(res.floats[2] == 12f);
    }

    static void TestAgentUpdate()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
function updateAgent(state) {
  state.floatsOut = state.floats.map(x => Math.floor(x));
  state.floats = undefined;
}
";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));
      TestAgentRequest req;
      req.floats = new float[] { 3.5f, 4.5f };
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, new Native.UpdateCallbacks());
      Debug.Assert(!maybeResponse.IsEmpty());
      TestAgentResponse res = maybeResponse.Get();
      Debug.Assert(res.floatsOut[0] == 3f);
      Debug.Assert(res.floatsOut[1] == 4f);
    }

    [Test]
    public void TestAgentUpdateByteArray()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
function updateAgent(dummy, buffer) {
  const v = new DataView(buffer);
  if(v.getInt8(0) != 43) throw 'ahhh';
  v.setInt8(0, 124);
}
";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

      byte[] bytes = new byte[1];
      bytes[0] = 43;
      TestAgentRequest req = new TestAgentRequest();
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, bytes, new Native.UpdateCallbacks());
      Debug.Assert(!maybeResponse.IsEmpty());
      Assert.AreEqual(124, bytes[0]);
    }

    static void TestCallServiceCallback(string serviceName, string argsJson, System.IntPtr reportResultPtr)
    {
      var reportResult = Marshal.GetDelegateForFunctionPointer<Native.ReportResultFunction>(reportResultPtr);

      if (serviceName == "vec123")
      {
        reportResult(JsonUtility.ToJson(new Vector3(1, 2, 3)));
      }
    }

    [Test]
    public void TestServicesBasic()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
function updateAgent(state) {
  const v = callVoosService('vec123');
  state.floatsOut = [v.x, v.y, v.z];
}
";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));
      TestAgentRequest req;
      req.floats = new float[0];
      var cbs = new Native.UpdateCallbacks();
      cbs.callService = TestCallServiceCallback;
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, cbs);
      Debug.Assert(!maybeResponse.IsEmpty());
      TestAgentResponse res = maybeResponse.Get();
      Debug.Assert(res.floatsOut[0] == 1f);
      Debug.Assert(res.floatsOut[1] == 2f);
      Debug.Assert(res.floatsOut[2] == 3f);
    }

    bool actorBoolValue = false;

    private void TestActorBooleanGetter(ushort actorId, ushort fieldId, out bool valueOut)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      valueOut = actorBoolValue;
    }

    private void TestActorBooleanSetter(ushort actorId, ushort fieldId, bool newValue)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      actorBoolValue = newValue;
    }

    [Test]
    public void TestActorBooleanAccessors()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
      function updateAgent(obj) {
        const prev = getActorBoolean(12, 34);
        if(prev != true) throw 'ahhh';
        setActorBoolean(12, 34, false);
      }";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));
      TestAgentRequest req;
      req.floats = new float[0];
      actorBoolValue = true;
      var callbacks = new Native.UpdateCallbacks();
      callbacks.getActorBoolean = TestActorBooleanGetter;
      callbacks.setActorBoolean = TestActorBooleanSetter;
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, callbacks);
      Debug.Assert(!maybeResponse.IsEmpty());
      Assert.AreEqual(false, actorBoolValue);
    }

    string actorStringValue = null;

    string TestActorStringGetter(ushort actorId, ushort fieldId)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      return actorStringValue;
    }

    void TestActorStringSetter(ushort actorId, ushort fieldId, string newValue)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      actorStringValue = newValue;
    }

    [Test]
    public void TestActorStringAccessors()
    {
      // Do this test several times, since there's lots of caching in this code path.
      for (int i = 0; i < 5; i++)
      {
        string brainUid = System.Guid.NewGuid().ToString();
        string agentUid = System.Guid.NewGuid().ToString();
        Debug.Log($"strting test for {brainUid}");
        actorStringValue = $"€ {brainUid} €";
        string brainSource = @"
          function updateAgent(obj) {
            const prev = getActorString(12, 34);
            sysLog(`got string: ${prev}`);
            if(prev != '€ " + brainUid + @" €') throw 'ahhh';
            setActorString(12, 34, 'from JS € " + agentUid + @" € euros €');
          }";
        Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

        var req = new TestAgentRequest { floats = new float[0] };
        var callbacks = new Native.UpdateCallbacks
        {
          getActorString = TestActorStringGetter,
          setActorString = TestActorStringSetter
        };
        var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, callbacks);
        Debug.Assert(!maybeResponse.IsEmpty());
        Assert.AreEqual("from JS € " + agentUid + " € euros €", actorStringValue);

        Debug.Log($"OK succeeded once");
      }
    }

    [Test]
    public void TestSetActorStringNull()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      actorStringValue = "overwrite me";
      string brainSource = @"
          function updateAgent(obj) {
            setActorString(12, 34, null);
          }";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

      var req = new TestAgentRequest { floats = new float[0] };
      var callbacks = new Native.UpdateCallbacks
      {
        getActorString = TestActorStringGetter,
        setActorString = TestActorStringSetter
      };
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, callbacks);
      Debug.Assert(!maybeResponse.IsEmpty());
      Assert.AreEqual("", actorStringValue);
    }

    [Test]
    public void TestSetActorStringUndefined()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      actorStringValue = "overwrite me";
      string brainSource = @"
          function updateAgent(obj) {
            setActorString(12, 34, undefined);
          }";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

      var req = new TestAgentRequest { floats = new float[0] };
      var callbacks = new Native.UpdateCallbacks
      {
        getActorString = TestActorStringGetter,
        setActorString = TestActorStringSetter
      };
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, callbacks);
      Debug.Assert(!maybeResponse.IsEmpty());
      Assert.AreEqual("", actorStringValue);
    }

    float actorFloatValue = 0f;

    private void TestActorFloatGetter(ushort actorId, ushort fieldId, out float valueOut)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      valueOut = actorFloatValue;
    }

    private void TestActorFloatSetter(ushort actorId, ushort fieldId, float newValue)
    {
      Assert.AreEqual(12, actorId);
      Assert.AreEqual(34, fieldId);
      actorFloatValue = newValue;
    }

    [Test]
    public void TestActorFloatAccessors()
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();
      string brainSource = @"
      function updateAgent(obj) {
        const prev = getActorFloat(12, 34);
        if(Math.abs(prev - 12.34) > 1e-6) throw 'ahhh';
        setActorFloat(12, 34, 56.78);
      }";
      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));
      TestAgentRequest req;
      req.floats = new float[0];
      actorFloatValue = 12.34f;
      var callbacks = new Native.UpdateCallbacks();
      callbacks.getActorFloat = TestActorFloatGetter;
      callbacks.setActorFloat = TestActorFloatSetter;
      var maybeResponse = V8InUnity.Native.UpdateAgent<TestAgentRequest, TestAgentResponse>(brainUid, agentUid, req, callbacks);
      Debug.Assert(!maybeResponse.IsEmpty());
      Assert.AreEqual(56.78f, actorFloatValue, 1e-6);
    }

    static int PerfTestIterations = 20000;

    struct Nothing { }

    static void RunPerformanceTest(string label, string brainSource)
    {
      string brainUid = System.Guid.NewGuid().ToString();
      string agentUid = System.Guid.NewGuid().ToString();

      Debug.Assert(V8InUnity.Native.ResetBrain(brainUid, brainSource));

      Nothing req = new Nothing();
      var sw = new System.Diagnostics.Stopwatch();
      string output = PerfTestIterations + " iters each: ";
      for (int i = 0; i < 5; i++)
      {
        sw.Restart();
        var maybeResponse = V8InUnity.Native.UpdateAgent<Nothing, Nothing>(brainUid, agentUid, req, new Native.UpdateCallbacks());
        float ms = sw.ElapsedMilliseconds;
        output += ms + ", ";
        Debug.Assert(!maybeResponse.IsEmpty());
      }
      output += " ms total.";
      Debug.Log(label + ": " + output);
    }

    static int NormalizeAndFloor(int index)
    {
      return Mathf.FloorToInt(100f * new Vector3(4, index, 6).normalized.x);
    }

    static void RunNormalizationComparisons()
    {
      Native.SetLookupIntFunction(NormalizeAndFloor);

      RunPerformanceTest("javascript vec3 norm", @"
let pos = {x: 4, y: 5, z: 6};
function lookupInt(index) { 
  let x = 4;
  let y = index;
  let z = 6;
  const mag = Math.sqrt(x*x + y*y + z*z);
x /= mag;
y /= mag;
z /= mag;
  return Math.floor(100 * x/mag);
}

function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += lookupInt(i%3);
  }
//log(`${x/N}`);
}
");

      RunPerformanceTest("unity vec3 norm", @"
function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += testLookup(i%3);
  }
//log(`${x/N}`);
}
");

      RunPerformanceTest("unity-cached vec3 norm", @"
let cache = {};
function lookupInt(index) {
  if(!(index in cache)) {
    cache[index] = testLookup(index);
  }
  return cache[index];
}

function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += lookupInt(i%3);
  }
//log(`${x/N}`);
}
");

    }

    static Transform transformUnderTest;
    static int LookupPositionComponent(int index)
    {
      if (index == 0)
      {
        return (int)transformUnderTest.position.x;
      }
      if (index == 1)
      {
        return (int)transformUnderTest.position.y;
      }
      else
      {
        return (int)transformUnderTest.position.z;
      }
    }

    static void RunTransformAccessComparisons()
    {
      Native.SetLookupIntFunction(LookupPositionComponent);

      var go = new GameObject();
      go.transform.position = new Vector3(4, 5, 6);
      transformUnderTest = go.transform;

      RunPerformanceTest("javascript lookup", @"
let pos = {x: 4, y: 5, z: 6};
function lookupInt(index) { 
  if(index == 0) {
    return pos.x;
  }
  else if(index == 1) {
    return pos.y;
  }
  else {
    return pos.z;
  }
}

function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += lookupInt(i%3);
  }
}
");

      RunPerformanceTest("unity lookup", @"
function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += testLookup(i%3);
  }
}
");

      RunPerformanceTest("unity-cached lookup", @"
let cache = {};
function lookupInt(index) {
  if(!(index in cache)) {
    cache[index] = testLookup(index);
  }
  return cache[index];
}

function updateAgent(state) {
  let N = " + PerfTestIterations + @";
  let x = 0;
  for(let i = 0; i < N; i++) {
    x += lookupInt(i%3);
  }
}
");

      GameObject.DestroyImmediate(go);
    }

    public static void RunPerformanceExperiments()
    {
      RunNormalizationComparisons();
      RunTransformAccessComparisons();
    }
  }
}
