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

public class ToolMemory : MonoBehaviour
{

  public struct MoveToolMemory
  {
    public bool showSettings;
    public bool showOffsets;
    public bool snapping;
    public bool autosetSpawn;
  }

  public struct RotateToolMemory
  {
    public bool showSettings;
    public bool showOffsets;
    public bool snapping;
    public bool autosetSpawn;
    public int axis;
  }

  public struct ConstructionToolMemory
  {
    public int typeIndex;
    public int styleIndex;
    public int colorIndex;
    public TerrainManager.BlockDirection direction;

    public ConstructionToolMemory(int _typeIndex, int _styleIndex, int _colorIndex, TerrainManager.BlockDirection _direction)
    {
      typeIndex = _typeIndex;
      styleIndex = _styleIndex;
      colorIndex = _colorIndex;
      direction = _direction;
    }
  };

  public ConstructionToolMemory constructionToolMemory;
  public MoveToolMemory moveToolMemory = new MoveToolMemory
  {
    showSettings = false,
    showOffsets = false,
    snapping = false,
    autosetSpawn = true
  };

  public RotateToolMemory rotateToolMemory = new RotateToolMemory
  {
    showSettings = false,
    showOffsets = false,
    snapping = false,
    autosetSpawn = true,
    axis = 0
  };

  public VoosActor selectedActor = null;
  public int inspectorTabIndex = 0;
  public string lastTool = null;
  public VoosActor copyPasteActor = null;

  public int logicTabIndex = 0;
  public VoosActor logicActor = null;
  public bool logicSidebarClosed = false;

  public VoosActor infoActor = null;

  public Dictionary<System.Type, int> subtoolbarDictionary = new Dictionary<System.Type, int>();
  public int RequestSubtoolbarIndex(System.Type type)
  {
    if (subtoolbarDictionary.ContainsKey(type))
    {
      return subtoolbarDictionary[type];
    }
    else
    {
      return 0;
    }
  }
  public void SetSubtoolbarIndex(System.Type type, int index)
  {
    subtoolbarDictionary[type] = index;

  }

}
