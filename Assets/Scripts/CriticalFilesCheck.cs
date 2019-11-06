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

using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriticalFilesCheck : MonoBehaviour
{
  [SerializeField] DynamicPopup popups;

  const string ManifestFile = "critical-files.txt";

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
  }

  void Start()
  {
    if (Application.isEditor)
    {
      return;
    }

    List<string> missingFiles = new List<string>();

    foreach (var line in File.ReadAllLines(ManifestFile))
    {
      string path = line.Trim();
      if (!File.Exists(path))
      {
        missingFiles.Add(path);
        Util.LogError($"Missing critical file: {path}");
      }
    }

    if (missingFiles.Count > 0)
    {

      popups.Show(
        "<size=80%>Sorry, we have detected that some Game Builder files are missing from the installation directory.\n"
        + "The game will now quit, and you'll be sent to instructions on how to restore these files.\n"
        + "Also, if you're running anti-virus, please make sure it is configured properly for Steam games.\n"
        + $"<size=50%>One missing file (of {missingFiles.Count}): {missingFiles[0]}",
          "Quit and open restore instructions", () =>
          {
            Application.OpenURL("https://support.steampowered.com/kb_article.php?ref=2037-QEUH-3335");
            Application.Quit();
          });
    }
  }
}
