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
using System.Collections.Generic;
using UnityEngine;

namespace V8InUnity
{
  // Bindings to the native plugin functions
  public class Native
  {
    public delegate void StringFunction(string str);

    public delegate int LookupIntFunction(int index);

    [DllImport("v8_in_unity")]
    public static extern void TestSort(int[] a, int length);

    [DllImport("v8_in_unity")]
    public static extern void SetLookupIntFunction(LookupIntFunction function);


    [DllImport("v8_in_unity")]
    public static extern void Evaluate(string javascriptCode);

    [DllImport("v8_in_unity")]
    public static extern bool ResetBrain(string brainUid, string javascript);

    [DllImport("v8_in_unity")]
    public static extern bool SetModule(string brainUid, string moduleUid, string javascript);

    public static bool SetModule(string brainUid, string moduleUid, string javascript, StringFunction handleErrors)
    {
      UserErrorHandler.Push(handleErrors);
      bool rv = SetModule(brainUid, moduleUid, javascript);
      UserErrorHandler.Pop();
      return rv;
    }

    [DllImport("v8_in_unity")]
    private static extern bool UpdateAgentJson(string brainUid, string agentUid, string json, System.IntPtr reportJsonResult);

    [DllImport("v8_in_unity")]
    private static extern bool UpdateAgentJsonBytes(string brainUid, string agentUid, string json, System.IntPtr bytes_in, int length_in, StringFunction reportJsonResult);

    private static int NumUpdateCalls = 0;

    private static byte[] DummyByteArray = { };

    public struct UpdateCallbacks
    {
      public StringFunction handleLog;
      public StringFunction handleError;

      public CallServiceFunction callService;

      public ActorBooleanGetter getActorBoolean;
      public ActorBooleanSetter setActorBoolean;

      public ActorVector3Getter getActorVector3;
      public ActorVector3Setter setActorVector3;

      public ActorQuaternionGetter getActorQuaternion;
      public ActorQuaternionSetter setActorQuaternion;

      public UserActorStringGetter getActorString;
      public UserActorStringSetter setActorString;

      public ActorFloatGetter getActorFloat;
      public ActorFloatSetter setActorFloat;
    }

    public static Util.Maybe<TResponse> UpdateAgent<TRequest, TResponse>(string brainUid, string agentUid,
      TRequest input,
      UpdateCallbacks callbacks)
    {
      return UpdateAgent<TRequest, TResponse>(brainUid, agentUid, input, DummyByteArray, callbacks);
    }

    class UpdateAgentLock : System.IDisposable
    {
      static bool UpdateAgentInProgress = false;

      public UpdateAgentLock()
      {
        if (UpdateAgentInProgress)
        {
          throw new System.Exception("UpdateAgent was called while another was still in progress!");
        }
        UpdateAgentInProgress = true;
      }

      public void Dispose()
      {
        Debug.Assert(UpdateAgentInProgress);
        UpdateAgentInProgress = false;
      }
    }

