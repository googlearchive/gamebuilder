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
using UnityEngine;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

public static class PtAnalytics {
  private const string TEST_TID = "UA-107885163-1";
  private const string PROD_TID = "UA-107834321-1";
  private const string PT_SOURCE_PRODUCT_NAME = "PolyToolkitUnity";
  private static AnalyticsSender sender;

  /// <summary>
  /// All possible event actions. The first part of the name (up to the first underscore) will be used as
  /// the category in Google Analytics (to group actions).
  /// </summary>
  public enum Action {
    ACCOUNT_SIGN_IN_CANCEL,
    ACCOUNT_SIGN_IN_FAILURE,
    ACCOUNT_SIGN_IN_START,
    ACCOUNT_SIGN_IN_SUCCESS,
    ACCOUNT_SIGN_OUT,
    ACCOUNT_VIEW_PROFILE,
    BROWSE_ASSET_DETAILS_CLICKED,
    BROWSE_ASSET_TYPE_SELECTED,
    BROWSE_CATEGORY_SELECTED,
    BROWSE_MISSING_AUTH,
    BROWSE_REFRESH_CLICKED,
    BROWSE_SEARCH_CLICKED,
    BROWSE_SEARCHED,
    BROWSE_VIEW_ON_WEB,
    MENU_BROWSE_ASSETS,
    MENU_GENERAL_INFORMATION,
    MENU_UPDATE_ATTRIBUTIONS_FILE,
    MENU_SHOW_SETTINGS,
    MENU_ONLINE_DOCUMENTATION,
    IMPORT_DESIRED_SIZE_SET,
    IMPORT_FAILED,
    IMPORT_LOCATION_CHANGED,    
    IMPORT_INSTANTIATE_TOGGLED,
    IMPORT_RECENTER_TOGGLED,
    IMPORT_SCALE_FACTOR_CHANGED,
    IMPORT_SCALE_MODE_CHANGED,
    IMPORT_STARTED,
    IMPORT_STARTED_FROM_SEARCH,
    IMPORT_SUCCESSFUL,
    // Due to a bug in how INSTALL_NEW was collected, this was renamed to INSTALL_NEW_2.
    INSTALL_NEW_2,
    INSTALL_UPGRADE,
  }

  public static void SendEvent(Action actionType, string label = null, long? value = null) {
    if (!PtSettings.Instance.sendEditorAnalytics) {
      // User has opted out. Don't send analytics.
      return;
    }

    InitSenderIfNeeded();
    // Translate our "event type" notion to a Google Analytics category/action pair.
    string action = actionType.ToString();
    string category = "UNKNOWN";
    int firstUnderscore = action.IndexOf('_');
    if (firstUnderscore >= 0) {
      category = action.Substring(0, firstUnderscore);
      // NOTE: we RETAIN the category prefix in the action, because in Analytics the actions
      // are not "namespaced" by category. They are global.
    }
    // Send it.
    sender.SendEvent(category, action, label, value);
  }

  public static void SendException(Exception exception, bool isFatal = false) {
    if (!PtSettings.Instance.sendEditorAnalytics) {
      // User has opted out. Don't send analytics.
      return;
    }

    InitSenderIfNeeded();
    sender.SendException(exception.GetType().ToString(), isFatal);
  }

  private static void InitSenderIfNeeded() {
    if (sender == null) {
      // To prevent our own testing usage from getting mixed with real usage, we use different
      // Google Analytics TIDs for each case:
      // * If we're running in the Poly Toolkit source code project, use TEST_TID.
      // * If we're deployed to a third-party developer's editor, use PROD_TID.
      sender = new AnalyticsSender(Application.productName == PT_SOURCE_PRODUCT_NAME ? TEST_TID : PROD_TID);
    }
  }
}
}