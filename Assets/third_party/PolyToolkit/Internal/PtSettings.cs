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
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

using PolyToolkit;

namespace PolyToolkitInternal {

// There should be only one instance of this asset in the project:
// the one that comes with the Poly Toolkit.
#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
public class PtSettings : ScriptableObject {
  /// <summary>
  /// Poly Toolkit version number.
  /// </summary>
  public static Version Version {
    get { return new Version { major = 1, minor = 1 }; }
  }

  private static PtSettings instance;

  public static PtSettings Instance {
    get {
      if (instance == null) {
        if (Application.isPlaying) {
          // We can't lazy-initialize because we don't know if we are on the main thread, and we'd need
          // to call Resoures.Load to find the PtSettings instance, which can only be called on
          // the main thread. So we must insist that the developer initialize the API properly.
          throw new Exception("Poly Toolkit not initialized (failed to get PtSettings). " +
              "Add a PolyToolkitManager to your scene, or manually call PolyApi.Init from main thread.");
        } else {
          // In the Editor, we can lazy-initialize because we only use the main thread.
          instance = FindPtSettings();
        }
      }
      return instance;
    }
  }

#if UNITY_EDITOR
  static PtSettings() {
    UnityEditor.EditorApplication.update += UpdateSettings;
  }

  static void UpdateSettings() {
    Init();
    if (Instance != null) {
      if (Instance.playerColorSpace != UnityEditor.PlayerSettings.colorSpace) {
        Instance.playerColorSpace = UnityEditor.PlayerSettings.colorSpace;
        UnityEditor.EditorUtility.SetDirty(Instance);
      }
    } else {
      UnityEditor.EditorApplication.update -= UpdateSettings;
    }
  }
#endif

  /// <summary>
  /// Initialize PtSettings. Must be called on main thread.
  /// </summary>
  public static void Init() {
    instance = FindPtSettings();
  }

  /// <summary>
  /// Finds the singleton PtSettings instance. Works during edit time and run time.
  /// At edit time, we search for the asset using FindAssets("t:PtSettings").
  /// At run time, we load it as a resource.
  /// </summary>
  /// <returns>The project's PtSettings instance.</returns>
  static PtSettings FindPtSettings() {
    if (Application.isPlaying) {
      // At run-time, just load the resource.
      PtSettings ptSettings = Resources.Load<PtSettings>("PtSettings");
      if (ptSettings == null) {
        Debug.LogError("PtSettings not found in Resources. Re-import Poly Toolkit.");
      }
      return ptSettings;
    }
#if UNITY_EDITOR
    // We're in the editor at edit-time, so just search for the asset in the project.
    string[] foundPaths = UnityEditor.AssetDatabase.FindAssets("t:PtSettings")
        .Select(UnityEditor.AssetDatabase.GUIDToAssetPath).ToArray();
    if (foundPaths.Length == 0) {
      Debug.LogError("Found no PtSettings assets. Re-import Poly Toolkit");
      return null;
    } else {
      if (foundPaths.Length > 1) {
        Debug.LogErrorFormat(
            "Found multiple PtSettings assets; delete them and re-import Poly Toolkit\n{0}",
            string.Join("\n", foundPaths));
      }
      return UnityEditor.AssetDatabase.LoadAssetAtPath<PtSettings>(
          foundPaths[0]);
    }
#else
    // We are not in the editor but somehow Application.isPlaying is false, which shouldn't happen.
    Debug.LogError("Unexpected config: UNITY_EDITOR not defined but Application.isPlaying==false.");
    return null;
#endif
  }

  // One or the other of material and descriptor should be set.
  // If both are set, they will be sanity-checked against each other.
  [Serializable]
  public struct SurfaceShaderMaterial {
    public string shaderUrl;
    #pragma warning disable 0649  // Don't warn about fields that are never assigned to.
    [SerializeField] private Material material;
    [SerializeField] private TiltBrushToolkit.BrushDescriptor descriptor;
    #pragma warning restore 0649

    internal Material Material {
      get {
#if UNITY_EDITOR
        if (material != null && descriptor != null) {
          if (material != descriptor.Material) {
            Debug.LogWarningFormat("{0} has conflicting materials", shaderUrl);
          }
        }
#endif
        if (material != null) {
          return material;
        } else if (descriptor != null) {
          return descriptor.Material;
        } else {
          return null;
        }
      }
    }
  }

  [HideInInspector] public ColorSpace playerColorSpace;

  // IMPORTANT: To make these properties editable by the user, add them to the
  // appropriate tab in PtSettingsEditor.cs.

  // Also, since this class is serializable, any changes to the values should be made
  // directly to the PtSettings asset in the project, *NOT* as default value initializers
  // on this class (because changes to default values won't apply to instances that are
  // deserialized from disk!).

  [Tooltip("Defines which special materials to assign when importing a Blocks object. " +
      "Materials for non-Blocks objects will be automatically created on import.")]
  public SurfaceShaderMaterial[] surfaceShaderMaterials = new SurfaceShaderMaterial[0];

  [Tooltip("Directory where asset files will be saved.")]
  public string assetObjectsPath;

  [Tooltip("Directory where asset source files (GLTF, resources) will be saved.")]
  public string assetSourcesPath;

  [Tooltip("Directory where run-time resources will be saved. Last component MUST be 'Resources'.")]
  public string resourcesPath;

  [Tooltip("The real-world length that corresponds to 1 unit in your scene. This is used to " +
      "scale imported objects.")]
  public LengthWithUnit sceneUnit;

  [Tooltip("Default import options.")]
  public EditTimeImportOptions defaultImportOptions;

  public TiltBrushToolkit.BrushManifest brushManifest;

  [Tooltip("Base PBR opaque material to use when importing assets.")]
  [FormerlySerializedAs("basePbrMaterial")]
  public Material basePbrOpaqueDoubleSidedMaterial;

  [Tooltip("Base PBR transparent material to use when importing assets.")]
  [FormerlySerializedAs("basePbrTransparentMaterial")]
  public Material basePbrBlendDoubleSidedMaterial;


  [Tooltip("Authentication settings.")]
  public PolyAuthConfig authConfig;
  [Tooltip("Cache settings.")]
  public PolyCacheConfig cacheConfig;

  [Tooltip("If true, sends anonymous usage data (editor only).")]
  public bool sendEditorAnalytics;

  [Header("Warnings")]
  [Tooltip("If true, warn before overwriting asset source folders. If you never make changes " +
  "(or add extra files) to asset source folders, you can safely disable this.")]
  public bool warnOnSourceOverwrite;

  [Tooltip("Warn when an incompatible API compatibility level is active.")]
  public bool warnOfApiCompatibility;

  private Dictionary<string, Material> surfaceShaderMaterialLookup;

  /// <returns>null if not found</returns>
  public Material LookupSurfaceShaderMaterial(string url) {
    if (surfaceShaderMaterialLookup == null) {
      surfaceShaderMaterialLookup = surfaceShaderMaterials.ToDictionary(
          elt => elt.shaderUrl, elt => elt.Material);
    }
    Material ret;
    surfaceShaderMaterialLookup.TryGetValue(url, out ret);
    return ret;
  }
}

}
