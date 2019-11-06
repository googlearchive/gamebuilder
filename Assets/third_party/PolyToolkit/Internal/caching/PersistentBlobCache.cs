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
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Linq;
using UnityEngine;
using System.Text;
using PolyToolkitInternal.model.util;

namespace PolyToolkitInternal.caching {
  /// <summary>
  /// A persistent disk-based LRU cache that can store associations of strings to arbitrary data.
  ///
  /// This can be used, for example, to implement a download cache for remote assets. This
  /// class is agnostic to the actual meaning of the keys and values. To this class, keys are just
  /// unique strings and values are just opaque byte arrays.
  ///
  /// This cache automatically offloads heavy work (I/O, decoding, etc) to a background thread to avoid
  /// blocking the main thread.
  /// 
  /// NOTE: We're not currently handling I/O errors -- the cache assumes that the file system works
  /// perfectly, which is a pretty fair assumption since we're using a hidden directory under
  /// AppData\Local\.... that normal users don't normally access (or even know about).
  /// Unless the user goes and messes around with the permissions of the directory, everything should
  /// work correctly. If we do get an I/O error (which would be rare), then we will just crash.
  /// </summary>
  [ExecuteInEditMode]
  public class PersistentBlobCache : MonoBehaviour {
    private const string BLOB_FILE_EXT = ".blob";

    public delegate void CacheReadCallback(bool success, byte[] data);

    /// <summary>
    /// Indicates whether Setup() was completed.
    /// </summary>
    private bool setupDone = false;

    /// <summary>
    /// Maximum number of entries allowed in the cache.
    /// </summary>
    private int maxEntries;

    /// <summary>
    /// Maximum total bytes allowed in the cache.
    /// </summary>
    private long maxSizeBytes;

    /// <summary>
    /// Root path to the cache.
    /// </summary>
    private string rootPath;

    /// <summary>
    /// MD5 hash computing function.
    /// </summary>
    private MD5 md5;

    /// <summary>
    /// Maps key hash to cache entry.
    /// This is owned by the BACKGROUND thread.
    /// </summary>
    private Dictionary<string, CacheEntry> cacheEntries = new Dictionary<string, CacheEntry>();

    /// <summary>
    /// Requests that are pending background work.
    /// </summary>
    private ConcurrentQueue<CacheRequest> requestsPendingWork = new ConcurrentQueue<CacheRequest>();

    /// <summary>
    /// Requests for which the background work is done, and which are pending delivery of callback in the
    /// main thread.
    /// </summary>
    private ConcurrentQueue<CacheRequest> requestsPendingDelivery = new ConcurrentQueue<CacheRequest>();

    /// <summary>
    /// Recycle pool of requests (to avoid reduce allocation).
    /// </summary>
    private ConcurrentQueue<CacheRequest> requestsRecyclePool = new ConcurrentQueue<CacheRequest>();

    /// <summary>
    /// Sets up a cache with the given characteristics.
    /// </summary>
    /// <param name="rootPath">The absolute path to the root of the cache.</param>
    /// <param name="maxEntries">The maximum number of entries in the cache.</param>
    /// <param name="maxSizeBytes">The maximum combined size of all entries in the cache.</param>
    public void Setup(string rootPath, int maxEntries, long maxSizeBytes) {
      this.rootPath = rootPath;
      this.maxEntries = maxEntries;
      this.maxSizeBytes = maxSizeBytes;

      // Check that we have a reasonable config:
      PolyUtils.AssertNotNullOrEmpty(rootPath, "rootPath can't be null or empty");
      PolyUtils.AssertTrue(Directory.Exists(rootPath), "rootPath must be an existing directory: " + rootPath);
      PolyUtils.AssertTrue(maxEntries >= 256, "maxEntries must be >= 256");
      PolyUtils.AssertTrue(maxSizeBytes >= 1048576, "maxSizeBytes must be >= 1MB");

      PtDebug.LogVerboseFormat("PBC initializing, root {0}, max entries {1}, max size {2}",
        rootPath, maxEntries, maxSizeBytes);

      md5 = MD5.Create();
      InitializeCache();

      setupDone = true;

      Thread backgroundThread = new Thread(BackgroundThreadMain);
      backgroundThread.IsBackground = true;
      backgroundThread.Start();
    }

