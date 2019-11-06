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
using System.IO;
using UnityEditor;
using UnityEngine;
using PolyToolkit;
using PolyToolkitInternal;
using System;
using System.Text.RegularExpressions;

namespace PolyToolkitEditor {
/// <summary>
/// Window that allows the user to browse and import Poly models.
///
/// Note that EditorWindow objects are created and deleted when the window is opened or closed, so this
/// object can't be responsible for any background logic like importing assets, etc. It should only deal
/// with UI. For all background logic and and real work, we rely on AssetBrowserManager.
/// </summary>
public class AssetBrowserWindow : EditorWindow {
  /// <summary>
  /// Title of the window (shown in the Unity UI).
  /// </summary>
  private const string WINDOW_TITLE = "Poly Toolkit";

  /// <summary>
  /// URL of the user's profile page.
  /// </summary>
  private const string USER_PROFILE_URL = "https://poly.google.com/user";

  /// <summary>
  /// Width and height of each asset thumbnail image in the grid.
  /// </summary>
  private const int THUMBNAIL_WIDTH = 128;
  private const int THUMBNAIL_HEIGHT = 96;

  /// <summary>
  /// Width of each grid cell in the assets grid.
  /// </summary>
  private const int CELL_WIDTH = THUMBNAIL_WIDTH;

  /// <summary>
  /// Spacing between grid cells in the assets grid.
  /// </summary>
  private const int CELL_SPACING = 5;

  /// <summary>
  /// Height of the title bar at the top of the window.
  /// </summary>
  private const int TITLE_BAR_HEIGHT = 64;

  /// <summary>
  /// Height of the bar that contains the back button.
  /// </summary>
  private const int BACK_BUTTON_BAR_HEIGHT = 28;

  /// <summary>
  /// Height of the title image.
  /// </summary>
  private const int TITLE_IMAGE_HEIGHT = 40;

  /// <summary>
  /// Padding around title image.
  /// </summary>
  private const int TITLE_IMAGE_PADDING = 12;

  /// <summary>
  /// Margin from the top where the UI begins.
  /// This does not account for the back button bar.
  /// </summary>
  private const int TOP_MARGIN_BASE = TITLE_BAR_HEIGHT + 10;

  /// <summary>
  /// Padding around the window.
  /// </summary>
  private const int PADDING = 5;

  /// <summary>
  /// Size of the user's profile picture.
  /// </summary>
  private const int PROFILE_PICTURE_SIZE = 40;

  /// <summary>
  /// Size of the left column (labels), in pixels.
  /// </summary>
  private const int LEFT_COL_WIDTH = 140;

  /// <summary>
  /// Height of the "successfully imported" box.
  /// </summary>
  private const int IMPORT_SUCCESS_BOX_HEIGHT = 120;

  /// <summary>
  /// Texture to use for the title bar.
  /// </summary>
  private const string TITLE_TEX = "Editor/Textures/PolyToolkitTitle.png";
  
  /// <summary>
  /// Texture to use for the back button (back arrow) if the skin is Unity pro.
  /// </summary>
  private const string BACK_ARROW_LIGHT_TEX = "Editor/Textures/BackArrow.png";

  /// <summary>
  /// Texture to use for the back button (back arrow) if the skin is Unity personal.
  /// </summary>
  private const string BACK_ARROW_DARK_TEX = "Editor/Textures/BackArrowDark.png";
  
  /// <summary>
  /// Texture to use for the back button bar background if the skin is Unity pro.
  /// </summary>
  private const string DARK_GREY_TEX = "Editor/Textures/DarkGrey.png";
  
  /// <summary>
  /// Texture to use for the back button bar background if the skin is Unity personal.
  /// </summary>
  private const string LIGHT_GREY_TEX = "Editor/Textures/LightGrey.png";

  /// <summary>
  /// Category key corresponding to selecting the "FEATURED" section.
  /// </summary>
  private const string KEY_FEATURED = "_featured";

  /// <summary>
  /// Category key corresponding to selecting the "Your Uploads" section.
  /// </summary>
  private const string KEY_YOUR_UPLOADS = "_your_uploads";

  /// <summary>
  /// Category key corresponding to selecting the "Your Uploads" section.
  /// </summary>
  private const string KEY_YOUR_LIKES = "_your_likes";

  /// <summary>
  /// Index of the "FEATURED" category below.
  /// </summary>
  private const int CATEGORY_FEATURED = 0;

  /// <summary>
  /// Categories that the user can choose to browse.
  /// </summary>
  private static readonly CategoryInfo[] CATEGORIES = {
    new CategoryInfo(KEY_FEATURED, "Featured", PolyCategory.UNSPECIFIED),
    new CategoryInfo(KEY_YOUR_UPLOADS, "Your Uploads", PolyCategory.UNSPECIFIED),
    new CategoryInfo(KEY_YOUR_LIKES, "Your Likes", PolyCategory.UNSPECIFIED),
    new CategoryInfo("animals", "Animals and Creatures", PolyCategory.ANIMALS),
    new CategoryInfo("architecture", "Architecture", PolyCategory.ARCHITECTURE),
    new CategoryInfo("art", "Art", PolyCategory.ART),
    new CategoryInfo("food", "Food and Drink", PolyCategory.FOOD),
    new CategoryInfo("nature", "Nature", PolyCategory.NATURE),
    new CategoryInfo("objects", "Objects", PolyCategory.OBJECTS),
    new CategoryInfo("people", "People and Characters", PolyCategory.PEOPLE),
    new CategoryInfo("places", "Places and Scenes", PolyCategory.PLACES),
    new CategoryInfo("tech", "Technology", PolyCategory.TECH),
    new CategoryInfo("transport", "Transport", PolyCategory.TRANSPORT),
  };

