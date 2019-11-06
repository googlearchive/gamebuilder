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

using PolyToolkit;
using System;
using System.Text;
using UnityEngine;

using PolyToolkitInternal;

/// <summary>
/// Test runtime import of gltf models.
/// </summary>
public class TestRuntimeImport : MonoBehaviour {

  private const string CLIENT_SECRET = "49385a554c3274635d6c47327d3a3c557d67793e79267852";
  private const string CLIENT_ID = "3539303a373737363831393b2178617c60227d7f7b7966252a74226e296f2d29174315175" +
    "15716131b1c5a4d1b034f5f40421c545b5a515b5d4c495e4e5e515134242c376a26292a";
  private const string API_KEY = "41487862577c4474616e3b5f4b39466e5161732a4b645d5b495752557276274673196e74496173";

  private const string kAssetId = "15ARMT6StKO";
  private const string kGltf2AssetId = "5eiqgJe4rMb";

  // Asset id specifically from Poly autopush, without which GetAsset wouldn't work.
  private const string kAssetIdFromPoly = "aqCWHdQNAiL";

  private PolyAuthConfig authConfig = new PolyAuthConfig(
    apiKey: Deobfuscate(API_KEY),
    clientId: Deobfuscate(CLIENT_ID),
    clientSecret: Deobfuscate(CLIENT_SECRET));

  public void TestImport(string id) {
    PolyApi.GetAsset(id, result => {
      if (result.Ok) {
        PolyApi.Import(result.Value, PolyImportOptions.Default());
      }
    });
  }

  // Use this for initialization
  void Start() {
    if (!PolyApi.IsInitialized) {
      PolyApi.Init(authConfig, new PolyCacheConfig());
    }

    if (!Authenticator.IsInitialized) {
      Authenticator.Initialize(authConfig);
    }

    TestImport(kAssetIdFromPoly);
  }
  
  public static string Deobfuscate(string input) {
    byte[] data = new byte[input.Length / 2];
    for (int i = 0; i < data.Length; i++) {
      byte b = Convert.ToByte(input.Substring(i * 2, 2), 16);
      data[i] = (byte)(b ^ i);
    }
    return Encoding.UTF8.GetString(data);
  }
}
