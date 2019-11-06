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

// Helper class to spread out work over frames. System.Actions can be
// enqueued, which will then be dequeued to a set limit per frame.
public class CoroutineQueue : MonoBehaviour
{
  private Queue<System.Action> queue = new Queue<System.Action>();
  private bool isProcessingQueue = false;
  private int dequeuesPerFrame = 10;

  public void Enqueue(System.Action action)
  {
    queue.Enqueue(action);
    // If isProcessingQueue is true, then we know the ProcessQueue
    // co-routine is running, so it will automatically pick up this item
    // from the queue and run it. If, however, isProcessingQueue is
    // false, then we have to start the ProcessQueue co-routine.
    if (!isProcessingQueue)
    {
      IEnumerator queueProcessor = ProcessQueue();
      // This will cause ProcessQueue to be called once per frame.
      StartCoroutine(queueProcessor);
    }
  }

  private IEnumerator ProcessQueue()
  {
    while (queue.Count > 0)
    {
      // This is the code that runs per frame. We will dequeue at
      // most dequeuesPerFrame until the queue is empty.
      int dequeuesThisFrame = 0;
      while (queue.Count > 0 && dequeuesThisFrame < dequeuesPerFrame)
      {
        System.Action action = queue.Dequeue();
        action();
        dequeuesThisFrame++;
      }
      // Note that we always need to wait a frame even if the queue
      // is now empty. Otherwise, everytime we add to the queue, the
      // coroutine will finish immediately after dequeueing, defeating
      // the purpose of this class.
      yield return null;
    }
    isProcessingQueue = false;
  }

}
