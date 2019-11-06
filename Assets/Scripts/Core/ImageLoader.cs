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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

// Image loader service that can be asked to load image URLs asynchronously
// into Image components.
public class ImageLoader : MonoBehaviour
{
  WorkshopAssetSource workshopAssetSource;

  // Requests in progress (actively downloading). Keyed by URL.
  Dictionary<string, PendingRequest> pendingRequests = new Dictionary<string, PendingRequest>();

  // Textures that were already loaded. Note that this is intentionally NOT an LRU cache, to prevent
  // scripts from being able to control when URLs are fetched (which they could
  // in theory do by spamming the cache to cause predictable evictions).
  private Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();

  private void Awake()
  {
    Util.FindIfNotSet(this, ref workshopAssetSource);
  }

  // Request that a URL be loaded into the given target image.
  // If targetImage == null, it just requests that the image be loaded for later.
  // If callback != null, the callback will be called when done.
  public void Request(string url, Image targetImage = null, System.Action<string, bool> callback = null)
  {
    Texture2D tex;
    PendingRequest request;
    if (loadedTextures.TryGetValue(url, out tex))
    {
      // We already have that image. Yay.
      if (targetImage != null)
      {
        targetImage.sprite = TextureToSprite(tex);
      }
      callback?.Invoke(url, true);
      return;
    }
    else if (pendingRequests.TryGetValue(url, out request))
    {
      // Request is already in progress, so just add this image to it.
      if (targetImage != null)
      {
        request.targetImages.Add(targetImage);
        request.callbacks.Add(callback);
      }
      return;
    }
    // Make a new request.
    MakeRequest(url, targetImage, callback);
  }

  // Cancels a pending request to load an image into the target image.
  public void CancelRequest(Image targetImage)
  {
    foreach (KeyValuePair<string, PendingRequest> request in pendingRequests)
    {
      request.Value.targetImages.Remove(targetImage);
      // Note that we don't stop the request; we still want to cache the
      // result when it completes.
    }
  }

  public Texture2D GetOrRequest(string url)
  {
    Texture2D tex;
    if (loadedTextures.TryGetValue(url, out tex))
    {
      return tex;
    }
    // It's ok to request the same URL more than once (requests are unique by URL).
    Request(url);
    return null;
  }

  private void MakeRequest(string url, Image target, System.Action<string, bool> callback)
  {
    // Only steam workshop URLs are supported ("sw:")
    Debug.Assert(url.StartsWith("sw:"), "Invalid Steam Workshop URL: " + url);
    ulong workshopId;
    if (!ulong.TryParse(url.Substring("sw:".Length), out workshopId))
    {
      throw new System.Exception("Invalid workshop ID in url: " + url);
    }
    // This line should not be printed often. If it is, something is wrong with caching.
    Debug.Log("ImageLoader downloading image: " + url);
    pendingRequests[url] = new PendingRequest(url, target, callback);
    workshopAssetSource.Get(workshopId, result => OnWorkshopFetchComplete(url, result));
  }

  void OnWorkshopFetchComplete(string url, Util.Maybe<string> workshopResult)
  {
    PendingRequest request;
    if (!pendingRequests.TryGetValue(url, out request))
    {
      // Definitely weird, but not fatal.
      Debug.LogError("Fetched workshop image has no corresponding request: " + url);
      return;
    }
    pendingRequests.Remove(url);
    if (workshopResult.IsEmpty())
    {
      // Failed.
      Debug.LogError("Failed to fetch image from Steam Workshop URL " + url +
        ". Error: " + workshopResult.GetErrorMessage());
      SetColorsAndTexture(request.targetImages, Color.magenta);
      CallCallbacks(request.callbacks, url, false);
      return;
    }
    string directory = workshopResult.Get();
    string pngFilePath = Path.Combine(directory, "image.png");
    string jpgFilePath = Path.Combine(directory, "image.jpg");
    string filePath = File.Exists(pngFilePath) ? pngFilePath : File.Exists(jpgFilePath) ? jpgFilePath : null;
    if (filePath == null)
    {
      Debug.LogError("Steam workshop image has no image.png or image.jpg in it: " + url);
      SetColorsAndTexture(request.targetImages, Color.yellow);
      CallCallbacks(request.callbacks, url, false);
      return;
    }
    Texture2D tex = Util.ReadPngToTexture(filePath);
    if (tex == null)
    {
      Debug.LogError("Failed to convert steam workshop image to texture: " + url);
      SetColorsAndTexture(request.targetImages, Color.red);
      CallCallbacks(request.callbacks, url, false);
      return;
    }

    loadedTextures[url] = tex;
    SetColorsAndTexture(request.targetImages, Color.white, tex);
    CallCallbacks(request.callbacks, url, true);
  }

  private static void SetColorsAndTexture(IEnumerable<Image> images, Color color, Texture2D tex = null)
  {
    foreach (Image image in images)
    {
      if (image != null)
      {
        image.sprite = TextureToSprite(tex);
        image.color = color;
      }
    }
  }

  private static void CallCallbacks(IEnumerable<System.Action<string, bool>> callbacks, string url, bool success)
  {
    foreach (System.Action<string, bool> callback in callbacks)
    {
      callback.Invoke(url, success);
    }
  }

  private static Sprite TextureToSprite(Texture2D tex)
  {
    return tex != null ? Sprite.Create(
        tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f) : null;
  }

  private class PendingRequest
  {
    public string url;
    public HashSet<UnityEngine.UI.Image> targetImages = new HashSet<UnityEngine.UI.Image>();
    public List<System.Action<string, bool>> callbacks = new List<System.Action<string, bool>>();
    public PendingRequest(string url, UnityEngine.UI.Image targetImage, System.Action<string, bool> callback)
    {
      this.url = url;
      if (targetImage != null)
      {
        this.targetImages.Add(targetImage);
      }
      if (callback != null)
      {
        this.callbacks.Add(callback);
      }
    }
  }
}
