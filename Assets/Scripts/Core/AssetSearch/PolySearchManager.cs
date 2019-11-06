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
using PolyToolkit;
using System.Text.RegularExpressions;

public class PolySearchManager : MonoBehaviour
{
  const int MAX_ASSETS_RETURNED = 20;
  const float SCALE_COEFFICIENT = 3;
  AssetCache assetCache;

  public static string TerrainBlockHashtag = "#GBTerrainBlock";
  public static string NoAutoFitHashtag = "#GBNoAutoFit";
  public static string PointFilterHashtag = "#GBPointFilter";

  void Awake()
  {
    Util.FindIfNotSet(this, ref assetCache);
  }

  public void RequestRenderable(string uri, RenderableRequestEventHandler requestCallback)
  {
    assetCache.Get(uri, (entry) => requestCallback(entry.GetAssetClone()));
  }

  public void Search(string searchstring, OnActorableSearchResult resultCallback, System.Action<bool> onComplete)
  {
    // Special case: if the search string is a URL of a Poly asset, just return it.
    if (searchstring.Contains("poly.google.com/"))
    {
      SearchByPolyUrl(searchstring, resultCallback, onComplete);
      return;
    }

    PolyListAssetsRequest req = new PolyListAssetsRequest();
    req.curated = true;
    req.keywords = searchstring;
    req.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
    req.orderBy = PolyOrderBy.BEST;
    req.pageSize = MAX_ASSETS_RETURNED;

    PolyApi.ListAssets(req, (result) => PolySearchCallback(result, resultCallback, onComplete));
  }

  // Searches directly by Poly URL.
  private void SearchByPolyUrl(string polyUrl, OnActorableSearchResult resultCallback, System.Action<bool> onComplete)
  {
    string[] parts = polyUrl.Split('/');
    string assetId = parts[parts.Length - 1];
    PolyApi.GetAsset(assetId, result =>
    {
      PolyListAssetsResult assetsResult;
      List<PolyAsset> assetList = new List<PolyAsset>();
      if (result.Ok)
      {
        // Successfully got the asset. This is good.
        // Is it acceptably licensed?
        if (result.Value.license == PolyAssetLicense.CREATIVE_COMMONS_BY)
        {
          // Good license. We can use it.
          assetList.Add(result.Value);
          assetsResult = new PolyListAssetsResult(PolyStatus.Success(), 1, assetList);
        }
        else
        {
          // Not CC-By. Can't use it.
          Debug.LogError("This asset (" + assetId + ") is not licensed by CC-By. Try another asset.");
          assetsResult = new PolyListAssetsResult(PolyStatus.Error("Asset " + assetId + " is not licensed as CC-By."), 0, assetList);
        }
      }
      else
      {
        // Failed to get the asset. This is bad.
        assetsResult = new PolyListAssetsResult(PolyStatus.Error("Failed to get asset " + assetId), 0, assetList);
      }
      PolySearchCallback(
          new PolyStatusOr<PolyListAssetsResult>(assetsResult), resultCallback, onComplete);
    });
    return;
  }

  public void DefaultSearch(OnActorableSearchResult resultCallback)
  {
    PolyListAssetsRequest req = new PolyListAssetsRequest();
    req.curated = true;
    req.maxComplexity = PolyMaxComplexityFilter.MEDIUM;
    req.orderBy = PolyOrderBy.BEST;
    req.pageSize = MAX_ASSETS_RETURNED;

    PolyApi.ListAssets(req, (result) => PolySearchCallback(result, resultCallback, null));
  }

  void PolySearchCallback(PolyStatusOr<PolyListAssetsResult> result, OnActorableSearchResult resultCallback, System.Action<bool> onComplete)
  {
    if (!result.Ok)
    {
      Debug.Log(result.Status);
      return;
    }

    onComplete?.Invoke(result.Value.assets.Count != 0);

    foreach (PolyAsset asset in result.Value.assets)
    {
      PolyApi.FetchThumbnail(asset, (newasset, status) => PolyThumbnailCallback(newasset, status, resultCallback));
    }
  }

  void PolyThumbnailCallback(PolyAsset asset, PolyStatus status, OnActorableSearchResult resultCallback)
  {
    if (!status.ok)
    {
      Debug.Log("There is a problem with poly stuff");
      return;
    }

    ActorableSearchResult _newresult = new ActorableSearchResult();
    _newresult.forceConcave = true; // Do this for Poly models, for now.
    _newresult.preferredRotation = Quaternion.identity;

    // Don't do any actor-level scaling for any of these hash tags
    if (asset.description.Contains(TerrainBlockHashtag) || asset.description.Contains(NoAutoFitHashtag))
    {
      _newresult.preferredScaleFunction = _ => new Vector3(1f, 1f, 1f);
    }
    else
    {
      // Do some custom tuned actor scale.
      _newresult.preferredScaleFunction = CalculatePreferredScale;
    }
    _newresult.renderableReference.assetType = AssetType.Poly;
    _newresult.name = asset.displayName;
    _newresult.renderableReference.uri = new PolyVoosAsset(asset.name).GetUri();
    _newresult.thumbnail = asset.thumbnailTexture;

    resultCallback(_newresult);
  }

  Vector3 CalculatePreferredScale(GameObject renderable)
  {
    if (renderable == null)
    {
      Debug.Log("calculating scale on null renderable: returning vector3.one");
      return Vector3.one;
    }
    Vector3 boundSize = Util.ComputeWorldRenderBounds(renderable).size;
    return Vector3.one * (SCALE_COEFFICIENT / boundSize.magnitude);    //vector3.one.magnitude = ~1.75
  }

  public void RequestResultByID(string polyID, OnActorableSearchResult resultCallback)
  {
    PolyApi.GetAsset($"assets/{polyID}", (result) => PolyAssetCallback(result, resultCallback));
  }

  void PolyAssetCallback(PolyStatusOr<PolyAsset> result, OnActorableSearchResult resultCallback)
  {
    if (!result.Ok)
    {
      Debug.LogError("There is a problem with poly poly stuff" + result.Status.errorMessage);
      return;
    }
    PolyApi.FetchThumbnail(result.Value, (newasset, status) => PolyThumbnailCallback(newasset, status, resultCallback));
  }
}

public interface AssetSearchManager
{
  void RequestRenderable(ActorableSearchResult _requestedResult, RenderableRequestEventHandler requestCallback, int index);
  void Search(string searchstring, OnActorableSearchResult resultCallback);
  void CancelSearch();
}