  /// <summary>
  /// Represent the several modes that the UI can be in.
  /// </summary>
  private enum UiMode {
    // Browsing assets by category/type (default mode).
    BROWSE,
    // Searching for assets by keyword (search box open).
    SEARCH,
    // Viewing the details of a particular asset.
    DETAILS,
  };

  /// <summary>
  /// Current UI mode.
  /// </summary>
  private UiMode mode = UiMode.BROWSE;

  /// <summary>
  /// The previous UI mode.
  /// </summary>
  private UiMode previousMode = UiMode.BROWSE;

  /// <summary>
  /// Index of the category that is currently selected (in CATEGORIES[]).
  /// </summary>
  private int selectedCategory = 0;

  /// <summary>
  /// The search terms the user has typed in the search box.
  /// </summary>
  private string searchTerms = "";

  /// <summary>
  /// The texture to use in place of a thumbnail when loading the thumbnail.
  /// </summary>
  private Texture2D loadingTex = null;
  
  /// <summary>
  /// The texture to use for the back button (back arrow).
  /// </summary>
  private Texture2D backArrowTex = null;
  
  /// <summary>
  /// Texture used for back button bar background.
  /// </summary>
  private Texture2D backBarBackgroundTex = null;

  /// <summary>
  /// Reference to the AssetBrowserManager.
  /// </summary>
  private AssetBrowserManager manager;

  /// <summary>
  /// Current scrolling position of the scroll view with the list of assets.
  /// </summary>
  private Vector2 assetListScrollPos;

  /// <summary>
  /// Current scrolling position of the details window.
  /// </summary>
  private Vector2 detailsScrollPos;

  /// <summary>
  /// If non-null, we're showing the details page for the given asset. If null,
  /// we are showing the grid screen that shows all assets.
  /// </summary>
  private PolyAsset selectedAsset = null;

  /// <summary>
  /// Indicates whether the query we're currently showing requires authentication.
  /// </summary>
  private bool queryRequiresAuth = false;

  /// <summary>
  /// The selected asset path in the "Import" section of the details page.
  /// </summary>
  private string ptAssetLocalPath = "";

  /// <summary>
  /// Current asset type filter (indicates the type of asset that the user wishes to see).
  /// </summary>
  private PolyFormatFilter? assetTypeFilter = null;

  /// <summary>
  /// Texture for the title bar.
  /// </summary>
  private Texture2D titleTex;

  /// <summary>
  /// The style we use for the asset title in the details page.
  /// </summary>
  private GUIStyle detailsTitleStyle;

  /// <summary>
  /// GUI helper that keeps track of our open layouts.
  /// </summary>
  private GUIHelper guiHelper = new GUIHelper();

  /// <summary>
  /// Currently selected import options.
  /// </summary>
  private EditTimeImportOptions importOptions;

  /// <summary>
  /// If true, the currently displayed asset was just imported successfully.
  /// </summary>
  private bool justImported;

  /// <summary>
  /// Shows the browser window.
  /// </summary>
  [MenuItem("Poly/Browse Assets...")]
  public static void BrowsePolyAssets() {
    GetWindow<AssetBrowserWindow>(WINDOW_TITLE, /* focus */ true);
    PtAnalytics.SendEvent(PtAnalytics.Action.MENU_BROWSE_ASSETS);
  }

  /// <summary>
  /// Shows the browser window.
  /// </summary>
  [MenuItem("Window/Poly: Browse Assets...")]
  public static void BrowsePolyAssets2() {
    BrowsePolyAssets();
  }

  /// <summary>
  /// Notifies this window that the given asset was just imported successfully
  /// (so we can show this state in the UI).
  /// </summary>
  /// <param name="assetId">The ID of the asset that was just imported.</param>
  public void HandleAssetImported(string assetId) {
    if (selectedAsset != null && assetId == selectedAsset.name) {
      justImported = true;
    }
  }

  /// <summary>
  /// Performs one-time initialization.
  /// </summary>
  private void Initialize() {
    PtDebug.Log("ABW: initializing.");
    if (manager == null) {
      manager = new AssetBrowserManager();
      manager.SetRefreshCallback(UpdateUi);
    }
    loadingTex = new Texture2D(1,1);
    titleTex = PtUtils.LoadTexture2DFromRelativePath(TITLE_TEX);
    backArrowTex = PtUtils.LoadTexture2DFromRelativePath(
      EditorGUIUtility.isProSkin ? BACK_ARROW_LIGHT_TEX : BACK_ARROW_DARK_TEX);
    backBarBackgroundTex = PtUtils.LoadTexture2DFromRelativePath(
      EditorGUIUtility.isProSkin ? DARK_GREY_TEX : LIGHT_GREY_TEX);

    detailsTitleStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
    detailsTitleStyle.fontSize = 15;
    detailsTitleStyle.fontStyle = FontStyle.Bold;
  }

  private void SetUiMode(UiMode newMode) {
    if (newMode != mode) {
      previousMode = mode;
      mode = newMode;
    }
    PtDebug.Log("ABW: changed UI mode to " + newMode);
  }

