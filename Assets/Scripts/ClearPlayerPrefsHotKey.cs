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

public class ClearPlayerPrefsHotKey : MonoBehaviour
{
  float holdTime = 0f;

  DynamicPopup popups;

  void Awake()
  {
    Util.FindIfNotSet(this, ref popups);
  }

  // Update is called once per frame
  void Update()
  {
    if (Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.End))
    {
      holdTime += Time.deltaTime;
    }
    else
    {
      holdTime = 0f;
    }

    if (holdTime > 2f)
    {
      var buttons = new List<PopupButton.Params>();
      buttons.Add(new PopupButton.Params
      {
        getLabel = () => "Clear and Quit",
        onClick = () =>
        {
          PlayerPrefs.DeleteAll();
          PlayerPrefs.Save();
          // Assume whoever does this is a developer.
        }
      });
      popups.Show(new DynamicPopup.Popup { getMessage = () => "Clear all player prefs?", buttons = buttons });
    }
  }
}
