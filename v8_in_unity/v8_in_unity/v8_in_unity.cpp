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

// v8_in_unity.cpp : Defines the exported functions for the DLL application.

// Include dirs:
// C:\building_v8\v8;C:\building_v8\v8\include;

// Lib dirs:
// C:\building_v8\v8\out.gn\x64.release.sample\obj

#pragma comment(lib, "v8_monolith.lib")

#ifdef _WIN32
#pragma comment(lib, "winmm.lib")
#pragma comment(lib, "dbghelp.lib")
#pragma comment(lib, "shlwapi.lib")
#endif

#include "v8_in_unity.h"
#include <algorithm>
#include <iostream>
#include <sstream>
#include <map>
#include <vector>
#include <string.h>
#include "libplatform/libplatform.h"
#include "v8.h"

using namespace v8;

const size_t MAX_FILEPATH_LENGTH = 1024;
const size_t MAX_GUID_LENGTH = 128;
const size_t MAX_JAVASCRIPT_SOURCE_LENGTH = 1024 * 1024;
const size_t MAX_JSON_LENGTH = 10 * 1024 * 1024;
const size_t MAX_BUFFER_SIZE = 10 * 1024 * 1024;
const size_t MAX_SERVICE_NAME_LENGTH = 128;
const size_t MAX_LOG_MESSAGE_LENGTH = 1024 * 1024;

static bool IsStringValid(const char *string, size_t max_length)
{
  size_t len = strnlen(string, max_length);
  bool valid = len != 0 && len < max_length;
  if (!valid)
  {
    std::cerr << "WARNING: A given string exceeded the maximum expected length of " << max_length << " bytes." << std::endl;
  }
  return valid;
}

struct V8State
{
  V8State() : platform(nullptr)
  {
  }
  Platform *platform;
};

static V8State V8_GLOBAL_STATE;

StringFunction HOST_DEBUG_LOG_FUNCTION = nullptr;
StringFunction HOST_ERROR_LOG_FUNCTION = nullptr;
SetLocalPositionFunction HOST_SET_LOCAL_POSITION_FUNCTION = nullptr;

LookupIntFunction LOOK_UP_INT_FUNCTION = nullptr;

static void Log(const char *msg)
{
  if (HOST_DEBUG_LOG_FUNCTION)
  {
    HOST_DEBUG_LOG_FUNCTION(msg);
  }
}

static void LogError(const char *msg)
{
  if (HOST_ERROR_LOG_FUNCTION)
  {
    HOST_ERROR_LOG_FUNCTION(msg);
  }
  else
  {
    std::cerr << msg << std::endl;
  }
}

static void LogError(const std::ostringstream &stream)
{
  LogError(stream.str().c_str());
}

static void LogV8Callback(const FunctionCallbackInfo<Value> &info)
{
  String::Utf8Value utf8(info.GetIsolate(), info[0]);
  if (utf8.length() > MAX_LOG_MESSAGE_LENGTH)
  {
    return;
  }
  Log(*utf8);
}

static void LogErrorV8Callback(const FunctionCallbackInfo<Value> &info)
{
  String::Utf8Value utf8(info.GetIsolate(), info[0]);
  if (utf8.length() > MAX_LOG_MESSAGE_LENGTH)
  {
    return;
  }
  LogError(*utf8);
}

static void PrintWithLineNumbers(std::ostream &os, CSHARP_STRING body)
{
  if (body == nullptr)
  {
    return;
  }

  std::stringstream bodyStream(body);
  std::string line;

  int lineNum = 1;
  while (std::getline(bodyStream, line, '\n'))
  {
    os << lineNum << ":\t" << line << std::endl;
    lineNum++;
  }
}

#include "../../third_party/chromium/LogException.cpp"

// Mostly for performacne tests.
static void FortyTwoV8Callback(const FunctionCallbackInfo<Value> &info)
{
  info.GetReturnValue().Set(42);
}

static void ThirteenV8Callback(const FunctionCallbackInfo<Value> &info)
{
  info.GetReturnValue().Set(13);
}

static void LookUpIntV8Callback(const FunctionCallbackInfo<Value> &info)
{
  Local<Context> context = info.GetIsolate()->GetCurrentContext();
  int rv = LOOK_UP_INT_FUNCTION(info[0]->Int32Value(context).FromJust());
  info.GetReturnValue().Set(rv);
}

static void BindFunction(Isolate *isolate, Local<ObjectTemplate> object, const char *functionName, FunctionCallback callback)
{
  object->Set(String::NewFromUtf8(isolate, functionName), FunctionTemplate::New(isolate, callback));
}

static void SetupGlobalTemplate(Isolate *isolate, Local<ObjectTemplate> global)
{
  BindFunction(isolate, global, "sysLog", LogV8Callback);
  BindFunction(isolate, global, "sysError", LogErrorV8Callback);

  BindFunction(isolate, global, "fortyTwo", FortyTwoV8Callback);
  BindFunction(isolate, global, "thirteen", ThirteenV8Callback);
  BindFunction(isolate, global, "testLookup", LookUpIntV8Callback);
}