  /// <summary>
  /// Renders the window GUI (invoked by Unity).
  /// </summary>
  public void OnGUI() {
    if (Application.isPlaying) {
      DrawTitleBar(/* withSignInUi */ false);
      GUILayout.Space(TOP_MARGIN_BASE);
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      GUILayout.Label("(This window doesn't work in Play mode)", EditorStyles.wordWrappedLabel);
      GUILayout.FlexibleSpace();
      guiHelper.EndHorizontal();
      return;
    }

    if (manager == null) {
      // Initialize, if we haven't yet. We delay this to OnGUI instead of earlier because we need
      // the Unity GUI system to be completely initialized (so we can create styles, for instance), which
      // only happens on OnGUI().
      PolyRequest request = BuildRequest();
      manager = new AssetBrowserManager(request);
      manager.SetRefreshCallback(UpdateUi);
      Initialize();
    }

    // We have to check if Poly is ready every time (it's cheap to check). This is because Poly can be
    // unloaded and wiped every time we enter or exit play mode.
    manager.EnsurePolyIsReady();

    int topMargin;
    if (!DrawHeader(out topMargin)) {
      // Abort rendering because a button was pressed (for example, the back button).
      return;
    }

    guiHelper.BeginArea(new Rect(PADDING, PADDING, position.width - 2 * PADDING, position.height - 2 * PADDING));
    GUILayout.Space(topMargin);

    switch (mode) {
      case UiMode.BROWSE:
        DrawBrowseUi();
        break;
      case UiMode.SEARCH:
        DrawSearchUi();
        break;
      case UiMode.DETAILS:
        DrawDetailsUi();
        break;
      default:
        throw new System.Exception("Invalid UI mode: " + mode);
    }

    guiHelper.EndArea();
    guiHelper.FinishAndCheck();
  }

  /// <summary>
  /// Draws the header and handles header-related events (like the back button).
  /// </summary>
  /// <param name="topMargin">(Out param). The top margin at which the rest of the content
  /// should be rendered. Only valid if this method returns true.</param>
  /// <returns>True if the header was successfully drawn and rendering should continue.
  /// False if it was aborted due to a back button press.</returns>
  private bool DrawHeader(out int topMargin) {
    DrawTitleBar(withSignInUi: true);
    bool hasBackButtonBar = HasBackButtonBar();
    topMargin = TOP_MARGIN_BASE;
    if (hasBackButtonBar) {
      bool backButtonClicked = DrawBackButtonBar();
      if (backButtonClicked) {
        HandleBackButton();
        return false;
      }
      // Increase the top margin of the content to account for the back button bar.
      topMargin += BACK_BUTTON_BAR_HEIGHT;
    }
    return true;
  }

