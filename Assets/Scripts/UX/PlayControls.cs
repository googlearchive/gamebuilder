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

public class PlayControls : MonoBehaviour
{
  public const KeyCode PlayPauseKey = KeyCode.P;

  [SerializeField] WindowHeaderUI windowHeaderUI;
  [SerializeField] Color pauseColor;
  [SerializeField] Color playColor;

  VoosEngine voosEngine;

  string whenPause = $"Play\n(Ctrl+{PlayPauseKey})";
  string whenPlay = $"Pause\n(Ctrl+{PlayPauseKey})";

  void Awake()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    windowHeaderUI.pauseButton.onClick.AddListener(OnPlayPauseToggle);
    windowHeaderUI.resetButton.onClick.AddListener(OnReset);
  }

  void Update()
  {
    windowHeaderUI.pauseBackgroundImage.color = GetIsPaused() ? pauseColor : playColor;
  }

  void OnPlayPauseToggle()
  {
    SetIsPaused(!GetIsPaused());
  }

  void OnReset()
  {
    voosEngine.ResetGame();
  }

  bool GetIsPaused()
  {
    return !voosEngine.GetIsRunning();
  }

  void SetIsPaused(bool on)
  {
    voosEngine.SetIsRunning(!on);
  }
}
