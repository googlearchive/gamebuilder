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

#pragma once

#ifdef _WIN32
#define V8_IN_UNITY_DLLEXPORT __declspec(dllexport)
#else
#define V8_IN_UNITY_DLLEXPORT
#endif

extern "C"
{
  typedef const char *CSHARP_STRING;
  typedef void *BYTE_ARRAY;

  typedef void (*StringFunction)(const char *);

  typedef void (*SetLocalPositionFunction)(float x, float y, float z);

  V8_IN_UNITY_DLLEXPORT void TestSort(int a[], int length);

  V8_IN_UNITY_DLLEXPORT int InitializeV8(const char *runtimeFilesDirectory);
  V8_IN_UNITY_DLLEXPORT int InitializeV8WithExecutablePath(const char *executablePath);
  V8_IN_UNITY_DLLEXPORT int DeinitializeV8();

  V8_IN_UNITY_DLLEXPORT void Evaluate(const char *javascriptSource);
  V8_IN_UNITY_DLLEXPORT int EvaluateToInteger(const char *javascriptSource);

  V8_IN_UNITY_DLLEXPORT void SetDebugLogFunction(StringFunction function);
  V8_IN_UNITY_DLLEXPORT void SetErrorLogFunction(StringFunction function);

  V8_IN_UNITY_DLLEXPORT bool ResetBrain(CSHARP_STRING brainUid, CSHARP_STRING javascript);
  V8_IN_UNITY_DLLEXPORT bool UpdateAgentJson(CSHARP_STRING brainUid, CSHARP_STRING agentUid, CSHARP_STRING json_in, StringFunction report_result);
  // We are purposefully using int instead of size_t for length_in.
  V8_IN_UNITY_DLLEXPORT bool UpdateAgentJsonBytes(CSHARP_STRING brainUid, CSHARP_STRING agentUid, CSHARP_STRING json_in, BYTE_ARRAY bytes_in, int length_in, StringFunction report_result);

  V8_IN_UNITY_DLLEXPORT bool SetModule(CSHARP_STRING brainUid, CSHARP_STRING moduleUid, CSHARP_STRING javascript);

  // Performance tests.
  typedef int (*LookupIntFunction)(int index);
  V8_IN_UNITY_DLLEXPORT void SetLookupIntFunction(LookupIntFunction function);

  // Host services system.
  typedef void (*ReportServiceResultFunction)(CSHARP_STRING resultJson);
  typedef void (*CallServiceFunction)(const char *serviceName, const char *argsJson, ReportServiceResultFunction reportFunction);

  V8_IN_UNITY_DLLEXPORT void SetCallServiceFunction(CallServiceFunction callService);

  // Synchronous actor API.

  // These IDs are only guaranteed to be valid within a single UpdateAgent call.
  typedef unsigned short TEMP_ACTOR_ID;

  typedef unsigned short ACTOR_FIELD_ID;

  typedef void (*ActorVector3Getter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float *x, float *y, float *z);
  typedef void (*ActorVector3Setter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float x, float y, float z);
  typedef void (*ActorQuaternionGetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float *x, float *y, float *z, float *w);
  typedef void (*ActorQuaternionSetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float x, float y, float z, float w);
  typedef void (*ActorBooleanGetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, bool *value);
  typedef void (*ActorBooleanSetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, bool value);
  typedef void (*ActorStringGetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, char *utf8_bytes, int max_bytes);
  typedef void (*ActorStringSetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, const char *utf8_bytes); // The implementation is expected to copy the string out of value, not just reference the pointer.
  typedef void (*ActorFloatGetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float *x);
  typedef void (*ActorFloatSetter)(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float x);

  V8_IN_UNITY_DLLEXPORT void SetActorVector3Getter(ActorVector3Getter f);
  V8_IN_UNITY_DLLEXPORT void SetActorVector3Setter(ActorVector3Setter f);
  V8_IN_UNITY_DLLEXPORT void SetActorQuaternionGetter(ActorQuaternionGetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorQuaternionSetter(ActorQuaternionSetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorBooleanGetter(ActorBooleanGetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorBooleanSetter(ActorBooleanSetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorStringGetter(ActorStringGetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorStringSetter(ActorStringSetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorFloatGetter(ActorFloatGetter f);
  V8_IN_UNITY_DLLEXPORT void SetActorFloatSetter(ActorFloatSetter f);
}
