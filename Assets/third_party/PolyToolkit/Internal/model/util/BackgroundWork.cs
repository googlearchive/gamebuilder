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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PolyToolkitInternal.model.util {

  /// <summary>
  ///   Work to be done on a background thread.
  /// </summary>
  public interface BackgroundWork {

    /// <summary>
    ///   The work to be done on the background.
    /// </summary>
    void BackgroundWork();

    /// <summary>
    ///   Work to be done on the main thread after the background work is completed.
    /// </summary>
    void PostWork();
  }
}
