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

#include <stdio.h>

#include "../v8_in_unity/v8_in_unity.h"
#include "timer.h"
#include <iostream>
#include <vector>
#include <string>
#include <sstream>
#include <cmath>

using namespace std;

// TODO uhh use a unit test framework.. :P
static int NUM_FAILURES = 0;
static int NUM_CHECKS = 0;

#define CHECK(x)                                                \
  NUM_CHECKS++;                                                 \
  if (!(x))                                                     \
  {                                                             \
    NUM_FAILURES++;                                             \
    printf("\n***** ASSERTION FAILURE ON LINE %d\n", __LINE__); \
  }
#define CHECK_APPROX_EQ(expected, actual, tolerance) CHECK(fabs((expected) - (actual)) < (tolerance))

#define MYMIN(x, y) ((x) < (y) ? (x) : (y))

static std::ostringstream error_msgs;

static std::string reported_json;

void myReportUpdatedAgentJson(const char *json)
{
  reported_json = std::string(json);
}

void myDebugLogFunction(const char *msg)
{
  cout << "(V8) " << msg << endl;
}

void myErrorLogFunction(const char *msg)
{
  error_msgs << msg << endl;
  // Commented out because a lot of cases test for the existence of errors. Can look messy.
  // std::cerr << msg << endl;
}

void myCallServiceFunction(const char *serviceName, const char *jsonArgs, ReportServiceResultFunction reportResult)
{
  std::ostringstream result;
  int x = atoi(jsonArgs);

  if (strcmp(serviceName, "addOne") == 0)
  {
    result << (x + 1);
  }
  else if (strcmp(serviceName, "addTwo") == 0)
  {
    result << (x + 2);
  }
  else
  {
    CHECK(false);
  }

  reportResult(result.str().c_str());
}

void testCompileError()
{
  Evaluate("if( {}");
}

void testRunError()
{
  Evaluate("functionDoesNotExist();");
}

void testSort()
{
  vector<int> foo = {2, 3, 1};
  TestSort(&foo[0], (int)foo.size());
  CHECK(foo[0] == 1);
  CHECK(foo[1] == 2);
  CHECK(foo[2] == 3);
}

void testDebugLog()
{
  int rv = EvaluateToInteger("\
sysLog(\"here is a log message from JS!\"); \
456;");
  CHECK(rv == 456);
}

void testEvaluateToInteger()
{
  int rv = EvaluateToInteger("\
var sum = 1; \
for(let i = 0; i < 5; i++) { \
sum *= 2; \
} \
sum;");
  CHECK(rv == 32);
}

void testBaselinePerformance()
{
  CpuTimer timer("Many Evals");
  const int N = 5000;
  for (int i = 0; i < N; i++)
  {
    Evaluate("Math.random();");
  }
  double msPerEval = timer.GetElapsedMilliSeconds() / N;

  // Yeah not great that this is hard-coded for my laptop...
  CHECK(msPerEval < 0.01);
  cout << "Average ms/eval: " << msPerEval << endl;
}

void testUpdateAgentFail()
{
  // Give valid JS, but runtime error.
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  // This code compiles, so reset succeeds..
  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {"
                   "  doesNotExist();"
                   "}"));

  // ..but the function DNE, so it should fail during update.
  error_msgs.str("");
  CHECK(!UpdateAgentJson(brainUid, agentUid, "{\"count\": 3}", myReportUpdatedAgentJson));
  CHECK(error_msgs.str().find("doesNotExist") != string::npos);
}

void testUpdateAgentJson()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {"
                   "  state.result = '';"
                   "  for(let i = 0; i < state.count; i++) {"
                   "    state.result += 'foo';"
                   "  }"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"count\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"count\":3,\"result\":\"foofoofoo\"}");
}

void testBrainsByValueNotAddress()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {"
                   "  state.result = '';"
                   "  for(let i = 0; i < state.count; i++) {"
                   "    state.result += 'foo';"
                   "  }"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"count\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"count\":3,\"result\":\"foofoofoo\"}");

  // Use a brain ID at a different address..to make sure we're mapping by value, not by address.
  std::string brainUid2("brain");
  CHECK(ResetBrain(brainUid2.c_str(),
                   "function updateAgent(state) {"
                   "  state.result = '';"
                   "  for(let i = 0; i < state.count; i++) {"
                   "    state.result += 'bar';"
                   "  }"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"count\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"count\":3,\"result\":\"barbarbar\"}");
}

