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

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class UpdateMismatchChecker : MonoBehaviour
{
  [SerializeField] public bool log = false;

  int updatesSinceLastFixedUpdate = 0;
  int fixedUpdatesSinceLastUpdate = 0;

  void FixedUpdate()
  {
    fixedUpdatesSinceLastUpdate++;
    using (Util.Profile("TooManyFixed"))
    {
      if (updatesSinceLastFixedUpdate > 1)
      {
        if (log)
        {
          Util.LogWarning($"{updatesSinceLastFixedUpdate} updates since last fixed!");
        }
      }
    }
    updatesSinceLastFixedUpdate = 0;
  }

  void Update()
  {
    updatesSinceLastFixedUpdate++;
    using (Util.Profile("TooManyUpdates"))
    {
      if (fixedUpdatesSinceLastUpdate > 1)
      {
        if (log)
        {
          Util.LogWarning($"{fixedUpdatesSinceLastUpdate} fixed since last update!");
        }
      }
    }
    fixedUpdatesSinceLastUpdate = 0;
  }
}