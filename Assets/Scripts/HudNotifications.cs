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
using System.Text;

public class HudNotifications : MonoBehaviour
{
  static float minStaySeconds = 3f;

  [SerializeField] int maxMessagesShown = 5;
  [SerializeField] int typingCharactersPerSecond = 5;

  [SerializeField] float staySecondsPerCharacter = 0.2f;

  public interface Display
  {
    void SetText(string text);
  }

  int lastEntryId = 0;
  float lastEntryAddTime = 0;

  int GenId()
  {
    lastEntryId++;
    return lastEntryId;
  }

  struct Entry
  {
    public float addTime;
    public string content;
    public int numRunes;
    public int id;
  }

  bool IsExpired(Entry entry)
  {
    if (entry.numRunes == 0)
    {
      return true;
    }

    float elapsed = Time.unscaledTime - entry.addTime;
    if (elapsed < minStaySeconds)
    {
      return false;
    }
    float staySeconds = entry.numRunes * staySecondsPerCharacter;
    return elapsed > staySeconds;
  }

  // Used in FIFO order
  LinkedList<Entry> messages = new LinkedList<Entry>();

  // TODO err this should be the interface, not the Basic impl
  public BasicNotificationsDisplay display;

  public void AddMessage(string msg)
  {
    messages.AddLast(new Entry { content = msg, addTime = Time.unscaledTime, id = GenId(), numRunes = Util.CountRunes(msg) });
    lastEntryAddTime = Time.unscaledTime;
  }

  StringBuilder displayedBuilder = new StringBuilder();

  static string TYPING_SUFFIX = "|\n";

  // Update is called once per frame
  void Update()
  {
    // Pop until we're under the max
    while (messages.Count > maxMessagesShown)
    {
      messages.RemoveFirst();
    }

    // Pop expired messages, but intentially stop at the first non-expired one.
    while (messages.Count > 0 && IsExpired(messages.First.Value))
    {
      messages.RemoveFirst();
    }

    if (display != null)
    {
      displayedBuilder.Clear();
      foreach (var entry in messages)
      {
        if (entry.id == lastEntryId)
        {
          // Simulate "typing"
          float typedSeconds = Time.unscaledTime - lastEntryAddTime;
          int charsTyped = Mathf.RoundToInt(typedSeconds * typingCharactersPerSecond);

          int endIndex = 0;
          for (int i = 0; i < charsTyped; i++)
          {
            endIndex = Util.ToNextTMProRuneStart(entry.content, endIndex);
            if (endIndex >= entry.content.Length)
            {
              break;
            }
          }

          displayedBuilder.Append(entry.content, 0, endIndex);
          if (endIndex < entry.content.Length)
          {
            displayedBuilder.Append(TYPING_SUFFIX);
          }
        }
        else
        {
          // For older messages, always show the whole thing.
          displayedBuilder.AppendLine(entry.content);
        }
      }
      display.SetText(displayedBuilder.ToString());
    }
  }
}
