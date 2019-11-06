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
using UnityEngine.UI;

// Does a screen takeover in case of exceptions or assertion failures.
// Also known as BSOD - Black Screen Of Death.
public class GlobalExceptionHandler : MonoBehaviour
{
  [SerializeField] Canvas errorCanvas;
  [SerializeField] TMPro.TMP_Text errorMessage;
  [SerializeField] Button errorQuitButton;
  [SerializeField] Button errorCopyButton;
  [SerializeField] Button loadAutosaveButton;
  [SerializeField] TMPro.TMP_Text autosaveStatusText;

  private DynamicPopup popups;
  private GameBuilderSceneController scenes;
  private AutoSaveController autosaves;
  private GameBuilder.GameBundleLibrary gameBundleLibrary;
  private string autosaveId;

  private bool errorScreenEnabled = true;

  // Don't BSOD nor GA-log any errors if we're closing. Yeah, a ton of errors
  // happen, but this has no user-visible effect, so we're gonna ignore them for
  // now.
  bool sceneClosing = false;
  float sceneClosingStart = 0f;

  private Queue<string> latestErrors = new Queue<string>();
  private const int QUEUE_MAX_LENGTH = 20;

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref scenes);
    Util.FindIfNotSet(this, ref autosaves);
    Util.FindIfNotSet(this, ref gameBundleLibrary);

    Application.logMessageReceived += LogCallback;
    errorQuitButton.onClick.AddListener(OnQuitClicked);
    errorCopyButton.onClick.AddListener(OnCopyClicked);
    loadAutosaveButton.onClick.AddListener(OnRecoverAutosaveClicked);
    errorCanvas.gameObject.SetActive(false);
  }

  public void NotifySceneClosing()
  {
    if (sceneClosing) return;

    // Closing time....
    sceneClosing = true;
    sceneClosingStart = Time.realtimeSinceStartup;
    Util.Log($"OK got NotifySceneClosing - t = {Time.realtimeSinceStartup}");
  }

  void Update()
  {
    // Safe-guard against bad calls to NotifySceneClosing. If someone told us
    // we're closing..but it has been 3s and we're still around...we didn't
    // really close did we?
    if (sceneClosing && (Time.realtimeSinceStartup - sceneClosingStart) > 3f)
    {
      throw new System.Exception("Someone said the scene was closing, but it has been 3s since - please don't call NotifySceneClosing unless we're actually cloinsg!");
    }
  }

  public bool IsShowing()
  {
    return errorCanvas.gameObject.activeSelf;
  }

  void OnDestroy()
  {
    Application.logMessageReceived -= LogCallback;
  }

  private void LogCallback(string condition, string stackTrace, LogType type)
  {
    if (sceneClosing)
    {
      return;
    }

    if ((type == LogType.Exception || type == LogType.Assert) && !errorCanvas.gameObject.activeSelf)
    {
      string message = "";
      while (latestErrors.Count > 0)
      {
        message = latestErrors.Dequeue() + "\n\n=======\n\n" + message;
      }
      message = condition + "\n\n" + stackTrace + "\n\n=======\nMOST RECENT ERRORS:\n\n" + message;
      errorMessage.text = message;

      if (errorScreenEnabled)
      {
        errorCanvas.gameObject.SetActive(true);
        gameObject.transform.SetAsLastSibling();
        autosaveId = autosaves.GetLastAutosaveId();
        loadAutosaveButton.interactable = autosaveId != null;
        autosaveStatusText.text = autosaveId != null ?
           ("(saved " + Mathf.RoundToInt(Time.unscaledTime - autosaves.GetLastAutosaveTime()) + "s ago)") :
           "No autosave available";
        KillSound();
      }
      else
      {
        Debug.LogError("BSOD disabled. Would have shown error: " + message);
      }

      // Only log stackTrace, as condition can contain sensitive info.
    }
    else if (type == LogType.Error)
    {
      latestErrors.Enqueue(condition + "\n" + stackTrace);
      if (latestErrors.Count > QUEUE_MAX_LENGTH) latestErrors.Dequeue();
    }
  }

  private void KillSound()
  {
    try
    {
      UserMain userMain = GameObject.FindObjectOfType<UserMain>();
      if (userMain != null)
      {
        userMain.TemporarilyKillMusicAndSfx();
      }
    }
    catch (System.Exception) { }
  }

  private void OnQuitClicked()
  {
    // Hack: if Ctrl + Shift is held, then just dismiss the error.
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift))
    {
      errorCanvas.gameObject.SetActive(false);
      return;
    }
    scenes.LoadSplashScreen();
    //Application.Quit();
    //#if UNITY_EDITOR
    //UnityEditor.EditorApplication.isPlaying = false;
    //#endif
  }

  private void OnRecoverAutosaveClicked()
  {
    if (autosaveId == null) return; // Shouldn't happen
    scenes.RestartAndLoadLibraryBundle(gameBundleLibrary.GetBundleEntry(autosaveId), new GameBuilderApplication.PlayOptions());
  }

  private void OnCopyClicked()
  {
    GUIUtility.systemCopyBuffer = errorMessage.text.Replace("\r", "").Replace("\n", "\r\n"); // make it Windows-friendly
  }

  [CommandTerminal.RegisterCommand(Help = "Enables or disables BSOD (error screen)")]
  public static void CommandBsod(CommandTerminal.CommandArg[] args)
  {
    GlobalExceptionHandler instance = GameObject.FindObjectOfType<GlobalExceptionHandler>();
    if (args.Length > 0)
    {
      if (args[0].ToString() == "on")
      {
        instance.errorScreenEnabled = true;
      }
      else if (args[0].ToString() == "off")
      {
        instance.errorScreenEnabled = false;
      }
      else
      {
        Debug.LogError("Invalid argument. Argument must be 'on' or 'off'.");
      }
    }
    CommandTerminal.HeadlessTerminal.Log("BSOD " + (instance.errorScreenEnabled ? "on" : "off"));
  }

  [CommandTerminal.RegisterCommand(Help = "Crashes game.")]
  public static void CommandCrash(CommandTerminal.CommandArg[] args)
  {
    throw new System.Exception("Command-triggered crash. " + (args.Length > 0 ? args[0].ToString() : ""));
  }
}
