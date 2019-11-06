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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PolyToolkit;
using System;

// Maybe generalize to a model library..for non-poly objects.
public class AssetCache : MonoBehaviour
{
  public delegate void ProcessAsset(Value entry);

  public GameObject imageRenderablePrefab;
  public Texture2D playerThumbnail;

  private Dictionary<string, ValueImpl> cacheByUri = new Dictionary<string, ValueImpl>();

  private Dictionary<string, List<ProcessAsset>> activeDownloadWaiters = new Dictionary<string, List<ProcessAsset>>();

  PolyImportOptions polyImportOptions = PolyImportOptions.Default();

  private Transform cacheRoot;

  private List<PolyAsset> downloadedPolyAssets = new List<PolyAsset>();
  private List<string> downloadedImageUrls = new List<string>();

  BuiltinPrefabLibrary builtinPrefabLibrary;
  WorkshopAssetSource steamWorkshop;

  public int GetNumActiveDownloads()
  {
    return activeDownloadWaiters.Count;
  }

  public void Get(string uri, ProcessAsset process)
  {
    Debug.Assert(uri != null);
    Debug.Assert(uri.Length > 0);
    Get(VoosAssetUtil.AssetFromUri(uri), process);
  }

  public void Get(VoosAsset asset, ProcessAsset process)
  {
    string uri = asset.GetUri();

    if (cacheByUri.ContainsKey(uri))
    {
      // We could call process immediately, but that makes this inconsistent.
      // We should maintain the invariant that process is ALWAYS called asynchronously.
      StartCoroutine(DeferredProcess(asset, process));
    }
    else
    {
      if (activeDownloadWaiters.ContainsKey(uri))
      {
        // Someone else already requested this, so just add ourselves to the wait list.
        activeDownloadWaiters[uri].Add(process);
      }
      else
      {
        // Start a new wait list.
        List<ProcessAsset> waitList = new List<ProcessAsset>();
        waitList.Add(process);
        activeDownloadWaiters[uri] = waitList;

        // Kick off the download.
        asset.Accept(new DownloadVisitor(this));
      }
    }
  }

  private void Awake()
  {
    Util.FindIfNotSet(this, ref builtinPrefabLibrary);
    Util.FindIfNotSet(this, ref steamWorkshop);

    cacheRoot = new GameObject("CachedAssets").transform;
    cacheRoot.parent = transform;
  }

  public class DownloadVisitor : VoosAssetVisitor
  {
    AssetCache cache;

    public DownloadVisitor(AssetCache cache)
    {
      this.cache = cache;
    }

#if USE_TRILIB
    static TriLib.AssetLoaderOptions triLibOpts = null;

    static TriLib.AssetLoaderOptions GetTriLibOpts()
    {
      if (triLibOpts == null)
      {
        triLibOpts = TriLib.AssetLoaderOptions.CreateInstance();
      }
      var opts = triLibOpts;
      opts.DontLoadCameras = true;
      opts.DontLoadLights = true;
      opts.UseLegacyAnimations = true;
      opts.GenerateMeshColliders = false; // We'll do this ourselves later.

      // Hmm support for alpha isn't great..the robot comes in clear if we do
      // this. Deal with it later, possibly manually.
      opts.DisableAlphaMaterials = true;

      return opts;
    }
#endif

