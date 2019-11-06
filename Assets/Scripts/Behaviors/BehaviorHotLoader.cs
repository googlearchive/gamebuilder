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
using Behaviors;
using System.IO;

// This is only used if the user activates the (advanced) javascript hotloading feature.
// The responsibility of this class is watching a given directory in the file system
// and dynamically loading all the files in that directory as cards into BehaviorSystem.
public class BehaviorHotLoader : MonoBehaviour
{
  private const float CHECK_INTERVAL = 1;
  private const string NEW_FILE_TEMPLATE =
      "export const PROPS = [\n" +
      "];\n\n" +
      "// Your functions here.";
  private const string PLACEHOLDER_ID = "__PLACEHOLDER_ID__";
  private static readonly string NEW_FILE_METADATA_TEMPLATE =
    ("{\n" +
    "  'cardSystemCardData' : {\n" +
    "    'userProvidedId': '" + PLACEHOLDER_ID + "',\n" +
    "    'isCard': true,\n" +
    "    'title': 'Your Card Title',\n" +
    "    'description': 'Your Card Description.',\n" +
    "    'categories' : ['Action']\n" +
    "  }\n" +
    "}").Replace('\'', '"');

  private BehaviorSystem behaviorSystem;
  private bool running;
  private string hotDirectory;
  private long lastWatchSignature;
  private float lastCheckTime;
  private List<string> loadedCardGuids = new List<string>();

  void Awake()
  {
    Util.FindIfNotSet(this, ref behaviorSystem);
  }

  public void StartRunning(string hotDirectory)
  {
    this.hotDirectory = hotDirectory;
    running = true;
    lastCheckTime = 0;
    lastWatchSignature = -1;
  }

  public void StopRunning()
  {
    running = false;
  }

  public bool IsRunning()
  {
    return running;
  }

  public string GetHotDirectory()
  {
    return hotDirectory;
  }

  void Update()
  {
    if (!running) return;
    if (Time.unscaledTime > lastCheckTime + CHECK_INTERVAL)
    {
      CheckForModifications();
      lastCheckTime = Time.unscaledTime;
    }
  }

  private void CheckForModifications()
  {
    long currentWatchSignature = CalcWatchSignature(hotDirectory);
    if (currentWatchSignature != lastWatchSignature)
    {
      lastWatchSignature = currentWatchSignature;
      try
      {
        ReloadFiles();
      }
      catch (IOException ex)
      {
        Complain($"Failed to reload files from {hotDirectory} because of an error:\n{ex.StackTrace}");
      }
    }
  }

  private void ReloadFiles()
  {
    Debug.Log("jshotload reloading files from " + hotDirectory);
    loadedCardGuids.Clear();
    foreach (string path in Directory.GetFiles(hotDirectory, "*.*", SearchOption.AllDirectories))
    {
      if (path.ToLowerInvariant().EndsWith(".js.txt") || path.ToLowerInvariant().EndsWith(".js"))
      {
        ReloadCard(path);
      }
    }
    CommandTerminal.HeadlessTerminal.Log("jshotload reloaded " + loadedCardGuids.Count + " cards.");
  }

  private void ReloadCard(string path)
  {
    string metaPath = path + ".metaJson";
    if (!File.Exists(metaPath))
    {
      Complain($"Can't load card from {path} because there is no corresponding .metaJson file.");
      return;
    }
    string jsContents = File.ReadAllText(path);
    string metaContents = File.ReadAllText(metaPath);
    BehaviorCards.CardMetadata cardMeta = JsonUtility.FromJson<BehaviorCards.CardMetadata>(metaContents);
    string providedId = cardMeta.cardSystemCardData.userProvidedId;
    if (!string.IsNullOrEmpty(cardMeta.cardSystemCardData.imageResourcePath) &&
      !cardMeta.cardSystemCardData.imageResourcePath.StartsWith("icon:") &&
      !cardMeta.cardSystemCardData.imageResourcePath.StartsWith("BuiltinAssets")
      )
    {
      Complain($"Error loading {path}: metaJson file has invalid format for imageResourcePath field.");
      return;
    }
    if (string.IsNullOrEmpty(providedId))
    {
      Complain($"Error loading {path}: metaJson file is missing userProvidedId field.");
      return;
    }
    providedId = providedId.ToLowerInvariant();
    if (!BehaviorSystem.IsGuid(providedId))
    {
      Complain($"Error loading {path}: userProvidedId must be a valid GUID (32 hex digits).");
      return;
    }
    Debug.Log($"Hot-loading card from {path} -> {providedId}");
    Behavior behavior = new Behavior
    {
      label = "Hotloaded",
      javascript = jsContents,
      metadataJson = metaContents
    };
    loadedCardGuids.Add(providedId);
    behaviorSystem.PutBehavior(providedId, behavior);
  }

