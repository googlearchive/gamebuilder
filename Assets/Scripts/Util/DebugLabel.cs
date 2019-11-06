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

// Slap this on a game object and call it to 
public class DebugLabel
{
  private struct Entry
  {
    public Vector3 worldPos;
    public string message;
    public int frameAdded;

    public bool IsExpired()
    {
      return Time.frameCount - frameAdded > 1;
    }
  }

  private class DrawerComponent : MonoBehaviour
  {
    List<Entry> entries = new List<Entry>();
    int lowestExpiredSlot = 0;
    bool drawsOutstanding = false;

    public void Add(Entry entry)
    {
      if (lowestExpiredSlot >= entries.Count)
      {
        entries.Add(entry);
        lowestExpiredSlot++;
      }
      else
      {
        entries[lowestExpiredSlot] = entry;
        // Now find the next expired..
        for (int i = lowestExpiredSlot + 1; i < entries.Count; i++)
        {
          if (entries[i].IsExpired())
          {
            lowestExpiredSlot = i;
            break;
          }
        }
      }
      drawsOutstanding = true;
    }

    void OnGUI()
    {
      for (int i = 0; i < entries.Count; i++)
      {
        Entry entry = entries[i];
        if (entry.IsExpired())
        {
          continue;
        }
        using (new Util.GUILayoutFrobArea(entry.worldPos, 100, 100))
        {
          GUILayout.Label(entry.message);
        }
      }
    }
  }

  private static DrawerComponent ComponentInstance = null;

  public static void Draw(Vector3 worldPosition, string text)
  {
#if UNITY_EDITOR
    if (ComponentInstance == null)
    {
      GameObject go = new GameObject("__EDITOR_ONLY_DEBUG_LABEL_SINGLETON__");
      ComponentInstance = go.AddComponent<DrawerComponent>();
    }

    ComponentInstance.Add(new Entry { message = text, worldPos = worldPosition, frameAdded = Time.frameCount });
#endif
  }
}