void testHelpfulCompileErrors()
{
  const char *brainUid = "brain";

  error_msgs.str("");
  CHECK(error_msgs.str().size() == 0);

  CHECK(!ResetBrain(brainUid,
                    "function updateAgent(state) {\n"
                    "  state.result = '';\n"
                    "  for(let i = 0; i < state.count; i++) {\n"
                    "    state.result += 'foo';\n"
                    "syntaxErrorHere!!\n"
                    "  }\n"
                    "}\n"));

  CHECK(error_msgs.str().size() > 0);
  CHECK(error_msgs.str().find(":5:") != string::npos);
  CHECK(error_msgs.str().find("syntaxErrorHere") != string::npos);
}

void testLogError()
{
  error_msgs.str("");
  CHECK(error_msgs.str().size() == 0);

  Evaluate("sysError('this is just a test');");

  CHECK(error_msgs.str().size() > 0);
  CHECK(error_msgs.str().find("this is just a test") != string::npos);
}

void testPostMessageFlush()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {"
                   "  state.result = 'lastTouchedByUpdateAgent';"
                   "  let resolver = null;"
                   "  new Promise( (resolve, reject) => resolver = resolve ).then(() => state.result = 'lastTouchedByUpdatePromise');"
                   "  resolver();"
                   "}"
                   "function postMessageFlush(state) {"
                   "  state.result = 'lastTouchedByPostFlush';"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"result\":\"lastTouchedByPostFlush\"}");
}

void testCallbackOverhead()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  {
    CHECK(ResetBrain(brainUid,
                     "function updateAgent(state) {"
                     "  let N = 500000;"
                     "  let x = 0;"
                     "  let m = {fortyTwo: 42, thirteen: 13};"
                     "  for (var i = 0; i < N; i++) {"
                     "    if(i % 2 == 0) { x += m.fortyTwo; }"
                     "    else { x += m.thirteen; }"
                     "    x += m.thirteen;"
                     "  }"
                     "}"));

    CpuTimer timer("JS only update");
    CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  }
  {
    CHECK(ResetBrain(brainUid,
                     "function updateAgent(state) {"
                     "  let N = 500000;"
                     "  let x = 0;"
                     "  for (var i = 0; i < N; i++) {"
                     "    if(i % 2 == 0) { x += fortyTwo(); }"
                     "    else { x += thirteen(); }"
                     "    x += thirteen();"
                     "  }"
                     "}"));

    CpuTimer timer("Native callback update");
    CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  }
}

void testModules()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";
  const char *moduleName = "FooMath";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  state.y = getVoosModule('FooMath')['double'](state.x);\n"
                   "  state.z = getVoosModule('FooMath')['triple'](state.x);\n"
                   "}\n"));

  CHECK(SetModule(brainUid, moduleName,
                  "export function double(x) {\n"
                  "  return 2 * x;\n"
                  "}\n"
                  "export function triple(x) {\n"
                  "  return 3 * x;\n"
                  "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"x\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"x\":3,\"y\":6,\"z\":9}");
}

void testModuleHotload()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";
  const char *moduleName = "FooMath";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  state.y = getVoosModule('FooMath')['transform'](state.x);\n"
                   "}\n"));

  CHECK(SetModule(brainUid, moduleName,
                  "export function transform(x) {\n"
                  "  return 2 * x;\n"
                  "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"x\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"x\":3,\"y\":6}");

  // Hotload with different math

  CHECK(SetModule(brainUid, moduleName,
                  "export function transform(x) {\n"
                  "  return 3 * x;\n"
                  "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"x\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"x\":3,\"y\":9}");
}

void testManyModules()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  state.y = getVoosModule('FooMath')['transform'](state.x);\n"
                   "  state.z = getVoosModule('BarMath')['transform'](state.x);\n"
                   "}\n"));

  CHECK(SetModule(brainUid, "FooMath",
                  "export function transform(x) {\n"
                  "  return 2 * x;\n"
                  "}\n"));

  CHECK(SetModule(brainUid, "BarMath",
                  "export function transform(x) {\n"
                  "  return 3 * x;\n"
                  "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{\"x\": 3}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"x\":3,\"y\":6,\"z\":9}");
}

void testPumpPromisesMessageLoopCalledAfterUpdateAgent()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";
  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  state.resolved = false;\n"
                   "  const p = Promise.resolve(1).then(() => state.resolved = true);\n"
                   "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK(reported_json == "{\"resolved\":true}");
}

