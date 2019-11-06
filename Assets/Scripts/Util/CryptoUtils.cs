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
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System;

public class CryptoUtils : MonoBehaviour
{
  public static string Encrypt(string text, string passphrase)
  {
    using (Aes aes = Aes.Create())
    {
      aes.GenerateIV();
      aes.Key = PassphraseToKey(passphrase);
      byte[] inputBytes = Encoding.UTF8.GetBytes(text);
      ICryptoTransform encryptor = aes.CreateEncryptor();
      byte[] outputBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
      return "GBV1 " + Convert.ToBase64String(aes.IV) + " " + Convert.ToBase64String(outputBytes);
    }
  }

  public static string Decrypt(string encrypted, string passphrase)
  {
    string[] parts = encrypted.Split(new char[] { ' ' }, 3);
    if (parts.Length != 3 || parts[0] != "GBV1")
    {
      throw new System.Exception("Invalid encrypted string.");
    }
    using (Aes aes = Aes.Create())
    {
      aes.IV = Convert.FromBase64String(parts[1]);
      aes.Key = PassphraseToKey(passphrase);
      byte[] inputBytes = Convert.FromBase64String(parts[2]);
      ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
      byte[] outputBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
      return Encoding.UTF8.GetString(outputBytes);
    }
  }

  private static byte[] PassphraseToKey(string passphrase)
  {
    using (AesManaged aes = new AesManaged())
    {
      aes.GenerateKey();
      byte[] key = new byte[aes.Key.Length];
      byte[] origBytes = Encoding.UTF8.GetBytes(passphrase);
      System.Array.Copy(origBytes, key, Mathf.Min(origBytes.Length, key.Length));
      return key;
    }
  }
}
