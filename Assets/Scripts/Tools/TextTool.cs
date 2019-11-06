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
using UnityEngine.EventSystems;

public class TextTool : Tool
{
  // Local scale of the selector node (preview), calibrated to the size that the text panel will be if
  // the scale is 1.
  readonly Vector3 SELECTOR_NODE_LOCAL_SCALE = new Vector3(0.9f, 0.2f, 0.05f);
  // The default scale to apply to the text actor.
  const float DEFAULT_TEXT_SCALE = 6;

  [SerializeField] VoosEngine engine;
  [SerializeField] string dimensionalRenderableUri;
  [SerializeField] string billboardRenderableUri;
  [SerializeField] Transform selectorNode;
  [SerializeField] AudioSource audioSource;
  [SerializeField] UnityEngine.UI.Image reticleImage;
  [SerializeField] AlwaysFaceCamera alwaysFaceCamera;
  TMPro.TMP_InputField inputField;
  GameObject inputFieldObject;

  UndoStack undoStack;
  DynamicPopup popups;

  VoosActor actorBeingEdited = null;
  bool actorWasJustCreated = false;
  string actorTextBeforeEditing = null;

  VoosActor lastActorEdited = null;
  bool snapping;
  TerrainManager.BlockDirection blockDirection;

  // Number of times the user manually rotated. This gets added to the final orientation.
  int numManualRotations = 0;

  const float DEFAULT_PANEL_HEIGHT = 1f;

  const string EMPTY_TEXT = "[insert text here]";