void testBasicService()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";
  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  state.four = callVoosService('addOne', 3);\n"
                   "  state.five = callVoosService('addTwo', 3);\n"
                   "}\n"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  std::cout << reported_json;
  CHECK(reported_json == "{\"four\":4,\"five\":5}");
}

void testVeryLongLogMessage()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";
  CHECK(ResetBrain(brainUid,
                   "function updateAgent(state) {\n"
                   "  sysError('x'.repeat(10 * 1024 * 1024));\n"
                   "}\n"));
  error_msgs.str("");
  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK(error_msgs.str().length() == 0);
}

void testVeryLongCode()
{
  const char *brainUid = "brain";
  std::string code(10 * 1024 * 1024, 'x');
  CHECK(!ResetBrain(brainUid, code.c_str()));
}

void testUpdateAgentArrayBuffer()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  std::vector<uint8_t> buf(1);
  buf[0] = 42;

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(foo, buffer) {"
                   "  const view = new DataView(buffer);"
                   "  if(view.getUint8(0) != 42) throw 'ahhh';"
                   "  view.setUint8(0, 123);"
                   "}"));

  CHECK(UpdateAgentJsonBytes(brainUid, agentUid, "{}", &buf[0], (int)buf.size(), myReportUpdatedAgentJson));
  CHECK(buf[0] == 123);
}

void testUnjsonbleResponseDoesNotCrash()
{
  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  std::vector<uint8_t> buf(1);
  buf[0] = 42;

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(obj) {"
                   "  obj['cycle'] = obj;"
                   "}"));

  error_msgs.str("");
  CHECK(false == UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK(error_msgs.str().find("Could not JSON::Stringify object") != string::npos);
}

TEMP_ACTOR_ID expected_actor_id = 12;
TEMP_ACTOR_ID expected_field_id = 34;
bool actor_bool_value = false;

float actor_vec3_x = 0.1f;
float actor_vec3_y = 0.1f;
float actor_vec3_z = 0.1f;

float actor_quat_x = 0.1f;
float actor_quat_y = 0.1f;
float actor_quat_z = 0.1f;
float actor_quat_w = 0.1f;

void TestActorBooleanGetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, bool *value_out)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  *value_out = actor_bool_value;
}
void TestActorBooleanSetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, bool value)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  actor_bool_value = value;
}

void TestActorVector3Getter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float *x, float *y, float *z)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  *x = actor_vec3_x;
  *y = actor_vec3_y;
  *z = actor_vec3_z;
}

void TestActorVector3Setter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float x, float y, float z)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  actor_vec3_x = x;
  actor_vec3_y = y;
  actor_vec3_z = z;
}

void TestActorQuaternionGetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float *x, float *y, float *z, float *w)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  *x = actor_quat_x;
  *y = actor_quat_y;
  *z = actor_quat_z;
  *w = actor_quat_w;
}

void TestActorQuaternionSetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, float x, float y, float z, float w)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  actor_quat_x = x;
  actor_quat_y = y;
  actor_quat_z = z;
  actor_quat_w = w;
}

