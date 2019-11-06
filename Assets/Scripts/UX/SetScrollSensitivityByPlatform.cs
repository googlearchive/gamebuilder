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

public class SetScrollSensitivityByPlatform : MonoBehaviour
{
  private float WindowsSensitivity = 30f;
  private float MacSensitivity = 1f;

  public void Awake()
  {
    UnityEngine.UI.ScrollRect scrollRect = GetComponent<UnityEngine.UI.ScrollRect>();
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
      scrollRect.scrollSensitivity = MacSensitivity;
#endif
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    scrollRect.scrollSensitivity = WindowsSensitivity;
#endif
  }
}