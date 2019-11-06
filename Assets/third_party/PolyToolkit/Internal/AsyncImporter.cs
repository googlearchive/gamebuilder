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

using PolyToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace PolyToolkitInternal {

/// <summary>
/// Handles asynchronous importing of assets.
/// </summary>
[ExecuteInEditMode]
public class AsyncImporter : MonoBehaviour {
  /// <summary>
  /// Callback called when an async import operation is finished.
  /// </summary>
  /// <param name="meshCreator">The mesh creator enumerable. The caller must fully enumerate this
  /// enumerable in order to create all meshes.</param>
  public delegate void AsyncImportCallback(PolyStatus status, GameObject root, IEnumerable meshCreator);

  /// <summary>
  /// Queue of operations whose background part has finished, and are awaiting to be picked up by
  /// the main thread for callback dispatching. To operate on this queue, hold the
  /// finishedOperationsLock lock.
  /// </summary>
  private Queue<ImportOperation> finishedOperations = new Queue<ImportOperation>();
  private object finishedOperationsLock = new byte[0];

  /// <summary>
  /// Must be called to set up this object before use.
  /// </summary>
  public void Setup() {
    // No init needed for now. But leave this hook in place for consistency with other classes,
    // and it's a good habit.
  }

  /// <summary>
  /// Imports the given format of the given asset, asynchronously in a background thread.
  /// Calls the supplied callback when done.
  /// </summary>
  public void ImportAsync(PolyAsset asset, PolyFormat format, PolyImportOptions options,
      AsyncImportCallback callback = null) {
    ImportOperation operation = new ImportOperation();
    operation.instance = this;
    operation.asset = asset;
    operation.format = format;
    operation.options = options;
    operation.callback = callback;
    operation.status = PolyStatus.Success();
    operation.loader = new FormatLoader(format);
    if (Application.isPlaying) {
      ThreadPool.QueueUserWorkItem(new WaitCallback(BackgroundThreadProc), operation);
    } else {
      // If we are in the editor, don't do this in a background thread. Do it directly
      // here on the main thread.
      BackgroundThreadProc(operation);
      Update();
    }
  }

  private static void BackgroundThreadProc(object userData) {
    ImportOperation operation = (ImportOperation)userData;
    try {  
      using (TextReader reader = new StreamReader(new MemoryStream(operation.format.root.contents), Encoding.UTF8)) {
        operation.importState = ImportGltf.BeginImport(
          operation.format.formatType == PolyFormatType.GLTF ? GltfSchemaVersion.GLTF1 : GltfSchemaVersion.GLTF2,
          reader, operation.loader, operation.options);
      }
    } catch (Exception ex) {
      Debug.LogException(ex);
      operation.status = PolyStatus.Error("Error importing asset.", ex);
    }
    // Done with background thread part, let's queue it so we can finish up on the main thread.
    operation.instance.EnqueueFinishedOperation(operation);
  }

  private void EnqueueFinishedOperation(ImportOperation operation) {
    lock (finishedOperationsLock) {
      finishedOperations.Enqueue(operation);
    }
  }

  public void Update() {
    // We process at most one import result per frame, to avoid doing too much work
    // in the main thread.
    ImportOperation operation;
    lock (finishedOperationsLock) {
      if (finishedOperations.Count == 0) return;
      operation = finishedOperations.Dequeue();
    }
    
    if (!operation.status.ok) {
      // Import failed.
      operation.callback(operation.status, root: null, meshCreator: null);
      return;
    }

    try {
      IEnumerable meshCreator;
      ImportGltf.GltfImportResult result = ImportGltf.EndImport(operation.importState,
          operation.loader, out meshCreator);

      if (!operation.options.clientThrottledMainThread) {
        // If we're not in throttled mode, create all the meshes immediately by exhausting
        // the meshCreator enumeration. Otherwise, it's the caller's responsibility to
        // do this.
        foreach (var unused in meshCreator) { /* empty */ }
        meshCreator = null;
      }
      // Success.
      operation.callback(PolyStatus.Success(), result.root, meshCreator);
    } catch (Exception ex) {
      // Import failed.
      Debug.LogException(ex);
      operation.callback(PolyStatus.Error("Failed to convert import to Unity objects.", ex),
          root: null, meshCreator: null);
    }
  }

  /// <summary>
  /// Represents in import operation that's in progress.
  /// </summary>
  private class ImportOperation {
    /// <summary>
    /// Instance of the AsyncImporter that this operation belongs go.
    /// We need this because the thread main method has to be static.
    /// </summary>
    public AsyncImporter instance;
    /// <summary>
    /// The asset we are importing.
    /// </summary>
    public PolyAsset asset;
    /// <summary>
    /// The format of the asset we are importing.
    /// </summary>
    public PolyFormat format;
    /// <summary>
    /// Import options.
    /// </summary>
    public PolyImportOptions options;
    /// <summary>
    /// The callback we are supposed to call at the end of the operation.
    /// </summary>
    public AsyncImportCallback callback;
    /// <summary>
    /// The import state as returned from ImportGltf.BeginImport.
    /// </summary>
    public ImportGltf.ImportState importState;
    /// <summary>
    /// The loader used to load resources for the import.
    /// </summary>
    public IUriLoader loader;
    /// <summary>
    /// Status of the import operation.
    /// </summary>
    public PolyStatus status;
  }
}

}
