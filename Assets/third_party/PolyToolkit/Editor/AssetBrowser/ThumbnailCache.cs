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
using UnityEngine;

namespace PolyToolkitEditor {
/// <summary>
/// Cache of thumbnail textures.
/// </summary>
public class ThumbnailCache {
  private const int MAX_ENTRIES = 1024;

  /// <summary>
  /// Maps asset ID to thumbnail texture.
  /// </summary>
  private Dictionary<string, CacheEntry> entries = new Dictionary<string, CacheEntry>();

  public ThumbnailCache() { }

  public bool TryGet(string assetId, out Texture2D texture) {
    CacheEntry entry;
    if (entries.TryGetValue(assetId, out entry)) {
      entry.lastUsedTime = DateTime.UtcNow.Ticks;
      texture = entry.texture;
      return true;
    }
    texture = null;
    return false;
  }

  public bool Contains(string assetId) {
    return entries.ContainsKey(assetId);
  }

  public void Put(string assetId, Texture2D texture) {
    CacheEntry entry;
    if (entries.TryGetValue(assetId, out entry)) {
      GameObject.DestroyImmediate(entry.texture);
      entry.texture = texture;
      entry.lastUsedTime = DateTime.UtcNow.Ticks;
    } else {
      entries[assetId] = new CacheEntry(assetId, texture);
    }
  }

  public void Clear() {
    foreach (CacheEntry entry in entries.Values) {
      Evict(entry, removeFromDictionary: false);
    }
    entries.Clear();
  }

  public void TrimCacheWithExceptions(HashSet<string> assetsInUse) {
    while (entries.Count > MAX_ENTRIES) {
      CacheEntry oldestNotInUse = GetOldestEntryNotInUse(assetsInUse);
      if (oldestNotInUse == null) {
        // All remaining assets are still in use, so there's nothing more
        // to throw away.
        break;
      }
      Evict(oldestNotInUse);
    }
  }
  
  private CacheEntry GetOldestEntryNotInUse(HashSet<string> assetsInUse) {
    CacheEntry oldest = null;
    foreach (CacheEntry entry in entries.Values) {
      oldest = ((oldest == null || entry.lastUsedTime < oldest.lastUsedTime)
        && !assetsInUse.Contains(entry.assetId)) ? entry : oldest;
    }
    return oldest;
  }

  private void Evict(CacheEntry entry, bool removeFromDictionary = true) {
    GameObject.DestroyImmediate(entry.texture);
    if (removeFromDictionary) {
      entries.Remove(entry.assetId);
    }
  }

  private class CacheEntry {
    public string assetId;
    /// <summary>
    /// Last time this entry was used (given in ticks).
    /// </summary>
    public long lastUsedTime;
    /// <summary>
    /// The cached texture.
    /// </summary>
    public Texture2D texture;

    public CacheEntry(string assetId, Texture2D texture) {
      this.assetId = assetId;
      this.texture = texture;
      this.lastUsedTime = DateTime.UtcNow.Ticks;
    }
  }
}

}