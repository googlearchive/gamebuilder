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

using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;
using System.Collections;
using PolyToolkitInternal.model.util;
using PolyToolkitInternal.caching;
using System.IO;
using PolyToolkit;

namespace PolyToolkitInternal.client.model.util {
  /// <summary>
  /// Manages web requests, limiting how many can happen simultaneously at any given time and re-using
  /// buffers as much as possible to avoid reallocation and garbage collection.
  /// 
  /// Not *all* web requests must be routed through this class. Small, infrequent web requests can be made directly
  /// via UnityWebRequest without using this class. However, larger or frequent requests should use this, since this
  /// will avoid the expensive allocation of numerous download buffers (a typical UnityWebRequest allocates many
  /// small buffers for temporary transfer and a larger buffer to contain the download, and they all become garbage
  /// that the GC has to clean up).
  /// </summary>
  [ExecuteInEditMode]
  public class WebRequestManager : MonoBehaviour {
    /// <summary>
    /// Constant passed to EnqueueRequest to mean that the request should not be retrieved from cache.
    /// </summary>
    public const long CACHE_NONE = 0;

    /// <summary>
    /// Constant passed to EnqueueRequest to mean that a cached copy is acceptable regardless of its age.
    /// </summary>
    public const long CACHE_ANY_AGE = -1;

    /// <summary>
    /// Maximum number of concurrent downloads to allow.
    /// </summary>
#if UNITY_STANDALONE
    private const int MAX_CONCURRENT_DOWNLOADS = 8;
#else
    private const int MAX_CONCURRENT_DOWNLOADS = 4;
#endif

    /// <summary>
    /// Delegate that creates a UnityWebRequest. This is used by client code to set up a UnityWebRequest
    /// with the desired parameters.
    /// </summary>
    /// <returns>The UnityWebRequest.</returns>
    public delegate UnityWebRequest CreationCallback();

    /// <summary>
    /// Delegate that processes the completion of a web request. We call this delegate to inform the client that
    /// a web request has completed.
    /// </summary>
    /// <param name="status">The status of the request.</param>
    /// <param name="responseCode">The HTTP response code.</param>
    /// <param name="responseBytes">The response body.</param>
    public delegate void CompletionCallback(PolyStatus status, int responseCode, byte[] responseBytes);

    /// <summary>
    /// Represents a pending request that we have in the queue.
    /// </summary>
    private class PendingRequest {
      /// <summary>
      /// The creation callback. When this request's turn arrives, we will call this to create the UnityWebRequest.
      /// </summary>
      public CreationCallback creationCallback;
      /// <summary>
      /// Completion callback. We will call this when the web request completes.
      /// </summary>
      public CompletionCallback completionCallback;
      /// <summary>
      /// Maximum age of the cached copy, in milliseconds.
      /// NO_CACHE means we will not use the cache.
      /// ANY_AGE means any age is OK.
      /// </summary>
      public long maxAgeMillis;

      public PendingRequest(CreationCallback creationCallback, CompletionCallback completionCallback,
          long maxAgeMillis) {
        this.creationCallback = creationCallback;
        this.completionCallback = completionCallback;
        this.maxAgeMillis = maxAgeMillis;
      }
    }

    /// <summary>
    /// Holds buffers for an active web request. Each concurrent active web request must own its own BufferHolder,
    /// which is where it stores data. Web requests are implemented as coroutines, so this is the same as saying
    /// that each of our active coroutines owns one BufferHolder.
    /// </summary>
    private class BufferHolder {
      // TODO: use pre-allocated buffers for downloading.
      // (For now, this object serves as a "token" whose availability controls the maximum concurrent downloads).
    }

    /// <summary>
    /// Requests that are pending execution. This is a concurrent queue because requests may come in from any
    /// thread. Requests are serviced on the main thread.
    /// </summary>
    private ConcurrentQueue<PendingRequest> pendingRequests = new ConcurrentQueue<PendingRequest>();

    /// <summary>
    /// List of BufferHolders that are idle (not being used by any download coroutine).
    /// BufferHolders are returned to this list when coroutines finish.
    /// </summary>
    private List<BufferHolder> idleBuffers = new List<BufferHolder>();

