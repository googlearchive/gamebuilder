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

public class ActorContentChecker : MonoBehaviour
{
  [SerializeField] GameObject clonePrompt;
  [SerializeField] TMPro.TextMeshProUGUI cloneMessage;
  [SerializeField] UnityEngine.UI.Button editOriginalButton;
  [SerializeField] UnityEngine.UI.Button breakLinkButton;
  [SerializeField] GameObject nothingSelectedPrompt;
  [SerializeField] GameObject multipleSelectedPrompt;
  [SerializeField] ActorLockedContentChecker lockedContentChecker;

  private UndoStack undo;
  private DynamicPopup popups;

  private VoosEngine voosEngine;
  private EditMain editMain;
  private VoosActor currActor;

  private System.Action<VoosActor> showCallback;
  private System.Action<bool> hideCallback;

  private bool isOpen = false;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref editMain);
    Util.FindIfNotSet(this, ref undo);
    Util.FindIfNotSet(this, ref popups);

    editOriginalButton.onClick.AddListener(() =>
    {
      SetActorInternal(voosEngine.GetActor(currActor.GetCloneParent()));
    });

    breakLinkButton.onClick.AddListener(() => BreakLink());
    voosEngine.onBeforeActorDestroy += (actor) =>
    {
      if (currActor != null && actor == currActor)
      {
        SetActorInternal(null);
        hideCallback(true);
      }
    };
  }

  void OnBreakLinkChanged(VoosActor actor)
  {
    if (actor == currActor) RefreshUI();
  }

  void Update()
  {
    if (isOpen && currActor == null)
    {
      NullActorRefresh();
    }
  }

  public void Open(VoosActor actor, System.Action<VoosActor> showCallback, System.Action<bool> hideCallback)
  {
    this.showCallback = showCallback;
    this.hideCallback = hideCallback;
    isOpen = true;
    gameObject.SetActive(true);
    SetActorInternal(actor);
  }

  public void Close()
  {
    gameObject.SetActive(false);
    isOpen = false;
    RefreshUI();
  }

  private void SetActorInternal(VoosActor actor)
  {
    currActor = actor;
    if (currActor != null)
    {
      BreakLinkIfCloneParentMissing();
    }
    RefreshUI();
  }

  void NullActorRefresh()
  {
    bool noActors = editMain.GetTargetActorsCount() == 0;
    nothingSelectedPrompt.gameObject.SetActive(noActors);
    multipleSelectedPrompt.gameObject.SetActive(!noActors);
  }

  private void RefreshUI()
  {
    nothingSelectedPrompt.gameObject.SetActive(false);
    multipleSelectedPrompt.gameObject.SetActive(false);
    clonePrompt.SetActive(false);
    lockedContentChecker.Close();

    if (!isOpen) return;

    if (currActor == null)
    {
      NullActorRefresh();
    }
    else if (currActor?.GetCloneParent() != null && currActor.GetCloneParent() != "")
    {
      clonePrompt.SetActive(true);
      VoosActor cloneParent = voosEngine.GetActor(currActor.GetCloneParent());
      cloneMessage.text = $"This is a copy of {cloneParent.GetDisplayName()}";
    }
    else
    {
      lockedContentChecker.Open(currActor, () =>
      {
        showCallback(currActor);
      }, () =>
      {
        hideCallback(false);
      });
    }
  }

  private void BreakLinkIfCloneParentMissing()
  {
    // If the clone parent has been deleted, break the link automatically.
    if (!string.IsNullOrEmpty(currActor?.GetCloneParent()) && null == voosEngine.GetActor(currActor.GetCloneParent()))
    {
      BreakLink(false);
    }
  }

  private void BreakLink(bool withUndo = true)
  {
    VoosActor actor = currActor;

    // Save for undo
    string prevParent = actor.GetCloneParent();

    SetActorInternal(null);
    actor.SetCloneParent(null);
    actor.MakeOwnCopyOfBrain();

    if (withUndo)
    {
      // Setup undo item
      string actorName = actor.GetName();
      string newBrain = actor.GetBrainName();
      undo.Push(new UndoStack.Item
      {
        actionLabel = $"Break copy-link of {actor.GetDisplayName()}",
        getUnableToDoReason = () => ActorUndoUtil.GetUnableToEditActorReason(voosEngine, actorName),
        doIt = () =>
        {
          var redoActor = voosEngine.GetActor(actorName);
          redoActor.SetCloneParent(null);
          // A bit sloppy: We're relying on the fact that brains are never deleted
          // (except on load).
          redoActor.SetBrainName(newBrain);
          OnBreakLinkChanged(redoActor);
        },
        getUnableToUndoReason = () =>
        {
          var prevParentActor = voosEngine.GetActor(prevParent);
          if (prevParentActor == null)
          {
            return $"The original copy no longer exists.";
          }
          return null;
        },
        undo = () =>
        {
          var undoActor = voosEngine.GetActor(actorName);
          var prevParentActor = voosEngine.GetActor(prevParent);
          Debug.Assert(prevParent != null, "BreakLink undo action: prevParent does not exist anymore");
          undoActor.SetCloneParent(prevParent);
          undoActor.SetBrainName(prevParentActor.GetBrainName());
          OnBreakLinkChanged(undoActor);
        }
      });
    }

    SetActorInternal(actor);
  }

  internal bool IsOpen()
  {
    return gameObject.activeSelf;
  }
}