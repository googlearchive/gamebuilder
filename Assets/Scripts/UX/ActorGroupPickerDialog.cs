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
using UnityEngine.UI;

public class ActorGroupPickerDialog : MonoBehaviour
{
  private const string PREFAB_PATH = "ActorGroupPicker";

  [SerializeField] TMPro.TextMeshProUGUI pickerPromptField;

  [SerializeField] Button noneButton;
  [SerializeField] Button playerButton;
  [SerializeField] Button byTagButton;
  [SerializeField] Button specificActorButton;
  [SerializeField] Button anyActorButton;
  [SerializeField] Button backFromTagsButton;
  [SerializeField] Button closeButton;
  [SerializeField] Button tagButtonTemplate;
  [SerializeField] GameObject tagsPanel;

  Canvas parentCanvas;
  string pickerPrompt;
  bool allowOffstageActors;
  ActorPickerDialog currentlyOpenActorPicker;

  // Called when actor group picker is closed.
  // success: True if user picked an actor group, false if canceled.
  // pickedActorGroup: the actor group that was picked, "" means none.
  public delegate void OnActorGroupPickerResult(bool success, ActorGroupSpec pickedActorGroup);


  VoosEngine engine;
  OnActorGroupPickerResult callback;
  bool populatedTags;

  public static ActorGroupPickerDialog Launch(Canvas parentCanvas, string pickerPrompt, bool allowOffstageActors, OnActorGroupPickerResult callback)
  {
    GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(PREFAB_PATH));
    if (parentCanvas != null) obj.transform.SetParent(parentCanvas.transform, false);
    ActorGroupPickerDialog dialog = obj.GetComponent<ActorGroupPickerDialog>();
    dialog.Setup(parentCanvas, pickerPrompt, allowOffstageActors, callback);
    return dialog;
  }

  private void Setup(Canvas parentCanvas, string pickerPrompt, bool allowOffstageActors, OnActorGroupPickerResult callback)
  {
    this.parentCanvas = parentCanvas;
    this.callback = callback;
    this.pickerPrompt = pickerPrompt;
    this.allowOffstageActors = allowOffstageActors;

    if (!string.IsNullOrEmpty(pickerPrompt))
    {
      pickerPromptField.text = pickerPrompt;
    }
    Util.FindIfNotSet(this, ref engine);
    tagsPanel.SetActive(false);
    noneButton.onClick.AddListener(OnNoneButtonClicked);
    playerButton.onClick.AddListener(OnPlayerButtonClicked);
    byTagButton.onClick.AddListener(OnByTagButtonClicked);
    specificActorButton.onClick.AddListener(OnSpecificActorButtonClicked);
    anyActorButton.onClick.AddListener(OnAnyActorButtonClicked);
    closeButton.onClick.AddListener(OnCloseButtonClicked);
    backFromTagsButton.onClick.AddListener(OnBackFromTagsButtonClicked);
    tagButtonTemplate.gameObject.SetActive(false);
  }

  public bool OnEscape()
  {
    if (currentlyOpenActorPicker != null)
    {
      currentlyOpenActorPicker.Close();
    }
    else
    {
      CloseAndReturn(false, null);
    }
    return true;
  }

  private void OnCloseButtonClicked()
  {
    CloseAndReturn(false, null);
  }

  private void OnNoneButtonClicked()
  {
    CloseAndReturn(true, ActorGroupSpec.NewNone());
  }

  private void OnPlayerButtonClicked()
  {
    CloseAndReturn(true, ActorGroupSpec.NewByTag("player"));
  }

  private void OnByTagButtonClicked()
  {
    MaybePopulateTags();
    tagsPanel.SetActive(true);
  }

  private void OnBackFromTagsButtonClicked()
  {
    tagsPanel.SetActive(false);
  }

  private void OnSpecificActorButtonClicked()
  {
    currentlyOpenActorPicker = ActorPickerDialog.Launch(
      null, pickerPrompt, allowOffstageActors, (success, actorName) =>
    {
      currentlyOpenActorPicker = null;
      if (success)
      {
        CloseAndReturn(true, ActorGroupSpec.NewByName(actorName));
      }
    });
  }

  private void OnAnyActorButtonClicked()
  {
    CloseAndReturn(true, ActorGroupSpec.NewAny());
  }

  private void MaybePopulateTags()
  {
    if (populatedTags)
    {
      return;
    }
    List<string> tags = engine.GetAllTagsInUse();
    foreach (string tag in tags)
    {
      GameObject newButton = GameObject.Instantiate(tagButtonTemplate.gameObject);
      newButton.SetActive(true);
      newButton.transform.SetParent(tagButtonTemplate.transform.parent, false);
      newButton.GetComponent<Button>().onClick.AddListener(() => CloseAndReturn(true, ActorGroupSpec.NewByTag(tag)));
      newButton.GetComponentInChildren<TMPro.TMP_Text>().text = tag;
    }
    populatedTags = true;
  }

  private void CloseAndReturn(bool success, ActorGroupSpec pickedActorGroup)
  {
    GameObject.Destroy(gameObject);
    if (callback != null)
    {
      callback(success, pickedActorGroup);
    }
  }
}
