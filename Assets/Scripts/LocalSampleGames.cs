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
using System.IO;

public class LocalSampleGames
{
  public static readonly GameInfo[] LOCAL_SAMPLE_GAMES = {
    new GameInfo("sample-fps", "FPS", "Sample FPS"),
    new GameInfo("sample-platformer", "Platformer", "Sample Platformer"),
    new GameInfo("card-demos", "Card Demos", "Logic card system demos"),
  };

  public struct GameInfo
  {
    public string baseFileName;
    public string title;
    public string description;
    public GameInfo(string fileName, string title, string description)
    {
      this.baseFileName = fileName;
      this.title = title;
      this.description = description;
    }

    public string GetVoosFilePath()
    {
      return Path.Combine(Application.streamingAssetsPath,
          "ExampleGames", "Public", baseFileName + ".voos");
    }

    public string GetThumbnailFilePath()
    {
      return Path.Combine(Application.streamingAssetsPath,
          "ExampleGames", "Public", baseFileName + ".png");
    }
  }
}
