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

public class VisualEffectsOptions : Sidebar
{
  UserMain userMain;
  [SerializeField] UnityEngine.UI.Toggle FXToggleFab;
  [SerializeField] RectTransform FXparent;

  string[] FXnames = new string[] {
    "Pixelate",
    "Film",
    "B&W",
    "Saturate",
    "VHS",
    "Krazy",
    "Drawing",
  };

  string[] FXscripts = new string[] {
    "CameraFilterPack_Pixel_Pixelisation",
    "CameraFilterPack_TV_Old_Movie",
    "CameraFilterPack_Color_GrayScale",
    "CameraFilterPack_Color_BrightContrastSaturation",
    "CameraFilterPack_TV_VHS",
    "CameraFilterPack_FX_Psycho",
    "CameraFilterPack_Drawing_Paper3"
  };

  [SerializeField] UnityEngine.UI.Button closeButton;
  public override void Setup(SidebarManager _sidebarManager)
  {
    base.Setup(_sidebarManager);
    Util.FindIfNotSet(this, ref userMain);
    closeButton.onClick.AddListener(RequestClose);

    for (int i = 0; i < FXnames.Length; i++)
    {
      UnityEngine.UI.Toggle newtoggle = Instantiate(FXToggleFab, FXparent).GetComponent<UnityEngine.UI.Toggle>();
      string scriptname = FXscripts[i];
      newtoggle.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = FXnames[i];
      newtoggle.onValueChanged.AddListener((on) => ToggleFX(on, scriptname));
    }

    Destroy(FXToggleFab.gameObject);
  }

  void ToggleFX(bool on, string s)
  {
    if (on)
    {
      userMain.AddCameraEffect(s);
    }
    else
    {
      userMain.RemoveCameraEffect(s);
    }
  }

}
