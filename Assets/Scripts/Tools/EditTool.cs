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
using System.Linq;

public class EditTool : Tool
{
  public const string NAME = "Edit";
  [SerializeField] GameObject inspectorEffectPrefab;
  [SerializeField] AudioSource audioSource;
  [SerializeField] AudioClip openBehaviorSoundClip;

  Transform selectionEffect;
  InspectorController inspectorController;
  ToolRingFXColor effectColor;

  const float TOOLBAR_DISABLED_ALPHA = .25f;

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);
    selectionEffect = Instantiate(inspectorEffectPrefab, transform).transform;
    effectColor = selectionEffect.GetComponent<ToolRingFXColor>();

    selectionEffect.gameObject.SetActive(false);
    inspectorController = editMain.GetInspectorController();
    inspectorController.onOpenActor = OnOpenActor;
    ForceUpdateTargetActor();

    inspectorController.Show();
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return editMain.GetTargetActorsCount() != 1;
  }

  void OnOpenActor(VoosActor actor)
  {
    if (actor != editMain.GetSingleTargetActor())
    {
      editMain.SetTargetActor(actor);
    }

    if (editMain.GetTargetActorsCount() > 0 && inspectorController.GetIsShowing())
    {
      editMain.TryEscapeOutOfCameraView();
    }

  }

  public override void Close()
  {
    // inspectorController.SwitchTab(toolMemory.inspectorTabIndex);
    inspectorController.Hide();

    Destroy(selectionEffect.gameObject);
    editMain.GetUserBody().SetHologramMenu(false);
    base.Close();
  }

  bool SelectionEffectActive()
  {
    return selectionEffect.gameObject.activeSelf;
  }

  void UpdateSelectionEffectVisibility(bool value)
  {
    if (value && !SelectionEffectActive()) audioSource.PlayOneShot(openBehaviorSoundClip);

    selectionEffect.gameObject.SetActive(value);
    editMain.GetUserBody().SetHologramMenu(value);
    if (value) effectColor.SetTint(editMain.GetAvatarTint());
  }

  public override string GetInvalidActorReason(VoosActor actor)
  {
    if (actor == null) return null;
    if (actor.GetWasClonedByScript())
    {
      return "LOCKED\nScript clones cannot be edited";
    }
    return base.GetInvalidActorReason(actor);
  }

  public override bool Trigger(bool on)
  {
    base.Trigger(on);

    if (on && !editMain.CursorOverUI()) return OnTrigger();
    else return true;
  }

  bool OnTrigger()
  {
    if (hoverActor != null && GetInvalidActorReason(hoverActor) == null)
    {
      bool addedOrPresent = editMain.AddSetOrRemoveTargetActor(hoverActor);

      if (editMain.GetTargetActorsCount() == 1)
      {
        inspectorController.SetActor(hoverActor);
        UpdateSelectionEffectVisibility(true);
      }
      return true;
    }
    else
    {
      if (!Util.HoldingModiferKeys())
      {
        editMain.ClearTargetActors();

      }
      return false;
    }
  }

  public override void ForceUpdateTargetActor()
  {
    if (editMain.GetSingleTargetActor() == null)
    {
      inspectorController.SetActor(null);
      UpdateSelectionEffectVisibility(false);

      return;
    }

    inspectorController.SetActor(editMain.GetSingleTargetActor());
    // inspectorController.SwitchTab(toolMemory.inspectorTabIndex);
    inspectorController.Show();
    UpdateSelectionEffectVisibility(true);

  }

  public override bool OnEscape()
  {
    if (editMain.GetSingleTargetActor() == null)
    {
      return false;
    }

    if (!inspectorController.OnMenuRequest())
    {
      editMain.SetTargetActor(null);
      ForceUpdateTargetActor();
    }

    return true;
  }

  public override bool KeyLock()
  {
    return inspectorController.KeyLock();
  }

  public override string GetName()
  {
    return NAME;
  }

  private void Update()
  {


    if (editMain.GetSingleTargetActor() != inspectorController.GetActor())
    {
      inspectorController.SetActor(editMain.GetSingleTargetActor());
    }

    UpdateSelectionEffectVisibility(editMain.GetSingleTargetActor() != null);

    //BehaviorAnimationUpdate();
  }

  /*  void BehaviorAnimationUpdate()
  {
    bool inspectorOn = actorInspector.IsOpenedOrOpening();
    editMain.GetUserBody().SetHologramMenu(inspectorOn);
  }
  */

  private void LateUpdate()
  {
    if (editMain.GetSingleTargetActor() != null)
    {
      selectionEffect.transform.position = editMain.GetSingleTargetActor().ComputeWorldRenderBounds().center;
      float scale = Mathf.Sqrt(Mathf.Pow(editMain.GetSingleTargetActor().ComputeWorldRenderBounds().size.x, 2) + Mathf.Pow(editMain.GetSingleTargetActor().ComputeWorldRenderBounds().size.z, 2));

      selectionEffect.transform.localScale = Vector3.one * (scale + .5f);
    }
  }

  public override bool GetToolEffectActive()
  {
    return inspectorController.GetActor() != null;
  }

  public override int GetToolEffectTargetViewId()
  {
    VoosActor actor = inspectorController.GetActor();
    return actor != null ? actor.GetPrimaryPhotonViewId() : -1;
  }

}