  private void Awake()
  {
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref popups);
  }
  /* 
    public override float GetMaxDistance()
    {
      return 10f;
    } */

  public override bool TargetsActors()
  {
    return true;
  }

  public override bool TargetsGround()
  {
    return true;
  }

  public override bool TargetsSpace()
  {
    return true;
  }

  bool IsBillboard()
  {
    return subtoolbarIndex == 0;
  }

  public override bool ShowHoverTargetFeedback()
  {
    return false;
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return false;
  }

  public override bool Trigger(bool on)
  {
    base.Trigger(on);
    if (on)
    {
      if (!IsEditing() && !coolingdown)
      {
        if (editableTarget && hoverActor != null)
        {
          BeginTextEditingOnExistingActor();
        }
        else
        {
          BeginTextEditing();
        }
      }
      else
      {
        EndTextEditing();
      }

    }
    return true;
  }

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);

    inputField = editMain.textToolInput;
    inputFieldObject = editMain.textToolInputObject;
    inputField.onEndEdit.AddListener((inputstring) => EndTextEditing());
    inputField.onValueChanged.AddListener(UpdateInputText);
    selectorNode.transform.localScale = DEFAULT_TEXT_SCALE * SELECTOR_NODE_LOCAL_SCALE;
    numManualRotations = 0;
  }

  void UpdateInputText(string s)
  {
    if (!(Input.GetButtonDown("Submit") && !Input.GetKey(KeyCode.LeftShift)))
    {
      UpdateText(s);
    }
  }

  Quaternion GetRotation()
  {
    switch (blockDirection)
    {
      case TerrainManager.BlockDirection.North:
        return Quaternion.identity;
      case TerrainManager.BlockDirection.East:
        return Quaternion.Euler(0, 90, 0);
      case TerrainManager.BlockDirection.South:
        return Quaternion.Euler(0, 180, 0);
      case TerrainManager.BlockDirection.West:
        return Quaternion.Euler(0, 270, 0);
      default:
        return Quaternion.identity;
    }
  }

  public override void UpdatePosition(Vector3 newpos)
  {
    targetPosition = snapping ? TerrainManager.SnapPosition(newpos) : newpos;
    targetPosition.y = targetPosition.y + DEFAULT_PANEL_HEIGHT;


    if (selectorNode.gameObject.activeSelf == editableTarget)
    {
      selectorNode.gameObject.SetActive(!editableTarget);
      reticleImage.enabled = editableTarget;
    }
    selectorNode.position = targetPosition;
    if (!IsBillboard()) selectorNode.rotation = GetRotation();
  }


  public override bool IsSelectionLocked()
  {
    return IsEditing();
  }

  bool IsEditing()
  {
    return actorBeingEdited != null;
  }

  VoosActor CreatePanel(Vector3 position, Quaternion rotation)
  {
    return engine.CreateActor(position, rotation, actor =>
    {
      actor.SetPreferOffstage(false);
      actor.SetDisplayName("Text");
      actor.SetLocalScale(Vector3.one * DEFAULT_TEXT_SCALE);
      actor.SetRenderableUri(IsBillboard() ? billboardRenderableUri : dimensionalRenderableUri);
      actor.SetIsSolid(false);
      actor.SetTint(new Color32(1, 1, 1, 190));
    });
  }

  void BeginTextEditing()
  {
    if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
    audioSource.Play();

    actorBeingEdited = CreatePanel(targetPosition, GetRotation());
    actorWasJustCreated = true;
    actorTextBeforeEditing = null;

    inputField.text = EMPTY_TEXT;
    inputFieldObject.SetActive(true);
    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
  }

  void BeginTextEditingOnExistingActor()
  {
    if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
    audioSource.Play();

    actorWasJustCreated = false;
    actorBeingEdited = hoverActor;
    actorTextBeforeEditing = actorBeingEdited.GetCommentText();

    inputField.text = actorBeingEdited.GetCommentText();
    inputFieldObject.SetActive(true);
    EventSystem.current.SetSelectedGameObject(inputField.gameObject);
  }

  void EndTextEditing()
  {
    if (IsEditing())
    {
      // Erm, do this first, cuz deactivating stuff could cause this to be
      // called again recursively. Like deactivating the inputFieldObject!
      lastActorEdited = actorBeingEdited;
      actorBeingEdited = null;

      inputFieldObject.SetActive(false);
      cooldownRoutine = StartCoroutine(CooldownRoutine());
      if (inputField.text == EMPTY_TEXT)
      {
        engine.DestroyActor(lastActorEdited);
      }
      else
      {
        editMain.SetTargetActor(lastActorEdited);
        // editMain.SetFocusedTargetActor(lastActorEdited);

        string undoText = actorTextBeforeEditing;

        if (actorWasJustCreated)
        {
          var engine = lastActorEdited.GetEngine();
          var actorData = engine.SaveActor(lastActorEdited);
          undoStack.PushUndoForCreatingActor(lastActorEdited, $"Create text panel");
        }
        else
        {
          string currText = lastActorEdited.GetCommentText();
          if (ActorUndoUtil.GetUnableToEditActorReason(lastActorEdited.GetEngine(), lastActorEdited.GetName()) == null)
          {
            undoStack.PushUndoForActor(
              lastActorEdited,
              $"Edit text panel",
              redoActor =>
              {
                redoActor.SetCommentText(currText);
                redoActor.ApplyPropertiesToClones();
              },
              undoActor =>
              {
                undoActor.SetCommentText(undoText);
                undoActor.ApplyPropertiesToClones();
              });
          }
        }
      }
    }
  }

  public override string GetName()
  {
    return "Text";
  }

  public override bool KeyLock()
  {
    return IsEditing();
  }

  public override bool MouseLookActive()
  {
    return !IsEditing();
  }

  public override void Close()
  {
    if (cooldownRoutine != null) StopCoroutine(cooldownRoutine);
    coolingdown = false;
    base.Close();
  }

  bool coolingdown = false;
  Coroutine cooldownRoutine;
  IEnumerator CooldownRoutine()
  {
    coolingdown = true;
    yield return new WaitForSecondsRealtime(.25f);
    coolingdown = false;
  }

  void UpdateText(string s)
  {
    this.actorBeingEdited.RequestOwnership();
    this.actorBeingEdited.SetCommentText(s);
  }

  bool editableTarget;

  void Update()
  {

    alwaysFaceCamera.enabled = IsBillboard();

    if (!IsBillboard()) ApplyAutoRotate();

    if (Input.GetButtonDown("Submit"))
    {
      if (!Input.GetKey(KeyCode.LeftShift))
      {
        EndTextEditing();
      }
    }

    if (!IsEditing())
    {


      if (inputControl.GetButtonDown("PrevToolOption"))
      {
        PreviousSubtoolbarItem();
      }

      if (inputControl.GetButtonDown("NextToolOption"))
      {
        NextSubtoolbarItem();
      }

      snapping = inputControl.GetButton("Snap");

      if (inputControl.GetButtonDown("Rotate"))
      {
        numManualRotations++;
      }

      if (hoverActor != null)
      {
        editableTarget = hoverActor.CanShowCommentText();
      }
      else
      {
        editableTarget = false;
      }
    }
  }

  public override bool CanEditTargetActors()
  {
    return false;
  }

  void ApplyAutoRotate()
  {
    // Vector from the edit avatar to the selector node (where the text preview is).
    Vector3 avatarToSelector = (selectorNode.transform.position - editMain.GetAvatarPosition()).WithY(0);

    // Get the angle with North, aviation-style (0 = North, 90 = East, 180 = South, 270 = West).
    float angle = Vector3.SignedAngle(avatarToSelector, Vector3.forward, Vector3.down) % 360;
    angle = angle < 0 ? angle += 360 : angle;

    // Set block direction according to angle, to ensure that text always faces the player.
    blockDirection =
        (angle >= 45 && angle < 135) ? TerrainManager.BlockDirection.West :
        (angle >= 135 && angle < 225) ? TerrainManager.BlockDirection.North :
        (angle >= 225 && angle < 315) ? TerrainManager.BlockDirection.East :
        TerrainManager.BlockDirection.South;

    // Apply the user's manual rotations.
    blockDirection = (TerrainManager.BlockDirection)(((int)blockDirection + numManualRotations) % 4);
  }
}
