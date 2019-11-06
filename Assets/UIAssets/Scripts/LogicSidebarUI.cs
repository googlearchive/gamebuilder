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

public class LogicSidebarUI : MonoBehaviour
{
    public Color primaryColor;
    public Color darkColor;
    public TMPro.TextMeshProUGUI label;
    public GameObject[] hideWhenCollapsed;
    public UnityEngine.UI.Button cardButton;
    public UnityEngine.UI.Image cardButtonImage;
    public UnityEngine.UI.Image cardButtonBackground;
    public TMPro.TextMeshProUGUI cardButtonText;
    public UnityEngine.UI.Button codeButton;
    public UnityEngine.UI.Image codeButtonImage;
    public UnityEngine.UI.Image codeButtonBackground;
    public TMPro.TextMeshProUGUI codeButtonText;
    public UnityEngine.UI.Button libraryButton;
    public UnityEngine.UI.Image libraryButtonBackground;
    public UnityEngine.UI.Image libraryButtonImage;
    public TMPro.TextMeshProUGUI libraryButtonText;
}
