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

public class UndoToast : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI messageField;
  [SerializeField] UnityEngine.UI.Button undoButton;
  [SerializeField] UnityEngine.UI.Button closeButton;
  [SerializeField] RectTransform rectTransform;
  [SerializeField] CanvasGroup canvasGroup;

  UndoStack stack;

  enum State
  {
    ShowingUndo,
    ShowingConfirmed,
    Closed
  }

  State state = State.Closed;

  const float SHOW_UNDO_DURATION = 5;
  const float SHOW_CONFIRMED_DURATION = 3;

  //lock up close from mouse button based on durations above
  bool timerLock = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref stack);
    undoButton.onClick.AddListener(UndoClicked);
    closeButton.onClick.AddListener(Close);
  }

  void OnEnable()
  {
    stack.onPushed += OnNewItemPushed;
  }

  void OnDisable()
  {
    stack.onPushed -= OnNewItemPushed;
  }

  void OnNewItemPushed()
  {
    Show(stack.GetTopItem().actionLabel);
  }

  void Show(string message)
  {
    StopAllCoroutines();
    timerLock = false;
    messageField.text = message;
    undoButton.gameObject.SetActive(true);
    state = State.ShowingUndo;

    canvasGroup.alpha = 1;
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;

    StartCoroutine(showUndoTimer());
  }

  IEnumerator showUndoTimer()
  {
    timerLock = true;
    yield return new WaitForSecondsRealtime(SHOW_UNDO_DURATION);
    timerLock = false;
  }

  IEnumerator showConfirmTimer()
  {
    timerLock = true;
    yield return new WaitForSecondsRealtime(SHOW_CONFIRMED_DURATION);
    timerLock = false;
  }

  public bool IsShowing()
  {
    return state != State.Closed;
  }

  void Update()
  {
    if (!timerLock && state != State.Closed && Input.GetMouseButtonDown(0))
    {
      if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition))
      {
        Close();
      }
    }

  }

  void UndoClicked()
  {
    stack.TriggerUndo();

    // TODO. probably want to put some delays on this
    if (stack.IsEmpty())
    {
      ShowConfirmation("Undone!");
    }
    else
    {
      Show(stack.GetTopItem().actionLabel);
    }
  }

  void ShowConfirmation(string message)
  {
    messageField.text = message;
    undoButton.gameObject.SetActive(false);
    state = State.ShowingConfirmed;
    StartCoroutine(showConfirmTimer());
  }

  public void Close()
  {
    StopAllCoroutines();
    state = State.Closed;

    canvasGroup.alpha = 0;
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
  }

}