static bool GetReusableFunctionReference(
    Isolate *isolate,
    Local<Context> *context,
    const std::string &functionName,
    Global<Function> *reference)
{
  // Fetch out the updateAgent function.
  Local<String> function_name =
      String::NewFromUtf8(isolate, functionName.c_str(), NewStringType::kNormal)
          .ToLocalChecked();

  Local<Value> maybe_function_ref;

  if (!(*context)->Global()->Get(*context, function_name).ToLocal(&maybe_function_ref) ||
      !maybe_function_ref->IsFunction())
  {
    return false;
  }

  reference->Reset(isolate, Local<Function>::Cast(maybe_function_ref));
  return true;
}

// Host services system.

class ServiceUser
{
public:
  ServiceUser() {}
  virtual ~ServiceUser() {}
  virtual void HandleServiceResult(CSHARP_STRING resultJson) = 0;
};

CallServiceFunction CALL_SERVICE_FUNCTION = nullptr;
ServiceUser *CURRENT_SERVICE_USER = nullptr;
bool WAITING_ON_SERVICE_RESULT_REPORT = false;

ActorVector3Getter ACTOR_VECTOR3_GETTER = nullptr;
ActorVector3Setter ACTOR_VECTOR3_SETTER = nullptr;
ActorQuaternionGetter ACTOR_QUATERNION_GETTER = nullptr;
ActorQuaternionSetter ACTOR_QUATERNION_SETTER = nullptr;
ActorBooleanGetter ACTOR_BOOLEAN_GETTER = nullptr;
ActorBooleanSetter ACTOR_BOOLEAN_SETTER = nullptr;
ActorStringGetter ACTOR_STRING_GETTER = nullptr;
ActorStringSetter ACTOR_STRING_SETTER = nullptr;
ActorFloatGetter ACTOR_FLOAT_GETTER = nullptr;
ActorFloatSetter ACTOR_FLOAT_SETTER = nullptr;

// 1 mb should be plenty for an individual actor's string.
static char GetActorStringBuffer[1 * 1024 * 1024];

void ReportServiceResult(CSHARP_STRING resultJson)
{
  if (!IsStringValid(resultJson, MAX_JSON_LENGTH))
  {
    std::cerr << "Service result was too big. Ignoring it." << std::endl;
    return;
  }

  WAITING_ON_SERVICE_RESULT_REPORT = false;
  if (CURRENT_SERVICE_USER == nullptr)
  {
    std::ostringstream err;
    err << "ReportServiceResult was called, but no current service user was set? Result json: " << resultJson;
    LogError(err);
    return;
  }
  CURRENT_SERVICE_USER->HandleServiceResult(resultJson);
}

static void CallService(const char *serviceName, const char *argsJson, ServiceUser *user)
{
  if (!IsStringValid(serviceName, MAX_SERVICE_NAME_LENGTH) || !IsStringValid(argsJson, MAX_JSON_LENGTH))
  {
    return;
  }

  if (CALL_SERVICE_FUNCTION == nullptr)
  {
    LogError("CallService was called, but no CallServiceFunction was set by the host.");
    return;
  }
  CURRENT_SERVICE_USER = user;
  WAITING_ON_SERVICE_RESULT_REPORT = true;
  CALL_SERVICE_FUNCTION(serviceName, argsJson, ReportServiceResult);
  CURRENT_SERVICE_USER = nullptr;

  if (WAITING_ON_SERVICE_RESULT_REPORT)
  {
    std::ostringstream err;
    err << "WARNING: Called host service '" << serviceName << "', but it never reported back results.";
    LogError(err);
  }
}

extern "C"
{
  void SetCallServiceFunction(CallServiceFunction callService)
  {
    CALL_SERVICE_FUNCTION = callService;
  }

  void SetActorVector3Getter(ActorVector3Getter f)
  {
    ACTOR_VECTOR3_GETTER = f;
  }
  void SetActorVector3Setter(ActorVector3Setter f)
  {
    ACTOR_VECTOR3_SETTER = f;
  }
  void SetActorQuaternionGetter(ActorQuaternionGetter f)
  {
    ACTOR_QUATERNION_GETTER = f;
  }
  void SetActorQuaternionSetter(ActorQuaternionSetter f)
  {
    ACTOR_QUATERNION_SETTER = f;
  }
  void SetActorBooleanGetter(ActorBooleanGetter f)
  {
    ACTOR_BOOLEAN_GETTER = f;
  }
  void SetActorBooleanSetter(ActorBooleanSetter f)
  {
    ACTOR_BOOLEAN_SETTER = f;
  }
  void SetActorStringGetter(ActorStringGetter f)
  {
    ACTOR_STRING_GETTER = f;
  }
  void SetActorStringSetter(ActorStringSetter f)
  {
    ACTOR_STRING_SETTER = f;
  }
  void SetActorFloatGetter(ActorFloatGetter f)
  {
    ACTOR_FLOAT_GETTER = f;
  }
  void SetActorFloatSetter(ActorFloatSetter f)
  {
    ACTOR_FLOAT_SETTER = f;
  }
}

class VoosBrain : public ServiceUser
{
public:
  // TODO create these using a factory method instead, to better handle ctor errors.

