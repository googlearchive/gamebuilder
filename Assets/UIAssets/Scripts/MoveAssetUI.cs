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

public class MoveAssetUI : MonoBehaviour
{
  public GameObject currentFrame;
  public GameObject[] parentFrames;
  public GameObject spawnFrame;
  public GameObject offsetFrame;
  public GameObject noSelection;
  public GameObject multiSelection;
  public GameObject pickerOverlay;

  public TMPro.TextMeshProUGUI header;
  public TMPro.TextMeshProUGUI positionLabel;
  public TMPro.TextMeshProUGUI spawnLabel;
  public TMPro.TextMeshProUGUI offsetLabel;

  public UnityEngine.UI.Button currentParentButton;
  public TMPro.TextMeshProUGUI currentParentButtonText;
  public UnityEngine.UI.Button restartParentButton;
  public TMPro.TextMeshProUGUI restartParentButtonText;

  public UnityEngine.UI.Toggle snapToggle;
  public UnityEngine.UI.Toggle settingsToggle;
  public UnityEngine.UI.Toggle offsetsToggle;
  public UnityEngine.UI.Toggle updateSpawnOnMoveToggle;
  public UnityEngine.UI.Toggle localSpaceToggle;

  public UnityEngine.UI.Button setSpawnToCurrent;
  public TMPro.TMP_InputField[] currentInputs;
  public TMPro.TMP_InputField[] spawnInputs;
  public TMPro.TMP_InputField[] offsetInputs;
}