    public void Visit(PolyVoosAsset asset)
    {
      string assetName = asset.assetId;
      string uri = asset.GetUri();

      PolyApi.GetAsset(assetName, maybeAsset =>
      {
        if (MaybeLogError(assetName, maybeAsset))
        {
          cache.CreateCacheEntry(uri, GameObject.CreatePrimitive(PrimitiveType.Cube), null);
          return;
        }

        PolyAsset polyAsset = maybeAsset.Value;

        // We need both the asset and the thumbnail to consider it "downloaded".
        // We'll download them in parallel, and whichever one finishes second
        // will call SetCacheEntry.
        GameObject assetObject = null;
        Texture2D thumbnail = null;

        PolyApi.FetchThumbnail(polyAsset, (_, maybeImported) =>
        {
          if (MaybeLogError(assetName, maybeImported))
          {
            return;
          }
          thumbnail = polyAsset.thumbnailTexture;

          // If we finished first, don't SetCacheEntry yet.
          if (assetObject != null)
          {
            // Ok, both resources are done. Report completion!
            // Set the cache (and flush the wait list)
            cache.CreateCacheEntry(uri, assetObject, thumbnail);
          }
        });

        System.Action<GameObject> onAssetImported = importedGameObject =>
        {
          cache.downloadedPolyAssets.Add(polyAsset);

          assetObject = importedGameObject;
          assetObject.name = assetName;
          PrepareImportedModel(assetObject, polyAsset.description);

          // If we finished first, don't SetCacheEntry yet.
          if (thumbnail != null)
          {
            // Ok, both resources are done. Report completion!
            // Set the cache (and flush the wait list)
            cache.CreateCacheEntry(uri, assetObject, thumbnail);
          }
        };

        var objFormat = polyAsset.GetFormatIfExists(PolyFormatType.OBJ);
        var fbx = polyAsset.GetFormatIfExists(PolyFormatType.FBX);

#if USE_TRILIB
        // Blocks models, like the pug, have both GLTFs and FBX's. But, TRILIB
        // doesn't seem to load Blocks FBX well, so don't do it. However, it's
        // not trivial to detect Blocks models. So our current heuristic is
        // going to simply be, only load the FBX if it has FBX but NOT OBJ. At
        // least as of 20190325, actual FBX uploads don't have OBJs. In the
        // future, we can even peek at the GLTF and see if it was generated by
        // Blocks.
        if (objFormat == null && fbx != null)
        {
          string tempDir = Util.CreateTempDirectory();

          Dictionary<string, string> url2path = new Dictionary<string, string>();
          foreach (var file in fbx.resources)
          {
            string localPath = System.IO.Path.Combine(tempDir, file.relativePath);
            url2path[file.url] = localPath;
          }

          // The root
          string rootPath = System.IO.Path.Combine(tempDir, fbx.root.relativePath);
          url2path[fbx.root.url] = rootPath;

          Util.DownloadFilesToDisk(url2path,
          () =>
          {
            // All done!
            using (var loader = new TriLib.AssetLoaderAsync())
            {
              TriLib.AssetLoaderOptions opts = GetTriLibOpts();
              loader.LoadFromFileWithTextures(rootPath, opts, null,
                obj =>
                {
                  System.IO.Directory.Delete(tempDir, true);
                  onAssetImported(obj);
                });
            }
          },
          errors =>
          {
            System.IO.Directory.Delete(tempDir, true);
            Debug.LogError($"Could not download Poly FBX for asset ID {assetName}. Errors: {string.Join("\n", errors)}");
            return;
          });
        }
        else
#endif
        {
          PolyApi.Import(polyAsset, cache.polyImportOptions, (_, maybeImported) =>
          {
            if (MaybeLogError(assetName, maybeImported))
            {
              return;
            }
            onAssetImported(maybeImported.Value.gameObject);
          });
        }
      });
    }

    private static void PrepareImportedModel(GameObject assetObject, string hashtags)
    {
      FitImportedModel(assetObject, hashtags);

      // Add colliders - important to do this after fitting.
      MeshFilter[] meshFilters = assetObject.GetComponentsInChildren<MeshFilter>();
      SkinnedMeshRenderer[] skinned = assetObject.GetComponentsInChildren<SkinnedMeshRenderer>();
      CombineInstance[] combine = new CombineInstance[meshFilters.Length + skinned.Length];
      int i = 0;
      while (i < meshFilters.Length)
      {
        combine[i].mesh = meshFilters[i].sharedMesh;
        combine[i].transform = assetObject.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
        i++;
      }
      foreach (var s in skinned)
      {
        combine[i].mesh = s.sharedMesh;
        combine[i].transform = assetObject.transform.worldToLocalMatrix * s.transform.localToWorldMatrix;
        i++;
      }
      MeshFilter mf = assetObject.AddComponent<MeshFilter>();
      mf.mesh = new Mesh();
      mf.mesh.CombineMeshes(combine);
      MeshCollider mc = assetObject.AddComponent<MeshCollider>();
      mc.convex = true;
      Destroy(mf);

      // Give the materials textures if they don't have them.
      MeshRenderer[] meshRenderers = assetObject.GetComponentsInChildren<MeshRenderer>();
      for (i = 0; i < meshRenderers.Length; i++)
      {
        MeshRenderer meshRenderer = meshRenderers[i];
        BakeBaseColorIntoTexture(meshRenderer.material);
      }

      if (hashtags.Contains(PolySearchManager.PointFilterHashtag))
      {
        Util.SetFilterModeOnAllTextures(assetObject, FilterMode.Point);
      }

      // Also enable GPU instancing!
      // foreach (var r in meshRenderers)
      // {
      //   foreach (var mat in r.sharedMaterials)
      //   {
      //     mat.enableInstancing = true;
      //   }
      // }
    }

