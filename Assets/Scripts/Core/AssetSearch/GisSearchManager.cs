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

public class GisSearchManager : MonoBehaviour
{
  const int MAX_ASSETS_RETURNED = 10;
  public const string APIkey = "PUT YOUR KEY HERE";
  const string searchID = "PUT YOUR ID HERE";
  const string usageRights = "cc_publicdomain|cc_attribute";
  // OnActorableSearchResult searchCallback;
  AssetCache assetCache;

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
    if (searchstring.Length == 0) return;
    StartCoroutine(SearchRoutine(searchstring, resultCallback, onComplete));
  }

  IEnumerator SearchRoutine(string searchstring, OnActorableSearchResult resultCallback, System.Action<bool> onComplete)
  {
    string query = $"https://www.googleapis.com/customsearch/v1?key={APIkey}&cx={searchID}&q={searchstring}&searchType=image&rights={usageRights}&safe=active";
    WWW temp = new WWW(query);
    yield return temp;

    if (temp == null || !temp.error.IsNullOrEmpty() || temp.text == null)
    {
      yield break;
    }

    try
    {
      GISRawResult result = JsonUtility.FromJson<GISRawResult>(temp.text);
      if (result == null || result.items == null || result.items.Length == 0)
      {
        yield break;
      }

      int resultsLeft = MAX_ASSETS_RETURNED;
      int index = 0;

      bool anyfound = false;
      while (resultsLeft > 0)
      {
        string mime = result.items[index].mime;
        if (mime == "image/jpeg" || mime == "image/png")
        {
          anyfound = true;
          ImageResult imageResult = new ImageResult(result.items[index].link, result.items[index].image.thumbnailLink, result.items[index].title);
          StartCoroutine(LoadThumbnail(imageResult, resultCallback));
          resultsLeft--;
        }

        index++;
        if (index >= result.items.Length) break;
      }

      onComplete?.Invoke(anyfound);
    }
    catch (System.ArgumentException)
    {
      // Probably bad result data - just ignore.
    }
  }

  IEnumerator LoadThumbnail(ImageResult imageResult, OnActorableSearchResult resultCallback)
  {
    if (imageResult.thumbnailUrl.IsNullOrEmpty())
    {
      yield break;
    }

    WWW temp = new WWW(imageResult.thumbnailUrl);
    yield return temp;

    if (temp == null || !temp.error.IsNullOrEmpty())
    {
      yield break;
    }

    ActorableSearchResult _newresult = new ActorableSearchResult();
    _newresult.preferredRotation = Quaternion.identity;
    _newresult.preferredScaleFunction = (go) => new Vector3(1f, 1f, 1f);
    _newresult.renderableReference.assetType = AssetType.Image;
    _newresult.name = imageResult.name;
    _newresult.renderableReference.uri = new ImageVoosAsset(imageResult.url).GetUri();
    _newresult.thumbnail = temp.texture;
    resultCallback(_newresult);
  }
}

public struct ImageResult
{
  public string name;
  public string url;
  public string thumbnailUrl;

  public ImageResult(string _url, string _thumbnailurl, string _name)
  {
    thumbnailUrl = _thumbnailurl;
    url = _url;
    name = _name;
  }
}

[System.Serializable]
public class GISRawResult
{
  public Item[] items;

  [System.Serializable]
  public class Item
  {
    public string title;
    public string link;
    public string mime;
    public Image image;
  }

  [System.Serializable]
  public class Image
  {
    public string thumbnailLink;
    public int height;
    public int width;
    public int thumbnailHeight;
    public int thumbnailWidth;
  }

  public override string ToString()
  {
    string s = "";
    for (int i = 0; i < items.Length; i++)
    {
      s += items[i].title + "\n";
    }
    return s;

  }
}
