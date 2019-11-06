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

public class ActorLockedContentChecker : MonoBehaviour
{
  [SerializeField] GameObject waitingToEditPrompt;
  [SerializeField] TMPro.TextMeshProUGUI waitingToEditText;

  private System.Action showCallback;
  private System.Action hideCallback;

  const float OWNERSHIP_CHECK_DELAY_S = 0.5f;
  private float lastOwnershipRequest = Mathf.NegativeInfinity;
  private VoosActor currActor;

  private bool isShowingContent = false;

  public void Open(VoosActor actor, System.Action showCallback, System.Action hideCallback)
  {
    Debug.Assert(actor != null);
    if (currActor != null)
    {
      Close();
    }
    currActor = actor;
    this.showCallback = showCallback;
    this.hideCallback = hideCallback;
    BindActor();
    gameObject.SetActive(true);
  }

  public void Close()
  {
    if (!gameObject.activeSelf) return;
    SetShowingContentInternal(false);
    UnbindActor();
    gameObject.SetActive(false);
  }

  void Update()
  {
    waitingToEditPrompt.gameObject.SetActive(false);
    if (IsOwnershipPending())
    {
      if (currActor.IsLocallyOwned())
      {
        // We've successfully gotten ownership. Reset the time and show inspector contents.
        lastOwnershipRequest = Mathf.NegativeInfinity;
        SetShowingContentInternal(true);
      }
      else if (Time.unscaledTime > lastOwnershipRequest + OWNERSHIP_CHECK_DELAY_S)
      {
        // We've failed to get ownership in the allotted time. Show the waiting to edit message.
        SetShowingContentInternal(false);
        waitingToEditPrompt.gameObject.SetActive(true);
        waitingToEditText.text = $"Waiting to edit after {currActor.GetLockingOwnerNickName()}";
      }
    }
    else if (!currActor.IsLocallyOwned())
    {
      // Safeguard case:
      // We may be showing content already, but have somehow lost ownership.
      SetShowingContentInternal(false);
      waitingToEditPrompt.gameObject.SetActive(true);
    }
  }

  private void BindActor()
  {
    // Ideally, we'd do this with each actor.SetVoosField call...it's OK if
    // while we're editing, someone else edits. Their edits should be reflected
    // immediately in our UI, and we should be able to immediately edit it
    // again.
    currActor.WantLock();
    currActor.RequestOwnership();
    lastOwnershipRequest = Time.realtimeSinceStartup;
  }

  private void UnbindActor()
  {
    currActor.UnwantLock();
  }

  private bool IsOwnershipPending()
  {
    return lastOwnershipRequest >= 0;
  }

  private void SetShowingContentInternal(bool showing)
  {
    if (isShowingContent == showing) return;
    isShowingContent = showing;
    if (isShowingContent)
    {
      showCallback();
    }
    else
    {
      hideCallback();
    }
  }

}