    /** 
     * Color the texture with the material's base color and set the base color to white
     * so that we can apply further tints to the base color as needed.
     */
    static void BakeBaseColorIntoTexture(Material mat)
    {
      int baseColorFactorId = Shader.PropertyToID("_BaseColorFactor");
      if (!mat.HasProperty(baseColorFactorId))
      {
        return;
      }

      Color baseColor = mat.GetColor(baseColorFactorId);
      if (Color.white.Equals(baseColor))
      {
        return;
      }

      // If it doesn't have a texture, give it a default white one.
      Texture2D texture = (Texture2D)mat.GetTexture(Shader.PropertyToID("_BaseColorTex"));
      if (!texture)
      {
        texture = new Texture2D(1, 1, TextureFormat.RGBA32, true, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
      }

      Color[] pixels = texture.GetPixels();
      for (int i = 0; i < pixels.Length; i++)
      {
        pixels[i] = (pixels[i].linear * baseColor).gamma;
      }
      texture.SetPixels(pixels);
      texture.Apply();

      mat.SetTexture(Shader.PropertyToID("_BaseColorTex"), texture);
      mat.SetColor(baseColorFactorId, Color.white);
    }

    public static void FitImportedModel(GameObject importedObject, string hashtags)
    {
      if (hashtags.Contains(PolySearchManager.NoAutoFitHashtag))
      {
        return;
      }

      Bounds origBounds = Util.ComputeWorldRenderBounds(importedObject);

      float desiredMaxAbs =
        hashtags.Contains(PolySearchManager.TerrainBlockHashtag) ?
        TerrainManager.BLOCK_SIZE.MaxAbsComponent()
        : 1f;

      // Uniform scale to fit into unit cube
      float scale = desiredMaxAbs / origBounds.size.MaxAbsComponent();

      // Compute an offset to make the origin the bottom of the object.
      float yOffset = origBounds.size.y / 2f * scale;

      // Apply to all immediate children, their scales and positions. So if the
      // renderable's root transform is identity, it should still be unit-cubed
      // and origin-floored.
      bool hadChild = false;
      foreach (Transform child in importedObject.transform)
      {
        child.localScale *= scale;
        child.localPosition *= scale;
        child.localPosition -= origBounds.center * scale;
        child.localPosition += yOffset * Vector3.up;

        hadChild = true;
      }

      Debug.Assert(hadChild, "Imported model had no children - not expected.");
    }

    public void Visit(ImageVoosAsset asset)
    {
      cache.StartCoroutine(LoadAndCacheImage(asset));
    }

    public void Visit(BuiltinVoosAsset asset)
    {
      cache.StartCoroutine(LoadAndCacheBuiltin(asset));
    }

    public void Visit(LocalFbxAsset asset)
    {
#if USE_TRILIB
      // This loads it, but also sets up a file watcher that will update the
      // cached object if the local file changes.
      LoadLocalFbx(asset.absoluteFilePath, gameObject =>
      {
        var cached = cache.CreateCacheEntry(asset.GetUri(), gameObject, null);

        // Setup the watcher.
        var watcher = new System.IO.FileSystemWatcher();
        watcher.Path = Path.GetDirectoryName(asset.absoluteFilePath);
        watcher.Filter = Path.GetFileName(asset.absoluteFilePath);
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true; // Pretty important..otherwise, no events work. Heh.
        watcher.Changed += (_, e) =>
        {
          // If the file changes, we should load it again, and update the cached value.
          LoadLocalFbx(asset.absoluteFilePath, newGameObject =>
          {
            cache.UpdateCacheEntry(asset.GetUri(), newGameObject);
          });
        };
      });
#else
      Util.LogError($"This build does not support loading FBX assets (for local path {asset.absoluteFilePath})");
      cache.CreateCacheEntry(asset.GetUri(), GameObject.CreatePrimitive(PrimitiveType.Cube), null);
#endif
    }

    IEnumerator LoadAndCacheImage(ImageVoosAsset asset)
    {
      WWW request = new WWW(asset.url);
      yield return request;
      Debug.Assert(request.isDone);
      GameObject renderable = Instantiate(cache.imageRenderablePrefab);
      renderable.GetComponentInChildren<Renderer>().material.mainTexture = request.texture;

      string uri = asset.GetUri();
      cache.CreateCacheEntry(uri, renderable, request.texture);

      cache.downloadedImageUrls.Add(asset.url);
    }

    IEnumerator LoadAndCacheBuiltin(BuiltinVoosAsset asset)
    {
      ResourceRequest request = Resources.LoadAsync<GameObject>(asset.resourcePath);
      yield return request;
      Debug.Assert(request.isDone);
      Debug.Assert(request.asset != null, $"Failed to load builtin asset resource {asset.resourcePath}");
      GameObject loadedAsset = GameObject.Instantiate((GameObject)request.asset);

      string uri = asset.GetUri();
      Texture2D thumbnail;
      const string BUILTIN_ASSETS_PREFIX = "builtin:BuiltinAssets/";
      // We should find a less hacky way to do this.
      if (asset.GetUri() == VoosActor.AVATAR_EXPLORER_URI)
      {
        thumbnail = cache.playerThumbnail;
      }
      else if (uri.ToLowerInvariant().StartsWith(BUILTIN_ASSETS_PREFIX.ToLowerInvariant()))
      {
        // It's a built-in asset.
        string builtinId = uri.Substring(BUILTIN_ASSETS_PREFIX.Length);
        if (cache.builtinPrefabLibrary.PrefabExists(builtinId))
        {
          // Corresponds to an existing prefab, so use the prefab's thumbnail.
          thumbnail = cache.builtinPrefabLibrary.Get(builtinId).GetThumbnail();
        }
        else
        {
          // Some built-in assets don't correspond to prefabs. In that case don't use a thumbnail.
          // Warn anyway...
          Debug.LogWarning($"No thumbnail for built-in asset: {uri}. It does not correspond to an actor prefab.");
          thumbnail = null;
        }
      }
      else
      {
        Debug.LogError("No idea where to get thumbnail for " + uri);
        thumbnail = null;
      }
      cache.CreateCacheEntry(uri, loadedAsset, thumbnail);
    }

#if USE_TRILIB
    static void LoadLocalFbx(string path, System.Action<GameObject> process)
    {
      var loader = new TriLib.AssetLoaderAsync();
      TriLib.AssetLoaderOptions opts = GetTriLibOpts();
      loader.LoadFromFileWithTextures(path, opts, null, gameObject =>
      {
        PrepareImportedModel(gameObject, "");
        process(gameObject);
      });
    }
#endif

    public void Visit(SteamWorkshopAsset asset)
    {
#if USE_TRILIB
      cache.steamWorkshop.Get(asset.publishedId, maybePath =>
      {
        if (maybePath.IsEmpty())
        {
          Util.LogError($"Error trying to download workshop item {asset.publishedId}: {maybePath.GetErrorMessage()}");
          cache.CreateCacheEntry(asset.GetUri(), GameObject.CreatePrimitive(PrimitiveType.Cube), null);
          // TODO ok we should set the cache item to an error thing..
          return;
        }

        // Load FBX if it's there.
        foreach (string file in Directory.EnumerateFiles(maybePath.Value))
        {
          if (Path.GetExtension(file).ToLowerInvariant() == ".fbx")
          {
            Util.Log($"OK loading FBX downloaded from workshop at {file}");
            LoadLocalFbx(file, imported => cache.CreateCacheEntry(asset.GetUri(), imported, null));
            break;
          }
        }
      });
#else
      Util.LogError($"This build does not support loading FBX assets (for workshop ID {asset.publishedId})");
      cache.CreateCacheEntry(asset.GetUri(), GameObject.CreatePrimitive(PrimitiveType.Cube), null);
#endif
    }
  }

