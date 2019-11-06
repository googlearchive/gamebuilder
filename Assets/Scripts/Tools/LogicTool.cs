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

public class LogicTool : Tool
{

  public const string NAME = "Logic";
  [SerializeField] GameObject inspectorEffectPrefab;
  [SerializeField] AudioSource audioSource;
  [SerializeField] AudioClip openBehaviorSoundClip;

  Transform selectionEffect;

  LogicSidebar logicSidebar;

  public override void Launch(EditMain _editmain)
  {
    base.Launch(_editmain);

    logicSidebar = editMain.GetLogicSidebar();
    selectionEffect = Instantiate(inspectorEffectPrefab, transform).transform;
    selectionEffect.GetComponent<ToolRingFXColor>().SetTint(editMain.GetAvatarTint());

    selectionEffect.gameObject.SetActive(false);

    logicSidebar.OnSwitchToCodeView = OnSwitchToCodeView;
    logicSidebar.OnSwitchToCardView = OnSwitchToCardView;

    logicSidebar.onOpenActor = OnOpenActor;

    if (toolMemory.logicTabIndex == 0)
    {
      logicSidebar.SetToCardView();
    }
    else
    {
      logicSidebar.SetToCodeView();
    }

    if (editMain.GetSingleTargetActor() != null)
    {
      if (!(editMain.GetSingleTargetActor() == toolMemory.logicActor && toolMemory.logicSidebarClosed))
        ForceUpdateTargetActor();
    }
    else
    {
      logicSidebar.OpenWithParams(null);
    }
  }


  void OnOpenActor(VoosActor actor)
  {
    if (actor != editMain.GetSingleTargetActor() && logicSidebar.IsOpenedOrOpening())
    {
      editMain.SetTargetActor(actor);
    }

    if (editMain.GetTargetActorsCount() > 0 && logicSidebar.IsOpenedOrOpening())
    {
      editMain.TryEscapeOutOfCameraView();
    }
  }

  public override bool ShowSelectedTargetFeedback()
  {
    return editMain.GetTargetActorsCount() != 1;
  }

  public override void Close()
  {
    toolMemory.logicSidebarClosed = !logicSidebar.IsOpenedOrOpening();
    toolMemory.logicActor = editMain.GetSingleTargetActor();

    logicSidebar.RequestClose();

    Destroy(selectionEffect.gameObject);

    editMain.GetUserBody().SetHologramMenu(false);
    base.Close();
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
        logicSidebar.OpenWithParams(editMain.GetFocusedTargetActor());
        UpdateSelectionEffectVisibility(true);
      }
      return true;
    }
    else
    {
      /*  if (!Util.HoldingModiferKeys())
       {
         editMain.ClearTargetActors();

       } */
      return false;
    }
  }



  public override void ForceUpdateTargetActor()
  {

    logicSidebar.OpenWithParams(editMain.GetSingleTargetActor());
    selectionEffect.gameObject.SetActive(editMain.GetSingleTargetActor() == null);
  }


  public override bool OnEscape()
  {
    if (!logicSidebar.OnMenuRequest())
    {
      if (editMain.GetSingleTargetActor() != null)
      {
        editMain.SetTargetActor(null);
        ForceUpdateTargetActor();
      }
      else return false;
    }
    return true;
  }

  void OnSwitchToCodeView()
  {
    toolMemory.logicTabIndex = 1;
  }

  void OnSwitchToCardView()
  {
    toolMemory.logicTabIndex = 0;
  }

  public override bool KeyLock()
  {
    if (logicSidebar.IsOpenedOrOpening())
    {

      return logicSidebar.KeyLock();
    }
    else
    {
      return false;
    }
  }

  public override string GetName()
  {
    return NAME;
  }

  private void Update()
  {
    if (logicSidebar.IsOpenedOrOpening())
    {
      if (editMain.GetSingleTargetActor() != logicSidebar.GetActiveActor())
      {
        logicSidebar.OpenWithParams(editMain.GetSingleTargetActor());
        // editMain.SetTargetActor(logicSidebar.GetActiveActor());
      }

      UpdateSelectionEffectVisibility(editMain.GetSingleTargetActor() != null && !editMain.UsingFirstPersonCamera());

    }
    else
    {

      UpdateSelectionEffectVisibility(false);
    }
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
  }


  private void LateUpdate()
  {
    if (editMain.GetSingleTargetActor() != null)
    {
      selectionEffect.transform.position = editMain.GetSingleTargetActor().ComputeWorldRenderBounds().center;
      float scale = Mathf.Sqrt(Mathf.Pow(editMain.GetSingleTargetActor().ComputeWorldRenderBounds().size.x, 2) + Mathf.Pow(editMain.GetSingleTargetActor().ComputeWorldRenderBounds().size.z, 2));//* 1.5f;

      selectionEffect.transform.localScale = Vector3.one * (scale + .5f);
    }
  }

  public override bool GetToolEffectActive()
  {
    return logicSidebar.GetActiveActor() != null;

  }

  public override int GetToolEffectTargetViewId()
  {
    VoosActor actor = logicSidebar.GetActiveActor();
    return actor != null ? actor.GetPrimaryPhotonViewId() : -1;
  }

  public override string GetToolEffectName()
  {
    return "ToolFX_Logic";
  }
}
