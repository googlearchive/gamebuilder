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

public class WindowHeaderUI : MonoBehaviour
{
  public RectTransform leftMenuLeft;
  public RectTransform leftMenuRight;
  public RectTransform leftMenuOverflow;

  public WindowHeaderSubmenuUI systemSubmenu;
  public UnityEngine.UI.Toggle systemToggle;

  public WindowHeaderSubmenuUI fileSubmenu;
  public UnityEngine.UI.Button newButton;
  public UnityEngine.UI.Toggle saveToggle;
  public UnityEngine.UI.Button openButton;
  public UnityEngine.UI.Toggle multiplayerToggle;

  public WindowHeaderSubmenuUI undoRedoSubmenu;
  public UnityEngine.UI.Button undoButton;
  public UnityEngine.UI.Button redoButton;

  public WindowHeaderSubmenuUI editSubmenu;
  public UnityEngine.UI.Button copyButton;
  public UnityEngine.UI.Toggle cameraFocusToggle;
  public UnityEngine.UI.Button deleteButton;
  public UnityEngine.UI.Toggle worldVisualsToggle;

  public WindowHeaderSubmenuUI moreSubmenu;
  public UnityEngine.UI.Toggle moreToggle;

  public UnityEngine.UI.Button viewButton;
  public UnityEngine.UI.Button rotateButton;

  public UnityEngine.UI.Toggle actorListToggle;
  public TMPro.TextMeshProUGUI actorListToggleText;

  public UnityEngine.UI.Button pauseButton;
  public UnityEngine.UI.Button resetButton;
  public UnityEngine.UI.Image pauseBackgroundImage;
  public UnityEngine.UI.Image undoImage;
  public UnityEngine.UI.Image redoImage;
  public UnityEngine.UI.Image saveButtonImage;
  public UnityEngine.UI.Image workshopButtonImage;

  public CanvasGroup[] editOnlyCanvasGroups;
  public UnityEngine.UI.Image playHeaderBackground;

  public GameObject buildButtonObject;
  public GameObject playButtonObject;
  public GameObject playOnlyObject;
}
