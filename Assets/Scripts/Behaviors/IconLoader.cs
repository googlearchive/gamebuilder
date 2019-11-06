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
using System.IO;
using CommandTerminal;

public class IconLoader : MonoBehaviour
{
  private const string ICONS_DIR = "third_party/material-design-icons";  // under Application.streamingAssetsPath
  private const string ICON_MANIFEST_FILE = "manifest.json";

  private Dictionary<string, List<string>> iconsPerCategory = new Dictionary<string, List<string>>();
  private Dictionary<string, string> iconPaths = new Dictionary<string, string>();

  void Awake()
  {
    string manifestPath = Path.Combine(Path.Combine(Application.streamingAssetsPath, ICONS_DIR), ICON_MANIFEST_FILE);
    Debug.Assert(File.Exists(manifestPath), "Icon manifest not found at path " + manifestPath);
    string[] entries = File.ReadAllText(manifestPath).Replace("\r", "").Split('\n');
    foreach (string entry in entries)
    {
      if (string.IsNullOrEmpty(entry)) continue;
      string[] parts = entry.Split('/');
      Debug.Assert(parts.Length == 2, "Icon manifest entry is invalid: " + entry);
      string category = parts[0];
      string iconName = parts[1];
      string iconFilePath = Path.Combine(Path.Combine(Application.streamingAssetsPath, ICONS_DIR), iconName + ".png");
      Debug.Assert(File.Exists(iconFilePath), "Icon mentioned in manifest does not exist: name=" + iconName + " path=" + iconFilePath);
      List<string> list;
      if (!iconsPerCategory.TryGetValue(category, out list))
      {
        list = new List<string>();
        iconsPerCategory[category] = list;
      }
      list.Add(iconName);
      iconPaths[iconName] = iconFilePath;
    }
    foreach (KeyValuePair<string, List<string>> pair in iconsPerCategory)
    {
      pair.Value.Sort();
    }
    Debug.Log("Icon manifest loaded (" + entries.Length + " icons).");
  }

  public void LoadIconTexture(string iconName, System.Action<string, Texture2D> onLoaded, System.Action<string> onError = null)
  {
    StartCoroutine(CoLoadIconImage(iconName, onLoaded, onError));
  }

  public void LoadIconSprite(string iconName, System.Action<string, Sprite> onLoaded, System.Action<string> onError = null)
  {
    LoadIconTexture(iconName, (name, tex) =>
    {
      onLoaded.Invoke(name, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero));
    }, onError);
  }

  public IEnumerable<string> EnumerateCategories()
  {
    List<string> keys = new List<string>(iconsPerCategory.Keys);
    keys.Sort();
    foreach (string key in keys)
    {
      yield return key;
    }
  }

  public IEnumerable<string> EnumerateIcons(string category)
  {
    List<string> iconNames;
    if (!iconsPerCategory.TryGetValue(category, out iconNames))
    {
      yield break;
    }
    foreach (string iconName in iconNames)
    {
      yield return iconName;
    }
  }

  private IEnumerator CoLoadIconImage(string iconName, System.Action<string, Texture2D> onLoaded, System.Action<string> onError)
  {
    string filePath;
    if (!iconPaths.TryGetValue(iconName, out filePath))
    {
      onError?.Invoke(iconName);
      yield break;
    }
    WWW www = new WWW("file://" + filePath);
    while (!www.isDone)
    {
      if (!string.IsNullOrEmpty(www.error))
      {
        Debug.LogError("Error loading icon " + iconName + ": " + www.error);
        onError?.Invoke(iconName);
        yield break;
      }
      yield return new WaitForSeconds(0.25f);
    }
    if (www.texture == null)
    {
      Debug.LogError("Error loading icon (texture is null): " + iconName);
      onError?.Invoke(iconName);
      yield break;
    }
    onLoaded.Invoke(iconName, www.texture);
  }

  [RegisterCommand(Help = "Debug icon picker")]
  static void CommandDebugIconPicker(CommandArg[] args)
  {
    IconPickerDialog.Launch(result => HeadlessTerminal.Log("Icon picker result: " + (result == null ? "null" : result)));
  }
}