    /// <summary>
    /// Cache for web responses.
    /// </summary>
    private PersistentBlobCache cache;

    public void Setup(PolyCacheConfig config) {
      // Create all the buffer holders. They are all initially idle.
      for (int i = 0; i < MAX_CONCURRENT_DOWNLOADS; i++) {
        idleBuffers.Add(new BufferHolder());
      }

      // Caching is only supported on Windows/Mac for now.
      bool cacheSupported = Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.OSXPlayer;
      PtDebug.LogFormat("Platform: {0}, cache supported: {1}", Application.platform, cacheSupported);

      if (cacheSupported && config.cacheEnabled) {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string defaultCachePath = Path.Combine(Path.Combine(Path.Combine(
          appDataPath, Application.companyName), Application.productName), "WebRequestCache");

        string cachePath = config.cachePathOverride;
        if (string.IsNullOrEmpty(cachePath)) {
          cachePath = defaultCachePath;
        }
        // Note: Directory.CreateDirectory creates all directories in the path.
        Directory.CreateDirectory(cachePath);

        cache = gameObject.AddComponent<PersistentBlobCache>();
        cache.Setup(cachePath, config.maxCacheEntries, config.maxCacheSizeMb * 1024 * 1024);
      }
    }

    /// <summary>
    /// Enqueues a request. Can be called from any thread.
    /// </summary>
    /// <param name="creationCallback">The callback that creates the UnityWebRequest. This callback will
    /// be called when the request is ready to be serviced.</param>
    /// <param name="completionCallback">The callback to call when the request is complete. Will be called
    /// when the request completes.</param>
    /// <param name="maxAgeMillis">Indicates the cache strategy. If this is NO_CACHE, the cache will
    /// not be used, if it's a positive value, it indicates what is the maximum age of the cached
    /// copy that is considered acceptable. If it's ANY_AGE, any cached copy regardless of age
    /// will be considered acceptable.</param>
    public void EnqueueRequest(CreationCallback creationCallback, CompletionCallback completionCallback,
        long maxAgeMillis) {
      // Your call is very important to us.
      // Please stay on the line and your request will be handled by the next available operator.
      pendingRequests.Enqueue(new PendingRequest(creationCallback, completionCallback, maxAgeMillis));

      // If we are running in the editor, we don't have an update loop, so we have to manually
      // start pending requests here.
      if (!Application.isPlaying) {
        StartPendingRequests();
      }
    }

    /// <summary>
    /// Clears the local web cache. This is asynchronous (the cache will be cleared in the background).
    /// </summary>
    public void ClearCache() {
      if (cache != null) {
        cache.RequestClear();
      }
    }

    private void Update() {
      StartPendingRequests();
    }

    private void StartPendingRequests() {
      // Start pending web requests if we have idle buffers.
      PendingRequest pendingRequest;
      while (idleBuffers.Count > 0 && pendingRequests.Dequeue(out pendingRequest)) {
        // Service the request.
        // Fetch an idle BufferHolder. We will own that BufferHolder for the duration of the coroutine.
        BufferHolder bufferHolder = idleBuffers[idleBuffers.Count - 1];
        // Remove it from the idle list because it's now in use. It will be returned to the pool
        // by HandleWebRequest, when it's done with it.
        idleBuffers.RemoveAt(idleBuffers.Count - 1);
        // Start the coroutine that will handle this web request. When the coroutine is done,
        // it will return the buffer to the pool.
        CoroutineRunner.StartCoroutine(this, HandleWebRequest(pendingRequest, bufferHolder));
      }
    }