  IEnumerator DeferredProcess(VoosAsset asset, ProcessAsset process)
  {
    yield return null;
    process(cacheByUri[asset.GetUri()]);
  }

  // Returns true if it is an error and it was logged.
  static bool MaybeLogError<T>(string assetName, PolyStatusOr<T> statusOr)
  {
    if (statusOr.Ok)
    {
      return false;
    }
    else
    {
      Debug.LogError($"Error trying to get Poly asset '{assetName}': {statusOr.Status}");
      return true;
    }
  }

  // Returns true if it is an error and it was logged.
  static bool MaybeLogError(string assetName, PolyStatus status)
  {
    if (status.ok)
    {
      return false;
    }
    else
    {
      Debug.LogError($"Error trying to get Poly asset '{assetName}': {status.errorMessage}");
      return true;
    }
  }

  void PrepareCachedAsset(string uri, GameObject assetObject)
  {
    assetObject.name = uri;
    assetObject.SetActive(false);
    assetObject.transform.parent = cacheRoot;
  }

  Value CreateCacheEntry(string uri, GameObject assetObject, Texture2D thumbnail)
  {
    PrepareCachedAsset(uri, assetObject);
    ValueImpl entry = new ValueImpl(assetObject, thumbnail);

    if (cacheByUri.ContainsKey(uri))
    {
      Debug.LogError($"AssetCache is overriding uri {uri}. Is this a redundant download?");
    }
    cacheByUri[uri] = entry;

    // Flush the waitlist
    if (activeDownloadWaiters.ContainsKey(uri))
    {
      foreach (ProcessAsset process in activeDownloadWaiters[uri])
      {
        process(entry);
      }
      activeDownloadWaiters.Remove(uri);
    }

    return entry;
  }