  /// <summary>
  /// Draws the title bar at the top of the window.
  /// The title bar includes the user's profile picture and the Sign In/Out UI.
  /// <param name="withSignInUi">If true, also include the sign in/sign out UI.</param>
  /// </summary>
  private void DrawTitleBar(bool withSignInUi) {
    GUI.DrawTexture(new Rect(0, 0, position.width, TITLE_BAR_HEIGHT), Texture2D.whiteTexture);
    
    GUIStyle titleStyle = new GUIStyle (GUI.skin.label); 
    titleStyle.margin = new RectOffset(TITLE_IMAGE_PADDING, TITLE_IMAGE_PADDING, TITLE_IMAGE_PADDING,
      TITLE_IMAGE_PADDING);
    if (GUILayout.Button(titleTex, titleStyle,
      GUILayout.Width(titleTex.width * TITLE_IMAGE_HEIGHT / titleTex.height),
      GUILayout.Height(TITLE_IMAGE_HEIGHT))) {
      // Clicked title image. Return to the featured assets page.
      SetUiMode(UiMode.BROWSE);
      selectedCategory = CATEGORY_FEATURED;
      StartRequest();
    }

    if (!withSignInUi) return;

    guiHelper.BeginArea(new Rect(TITLE_IMAGE_PADDING, TITLE_IMAGE_PADDING,
        position.width - 2 * TITLE_IMAGE_PADDING, TITLE_BAR_HEIGHT));

    if (PolyApi.IsAuthenticated) {
      // User is authenticated, so show the profie picture.
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      Texture2D userTex = PolyApi.UserIcon != null && PolyApi.UserIcon.texture != null ?
        PolyApi.UserIcon.texture : loadingTex;
      if (GUILayout.Button(new GUIContent(userTex, /* tooltip */ PolyApi.UserName),
          GUIStyle.none, GUILayout.Width(PROFILE_PICTURE_SIZE), GUILayout.Height(PROFILE_PICTURE_SIZE))) {
        // Clicked profile picture. Show the dropdown menu.
        ShowProfileDropdownMenu();
      }
      guiHelper.EndHorizontal();
    } else if (PolyApi.IsAuthenticating) {
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      GUILayout.Label("Signing in... Please wait.");
      guiHelper.EndHorizontal();
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      bool cancelSignInClicked = GUILayout.Button("Cancel", EditorStyles.miniButton);
      guiHelper.EndHorizontal();
      if (cancelSignInClicked) {
        PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_SIGN_IN_CANCEL);
        manager.CancelSignIn();
      }
    } else {
      // Not signed in. Show "Sign In" button.
      GUILayout.Space(30);
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Sign in")) {
        PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_SIGN_IN_START);
        manager.LaunchSignInFlow();
      }
      guiHelper.EndHorizontal();
    }
    guiHelper.EndArea();
  }

  /// <summary>
  /// Draws the "browse" UI. The main UI allows the user to select a category to browse and set filters,
  /// and allows them to browse the assets grid. When they click on an asset, they go to the details UI.
  /// </summary>
  private void DrawBrowseUi() {
    guiHelper.BeginHorizontal();
    GUILayout.FlexibleSpace();
    bool searchClicked = GUILayout.Button("Search...");
    guiHelper.EndHorizontal();

    if (searchClicked) {
      SetUiMode(UiMode.SEARCH);
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_SEARCH_CLICKED);
      manager.ClearRequest();
      return;
    }

    guiHelper.BeginHorizontal();

    // Draw the category dropdowns.
    GUILayout.Label("Show:", GUILayout.Width(LEFT_COL_WIDTH));
    if (EditorGUILayout.DropdownButton(new GUIContent(CATEGORIES[selectedCategory].title),
        FocusType.Keyboard)) {
      GenericMenu menu = new GenericMenu();
      for (int i = 0; i < CATEGORIES.Length; i++) {
        if (i == 3) menu.AddSeparator("");
        menu.AddItem(new GUIContent(CATEGORIES[i].title), i == selectedCategory, DropdownMenuCallback, i);
      }
      menu.ShowAsContext();
    }
    guiHelper.EndHorizontal();
    
    // Draw the "Asset type" toggles.
    bool showAssetTypeFilter = (CATEGORIES[selectedCategory].key != KEY_YOUR_LIKES);

    if (showAssetTypeFilter) {
      guiHelper.BeginHorizontal();
      GUILayout.Label("Asset type:", GUILayout.Width(LEFT_COL_WIDTH));
      bool blocksToggle = GUILayout.Toggle(assetTypeFilter == PolyFormatFilter.BLOCKS, "Blocks", "Button");
      bool tiltBrushToggle = GUILayout.Toggle(assetTypeFilter == PolyFormatFilter.TILT, "Tilt Brush", "Button");
      bool allToggle = GUILayout.Toggle(assetTypeFilter == null, "All", "Button");
      guiHelper.EndHorizontal();
      GUILayout.Space(10);
      if (blocksToggle && assetTypeFilter != PolyFormatFilter.BLOCKS) {
        assetTypeFilter = PolyFormatFilter.BLOCKS;
        PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_ASSET_TYPE_SELECTED, assetTypeFilter.ToString());
        StartRequest();
        return;
      } else if (tiltBrushToggle && assetTypeFilter != PolyFormatFilter.TILT) {
        assetTypeFilter = PolyFormatFilter.TILT;
        PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_ASSET_TYPE_SELECTED, assetTypeFilter.ToString());
        StartRequest();
        return;
      } else if (allToggle && assetTypeFilter != null) {
        assetTypeFilter = null;
        PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_ASSET_TYPE_SELECTED, assetTypeFilter.ToString());
        StartRequest();
        return;
      }
    }

    DrawResultsGrid();
  }

  private void HandleBackButton() {
    switch (mode) {
      case UiMode.SEARCH:
        SetUiMode(UiMode.BROWSE);
        StartRequest();
        return;
      case UiMode.DETAILS:
        selectedAsset = null;
        justImported = false;
        manager.ClearCurrentAssetResult();
        SetUiMode(previousMode);
        return;
      default:
        throw new Exception("Invalid UI mode for back button: " + mode);
    }
  }

  private void DrawSearchUi() {
    bool searchClicked = false;

    // Pressing ENTER in the search terms box is the same as clicking "Search". We have to check
    // this BEFORE we draw the text field, otherwise it will consume the event and we won't see it.
    if (Event.current != null && Event.current.type == EventType.KeyDown &&
        Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "searchTerms") {
      searchClicked = true;
    }

    GUILayout.Space(10);
    GUILayout.Label("Search", EditorStyles.boldLabel);
    GUILayout.Label("Enter search terms (or a Poly URL) below:", EditorStyles.wordWrappedLabel);
    GUI.SetNextControlName("searchTerms");
    searchTerms = EditorGUILayout.TextField(searchTerms, EditorStyles.textArea);
    guiHelper.BeginHorizontal();
    GUILayout.FlexibleSpace();
    searchClicked = GUILayout.Button("Search") || searchClicked;
    guiHelper.EndHorizontal();

    if (searchClicked && searchTerms.Trim().Length > 0) {
      // Note: for privacy reasons we don't log the search terms, just the fact that a search was made.
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_SEARCHED);
      
      string assetId;
      if (SearchTermIsAssetPage(searchTerms, out assetId)) {
        manager.StartRequestForSpecificAsset(assetId);
        SetUiMode(UiMode.DETAILS);
        return;
      }

      StartRequest();
    }
    DrawResultsGrid();
  }

  /// <summary>
  /// Returns true if the url was a valid asset url, false if not.
  /// </summary>
  /// <param name="url">The url of the asset to get.</param>
  /// <param name="assetId">The id of the asset to get from the url, null if the url is invalid.</param>
  private bool SearchTermIsAssetPage(string url, out string assetId) {
    Regex regex = new Regex("^(http[s]?:\\/\\/)?.*\\/view\\/([^?]+)");
    Match match = regex.Match(url);
    if (match.Success) {
      assetId = match.Groups[2].ToString();
      assetId = assetId.Trim();
      return true;
    }

    assetId = null;
    return false;
  }

  /// <summary>
  /// Draws the query results grid, using the current query results as reported by
  /// the AssetsBrowserManager.
  /// </summary>
  private void DrawResultsGrid() {
    if (manager.IsQuerying) {
      GUILayout.Space(30);
      GUILayout.Label("Fetching assets. Please wait...");
      return;
    }

    if (manager.CurrentResult == null) {
      return; 
    }

    if (manager.CurrentResult != null && !manager.CurrentResult.Status.ok) {
      GUILayout.Space(30);
      GUILayout.Label("There was a problem with that request! Please try again later.",
        EditorStyles.wordWrappedLabel);

      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Retry")) {
        // Retry the previous request.
        manager.ClearCaches();
        StartRequest();
      }
      GUILayout.FlexibleSpace();
      guiHelper.EndHorizontal();

      return;
    }
    
    if (manager.CurrentResult.Value.assets == null ||
        manager.CurrentResult.Value.assets.Count == 0) {
      GUILayout.Space(30);
      GUILayout.Label("No results.");
      return;
    }

    PolyListAssetsResult result = manager.CurrentResult.Value;
    if (!result.status.ok) {
      GUILayout.Space(30);
      GUILayout.Label("*** ERROR fetching results. Try again..");
      return;
    }

    guiHelper.BeginHorizontal();
    string resultCountLabel;
    resultCountLabel = string.Format("{0} assets found.", result.totalSize);
    if (result.assets.Count < result.totalSize) {
      resultCountLabel += string.Format(" (showing 1-{0}).", result.assets.Count);
    }
    GUILayout.Label(resultCountLabel, EditorStyles.miniLabel);
    GUILayout.FlexibleSpace();
    bool refreshClicked = GUILayout.Button("Refresh");
    guiHelper.EndHorizontal();
    GUILayout.Space(5);

    if (refreshClicked) {
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_REFRESH_CLICKED);
      manager.ClearCaches();
      StartRequest();
      return;
    }

    // Calculate how many assets we want to show on each row, based on the window width.
    // This allows the user to dynamically resize the window and have the assets grid
    // automatically reflow.
    int assetsPerRow = Mathf.Clamp(
      Mathf.FloorToInt((position.width - 40) / (CELL_WIDTH + CELL_SPACING)), 1, 8);
    int assetsThisRow = 0;

    // The assets grid is in a scroll view.
    assetListScrollPos = guiHelper.BeginScrollView(assetListScrollPos);
    GUILayout.Space(5);
    guiHelper.BeginVertical();
    guiHelper.BeginHorizontal();

    PolyAsset clickedAsset = null;
    foreach (PolyAsset asset in result.assets) {
      if (assetsThisRow >= assetsPerRow) {
        // Begin new row.
        guiHelper.EndHorizontal();
        GUILayout.Space(20);
        guiHelper.BeginHorizontal();
        assetsThisRow = 0;
      }

      if (assetsThisRow > 0) GUILayout.Space(CELL_SPACING);
      guiHelper.BeginVertical();
      if (GUILayout.Button(asset.thumbnailTexture ?? loadingTex,
          GUILayout.Width(THUMBNAIL_WIDTH), GUILayout.Height(THUMBNAIL_HEIGHT))) {
        clickedAsset = asset;
      }
      GUILayout.Label(asset.displayName, EditorStyles.boldLabel, GUILayout.Width(CELL_WIDTH));
      GUILayout.Label(asset.authorName, GUILayout.Width(CELL_WIDTH));
      guiHelper.EndVertical();
      assetsThisRow++;
    }

    // Complete last row with dummy cells.
    if (assetsThisRow > 0) {
      while (assetsThisRow < assetsPerRow) {
        guiHelper.BeginVertical();
        GUILayout.Label("", GUILayout.Width(CELL_WIDTH), GUILayout.Height(THUMBNAIL_HEIGHT));
        GUILayout.Label("", EditorStyles.boldLabel, GUILayout.Width(CELL_WIDTH));
        GUILayout.Label("", GUILayout.Width(CELL_WIDTH));
        guiHelper.EndVertical();
        assetsThisRow++;
      }
    }

    guiHelper.EndHorizontal();
    GUILayout.Space(10);
    
    bool loadMoreClicked = false;
    if (manager.resultHasMorePages) {
      // If the current response has at least another page of results left, show the load more button.
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      loadMoreClicked = GUILayout.Button("Load more");
      GUILayout.FlexibleSpace();
      guiHelper.EndHorizontal();
    }
    GUILayout.Space(5);

    guiHelper.EndVertical();
    guiHelper.EndScrollView();
    
    if (loadMoreClicked) {
      manager.GetNextPageRequest();
      return;
    }

    if (clickedAsset != null) {
      PrepareDetailsUi(clickedAsset);
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_ASSET_DETAILS_CLICKED,
          GetAssetFormatDescription(selectedAsset));
      SetUiMode(UiMode.DETAILS);
    }
  }

  /// <summary>
  /// Called as a callback by AssetBrowserManager whenever something interesting changes which
  /// requires us to update our UI.
  /// </summary>
  private void UpdateUi() {
    // Tell Unity to repaint this window, which will invoke OnGUI().
    Repaint();
  }

  /// <summary>
  /// Returns whether or not the given category requires authentication.
  /// </summary>
  private bool CategoryRequiresAuth(int category) {
    string key = CATEGORIES[category].key;
    return (key == KEY_YOUR_UPLOADS || key == KEY_YOUR_LIKES);
  }

  /// <summary>
  /// Shows the profile dropdown menu (the dropdown menu that appears when the user clicks their profile
  /// picture).
  /// </summary>
  private void ShowProfileDropdownMenu() {
    GenericMenu menu = new GenericMenu();
    menu.AddItem(new GUIContent("My Profile (web)"), /* on */ false, () => {
      PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_VIEW_PROFILE);
      Application.OpenURL(USER_PROFILE_URL);
    });
    menu.AddSeparator("");
    menu.AddItem(new GUIContent("Sign Out"), /* on */ false, () => {
      PolyApi.SignOut();
      PtAnalytics.SendEvent(PtAnalytics.Action.ACCOUNT_SIGN_OUT);
      // If the user was viewing a category that requires sign in, reset back to the home page.
      if (queryRequiresAuth) {
        selectedCategory = CATEGORY_FEATURED;
        StartRequest();
      }
    });
    menu.ShowAsContext();
  }

  /// <summary>
  /// Callback invoked when the user picks a new category from the category dropdown.
  /// </summary>
  /// <param name="userData">The index of the selected category.</param>
  private void DropdownMenuCallback(object userData) {
    int selection = (int)userData;
    if (selection == selectedCategory) return;

    // If the user picked a category that requires authentication, and they are not authenticated,
    // tell them why they can't view it.
    if (CategoryRequiresAuth(selection) && !PolyApi.IsAuthenticated) {
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_MISSING_AUTH);
      EditorUtility.DisplayDialog("Sign in required",
          "To view your uploads or likes, you must sign in first.", "OK");
      return;
    }

    selectedCategory = (int)userData;
    PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_CATEGORY_SELECTED, CATEGORIES[selectedCategory].key);

    StartRequest();
  }

  /// <summary>
  /// Builds and returns a PolyRequest from the current state of the AssetBrowswerWindow variables.
  /// </summary>
  private PolyRequest BuildRequest() {
    CategoryInfo info = CATEGORIES[selectedCategory];

    if (info.key == KEY_YOUR_UPLOADS) {
      PolyListUserAssetsRequest listUserAssetsRequest = PolyListUserAssetsRequest.MyNewest();
      listUserAssetsRequest.formatFilter = assetTypeFilter;
      return listUserAssetsRequest;
    }

    if (info.key == KEY_YOUR_LIKES) {
      PolyListLikedAssetsRequest listLikedAssetsRequest = PolyListLikedAssetsRequest.MyLiked();
      return listLikedAssetsRequest;
    }

    PolyListAssetsRequest listAssetsRequest;
    if (info.key == KEY_FEATURED) {
      listAssetsRequest = PolyListAssetsRequest.Featured();
    } else {
      listAssetsRequest = new PolyListAssetsRequest();
      listAssetsRequest.category = info.polyCategory;
    }

    listAssetsRequest.formatFilter = assetTypeFilter;
    // Only show curated results.
    listAssetsRequest.curated = true;
    return listAssetsRequest;
  }

  /// <summary>
  /// Sends a list assets request to the assets service according to the parameters currently selected in the UI.
  /// When the request is done, AssetBrowserManager will inform us through the callback.
  /// </summary>
  private void StartRequest() {
   if (mode == UiMode.BROWSE) {
    PolyRequest request = BuildRequest();
    queryRequiresAuth = CategoryRequiresAuth(selectedCategory);
    if (!queryRequiresAuth || PolyApi.IsAuthenticated) {
      manager.StartRequest(request);
    }
   } else if (mode == UiMode.SEARCH) {
    PolyListAssetsRequest request = new PolyListAssetsRequest();
    request.keywords = searchTerms; 
    manager.StartRequest(request);
   } else {
    throw new System.Exception("Unexpected UI mode for StartQuery: " + mode);
   }
   // Reset scroll bar position.
   assetListScrollPos = Vector2.zero;
  }

  /// <summary>
  /// Set the variables of the details ui page for the newly selected asset.
  /// </summary>
  private void PrepareDetailsUi(PolyAsset newSelectedAsset) {
    selectedAsset = newSelectedAsset;
    ptAssetLocalPath = PtUtils.GetDefaultPtAssetPath(selectedAsset);
    detailsScrollPos = Vector2.zero;
    importOptions = PtSettings.Instance.defaultImportOptions;
  }

  /// <summary>
  /// Draws the asset details page. This is the page that shows the details about the selected asset
  /// and allows the user to click to import it.
  /// </summary>
  private void DrawDetailsUi() {
    if (manager.IsQuerying) {
      // Check if manager is querying for a specific asset.
      GUILayout.Space(30);
      GUILayout.Label("Fetching asset. Please wait...");
      selectedAsset = null;
      return;
    }

    // If we just got the specific asset result, set the details ui import options to the relevant
    // variables at first.
    if (manager.CurrentAssetResult != null && selectedAsset == null) {
      PrepareDetailsUi(manager.CurrentAssetResult);
    }

    // Check if the user selected something in the PtAsset picker.
    if (Event.current != null && Event.current.commandName == "ObjectSelectorUpdated") {
      UnityEngine.Object picked = EditorGUIUtility.GetObjectPickerObject();
      ptAssetLocalPath = (picked != null && picked is PtAsset) ?
          AssetDatabase.GetAssetPath(picked) :
          PtUtils.GetDefaultPtAssetPath(selectedAsset);
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_LOCATION_CHANGED);
    }

    if (selectedAsset == null) {
      manager.ClearCurrentAssetResult();
      GUILayout.Space(20);
      GUILayout.Label("Asset not found.");
      return;
    }

    detailsScrollPos = guiHelper.BeginScrollView(detailsScrollPos);
    const float width = 150;
    
    guiHelper.BeginHorizontal();
    GUILayout.Label(selectedAsset.displayName, detailsTitleStyle);
    guiHelper.EndHorizontal();

    guiHelper.BeginHorizontal();
    GUILayout.Label(selectedAsset.authorName, EditorStyles.wordWrappedLabel);
    
    GUILayout.FlexibleSpace();
    if (GUILayout.Button("View on Web", GUILayout.MaxWidth(100))) {
      PtAnalytics.SendEvent(PtAnalytics.Action.BROWSE_VIEW_ON_WEB);
      Application.OpenURL(selectedAsset.Url);
    }
    GUILayout.Space(5);
    guiHelper.EndHorizontal();

    GUILayout.Space(10);

    Texture2D image = selectedAsset.thumbnailTexture ?? loadingTex;
    float displayWidth = position.width - 40;
    float displayHeight = image.height * displayWidth / image.width;
    displayHeight = Mathf.Min(image.height, displayHeight);
    displayWidth = Mathf.Min(image.width, displayWidth);
    GUILayout.Label(image, GUILayout.Width(displayWidth), GUILayout.Height(displayHeight));

    if (manager.IsDownloadingAsset(selectedAsset)) {
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      GUILayout.Label("Downloading asset. Please wait...");
      GUILayout.FlexibleSpace();
      guiHelper.EndHorizontal();
      guiHelper.EndScrollView();
      return;
    } else if (justImported) {
      Rect lastRect = GUILayoutUtility.GetLastRect();
      Rect boxRect = new Rect(PADDING, lastRect.yMax + PADDING, position.width - 2 * PADDING,
          IMPORT_SUCCESS_BOX_HEIGHT);
      GUI.DrawTexture(boxRect, backBarBackgroundTex);
      GUILayout.Space(20);
      guiHelper.BeginHorizontal();
      GUILayout.FlexibleSpace();
      guiHelper.BeginVertical();
      GUILayout.Label("Asset successfully imported.");
      GUILayout.Label("Saved to: " + PtSettings.Instance.assetObjectsPath);
      GUILayout.Space(10);
      if (GUILayout.Button("OK")) {
        // Dismiss.
        justImported = false;
      }
      guiHelper.EndVertical();
      GUILayout.FlexibleSpace();
      guiHelper.EndHorizontal();
      guiHelper.EndScrollView();
      return;
    }
    
    GUILayout.Space(5);
    GUILayout.Label("Import Options", EditorStyles.boldLabel);
    GUILayout.Space(5);

    guiHelper.BeginHorizontal();
    GUILayout.Space(12);
    GUILayout.Label("Import Location", GUILayout.Width(width));
    GUILayout.FlexibleSpace();
    GUILayout.Label(ptAssetLocalPath, EditorStyles.wordWrappedLabel);
    GUILayout.Space(5);
    guiHelper.EndHorizontal();

    guiHelper.BeginHorizontal();
    GUILayout.Space(width - 10);
    GUILayout.FlexibleSpace();
    if (GUILayout.Button("Replace existing...")) {
      PtAsset current = AssetDatabase.LoadAssetAtPath<PtAsset>(ptAssetLocalPath);
      EditorGUIUtility.ShowObjectPicker<PtAsset>(current, /* allowSceneObjects */ false, PtAsset.FilterString, 0);
    }
    GUILayout.Space(5);
    guiHelper.EndHorizontal();

    EditTimeImportOptions oldOptions = importOptions;
    importOptions = ImportOptionsGui.ImportOptionsField(importOptions);
    if (oldOptions.alsoInstantiate != importOptions.alsoInstantiate) {
      // Persist 'always instantiate' option if it was changed.
      PtSettings.Instance.defaultImportOptions.alsoInstantiate = importOptions.alsoInstantiate;
    }
    SendImportOptionMutationAnalytics(oldOptions, importOptions);
    
    GUILayout.Space(10);
    GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
    GUILayout.Space(10);

    guiHelper.BeginHorizontal();
    GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
    buttonStyle.fontStyle = FontStyle.Bold;
    buttonStyle.padding = new RectOffset(10, 10, 8, 8);
    bool importButtonClicked = GUILayout.Button("Import into Project", buttonStyle);
    GUILayout.FlexibleSpace();
    guiHelper.EndHorizontal();
    GUILayout.Space(5);
    
    if (!File.Exists(PtUtils.ToAbsolutePath(ptAssetLocalPath))) {
      GUILayout.Label(PolyInternalUtils.ATTRIBUTION_NOTICE, EditorStyles.wordWrappedLabel);
    } else {
      GUILayout.Label("WARNING: The indicated asset already exists and will be OVERWRITTEN by " +
        "the new asset. Existing instances of the old asset will be automatically updated " +
        "to the new asset.",
        EditorStyles.wordWrappedLabel);
    }
    guiHelper.EndScrollView();

    if (importButtonClicked) {
      if (!ptAssetLocalPath.StartsWith("Assets/") || !ptAssetLocalPath.EndsWith(".asset")) {
        EditorUtility.DisplayDialog("Invalid import path",
          "The import path must begin with Assets/ and have the .asset extension.", "OK");
        return;
      }
      string errorString;
      if (!ValidateImportOptions(importOptions.baseOptions, out errorString)) {
        EditorUtility.DisplayDialog("Invalid import options", errorString, "OK");
        return;
      }
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_STARTED, GetAssetFormatDescription(selectedAsset));
      if (previousMode == UiMode.SEARCH) {
        PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_STARTED_FROM_SEARCH,
            GetAssetFormatDescription(selectedAsset));
      }

      manager.StartDownloadAndImport(selectedAsset, ptAssetLocalPath, importOptions);
    }
  }

  /// <summary>
  /// Returns whether the import options are valid and, if not, a relevant error message to display.
  /// </summary>
  private bool ValidateImportOptions(PolyImportOptions options, out string errorString) {
    switch (options.rescalingMode) {
      case PolyImportOptions.RescalingMode.CONVERT:
        if (options.scaleFactor == 0.0f) {
          errorString = "Scale factor must not be 0.";
          return false;
        }
        break;
      case PolyImportOptions.RescalingMode.FIT:
        if (options.desiredSize == 0.0f) {
          errorString = "Desired size must not be 0.";
          return false;
        }
        break;
      default:
        throw new System.Exception("Import options must hvae a valid rescaling mode");
    }
    errorString = null;
    return true;
  }

  /// <summary>
  /// Returns true if the current UI mode has a back button bar.
  /// </summary>
  /// <returns>True if the current UI mode has a back button bar, false otherwise.</returns>
  private bool HasBackButtonBar() {
    return mode == UiMode.DETAILS || mode == UiMode.SEARCH;
  }

  /// <summary>
  /// Draws the "Back" button bar.
  /// </summary>
  /// <returns>True if the back button was clicked, false otherwise.</returns>
  private bool DrawBackButtonBar() {
    guiHelper.BeginArea(new Rect(0, TITLE_BAR_HEIGHT, position.width, BACK_BUTTON_BAR_HEIGHT));
    GUI.DrawTexture(new Rect(0, 0, position.width, BACK_BUTTON_BAR_HEIGHT), backBarBackgroundTex);
    guiHelper.BeginHorizontal();
    GUILayout.Space(PADDING);
    bool backButtonClicked = GUILayout.Button(new GUIContent("Back", backArrowTex), EditorStyles.miniLabel);
    GUILayout.FlexibleSpace();
    guiHelper.EndHorizontal();
    guiHelper.EndArea();
    return backButtonClicked;
  }

  /// <summary>
  /// Returns a string with all the asset formats of the given asset.
  /// </summary>
  /// <param name="asset">The asset.</param>
  /// <returns>A string with all the comma-separated asset formats of the given asset.</returns>
  private string GetAssetFormatDescription(PolyAsset asset) {
    List<string> formatTypes = new List<string>();
    if (asset.formats != null) {
      foreach (PolyFormat format in asset.formats) {
        formatTypes.Add(format.formatType.ToString());
      }
    } else {
      // Shouldn't happen [tm].
      formatTypes.Add("NULL");
    }
    formatTypes.Sort();
    return string.Join(",", formatTypes.ToArray());
  }

  /// <summary>
  /// Sends Analytics events for mutations in the given import options.
  /// </summary>
  /// <param name="before">The options before the mutation.</param>
  /// <param name="after">The options after the mutation.</param>
  private void SendImportOptionMutationAnalytics(EditTimeImportOptions before, EditTimeImportOptions after) {
    if (before.alsoInstantiate != after.alsoInstantiate) {
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_INSTANTIATE_TOGGLED, after.alsoInstantiate.ToString());
    }
    if (before.baseOptions.desiredSize != after.baseOptions.desiredSize) {
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_DESIRED_SIZE_SET,
          after.baseOptions.desiredSize.ToString());
    }
    if (before.baseOptions.recenter != after.baseOptions.recenter) {
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_RECENTER_TOGGLED,
          after.baseOptions.recenter.ToString());
    }
    if (before.baseOptions.rescalingMode != after.baseOptions.rescalingMode) {
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_SCALE_MODE_CHANGED,
          after.baseOptions.rescalingMode.ToString());
    }
    if (before.baseOptions.scaleFactor != after.baseOptions.scaleFactor) {
      PtAnalytics.SendEvent(PtAnalytics.Action.IMPORT_SCALE_FACTOR_CHANGED,
          after.baseOptions.scaleFactor.ToString());
    }
  }

  private void OnDestroy() {
    PtDebug.Log("ABW: destroying.");
    manager.SetRefreshCallback(null);
  }

  private struct CategoryInfo {
    public string key;
    public string title;
    public PolyCategory polyCategory;
    public CategoryInfo(string key, string title, PolyCategory polyCategory = PolyCategory.UNSPECIFIED) {
      this.key = key;
      this.title = title;
      this.polyCategory = polyCategory;
    }
  }
}

}
