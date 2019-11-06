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

public class InWorldLogPanel : MonoBehaviour
{
  public RectTransform rectTransform;
  [SerializeField] TMPro.TextMeshProUGUI textField;
  [SerializeField] TMPro.TextMeshProUGUI messageField;
  [SerializeField] RectTransform messageRect;
  [SerializeField] Color messageColor;
  public VoosActor actor;

  InWorldLogManager manager;

  //const int MAX_LINES = 4;
  const string MESSAGE_RECEIVED = "MessageReceived";
  const float MESSAGE_ANIMATE_TIME = .2f;
  const float MESSAGE_DISPLAY_TIME = .7f;
  const float MESSAGE_HIDE_TIME = .25f;

  //List<string> displayStrings = new List<string>();

  public void Setup(VoosActor actor, InWorldLogManager manager)
  {
    this.actor = actor;
    this.manager = manager;
  }

  public void SetPosition(Vector2 pos)
  {
    rectTransform.anchoredPosition = pos;
  }

  void Update()
  {
    //textField.text = string.Join("\n", displayStrings);
  }

  internal void RequestDestruct()
  {
    StopAllCoroutines();
    Destroy(gameObject);
  }

  // removing all this until we actually need it.
  // public void HandleLogEvent(BehaviorSystem.BehaviorLogEvent entry, bool isError)
  // {
  //   // AddToLineToDisplayText(entry.logText + ":" + entry.messageName);
  //   if (entry.eventName == MESSAGE_RECEIVED)
  //   {
  //     MessagePopup(entry.messageName);
  //   }
  // }

  private void MessagePopup(string messageName)
  {
    if (messageRoutine != null) StopCoroutine(messageRoutine);
    messageField.text = messageName;
    messageRoutine = StartCoroutine(MessageRoutine());
  }



  Coroutine messageRoutine;
  IEnumerator MessageRoutine()
  {
    messageField.gameObject.SetActive(true);
    messageField.color = Color.clear;
    messageRect.localScale = Vector3.zero;

    float timer = MESSAGE_ANIMATE_TIME;
    while (timer > 0)
    {
      timer -= Time.unscaledDeltaTime;
      if (timer < 0) timer = 0;
      float percent = 1 - timer / MESSAGE_ANIMATE_TIME;
      messageRect.anchoredPosition = manager.GetLerpMessagePosition(this, percent);
      messageRect.localScale = Vector3.one * percent;
      messageField.color = Color.Lerp(Color.clear, messageColor, percent);
      yield return null;
    }

    timer = MESSAGE_DISPLAY_TIME;
    while (timer > 0)
    {
      timer -= Time.unscaledDeltaTime;
      if (timer < 0) timer = 0;
      messageRect.anchoredPosition = manager.GetLerpMessagePosition(this, 1);
      // float percent = 1 - timer / MESSAGE_DISPLAY_TIME;
      yield return null;
    }

    timer = MESSAGE_HIDE_TIME;
    while (timer > 0)
    {
      timer -= Time.unscaledDeltaTime;
      if (timer < 0) timer = 0;
      float percent = timer / MESSAGE_DISPLAY_TIME;
      messageRect.anchoredPosition = manager.GetLerpMessagePosition(this, 1);
      messageField.color = Color.Lerp(Color.clear, messageColor, percent);
      yield return null;
    }
    messageField.gameObject.SetActive(false);
  }

  void AddToLineToDisplayText(string newString)
  {
    // displayStrings.Add(newString);
    //if (displayStrings.Count > MAX_LINES) displayStrings.RemoveAt(0);
  }
}
