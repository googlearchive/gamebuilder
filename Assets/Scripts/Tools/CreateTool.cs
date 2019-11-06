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
using PolyToolkit;
using UnityEngine;

public class CreateTool : Tool
{
  public Transform selectorNode;
  CreationLibrarySidebar creationLibrary;
  OneOffEffects effects;
  AssetSearch assetSearch;

  [SerializeField] CreateToolRay createToolRay;
  [SerializeField] CreateToolPreview createToolPreview;
  [SerializeField] Material previewMaterial;
  [SerializeField] VoosEngine voosEngine;
  [SerializeField] DynamicPopup popups;
  [SerializeField] GameObject rayContainerObject;//so i can disable the effect in first and 3rd person

  UndoStack undoStack;


  ActorableSearchResult result;


  // Stores the modification to rotation caused by R key
  float rotationMod = 0;

  bool inSaveMode = false;

  bool snapping = false;

  static bool didShowCopyTip = false; // Make static for lack of preserved tool state.
  int numDuplicatesCreated = 0;
  string uriLastCreated = null;

  public void Awake()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref effects);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref assetSearch);
  }

  public override bool TargetsActors()
  {
    return true;
  }

  public override bool TargetsGround()
  {
    return true;
  }

  public override bool ShowHoverTargetFeedback()
  {
    return false;
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return false;
  }

  public override bool TargetsSpace()
  {
    return true;
  }

  public override string GetName()
  {
    return "New";
  }

  Vector3 GetScale()
  {
    Vector3 _scale = Vector3.one;
    if (!IsCurrentAssetRenderableEmpty())
    {
      _scale = GetRenderableObject().transform.localScale;
    }
    return _scale;
  }

  Quaternion GetAdditionalRotation()
  {
    return Quaternion.Euler(0, rotationMod, 0);
  }

  bool menuClickedOnMouseDown = false;
  bool DragMenuHack()
  {
    bool check = menuClickedOnMouseDown;
    menuClickedOnMouseDown = false;
    return check;
  }

  public override bool CanEditTargetActors()
  {
    return false;
  }

  public override bool Trigger(bool on)
  {
    bool triggerDownPrev = isTriggerDown;
    base.Trigger(on);
    bool menuClickCheck = DragMenuHack();

    if (!on && triggerDownPrev)//if (!on && (triggerDownPrev || menuClickCheck))
    {
      if (!editMain.CursorOverUI())
      {
        if (GetSelected().renderableReference.uri == null)
        {
          return true;
        }

        if (hoverActor != null)
        {
          if (result.pfxId != null)
          {
            SetPfxIdOnActor(hoverActor, result.pfxId);
            return true;
          }
          else if (result.sfxId != null)
          {
            SetSfxIdOnActor(hoverActor, result.sfxId);
            return true;
          }
        }

        Vector3 _scale = GetScale();
        Quaternion initRotation = GetAdditionalRotation();
        effects.TriggerActorSpawn(
          GetSelected(),
          targetPosition, initRotation, _scale,
          actor =>
          {
            // OK we could be fancy and also trigger the spawn effect upon
            // redo..but no.
            undoStack.PushUndoForCreatingActor(actor, $"Create {actor.GetDisplayName()}");
            editMain.SetTargetActor(actor);
          },
          false, /* offstage is always false now */
          editMain.GetAvatarTint(),
          GetSelected().GetRenderableOffset(),
          GetSelected().preferredRotation);

        if (GetSelected().renderableReference.uri != uriLastCreated)
        {
          numDuplicatesCreated = 1;
          uriLastCreated = GetSelected().renderableReference.uri;
        }
        else
        {
          numDuplicatesCreated++;
        }
      }
    }

    return true;
  }

  private void SetPfxIdOnActor(VoosActor actor, string pfxId)
  {
    string lastPfxId = hoverActor.GetPfxId();
    undoStack.PushUndoForActor(
    hoverActor,
    $"Set particle effect for {hoverActor.GetDisplayName()}",
    hoverActor =>
    {
      hoverActor.SetPfxId(pfxId);
      hoverActor.ApplyPropertiesToClones();
    },
    hoverActor =>
    {
      hoverActor.SetPfxId(lastPfxId);
      hoverActor.ApplyPropertiesToClones();
    });
    hoverActor.SetPfxId(pfxId);
  }

  private void SetSfxIdOnActor(VoosActor actor, string sfxId)
  {
    string lastSfxId = hoverActor.GetSfxId();
    undoStack.PushUndoForActor(
    hoverActor,
    $"Set sound effect for {hoverActor.GetDisplayName()}",
    hoverActor =>
    {
      hoverActor.SetSfxId(sfxId);
      hoverActor.ApplyPropertiesToClones();
    },
    hoverActor =>
    {
      hoverActor.SetSfxId(lastSfxId);
      hoverActor.ApplyPropertiesToClones();
    });
    hoverActor.SetSfxId(sfxId);
  }

  public override bool IsSelectionLocked()
  {
    return false;
  }

  void MaybeShowCopyTip()
  {
    if (didShowCopyTip)
    {
      return;
    }

    if (numDuplicatesCreated > 5)
    {
      popups.Show($"Making lots of {GetSelected().name}s? You may want to copy them instead (CTRL + C) so you can easily edit them all together later.", "Got it", () => { }, 800f);
      didShowCopyTip = true;
    }
  }

  public override bool KeyLock()
  {
    return creationLibrary.IsSearchActive();
  }

  void UpdateAsset(ActorableSearchResult result)
  {
    this.result = result;
    ActorableSearchResult tempResult = result;
    createToolPreview.SetRenderableByReference(
      result.renderableReference,
      GetAdditionalRotation(),
      result.GetRenderableOffset(),
      result.preferredRotation,
      result.preferredScaleFunction,
      () => { return tempResult.renderableReference.uri == this.result.renderableReference.uri; });
  }

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);

    creationLibrary = editMain.GetCreationLibrarySidebar();
    creationLibrary.updateAsset += UpdateAsset;
    UpdateAsset(creationLibrary.GetLastResult());
    creationLibrary.RequestOpen();
    createToolRay.SetLocalRayOriginTransform(emissionAnchor);
    createToolRay.SetTint(editMain.GetAvatarTint());
    createToolPreview.SetTint(editMain.GetAvatarTint());
    editMain.TryEscapeOutOfCameraView();
  }

  public override void Close()
  {
    base.Close();
    creationLibrary.updateAsset -= UpdateAsset;
    creationLibrary.RequestClose();
    editMain.modelLoadingObject.SetActive(false);
  }

  private void Update()
  {
    if (!editMain.UserMainKeyLock())
    {
      if (inputControl.GetButtonDown("Rotate"))
      {
        rotationMod = rotationMod + 45;
      }
    }

    if (inputControl.GetButtonDown("Action1"))
    {
      menuClickedOnMouseDown = editMain.CursorOverUI();
    }

    snapping = inputControl.GetButton("Snap");

    bool spawningEffectOnActor =
    hoverActor != null && (result.pfxId != null || result.sfxId != null);

    editMain.modelLoadingObject.SetActive(IsCurrentAssetRenderableEmpty());
    createToolPreview.gameObject.SetActive(!spawningEffectOnActor);
    rayContainerObject.SetActive(!editMain.Using3DCamera() && !spawningEffectOnActor);
  }

  public GameObject GetRenderableObject()
  {
    return createToolPreview.GetResultRenderable();
  }

  public ActorableSearchResult GetSelected()
  {
    return result;
  }

  public bool IsCurrentAssetRenderableEmpty()
  {
    return createToolPreview.GetResultRenderable() == null;
  }

  public override void UpdatePosition(Vector3 newpos)
  {
    targetPosition = snapping ? TerrainManager.SnapPosition(newpos) : newpos;
    selectorNode.position = targetPosition;
    selectorNode.rotation = GetAdditionalRotation();
    createToolRay.UpdateRayWithObject(selectorNode.gameObject);
  }

  public override string GetToolEffectName()
  {
    return "CreateToolRayEffect";
  }

  public override Vector3 GetToolEffectTargetPosition()
  {
    return targetPosition;
  }

  public override bool GetToolEffectActive()
  {
    return true;
  }

  public override string GetCreatePreviewSettingsJson()
  {
    var settings = new UserBody.CreatePreviewSettings
    {
      renderable = GetSelected().renderableReference,
      renderableOffset = GetSelected().GetRenderableOffset(),
      renderableRotation = GetSelected().preferredRotation,
      addlRotation = GetAdditionalRotation(),
      scale = GetScale()
    };
    return JsonUtility.ToJson(settings);
  }

  public override bool OnEscape()
  {
    return creationLibrary.OnEscape();
  }
}
