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

using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class CryptoUtilTest
{
  [Test]
  public void TestCrypto()
  {
    string orig = "Lorem ipsum dolor sit amet.";
    string passphrase = "I am a super secure passphrase";
    Assert.AreEqual(orig, CryptoUtils.Decrypt(CryptoUtils.Encrypt(orig, passphrase), passphrase));
  }
}