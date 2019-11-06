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

using CommandTerminal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using SD = System.Diagnostics;

public class GraphicsConsoleCommands
{
  public static void Log(string message)
  {
    Util.Log(message);
    CommandTerminal.HeadlessTerminal.Buffer.HandleLog(message, TerminalLogType.Message, null);
  }

  [RegisterCommand(Help = "")]
  static void CommandTexLimit(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"{QualitySettings.masterTextureLimit}");
      return;
    }
    int lim = args[0].Int;
    QualitySettings.masterTextureLimit = lim;
  }

  [RegisterCommand(Help = "")]
  static void CommandShaderLod(CommandArg[] args)
  {
    if (args.Length == 0)
    {
      Log($"{Shader.globalMaximumLOD}");
      return;
    }
    int lod = args[0].Int;
    Shader.globalMaximumLOD = lod;
  }

  [RegisterCommand(Help = "Show all lights in scene")]
  static void CommandLights(CommandArg[] args)
  {
    int toggleId = -1;
    if (args.Length > 0)
    {
      toggleId = args[0].Int;
    }
    int count = 0;
    foreach (Light light in GameObject.FindObjectsOfType<Light>())
    {
      Log($"{count}. {light.name}, type={light.type}, shadows={light.shadows}, rmode={light.renderMode}");

      if (count == toggleId)
      {
        light.enabled = !light.enabled;
        Log($"Toggled previous light to: {light.enabled}");
      }

      count++;
    }
  }

  [RegisterCommand(Help = "")]
  static void CommandTerrainMipBias(CommandArg[] args)
  {
    var terrain = GameObject.FindObjectOfType<TerrainSystem>();

    if (args.Length == 0)
    {
      Log($"{terrain.GetMipBias()}");
      return;
    }

    terrain.SetMipBias(args[0].Float);
  }

  [RegisterCommand(Help = "")]
  static void CommandTerrainAniso(CommandArg[] args)
  {
    var terrain = GameObject.FindObjectOfType<TerrainSystem>();

    if (args.Length == 0)
    {
      Log($"{terrain.GetAnisoLevel()}");
      return;
    }

    terrain.SetAnisoLevel(args[0].Int);
  }

  [RegisterCommand(Help = "")]
  static void CommandTerrainTex(CommandArg[] args)
  {
    int size = args[0].Int;
    var terrain = GameObject.FindObjectOfType<TerrainSystem>();
    terrain.CreateTextureArrays(true, size);
    Log($"terrain textures set to {size}");
  }

  [RegisterCommand(Help = "")]
  static void CommandRenderPath(CommandArg[] args)
  {
    bool wasDeferred = Camera.main.renderingPath == RenderingPath.DeferredLighting;
    Camera.main.renderingPath = wasDeferred ? RenderingPath.Forward : RenderingPath.DeferredLighting;
    Log($"{Camera.main.actualRenderingPath}");
  }
}