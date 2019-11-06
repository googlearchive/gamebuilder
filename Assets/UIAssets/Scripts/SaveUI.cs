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

public class SaveUI : MonoBehaviour
{
  public UnityEngine.UI.Button closeButton;
  public UnityEngine.UI.Image screenshotImage;
  public UnityEngine.UI.Button screenshotButton;
  public TMPro.TMP_InputField nameInput;
  public TMPro.TMP_InputField descriptionInput;
  public UnityEngine.UI.Button saveButton;
  public TMPro.TextMeshProUGUI saveButtonText;
  public UnityEngine.UI.Button newSaveButton;
  public UnityEngine.UI.Button workshopButton;
  public TMPro.TextMeshProUGUI workshopButtonText;


  public UnityEngine.UI.Button feedbackTextPrimaryButton;
  public TMPro.TextMeshProUGUI feedbackTextPrimary;
  public UnityEngine.UI.Button feedbackTextSecondaryButton;
  public TMPro.TextMeshProUGUI feedbackTextSecondary;
}
