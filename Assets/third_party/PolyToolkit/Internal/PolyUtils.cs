// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace PolyToolkitInternal {
  public static class PolyUtils {
    public static void AssertNotNull(System.Object objToAssert, string message) {
      if (objToAssert == null) {
        Throw(message);
      }
    }

    public static void AssertNotNullOrEmpty(string str, string message) {
      if (str == null || str == "") {
        Throw(message);
      }
    }

    public static void AssertTrue(bool cond, string message) {
      if (!cond) {
        Throw(message);
      }
    }

    public static void Throw(string message) {
      throw new Exception("ERROR: " + message);
    }
  }
}