  bool valid;

  VoosBrain(const char *javascript) : isolate_(nullptr), valid(false)
  {
    create_params.array_buffer_allocator = v8::ArrayBuffer::Allocator::NewDefaultAllocator();
    isolate_ = Isolate::New(create_params);
    isolate_->SetData(0, (void *)this);

    // Create the context
    Isolate::Scope isolate_scope(isolate_);
    // Create a stack-allocated handle scope.
    HandleScope handle_scope(isolate_);

    Local<ObjectTemplate> global_template = ObjectTemplate::New(isolate_);
    SetupGlobalTemplate(isolate_, global_template);
    BindFunction(isolate_, global_template, "getVoosModule", GetModuleV8Callback);
    BindFunction(isolate_, global_template, "callVoosService", CallServiceV8Callback);
    BindFunction(isolate_, global_template, "getActorBoolean", GetActorBooleanV8Callback);
    BindFunction(isolate_, global_template, "setActorBoolean", SetActorBooleanV8Callback);
    BindFunction(isolate_, global_template, "getActorVector3", GetActorVector3V8Callback);
    BindFunction(isolate_, global_template, "setActorVector3", SetActorVector3V8Callback);
    BindFunction(isolate_, global_template, "getActorQuaternion", GetActorQuaternionV8Callback);
    BindFunction(isolate_, global_template, "setActorQuaternion", SetActorQuaternionV8Callback);
    BindFunction(isolate_, global_template, "getActorString", GetActorStringV8Callback);
    BindFunction(isolate_, global_template, "setActorString", SetActorStringV8Callback);
    BindFunction(isolate_, global_template, "getActorFloat", GetActorFloatV8Callback);
    BindFunction(isolate_, global_template, "setActorFloat", SetActorFloatV8Callback);

    Local<Context> context = Context::New(isolate_, nullptr, global_template);
    reusable_context_.Reset(isolate_, context);

    // Compile source and pull out the functions we expect.
    Context::Scope context_scope(context);

    // Compile and evaluate the JS
    if (!CompileBrainJavascript(javascript))
    {
      valid = false;
      return;
    }

    // Fetch out the functions we need to call
    if (!GetReusableFunctionReference(GetIsolate(), &context, "updateAgent", &reusable_update_agent_function_))
    {
      LogError("Could not find updateAgent function in brain JS! Brain JS:");
      LogError(javascript);
      valid = false;
      return;
    }

    // This one is optional.
    GetReusableFunctionReference(GetIsolate(), &context, "postMessageFlush", &reusable_post_message_flush_function_);

    valid = true;
  }

  ~VoosBrain()
  {
    // IMPORTANT: If reset's are not called, we will crash soon after.
    // Probably because we must reset before disposing the isolate?

    reusable_context_.Reset();
    reusable_update_agent_function_.Reset();
    reusable_post_message_flush_function_.Reset();
    for (auto &entry : module_namespaces_by_id)
    {
      entry.second.Reset();
    }
    module_namespaces_by_id.clear();

    // TODO do we really need an isolate per brain? Not really a relevant
    // question right now, since we only have one "brain".
    isolate_->Dispose();
    delete create_params.array_buffer_allocator;
  }

  static MaybeLocal<Module> ModuleResolveCallback(Local<Context> context,
                                                  Local<String> specifier,
                                                  Local<Module> referrer)
  {
    std::cerr << "TEMP TEMP resolver called, looking for specifier: " << *specifier << std::endl;
    std::cerr << "NOT IMPLEMENTED! Returning empty." << std::endl;
    return MaybeLocal<Module>();
  }

  bool SetModule(const char *moduleUid, const char *javascriptSource)
  {
    if (!IsStringValid(moduleUid, MAX_GUID_LENGTH) || !IsStringValid(javascriptSource, MAX_JAVASCRIPT_SOURCE_LENGTH))
    {
      return false;
    }

    if (!valid)
    {
      LogError("SetModule called on invalid brain");
      return false;
    }

    Isolate::Scope isolate_scope(GetIsolate());
    HandleScope handle_scope(GetIsolate());
    Local<Context> context = GetReusableContext();
    Context::Scope context_scope(context);

    TryCatch try_catch(isolate_);

    Local<String> sourceString =
        String::NewFromUtf8(isolate_, javascriptSource, NewStringType::kNormal).ToLocalChecked();

    auto moduleIdValue = String::NewFromUtf8(isolate_, moduleUid);
    ScriptOrigin origin(moduleIdValue,
                        Integer::New(isolate_, 0),
                        Integer::New(isolate_, 0),
                        Boolean::New(isolate_, false),
                        Local<Integer>(),
                        Local<Value>(),
                        Local<Boolean>(),
                        Boolean::New(isolate_, false),
                        Boolean::New(isolate_, true));
    ScriptCompiler::Source source(sourceString, origin);
    MaybeLocal<Module> compiledModule = ScriptCompiler::CompileModule(isolate_, &source);
    if (compiledModule.IsEmpty())
    {
      LogException("Error while compiling module JS: ", isolate_, &try_catch);
      return false;
    }

    Maybe<bool> instantiateResult = compiledModule.ToLocalChecked()->InstantiateModule(context, ModuleResolveCallback);
    if (instantiateResult.IsNothing())
    {
      LogException("Exception caught while instantiating module JS: ", isolate_, &try_catch);
      return false;
    }

    MaybeLocal<Value> evalResult = compiledModule.ToLocalChecked()->Evaluate(context);
    if (try_catch.HasCaught())
    {
      LogException("Exception caught while evaluating module JS: ", GetIsolate(), &try_catch);
      return false;
    }

    module_namespaces_by_id[std::string(moduleUid)].Reset(context->GetIsolate(), compiledModule.ToLocalChecked()->GetModuleNamespace());

    return true;
  }

