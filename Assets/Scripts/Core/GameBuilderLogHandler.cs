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
using CommandTerminal;
using System;
using System.Linq;

// TODO rename this to like, console feeder. Its main job is to relay messages
// from systems to the console.
public class GameBuilderLogHandler : MonoBehaviour
{
  [SerializeField] HudNotifications hudMessageSystem;
  [SerializeField] VoosEngine voosEngine;
  [SerializeField] BehaviorSystem behaviorSystem;
  [SerializeField] DynamicPopup popup;

  public System.Action<string, string, VoosEngine.BehaviorLogItem> onDisplayCodeError;

  public bool showUnityLogsWarningsInConsole = false;

  float lastErrorPopupTime = 0f;

  // For message flooding detection:
  // How many log messages we have received in the 1 second interval [floodSecond,floodSecond+1[
  int floodCount;
  int floodSecond;
  const int FLOOD_THRESHOLD = 100; // Messages/second.
  float lastFloodNotifyTime = 0f;

  EditMain editMain = null;

  bool IsCodeEditorOpen()
  {
    if (editMain == null)
    {
      editMain = FindObjectOfType<EditMain>();
    }

    if (editMain != null)
    {
      return editMain.IsCodeViewOpen();
    }
    else
    {
      return false;
    }
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref hudMessageSystem);
    Util.FindIfNotSet(this, ref behaviorSystem);
    Util.FindIfNotSet(this, ref popup);

    voosEngine.onBehaviorException += HandleBehaviorException;
    voosEngine.onBehaviorLogMessage += HandleBehaviorLogMessage;
    voosEngine.OnModuleCompileError += HandleModuleCompileError;
  }

  void OnDestroy()
  {
    voosEngine.onBehaviorException -= HandleBehaviorException;
    voosEngine.onBehaviorLogMessage -= HandleBehaviorLogMessage;
    voosEngine.OnModuleCompileError -= HandleModuleCompileError;
  }

  private void HandleModuleCompileError(VoosEngine.ModuleCompileError error)
  {
    string behaviorUri = error.moduleKey;

    var beh = behaviorSystem.GetBehaviorData(behaviorUri);
    var meta = JsonUtility.FromJson<BehaviorCards.CardMetadata>(beh.metadataJson);

    HashSet<string> usingBrainIds = new HashSet<string>(from entry in behaviorSystem.BrainsForBehavior(behaviorUri)
                                                        select entry.id);

    VoosActor oneActor = (from actor in voosEngine.EnumerateActors()
                          where usingBrainIds.Contains(actor.GetBrainName())
                          select actor).FirstOrDefault();

    string actorsUsing = oneActor == null ? "No actors using it" : $"One actor using it: {oneActor.GetDebugName()}";
    string msg = $"<color=yellow>Error with card '{meta.cardSystemCardData.title}' (line {error.lineNum}). {actorsUsing}. The error:</color>\n<color=red>{error.message}</color>";
    CommandTerminal.HeadlessTerminal.Buffer.HandleLog(msg, TerminalLogType.Error, null);

    // NOTE: Ideally, we'd do this if we know the code editor isn't viewing this particular behavior
    if (Time.timeSinceLevelLoad < 5f && (!IsCodeEditorOpen() || error.lineNum == -1))
    {
      popup.Show($"There was an error with card '{meta.cardSystemCardData.title}' (line {error.lineNum}):\n{error.message}\n<color=#666666>{actorsUsing}.</color>", "OK", null, 1400f);
    }
  }

  string GetBehaviorTitle(string brainId, string useId)
  {
    var uri = behaviorSystem.GetBrain(brainId).GetUse(useId).behaviorUri;
    var beh = behaviorSystem.GetBehaviorData(uri);

    var behaviorDesc = "";

    if (BehaviorCards.IsCard(beh))
    {
      var md = BehaviorCards.CardMetadata.GetMetaDataFor(beh);
      behaviorDesc = $"{md.title}";
    }
    else if (BehaviorCards.IsPanel(beh))
    {
      var md = BehaviorCards.PanelMetadata.Get(beh);
      behaviorDesc = $"{md.title}";
    }
    return behaviorDesc;
  }

  private void HandleBehaviorLogMessage(VoosEngine.BehaviorLogItem item)
  {
    UpdateFloodDetection();
    if (item.message.Contains("ERROR: "))
    {
      MaybeNotifyUserOfBehaviorError(item);
    }
    VoosActor actor = voosEngine.GetActor(item.actorId);
    string behDesc = GetBehaviorTitle(actor.GetBrainName(), item.useId);
    CommandTerminal.HeadlessTerminal.Buffer.HandleLog($"<color=#666666>[{actor.GetDisplayName()} '{behDesc}' on{item.messageName}:{item.lineNum}]</color> <color=white>{item.message}</color>", TerminalLogType.Message, null);
  }

  private void HandleBehaviorException(VoosEngine.BehaviorLogItem item)
  {
    MaybeNotifyUserOfBehaviorError(item);
    VoosActor actor = voosEngine.GetActor(item.actorId);
    string behDesc = GetBehaviorTitle(actor.GetBrainName(), item.useId);
    CommandTerminal.HeadlessTerminal.Buffer.HandleLog($"<color=yellow>[{actor.GetDisplayName()} '{behDesc}' on{item.messageName}:{item.lineNum}]</color> <color=red>{item.message}</color>", TerminalLogType.Error, null);
  }

  void UpdateFloodDetection()
  {
    int thisSecond = (int)(Time.unscaledTime);
    if (thisSecond != floodSecond)
    {
      floodSecond = thisSecond;
      floodCount = 0;
    }
    ++floodCount;
    if (floodCount > FLOOD_THRESHOLD && Time.unscaledTime - lastFloodNotifyTime > 5f)
    {
      floodCount = 0;
      lastFloodNotifyTime = Time.unscaledTime;
      hudMessageSystem.AddMessage("<color=red>TOO MANY MESSAGES!</color> Printing >" + FLOOD_THRESHOLD + "/second. <color=green>tilde (~)</color> to view.");
    }
  }

  void MaybeNotifyUserOfBehaviorError(VoosEngine.BehaviorLogItem item)
  {
    // Errors are a big deal - quietly failing just leads to further confusion.
    // So, we're just gonna do popups.
    if (Time.unscaledTime - lastErrorPopupTime > 3f && (!IsCodeEditorOpen() || item.lineNum == -1))
    {
      lastErrorPopupTime = Time.unscaledTime;

      var brainId = voosEngine.GetActor(item.actorId).GetBrainName();

      var uri = behaviorSystem.GetBrain(brainId).GetUse(item.useId).behaviorUri;

      VoosActor actor = voosEngine.GetActor(item.actorId);
      string behDesc = GetBehaviorTitle(actor.GetBrainName(), item.useId);
      var niceMsg = $"Script error for actor '{actor.GetDisplayName()}' from card '{behDesc}':\n{item.message}";
      string fullMessage = $"{niceMsg}\n<color=yellow>'on{item.messageName}' will be disabled until the script is edited or the game is reset.</color>\nYou may want to pause the game if the error is repeating.";
      onDisplayCodeError?.Invoke(fullMessage, uri, item);
    }
  }
}
