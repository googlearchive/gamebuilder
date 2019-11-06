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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Behaviors;
using System.IO;
using BehaviorProperties;

public partial class BehaviorCards
{
  class PanelCommon : CardPanel.IPanelCommon
  {
    static string PlaceHolderIconResPath = "BuiltinAssets/PanelIcons/trophy-icon";

    UnassignedBehavior item;

    public PanelCommon(UnassignedBehavior item)
    {
      this.item = item;
    }

    PanelMetadata.Data GetMetadata()
    {
      return JsonUtility.FromJson<PanelMetadata>(item.GetMetadataJson()).cardSystemPanelData;
    }
    public string GetDescription() { return GetMetadata().description; }
    public Color GetColor() { return GetMetadata().color; }
    public string GetTitle() { return GetMetadata().title; }

    public Sprite GetIcon()
    {
      string resPath = GetMetadata().iconResourcePath.OrDefault(PlaceHolderIconResPath);
      return Resources.Load(resPath, typeof(Sprite)) as Sprite;
    }
  }
}