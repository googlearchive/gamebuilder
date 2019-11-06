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

namespace PolyToolkitInternal.model.export {

  public class FormatSaveData {
    public FormatDataFile root;
    public List<FormatDataFile> resources;
    public byte[] zippedFiles;
    public Int64 triangleCount;
  }

  public class FormatDataFile {
    public String fileName;
    public String mimeType;
    public byte[] bytes;
    public String tag;
    public byte[] multipartBytes;
  }

  /// <summary>
  ///   A struct containing the serialized bytes of a model.
  /// </summary>
  public struct SaveData {
    public string filenameBase;
    public byte[] objFile;
    public byte[] mtlFile;
    public FormatSaveData GLTFfiles;
    public byte[] objMtlZip;
    public byte[] polyFile;
    public byte[] polyZip;
    public byte[] thumbnailFile;
  }
}