  private static void Complain(string errorMessage)
  {
    CommandTerminal.HeadlessTerminal.Log(CommandTerminal.TerminalLogType.Error, "jshotload: {0}", errorMessage);
    Debug.LogError("jshotload error: " + errorMessage);
  }

  // Given a path on disk, calculates a funny number based on the files in that path.
  // The funny number has no meaning but has these properties:
  //   1. It changes when a file is added/deleted/modified.
  //   2. It doesn't change unless a file is added/deleted/modified.
  // The current implementation is based on file system mtimes but it could be based
  // on MD5 sums or whatever else.
  private static long CalcWatchSignature(string basePath)
  {
    List<string> paths = new List<string>(Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories));
    paths.Sort();
    long signature = 0;
    foreach (string path in paths)
    {
      string pathLower = path.ToLowerInvariant();
      if (!pathLower.EndsWith(".js") && !pathLower.EndsWith(".js.txt") && !pathLower.EndsWith(".metaJson"))
      {
        // Not a relevant file to us. Pretend it doesn't exist.
        continue;
      }
      // Overflow is ok.
      signature += File.GetLastWriteTime(path).ToBinary();
    }
    return signature;
  }

  [CommandTerminal.RegisterCommand(Help = "Sets up Javascript hotload from disk (advanced).")]
  public static void CommandJsHotLoad(CommandTerminal.CommandArg[] args)
  {
    BehaviorHotLoader hotLoader = GameObject.FindObjectOfType<BehaviorHotLoader>();
    if (args.Length < 1)
    {
      // Print status/help.
      if (hotLoader.IsRunning())
      {
        CommandTerminal.HeadlessTerminal.Log(
          "jshotload IS ACTIVE, directory: " + hotLoader.GetHotDirectory() + "\n\n" +
          "To stop, use 'jshotload stop'.\nTo generate a file use 'jshotload gen someFileName'");
      }
      else
      {
        CommandTerminal.HeadlessTerminal.Log(
          "When active, jshotload will continuously load JS and Metadata files\n" +
          "from a given filesystem directory and import them as cards.\n\n" +
          "jshotload start C:\\Some\\Path\\Here     - activate JS hotloading\n" +
          "jshotload gen someFileName            - generate a new file (convenience)\n" +
          "jshotload stop                        - deactivate JS hotloading\n\n" +
          "WARNING: any edits you make to those cards in Game Builder will be\n" +
          "overwritten when you change the originals in the file system! So\n" +
          "please ONLY do edit in the filesystem if you use this feature.");
      }
      return;
    }
    string verb = args[0].String;
    switch (verb)
    {
      case "start":
        {
          string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
          if (args.Length < 2)
          {
            CommandTerminal.HeadlessTerminal.Log("'jshotload on' requires a path argument. Type just 'jshotload' for help.");
            return;
          }
          string path = args[1].String;
          if (!Path.IsPathRooted(path) && homeDir != null)
          {
            // Try to interpret as relative to user profile.
            path = Path.Combine(homeDir, path);
          }

          if (Directory.Exists(path))
          {
            hotLoader.StartRunning(path);
            CommandTerminal.HeadlessTerminal.Log("jshotload starting with path " + path);
          }
          else
          {
            CommandTerminal.HeadlessTerminal.Log("Path does not exist: " + path);
          }
        }
        break;
      case "gen":
        if (args.Length < 2)
        {
          CommandTerminal.HeadlessTerminal.Log("'jshotload gen' requires a file name. Type just 'jshotload' for help.");
          return;
        }
        string fileName = args[1].String;
        hotLoader.GenerateNewFile(fileName);
        break;
      case "stop":
        {
          if (hotLoader.IsRunning())
          {
            CommandTerminal.HeadlessTerminal.Log("jshotload stopping.");
            hotLoader.StopRunning();
          }
          else
          {
            CommandTerminal.HeadlessTerminal.Log("jshotload is not running.");
          }
        }
        break;
      default:
        {
          CommandTerminal.HeadlessTerminal.Log("Invalid argument. Type 'jshotload' for help.");
        }
        break;
    }
  }

  private void GenerateNewFile(string fileName)
  {
    if (!running)
    {
      Complain("jshotload is not running.");
      return;
    }
    try
    {
      if (!fileName.ToLowerInvariant().EndsWith(".js") && !fileName.ToLowerInvariant().EndsWith(".js.txt"))
      {
        fileName += ".js";
      }
      string filePath = Path.Combine(hotDirectory, fileName);
      string metaFilePath = filePath + ".metaJson";
      File.WriteAllText(filePath, NEW_FILE_TEMPLATE);
      string guid = behaviorSystem.GenerateUniqueId();
      File.WriteAllText(filePath + ".metaJson", NEW_FILE_METADATA_TEMPLATE.Replace(PLACEHOLDER_ID, guid));
      CommandTerminal.HeadlessTerminal.Log($"Created {filePath} and {metaFilePath}.");
    }
    catch (IOException ex)
    {
      Complain("Failed to generate new file: " + ex.StackTrace);
    }
  }
}