void testActorBoolAccessors()
{
  SetActorBooleanGetter(TestActorBooleanGetter);
  SetActorBooleanSetter(TestActorBooleanSetter);

  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  actor_bool_value = true;

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(obj) {"
                   "  const prev = getActorBoolean(12, 34);"
                   "  if(prev != true) throw 'ahhh';"
                   "  setActorBoolean(12, 34, false);"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK(actor_bool_value == false);
}

void testActorVec3Accessors()
{
  SetActorVector3Getter(TestActorVector3Getter);
  SetActorVector3Setter(TestActorVector3Setter);

  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  actor_vec3_x = 12;
  actor_vec3_y = 34;
  actor_vec3_z = 56;

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(obj) {"
                   "  const vec = {};"
                   "  getActorVector3(12, 34, vec);"
                   "  if(Math.round(vec.x) != 12) throw 'ahhh';"
                   "  if(Math.round(vec.y) != 34) throw 'ahhh';"
                   "  if(Math.round(vec.z) != 56) throw 'ahhh';"
                   "  setActorVector3(12, 34, 1, 2, 3);"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK_APPROX_EQ(1, actor_vec3_x, 1e-4);
  CHECK_APPROX_EQ(2, actor_vec3_y, 1e-4);
  CHECK_APPROX_EQ(3, actor_vec3_z, 1e-4);
}

void testActorQuatAccessors()
{
  SetActorQuaternionGetter(TestActorQuaternionGetter);
  SetActorQuaternionSetter(TestActorQuaternionSetter);

  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  actor_quat_x = 12;
  actor_quat_y = 34;
  actor_quat_z = 56;
  actor_quat_w = 78;

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(obj) {"
                   "  const vec = {};"
                   "  getActorQuaternion(12, 34, vec);"
                   "  if(Math.round(vec.x) != 12) throw 'ahhh';"
                   "  if(Math.round(vec.y) != 34) throw 'ahhh';"
                   "  if(Math.round(vec.z) != 56) throw 'ahhh';"
                   "  if(Math.round(vec.w) != 78) throw 'ahhh';"
                   "  setActorQuaternion(12, 34, 1, 2, 3, 4);"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  CHECK_APPROX_EQ(1, actor_quat_x, 1e-4);
  CHECK_APPROX_EQ(2, actor_quat_y, 1e-4);
  CHECK_APPROX_EQ(3, actor_quat_z, 1e-4);
  CHECK_APPROX_EQ(4, actor_quat_w, 1e-4);
}

std::string test_actor_string;

void TestActorStringGetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, char *out, int max_bytes)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  memcpy(out, test_actor_string.c_str(), MYMIN(max_bytes, test_actor_string.length()));
}

void TestActorStringSetter(TEMP_ACTOR_ID actor_id, ACTOR_FIELD_ID field_id, const char *value)
{
  CHECK(actor_id == expected_actor_id);
  CHECK(field_id == expected_field_id);
  test_actor_string = std::string(value);
}

void testStringAccessors()
{
  SetActorStringGetter(TestActorStringGetter);
  SetActorStringSetter(TestActorStringSetter);

  const char *agentUid = "pinky";
  const char *brainUid = "brain";

  test_actor_string = std::string("€ unicode TM: ™");

  CHECK(ResetBrain(brainUid,
                   "function updateAgent(obj) {\n"
                   "  const vec = {};\n"
                   "  const s = getActorString(12, 34);\n"
                   "  if(s != '€ unicode TM: ™')  throw 'did not get expected string!';\n"
                   "  const newS = '€ all € about € the € euros €™';\n"
                   "  setActorString(12, 34, newS);\n"
                   "}"));

  CHECK(UpdateAgentJson(brainUid, agentUid, "{}", myReportUpdatedAgentJson));
  std::string expected("€ all € about € the € euros €™");
  CHECK(expected == test_actor_string);
}

int main(int argc, char *argv[])
{
  SetDebugLogFunction(myDebugLogFunction);
  SetErrorLogFunction(myErrorLogFunction);
  SetCallServiceFunction(myCallServiceFunction);

  int initRv = InitializeV8WithExecutablePath(argv[0]);
  if (initRv != 0)
  {
    cerr << "Initialization returned non-zero: " << initRv << endl;
    return initRv;
  }

  initRv = InitializeV8WithExecutablePath(argv[0]);
  // Calling this a second time should be an error, but not crash.
  CHECK(initRv == 1);

  testCompileError();
  testRunError();
  testSort();
  testDebugLog();
  testEvaluateToInteger();
  testBaselinePerformance();
  testUpdateAgentFail();
  testUpdateAgentJson();
  testBrainsByValueNotAddress();
  testHelpfulCompileErrors();
  testLogError();
  testCallbackOverhead();
  testPostMessageFlush();
  testModules();
  testModuleHotload();
  testManyModules();
  testPumpPromisesMessageLoopCalledAfterUpdateAgent();
  testBasicService();
  testVeryLongLogMessage();
  testVeryLongCode();
  testUpdateAgentArrayBuffer();
  testUnjsonbleResponseDoesNotCrash();
  testActorBoolAccessors();
  testActorVec3Accessors();
  testActorQuatAccessors();
  testStringAccessors();

  int deinitRv = DeinitializeV8();
  if (deinitRv != 0)
  {
    cerr << "De-initialization returned non-zero: " << deinitRv << endl;
    return deinitRv;
  }

  if (NUM_FAILURES > 0)
  {
    cerr << "\n\n*************\n"
         << NUM_FAILURES << " failures reported!!"
         << "\n*************\n";
  }
  else
  {
    cout << "\nAll " << NUM_CHECKS << " checks passed :)\n"
         << endl;
  }
  return NUM_FAILURES;
}