    /// <summary>
    /// Requests a read from the cache.
    /// </summary>
    /// <param name="key">The key to read.</param>
    /// <param name="maxAgeMillis">Maximum age for a cache hit. If the copy we have on cache is older
    /// than this, the request will fail. Use -1 to mean "any age".</param>
    /// <param name="callback">The callback that is to be called (asynchronously) when the read operation
    /// finishes. This callback will be called on the MAIN thread.</param>
    public void RequestRead(string key, long maxAgeMillis, CacheReadCallback callback) {
      string hash = GetHash(key);
      CacheRequest request;
      if (!requestsRecyclePool.Dequeue(out request)) {
        request = new CacheRequest();
      }
      request.type = RequestType.READ;
      request.key = key;
      request.hash = hash;
      request.readCallback = callback;
      request.maxAgeMillis = maxAgeMillis;
      PtDebug.LogVerboseFormat("PBC: enqueing READ request for {0}", key);
      requestsPendingWork.Enqueue(request);
    }

    /// <summary>
    /// Requests a write to the cache. The data will be written asynchronously.
    /// </summary>
    /// <param name="key">The key to write.</param>
    /// <param name="data">The data to write.</param>
    public void RequestWrite(string key, byte[] data) {
      string hash = GetHash(key);
      CacheRequest request;
      if (!requestsRecyclePool.Dequeue(out request)) {
        request = new CacheRequest();
      }
      request.type = RequestType.WRITE;
      request.key = key;
      request.hash = hash;
      request.data = data;
      PtDebug.LogVerboseFormat("PBC: enqueing WRITE request for {0}", key);
      requestsPendingWork.Enqueue(request);
    }

    /// <summary>
    /// Requests that the cache be cleared. The cache will be cleared asynchronously.
    /// </summary>
    public void RequestClear() {
      CacheRequest request;
      if (!requestsRecyclePool.Dequeue(out request)) {
        request = new CacheRequest();
      }
      request.type = RequestType.CLEAR;
      PtDebug.LogVerboseFormat("PBC: enqueing CLEAR request.");
      requestsPendingWork.Enqueue(request);
    }

    /// <summary>
    /// Checks for pending deliveries and delivers them.
    /// </summary>
    private void Update() {
      if (!setupDone) return;

      // To avoid locking the queue on every frame, exit early if the volatile count is 0.
      if (requestsPendingDelivery.VolatileCount == 0) return;            

      // Check for a pending delivery.
      // Note that for performance reasons, we limit ourselves to delivering one result per frame.
      CacheRequest delivery;
      if (!requestsPendingDelivery.Dequeue(out delivery)) {
        return;
      }

      PtDebug.LogVerboseFormat("PBC: delivering result on {0} ({1}, {2} bytes).",
          delivery.hash, delivery.success ? "SUCCESS" : "FAILURE", delivery.data != null ? delivery.data.Length : -1);

      // Deliver the results to the callback.
      delivery.readCallback(delivery.success, delivery.data);

      // Recycle the request for reuse.
      delivery.Reset();
      requestsRecyclePool.Enqueue(delivery);
    }

    /// <summary>
    /// Initializes the cache (reads the cache state from disk).
    /// </summary>
    private void InitializeCache() {
      foreach (string file in Directory.GetFiles(rootPath)) {
        if (file.EndsWith(BLOB_FILE_EXT)) {
          FileInfo finfo = new FileInfo(file);
          string hash = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
          cacheEntries[hash] = new CacheEntry(hash, finfo.Length, TicksToMillis(finfo.LastWriteTimeUtc.Ticks));
          PtDebug.LogVerboseFormat("PBC: loaded existing cache item: {0} => {1} bytes",
              hash, cacheEntries[hash].fileSize);
        }
      }
    }

    /// <summary>
    /// Returns the hash of the given key.
    /// </summary>
    /// <param name="key">The input string.</param>
    /// <returns>The hash of the input string, in lowercase hexadecimal format.</returns>
    private string GetHash(string key) {
      byte[] hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hashBytes.Length; i++) {
        // x2 is 2-digit hexadecimal format (like "d8")
        sb.Append(hashBytes[i].ToString("x2"));
      }
      // x4 is 4-digit hexadecimal format (like "a1b2").
      return sb.ToString() + key.Length.ToString("x4");
    }

    /// <summary>
    /// Returns the full path corresponding to a hash value.
    /// </summary>
    /// <param name="hash">The hash value</param>
    /// <returns>The full path to the file that stores the asset with the indicated hash.</returns>
    private string HashToFullPath(string hash) {
      return Path.Combine(rootPath, hash + BLOB_FILE_EXT);
    }

