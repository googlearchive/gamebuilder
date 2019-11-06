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
using System.IO;
using UnityEngine;

namespace GameBuilder
{
  // Represents a bundle on disk, that may or may not exist. Read-only.
  public class GameBundle
  {
    [System.Serializable]
    public class Metadata
    {
      public string name;
      public string description;
    }

    string directory;

    Metadata metadata = null;

    Texture2D thumbnail = null;

    public Metadata GetMetadata()
    {
      if (metadata == null)
      {
        if (File.Exists(GetMetadataPath()))
        {
          metadata = Util.ReadFromJson<GameBundle.Metadata>(GetMetadataPath());
        }
        else
        {
          metadata = new Metadata();
          metadata.name = Path.GetFileName(directory);
          metadata.description = "";
        }
      }

      return metadata;
    }

    public Texture2D GetThumbnail()
    {
      if (thumbnail == null)
      {
        thumbnail = Util.ReadPngToTexture(GetThumbnailPath());
      }

      return thumbnail;
    }

    public GameBundle(string directory)
    {
      this.directory = directory;
    }

    public string GetThumbnailPath()
    {
      return Path.Combine(directory, "thumbnail.png");
    }

    public string GetVoosPath()
    {
      return Path.Combine(directory, "scene.voos");
    }

    public string GetMetadataPath()
    {
      return Path.Combine(directory, "metadata.json");
    }

    public string GetAssetsPath()
    {
      return Path.Combine(directory, "assets");
    }
  }

  public class GameBundleLibrary : MonoBehaviour
  {
    public delegate void SaveDoneCallback(string bundleId);

    public struct Entry
    {
      public string id;
      public GameBundle bundle;

      public bool IsAutosave()
      {
        return AutoSaveController.IsAutosave(id);
      }
    }

    [SerializeField] Util.AbstractPath location;

    WorkshopAssetSource workshop;

    public string GetLibraryAbsolutePath()
    {
      return location.GetAbsolute();
    }

    void Awake()
    {
      Util.UpgradeUserDataDir();

      Directory.CreateDirectory(location.GetAbsolute());

      Util.FindIfNotSet(this, ref workshop);
    }

    public string GetBundleDirectory(string bundleId)
    {
      if (bundleId.IsNullOrEmpty())
      {
        throw new System.Exception("No bundleId given");
      }
      return Path.Combine(location.GetAbsolute(), bundleId);
    }

    // Returns an ID for the bundle. The current VOOS state will be saved as the scene.
    public string SaveNew(SaveLoadController saveLoad, GameBundle.Metadata metadata, Texture2D thumbnail, System.Action onSaveComplete)
    {
      string id = GenerateBundleId();
      SaveMaybeOverwrite(saveLoad, id, metadata, thumbnail, onSaveComplete);
      return id;
    }

    public void SaveMaybeOverwrite(SaveLoadController saveLoad, string id, GameBundle.Metadata metadata, Texture2D thumbnail, System.Action onSaveComplete)
    {
      string dir = GetBundleDirectory(id);
      Util.SetNormalFileAttributes(dir);
      Directory.CreateDirectory(dir);

      GameBundle bundle = new GameBundle(dir);
      if (thumbnail != null)
      {
        Util.SetNormalFileAttributes(bundle.GetThumbnailPath());
        Util.SaveTextureToPng(thumbnail, bundle.GetThumbnailPath());
      }

      Util.SetNormalFileAttributes(bundle.GetMetadataPath());
      File.WriteAllText(bundle.GetMetadataPath(), JsonUtility.ToJson(metadata));

      Util.SetNormalFileAttributes(bundle.GetVoosPath());
      saveLoad.RequestSave(bundle.GetVoosPath(), onSaveComplete);

#if !USE_STEAMWORKS
      workshop.Save(bundle.GetAssetsPath());
#endif
    }

    public GameBundle GetBundle(string bundleId)
    {
      if (bundleId.IsNullOrEmpty())
      {
        return null;
      }
      return new GameBundle(GetBundleDirectory(bundleId));
    }

    public Entry GetBundleEntry(string bundleId)
    {
      return new Entry
      {
        id = bundleId,
        bundle = GetBundle(bundleId)
      };
    }

    public IEnumerable<Entry> Enumerate()
    {
      foreach (string absolutePath in Directory.EnumerateDirectories(location.GetAbsolute()))
      {
        string id = Path.GetFileName(absolutePath);
        GameBundle bundle = new GameBundle(GetBundleDirectory(id));
        // Assume that if a scene is there, that's all we really care about.
        // That's the most important data, after all.
        if (!File.Exists(bundle.GetVoosPath()))
        {
          Debug.LogWarning($"A directory '{id}' inside '{location.GetAbsolute()}' seems like an invalid bundle directory. File is missing: {bundle.GetVoosPath()}. Skipping it.");
          continue;
        }

        yield return new Entry { id = id, bundle = bundle };
      }
    }

    public bool BundleExists(string bundleId)
    {
      if (bundleId.IsNullOrEmpty())
      {
        throw new System.Exception("No bundleId given");
      }
      return Directory.Exists(GetBundleDirectory(bundleId));
    }

    public void DeletePermanently(string bundleId)
    {
      if (bundleId.IsNullOrEmpty())
      {
        throw new System.Exception("No bundleId given");
      }

      string dir = GetBundleDirectory(bundleId);

      // Be extra careful...
      if (!dir.Contains(bundleId))
      {
        throw new System.Exception($"Suspicious bundle directory - not going to delete anything: {dir}");
      }

      if (!Directory.Exists(dir))
      {
        return;
      }

      // Protect against random read-only...
      Util.SetNormalFileAttributes(dir);
      foreach (string file in Directory.GetFiles(dir))
      {
        Util.SetNormalFileAttributes(file);
      }

      Directory.Delete(dir, true);
    }

    public void CopyBundle(string srcId, string destId)
    {
      string srcDir = GetBundleDirectory(srcId);
      string destDir = GetBundleDirectory(destId);

      if (!Directory.Exists(srcDir))
      {
        throw new System.Exception($"Source directory {srcDir} DNE");
      }

      if (!Directory.Exists(destDir))
      {
        Directory.CreateDirectory(destDir);
      }

      foreach (string srcFilePath in Directory.EnumerateFiles(srcDir))
      {
        string filename = Path.GetFileName(srcFilePath);
        string destFilePath = Path.Combine(destDir, filename);
        File.Copy(srcFilePath, destFilePath);
      }
    }

    static string GenerateBundleId()
    {
      return System.Guid.NewGuid().ToString("N");
    }
  }
}