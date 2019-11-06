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
using System.Collections.Generic;
using UnityEngine;
using PolyToolkitInternal;
using System.Collections;

namespace PolyToolkit {
  /// <summary>
  /// Represents a Poly asset (the information about a 3D object in Poly).
  /// </summary>
  /// <remarks>
  /// This is not the actual object that is added to the scene. This is just a container for
  /// the object's data, from which a GameObject can eventually be constructed.
  /// </remarks>
  [AutoStringifiable]
  public class PolyAsset {
    /// <summary>
    /// Format of the URL to a particular asset, given its ID.
    /// </summary>
    private const string URL_FORMAT = "https://poly.google.com/view/{0}";
    /// <summary>
    /// Identifier for the asset. This is an alphanumeric string that identifies the asset,
    /// but is not meant for display. For example, "assets/L1o2e3m4I5p6s7u8m".
    /// </summary>
    public string name;
    /// <summary>
    /// Human-readable name of the asset.
    /// </summary>
    public string displayName;
    /// <summary>
    /// Name of the asset's author.
    /// </summary>
    public string authorName;
    /// <summary>
    /// Human-readable description of the asset.
    /// </summary>
    public string description;
    /// <summary>
    /// Date and time when the asset was created.
    /// </summary>
    public DateTime createTime;
    /// <summary>
    /// Date and time when the asset was last updated.
    /// </summary>
    public DateTime updateTime;
    /// <summary>
    /// A list of the available formats for this asset. Each format describes a content-type of a
    /// representation of the asset, and specifies where the underlying data files can be found.
    /// </summary>
    public List<PolyFormat> formats = new List<PolyFormat>();
    /// <summary>
    /// Thumbnail image information for this asset.
    /// </summary>
    public PolyFile thumbnail;
    /// <summary>
    /// The license under which the author has made this asset available for use, if any.
    /// </summary>
    public PolyAssetLicense license;
    /// <summary>
    /// Visibility of this asset (who can access it).
    /// </summary>
    public PolyVisibility visibility;
    /// <summary>
    /// If true, the asset was manually curated by the Poly team.
    /// </summary>
    public bool isCurated;

    /// <summary>
    /// The texture with the asset's thumbnail image. Only available after successfully fetched.
    /// </summary>
    public Texture2D thumbnailTexture;

    /// <summary>
    /// Returns a PolyFormat of the given type, if it exists.
    /// If the asset has more than one format of the given type, returns the first one seen.
    /// If the asset does not have a format of the given type, returns null.
    /// </summary>
    public PolyFormat GetFormatIfExists(PolyFormatType type) {
      foreach (PolyFormat format in formats) {
        if (format == null) {
          continue;
        }
        if (format.formatType == type) return format;
      }
      return null;
    }

    /// <summary>
    /// Returns whether the asset is known to be mutable, due to its visibility.
    /// Public and unlisted assets are immutable. Private assets are mutable.
    /// </summary>
    /// <remarks>
    /// Immutable assets can be cached indefinitely, since they can't be modified.
    /// Depending on your use-case, you may wish to frequently re-download mutable assets, if you expect them to be
    /// changed while your app is running.
    /// </remarks>
    public bool IsMutable {
      get {
        return visibility == PolyVisibility.PRIVATE || visibility == PolyVisibility.UNSPECIFIED;
      }
    }

    /// <summary>
    /// Returns the Poly url of the asset.
    /// </summary>
    public string Url {
      get {
        return string.Format(URL_FORMAT, name.Replace("assets/", ""));
      }
    }

