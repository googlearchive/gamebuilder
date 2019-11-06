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

public class JsonGarbageTest : MonoBehaviour
{

  [System.Serializable]
  struct AddForceArgs
  {
    public Vector3 force;
    public Vector3 force2;
    public int forceMode;
  }

  // Use this for initialization
  void Start()
  {

  }

  static string jsonString = @"
{
""force"" : { ""x"": 1, ""y"" : 2, ""z"" : 3},
""force2"" : { ""x"": 1, ""y"" : 2, ""z"" : 3},
    ""forceMode"" : 123}
";

  // Update is called once per frame
  void Update()
  {
    //AddForceArgs args = new AddForceArgs { force = Vector3.up, forceMode = 123 };
    AddForceArgs args;

    using (new Util.ProfileBlock("fromJson"))
    {
      args = JsonUtility.FromJson<AddForceArgs>(jsonString);
    }

    string stringCheck = null;
    using (new Util.ProfileBlock("toJson"))
    {
      stringCheck = JsonUtility.ToJson(args);
    }
    Debug.Log(stringCheck);

    //}

    //string jsonStr = JsonUtility.ToJson(args);
    //Debug.Log(jsonStr);

  }
}
