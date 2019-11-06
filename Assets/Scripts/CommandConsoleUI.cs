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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CommandConsoleUI : MonoBehaviour
{
  [SerializeField] RectTransform canvasRect;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] TMPro.TextMeshProUGUI textField;
  [SerializeField] RectTransform scrollbarRect;
  [SerializeField] RectTransform scrollbarTrackRect;
  [SerializeField] GameObject scrollbarActive;
  [SerializeField] TMPro.TMP_InputField inputField;
  [SerializeField] CommandTerminal.HeadlessTerminal terminal;
  HudManager hudManager;
  InputControl inputControl;

  // Log contents after processing from buffer. Currently, this means splitting on multiline statements
  // and lines longer than MAX_CHARS_PER_LINE.
  private List<string> processedLogLines = new List<string>();
  // TODO: should be able to calculate these from the textbox.
  private static int MAX_VISIBLE_LINES = 12;
  private static int MAX_CHARS_PER_LINE = 220;
  private int logScrollPosition;
  float scrollbarDragOffset = -1;

  bool shouldMoveCursor = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref terminal);
    Util.FindIfNotSet(this, ref inputControl);
    inputField.onEndEdit.AddListener(OnInputFieldEnd);
    inputField.onValueChanged.AddListener(OnInputFieldChanged);
    CommandTerminal.HeadlessTerminal.Buffer.onLogsAdded += OnLogsAdded;
    CommandTerminal.HeadlessTerminal.Buffer.onLogsShifted += OnLogsShifted;
    CommandTerminal.HeadlessTerminal.Buffer.onLogsCleared += OnLogsCleared;
    EventTrigger scrollTrigger = scrollbarRect.GetComponent<EventTrigger>();

    EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
    beginDragEntry.eventID = EventTriggerType.BeginDrag;
    beginDragEntry.callback.AddListener((data) => { OnScrollbarBeginDrag((PointerEventData)data); });
    scrollTrigger.triggers.Add(beginDragEntry);

    EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
    endDragEntry.eventID = EventTriggerType.EndDrag;
    endDragEntry.callback.AddListener((data) => { StopDragging(); });
    scrollTrigger.triggers.Add(endDragEntry);
  }

  public bool IsConsoleInputActive()
  {
    return inputField.isFocused;
  }

  void Open()
  {
    canvasRect.gameObject.SetActive(true);
    UpdateTextInUI();
    inputField.ActivateInputField();
    hudManager.UpdateVerticalOffsetAsPercent(GetHeightInPercent());
  }

  void Close()
  {
    canvasRect.gameObject.SetActive(false);
    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    hudManager.UpdateVerticalOffsetAsPercent(GetHeightInPercent());
    StopDragging();
  }

  public float GetHeightInPercent()
  {
    return IsOpen() ? rectTransform.rect.height / canvasRect.rect.height : 0;
  }

  bool IsOpen()
  {
    return canvasRect.gameObject.activeSelf;
  }

  void OnLogsAdded()
  {
    bool wasAtBottom = logScrollPosition >= processedLogLines.Count - MAX_VISIBLE_LINES;

    int lastId = CommandTerminal.HeadlessTerminal.Buffer.Logs.Count - 1;
    string logLine = CommandTerminal.HeadlessTerminal.Buffer.Logs[lastId].ToString();

    // Split string on newlines.
    string[] splitLogLines = logLine.NormalizeLineEndings().Split('\n');
    int linesAdded = 0;
    for (int j = 0; j < splitLogLines.Length; j++)
    {
      string splitLogLine = splitLogLines[j];
      // Further split string segments on length.
      for (int k = 0; k < Mathf.Max(splitLogLine.Length, 1); k += MAX_CHARS_PER_LINE)
      {
        linesAdded++;
        processedLogLines.Add(
          splitLogLine.Substring(
            k, Mathf.Min(MAX_CHARS_PER_LINE, splitLogLine.Length - k)));
      }
    }
#if UNITY_EDITOR
    Debug.Assert(linesAdded == ComputeTotalLines(logLine));
#endif

    // If we were at the bottom of the log, we should go to the new bottom.
    if (wasAtBottom)
    {
      logScrollPosition = Mathf.Max(0, processedLogLines.Count - MAX_VISIBLE_LINES);
    }

    UpdateLog();
  }

  int ComputeTotalLines(string logLine)
  {
    string[] splitLogLines = logLine.NormalizeLineEndings().Split('\n');
    int totalLines = 0;
    for (int j = 0; j < splitLogLines.Length; j++)
    {
      string splitLogLine = splitLogLines[j];
      totalLines += (splitLogLine.Length / MAX_CHARS_PER_LINE) + 1;
    }
    return totalLines;
  }

  void OnLogsShifted(CommandTerminal.LogItem item)
  {
    // Find the number of lines we should remove from the beginning of the processed lines.
    string logLine = item.ToString();
    int totalLines = ComputeTotalLines(logLine);
    processedLogLines.RemoveRange(0, totalLines);
    logScrollPosition = Mathf.Min(logScrollPosition, Mathf.Max(processedLogLines.Count - MAX_VISIBLE_LINES, 0));
    UpdateLog();
  }

  void OnLogsCleared()
  {
    processedLogLines.Clear();
    logScrollPosition = 0;
    UpdateLog();
  }

  void UpdateTextInUI()
  {
    textField.text = string.Join("\n", processedLogLines.GetRange(
      Mathf.Max(0, logScrollPosition),
      Mathf.Min(processedLogLines.Count, MAX_VISIBLE_LINES)
    ));
  }

  public void ToggleConsole()
  {
    if (IsOpen())
    {
      Close();
    }
    else
    {
      Open();
    }
  }

  void SetInputToCommandText()
  {
    inputField.text = terminal.command_text;
    inputField.MoveToEndOfLine(false, false);
    shouldMoveCursor = true;
  }

  void Update()
  {
    if (shouldMoveCursor)
    {
      inputField.MoveToEndOfLine(false, false);
      shouldMoveCursor = false;
    }

    if (Input.GetKeyDown(KeyCode.Tab) && IsConsoleInputActive())
    {
      terminal.CompleteCommand();
    }

    if (Input.GetKeyDown(KeyCode.UpArrow) && IsConsoleInputActive())
    {
      terminal.HistoryUp();
      SetInputToCommandText();
    }

    if (Input.GetKeyDown(KeyCode.DownArrow) && IsConsoleInputActive())
    {
      terminal.HistoryDown();
      SetInputToCommandText();
    }

    UpdateLog();

    if (inputControl.GetButtonDown("Console") && IsOpen())
    {
      Close();
    }

    hudManager.UpdateVerticalOffsetAsPercent(GetHeightInPercent());
  }

  void UpdateLog()
  {
    // Scroll log if MMB was scrolled or scrollbar was dragged.
    int newLogScrollPosition = logScrollPosition;
    if (scrollbarDragOffset >= 0)
    {
      Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, Input.mousePosition);
      Vector2 localPoint;
      RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screen, null, out localPoint);
      float dragPosition = -localPoint.y - scrollbarDragOffset;
      newLogScrollPosition = Mathf.Clamp(
        Mathf.RoundToInt((dragPosition / textField.rectTransform.rect.height) * processedLogLines.Count),
        0,
        processedLogLines.Count - MAX_VISIBLE_LINES);
    }
    else
    {
      newLogScrollPosition = Mathf.Clamp(
        Mathf.RoundToInt(logScrollPosition - Input.mouseScrollDelta.y),
        0,
        processedLogLines.Count - MAX_VISIBLE_LINES);
    }
    if (newLogScrollPosition != logScrollPosition)
    {
      logScrollPosition = newLogScrollPosition;
    }

    // Update the scrollbar visibility / position.
    if (MAX_VISIBLE_LINES < processedLogLines.Count)
    {
      scrollbarRect.gameObject.SetActive(true);
      scrollbarRect.SetSizeWithCurrentAnchors(
        RectTransform.Axis.Vertical,
        textField.rectTransform.rect.height * (MAX_VISIBLE_LINES * 1.0f) / processedLogLines.Count);
      scrollbarRect.anchoredPosition = new Vector2(
        scrollbarRect.anchoredPosition.x,
        -textField.rectTransform.rect.height * (logScrollPosition * 1.0f) / processedLogLines.Count);
    }
    else
    {
      scrollbarRect.gameObject.SetActive(false);
    }

    UpdateTextInUI();
  }

  void OnInputFieldChanged(string s)
  {
    if (inputControl.GetButtonDown("Console"))
    {
      // Ignore any resulting prompt change, such as typing of ~, by restoring
      // previous command.
      inputField.text = terminal.command_text;
      Close();
      return;
    }

    terminal.command_text = inputField.text;
  }

  void OnInputFieldEnd(string s)
  {
    if (Input.GetButtonDown("Submit"))
    {
      terminal.EnterCommand();
      inputField.text = terminal.command_text;
      inputField.text = "";
      inputField.ActivateInputField();
      // Go to bottom of history
      logScrollPosition = Mathf.Max(0, processedLogLines.Count - MAX_VISIBLE_LINES);
    }
  }

  private void OnScrollbarBeginDrag(PointerEventData data)
  {
    Vector2 localPoint;
    RectTransformUtility.ScreenPointToLocalPointInRectangle(
      scrollbarRect,
      RectTransformUtility.WorldToScreenPoint(null, data.pressPosition),
      null,
      out localPoint);
    scrollbarDragOffset = -localPoint.y;
    scrollbarActive.SetActive(true);
  }

  private void StopDragging()
  {
    scrollbarDragOffset = -1;
    scrollbarActive.gameObject.SetActive(false);
  }

  internal void SetHudManager(HudManager hudManager)
  {
    this.hudManager = hudManager;
  }
}
