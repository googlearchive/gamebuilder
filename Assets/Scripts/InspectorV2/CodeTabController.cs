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
using UnityEngine.EventSystems;

public class CodeTabController : MonoBehaviour
{
  [SerializeField] CodeTabContentController contentController;
  [SerializeField] ResourceClaimer resourceClaimer;
  private string cardUri;
  private VoosEngine.BehaviorLogItem? error;
  private bool isNewCard;
  private BehaviorSystem behaviorSystem;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref behaviorSystem);
    resourceClaimer.Setup();
    contentController.Setup();

    behaviorSystem.onBehaviorDelete += (deleteEvent) =>
    {
      if (gameObject.activeSelf && BehaviorSystem.IdToEmbeddedBehaviorUri(deleteEvent.id) == cardUri)
      {
        Open(null);
      }
    };
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Open(string openCardUri = null, VoosEngine.BehaviorLogItem? error = null)
  {
    gameObject.SetActive(true);

    resourceClaimer.Unclaim();
    isNewCard = false;

    this.error = null;
    // Which card are we editing ?
    if (openCardUri != null && behaviorSystem.IsBehaviorUriValid(openCardUri))
    {
      cardUri = openCardUri;
      this.error = error;
    }
    else if (cardUri == null || !behaviorSystem.IsBehaviorUriValid(cardUri))
    {
      UnassignedBehavior newBehavior = behaviorSystem.CreateNewBehavior(
        CodeTemplates.MISC, BehaviorCards.GetMiscMetadataJson());
      cardUri = newBehavior.GetBehaviorUri();
      isNewCard = true;
    }

    resourceClaimer.Claim(UnassignedBehavior.GetClaimResourceId(cardUri), OnClaimStatusChanged);
  }

  public void Close()
  {
    gameObject.SetActive(false);
    resourceClaimer.Unclaim();
  }

  void OnClaimStatusChanged(bool claimed)
  {
    if (claimed)
    {
      // It's possible that the other user deleted the card before unclaiming.
      if (!behaviorSystem.IsBehaviorUriValid(cardUri))
      {
        Open(null);
      }
      else
      {
        contentController.Open(cardUri, isNewCard, error);
      }
    }
    else
    {
      contentController.Close();
    }
  }

  public bool KeyLock()
  {
    return resourceClaimer.IsClaimed() && contentController.KeyLock();
  }

  public bool OnMenuRequest()
  {
    return resourceClaimer.IsClaimed() && contentController.OnMenuRequest();
  }

}