    /// <summary>
    /// (Background thread). Main function that constantly checks the queues for pending requests and executes
    /// them as they arrive.
    /// </summary>
    private void BackgroundThreadMain() {
      try {
        while (true) {
          CacheRequest request;

          // Wait until the next request comes in.
          if (!requestsPendingWork.WaitAndDequeue(/* waitTime */ 5000, out request)) {
            continue;
          }

          // Process it.
          switch (request.type) {
            case RequestType.READ:
              BackgroundHandleReadRequest(request);
              break;
            case RequestType.WRITE:
              BackgroundHandleWriteRequest(request);
              break;
            case RequestType.CLEAR:
              BackgroundHandleClearRequest(request);
              break;
            default:
              PolyUtils.Throw("Invalid cache request type, should be READ, WRITE or CLEAR.");
              break;
          }
        }
      } catch (ThreadAbortException) {
        // That's ok (happens on project shutdown).
      } catch (Exception ex) {
        Debug.LogErrorFormat("Cache background thread crashed: " + ex);
      }
    }

    /// <summary>
    /// (Background thread). Handles a read request, reading it from disk and scheduling the delivery
    /// of the results to the caller.
    /// </summary>
    /// <param name="readRequest">The read request to execute.</param>
    private void BackgroundHandleReadRequest(CacheRequest readRequest) {
      PtDebug.LogVerboseFormat("PBC: executing read request for {0} ({1})", readRequest.key,
        readRequest.hash);

      string fullPath = HashToFullPath(readRequest.hash);

      CacheEntry entry;
      if (!cacheEntries.TryGetValue(readRequest.hash, out entry)) {
        // Not in the cache
        readRequest.data = null;
        readRequest.success = false;
      } else if (readRequest.maxAgeMillis > 0 && entry.AgeMillis > readRequest.maxAgeMillis) {
        // Too old.
        readRequest.data = null;
        readRequest.success = false;
      } else if (!File.Exists(fullPath)) {
        // Too old.
        readRequest.data = null;
        readRequest.success = false;
      } else {
        // Found it.
        readRequest.data = File.ReadAllBytes(fullPath);
        readRequest.success = true;
        // Update the read timestamp.
        entry.readTimestampMillis = TicksToMillis(DateTime.UtcNow.Ticks);
      }

      // Schedule the result for delivery to the caller.
      requestsPendingDelivery.Enqueue(readRequest);
    }

    /// <summary>
    /// (Background thread). Handles a write request. Writes the data to disk.
    /// </summary>
    /// <param name="writeRequest">The write request to execute.</param>
    private void BackgroundHandleWriteRequest(CacheRequest writeRequest) {
      PtDebug.LogVerboseFormat("PBC: executing write request for {0}", writeRequest.hash);

      string fullPath = HashToFullPath(writeRequest.hash);
      string tempPath = Path.Combine(Path.GetDirectoryName(fullPath), "temp.dat");

      // In the event of a crash or hardware issues -- e.g., user trips on the power cord, our write
      // to disk might be interrupted in an inconsistent state. So instead of writing directly to
      // the destination file, we write to a temporary file and then move.
      File.WriteAllBytes(tempPath, writeRequest.data);
      if (File.Exists(fullPath)) File.Delete(fullPath);
      File.Move(tempPath, fullPath);

      // Update the file size and last used time information in the cache.
      CacheEntry entry;
      if (!cacheEntries.TryGetValue(writeRequest.hash, out entry)) {
        entry = cacheEntries[writeRequest.hash] = new CacheEntry(writeRequest.hash);
      }
      entry.fileSize = writeRequest.data.Length;
      entry.writeTimestampMillis = TicksToMillis(DateTime.UtcNow.Ticks);

      // We are done with writeRequest, so we can recycle it.
      writeRequest.Reset();
      requestsRecyclePool.Enqueue(writeRequest);

      // Check if the cache needs trimming.
      TrimCache();
    }

    /// <summary>
    /// (Background thread). Clears the entire cache.
    /// </summary>
    private void BackgroundHandleClearRequest(CacheRequest clearRequest) {
      PtDebug.LogVerboseFormat("Clearing the cache.");
      foreach (string file in Directory.GetFiles(rootPath, "*" + BLOB_FILE_EXT)) {
        File.Delete(file);
      }
      cacheEntries.Clear();
      clearRequest.Reset();
      requestsRecyclePool.Enqueue(clearRequest);
    }