  bool UpdateAgentJson(const char *state_json_string, BYTE_ARRAY bytes_in, int length_in, StringFunction report_result_json)
  {
    if (!valid)
    {
      LogError("UpdateAgent called on invalid brain");
      return false;
    }

    Isolate::Scope isolate_scope(GetIsolate());
    HandleScope handle_scope(GetIsolate());
    Local<Context> context = GetReusableContext();
    Context::Scope context_scope(context);

    // Create an object to hold input/output vars.

    Local<String> json_v8string = String::NewFromUtf8(GetIsolate(), state_json_string, NewStringType::kNormal).ToLocalChecked();
    MaybeLocal<Value> maybe_state_obj = JSON::Parse(context, json_v8string);

    Local<ArrayBuffer> array_buffer_in = ArrayBuffer::New(GetIsolate(), bytes_in, length_in, ArrayBufferCreationMode::kExternalized);

    if (maybe_state_obj.IsEmpty())
    {
      std::ostringstream oss;
      oss << "Failed to JSON-parse state:\n";
      oss << state_json_string;
      LogError(oss.str().c_str());
      return false;
    }
    Local<Value> state_obj = maybe_state_obj.ToLocalChecked();

    // Call updateAgent
    TryCatch try_catch(GetIsolate());
    const int argc = 2;
    Local<Value> argv[argc] = {state_obj, array_buffer_in};
    Local<Function> update_agent_function = Local<Function>::New(GetIsolate(), reusable_update_agent_function_);
    Local<Value> result;
    if (!update_agent_function->Call(context, context->Global(), argc, argv).ToLocal(&result))
    {
      LogException("Error while calling updateAgent: ", GetIsolate(), &try_catch);
      return false;
    }

    while (platform::PumpMessageLoop(V8_GLOBAL_STATE.platform, GetIsolate()))
      continue;
    if (try_catch.HasCaught())
    {
      LogException("Exception caught while pumping message loop: ", GetIsolate(), &try_catch);
      return false;
    }

    if (!reusable_post_message_flush_function_.IsEmpty())
    {
      const int argc = 2;
      Local<Value> argv[argc] = {state_obj, array_buffer_in};
      Local<Function> post_flush_function = Local<Function>::New(GetIsolate(), reusable_post_message_flush_function_);
      Local<Value> result;
      if (!post_flush_function->Call(context, context->Global(), argc, argv).ToLocal(&result))
      {
        LogException("Error while calling postMessageFlush: ", GetIsolate(), &try_catch);
        return false;
      }
    }

    if (report_result_json)
    {
      // Pull out the JSON state, stringify, and report.
      MaybeLocal<String> maybe_string = JSON::Stringify(context, state_obj);
      if (maybe_string.IsEmpty())
      {
        LogError("Could not JSON::Stringify object returned by JS! Not reporting to caller.");
        return false;
      }
      String::Utf8Value json_value(GetIsolate(), maybe_string.ToLocalChecked());
      if (json_value.length() > MAX_JSON_LENGTH)
      {
        LogError("JSON result too large. Not reporting to caller.");
        return false;
      }
      else
      {
        report_result_json(*json_value);
      }
    }

    return true;
  }

  void HandleServiceResult(CSHARP_STRING resultJson)
  {
    Local<String> json_v8string = String::NewFromUtf8(GetIsolate(), resultJson, NewStringType::kNormal).ToLocalChecked();
    last_service_call_result = JSON::Parse(GetReusableContext(), json_v8string);

    if (last_service_call_result.IsEmpty())
    {
      std::ostringstream err;
      err << "Could not parse service result json: " << resultJson;
      LogError(err);
    }
  }

private:
  Local<Value> GetModuleNamespaceObject(const char *module_id)
  {
    return module_namespaces_by_id[module_id].Get(GetIsolate());
  }

  static VoosBrain *GetThis(const FunctionCallbackInfo<Value> &info)
  {
    return (VoosBrain *)info.GetIsolate()->GetData(0);
  }

