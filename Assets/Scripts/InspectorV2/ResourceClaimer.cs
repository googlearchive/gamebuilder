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

class ResourceClaimer : MonoBehaviour
{
  [SerializeField] TMPro.TextMeshProUGUI waitingToEditMessage;

  private string resourceClaimId;
  public static float TRY_CLAIM_INTERVAL_S = 1f;
  private float timeSinceLastClaim;
  private ClaimKeeper claimKeeper;
  private IClaimToken claimToken;
  private System.Action<bool> claimStatusChangedCallback;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref claimKeeper);
  }

  public void Claim(string resourceClaimId, System.Action<bool> claimStatusChangedCallback)
  {
    Unclaim();

    this.resourceClaimId = resourceClaimId;
    this.claimStatusChangedCallback = claimStatusChangedCallback;
    timeSinceLastClaim = Time.realtimeSinceStartup;

    TryClaim();
  }

  public void Unclaim()
  {
    if (claimToken != null)
    {
      claimToken.Dispose();
      claimToken = null;
      claimStatusChangedCallback?.Invoke(false);
    }
    this.resourceClaimId = null;
  }

  public bool IsClaimed()
  {
    return claimToken != null;
  }

  void Update()
  {
    if (resourceClaimId == null) return;
    if (claimToken == null)
    {
      float time = Time.realtimeSinceStartup;
      if (time >= timeSinceLastClaim + TRY_CLAIM_INTERVAL_S)
      {
        TryClaim();
      }
    }
    if (claimToken != null && !claimToken.IsStillMine())
    {
      claimToken.Dispose();
      claimToken = null;
      claimStatusChangedCallback?.Invoke(false);
    }
  }

  private void TryClaim()
  {
    claimToken = claimKeeper.Claim(resourceClaimId);
    if (claimToken != null)
    {
      claimStatusChangedCallback?.Invoke(true);
      gameObject.SetActive(false);
    }
    else
    {
      timeSinceLastClaim = Time.realtimeSinceStartup;
      waitingToEditMessage.text = $"Waiting to edit after {claimKeeper.GetEffectiveOwnerNickname(resourceClaimId)}";
      gameObject.SetActive(true);
    }
  }
}