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

using System.Collections.Generic;
using System.Threading;

namespace PolyToolkitInternal.model.util {
  /// <summary>
  ///   Simple thread-safe queue for sending work across threads.  Unfortunately,
  ///   Unity doesn't have support for System.Collection.Concurrent, so we have
  ///   to roll our own.
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class ConcurrentQueue<T> {

    // The non-thread-safe queue used as a backing store.
    private Queue<T> queue = new Queue<T>();

    // Count of items in the queue, so that they can be queried without locking the queue.
    private volatile int volatileCount;

    /// <summary>
    /// Returns the count of items in the queue without locking the queue. Use this value
    /// with caution, since the queue's count may be updated concurrently by threads
    /// enqueueing and dequeueing items.
    /// 
    /// This is safe to use in some scenarios: for example, if you have one thread that only
    /// enqueues things and another thread that only dequeues things, and they are the only
    /// threads that use the queue, then you can safely assume that when you check VolatileCount
    /// from the dequeuing thread, it will be less than or equal to the actual number of
    /// items on the queue, since it can only have increased since you checked.
    /// </summary>
    public int VolatileCount { get { return volatileCount; } }

    /// <summary>
    ///   Add something to the back of the Queue.  This can be called from any thread.
    /// </summary>
    /// <param name="obj">The object to enqueue.</param>
    public void Enqueue(T obj) {
      Monitor.Enter(queue);
      try {
        queue.Enqueue(obj);
        volatileCount = queue.Count;
        Monitor.Pulse(queue);
      } finally {
        Monitor.Exit(queue);
      }
    }

    /// <summary>
    ///   Try to remove something from the front of the queue.  If the queue is empty,
    ///   the default value (usually null) is returned.
    /// </summary>
    /// <param name="obj">The object taken from the queue.</param>
    /// <returns>True if we were able to remove an item successfully.</returns>
    public bool Dequeue(out T obj) {
      Monitor.Enter(queue);
      try {
        if (queue.Count > 0) {
          obj = queue.Dequeue();
          volatileCount = queue.Count;
          return true;
        } else {
          obj = default(T);
          return false;
        }
      } finally {
        Monitor.Exit(queue);
      }
    }

    /// <summary>
    ///   Try to remove something from the front of the queue.  If the queue is empty, wait the
    ///   given amount of time for something to be put into the queue.
    ///   If nothing is found, the default value (usually null) is returned.
    /// </summary>
    /// <param name="waitTime">Maximum time to wait for an item.</param>
    /// <param name="obj">The object from the queue.</param>
    /// <returns>True if we were able to remove something successfully.</returns>
    public bool WaitAndDequeue(int waitTime, out T obj) {
      Monitor.Enter(queue);
      try {
        // If something is in the queue, return immediately
        if (queue.Count > 0) {
          obj = queue.Dequeue();
          volatileCount = queue.Count;
          return true;
        }
        // Otherwise wait for a notification that something was added
        Monitor.Wait(queue, waitTime);
        if (queue.Count > 0) {
          obj = queue.Dequeue();
          volatileCount = queue.Count;
          return true;
        } else {
          obj = default(T);
          return false;
        }
      } finally {
        Monitor.Exit(queue);
      }
    }
  }
}