    private void TrimCache() {
      long totalSize = 0;
      foreach (CacheEntry cacheEntry in cacheEntries.Values) {
        totalSize += cacheEntry.fileSize;
      }

      if (totalSize <= maxSizeBytes && cacheEntries.Count <= maxEntries) {
        // We're within budget, no need to trim the cache.
        return;
      }

      // Sort the entries from oldest to newest. This is the order in which we will evict them.
      Queue<CacheEntry> entriesOldestToNewest =
        new Queue<CacheEntry>(cacheEntries.Values.OrderBy(entry => entry.writeTimestampMillis));

      // Each iteration evicts the oldest item, until we're back under budget.
      while (totalSize > maxSizeBytes || cacheEntries.Count > maxEntries) {
        PtDebug.LogVerboseFormat("PBC: trimming cache, bytes {0}/{1}, entries {2}/{3}",
          totalSize, maxSizeBytes, cacheEntries.Values.Count, maxEntries);

        // What's the oldest file?
        if (entriesOldestToNewest.Count == 0) break;
        CacheEntry oldest = entriesOldestToNewest.Dequeue();

        // Delete this file.
        string filePath = HashToFullPath(oldest.hash);
        if (File.Exists(filePath)) {
          File.Delete(filePath);
        }
        cacheEntries.Remove(oldest.hash);

        // Update our accounting
        totalSize -= oldest.fileSize;
      }

      PtDebug.LogVerboseFormat("PBC: end of trim, bytes {0}/{1}, entries {2}/{3}",
          totalSize, maxSizeBytes, cacheEntries.Count, maxEntries);
    }

    private static long TicksToMillis(long ticks) {
      // According to the docs: "A single tick represents one hundred nanoseconds or one ten-millionth of a
      // second. There are 10,000 ticks in a millisecond, or 10 million ticks in a second."
      // https://msdn.microsoft.com/en-us/library/system.datetime.ticks(v=vs.110).aspx
      return ticks / 10000L;
    }

    private void OnEnable() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        // In the Unity Editor, we need to install a delegate to get Update() frequently
        // (otherwise we'd only get it when something in the scene changes).
        UnityEditor.EditorApplication.update += Update;
      }
#endif
    }

    private void OnDisable() {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        // In the Unity Editor, we need to install a delegate to get Update() frequently
        // (otherwise we'd only get it when something in the scene changes).
        UnityEditor.EditorApplication.update -= Update;
      }
#endif
    }

    /// <summary>
    /// Represents each entry in the cache.
    /// </summary>
    private class CacheEntry {
      /// <summary>
      /// Hash of the entry's key.
      /// </summary>
      public string hash;

      /// <summary>
      /// Size of the file, in bytes.
      /// </summary>
      public long fileSize = 0;

      /// <summary>
      /// Time in millis when the file was last written to.
      /// </summary>
      public long writeTimestampMillis = 0;

      /// <summary>
      /// Time in millis when the file was last read.
      /// </summary>
      public long readTimestampMillis = 0;

      /// <summary>
      /// Age of the file (millis since it was last written).
      /// </summary>
      public long AgeMillis { get { return TicksToMillis(DateTime.UtcNow.Ticks) - writeTimestampMillis; } }

      public CacheEntry(string hash) { this.hash = hash;  }
      public CacheEntry(string hash, long fileSize, long writeTimestampMillis) {
        this.hash = hash;
        this.fileSize = fileSize;
        this.writeTimestampMillis = writeTimestampMillis;
      }
    }

    public enum RequestType {
      // Read a file from the cache.
      READ,
      // Write a file to the cache.
      WRITE,
      // Clear the entire cache.
      CLEAR
    }

    /// <summary>
    /// Represents a cache operation request.
    ///
    /// We reuse the same class for read and write requests (even though it might be a bit confusing)
    /// because we pool these objects to avoid allocation.
    /// </summary>
    private class CacheRequest {
      /// <summary>
      /// Type of request (see RequestType for details).
      /// </summary>
      public RequestType type;
      /// <summary>
      /// The key to read or write.
      /// </summary>
      public string key;
      /// <summary>
      /// The hash of the key.
      /// </summary>
      public string hash;
      /// <summary>
      /// The callback to call when the request is complete (for READ requests only).
      /// </summary>
      public CacheReadCallback readCallback;
      /// <summary>
      /// The request data. For READ requests, this is an out parameter that points to the
      /// data at the end of the operation. For WRITE requests, this is an in parameter that
      /// points to the data to write.
      /// </summary>
      public byte[] data;
      /// <summary>
      /// For READ requests, this is the maximum accepted age of the cached copy, in millis.
      /// Ignored for WRITE requests.
      /// </summary>
      public long maxAgeMillis;
      /// <summary>
      /// Indicates whether or not the request was successful. Only used for READ requests.
      /// </summary>
      public bool success;

      public CacheRequest() {
        Reset();
      }

      public void Reset() {
        type = RequestType.READ;
        hash = null;
        readCallback = null;
        success = false;
        data = null;
        maxAgeMillis = -1;
      }
    }
  }
}