  static bool ExtractActorAccessorCommon(TEMP_ACTOR_ID *actor_id_out, ACTOR_FIELD_ID *field_id_out, const FunctionCallbackInfo<Value> &info)
  {
    if (info.Length() < 2)
    {
      LogError("Not enough args for actor accessor. Need at least 2.");
      return false;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();

    Maybe<uint32_t> actor_id = info[0]->Uint32Value(context);
    if (actor_id.IsNothing())
    {
      LogError("Invalid actor id argument given for actor accessor. Need a number.");
      return false;
    }
    *actor_id_out = actor_id.FromJust();

    Maybe<uint32_t> field_id = info[1]->Uint32Value(context);
    if (field_id.IsNothing())
    {
      LogError("Invalid field id argument given for actor accessor. Need a number.");
      return false;
    }

    *field_id_out = field_id.FromJust();
    return true;
  }

  static void GetActorBooleanV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_BOOLEAN_GETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_BOOLEAN_GETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    bool value_out = false;
    ACTOR_BOOLEAN_GETTER(actor_id, field_id, &value_out);
    info.GetReturnValue().Set(value_out);
  }

  static void SetActorBooleanV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_BOOLEAN_SETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_BOOLEAN_SETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Maybe<bool> value = info[2]->BooleanValue(context);

    if (value.IsNothing())
    {
      LogError("3rd argument needs to be a boolean!");
      return;
    }

