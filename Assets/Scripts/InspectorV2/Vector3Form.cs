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
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

public class Vector3Form : MonoBehaviour
{
  [SerializeField] TMPro.TMP_InputField xInput;
  [SerializeField] TMPro.TMP_InputField yInput;
  [SerializeField] TMPro.TMP_InputField zInput;

  public delegate void OnValueChanged(Vector3 position);
  private OnValueChanged onValueChanged = (p) => { };

  private bool xChanged = false;
  private bool yChanged = false;
  private bool zChanged = false;

  void Awake()
  {
    xInput.onValueChanged.AddListener((x) =>
    {
      if (xInput.isFocused) xChanged = true;
    });
    yInput.onValueChanged.AddListener((x) =>
    {
      if (yInput.isFocused) yChanged = true;
    });
    zInput.onValueChanged.AddListener((x) =>
    {
      if (zInput.isFocused) zChanged = true;
    });
    xInput.onEndEdit.AddListener((x) =>
    {
      if (xChanged)
      {
        OnInputChanged();
      }
      xChanged = false;
    });
    yInput.onEndEdit.AddListener((y) =>
    {
      if (yChanged)
      {
        OnInputChanged();
      }
      yChanged = false;
    });
    zInput.onEndEdit.AddListener((z) =>
    {
      if (zChanged)
      {
        OnInputChanged();
      }
      zChanged = false;
    });
  }

  public bool IsFocused()
  {
    return xInput.isFocused || yInput.isFocused || zInput.isFocused;
  }

  public void SetValue(Vector3 value)
  {
    if (!xInput.isFocused)
    {
      xInput.text = value.x.ToString();
    }
    if (!yInput.isFocused)
    {
      yInput.text = value.y.ToString();
    }
    if (!zInput.isFocused)
    {
      zInput.text = value.z.ToString();
    }
  }

  public void AddListener(OnValueChanged listener)
  {
    onValueChanged += listener;
  }

  public Vector3 GetValue()
  {
    float x = 0, y = 0, z = 0;
    float.TryParse(xInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out x);
    float.TryParse(yInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out y);
    float.TryParse(zInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out z);
    return new Vector3(x, y, z);
  }

  private void OnInputChanged()
  {
    onValueChanged(GetValue());
  }
}
