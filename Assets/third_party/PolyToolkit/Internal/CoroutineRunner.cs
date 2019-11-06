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

#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PolyToolkitInternal {
  /// <summary>
  /// Runs coroutines in a way that's compatible with the Unity editor (which normally doesn't
  /// like Coroutines).
  /// </summary>
  public class CoroutineRunner {
    /// <summary>
    /// The stack of coroutine(s) we are running.
    /// The top element is the one we are currently running. When it ends, we return to the
    /// previous one, etc, until all of them are done.
    /// </summary>
    private Stack<IEnumerator> coroutineStack = null;

    /// <summary>
    /// If not null, the coroutine has yielded the async operation and we're waiting for it to end.
    /// </summary>
    private AsyncOperation currentAsyncOperation = null;

    /// <summary>
    /// Start a coroutine. This will smartly decide whether to use the regular Unity framework or
    /// implement it manually, depending on whether we're running in the editor or not.
    /// </summary>
    /// <param name="host">The behavior that hosts the coroutine.</param>
    /// <param name="coroutine">The coroutine to run.</param>
    public static void StartCoroutine(MonoBehaviour host, IEnumerator coroutine) {
      if (Application.isPlaying) {
        // No need for our special logic. Just do it in the regular Unity way.
        host.StartCoroutine(coroutine);
        return;
      }
      // We are in the editor, so we need to use our custom runner.
      CoroutineRunner runner = new CoroutineRunner(coroutine);
      runner.Start();
    }

    private CoroutineRunner(IEnumerator coroutine) {
      coroutineStack = new Stack<IEnumerator>();
      coroutineStack.Push(coroutine);
    }

    public void Start() {
      // During the execution of this coroutine, we will have to hook into the Editor's update loop.
      EditorApplication.update += Tick;
    }

    private void Tick() {
      if (currentAsyncOperation != null) {
        if (!currentAsyncOperation.isDone) {
          // Async operation isn't done yet. We shouldn't continue the coroutine until it's done.
          return;
        }
        currentAsyncOperation = null;
      }

      // Ok, we are ready to continue the coroutine.
      IEnumerator topCoroutine = coroutineStack.Peek();
      if (!topCoroutine.MoveNext()) {
        // End of coroutine. Pop it and return to previous coroutine on the stack.
        coroutineStack.Pop();
        if (coroutineStack.Count == 0) {
          // The end! Thank you for using CoroutineRunner.
          EditorApplication.update -= Tick;
        }
      } else if (topCoroutine.Current is IEnumerator) {
        // When a coroutine returns an IEnumerator, it wants to execute a nested coroutine.
        coroutineStack.Push((IEnumerator)topCoroutine.Current);
      } else if (topCoroutine.Current == null) {
        // When a coroutine returns null, it means "I got nothing, but call again later."
        return;
      } else if (topCoroutine.Current is AsyncOperation) {
        // When a coroutine returns an AsyncOperation, it wants us to wait until it's finished
        // to continue the coroutine.
        currentAsyncOperation = (AsyncOperation)topCoroutine.Current;
      }
    }
  }
}

#else

// Trivial implementation for non Unity-editor builds (which simply delegates
// to MonoBehaviour.StartCoroutine.

using UnityEngine;
using System.Collections;
namespace PolyToolkitInternal {
  public class CoroutineRunner {
    public static void StartCoroutine(MonoBehaviour host, IEnumerator coroutine) {
      host.StartCoroutine(coroutine);
    }
  }
}
#endif