  void UpdateCacheEntry(string uri, GameObject newAssetObject)
  {
    PrepareCachedAsset(uri, newAssetObject);
    ValueImpl cached = cacheByUri[uri];
    cached.UpdateAssetObject(newAssetObject);
  }

  public bool AnyAttributionsToShow()
  {
    return downloadedPolyAssets.Count + downloadedImageUrls.Count > 0;
  }

  public string GetAttributionsString()
  {
    string msg = "";
    if (downloadedPolyAssets.Count > 0)
    {
      msg += PolyApi.GenerateAttributions(true, downloadedPolyAssets);
    }

    if (downloadedImageUrls.Count > 0)
    {
      if (msg != "") msg += "\n";
      msg += GenerateImageAttributions(); ;
    }
    return msg;
  }

  private string GenerateImageAttributions()
  {
    return $"URLs of images used:\n" + String.Join("\n", downloadedImageUrls);
  }

  public interface Value
  {
    // NOTE: The asset gameobject given to you will be IN-ACTIVE. If we were to
    // activate it for you, that could incur a lot of unnecessary costs, so just
    // activate it yourself once it's set up. For example, if you're going to
    // parent it under something else, do that first, then activate it,
    // otherwise you incur extra mesh collider costs.
    GameObject GetAssetClone();

    // Triggered whenever the underlying asset has changed. This is mainly for
    // local file system assets, which the user may change for rapid iteration.
    event System.Action onAssetChanged;

    Texture2D GetThumbnail();
  }

  private class ValueImpl : Value
  {
    GameObject assetObject;
    Texture2D thumbnail;

    public event System.Action onAssetChanged;

    public ValueImpl(GameObject assetObject, Texture2D thumbnail)
    {
      this.assetObject = assetObject;
      this.thumbnail = thumbnail;
    }

    public void UpdateAssetObject(GameObject newAssetObject)
    {
      GameObject.Destroy(this.assetObject);
      this.assetObject = newAssetObject;
      this.onAssetChanged?.Invoke();
    }

    public GameObject GetAssetClone()
    {
      return GameObject.Instantiate(assetObject);
    }

    public Texture2D GetThumbnail()
    {
      return thumbnail;
    }
  }
}