    ACTOR_BOOLEAN_SETTER(actor_id, field_id, value.FromJust());
  }

  static void GetActorStringV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_STRING_GETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_STRING_GETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    // Clear it, to avoid using old values. To be safe.
    GetActorStringBuffer[0] = '\0';

    ACTOR_STRING_GETTER(actor_id, field_id, GetActorStringBuffer, sizeof(GetActorStringBuffer));
    Local<String> val = String::NewFromUtf8(info.GetIsolate(), GetActorStringBuffer);
    String::Utf8Value value(info.GetIsolate(), val);
    info.GetReturnValue().Set(val);

    // Clear this, to avoid any possibility of V8 referencing the buffer.
    GetActorStringBuffer[0] = '\0';
  }

  static void SetActorStringV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_STRING_SETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_STRING_SETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    if (!info[2]->IsString() && !info[2]->IsNullOrUndefined())
    {
      LogError("3rd argument needs to be a string or undefined!");
      return;
    }

    if (info[2]->IsNullOrUndefined())
    {
      // Send over as empty.
      ACTOR_STRING_SETTER(actor_id, field_id, "");
    }
    else
    {
      String::Utf8Value value(info.GetIsolate(), info[2]);
      ACTOR_STRING_SETTER(actor_id, field_id, *value);
    }
  }

  static void GetActorVector3V8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_VECTOR3_GETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_VECTOR3_GETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Local<Value> out_val = info[2];
    if (out_val.IsEmpty() || !out_val->IsObject())
    {
      LogError("GetActorVector3V8Callback: Third argument must be an object");
      return;
    }

    float xout = 0.0;
    float yout = 0.0;
    float zout = 0.0;
    ACTOR_VECTOR3_GETTER(actor_id, field_id, &xout, &yout, &zout);

    Local<Object> out_obj = out_val->ToObject(context).ToLocalChecked();
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "x"), Number::New(info.GetIsolate(), xout));
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "y"), Number::New(info.GetIsolate(), yout));
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "z"), Number::New(info.GetIsolate(), zout));
  }

  static void SetActorVector3V8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_VECTOR3_SETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_VECTOR3_SETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Maybe<double> x = info[2]->NumberValue(context);
    if (x.IsNothing())
    {
      LogError("3rd argument needs to be a number!");
      return;
    }
    Maybe<double> y = info[3]->NumberValue(context);
    if (y.IsNothing())
    {
      LogError("4th argument needs to be a number!");
      return;
    }
    Maybe<double> z = info[4]->NumberValue(context);
    if (z.IsNothing())
    {
      LogError("5th argument needs to be a number!");
      return;
    }

    ACTOR_VECTOR3_SETTER(actor_id, field_id, (float)x.FromJust(), (float)y.FromJust(), (float)z.FromJust());
  }

  static void GetActorFloatV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_FLOAT_GETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_FLOAT_GETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    float value_out = false;
    ACTOR_FLOAT_GETTER(actor_id, field_id, &value_out);
    info.GetReturnValue().Set(value_out);
  }

  static void SetActorFloatV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_FLOAT_SETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_FLOAT_SETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Maybe<double> value = info[2]->NumberValue(context);

    if (value.IsNothing())
    {
      LogError("3rd argument needs to be a float!");
      return;
    }

    ACTOR_FLOAT_SETTER(actor_id, field_id, (float)value.FromJust());
  }

  static void GetActorQuaternionV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_QUATERNION_GETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_QUATERNION_GETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Local<Value> out_val = info[2];
    if (out_val.IsEmpty() || !out_val->IsObject())
    {
      LogError("GetActorQuaternionV8Callback: Third argument must be an object");
      return;
    }

    float xout = 0.0;
    float yout = 0.0;
    float zout = 0.0;
    float wout = 0.0;
    ACTOR_QUATERNION_GETTER(actor_id, field_id, &xout, &yout, &zout, &wout);

    Local<Object> out_obj = out_val->ToObject(context).ToLocalChecked();
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "x"), Number::New(info.GetIsolate(), xout));
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "y"), Number::New(info.GetIsolate(), yout));
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "z"), Number::New(info.GetIsolate(), zout));
    out_obj->Set(String::NewFromUtf8(info.GetIsolate(), "w"), Number::New(info.GetIsolate(), wout));
  }

  static void SetActorQuaternionV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (ACTOR_QUATERNION_SETTER == nullptr)
    {
      // TODO ideally we'd cause a V8 exception throw
      LogError("No ACTOR_QUATERNION_SETTER set");
      return;
    }

    TEMP_ACTOR_ID actor_id;
    ACTOR_FIELD_ID field_id;
    if (!ExtractActorAccessorCommon(&actor_id, &field_id, info))
    {
      return;
    }

    Local<Context> context = info.GetIsolate()->GetCurrentContext();
    Maybe<double> x = info[2]->NumberValue(context);
    if (x.IsNothing())
    {
      LogError("3rd argument needs to be a number!");
      return;
    }
    Maybe<double> y = info[3]->NumberValue(context);
    if (y.IsNothing())
    {
      LogError("4th argument needs to be a number!");
      return;
    }
    Maybe<double> z = info[4]->NumberValue(context);
    if (z.IsNothing())
    {
      LogError("5th argument needs to be a number!");
      return;
    }
    Maybe<double> w = info[5]->NumberValue(context);
    if (z.IsNothing())
    {
      LogError("6th argument needs to be a number!");
      return;
    }

    ACTOR_QUATERNION_SETTER(actor_id, field_id, (float)x.FromJust(), (float)y.FromJust(), (float)z.FromJust(), (float)w.FromJust());
  }

  // Short-cut for non-performance-sensitive functions, like using Unity's Physics.Raycast.
  // Which services are available should be agreed upon between the host and the JS code.
  static void CallServiceV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    if (CALL_SERVICE_FUNCTION == nullptr)
    {
      LogError("Scripts wanted to use callService, but no call-service-function was set (via SetCallServiceFunction).");
      return;
    }

    VoosBrain *brain = GetThis(info);
    String::Utf8Value serviceName(info.GetIsolate(), info[0]);

    if (serviceName.length() > MAX_GUID_LENGTH)
    {
      return;
    }

    MaybeLocal<String> maybeArgsJson = JSON::Stringify(brain->GetReusableContext(), info[1]);

    if (maybeArgsJson.IsEmpty())
    {
      std::ostringstream err;
      err << "Could not JSON-nify arguments when trying to call service '" << *serviceName << "'. Returning nothing.";
      LogError(err);
      return;
    }

    String::Utf8Value argsJson(info.GetIsolate(), maybeArgsJson.ToLocalChecked());
    if (argsJson.length() > MAX_JSON_LENGTH)
    {
      return;
    }
    CallService(*serviceName, *argsJson, brain);

    if (brain->last_service_call_result.IsEmpty())
    {
      std::ostringstream err;
      err << "Failed to capture result of service '" << *serviceName << "'. See prior errors for details. Input args json: " << *argsJson;
      LogError(err);
      return;
    }

    info.GetReturnValue().Set(brain->last_service_call_result.ToLocalChecked());
  }

  static void GetModuleV8Callback(const FunctionCallbackInfo<Value> &info)
  {
    VoosBrain *brain = GetThis(info);
    String::Utf8Value module_id(info.GetIsolate(), info[0]);
    if (module_id.length() > MAX_GUID_LENGTH)
    {
      return;
    }
    info.GetReturnValue().Set(brain->GetModuleNamespaceObject(*module_id));
  }

  void SetField(Local<Object> obj, std::string key_string, Local<Value> value)
  {
    Local<String> key = String::NewFromUtf8(GetIsolate(), key_string.c_str(), NewStringType::kNormal).ToLocalChecked();
    obj->Set(key, value);
  }

  float GetFloatField(Local<Object> obj, std::string key_string)
  {
    Local<String> key = String::NewFromUtf8(GetIsolate(), key_string.c_str(), NewStringType::kNormal).ToLocalChecked();
    // TODO get rid of 'Checked' calls
    return (float)obj->Get(key)->NumberValue(GetIsolate()->GetCurrentContext()).ToChecked();
  }

  // This should be waaaayyy faster than Context::New(...).
  Local<Context> GetReusableContext()
  {
    return Local<Context>::New(isolate_, reusable_context_);
  }

  // Everything in this block is fairly cheap. Even doing it 100x doesn't affect timings much.
  // Assumes the isolate has a context active.
  bool CompileBrainJavascript(const char *javascriptSource)
  {
    TryCatch try_catch(isolate_);

    Local<String> source =
        String::NewFromUtf8(isolate_, javascriptSource, NewStringType::kNormal).ToLocalChecked();

    MaybeLocal<Script> compiled = Script::Compile(isolate_->GetCurrentContext(), source);
    if (compiled.IsEmpty())
    {
      LogException("Error while compiling brain JS: ", isolate_, &try_catch);
      return false;
    }

    MaybeLocal<Value> result = compiled.ToLocalChecked()->Run(isolate_->GetCurrentContext());
    if (try_catch.HasCaught())
    {
      LogException("Exception caught while running brain JS: ", isolate_, &try_catch);
      return false;
    }

    return true;
  }

  Isolate *GetIsolate() { return isolate_; }

  Isolate::CreateParams create_params;
  Isolate *isolate_;
  Global<Context> reusable_context_;
  Global<Function> reusable_update_agent_function_;
  Global<Function> reusable_post_message_flush_function_;
  std::map<std::string, Global<Value>> module_namespaces_by_id;

  MaybeLocal<Value> last_service_call_result;
};

