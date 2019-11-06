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
using UnityEngine;
using UnityEditor;
using System.Text;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using PolyToolkitInternal;

namespace PolyToolkitEditor {

/// <summary>
/// Sends Google Analytics events.
/// Implemented according to the Google Analytics protocol reference:
/// https://developers.google.com/analytics/devguides/collection/protocol/v1/
/// 
/// NOTE: Poly Toolkit only collect analytics at edit time (this logic only runs in the editor).
/// These analytics are anonymous and help us improve the application. They allow us to infer
/// information such as what features users are using the most, what configuration settings are
/// most frequent, how many errors are happening on asset importing, etc. This data is only
/// accessed in aggregate form. Analytics can be turned off in the Poly Toolkit settings menu
/// (the logic to check that flag is in PtAnalytics.cs, which calls this module).
/// 
/// No part of this analytics code gets included in the app at runtime. To ensure that it
/// doesn't, this file is conditionally compiled with "#if UNITY_EDITOR" directives, so that
/// any attempt to use it a run time will trigger a compile error.
/// </summary>
public class AnalyticsSender {
  /// <summary>
  /// Google Analytics request prefix (with version, TID and CID).
  /// Generated during initialization.
  /// </summary>
  private readonly string prefix;

  /// <summary>
  /// Initializes the AnalyticsSender.
  /// </summary>
  /// <param name="tid">The Google Analytics TID to use ("UA-XXXXX-Y").</param>
  public AnalyticsSender(string tid) {
    string cid = PlayerPrefs.GetString("GA_CID");
    if (string.IsNullOrEmpty(cid)) {
      // NOTE: the CID "client ID" is a persistent (but anonymous) token used in Google Analytics to
      // help correlate different events into a single "session", which allows us to know, for example,
      // how long users are spending on certain screens and in what sequence they visit different parts
      // of the app. The CID does NOT contain any personally identifiable information: it's just a random
      // number generated locally. There is no way to correlate a CID with a particular user, and
      // it's not transmitted anywhere else (it's only included in Google Analytics API calls, not in
      // any other API calls).
      cid = GUID.Generate().ToString();
      PlayerPrefs.SetString("GA_CID", cid);
    }
    prefix = string.Format("v=1&tid={0}&cid={1}", tid, cid);
  }

  /// <summary>
  /// Collects a page view of the given URI.
  /// </summary>
  /// <param name="path">The path of the page that was viewed (e.g. "/LoremIpsum").</param>
  public void SendPageView(string path) {
    SendHit(new Dictionary<string, string> {
      { "t", "pageview" },
      { "dp", path }
    });
  }

  /// <summary>
  /// Collects an event.
  /// </summary>
  /// <param name="category">Category of the event.</param>
  /// <param name="action">Event action.</param>
  /// <param name="label">Event label.</param>
  /// <param name="val">Event value.</param>
  public void SendEvent(string category, string action, string label = null, long? val = null) {
    Dictionary<string, string> fields = new Dictionary<string, string> {
      { "t", "event" },
      { "ec", category },
      { "ea", action }
    };
    if (label != null) fields["el"] = label;
    if (val != null) fields["ev"] = val.Value.ToString();
    SendHit(fields);
  }

  /// <summary>
  /// Collects an Exception.
  /// </summary>
  /// <param name="description">Description of the exception.</param>
  /// <param name="isFatal">True if the exception was fatal.</param>
  public void SendException(string description, bool isFatal = false) {
    SendHit(new Dictionary<string, string> {
      { "t", "exception" },
      { "exd", description },
      { "exf", isFatal ? "1" : "0" }
    });
  }

  /// <summary>
  /// Sends a hit to Google Analytics.
  /// </summary>
  /// <param name="fields">Key-value pairs that make up the properties of the hit. These
  /// are assumed to be in the appropriate Google Analytics format.</param>
  private void SendHit(Dictionary<string,string> fields) {
    StringBuilder sb = new StringBuilder(prefix);
    foreach (KeyValuePair<string, string> pair in fields) {
      sb.AppendFormat("&{0}={1}", WWW.EscapeURL(pair.Key), WWW.EscapeURL(pair.Value));
    }
    string payload = sb.ToString();
    try {
      UnityWebRequest request = new UnityWebRequest("https://www.google-analytics.com/collect", "POST");
      request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
      request.uploadHandler.contentType = "application/x-www-form-urlencoded";
      UnityCompat.SendWebRequest(request);
      PtDebug.LogFormat("ANALYTICS: sent hit: {0}", payload);
    } catch (Exception ex) {
      // Reporting these as errors would be noisy and annoying. We don't want to do that -- maybe the user is
      // offline. Not being able to send analytics isn't a big deal. So only log this error if
      // PtDebug verbose logging is on.
      PtDebug.LogFormat("*** Error sending analytics: {0}", ex);
    }
  }
}
}
#endif