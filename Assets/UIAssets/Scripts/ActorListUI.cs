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

public class ActorListUI : MonoBehaviour
{
  public GameObject actorListItemPrefab;

  public RectTransform onStageListParent;
  public RectTransform offStageListParent;

  public UnityEngine.UI.Button sortButton;
  public TMPro.TextMeshProUGUI sortButtonText;

  public UnityEngine.UI.Toggle hideCopiesToggle;

  public UnityEngine.UI.Button closeButton;
  public TMPro.TMP_InputField searchInput;
  public UnityEngine.UI.Button clearSearchButton;
}
