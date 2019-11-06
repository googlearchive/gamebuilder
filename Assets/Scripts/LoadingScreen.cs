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

public class LoadingScreen : MonoBehaviour
{
  [SerializeField] GameObject loadingScreenPrefab;
  [SerializeField] TMPro.TMP_Text statusText;
  [SerializeField] GameObject progressBar;
  [SerializeField] RectTransform progressBarBackground;
  [SerializeField] RectTransform progressBarForeground;
  [SerializeField] UnityEngine.UI.Button cancelButton;

  const float FADE_TIME = 0.75f;

  bool fading;
  float fadeStartTime;
  CanvasGroup canvasGroup;
  System.Action cancelCallback;

  void Awake()
  {
    cancelButton.onClick.AddListener(() => { cancelCallback?.Invoke(); });
    ResetWidgets();
  }

  public void ShowAndDo(System.Action actuallyLoad)
  {
    StartCoroutine(LoadingScreenSequence(actuallyLoad));
  }

  public void SetStatusText(string text)
  {
    statusText.text = text;
  }

  public void SetProgress(float progress0to1)
  {
    progressBar.SetActive(true);
    progressBarForeground.sizeDelta = new Vector2(
      progressBarBackground.sizeDelta.x * progress0to1, progressBarForeground.sizeDelta.y);
  }

  public void SetCancelButton(string text, System.Action callback)
  {
    cancelButton.GetComponentInChildren<TMPro.TMP_Text>().text = text;
    cancelCallback = callback;
    cancelButton.gameObject.SetActive(true);
  }

  public void Hide()
  {
    loadingScreenPrefab.SetActive(false);
  }

  public void FadeAndHide()
  {
    fading = true;
    fadeStartTime = Time.unscaledTime;
  }

  public void Show()
  {
    fading = false;
    ResetWidgets();
    loadingScreenPrefab.SetActive(true);
  }

  private void ResetWidgets()
  {
    // Progress bar starts hidden. Will show later if requested.
    progressBar.SetActive(false);
    cancelButton.gameObject.SetActive(false);
    statusText.text = "";
  }

  void Update()
  {
    if (!loadingScreenPrefab.activeSelf)
    {
      return;
    }
    canvasGroup = canvasGroup ?? loadingScreenPrefab.GetComponent<CanvasGroup>();
    if (fading)
    {
      float elapsed = Time.unscaledTime - fadeStartTime;
      if (elapsed > FADE_TIME)
      {
        fading = false;
        Hide();
      }
      canvasGroup.alpha = Mathf.Clamp(1.0f - elapsed / FADE_TIME, 0, 1);
    }
    else
    {
      canvasGroup.alpha = 1;
    }
  }

  private IEnumerator LoadingScreenSequence(System.Action actuallyLoad)
  {
    Show();
    yield return null;
    yield return null;
    actuallyLoad();
  }
}
