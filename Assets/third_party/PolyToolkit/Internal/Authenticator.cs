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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using PolyToolkit;

using PolyToolkitInternal.entitlement;

namespace PolyToolkitInternal {
  /// <summary>
  /// Basic authentication module.
  /// This is exposed in the Poly Toolkit API through the Poly class. This is just the implementation.
  /// </summary>
  [ExecuteInEditMode]
  public class Authenticator : MonoBehaviour {
    public static Authenticator Instance {
      get {
        PolyUtils.AssertNotNull(instance,
            "Can't access Authenticator.Instance before calling Authenticator.Initialize");
        return instance;
      }
    }

    /// <summary>
    /// Returns whether Authenticator was initialized by calling Initialize().
    /// </summary>
    public static bool IsInitialized { get { return instance != null; } }

    /// <summary>
    /// Returns whether or not the user is authenticated.
    /// </summary>
    public bool IsAuthenticated {
      get {
        return oauth2Identity != null && oauth2Identity.LoggedIn;
      }
    }

    /// <summary>
    /// Returns whether or not authentication is in progress.
    /// </summary>
    public bool IsAuthenticating {
      get {
        return oauth2Identity != null && oauth2Identity.WaitingOnAuthorization;
      }
    }

    /// <summary>
    /// Returns whether or not authentication is supported on this platform. Authentication is only
    /// supported on Windows/Mac for now.
    /// </summary>
    public bool IsAuthenticationSupported { get {
      return Application.platform == RuntimePlatform.WindowsEditor ||
        Application.platform == RuntimePlatform.WindowsPlayer ||
        Application.platform == RuntimePlatform.OSXEditor ||
        Application.platform == RuntimePlatform.OSXPlayer; } }

    /// <summary>
    /// Returns the current access token, if available. Otherwise, null.
    /// </summary>
    public string AccessToken {
      get {
        return oauth2Identity != null ? oauth2Identity.AccessToken : null;
      }
    }

    /// <summary>
    /// Returns the current refresh token, if available. Otherwise, null.
    /// </summary>
    public string RefreshToken {
      get {
        return oauth2Identity != null ? oauth2Identity.RefreshToken : null;
      }
    }

    /// <summary>
    /// Returns the name of the signed-in user, or null if unavailable.
    /// </summary>
    public string UserName {
      get {
        return (oauth2Identity == null || oauth2Identity.Profile == null) ?
            null : oauth2Identity.Profile.name;
      }
    }

    /// <summary>
    /// Returns the icon that represents the user, or null if unavailable.
    /// </summary>
    public Sprite UserIcon {
      get {
        return (oauth2Identity == null || oauth2Identity.Profile == null) ?
            null : oauth2Identity.Profile.icon;
      }
    }

    private static Authenticator instance;

    /// <summary>
    /// OAuth2Identity we use for authentication. This will be null in platforms that don't
    /// support authentication.
    /// </summary>
    private OAuth2Identity oauth2Identity;

    /// <summary>
    /// Initializes the authenticator.
    ///
    /// Note that if config.autoLaunchSignInFlow is true (default), the sign-in flow (browser-based)
    /// will be launched immediately (unless the user is already authenticated).
    ///
    /// If config.autoLaunchSignInFlow is false, then you will remain in unauthenticated state
    /// until you manually call LaunchSignInFlow().
    /// </summary>
    /// <param name="config">The authentication configuration to use.</param>
    public static void Initialize(PolyAuthConfig config) {
      PolyUtils.AssertTrue(instance == null, "Authenticator.Initialize called twice.");
      
      GameObject hostObject = PolyInternalUtils.CreateSingletonGameObject("PolyToolkit Authenticator");
      instance = hostObject.AddComponent<Authenticator>();

      // Currently, authentication is only supported on Windows/Mac for now.
      PtDebug.LogFormat("Platform: {0}, authentication supported: {1}", Application.platform,
        instance.IsAuthenticationSupported);
      if (instance.IsAuthenticationSupported) {
        InitializeCertificateValidation();
        instance.Setup(config);
      }
    }

    private void Setup(PolyAuthConfig config) {
      oauth2Identity = gameObject.AddComponent<OAuth2Identity>();
      oauth2Identity.Setup(config.serviceName, config.clientId, config.clientSecret,
        config.additionalScopes != null ? string.Join(" ", config.additionalScopes) : "");
    }

    public static void Shutdown() {
      DestroyImmediate(instance.gameObject);
      instance = null;
    }

    /// <summary>
    /// Attempts to authenticate.  
    /// </summary>
    /// <param name="interactive">If true, launch the sign in flow (browser) if necessary. If false,
    /// attempt to authenticate silently.</param>
    /// <param name="callback">Callback to call when authentication completes.</param>
    public void Authenticate(bool interactive, Action<PolyStatus> callback) {
      if (!instance.IsAuthenticationSupported) {
        callback(PolyStatus.Error("Authentication is not supported on this platform."));
      }
      oauth2Identity.Login(
        () => { callback(PolyStatus.Success()); },
        () => { callback(PolyStatus.Error("Authentication failed.")); },
        interactive);
    }

    /// <summary>
    /// Attempts to authenticate using the provided tokens.
    /// This will NOT launch a sign-in flow. It will use the given tokens directly.
    /// </summary>
    /// <param name="accessToken">The access token to use.</param>
    /// <param name="refreshToken">The refresh token to use.</param>
    /// <param name="callback">The callback to call when authentication completes.</param>
    public void Authenticate(string accessToken, string refreshToken, Action<PolyStatus> callback) {
      if (!instance.IsAuthenticationSupported) {
        callback(PolyStatus.Error("Authentication is not supported on this platform."));
      }
      oauth2Identity.LoginWithTokens(
        () => { callback(PolyStatus.Success()); },
        () => { callback(PolyStatus.Error("Authentication failed (with tokens).")); },
        accessToken, refreshToken);
    }

    /// <summary>
    /// Cancels the current authentication flow, if one is in progress.
    /// Does nothing if authentication isn't currently in progress.
    /// </summary>
    public void CancelAuthentication() {
      if (!instance.IsAuthenticationSupported) return;
      oauth2Identity.CancelLogin();
    }

    /// <summary>
    /// Signs out. This will delete any existing access and refresh tokens. It will also clear
    /// the cache (asynchronously).
    /// </summary>
    public void SignOut() {
      if (!instance.IsAuthenticationSupported) return;
      oauth2Identity.Logout();
      // When signing out, we should clear the cache too since the cache might have data that
      // belongs to the signed-in user.
      PolyApi.ClearCache();
    }

    private static void InitializeCertificateValidation() {
      ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate,
          X509Chain chain, SslPolicyErrors sslPolicyErrors) => {
        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None) {
          for (int i = 0; i < chain.ChainStatus.Length; i++) {
            if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
              chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
              chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
              chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
              chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
              bool chainIsValid = chain.Build((X509Certificate2)certificate);
              if (!chainIsValid) {
                return false;
              }
            }
          }
        }
        return true;
      };
    }

    /// <summary>
    ///   Refreshes an access token, if a given refresh token is valid, and then calls one of the given callbacks.
    /// </summary>
    public void Reauthorize(Action<PolyStatus> callback) {
      if (!instance.IsAuthenticationSupported) {
        callback(PolyStatus.Error("Authentication is not supported on this platform."));
      }

      CoroutineRunner.StartCoroutine(this, oauth2Identity.Reauthorize(
        successCallback: () => { callback(PolyStatus.Success()); },
        failureCallback: (string error) => { callback(PolyStatus.Error(error)); }
      ));
    }
  }
}