    /// <summary>
    /// Co-routine that services one PendingRequest. This method must be called with StartCoroutine.
    /// </summary>
    /// <param name="request">The request to service.</param>
    private IEnumerator HandleWebRequest(PendingRequest request, BufferHolder bufferHolder) {
      // NOTE: This method runs on the main thread, but never blocks -- the blocking part of the work is
      // done by yielding the UnityWebRequest, which releases the main thread for other tasks while we
      // are waiting for the web request to complete (by the miracle of coroutines).

      // Let the caller create the UnityWebRequest, configuring it as they want. The caller can set the URL,
      // method, headers, anything they want. The only thing they can't do is call Send(), as we're in charge
      // of doing that.
      UnityWebRequest webRequest = request.creationCallback();

      PtDebug.LogVerboseFormat("Web request: {0} {1}", webRequest.method, webRequest.url);

      bool cacheAllowed = cache != null && webRequest.method == "GET" && request.maxAgeMillis != CACHE_NONE;

      // Check the cache (if it's a GET request and cache is enabled).
      if (cacheAllowed) {
        bool cacheHit = false;
        byte[] cacheData = null;
        bool cacheReadDone = false;
        cache.RequestRead(webRequest.url, request.maxAgeMillis, (bool success, byte[] data) => {
          cacheHit = success;
          cacheData = data;
          cacheReadDone = true;
        });
        while (!cacheReadDone) {
          yield return null;
        }
        if (cacheHit) {
          PtDebug.LogVerboseFormat("Web request CACHE HIT: {0}, response: {1} bytes",
            webRequest.url, cacheData.Length);
          request.completionCallback(PolyStatus.Success(), /* responseCode */ 200, cacheData);

          // Return the buffer to the pool for reuse.
          CleanUpAfterWebRequest(bufferHolder);

          yield break;
        } else {
          PtDebug.LogVerboseFormat("Web request CACHE MISS: {0}.", webRequest.url);
        }
      }

      DownloadHandlerBuffer handler = new DownloadHandlerBuffer();
      webRequest.downloadHandler = handler;

      // We need to asset that we actually succeeded in setting the download handler, because this can fail
      // if, for example, the creation callback mistakenly called Send().
      PolyUtils.AssertTrue(webRequest.downloadHandler == handler,
        "Couldn't set download handler. It's either disposed of, or the creation callback mistakenly called Send().");

      // Start the web request. This will suspend this coroutine until the request is done.
      PtDebug.LogVerboseFormat("Sending web request: {0}", webRequest.url);
      yield return UnityCompat.SendWebRequest(webRequest);

      // Request is finished. Call user-supplied callback.
      // We surround this part with "if PtDebug.DEBUG_LOG_VERBOSE" because calling webRequest.downloadHandler.text
      // is very expensive, and even if verbose logging is off, we would incur the cost of decoding.
      #pragma warning disable 0162  // Don't warn about unreachable code.
      if (PtDebug.DEBUG_LOG_VERBOSE) {
        PtDebug.LogVerboseFormat("Web request finished: {0}, HTTP response code {1}, response: {2}",
            webRequest.url, webRequest.responseCode, webRequest.downloadHandler.text);
      }
      #pragma warning restore 0162  // Don't warn about unreachable code.

      PolyStatus status = UnityCompat.IsNetworkError(webRequest) ? PolyStatus.Error(webRequest.error) : PolyStatus.Success();
      request.completionCallback(status, (int)webRequest.responseCode, webRequest.downloadHandler.data);

      // Cache the result, if applicable.
      if (!UnityCompat.IsNetworkError(webRequest) && cacheAllowed) {
        byte[] data = webRequest.downloadHandler.data;
        if (data != null && data.Length > 0) {
          // Note: DownloadHandlerBuffer.data is a copy of the underlying buffer, so we own the
          // byte array that it returns. This means it's safe to pass it to RequestWrite, which is
          // asynchronous:
          cache.RequestWrite(webRequest.url, data);
        }
      }

      // Clean up.
      webRequest.Dispose();
      CleanUpAfterWebRequest(bufferHolder);
    }

    private void CleanUpAfterWebRequest(BufferHolder bufferHolder) {
      // Return the buffer to the pool for reuse.
      idleBuffers.Add(bufferHolder);

      // If we are running in the editor, we don't have an update loop, so we have to manually
      // start pending requests here.
      if (!Application.isPlaying) {
        StartPendingRequests();
      }
    }
  }
}
