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

using UnityEngine;
using UnityEngine.UI;

public class PersistentMessagePanel : MonoBehaviour
{
  const float OPACITY = 0.6f;

  [SerializeField] GameObject panel;
  [SerializeField] Image backgroundImage;
  [SerializeField] TMPro.TMP_Text messageText;

  public void Show(string message)
  {
    messageText.text = message;
    panel.SetActive(true);
  }

  public void Hide()
  {
    panel.SetActive(false);
  }

  void Update()
  {
    if (panel.activeSelf)
    {
      backgroundImage.color = Time.unscaledTime % 1 > 0.5 ? new Color(0, 0, 0, OPACITY) : new Color(0.5f, 0, 0, OPACITY);
    }
  }
}
