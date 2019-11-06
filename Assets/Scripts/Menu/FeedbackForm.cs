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

public class FeedbackForm : MonoBehaviour
{
  [SerializeField] TMPro.TMP_InputField inputField;
  [SerializeField] UnityEngine.UI.Button sendInput;
  [SerializeField] UnityEngine.UI.Button cancelButton;
  [SerializeField] GameObject mainObject;

  public void Start()
  {
    inputField.onEndEdit.AddListener(OnEndEdit);
    sendInput.onClick.AddListener(() => SendInput(inputField.text));
    cancelButton.onClick.AddListener(Close);
  }

  void OnEndEdit(string inputstring)
  {
    if (Input.GetButtonDown("Submit"))
    {
      SendInput(inputstring);
    }
  }

  public IEnumerator PostToForm(string input)
  {
    // TODO: implement a mechanism to send feedback, if you want to.
    Debug.LogError("Feedback form not implemented.");
    yield break;
  }

  void SendInput(string inputstring)
  {
    if (inputstring == "") return;

    StartCoroutine(PostToForm(inputstring));
    Close();
  }

  public void Close()
  {
    Destroy(mainObject);
  }
}