// TODO move this into a class.
std::map<std::string, std::unique_ptr<VoosBrain>> BRAIN_BY_UID;

// Keeps an isolate around so you don't have to create a new one each time you want to run some JS.
class ReusableContext
{
public:
  ReusableContext() : isolate_(nullptr)
  {
    create_params.array_buffer_allocator = v8::ArrayBuffer::Allocator::NewDefaultAllocator();
    isolate_ = Isolate::New(create_params);

    // Create the context
    Isolate::Scope isolate_scope(isolate_);
    // Create a stack-allocated handle scope.
    HandleScope handle_scope(isolate_);

    Local<ObjectTemplate> global_template = ObjectTemplate::New(isolate_);
    SetupGlobalTemplate(isolate_, global_template);

    Local<Context> context = Context::New(isolate_, nullptr, global_template);
    reusableContext_.Reset(isolate_, context);
  }

  Isolate *GetIsolate() { return isolate_; }

  void Evaluate(const char *javascriptSource)
  {
    Isolate::Scope isolate_scope(isolate_);
    HandleScope handle_scope(isolate_);
    auto context = GetReusableContext();
    Context::Scope context_scope(context);
    CompileAndRun(javascriptSource);
  }

  ~ReusableContext()
  {
    reusableContext_.Reset();
    isolate_->Dispose();
    delete create_params.array_buffer_allocator;
  }

  // This should be waaaayyy faster than Context::New(...).
  v8::Local<v8::Context> GetReusableContext()
  {
    return v8::Local<v8::Context>::New(isolate_, reusableContext_);
  }

  // Everything in this block is fairly cheap. Even doing it 100x doesn't affect timings much.
  // Assumes the isolate has a context active.
  void CompileAndRun(const char *javascriptSource)
  {
    Local<String> source =
        String::NewFromUtf8(isolate_, javascriptSource, NewStringType::kNormal).ToLocalChecked();
    // Compile the source code.
    MaybeLocal<Script> compiled = Script::Compile(isolate_->GetCurrentContext(), source);
    if (compiled.IsEmpty())
    {
      LogError("Could not compile! JS source:");
      LogError(javascriptSource);
      return;
    }

    TryCatch try_catch(isolate_);
    MaybeLocal<Value> result = compiled.ToLocalChecked()->Run(GetIsolate()->GetCurrentContext());
    if (try_catch.HasCaught())
    {
      LogError("Exception caught while running:");
      String::Utf8Value message(GetIsolate(), try_catch.Message()->Get());
      LogError(*message);
    }
  }

private:
  Isolate::CreateParams create_params;
  Isolate *isolate_;
  Global<Context> reusableContext_;
};

static ReusableContext *CONTEXT_ = nullptr;

