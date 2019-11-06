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

public class CardLibraryUI : MonoBehaviour
{
  public UnityEngine.UI.ScrollRect libraryScrollRect;
  public RectTransform libraryViewport;
  public RectTransform libraryContainer;
  public TMPro.TMP_InputField inputField;
  public TMPro.TMP_Dropdown categoryDropdown;
  public UnityEngine.UI.Button clearSearchButton;
  public UnityEngine.UI.Button closeButton;
  public GameObject selectionModePrompt;
  public UnityEngine.UI.Button selectionCancelButton;
  public UnityEngine.UI.Button selectionDoneButton;
  public ButtonDropdownMenu exportDropdown;
  public ButtonDropdownMenu importDropdown;
  public UnityEngine.UI.Button importButton;
  public UnityEngine.UI.Button exportButton;

}
