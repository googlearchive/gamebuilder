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
using System.IO;
using GameBuilder;

// Periodically saves the current scene into a rotating set of slots.
public class AutoSaveController : MonoBehaviour
{
#if UNITY_EDITOR
  float periodSeconds = 60f;
#else
  float periodSeconds = 60f;
#endif

  static string AutoSaveBundleIdPrefix = "autosave-";

  string lastAutosaveId = null;
  float lastAutosaveTime = 0;

  public static bool IsAutosave(string bundleId)
  {
    return !bundleId.IsNullOrEmpty() && bundleId.Contains(AutoSaveBundleIdPrefix);
  }

  public static bool IsAutosave(GameBundleLibrary.Entry entry)
  {
    return IsAutosave(entry.id);
  }

  int maxSlots = 10;
  GameBundleLibrary bundleLibrary;
  SaveLoadController saveLoad;

  [SerializeField] HudNotifications notes;
  DynamicPopup popups;

  int nextSlot = -1;

  bool paused = false;

  string[] slotPathsCache = null;

  void Awake()
  {
    Util.FindIfNotSet(this, ref bundleLibrary);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref saveLoad);
  }

  void Start()
  {
    Util.UpgradeUserDataDir();
    nextSlot = GetFreeSlotOrOldestExisting();
    StartCoroutine(SavingCoroutine());
  }

  public string GetLastAutosaveId()
  {
    return lastAutosaveId;
  }

  public float GetLastAutosaveTime()
  {
    return lastAutosaveTime;
  }

  string GetSlotBundleId(int slot)
  {
    return $"{AutoSaveBundleIdPrefix}{slot}";
  }

  int GetFreeSlotOrOldestExisting()
  {
    using (new Util.ProfileBlock("GetFreeSlotOrOldestExisting"))
    {
      int oldest = -1;
      System.DateTime oldestWriteTime = System.DateTime.Now;

      for (int i = 0; i < maxSlots; i++)
      {
        string bundleId = GetSlotBundleId(i);
        GameBundle bundle = bundleLibrary.GetBundle(bundleId);
        string voosPath = bundle.GetVoosPath();

        if (!File.Exists(voosPath))
        {
          return i;
        }

        System.DateTime writeTime = System.IO.File.GetLastWriteTime(voosPath);
        if (oldest == -1 || writeTime < oldestWriteTime)
        {
          oldest = i;
          oldestWriteTime = writeTime;
        }
      }

      return oldest;
    }
  }

  void DoAutosave(System.Action<string> onComplete)
  {
    using (Util.Profile("autosave"))
    {
      var sw = new System.Diagnostics.Stopwatch();
      sw.Restart();

      string destId = GetSlotBundleId(nextSlot);
      nextSlot = (nextSlot + 1) % maxSlots;

      // SaveMaybeOverwrite doesn't delete all files. Like thumbnails, if any.
      using (Util.Profile("deleteSlotBundle"))
      {
        if (bundleLibrary.BundleExists(destId))
        {
          bundleLibrary.DeletePermanently(destId);
        }
      }

      GameBundle.Metadata meta = new GameBundle.Metadata
      {
        name = $"{System.DateTime.Now} Autosave",
        description = $""
      };

      // Copy from the active bundle so our autosave looks like it.
      string srcId = GameBuilderApplication.ActiveBundleId;
      using (Util.Profile("CopyBundle"))
      {
        if (srcId != null && bundleLibrary.BundleExists(srcId))
        {
          var srcBundle = bundleLibrary.GetBundle(srcId);
          meta = srcBundle.GetMetadata();
          meta.name += $" ({System.DateTime.Now} Autosave)";

          bundleLibrary.CopyBundle(srcId, destId);
        }
      }

      using (Util.Profile("SaveBundle"))
      {
        // Now overwrite just the scene.voos file
        bundleLibrary.SaveMaybeOverwrite(saveLoad, destId, meta, null, () =>
        {
          sw.Stop();
          Util.Log($"autosave took total of {sw.ElapsedMilliseconds}ms");
          // TODO copy the steam workshop meta data too, if it's there.

          lastAutosaveId = destId;
          lastAutosaveTime = Time.unscaledTime;
          onComplete?.Invoke(destId);
        });
      }
    }
  }

  IEnumerator SavingCoroutine()
  {
    while (true)
    {
      yield return new WaitForSecondsRealtime(periodSeconds);

      // Don't autosave during recovery mode. You might be overwriting an
      // autosave that the user wants to recover!
      if (this.isActiveAndEnabled && !GameBuilderApplication.IsRecoveryMode && !paused)
      {
        try
        {
          DoAutosave(bundleId =>
          {
            // HACK HACK. Only because CMS is tied to player panels, when it really should be global.
            if (notes == null)
            {
              notes = FindObjectOfType<HudNotifications>();
            }

            if (notes != null)
            {
              notes.AddMessage($"Auto-saved!");
            }
          });
        }
        catch (System.Exception e)
        {
          popups.ShowTwoButtons(
            $"Woops! Failed to autosave due to error:\n{e.Message}\nWe will disable autosaves until you restart Game Builder. Apologies for the error!",
            "OK", () => { },
            "Copy error details", () => { Util.CopyToUserClipboard(e.ToString()); },
            800f);
          SetPaused(true);
        }
      }
    }
  }

#if UNITY_EDITOR
  void Update()
  {
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.P))
    {
      TriggerAutosave(id =>
      {
        Util.Log($"autosaved to {id}");
      });
    }
  }
#endif

  public void SetPaused(bool paused)
  {
    this.paused = paused;
  }

  public void TriggerAutosave(System.Action<string> onComplete)
  {
    DoAutosave(onComplete);
  }
}