    public static Util.Maybe<TResponse> UpdateAgent<TRequest, TResponse>(string brainUid, string agentUid,
      TRequest input, byte[] bytes,
      UpdateCallbacks callbacks)
    {
      using (new UpdateAgentLock())
      using (InGameProfiler.Section("Native.UpdateAgent"))
      using (var pinnedBytes = Util.Pin(bytes))
      {
        NumUpdateCalls++;
        bool ok = false;
        string inputJson = null;
        string outputJson = null;

        using (InGameProfiler.Section("ToJson"))
        {
          inputJson = JsonUtility.ToJson(input, false);
        }
        using (InGameProfiler.Section("UpdateAgentJsonNative"))
        {
          UserErrorHandler.Push(callbacks.handleError);
          UserLogMessageHandler.Push(callbacks.handleLog);

          userActorStringGetter = callbacks.getActorString;
          userActorStringSetter = callbacks.setActorString;

          using (InGameProfiler.Section("setting callbacks"))
          {
            // Avoid unnecessary calls to the Set... delegate bind functions,
            // since those can take ~0.2ms each! Also, any pinning is
            // unnecessary, since the life time of use is limited to this
            // function. See:
            // https://blogs.msdn.microsoft.com/cbrumme/2003/05/06/asynchronous-operations-pinning/

            if (lastCallServiceFunction != callbacks.callService)
            {
              SetCallServiceFunction(callbacks.callService);
              lastCallServiceFunction = callbacks.callService;
            }

            // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_ACCESSOR_DELEGATE_MAYBE_SETS
            if (lastBooleanGetterCallback != callbacks.getActorBoolean)    // GENERATED
            {
              SetActorBooleanGetter(callbacks.getActorBoolean);    // GENERATED
              lastBooleanGetterCallback = callbacks.getActorBoolean;    // GENERATED
            }

            if (lastBooleanSetterCallback != callbacks.setActorBoolean)    // GENERATED
            {
              SetActorBooleanSetter(callbacks.setActorBoolean);    // GENERATED
              lastBooleanSetterCallback = callbacks.setActorBoolean;    // GENERATED
            }

            if (lastVector3GetterCallback != callbacks.getActorVector3)    // GENERATED
            {
              SetActorVector3Getter(callbacks.getActorVector3);    // GENERATED
              lastVector3GetterCallback = callbacks.getActorVector3;    // GENERATED
            }

            if (lastVector3SetterCallback != callbacks.setActorVector3)    // GENERATED
            {
              SetActorVector3Setter(callbacks.setActorVector3);    // GENERATED
              lastVector3SetterCallback = callbacks.setActorVector3;    // GENERATED
            }

            if (lastQuaternionGetterCallback != callbacks.getActorQuaternion)    // GENERATED
            {
              SetActorQuaternionGetter(callbacks.getActorQuaternion);    // GENERATED
              lastQuaternionGetterCallback = callbacks.getActorQuaternion;    // GENERATED
            }

            if (lastQuaternionSetterCallback != callbacks.setActorQuaternion)    // GENERATED
            {
              SetActorQuaternionSetter(callbacks.setActorQuaternion);    // GENERATED
              lastQuaternionSetterCallback = callbacks.setActorQuaternion;    // GENERATED
            }

            if (lastFloatGetterCallback != callbacks.getActorFloat)    // GENERATED
            {
              SetActorFloatGetter(callbacks.getActorFloat);    // GENERATED
              lastFloatGetterCallback = callbacks.getActorFloat;    // GENERATED
            }

            if (lastFloatSetterCallback != callbacks.setActorFloat)    // GENERATED
            {
              SetActorFloatSetter(callbacks.setActorFloat);    // GENERATED
              lastFloatSetterCallback = callbacks.setActorFloat;    // GENERATED
            }

            // END_GAME_BUILDER_CODE_GEN
          }
          // Safe callback passing: https://docs.microsoft.com/en-us/dotnet/framework/interop/marshaling-a-delegate-as-a-callback-method
          StringFunction captureJsonFunction = new StringFunction(json => outputJson = json);
          ok = UpdateAgentJsonBytes(brainUid, agentUid,
            inputJson, pinnedBytes.GetPointer(), bytes.Length,
            captureJsonFunction);

          UserErrorHandler.Pop();
          UserLogMessageHandler.Pop();
        }

        if (!ok)
        {
          // TODO consider using the JSON return value for communicating the
          // exception from JS...and throwing an exception!!
          Debug.LogError("UpdateAgent failed. inputJson: " + inputJson);
          return Util.Maybe<TResponse>.CreateEmpty();
        }
        else
        {
          using (InGameProfiler.Section("FromJson"))
          {
#if UNITY_EDITOR
            if (outputJson.Length > 5 * 1024 * 1024)
            {
              Util.LogError($"JSON response from VOOS update is getting dangerously large..exceeding 5MB. Full content: {outputJson}");
              Debug.Assert(false, "Editor-only JSON size check. See log for more details.");
            }
#endif
            TResponse response = JsonUtility.FromJson<TResponse>(outputJson);
            return Util.Maybe<TResponse>.CreateWith(response);
          }
        }

      }
    }

