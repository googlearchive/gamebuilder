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

using System.IO;
using System.Text;
using UnityEngine;

namespace PolyToolkitInternal {
  /// <summary>
  /// Internal utilities (that is, for use by our internal code, not by users of the library).
  /// </summary>
  public static class PolyInternalUtils {
    public const string ATTRIBUTION_NOTICE =
        "IMPORTANT: Third-party assets are licensed through the Creative Commons license. When using any " +
        "third-party assets in your project, you are required to give proper attribution. For more information " +
        "refer to https://goo.gl/CNVF5Z. By continuing, you agree to use assets in " +
        "accordance to their license.";
    /// <summary>
    /// Creates a singleton GameObject in a way that's appropriate for
    /// the current runtime environment (Unity Editor or regular play mode).
    /// </summary>
    /// <param name="name">Name of the object.</param>
    public static GameObject CreateSingletonGameObject(string name) {
      GameObject obj;
      if (Application.isPlaying) {
        // Create a singleton object that we will use to add our MonoBehaviours.
        obj = new GameObject(name);
        // Preserve this object even when switching scenes.
        GameObject.DontDestroyOnLoad(obj);
        return obj;
      }
      // Running in the editor.
      // Make sure any old instance of the object is deleted first.
      GameObject old;
      while ((old = GameObject.Find(name))) {
        GameObject.DestroyImmediate(old);
      }
      obj = new GameObject(name);
      // We will make a temporary GameObject that only exists in RAM in the Editor. It should not get saved
      // to the user's scene, as it's just for our internal use.
      obj.hideFlags = HideFlags.HideAndDontSave;
      obj.tag = "EditorOnly";
      return obj;
    }

    /// <summary>
    /// Convert a file path to a MD5 hash string of the contents and its extension name. This is
    /// used for the purpose of sanitizing any possibly maliciously named format files.
    /// </summary>
    public static string ConvertFilePathToHash(string path) {
      StringBuilder sb = new StringBuilder();
      // Replace the file name with a safe name.
      sb.AppendFormat("{0}{1}", ConstructMd5StringHash(Path.GetFileName(path)),
        Path.GetExtension(path));

      return sb.ToString();
    }

    /// <summary>
    /// Construct a MD5 hash of a string, and return it in string format.
    /// </summary>
    public static string ConstructMd5StringHash(string str) {
      using (System.Security.Cryptography.MD5CryptoServiceProvider md5 =
        new System.Security.Cryptography.MD5CryptoServiceProvider()) {
        byte[] hashData = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
        StringBuilder sb = new StringBuilder();
        foreach (byte b in hashData) {
          sb.Append(b.ToString("X2"));
        }
        return sb.ToString();
      }
    }
  }
}
