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
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MoveToolSettings : MonoBehaviour
{
  [SerializeField] MoveAssetUI assetUI;
  [SerializeField] GameObject selectorToolFab;

  EditMain editMain;
  UndoStack undoStack;
  ObjectSelectorTool objectSelectorTool;
  HierarchyPanelController hierarchyPanelController;
  InputControl inputControl;
  public bool snapping { get; private set; }
  public bool localSpaceToggle { get; private set; }
  public void Setup()
  {
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref hierarchyPanelController);
    Util.FindIfNotSet(this, ref inputControl);

    assetUI.setSpawnToCurrent.onClick.AddListener(SetSpawnToCurrentPosition);
    assetUI.currentParentButton.onClick.AddListener(SetCurrentParent);
    assetUI.restartParentButton.onClick.AddListener(SetSpawnParent);
    assetUI.snapToggle.onValueChanged.AddListener(SetSnappingSetting);
    assetUI.localSpaceToggle.onValueChanged.AddListener(SetLocalSpace);

    HookUpInputFields();

    assetUI.spawnLabel.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Where the actor spawns when you restart the game");
    assetUI.offsetLabel.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Model's positional offset from center pivot");
    assetUI.snapToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription(GetSnapTooltip());
    assetUI.currentParentButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Current parent of object");
    assetUI.restartParentButton.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Parent on restart");
  }

  string GetSnapTooltip()
  {
    return $"Toggle Snap\n({inputControl.GetKeysForAction("Snap")})";//(Ctrl/Home)"
  }

  private void SetCurrentParent()
  {
    VoosActor actor = editMain.GetSingleTargetActor();
    SelectActor((selectedActor) =>
    {
      bool autosetParent = AutosetSpawn();
      SetCurrentParentForActor(actor, selectedActor, undoStack, autosetParent);
    });
  }

  public static void SetCurrentParentForActor(
    VoosActor actor, VoosActor selectedActor, UndoStack undoStack, bool autosetParent = false)
  {
    string newParentName = selectedActor?.GetName();
    string prevParentName = actor.GetTransformParent();
    string prevSpawnParentName = actor.GetSpawnTransformParent();
    undoStack.PushUndoForActor(
      actor,
      $"Set actor parent",
      (undoActor) =>
      {
        VoosActor newParent = undoActor.GetEngine().GetActor(newParentName);
        string parentName = VoosActor.IsValidParent(undoActor, newParent) ? newParent?.GetName() : null;
        undoActor.SetTransformParent(parentName);
        if (autosetParent)
        {
          undoActor.SetSpawnTransformParent(parentName);
        }
      },
      (undoActor) =>
      {
        VoosActor prevParent = undoActor.GetEngine().GetActor(prevParentName);
        undoActor.SetTransformParent(VoosActor.IsValidParent(undoActor, prevParent) ? prevParent?.GetName() : null);
        if (autosetParent)
        {
          VoosActor prevSpawnParent = undoActor.GetEngine().GetActor(prevSpawnParentName);
          undoActor.SetSpawnTransformParent(
            VoosActor.IsValidParent(undoActor, prevSpawnParent) ? prevSpawnParent?.GetName() : null);
        }
      });
  }

  private void SetSpawnParent()
  {
    VoosActor actor = editMain.GetSingleTargetActor();
    SelectActor((selectedActor) =>
    {
      string newParentName = selectedActor?.GetName();
      string prevParentName = actor.GetSpawnTransformParent();
      undoStack.PushUndoForActor(
        actor,
        $"Set actor parent on reset",
        (undoActor) =>
        {
          VoosActor newParent = undoActor.GetEngine().GetActor(newParentName);
          undoActor.SetSpawnTransformParent(VoosActor.IsValidParent(undoActor, newParent) ? newParent?.GetName() : null);
        },
        (undoActor) =>
        {
          VoosActor prevParent = undoActor.GetEngine().GetActor(prevParentName);
          undoActor.SetSpawnTransformParent(VoosActor.IsValidParent(undoActor, prevParent) ? prevParent?.GetName() : null);
        }
      );
    });
  }

  void HookUpInputFields()
  {
    for (int i = 0; i < 3; i++)
    {
      int index = i;

      assetUI.currentInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        editMain.GetSingleTargetActor().GetPosition,
         (vec) => SetActorPosition(editMain.GetSingleTargetActor(), vec)));

      assetUI.spawnInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        editMain.GetSingleTargetActor().GetSpawnPosition,
        (vec) => SetActorSpawnPosition(editMain.GetSingleTargetActor(), vec)));

      assetUI.offsetInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        editMain.GetSingleTargetActor().GetRenderableOffset,
        (vec) => SetActorOffset(editMain.GetSingleTargetActor(), vec)));
    }
  }

  void SetActorPosition(VoosActor actor, Vector3 newPosition)
  {
    Vector3 currPosition = actor.GetPosition();
    Quaternion currRotation = actor.GetRotation();
    bool autosetSpawn = AutosetSpawn();

    undoStack.PushUndoForActor(
      actor,
      $"Set actor position",
      (undoActor) =>
      {
        undoActor.SetPosition(newPosition);
        undoActor.SetRotation(currRotation);
        if (autosetSpawn)
        {
          undoActor.SetSpawnPositionRotationOfEntireFamily();
        }
      },
      (undoActor) =>
      {
        undoActor.SetPosition(currPosition);
        undoActor.SetRotation(currRotation);
        if (autosetSpawn)
        {
          undoActor.SetSpawnPositionRotationOfEntireFamily();
        }
      }
    );
  }

  void SetActorSpawnPosition(VoosActor actor, Vector3 newPosition, bool setParent = false, string spawnParent = null)
  {
    Vector3 currPosition = actor.GetSpawnPosition();
    Quaternion currRotation = actor.GetSpawnRotation();
    string currParent = actor.GetSpawnTransformParent();

    undoStack.PushUndoForActor(
      actor,
      $"Set actor spawn position",
      (undoActor) =>
      {
        undoActor.SetSpawnPosition(newPosition);
        undoActor.SetSpawnRotation(currRotation);
        if (setParent)
        {
          VoosActor setSpawnParent = undoActor.GetEngine().GetActor(spawnParent);
          undoActor.SetSpawnTransformParent(setSpawnParent?.GetName());
        }
      },
      (undoActor) =>
      {
        undoActor.SetSpawnPosition(currPosition);
        undoActor.SetSpawnRotation(currRotation);
        if (setParent)
        {
          VoosActor setSpawnParent = undoActor.GetEngine().GetActor(currParent);
          undoActor.SetSpawnTransformParent(setSpawnParent?.GetName());
        }
      }
    );
  }

  void SetActorOffset(VoosActor actor, Vector3 vec)
  {
    Vector3 oldPos = actor.GetRenderableOffset();
    undoStack.PushUndoForActor(actor,
      $"Set actor offset",
      (undoActor) => undoActor.SetRenderableOffset(vec),
      (undoActor) => undoActor.SetRenderableOffset(oldPos));
  }

  private void SetSpawnToCurrentPosition()
  {
    VoosActor actor = editMain.GetSingleTargetActor();
    if (actor == null) return;
    SetActorSpawnPosition(actor, actor.GetPosition(), true, actor.GetTransformParent());
  }

  void OnActorVec3Edit(string newValue, int index, System.Func<Vector3> getVec, System.Action<Vector3> setVec)
  {
    Debug.Assert(editMain.GetSingleTargetActor() != null);

    float floatVal;

    if (float.TryParse(newValue, NumberStyles.Number, CultureInfo.InvariantCulture, out floatVal))
    {
      Vector3 pos = getVec();
      pos[index] = floatVal;
      setVec(pos);
    }
  }

  void Update()
  {
    int actorCount = editMain.GetTargetActorsCount();
    bool onlyOne = actorCount == 1;

    assetUI.currentFrame.SetActive(GetShowSettings() && onlyOne);
    assetUI.snapToggle.gameObject.SetActive(GetShowSettings() && onlyOne);
    assetUI.localSpaceToggle.gameObject.SetActive(GetShowSettings() && onlyOne);
    assetUI.offsetsToggle.gameObject.SetActive(GetShowSettings() && onlyOne);
    assetUI.spawnFrame.SetActive(GetShowSettings() && onlyOne);
    assetUI.offsetFrame.SetActive(GetShowSettings() && GetShowOffsets() && onlyOne);
    foreach (GameObject go in assetUI.parentFrames) go.SetActive(GetShowSettings() && onlyOne);

    assetUI.noSelection.SetActive(GetShowSettings() && actorCount == 0);
    assetUI.multiSelection.SetActive(GetShowSettings() && actorCount > 1);

    assetUI.snapToggle.onValueChanged.RemoveListener(SetSnappingSetting);
    assetUI.snapToggle.isOn = inputControl.GetButton("Snap") || snapping;
    assetUI.snapToggle.onValueChanged.AddListener(SetSnappingSetting);

    if (onlyOne)
    {
      ActorUpdate(editMain.GetSingleTargetActor());
    }
    else
    {
      assetUI.header.text = "Move Tool";
    }

    UpdateNavigation();
  }

  private void UpdateNavigation()
  {
    if (Input.GetKeyDown(KeyCode.Tab) && EventSystem.current.currentSelectedGameObject != null)
    {
      if (Util.IsShiftHeld())
      {
        Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnUp();
        if (selectable != null) selectable.Select();
      }
      else
      {
        Selectable selectable = EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>().FindSelectableOnDown();
        if (selectable != null) selectable.Select();
      }
    }
  }

  private void ActorUpdate(VoosActor actor)
  {
    assetUI.header.text = $"{actor.GetDisplayName()} : Move";
    UpdateVec3Input(actor.GetPosition(), assetUI.currentInputs);
    UpdateVec3Input(actor.GetSpawnPosition(), assetUI.spawnInputs);
    UpdateVec3Input(actor.GetRenderableOffset(), assetUI.offsetInputs);
    VoosActor parent = actor.GetEngine().GetActor(actor.GetTransformParent());
    assetUI.currentParentButtonText.text = parent != null ? parent.GetDisplayName() : "<none>";
    VoosActor spawnParent = actor.GetEngine().GetActor(actor.GetSpawnTransformParent());
    assetUI.restartParentButtonText.text = spawnParent != null ? spawnParent.GetDisplayName() : "<none>";
  }

  void UpdateVec3Input(Vector3 vec, TMPro.TMP_InputField[] inputs)
  {
    if (!inputs[0].isFocused) inputs[0].text = vec.x.ToFourDecimalPlaces();
    if (!inputs[1].isFocused) inputs[1].text = vec.y.ToFourDecimalPlaces();
    if (!inputs[2].isFocused) inputs[2].text = vec.z.ToFourDecimalPlaces();
  }

  internal void RequestDestroy()
  {
    if (objectSelectorTool != null)
    {
      editMain.RemoveToolFromList(objectSelectorTool);
      objectSelectorTool = null;
    }
    Destroy(gameObject);
  }

  internal bool GetShowSettings()
  {
    return assetUI.settingsToggle.isOn;
  }

  internal bool ShouldSnap()
  {
    return snapping || inputControl.GetButton("Snap");// assetUI.snapToggle.isOn;
  }

  internal bool GetSnappingSetting()
  {
    return snapping;
  }

  internal bool GetLocalSpace()
  {
    return assetUI.localSpaceToggle.isOn;
  }

  public bool GetShowOffsets()
  {
    return assetUI.offsetsToggle.isOn;
  }

  internal bool AutosetSpawn()
  {
    return assetUI.updateSpawnOnMoveToggle.isOn;
  }

  internal void SetShowSettings(bool proMode)
  {
    assetUI.settingsToggle.isOn = proMode;
  }

  internal void SetSnappingSetting(bool snapping)
  {
    this.snapping = snapping;
    assetUI.snapToggle.isOn = snapping || inputControl.GetButtonDown("Snap");
    PlayerPrefs.SetInt("moveTool-snapping", snapping ? 1 : 0);
  }

  internal void SetLocalSpace(bool localSpace)
  {
    assetUI.localSpaceToggle.isOn = localSpace;
    PlayerPrefs.SetInt("moveTool-localSpace", localSpace ? 1 : 0);
  }

  internal void SetAutosetSpawn(bool autosetSpawn)
  {
    assetUI.updateSpawnOnMoveToggle.isOn = autosetSpawn;
  }

  public void SetShowOffsets(bool on)
  {
    assetUI.offsetsToggle.isOn = on;
  }

  public void SelectActor(System.Action<VoosActor> callback)
  {
    if (objectSelectorTool != null)
    {
      editMain.RemoveToolFromList(objectSelectorTool);
    }
    objectSelectorTool = editMain.AppendTool<ObjectSelectorTool>(selectorToolFab);
    // Hack because CanvasRender can't be disabled ... maybe we can add an animation here?
    assetUI.pickerOverlay.SetActive(true);
    MoveToolSettings settings = this;
    System.Action<VoosActor> moveToolCallback = (actor) =>
    {
      hierarchyPanelController.SetSelectCallback(null);
      editMain.RemoveToolFromList(settings.objectSelectorTool);
      assetUI.pickerOverlay.SetActive(false);
      Debug.Log((actor != null ? actor.GetDisplayName() : "nothing") + " " + "selected");
      callback(actor);
    };
    hierarchyPanelController.SetSelectCallback(moveToolCallback);
    objectSelectorTool.OnActorSelect = moveToolCallback;
  }
}
