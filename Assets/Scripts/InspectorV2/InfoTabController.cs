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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoTabController : AbstractTabController
{
  [SerializeField] InfoTabUI ui;

  public struct NameUndoState
  {
    readonly string name;

    public NameUndoState(string name)
    {
      this.name = name;
    }

    public void PushTo(VoosActor actor)
    {
      actor.SetDisplayName(name);
      // HACK FOR CLONES
      actor.ApplyPropertiesToClones();
    }
  }

  public struct DescriptionUndoState
  {
    readonly string description;

    public DescriptionUndoState(string description)
    {
      this.description = description;
    }

    public void PushTo(VoosActor actor)
    {
      actor.SetDescription(description);
      // HACK FOR CLONES
      actor.ApplyPropertiesToClones();
    }
  }

  public struct TagsUndoState
  {
    readonly string joinedTags;

    public TagsUndoState(string joinedTags)
    {
      this.joinedTags = joinedTags;
    }

    public void PushTo(VoosActor actor)
    {
      string[] tags = joinedTags.Split(new char[] { ' ', ',', '\n' });
      tags = Array.FindAll(tags, tag => tag != "");
      actor.SetTags(tags);
      // HACK FOR CLONES
      actor.ApplyPropertiesToClones();
    }
  }

  [SerializeField] GameObject snapshotPopupPrefab;

  private VoosEngine voosEngine;
  private EditMain editMain;
  private VoosActor actor;
  private SnapshotCamera snapshotCamera;
  private SceneActorLibrary sceneActorLibrary;
  private UndoStack undoStack;

  const float POPUP_DURATION = 3;

  public override void Setup()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref snapshotCamera);
    Util.FindIfNotSet(this, ref sceneActorLibrary);
    Util.FindIfNotSet(this, ref undoStack);
    ui.nameField.onEndEdit.AddListener(OnNameFieldEndEdit);
    ui.tagsField.onEndEdit.AddListener(OnTagsFieldEndEdit);
    ui.descriptionField.onEndEdit.AddListener(OnDescriptionFieldEndEdit);
    ui.isOffStageToggle.onValueChanged.AddListener(OnOffstageToggleChanged);
    ui.hideInPlayModeToggle.onValueChanged.AddListener(ToggleHideInPlayMode);
    ui.saveToCreationLibraryButton.onClick.AddListener(SaveActor);
  }

  protected override void Update()
  {
    if (actor != null)
    {
      UpdateAllFields();
    }
    base.Update();
  }

  void ToggleHideInPlayMode(bool value)
  {
    if (actor == null) return;
    bool prevValue = actor.GetHideInPlayMode();
    undoStack.PushUndoForActor(
      actor,
      $"Set hide for {actor.GetDisplayName()}",
      actor =>
      {
        actor.SetHideInPlayMode(value);
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        actor.SetHideInPlayMode(prevValue);
        actor.ApplyPropertiesToClones();
      });
  }

  public void UpdateAllFields()
  {
    if (!ui.nameField.isFocused)
    {
      ui.nameField.text = actor.GetDisplayName();
    }
    if (!ui.tagsField.isFocused)
    {
      ui.tagsField.text = actor.GetJoinedTags();
    }
    if (!ui.descriptionField.isFocused)
    {
      ui.descriptionField.text = actor.GetDescription();
    }

    // this pattner (remove listener, set, add again) is a hack to make the undo system worl
    ui.hideInPlayModeToggle.onValueChanged.RemoveListener(ToggleHideInPlayMode);
    ui.hideInPlayModeToggle.isOn = actor.GetHideInPlayMode();
    ui.hideInPlayModeToggle.onValueChanged.AddListener(ToggleHideInPlayMode);

    ui.isOffStageToggle.onValueChanged.RemoveListener(OnOffstageToggleChanged);
    ui.isOffStageToggle.isOn = actor.GetIsOffstageEffective();
    ui.isOffStageToggle.onValueChanged.AddListener(OnOffstageToggleChanged);

    // Only let user toggle on/off stage if this is an unparented actor.
    ui.isOffStageToggle.interactable = !actor.IsParentedToAnotherActor();

    // ui.isOffStageToggle.gameObject.SetActive(editMain.ShowAdvanced());
  }

  public override void Open(VoosActor actor, Dictionary<string, object> props)
  {
    base.Open(actor, props);

    this.actor = actor;

    ui.stripes.gameObject.SetActive(true);
    ui.imageField.gameObject.SetActive(false);
    actor.GetThumbnail((thumbnail) =>
    {
      ui.stripes.gameObject.SetActive(false);
      ui.imageField.gameObject.SetActive(true);
      ui.imageField.texture = thumbnail;
    });

    UpdateAllFields();
  }

  private void OnNameFieldEndEdit(string text)
  {
    var undoState = new NameUndoState(actor.GetDisplayName());
    var redoState = new NameUndoState(text);
    undoStack.PushUndoForActor(
      actor,
      $"Set name for {actor.GetDisplayName()}",
      actor => redoState.PushTo(actor),
      actor => undoState.PushTo(actor));
  }

  private void OnTagsFieldEndEdit(string text)
  {
    var undoState = new TagsUndoState(actor.GetJoinedTags());
    var redoState = new TagsUndoState(text);
    undoStack.PushUndoForActor(
      actor,
      $"Set tags for {actor.GetDisplayName()}",
      actor => redoState.PushTo(actor),
      actor => undoState.PushTo(actor));
  }

  private void OnDescriptionFieldEndEdit(string text)
  {
    var undoState = new DescriptionUndoState(actor.GetDescription());
    var redoState = new DescriptionUndoState(text);
    undoStack.PushUndoForActor(
      actor,
      $"Set description for {actor.GetDisplayName()}",
      actor => redoState.PushTo(actor),
      actor => undoState.PushTo(actor));
  }

  private void OnOffstageToggleChanged(bool value)
  {
    bool prevValue = actor.GetPreferOffstage();
    undoStack.PushUndoForActor(
      actor,
      $"Set description for {actor.GetDisplayName()}",
      actor =>
      {
        if (!actor.IsParentedToAnotherActor())
        {
          actor.SetPreferOffstage(value);
        }
      },
      actor =>
      {
        if (!actor.IsParentedToAnotherActor())
        {
          actor.SetPreferOffstage(prevValue);
        }
      });
  }

  private void SaveActor()
  {
    editMain.AddDebugMessage("Saving " + actor.GetDisplayName());
    Texture2D thumbnail = snapshotCamera.SnapshotActor(actor);

    if (sceneActorLibrary.Exists(actor.GetDisplayName()))
    {
      // TODO: Confirm overwrite?
    }
    sceneActorLibrary.Put(actor.GetDisplayName(), actor, thumbnail);
#if UNITY_EDITOR
    // Ctrl + Alt + click also copies the actor to clipboard for easy (and hacky) exporting.
    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
    {
      GUIUtility.systemCopyBuffer = sceneActorLibrary.ExportToJson(actor, thumbnail);
      editMain.AddDebugMessage("Also saved TO CLIPBOARD as prefab (DEBUG)");
    }
#endif

    Sprite _sprite = Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), new Vector2(.5f, .5f), 100);
    Instantiate(snapshotPopupPrefab, editMain.GetMainRect()).GetComponent<Popup>().Setup(_sprite, POPUP_DURATION);

    // TODO: put this back
    // creationLibrary.OnSaveActorPrefab(prefab);
  }

}
