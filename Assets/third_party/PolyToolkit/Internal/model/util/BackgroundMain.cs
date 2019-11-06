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
using System.Threading;
using UnityEngine;

namespace PolyToolkitInternal.model.util {

  /// <summary>
  /// Class to manage background work tasks.
  /// </summary>
  [ExecuteInEditMode]
  public class BackgroundMain : MonoBehaviour {
    private Thread backgroundThread;
    private bool running = true;
    private bool setupDone = false;

    /// <summary>
    /// Queue to hold BackgroundWork tasks.
    /// </summary>
    private ConcurrentQueue<BackgroundWork> backgroundQueue = new ConcurrentQueue<BackgroundWork>();

    /// <summary>
    /// Queue to hold completed background work tasks so that the main thread may execute the relevant post work.
    /// </summary>
    private ConcurrentQueue<BackgroundWork> forMainThread = new ConcurrentQueue<BackgroundWork>();

    /// <summary>
    ///   Enqueue work that should be done on a background thread.
    /// </summary>
    /// <param name="work">The work.</param>
    public void DoBackgroundWork(BackgroundWork work) {
      if (Application.isPlaying) {
        backgroundQueue.Enqueue(work);
      } else {
        // In the Editor, just do the work right away on the main thread (background threads
        // don't work well -- or we just don't know how to use them).
        work.BackgroundWork();
        work.PostWork();
      }
    }

    /// <summary>
    ///   Main function for the background work thread.
    /// </summary>
    private void ProcessBackgroundWork() {
      while (running) {
        BackgroundWork work;
        if (backgroundQueue.WaitAndDequeue(/* wait time ms */ 1000, out work)) {
          try {
            work.BackgroundWork();
            forMainThread.Enqueue(work);
          } catch(Exception e) {
            // Should probably be a fatal error.  For now, just log something.
            Debug.LogError("Exception handling background work: " + e);
          }
        }
      }
    }

    /// <summary>
    /// Does one-time setup. Must be called before anything.
    /// </summary>
    public void Setup() {
      if (Application.isPlaying) {
        backgroundThread = new Thread(ProcessBackgroundWork);
        backgroundThread.IsBackground = true;
        backgroundThread.Priority = System.Threading.ThreadPriority.Lowest;
        backgroundThread.Start();
      }
      setupDone = true;
    }

    void Update() {
      if (!Application.isPlaying) return;

      if (!setupDone) return;
      // While we have done less than 5ms of work from the work queue, start doing new work. Note that the entirety of
      // the de-queued new work will be performed this frame.
      float startTime = Time.realtimeSinceStartup;
      BackgroundWork work;
      while ((Time.realtimeSinceStartup - startTime) < 0.005f && forMainThread.Dequeue(out work)) {
        work.PostWork();
      }
    }
  }
}