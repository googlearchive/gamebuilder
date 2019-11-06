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
using URP = UnityEngine.Rendering.PostProcessing;

public class SnapshotCamera : MonoBehaviour
{
  [SerializeField] RenderTexture actorTexture;
  [SerializeField] RenderTexture gameViewTexture;
  [SerializeField] Camera targetCamera;

  UserMain userMain;
  URP.PostProcessVolume post;

  public Texture2D SnapshotActor(VoosActor actor)
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref post);
    PositionCameraForActor(actor);
    RenderTexture currentRT = RenderTexture.active;

    Texture2D screenshotTexture = new Texture2D(actorTexture.width, actorTexture.height);

    targetCamera.targetTexture = actorTexture;
    RenderTexture.active = actorTexture;
    targetCamera.Render();

    screenshotTexture.ReadPixels(new Rect(0, 0, actorTexture.width, actorTexture.height), 0, 0);
    screenshotTexture.Apply();

    RenderTexture.active = currentRT;
    return screenshotTexture;
  }

  void PositionCameraForActor(VoosActor actor)
  {
    // get bounding box of actor
    Bounds targetBounds = new Bounds(actor.transform.position, Vector3.zero);
    foreach (Renderer rend in actor.transform.GetComponentsInChildren<Renderer>())
    {
      targetBounds.Encapsulate(rend.bounds);
    }

    //very primitive positioning
    targetCamera.transform.position = targetBounds.center + (new Vector3(1, 1, 1)).normalized * targetBounds.size.magnitude;
    targetCamera.transform.LookAt(targetBounds.center);
  }

  public Texture2D SnapshotGameView()
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref post);
    RenderTexture currentRT = RenderTexture.active;
    RenderTexture.active = gameViewTexture;

    targetCamera.CopyFrom(userMain.GetCamera());
    targetCamera.targetTexture = gameViewTexture;
    // IMPORTANT: For some reason, Unity forward path, with Camera.Render(),
    // does not like alphatest surface shaders. The end effect is our terrain
    // does not show up in thumbnails for Low Quality. Could not figure out
    // why...but whatever, for this 1 render let's just do deferred.
    targetCamera.renderingPath = RenderingPath.DeferredShading;
    targetCamera.rect = new Rect(0, 0, 1, 1);
    bool blurEnabled = SetMotionBlurEnabled(false);
    targetCamera.Render();
    SetMotionBlurEnabled(blurEnabled);

    Texture2D screenshotTexture = new Texture2D(gameViewTexture.width, gameViewTexture.height);
    screenshotTexture.ReadPixels(new Rect(0, 0, gameViewTexture.width, gameViewTexture.height), 0, 0);
    screenshotTexture.Apply();
    RenderTexture.active = currentRT;
    return screenshotTexture;
  }

  // Returns the prior enabled value.
  bool SetMotionBlurEnabled(bool enabled)
  {
    URP.MotionBlur blur = null;
    var oldEnabled = false;
    post.profile.TryGetSettings(out blur);
    if (blur != null)
    {
      oldEnabled = blur.enabled.value;
      blur.enabled.value = enabled;
    }
    return oldEnabled;
  }
}