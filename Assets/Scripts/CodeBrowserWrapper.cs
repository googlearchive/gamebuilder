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

// This is an abstraction layer that wraps the code browser implementations so that
// the rest of the app can interact with it without knowing about which concrete implementation
// is being used.
//
// If the USE_ZFBROWSER symbol is defined, this class will use ZFBrowser for the implementation;
// otherwise, it uses a basic text box implementation.
public class CodeBrowserWrapper : MonoBehaviour
{
  bool initialized;
  ICodeBrowser codeBrowser;
  [SerializeField] BasicCodeBrowser basicCodeBrowser;

  private void LazyInitialize()
  {
    Debug.Assert(basicCodeBrowser != null, "basicCodeBrowser property not set in CodeBrowserWrapper");
    initialized = true;
#if USE_ZFBROWSER
    basicCodeBrowser.gameObject.SetActive(false);
    ZenFulcrum.EmbeddedBrowser.Browser zfBrowser = gameObject.AddComponent<ZenFulcrum.EmbeddedBrowser.Browser>();
    zfBrowser.Url = "localGame://gamebuilder/js-editor.html";
    zfBrowser.allowContextMenuOn = ZenFulcrum.EmbeddedBrowser.BrowserNative.ContextMenuOrigin.Editable;
    ZenFulcrum.EmbeddedBrowser.PointerUIGUI pointerScript = gameObject.AddComponent<ZenFulcrum.EmbeddedBrowser.PointerUIGUI>();
    pointerScript.enableMouseInput = true;
    pointerScript.enableTouchInput = true;
    pointerScript.enableInput = true;
    pointerScript.automaticResize = true;
    codeBrowser = new ZfBrowserImpl(zfBrowser);
#else
    codeBrowser = basicCodeBrowser;
    // Don't do this:
    //   basicCodeBrowser.gameObject.SetActive(true);
    // Because this causes the code browser to appear onscreen during init,
    // which we don't want.
#endif
  }

  public ICodeBrowser GetCodeBrowser()
  {
    if (!initialized)
    {
      LazyInitialize();
    }
    return codeBrowser;
  }
}

public interface ICodeBrowser
{
  void Show(bool show);
  bool KeyboardHasFocus();
  void SetCode(string liveCode);
  void SetScrollTop(int scrollTop);
  void SetReadOnly(bool readOnly);
  void CheckIsReadyAsync(System.Action<bool> callback);
  void AddSystemSource(string contents, string fileName);
  void GetCodeAsync(System.Action<string> callback);
  void GetScrollTopAsync(System.Action<int> scrollTop, System.Action onError);
  void AddError(int baseOneLineNumber, string message);
  void ClearErrors();
  void ZoomIn();
  void ZoomOut();
  float GetZoom();
  void SetZoom(float zoom);
}

#if USE_ZFBROWSER
class ZfBrowserImpl : ICodeBrowser
{
  ZenFulcrum.EmbeddedBrowser.Browser browser;
  ZenFulcrum.EmbeddedBrowser.PointerUIGUI browserUI;

  public ZfBrowserImpl(ZenFulcrum.EmbeddedBrowser.Browser browser)
  {
    this.browser = browser;
    browserUI = browser.GetComponentInChildren<ZenFulcrum.EmbeddedBrowser.PointerUIGUI>();
  }

  public void Show(bool show)
  {
  }

  public bool KeyboardHasFocus()
  {
    return browserUI.KeyboardHasFocus;
  }

  public void SetCode(string liveCode)
  {
    browser.CallFunction("setCode", liveCode);
  }

  public void SetScrollTop(int scrollTop)
  {
    browser.CallFunction("setScrollTop", scrollTop);
  }

  public void SetReadOnly(bool readOnly)
  {
    browser.CallFunction("setReadOnly", readOnly);
  }

  public void CheckIsReadyAsync(System.Action<bool> callback)
  {
    browser.CallFunction("isReady").Then(value => callback.Invoke((bool)value));
  }

  public void AddSystemSource(string contents, string fileName)
  {
    browser.CallFunction("addSystemSource", contents, fileName);
  }

  public void GetCodeAsync(System.Action<string> callback)
  {
    browser.CallFunction("getCode").Then(value => callback.Invoke((string)value));
  }

  public void GetScrollTopAsync(System.Action<int> callback, System.Action onError)
  {
    browser.CallFunction("getScrollTop").Then(value => callback.Invoke((int)value)).Catch(unused => onError.Invoke());
  }

  public void AddError(int baseOneLineNumber, string message)
  {
    ZenFulcrum.EmbeddedBrowser.JSONNode[] args = new ZenFulcrum.EmbeddedBrowser.JSONNode[] {
      new ZenFulcrum.EmbeddedBrowser.JSONNode((double)baseOneLineNumber),
      new ZenFulcrum.EmbeddedBrowser.JSONNode(message)
    };
    browser.CallFunction("addError", args);
  }

  public void ClearErrors()
  {
    browser.CallFunction("clearErrors");
  }

  public void ZoomIn()
  {
    browser.Zoom++;
  }

  public void ZoomOut()
  {
    browser.Zoom--;
  }

  public float GetZoom()
  {
    return browser.Zoom;
  }

  public void SetZoom(float newZoom)
  {
    browser.Zoom = newZoom;
  }
}
#endif