    /// <summary>
    /// Returns attribution information about the asset.
    /// </summary>
    public string AttributionInfo {
      get {
        return AttributionGeneration.GenerateAttributionString(displayName, authorName, Url,
          license == PolyAssetLicense.CREATIVE_COMMONS_BY ? AttributionGeneration.CC_BY_LICENSE
          : "All Rights Reserved");
      }
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// A specific representation of an asset, containing all the information needed to retrieve and
  /// describe this representation.
  /// </summary>
  /// <remarks>
  /// Each format is a "package" of files, with one root file and any number of resource files that accompany
  /// it. For example, for the OBJ format, the root file is the OBJ file that contains the asset's geometry
  /// and the corresponding MTL files are resource files.
  /// </remarks>
  [AutoStringifiable]
  public class PolyFormat {
    /// <summary>
    /// Format type (OBJ, GLTF, etc).
    /// </summary>
    public PolyFormatType formatType;
    /// <summary>
    /// The root (main) file for this format.
    /// </summary>
    public PolyFile root;
    /// <summary>
    /// The list of resource (auxiliary) files for this format.
    /// </summary>
    public List<PolyFile> resources = new List<PolyFile>();
    /// <summary>
    /// Complexity of this format.
    /// </summary>
    public PolyFormatComplexity formatComplexity;

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Represents a Poly file.
  /// </summary>
  [AutoStringifiable]
  public class PolyFile {
    /// <summary>
    /// The relative path of the file in the local filesystem when it was uploaded.
    /// For resource files, the path is relative to the root file. This always includes the name fo the
    /// file, and may or may not include a directory path.
    /// </summary>
    public string relativePath;
    /// <summary>
    /// The URL at which the contents of this file can be retrieved.
    /// </summary>
    public string url;
    /// <summary>
    /// The content type of this file. For example, "text/plain".
    /// </summary>
    public string contentType;
    /// <summary>
    /// Binary contents of this file. Only available after fetched.
    /// </summary>
    [AutoStringifyAbridged]
    public byte[] contents;
    /// <summary>
    /// Cached text contents of this file (lazily decoded from binary).
    /// </summary>
    [AutoStringifyAbridged]
    private string text;

    public PolyFile(string relativePath, string url, string contentType) {
      this.relativePath = relativePath;
      this.url = url;
      this.contentType = contentType;
    }

    /// <summary>
    /// Returns the contents of this file as text.
    /// </summary>
    public string Text {
      get {
        if (text == null) text = System.Text.Encoding.UTF8.GetString(contents);
        return text;
      }
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Information on the complexity of a format.
  /// </summary>
  [AutoStringifiable]
  public class PolyFormatComplexity {
    /// <summary>
    /// Approximate number of triangles in the asset's geometry.
    /// </summary>
    public long triangleCount;
    /// <summary>
    /// Hint for the level of detail (LOD) of this format relative to the other formats in this
    /// same asset. 0 is the most detailed version.
    /// </summary>
    public int lodHint;

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

 /// <summary>
 /// Possible format types that can be returned from the Poly REST API.
 /// </summary>
 public enum PolyFormatType {
  UNKNOWN = 0,
  OBJ = 1,
  GLTF = 2,
  GLTF_2 = 3,
  TILT = 4,
  FBX = 5,
 }

 /// <summary>
 /// Possible asset licenses.
 /// </summary>
 public enum PolyAssetLicense {
    /// <summary>
    /// License unknown/unspecified.
    /// </summary>
    UNKNOWN = 0,
    /// <summary>
    /// Creative Commons license.
    /// </summary>
    CREATIVE_COMMONS_BY = 1,
    /// <summary>
    /// All Rights Reserved by author (not licensed).
    /// </summary>
    ALL_RIGHTS_RESERVED = 2,
  }

  /// <summary>
  /// Visibility filters for a PolyListUserAssets request.
  /// </summary>
  public enum PolyVisibilityFilter {
    /// <summary>
    /// No visibility specified. Returns all assets.
    /// </summary>
    UNSPECIFIED = 0,
    /// <summary>
    /// Return only private assets.
    /// </summary>
    PRIVATE = 1,
    /// <summary>
    /// Return only published assets, including unlisted assets.
    /// </summary>
    PUBLISHED = 2,
  }

  /// <summary>
  /// Visibility of a Poly asset.
  /// </summary>
  public enum PolyVisibility {
    /// <summary>
    /// Unknown (and invalid) visibility.
    /// </summary>
    UNSPECIFIED = 0,
    /// <summary>
    /// Only the owner of the asset can access it.
    /// </summary>
    PRIVATE = 1,
    /// <summary>
    /// Read access to anyone who knows the asset ID (link to the asset), but the
    /// logged-in user's unlisted assets are returned in PolyListUserAssets.
    /// </summary>
    UNLISTED = 2,
    /// <summary>
    /// Read access for everyone.
    /// </summary>
    PUBLISHED = 3,
  }

  /// <summary>
  /// Category of a Poly asset.
  /// </summary>
  public enum PolyCategory {
    UNSPECIFIED = 0,
    ANIMALS = 1,
    ARCHITECTURE = 2,
    ART = 3,
    FOOD = 4,
    NATURE = 5,
    OBJECTS = 6,
    PEOPLE = 7,
    PLACES = 8,
    TECH = 9,
    TRANSPORT = 10,
  }

  /// <summary>
  /// How the requested assets should be ordered in the response.
  /// </summary>
  public enum PolyOrderBy {
    BEST,
    NEWEST,
    OLDEST,
    // Liked time is only a valid in a PolyListLikedAssetsRequest.
    LIKED_TIME
  }

  /// <summary>
  /// Options for filtering to return only assets that contain the given format.
  /// </summary>
  public enum PolyFormatFilter {
    BLOCKS = 1,
    FBX = 2,
    GLTF = 3,
    GLTF_2 = 4,
    OBJ = 5,
    TILT = 6,
  }

  /// <summary>
  /// Options for filtering on the maximum complexity of the asset.
  /// </summary>
  public enum PolyMaxComplexityFilter {
    UNSPECIFIED = 0,
    SIMPLE = 1,
    MEDIUM = 2,
    COMPLEX = 3,
  }

  /// <summary>
  /// Base class that all request types derive from.
  /// </summary>
  public abstract class PolyRequest {
    /// <summary>
    /// How to sort the results.
    /// </summary>
    public PolyOrderBy orderBy = PolyOrderBy.NEWEST;
    /// <summary>
    /// Size of each returned page.
    /// </summary>
    public int pageSize = 45;
    /// <summary>
    /// Page continuation token for pagination.
    /// </summary>
    public string pageToken = null;
  }

  /// <summary>
  /// Represents a set of Poly request parameters determining which assets should be returned.
  /// null values mean "don't filter by this parameter".
  /// </summary>
  [AutoStringifiable]
  public class PolyListAssetsRequest : PolyRequest {
    public string keywords = "";
    public bool curated = false;
    /// <summary>
    /// Category can be any of the PolyCategory object categories (e.g. "PolyCategory.ANIMALS").
    /// </summary>
    public PolyCategory category = PolyCategory.UNSPECIFIED;
    public PolyMaxComplexityFilter maxComplexity = PolyMaxComplexityFilter.UNSPECIFIED;
    public PolyFormatFilter? formatFilter = null;

    public PolyListAssetsRequest() {}

    /// <summary>
    /// Returns a ListAssetsRequest that requests the featured assets. This approximates what the
    /// user would see in the Poly main page, but the ordering might be different.
    /// </summary>
    public static PolyListAssetsRequest Featured() {
      PolyListAssetsRequest featured = new PolyListAssetsRequest();
      featured.curated = true;
      featured.orderBy = PolyOrderBy.BEST;
      return featured;
    }

    /// <summary>
    /// Returns a ListAssetsRequest that requests the latest assets. This query is not curated,
    /// so it will return the latest assets regardless of whether they have been reviewed.
    /// If you wish to enable curation, set curated=true on the returned object.
    /// </summary>
    public static PolyListAssetsRequest Latest() {
      PolyListAssetsRequest latest = new PolyListAssetsRequest();
      latest.orderBy = PolyOrderBy.NEWEST;
      return latest;
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }
  
  /// <summary>
  /// Represents a set of Poly request parameters determining which of the user's assets should be returned.
  /// null values mean "don't filter by this parameter".
  /// </summary>
  [AutoStringifiable]
  public class PolyListUserAssetsRequest : PolyRequest {
    public PolyFormatType format = PolyFormatType.UNKNOWN;
    public PolyVisibilityFilter visibility = PolyVisibilityFilter.UNSPECIFIED;
    public PolyFormatFilter? formatFilter = null;

    public PolyListUserAssetsRequest() { }
    
    /// <summary>
    /// Returns a ListUserAssetsRequest that requests the user's latest assets.
    /// </summary>
    public static PolyListUserAssetsRequest MyNewest() { 
      PolyListUserAssetsRequest myNewest = new PolyListUserAssetsRequest();
      myNewest.orderBy = PolyOrderBy.NEWEST;
      return myNewest;
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Represents a set of Poly request parameters determining which liked assets should be returned.
  /// Currently, only requests for the liked assets of the logged in user are supported.
  /// null values mean "don't filter by this parameter".
  /// </summary>
  [AutoStringifiable]
  public class PolyListLikedAssetsRequest : PolyRequest {
    /// <summary>
    // A valid user id. Currently, only the special value 'me', representing the
    // currently-authenticated user is supported. To use 'me', you must pass
    // an OAuth token with the request.
    /// </summary>
    public string name = "me";

    public PolyListLikedAssetsRequest() { }
    
    /// <summary>
    /// Returns a ListUserAssetsRequest that requests the user's most recently liked assets.
    /// </summary>
    public static PolyListLikedAssetsRequest MyLiked() { 
      PolyListLikedAssetsRequest myLiked = new PolyListLikedAssetsRequest();
      myLiked.orderBy = PolyOrderBy.LIKED_TIME;
      return myLiked;
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Represents the status of an operation: success or failure + error message.
  ///
  /// A typical pattern is to return a PolyStatus to indicate the success of an operation, instead of just a bool.
  /// So your code would do something like:
  ///
  /// @{
  /// PolyStatus MyMethod() {
  ///   if (somethingWentWrong) {
  ///     return PolyStatus.Error("Failed to reticulate spline.");
  ///   }
  ///   ...
  ///   return PolyStatus.Success();
  /// }
  /// @}
  ///
  /// You can also chain PolyStatus failures, using one PolyStatus as the cause of another:
  ///
  /// @{
  /// PolyStatus MyMethod() {
  ///   PolyStatus status = TesselateParabolicNonUniformDarkMatterQuantumSuperManifoldWithCheese();
  ///   if (!status.ok) {
  ///     return PolyStatus.Error(status, "Tesselation failure.");
  ///   }
  ///   ...
  ///   return PolyStatus.Success();
  /// }
  /// @}
  ///
  /// Using PolyStatus vs. throwing exceptions: PolyStatus typically represents an "expected" failure, that is,
  /// an operation where failure is common and acceptable. For example, validating user input, consuming some
  /// external file which might or might not be well formatted, sending a web request, etc. For unexpected
  /// failures (logic errors, assumption violations, etc), it's best to use exceptions.
  /// </summary>
  public struct PolyStatus {
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool ok;
    /// <summary>
    /// If the operation failed, this is the error message. This is an error message suitable for
    /// logging, not necessarily a user-friendly message.
    /// </summary>
    public string errorMessage;

    /// <summary>
    /// Creates a new PolyStatus with the given success status and error message.
    /// </summary>
    /// <param name="ok">Whether the operation succeeded.</param>
    /// <param name="errorMessage">The error message (only relevant if ok == false).</param>
    public PolyStatus(bool ok, string errorMessage = "") {
      this.ok = ok;
      this.errorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a new success status.
    /// </summary>
    public static PolyStatus Success() {
      return new PolyStatus(true);
    }

    /// <summary>
    /// Creates a new error status with the given error message.
    /// </summary>
    public static PolyStatus Error(string errorMessage) {
      return new PolyStatus(false, errorMessage);
    }

    /// <summary>
    /// Creates a new error status with the given error message.
    /// </summary>
    public static PolyStatus Error(string format, params object[] args) {
      return new PolyStatus(false, string.Format(format, args));
    }

    /// <summary>
    /// Creates a new error status with the given error message and cause.
    /// The error message will automatically include all error messages in the causal chain.
    /// </summary>
    public static PolyStatus Error(PolyStatus cause, string errorMessage) {
      return new PolyStatus(false, errorMessage + "\nCaused by: " + cause.errorMessage);
    }

    /// <summary>
    /// Creates a new error status with the given error message and cause.
    /// The error message will automatically include all error messages in the causal chain.
    /// </summary>
    public static PolyStatus Error(PolyStatus cause, string format, params object[] args) {
      return new PolyStatus(false, string.Format(format, args) + "\nCaused by: " + cause.errorMessage);
    }

    public override string ToString() {
      return ok ? "OK" : string.Format("ERROR: {0}", errorMessage);
    }
  }

  /// <summary>
  /// A union of a PolyStatus and a type. Used to represent the result of an operation, which can either
  /// be an error (represented as a PolyStatus), or a result object (the parameter type T).
  /// </summary>
  /// <typeparam name="T">The result object.</typeparam>
  public class PolyStatusOr<T> {
    private PolyStatus status;
    private T value;

    /// <summary>
    /// Creates a PolyStatusOr with the given error status.
    /// </summary>
    /// <param name="status">The error status with which to create it.</param>
    public PolyStatusOr(PolyStatus status) {
      if (status.ok) {
        throw new Exception("PolyStatusOr(PolyStatus) can only be used with an error status.");
      }
      this.status = status;
      this.value = default(T);
    }

    /// <summary>
    /// Creates a PolyStatusOr with the given value.
    /// The status will be set to success.
    /// </summary>
    /// <param name="value">The value with which to create it.</param>
    public PolyStatusOr(T value) {
      this.status = PolyStatus.Success();
      this.value = value;
    }

    /// <summary>
    /// Returns the status.
    /// </summary>
    public PolyStatus Status { get { return status; } }

    /// <summary>
    /// Shortcut to Status.ok.
    /// </summary>
    public bool Ok { get { return status.ok; } }

    /// <summary>
    /// Returns the value. The value can only be obtained if the status is successful. If the status
    /// is an error, reading this property will throw an exception.
    /// </summary>
    public T Value {
      get {
        if (!status.ok) {
          throw new Exception("Can't get value from an unsuccessful PolyStatusOr: " + this);
        }
        return value;
      }
    }

    public override string ToString() {
      return string.Format("PolyStatusOr<{0}>: {1}{2}", typeof(T).Name, status,
        status.ok ? (value == null ? "(null)" : value.ToString()) : "");
    }
  }

  /// <summary>
  /// Base class for all result types.
  /// </summary>
  public abstract class PolyBaseResult {
    /// <summary>
    /// The status of the operation (success or failure).
    /// </summary>
    public PolyStatus status;
  }

  /// <summary>
  /// Represents the result of a PolyListAssetsRequest or PolyListUserAssetsRequest.
  /// </summary>
  [AutoStringifiable]
  public class PolyListAssetsResult : PolyBaseResult {
    /// <summary>
    /// A list of assets that match the criteria specified in the request.
    /// </summary>
    public List<PolyAsset> assets;
    /// <summary>
    /// The total number of assets in the list, without pagination.
    /// </summary>
    public int totalSize;
    /// <summary>
    /// The token to retrieve the next page of results, if any.
    /// If there is no next page, this will be null.
    /// </summary>
    public string nextPageToken;

    public PolyListAssetsResult(PolyStatus status, int totalSize = 0, List<PolyAsset> assets = null,
      string nextPageToken = null) {
      this.status = status;
      this.assets = assets;
      this.totalSize = totalSize;
      this.nextPageToken = nextPageToken;
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Represents the result of importing an asset.
  /// </summary>
  public class PolyImportResult {
    /// <summary>
    /// The GameObject representing the imported asset.
    /// </summary>
    public GameObject gameObject;

    /// <summary>
    /// The main thread throttler object, if importing in "throttled" mode. This will be null if not
    /// in throttled mode. Enumerate this on the main thread to gradually perform necessary main
    /// thread operations like creating meshes, textures, etc (see documentation for PolyImportOptions for
    /// more details).
    ///
    /// IMPORTANT: this enumerator is not designed to be used across scene (level) loads. Always finish
    /// enumerating it before loading a new scene.
    /// </summary>
    public IEnumerable mainThreadThrottler;

    public PolyImportResult(GameObject gameObject) {
      this.gameObject = gameObject;
    }
  }

  /// <summary>
  /// Represents the result of fetching files for an asset.
  /// </summary>
  [AutoStringifiable]
  public class PolyFormatTypeFetchResult : PolyBaseResult {
    public PolyAsset asset;

    public PolyFormatTypeFetchResult(PolyStatus status, PolyAsset asset) {
      this.status = status;
      this.asset = asset;
    }

    public override string ToString() {
      return AutoStringify.Stringify(this);
    }
  }

  /// <summary>
  /// Options for fetching a thumbnail.
  /// </summary>
  public class PolyFetchThumbnailOptions {
    /// <summary>
    /// If nonzero, this is the requested thumbnail image size, in pixels. This is the size
    /// of the image's largest dimension (width, for most thumbnails).
    /// This is just a hint that the implementation will try (but is not guaranteed) to honor.
    /// </summary>
    public int requestedImageSize { get; private set; }

    public PolyFetchThumbnailOptions() {}

    public void SetRequestedImageSize(int requestedImageSize) {
      this.requestedImageSize = requestedImageSize;
    }
  }
}