    private static readonly Stack<StringFunction> UserLogMessageHandler = new Stack<StringFunction>();

    private static void DebugLog(string msg)
    {
      Util.Log($"V8F{NumUpdateCalls}: {msg}");
      if (UserLogMessageHandler.Count > 0)
      {
        UserLogMessageHandler.Peek()?.Invoke(msg);
      }
    }

    private static readonly Stack<StringFunction> UserErrorHandler = new Stack<StringFunction>();

    private static void ErrorLog(string msg)
    {
      Util.LogError($"N{NumUpdateCalls}: {msg}");
      if (UserErrorHandler.Count > 0)
      {
        UserErrorHandler.Peek()?.Invoke(msg);
      }
    }

    private static string GetRuntimeFilesRoot()
    {
      return System.IO.Path.Combine(Application.streamingAssetsPath, "V8InUnity");
    }

    [DllImport("v8_in_unity")]
    private static extern int InitializeV8(string runtimeFilesDirectory);

    [DllImport("v8_in_unity")]
    private static extern void SetDebugLogFunction(StringFunction function);

    [DllImport("v8_in_unity")]
    private static extern void SetErrorLogFunction(StringFunction function);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ReportResultFunction(string resultJson);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CallServiceFunction(string serviceName, string argsJson, System.IntPtr reportFunction);

    [DllImport("v8_in_unity")]
    private static extern void SetCallServiceFunction(CallServiceFunction function);

    static Native()
    {
      InitializeV8(GetRuntimeFilesRoot());
      SetDebugLogFunction(DebugLog);
      SetErrorLogFunction(ErrorLog);
      SetActorStringGetter(ActorStringGetterImpl);
      SetActorStringSetter(ActorStringSetterImpl);
    }