extern "C"
{
  void TestSort(int a[], int length)
  {
    std::sort(a, a + length);
  }

  void SetDebugLogFunction(StringFunction function)
  {
    HOST_DEBUG_LOG_FUNCTION = function;
    // Log("Host debug log function set");
  }

  void SetErrorLogFunction(StringFunction function)
  {
    HOST_ERROR_LOG_FUNCTION = function;
    // Log("Host error log function set");
  }

  int InitializeV8(const char *runtimeFilesDirectory)
  {
    if (!IsStringValid(runtimeFilesDirectory, MAX_FILEPATH_LENGTH))
    {
      return 1;
    }
    if (strnlen(runtimeFilesDirectory, MAX_FILEPATH_LENGTH))
      std::cout << "Got runtime files dir of " << runtimeFilesDirectory << std::endl;
    std::string exePath = std::string(runtimeFilesDirectory) + "/foo.exe";
    return InitializeV8WithExecutablePath(exePath.c_str());
  }

  int InitializeV8WithExecutablePath(const char *executablePath)
  {
    if (!IsStringValid(executablePath, MAX_FILEPATH_LENGTH))
    {
      return 1;
    }

    if (V8_GLOBAL_STATE.platform != nullptr)
    {
      std::cout << "WARNING: InitializeV8WithExecutablePath called, but platform was already initialized! Doing nothing." << std::endl;
      return 1;
    }

    // Initialize V8.
    V8::InitializeICUDefaultLocation(executablePath);
    V8::InitializeExternalStartupData(executablePath);
    V8_GLOBAL_STATE.platform = platform::CreateDefaultPlatform();
    V8::InitializePlatform(V8_GLOBAL_STATE.platform);
    if (V8::Initialize())
    {
      CONTEXT_ = new ReusableContext();
      return 0;
    }
    else
    {
      return 1;
    }
  }

  void Evaluate(const char *javascriptSource)
  {
    if (!IsStringValid(javascriptSource, MAX_JAVASCRIPT_SOURCE_LENGTH))
    {
      return;
    }
    CONTEXT_->Evaluate(javascriptSource);
  }

  int EvaluateToInteger(const char *javascriptSource)
  {
    if (!IsStringValid(javascriptSource, MAX_JAVASCRIPT_SOURCE_LENGTH))
    {
      return -1;
    }

    int rv = 0;

    // Create a new Isolate and make it the current one.
    Isolate::CreateParams create_params;
    create_params.array_buffer_allocator =
        v8::ArrayBuffer::Allocator::NewDefaultAllocator();
    Isolate *isolate = Isolate::New(create_params);
    {
      Isolate::Scope isolate_scope(isolate);
      // Create a stack-allocated handle scope.
      HandleScope handle_scope(isolate);

      Local<ObjectTemplate> global_template = ObjectTemplate::New(isolate);
      SetupGlobalTemplate(isolate, global_template);

      Local<Context> context = Context::New(isolate, 0, global_template);

      // Enter the context for compiling and running the hello world script.
      Context::Scope context_scope(context);

      // Create a string containing the JavaScript source code.
      Local<String> source =
          String::NewFromUtf8(isolate, javascriptSource, NewStringType::kNormal).ToLocalChecked();
      // Compile the source code.
      Local<Script> script = Script::Compile(context, source).ToLocalChecked();
      // Run the script to get the result.
      Local<Value> result = script->Run(context).ToLocalChecked();
      rv = result->Int32Value(context).FromJust();
    }
    // Dispose the isolate and tear down V8.
    isolate->Dispose();
    delete create_params.array_buffer_allocator;

    return rv;
  }

  int DeinitializeV8()
  {
    if (V8_GLOBAL_STATE.platform == nullptr)
    {
      std::cout << "WARNING: DeinitializeV8 called before InitializeV8 called. Doing nothing." << std::endl;
      return 1;
    }

    BRAIN_BY_UID.clear();

    if (V8::Dispose())
    {
      V8::ShutdownPlatform();
      delete V8_GLOBAL_STATE.platform;
      V8_GLOBAL_STATE.platform = nullptr;
      return 0;
    }
    else
    {
      return 1;
    }
  }

  bool SetModule(CSHARP_STRING brainUid, CSHARP_STRING moduleUid, CSHARP_STRING javascript)
  {
    std::string brainKey(brainUid);
    if (BRAIN_BY_UID.find(brainKey) == BRAIN_BY_UID.end())
    {
      std::ostringstream errs;
      errs << "Unknown brain UID: " << brainUid;
      LogError(errs.str().c_str());
      return false;
    }
    else
    {
      VoosBrain &brain = *BRAIN_BY_UID[brainKey];
      return brain.SetModule(moduleUid, javascript);
    }
  }

  bool ResetBrain(CSHARP_STRING brainUid, CSHARP_STRING javascript)
  {
    if (!IsStringValid(brainUid, MAX_GUID_LENGTH) || !IsStringValid(javascript, MAX_JAVASCRIPT_SOURCE_LENGTH))
    {
      return false;
    }
    std::string brainKey(brainUid);
    std::unique_ptr<VoosBrain> brain = std::make_unique<VoosBrain>(javascript);
    if (brain->valid)
    {
      BRAIN_BY_UID[brainKey] = std::move(brain);
      return true;
    }
    else
    {
      std::ostringstream msg;
      msg << "Failed to reset brain with javascript:" << std::endl;
      PrintWithLineNumbers(msg, javascript);
      LogError(msg.str().c_str());
      return false;
    }
  }

  BYTE_ARRAY DummyArray = {};

  bool UpdateAgentJson(CSHARP_STRING brainUid, CSHARP_STRING agentUid, CSHARP_STRING json_in, StringFunction report_result)
  {
    return UpdateAgentJsonBytes(brainUid, agentUid, json_in, DummyArray, 0, report_result);
  }

  bool UpdateAgentJsonBytes(CSHARP_STRING brainUid, CSHARP_STRING agentUid, CSHARP_STRING json_in, BYTE_ARRAY bytes_in, int length_in, StringFunction report_result)
  {
    if (!IsStringValid(brainUid, MAX_GUID_LENGTH) || !IsStringValid(agentUid, MAX_GUID_LENGTH) || !IsStringValid(json_in, MAX_JSON_LENGTH))
    {
      return false;
    }

    if (length_in > MAX_BUFFER_SIZE)
    {
      LogError("Buffer was too big. Doing nothing.");
      return false;
    }

    std::string brainKey(brainUid);
    if (BRAIN_BY_UID.find(brainKey) == BRAIN_BY_UID.end())
    {
      std::ostringstream errs;
      errs << "Unknown brain UID: " << brainUid;
      LogError(errs.str().c_str());
      return false;
    }
    else
    {
      VoosBrain &brain = *BRAIN_BY_UID[brainKey];
      return brain.UpdateAgentJson(json_in, bytes_in, length_in, report_result);
    }
  }

  void SetLookupIntFunction(LookupIntFunction function)
  {
    LOOK_UP_INT_FUNCTION = function;
  }
}
