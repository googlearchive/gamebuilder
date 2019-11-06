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

public class ImageSystem : MonoBehaviour
{
  private SojoSystem sojoSystem;
  private ImageLoader imageLoader;
  private const string SOJO_ID_PREFIX = "IMAGE:";

  void Awake()
  {
    Util.FindIfNotSet(this, ref sojoSystem);
    Util.FindIfNotSet(this, ref imageLoader);

    // Prewarm cache.
    foreach (Sojo sojo in sojoSystem.GetAllSojosOfType(SojoType.Image))
    {
      PreloadImageForSojo(sojo);
    }
    // If any images get added, preload them too.
    sojoSystem.onSojoPut += sojo => PreloadImageForSojo(sojo);
  }

  // Note: for now this only supports Steam Workshop URLs in the form "sw:id" where <id> is
  // the steam workshop upload ID.
  public string ImportImageFromUrl(string url)
  {
    if (string.IsNullOrEmpty(url))
    {
      throw new System.Exception("The URL is invalid (empty).");
    }
    // For now, we only accept steam workshop URLs (sw:*)
    if (!url.StartsWith("sw:"))
    {
      throw new System.Exception("Only 'sw:' URLs are supported. URL was: " + url);
    }
    ulong steamWorkshopId;
    if (!ulong.TryParse(url.Substring("sw:".Length), out steamWorkshopId))
    {
      throw new System.Exception("Could not parse workshop ID from URL: " + url);
    }
    // Do we already have this image?
    foreach (Sojo sojo in sojoSystem.GetAllSojosOfType(SojoType.Image))
    {
      if (sojo.contentType == SojoType.Image && sojo.name == url)
      {
        return sojo.id;
      }
    }
    // We have to build a new SOJO.
    string id = SOJO_ID_PREFIX + url;
    sojoSystem.PutSojo(new Sojo(id, id, SojoType.Image, "{}"));
    return id;
  }

  public string GetImageUrl(string imageId)
  {
    if (!imageId.StartsWith(SOJO_ID_PREFIX))
    {
      return null;
    }
    Sojo sojo = sojoSystem.GetSojoById(imageId);
    if (sojo == null || sojo.contentType != SojoType.Image)
    {
      return null;
    }
    // The URL is everything after the prefix in the ID.
    return sojo.id.Substring(SOJO_ID_PREFIX.Length);
  }

  private void PreloadImageForSojo(Sojo sojo)
  {
    string url = GetImageUrl(sojo.id);
    if (url != null)
    {
      imageLoader.Request(url);
    }
  }
}
