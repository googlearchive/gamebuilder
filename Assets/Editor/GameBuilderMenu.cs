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
using UnityEditor;
using UnityEngine;
using System.IO;
using SD = System.Diagnostics;
using System.Linq;

public class GameBuilderMenu
{
  [MenuItem("Game Builder/Stop %#y")]
  public static void StopPlayMode()
  {
    EditorApplication.isPlaying = false;
  }

  [MenuItem("Game Builder/Build")]
  public static void Build()
  {
    BuildInternal(Path.GetFullPath("./build_output"), 0);
  }

  static void BuildInternal(string outDir, BuildOptions extraBuildOpts)
  {
    if (Directory.Exists(outDir))
    {
      bool delete = EditorUtility.DisplayDialog("Delete previous build?", $"The build output directory '{outDir}' exists. If that's a previous build, it must be deleted before proceeding (merely overwriting a previous build can lead to issues).", "Delete it", "Cancel build");
      if (!delete) return;
      Debug.Log($"Deleting {outDir}...");
      Directory.Delete(outDir, true);
    }

    var target = EditorUserBuildSettings.activeBuildTarget;

    string exeName = null;
    switch (target)
    {
      case BuildTarget.StandaloneWindows64:
        exeName = "Game Builder.exe";
        break;
      case BuildTarget.StandaloneOSX:
        exeName = "Game Builder.app";
        break;
      default:
        throw new System.Exception($"Unsupported build target: {target}");
    }

    string buildTargetPath = Path.Combine(outDir, exeName);
    var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, buildTargetPath, target, BuildOptions.ForceEnableAssertions | BuildOptions.StrictMode | extraBuildOpts);

    // Get the GIT commit version and write it to a text file
    SD.Process gitCall = new SD.Process();
    gitCall.StartInfo.FileName = "git";
    gitCall.StartInfo.Arguments = "rev-parse HEAD";
    gitCall.StartInfo.UseShellExecute = false;
    gitCall.StartInfo.RedirectStandardOutput = true;
    gitCall.StartInfo.RedirectStandardError = true;
    gitCall.Start();
    string gitCommit = gitCall.StandardOutput.ReadToEnd().Trim();
    string stdErr = gitCall.StandardError.ReadToEnd();
    gitCall.WaitForExit();
    if (gitCall.ExitCode != 0 || gitCommit.IsNullOrEmpty())
    {
      Debug.LogError($"Failed to run git. Code: {gitCall.ExitCode}. STDERR: {stdErr}");
    }
    else
    {
      File.WriteAllText(Path.Combine(outDir, "built-commit"), gitCommit);
    }

    // Create the critical files list
    using (StreamWriter writer = new StreamWriter(Path.Combine(outDir, "critical-files.txt")))
    {
      foreach (string filePath in
        Directory.GetFiles(outDir, "*.dll", SearchOption.AllDirectories).
        Concat(Directory.GetFiles(outDir, "*.exe", SearchOption.AllDirectories)))
      {
        // NOTE: It doesn't work if you pass outDir to MakeRelativePath..you end
        // up getting "content/blah.exe"
        string relPath = filePath.MakeRelativePath(buildTargetPath);
        if (relPath.IsNullOrEmpty())
        {
          continue;
        }
        // UnityEngine.Debug.Log(relPath);
        writer.WriteLine(relPath);
      }
    }

    bool show = EditorUtility.DisplayDialog("Build done!", $@"
{System.DateTime.Now}

Build is located at: {outDir}
".Trim(), "Show build location", "OK");

    if (show)
    {
      EditorUtility.RevealInFinder(outDir);
    }
  }

  // [MenuItem("Game Builder/Gen Simple-Data Terrain")]
  public static void GenSimpleDataTerrain()
  {
    using (var ms = new System.IO.MemoryStream())
    {
      using (var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress))
      using (var bw = new System.IO.BinaryWriter(gz))
      {
        const ushort version = 0;
        uint numBlocks = 5;

        bw.Write(version);

        bw.Write(numBlocks);
        for (int y = 0; y < numBlocks; y++)
        {
          bw.Write((short)0);
          bw.Write((short)y);
          bw.Write((short)0);

          bw.Write((byte)1);
          bw.Write((byte)0);
          bw.Write((ushort)20); // Grass
        }
      }

      // Must do this here - not inside any of the inner 'using' scopes above.
      byte[] zippedBytes = ms.ToArray();
      Util.Log($"simpleData string: {System.Convert.ToBase64String(zippedBytes)}");
    }
  }
}
