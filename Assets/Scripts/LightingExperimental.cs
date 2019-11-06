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

public class LightingExperimental : MonoBehaviour
{
#if UNITY_EDITOR
  [CommandTerminal.RegisterCommand(Help = "Test lighting")]
  public static void CommandMidnight(CommandTerminal.CommandArg[] args)
  {
    foreach (Light light in GameObject.FindObjectsOfType<Light>())
    {
      if (light.type == LightType.Directional)
      {
        light.intensity = 0.2f;
      }
      else if (light.type == LightType.Point)
      {
        GameObject.Destroy(light);
      }
    }
    RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.2f);
    GameObject.FindObjectOfType<GameBuilderStage>().SetSkyColor(Color.black);
    foreach (VoosActor actor in GameObject.FindObjectsOfType<VoosActor>())
    {
      if ((actor.GetDisplayName() ?? "").ToLowerInvariant().Contains("lantern") || actor.HasTag("light"))
      {
        GameObject obj = new GameObject();
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 1.2f;
        light.range = 10 * actor.GetLocalScale().x;
        light.color = actor.GetTint();
        obj.transform.SetParent(actor.gameObject.transform, false);
        obj.transform.localPosition = Vector3.up * 0.5f;
      }
    }
    foreach (Camera camera in GameObject.FindObjectsOfType<Camera>())
    {
      camera.renderingPath = RenderingPath.DeferredLighting;
    }
  }
#endif
}
