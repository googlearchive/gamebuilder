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

public class TerrainToolSettingsUI : MonoBehaviour
{
  public ImageToggleUI textureTogglePrefab;
  public RectTransform textureParent;
  public UnityEngine.UI.ToggleGroup textureToggleGroup;
  public UnityEngine.UI.ScrollRect scrollRect;

  public TMPro.TextMeshProUGUI toolModeText;
  public TMPro.TextMeshProUGUI shapeHeaderText;
  public TMPro.TextMeshProUGUI styleHeaderText;
  public TMPro.TextMeshProUGUI rotateButtonText;

  public UnityEngine.UI.Toggle[] modeToggles;
  public UnityEngine.UI.Toggle[] shapeToggles;

  public UnityEngine.UI.Button selectCopy;
  public UnityEngine.UI.Button selectPaint;
  public UnityEngine.UI.Button selectDelete;

  public GameObject editSettingsObject;
  public GameObject shapeSettingsObject;
  public GameObject styleSettingsObject;

  public UnityEngine.UI.Button rotateButton;

  public UnityEngine.UI.Button importStyleButton;
  public UnityEngine.UI.Button deleteStyleButton;
  public AssetPreImportDialogUI assetPreImportDialog;

  public TMPro.TMP_Text statusText;
}
