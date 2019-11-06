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
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;

public class TemplateSelectorMenu : MonoBehaviour
{
  private struct BuiltInTemplateInfo
  {
    public string name;
    public string baseFileName;
    public bool isTutorial;

    public BuiltInTemplateInfo(string name, string baseFileName, bool isTutorial = false)
    {
      this.name = name;
      this.baseFileName = baseFileName;
      this.isTutorial = isTutorial;
    }
  }

  private BuiltInTemplateInfo[] TEMPLATES = {
    new BuiltInTemplateInfo("<size=35>Tutorial</size>\n<size=18>Start here!</size>", "tutorial", isTutorial: true),
    new BuiltInTemplateInfo("Empty", "template-empty"),
    new BuiltInTemplateInfo("3D Platformer", "template-platformer"),
    new BuiltInTemplateInfo("First-Person Shooter", "template-fps"),
    new BuiltInTemplateInfo("Driving", "template-driving")
  };

  [SerializeField] TemplateItem templateItem;
  [SerializeField] UnityEngine.UI.Button closeButton;

  DynamicPopup popups;
  LoadingScreen loadingScreen;
  GameBuilderSceneController sceneController;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref loadingScreen);
    Util.FindIfNotSet(this, ref sceneController);

    templateItem.gameObject.SetActive(false);
    foreach (BuiltInTemplateInfo templateInfo in TEMPLATES)
    {
      GameObject obj = GameObject.Instantiate(templateItem.gameObject);
      obj.transform.SetParent(templateItem.transform.parent, false);
      TemplateItem thisItem = obj.GetComponent<TemplateItem>();
      thisItem.SetTitle(templateInfo.name);
      thisItem.SetThumbnailPath(Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Public", templateInfo.baseFileName + ".png"));
      thisItem.AddClickListener(() => OnTemplateClicked(templateInfo));
      obj.SetActive(true);
    }

    closeButton.onClick.AddListener(Close);
  }

  public void Show()
  {
    gameObject.SetActive(true);
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  void OnTemplateClicked(BuiltInTemplateInfo template)
  {
    string fullPath = Path.Combine(Application.streamingAssetsPath, "ExampleGames", "Public", template.baseFileName + ".voos");
    if (template.isTutorial)
    {
      loadingScreen.ShowAndDo(() => sceneController.RestartAndLoadTutorial());
    }
    else
    {
      popups.AskHowToPlay(playOpts =>
      {
        var gameOpts = new GameBuilderApplication.GameOptions { playOptions = playOpts };
        loadingScreen.ShowAndDo(() => sceneController.RestartAndLoad(fullPath, gameOpts));
      });
    }
  }
}