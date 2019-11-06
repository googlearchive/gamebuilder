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

public class RotateToolSettings : MonoBehaviour
{
  [SerializeField] RotateAssetUI assetUI;

  EditMain editMain;
  UndoStack undoStack;
  InputControl inputControl;
  public bool snapping { get; private set; }
  public void Setup()
  {
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref inputControl);

    assetUI.setSpawnToCurrent.onClick.AddListener(SetSpawnToCurrentPosition);
    assetUI.snapToggle.onValueChanged.AddListener(SetSnapping);

    HookUpInputFields();

    assetUI.spawnLabel.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("How the actor spawns when you restart the game");
    assetUI.offsetLabel.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription("Model's rotation offset from center pivot");
    assetUI.snapToggle.gameObject.AddComponent<ItemWithTooltipWithEventSystem>().SetDescription(GetSnapTooltip());
  }

  string GetSnapTooltip()
  {
    return $"Toggle Snap\n({inputControl.GetKeysForAction("Snap")})";
  }

  void HookUpInputFields()
  {
    for (int i = 0; i < 3; i++)
    {
      int index = i;

      assetUI.currentInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        () => GetActorRotation(editMain.GetSingleTargetActor()),
         (vec) => SetActorRotation(editMain.GetSingleTargetActor(), vec)));

      assetUI.spawnInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        () => GetActorSpawnRotation(editMain.GetSingleTargetActor()),
        (vec) => SetActorSpawnRotation(editMain.GetSingleTargetActor(), vec)));

      assetUI.offsetInputs[i].onEndEdit.AddListener((value) => OnActorVec3Edit(
        value,
        index,
        () => GetActorOffsetRotation(editMain.GetSingleTargetActor()),
        (vec) => SetActorOffset(editMain.GetSingleTargetActor(), vec)));
    }
  }

  Vector3 GetActorRotation(VoosActor actor)
  {
    return actor.GetRotation().eulerAngles;
  }

  Vector3 GetActorSpawnRotation(VoosActor actor)
  {
    return actor.GetSpawnRotation().eulerAngles;
  }

  Vector3 GetActorOffsetRotation(VoosActor actor)
  {
    return actor.GetRenderableRotation().eulerAngles;
  }

  void SetActorSpawnRotation(VoosActor actor, Vector3 vec)
  {
    Quaternion newRotation = Quaternion.Euler(vec);
    Vector3 currPosition = actor.GetSpawnPosition();
    Quaternion currRotation = actor.GetSpawnRotation();

    undoStack.PushUndoForActor(
      actor,
      $"Set actor spawn rotation",
      (undoActor) =>
      {
        undoActor.SetSpawnPosition(currPosition);
        undoActor.SetSpawnRotation(newRotation);
      },
      (undoActor) =>
      {
        undoActor.SetSpawnPosition(currPosition);
        undoActor.SetSpawnRotation(currRotation);
      }
    );
  }

  void SetActorRotation(VoosActor actor, Vector3 vec)
  {
    Quaternion newRotation = Quaternion.Euler(vec);
    Vector3 currPosition = actor.GetPosition();
    Quaternion currRotation = actor.GetRotation();

    undoStack.PushUndoForActor(
      actor,
      $"Set actor rotation",
      (undoActor) =>
      {
        undoActor.SetPosition(currPosition);
        undoActor.SetRotation(newRotation);
      },
      (undoActor) =>
      {
        undoActor.SetPosition(currPosition);
        undoActor.SetRotation(currRotation);
      }
    );
  }

  void SetActorOffset(VoosActor actor, Vector3 vec)
  {
    Quaternion oldRot = actor.GetRenderableRotation();
    undoStack.PushUndoForActor(
      actor,
      $"Set actor offset",
      (undoActor) => undoActor.SetRenderableRotation(Quaternion.Euler(vec)),
      (undoActor) => undoActor.SetRenderableRotation(oldRot)
    );
  }

  private void SetSpawnToCurrentPosition()
  {
    VoosActor actor = editMain.GetSingleTargetActor();
    if (actor == null) return;

    SetActorSpawnRotation(actor, GetActorRotation(actor));
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
    assetUI.toolModeFrame.gameObject.SetActive(GetShowSettings() && onlyOne);
    assetUI.spawnFrame.SetActive(GetShowSettings() && onlyOne);
    assetUI.offsetsToggle.gameObject.SetActive(GetShowSettings() && onlyOne);
    assetUI.offsetFrame.SetActive(GetShowSettings() && GetShowOffsets() && onlyOne);

    assetUI.noSelection.SetActive(GetShowSettings() && actorCount == 0);
    assetUI.multiSelection.SetActive(GetShowSettings() && actorCount > 1);

    if (inputControl.GetButton("Snap"))
    {
      assetUI.snapToggle.isOn = true;
    }
    else if (assetUI.snapToggle.isOn != snapping)
    {
      assetUI.snapToggle.isOn = snapping;
    }


    if (onlyOne)
    {
      ActorUpdate(editMain.GetSingleTargetActor());
    }
    else
    {
      assetUI.header.text = "Rotate Tool";
    }

    UpdateNavigation();
  }

  public bool GetShowOffsets()
  {
    return assetUI.offsetsToggle.isOn;
  }

  public void SetShowOffsets(bool on)
  {
    assetUI.offsetsToggle.isOn = on;
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
    assetUI.header.text = $"{actor.GetDisplayName()} : Rotate";
    UpdateVec3Input(actor.GetRotation().eulerAngles, assetUI.currentInputs);
    UpdateVec3Input(actor.GetSpawnRotation().eulerAngles, assetUI.spawnInputs);
    UpdateVec3Input(actor.GetRenderableRotation().eulerAngles, assetUI.offsetInputs);
  }

  void UpdateVec3Input(Vector3 vec, TMPro.TMP_InputField[] inputs)
  {
    if (!inputs[0].isFocused) inputs[0].text = vec.x.ToFourDecimalPlaces();
    if (!inputs[1].isFocused) inputs[1].text = vec.y.ToFourDecimalPlaces();
    if (!inputs[2].isFocused) inputs[2].text = vec.z.ToFourDecimalPlaces();
  }

  internal void RequestDestroy()
  {
    Destroy(gameObject);
  }

  public bool GetShowSettings()
  {
    return assetUI.settingsToggle.isOn;
  }

  internal bool GetSnapping()
  {
    return snapping || inputControl.GetButton("Snap");// assetUI.snapToggle.isOn;
  }


  internal bool AutosetSpawn()
  {
    return assetUI.updateSpawnOnMoveToggle.isOn;
  }

  internal void SetShowSettings(bool value)
  {
    assetUI.settingsToggle.isOn = value;
  }
  internal void SetSnapping(bool snapping)
  {
    assetUI.snapToggle.isOn = snapping;
    if (!inputControl.GetButton("Snap"))
    {
      this.snapping = snapping;
    }
  }

  internal void SetAutosetSpawn(bool autosetSpawn)
  {
    assetUI.updateSpawnOnMoveToggle.isOn = autosetSpawn;
  }
}
