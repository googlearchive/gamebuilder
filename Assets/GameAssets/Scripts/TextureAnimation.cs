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

public class TextureAnimation : MonoBehaviour
{
  public int frameCount = 4;
  public float frameDuration = .25f;
  const string TEX_NAME = "_Texture";

  Renderer renderer;

  private void Start()
  {
    renderer = GetComponent<Renderer>();
    List<string> textureNames = new List<string>();
    renderer.material.GetTexturePropertyNames(textureNames);
    if (!textureNames.Contains(TEX_NAME)) Destroy(this);
  }

  void NextFrame()
  {
    curFrame = (curFrame - 1) % 4;
    //Debug.Log( curFrame / (float) frameCount );
    renderer.material.SetTextureOffset(TEX_NAME, new Vector2(0, curFrame / (float)frameCount));
  }

  int curFrame = 0;
  float timePassed = 0;
  void Update()
  {
    timePassed += Time.deltaTime;
    if (timePassed > frameDuration)
    {
      timePassed -= frameDuration;
      NextFrame();
    }
  }
}