    // These must match v8_in_unity.h typedefs
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorBooleanGetter(ushort tempActorId, ushort fieldId, out bool valueOut);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorBooleanSetter(ushort tempActorId, ushort fieldId, bool newValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorVector3Getter(ushort tempActorId, ushort fieldId, out float xOut, out float yOut, out float zOut);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorVector3Setter(ushort tempActorId, ushort fieldId, float newX, float newY, float newZ);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorQuaternionGetter(ushort tempActorId, ushort fieldId, out float xOut, out float yOut, out float zOut, out float wOut);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorQuaternionSetter(ushort tempActorId, ushort fieldId, float newX, float newY, float newZ, float newW);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorFloatGetter(ushort tempActorId, ushort fieldId, out float valueOut);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ActorFloatSetter(ushort tempActorId, ushort fieldId, float newValue);

    // Because strings are complicated, we expose simple callbacks and hide the marshalling details.
    public delegate string UserActorStringGetter(ushort tempActorId, ushort fieldId);
    public delegate void UserActorStringSetter(ushort tempActorId, ushort fieldId, string newValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void ActorStringGetter(ushort tempActorId, ushort fieldId, System.IntPtr utf8Bytes, int maxBytes);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void ActorStringSetter(ushort tempActorId, ushort fieldId, System.IntPtr utf8Bytes);

    // TODO generate these
    // TODO these could just be one function that take both getter/setter..
    [DllImport("v8_in_unity")]
    static extern void SetActorBooleanGetter(ActorBooleanGetter getter);
    [DllImport("v8_in_unity")]
    static extern void SetActorBooleanSetter(ActorBooleanSetter setter);
    [DllImport("v8_in_unity")]
    static extern void SetActorFloatGetter(ActorFloatGetter getter);
    [DllImport("v8_in_unity")]
    static extern void SetActorFloatSetter(ActorFloatSetter setter);
    [DllImport("v8_in_unity")]
    static extern void SetActorVector3Getter(ActorVector3Getter getter);
    [DllImport("v8_in_unity")]
    static extern void SetActorVector3Setter(ActorVector3Setter setter);
    [DllImport("v8_in_unity")]
    static extern void SetActorQuaternionGetter(ActorQuaternionGetter getter);
    [DllImport("v8_in_unity")]
    static extern void SetActorQuaternionSetter(ActorQuaternionSetter setter);
    [DllImport("v8_in_unity")]
    static extern void SetActorStringGetter(ActorStringGetter getter);
    [DllImport("v8_in_unity")]
    static extern void SetActorStringSetter(ActorStringSetter setter);

    // This is so we don't call Set***Getter/Setter each frame. That actually
    // costs 0.2ms!

    private static CallServiceFunction lastCallServiceFunction;

    // BEGIN_GAME_BUILDER_CODE_GEN ACTOR_ACCESSOR_DELEGATE_CACHES
    private static ActorBooleanGetter lastBooleanGetterCallback;    // GENERATED
    private static ActorBooleanSetter lastBooleanSetterCallback;    // GENERATED
    private static ActorVector3Getter lastVector3GetterCallback;    // GENERATED
    private static ActorVector3Setter lastVector3SetterCallback;    // GENERATED
    private static ActorQuaternionGetter lastQuaternionGetterCallback;    // GENERATED
    private static ActorQuaternionSetter lastQuaternionSetterCallback;    // GENERATED
    private static ActorFloatGetter lastFloatGetterCallback;    // GENERATED
    private static ActorFloatSetter lastFloatSetterCallback;    // GENERATED
                                                                // END_GAME_BUILDER_CODE_GEN


    // Implementation of string accessors

    // This size should match v8_in_unity.cpp
    const int MaxActorStringBytes = 1 * 1024 * 1024;
    static byte[] ActorStringCodingBuffer = new byte[MaxActorStringBytes];

    private static UserActorStringGetter userActorStringGetter;
    private static UserActorStringSetter userActorStringSetter;

    private static void ActorStringGetterImpl(ushort tempActorId, ushort fieldId, System.IntPtr utf8Bytes, int maxBytes)
    {
      Debug.Assert(maxBytes == ActorStringCodingBuffer.Length);

      if (userActorStringGetter == null) return;
      string actorStringValue = userActorStringGetter(tempActorId, fieldId);
      int encodedBytes = System.Text.Encoding.UTF8.GetByteCount(actorStringValue);
      int bytesNeeded = encodedBytes + 1;  // +1 for null terminator.
      if (bytesNeeded > maxBytes)
      {
        Util.LogError($"ActorStringGetter called, but the actor string's byte size (utf8) exceeds the max size");
        return;
      }

      System.Text.Encoding.UTF8.GetBytes(actorStringValue, 0, actorStringValue.Length, ActorStringCodingBuffer, 0);
      // Null-terminate
      ActorStringCodingBuffer[bytesNeeded - 1] = 0;
      Marshal.Copy(ActorStringCodingBuffer, 0, utf8Bytes, bytesNeeded);
    }

    private static void ActorStringSetterImpl(ushort tempActorId, ushort fieldId, System.IntPtr utf8Bytes)
    {
      if (userActorStringSetter == null) return;
      // Count str length
      // TODO would it be better to get this passed in, since native could do it faster..?
      int nullIndex = 0;
      while (Marshal.ReadByte(utf8Bytes, nullIndex) != 0) nullIndex++;

      // We don't need to include the null terminator, since UTF8.GetString
      // takes a byte count directly.
      int inBytes = nullIndex;

      if (inBytes > MaxActorStringBytes)
      {
        Util.LogError($"ActorStringSetter called with buffer that is too large. Expected {MaxActorStringBytes} bytes, but got {inBytes} bytes.");
        return;
      }
      // TODO ehh don't we need the null term here??
      Marshal.Copy(utf8Bytes, ActorStringCodingBuffer, 0, inBytes);
      string actorStringValue = System.Text.Encoding.UTF8.GetString(ActorStringCodingBuffer, 0, inBytes);
      userActorStringSetter(tempActorId, fieldId, actorStringValue);
    }

  }
}