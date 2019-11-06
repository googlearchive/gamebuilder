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
using BehaviorProperties;
using System;
using UnityEngine.EventSystems;

public class CodeEditorController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
  [SerializeField] RectTransform rectTransform;
  ICodeBrowser browser;

  // Per instance state

  VoosEngine voosEngine;
  BehaviorSystem behaviorSystem;
  CursorManager cursorManager;

  public bool isEditorReady { get; private set; }
  public event System.Action onRecompiled;

  // The target, ie. current thing being edited
  UnassignedBehavior target = null;
  bool waitingOnRetrieveCode = false;
  bool compileRequested = false;

  // This will change with each keystroke, etc.
  string liveCode = null;

  // The code that was last compiled (not necessarily successfully)
  string lastCompiledCode = null;

  Dictionary<string, int> scrollPosByUri = new Dictionary<string, int>();

  void Awake()
  {
    CodeBrowserWrapper codeBrowserWrapper = null;
    Util.FindIfNotSet(this, ref codeBrowserWrapper);
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref cursorManager);
    Util.FindIfNotSet(this, ref behaviorSystem);

    browser = codeBrowserWrapper.GetCodeBrowser();
    isEditorReady = false;
    InitializeJsEditor();
  }

  void OnEnable()
  {
    voosEngine.OnScriptRuntimeError += HandleError;
    voosEngine.OnModuleCompileError += HandleCompileError;
    voosEngine.OnBeforeModuleCompile += OnBeforeModuleCompile;
    voosEngine.onBehaviorException += OnBehaviorException;
  }

  void OnDisable()
  {
    Clear();

    voosEngine.OnScriptRuntimeError -= HandleError;
    voosEngine.OnModuleCompileError -= HandleCompileError;
    voosEngine.OnBeforeModuleCompile -= OnBeforeModuleCompile;
    voosEngine.onBehaviorException -= OnBehaviorException;
  }

  private void OnBehaviorException(VoosEngine.BehaviorLogItem item)
  {
    AddErrorToMonaco(item.lineNum, item.message);
  }

  // TODO do we need this anymore, given everything is synchronous now?
  private void OnBeforeModuleCompile(string behaviorUri)
  {
    this.ClearErrors();
  }

  void HandleCompileError(VoosEngine.ModuleCompileError error)
  {
    HandleError(error.message);
  }

  bool IsTargetValid()
  {
    return target != null && target.IsValid();
  }

  void HandleError(string message)
  {
    if (!IsTargetValid())
    {
      return;
    }
    int lineNumber = target.GetLineNumberForError(message);
    if (lineNumber == -1)
    {
      // Not relevant to this behavior. Don't bother.
      return;
    }
    AddErrorToMonaco(lineNumber, message);
  }

  public void Set(UnassignedBehavior target, VoosEngine.BehaviorLogItem? error = null)
  {
    Clear();
    Debug.Assert(target.IsValid(), "Given target was not valid?");

    this.target = target;

    // Use draft code if it's there.
    liveCode = Util.IsNotNullNorEmpty(target.GetDraftCode()) ?
      target.GetDraftCode().NormalizeLineEndings() :
      target.GetCode().NormalizeLineEndings();

    lastCompiledCode = target.GetCode().NormalizeLineEndings();

    browser.SetCode(liveCode);

    // Restore scroll pos if this is the same as before
    if (scrollPosByUri.ContainsKey(target.GetBehaviorUri()))
    {
      browser.SetScrollTop(scrollPosByUri[target.GetBehaviorUri()]);
    }

    browser.SetReadOnly(target.IsBehaviorReadOnly());

    if (error.HasValue)
    {
      AddErrorToMonaco(error.Value.lineNum, error.Value.message);
    }
  }

  public void Clear()
  {
    if (IsTargetValid())
    {
      if (lastCompiledCode != liveCode)
      {
        // Uses made un-saved changes. Make sure we save it.
        target.SetDraftCode(liveCode);
      }

      target = null;
    }

    waitingOnRetrieveCode = false; // The outstanding call, if any, will know to terminate itself.

    if (compileRequested)
    {
      Debug.LogError($"Editor was cleared while there was still an outstanding compile request. Please poll IsCompilePending() before clearing. The compile request will be ignored.");
    }
    compileRequested = false;

    liveCode = null;
    lastCompiledCode = null;

    browser.SetCode("");
  }

  void InitializeJsEditor()
  {
    if (isEditorReady)
    {
      return;
    }

    browser.CheckIsReadyAsync(value =>
    {
      if ((bool)value)
      {
        isEditorReady = true;

        // Load all typings
        foreach (var entry in behaviorSystem.typings)
        {
          string path = entry.GetAbsolute();
          if (!System.IO.File.Exists(path))
          {
            Debug.LogError($"Bad typings path: {path}");
          }
          else
          {
            string contents = System.IO.File.ReadAllText(path);
            browser.AddSystemSource(contents, System.IO.Path.GetFileName(path));
          }
        }

        int count = 0;
        foreach (TextAsset jsSource in behaviorSystem.ForSystemSources())
        {
          // Only include the ones marked as visible to Monaco.
          if (jsSource.text.Contains("// VISIBLE_TO_MONACO"))
          {
            browser.AddSystemSource(jsSource.text, $"voos_{count++}-{jsSource.name}.js");
          }
        }
      }
      else
      {
        // Call again.
        InitializeJsEditor();
      }
    });
  }

  public void RequestCodeCompile()
  {
    compileRequested = true;
  }

  public bool IsCompilePending()
  {
    return compileRequested;
  }

  // Zoom == 0 means default level of zoom.
  public float GetZoom()
  {
    return browser.GetZoom();
  }

  public void SetZoom(float zoom)
  {
    browser.SetZoom(zoom);
  }

  private void Update()
  {
    if (!isEditorReady)
    {
      return;
    }

    // Continuously poll for latest code
    if (!waitingOnRetrieveCode)
    {
      waitingOnRetrieveCode = true;
      // If this changes (due to a Set/Clear), we should ignore the retrieve
      // results. Otherwise, we get bugs like b/TODO
      var expectedBehaviorForRetrieve = target;
      browser.GetCodeAsync(value =>
      {
        if (expectedBehaviorForRetrieve != target || !IsTargetValid())
        {
          return;
        }

        try
        {
          waitingOnRetrieveCode = false;
          liveCode = ((string)value).NormalizeLineEndings();

          if (lastCompiledCode != liveCode)
          {
            // This is every keystroke...so not that useful beyond the first
            // one, ie. they tried at all.
          }

          if (compileRequested)
          {
            compileRequested = false;

            // Yeah, there is a potential race condition here where if the user
            // hits F5 during a retrieve. Then the retrieved code may be stale.
            // But this should be rare and OK, since we'll still show the "code
            // modified (relative to last compile)" notice.
            if (lastCompiledCode != liveCode)
            {
              lastCompiledCode = liveCode;
              target.SetCode(liveCode);
              onRecompiled?.Invoke();

              // NOTE: V2 is just to distinguish this event from the previous GA
              // event, which was only logged once per scene.
            }
          }
        }
        catch (System.Exception e)
        {
          Util.LogError($"Exception while polling code for changes: {e}\n{e.StackTrace}");
        }
      });
    }

    UpdateScrollPosPolling();
  }

  bool waitingOnScrollPosPoll = false;

  void UpdateScrollPosPolling()
  {
    if (waitingOnScrollPosPoll)
    {
      return;
    }
    else
    {
      waitingOnScrollPosPoll = true;
      browser.GetScrollTopAsync(value =>
      {
        waitingOnScrollPosPoll = false;
        if (!this.isActiveAndEnabled || this.target == null) return;
        scrollPosByUri[target.GetBehaviorUri()] = (int)value;
      }, () =>
      {
        waitingOnScrollPosPoll = false;
      });
    }
  }

  void AddErrorToMonaco(int baseOneLineNumber, string message)
  {
    browser.AddError(baseOneLineNumber, message);
  }

  void ClearErrors()
  {
    browser.ClearErrors();
  }

  public bool HasUnsavedChanges()
  {
    return isEditorReady && !lastCompiledCode.Equals(liveCode);
  }

  public void FitRect(RectTransform rt)
  {
    rt.GetWorldCorners(TEMP_CORNERS_ARRAY);

    int width = Mathf.Max(1, Mathf.RoundToInt(TEMP_CORNERS_ARRAY[3].x - TEMP_CORNERS_ARRAY[1].x));
    // Even width is important to achieve perfect crispness in lower 16x9 res's.
    if (width % 2 == 1)
    {
      width += 1;
    }
    int height = Mathf.Max(1, Mathf.RoundToInt(TEMP_CORNERS_ARRAY[1].y - TEMP_CORNERS_ARRAY[3].y));

    Vector2 pos = Vector2.Lerp(TEMP_CORNERS_ARRAY[1], TEMP_CORNERS_ARRAY[3], 0.5f);
    pos.x = Mathf.RoundToInt(pos.x);
    // This +0.5f is important to achieve perfect crispness in lower 16x9 res's.
    pos.y = Mathf.RoundToInt(pos.y) + 0.5f;
    transform.position = pos;

    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
  }

  public void OnPointerEnter(PointerEventData eventData)
  {
    cursorManager.SetCursor(CursorManager.CursorType.Text);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    cursorManager.SetCursor(CursorManager.CursorType.Pointer);
  }

  static Vector3[] TEMP_CORNERS_ARRAY = new Vector3[4];

  public void ZoomIn()
  {
    browser.ZoomIn();
  }

  public void ZoomOut()
  {
    browser.ZoomOut();
  }
}