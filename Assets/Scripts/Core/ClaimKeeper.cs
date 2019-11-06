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

// Distributed Claims System.
//
// This protects resources from being concurrently edited by more than one player.
// It's not a *locking* system because it's optimistic and not 100% reliable.
// It's more of a civilized shouting system where each player claims resources
// and other players know not to claim the same ones. Occasionally (rarely)
// claim conflicts can happen, in which case everyone agrees on who wins.
//
// HOW IT WORKS: Every player maintains a list of "claims" (a list of resource IDs,
// where each resource ID is an opaque string that symbolizes some shared resource).
// These lists are networked, so everyone has an eventually consistent copy of all
// the lists.
//
// If only one player has a claim on a given resource, they are the owner of
// that resource. When they are done, they unclaim it.
//
// If a player realizes that a resource is claimed by someone else, they will
// kindly refrain from claiming it.
//
// So when does this go wrong? Rarely, but it could happen: if players 1 and 2
// both try to claim the same resource at the same time, unaware of each other's intentions,
// they will BOTH end up claiming it. When this happens, the resource belongs to the
// claimer WITH THE HIGHEST ID (player 2 in this case). Player 1 will kindly back away
// from it and unclaim it.
//
// In any event, note that ownership is CLEARLY DEFINED: everyone (eventually) agrees on who
// owns each resource.
//
// HOW TO USE THIS CLASS:
//
// 1. Come up with a good scheme for resource IDs to identify your resource.
//    Example: if you're controlling access to sound effects, you could
//    establish that the resource id is "SFX:" + soundEffectId.
//
// 2. When you want to start editing the resource, call Claim().
//
// 3. If that fails, don't edit the resource. If it succeeds, it returns an IClaimToken
//    that represents your claim. You are responsible for calling Dispose() on it when
//    you want to release the claim.
//
// 4. It's okay to start editing. But poll claimToken.IsStillMine() periodically
//    to check if the resource is still yours. It will usually be but, because claims
//    are asynchronous, you may suddenly lose ownership if there is a conflict and
//    you are the loser. If claimToken.IsStillMine() returns false at any point,
//    it means you've lost it. This should be rare. In that case close your editing
//    UI, apologize to the user, etc.
//
// 5. When you are done, REGARDLESS of whether you lost ownership or not, call
//    Dispose() on the claim token.
//
// TESTING
// If you want to test what happens when you get a conflicting claim, you can use
// the console commands: ckclaim, ckunclaim, cklist.
// A good test is:
//     Start editing your resource.
//     Check that you claimed it with cklist.
//     From another client with a higher ID, claim the same resource with ckclaim.
//     Verify that your UI correctly aborts.
public class ClaimKeeper : Photon.PunBehaviour
{
  // Claims by each player.
  private Dictionary<int, HashSet<string>> claimsByPlayerId = new Dictionary<int, HashSet<string>>();

  // Tries to claim the given resource.
  // If this returns an IClaimToken, the claim was successful. You must Dispose() of it when done.
  // If this returns null, the claim failed.
  public IClaimToken Claim(string resourceId)
  {
#if USE_PUN
    return DoClaim(resourceId) ? new ClaimToken(this, resourceId) : null;
#else
    return new ClaimToken(this, resourceId);
#endif
  }

  public string GetEffectiveOwnerNickname(string resourceId)
  {
#if USE_PUN
    int ownerId;
    if (!GetEffectiveOwner(resourceId, out ownerId)) return null;
    return PhotonPlayer.Find(ownerId).NickName;
#else
    return "NA";
#endif

  }

  public bool IsMine(string resourceId)
  {
#if USE_PUN
    int ownerId;
    return GetEffectiveOwner(resourceId, out ownerId) && ownerId == PhotonNetwork.player.ID;
#else
    return true;
#endif
  }

  // END OF PUBLIC INTERFACE. Users of this class only call Claim.
  // The rest of the logic is handled by the ClaimToken itself.
  // -------------------------------------------------------------------------------------------

  private class ClaimToken : IClaimToken
  {
    private bool disposed;
    private ClaimKeeper keeper;
    private string resourceId;

    public ClaimToken(ClaimKeeper keeper, string resourceId)
    {
      this.keeper = keeper;
      this.resourceId = resourceId;
    }

    public bool IsStillMine()
    {
      Debug.Assert(!disposed);
      return keeper.IsMine(resourceId);
    }

    public void Dispose()
    {
      if (!disposed)
      {
        if (keeper != null)  // Could be null on app shutdown
        {
#if USE_PUN
          keeper.DoUnclaim(resourceId);
#endif
        }
        disposed = true;
      }
    }
  }

#if USE_PUN
  private bool DoClaim(string resourceId, bool force = false)
  {
    int ownerId;
    if (!force && GetEffectiveOwner(resourceId, out ownerId) && ownerId != PhotonNetwork.player.ID)
    {
      // Nope. Someone else already has a claim on it.
      return false;
    }
    LocalAddClaim(PhotonNetwork.player.ID, resourceId);
    photonView.RPC("AddClaimRPC", PhotonTargets.AllViaServer, resourceId);
    // Nobody else seems to have a claim on it, but maybe we're just out of date.
    // So we have it... for now.
    return true;
  }

  // Releases a claim to a given resource.
  private void DoUnclaim(string resourceId)
  {
    LocalRemoveClaim(PhotonNetwork.player.ID, resourceId);
    photonView.RPC("RemoveClaimRPC", PhotonTargets.AllViaServer, resourceId);
  }

  // Returns the player ID that owns the given resource. Owning means having
  // an undisputed claim on it, or being the winner of a dispute, if there is
  // more than one claim on it.
  // Returns null if nobody owns it.
  private bool GetEffectiveOwner(string resourceId, out int ownerId)
  {
    bool foundOwner = false;
    ownerId = 0;
    foreach (KeyValuePair<int, HashSet<string>> pair in claimsByPlayerId)
    {
      // In case there's more than one claim to the same resource, the player with the
      // highest ID is considered the owner.
      if (pair.Value.Contains(resourceId) && (!foundOwner || ownerId < pair.Key))
      {
        ownerId = pair.Key;
        foundOwner = true;
      }
    }
    return foundOwner;
  }

  [PunRPC]
  void AddClaimRPC(string resourceId, PhotonMessageInfo info)
  {
    LocalAddClaim(info.sender.ID, resourceId);
  }

  [PunRPC]
  void RemoveClaimRPC(string resourceId, PhotonMessageInfo info)
  {
    LocalRemoveClaim(info.sender.ID, resourceId);
  }

  [PunRPC]
  void SetClaimsRPC(string claimedResourceIds, PhotonMessageInfo info)
  {
    claimsByPlayerId.Remove(info.sender.ID);
    foreach (string resourceId in claimedResourceIds.Split(','))
    {
      LocalAddClaim(info.sender.ID, resourceId);
    }
  }

  public override void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
  {
    // A new player connected, so everyone must restate their current claims.
    HashSet<string> myClaimsSet = GetClaimSetFor(PhotonNetwork.player.ID);
    string[] myClaims = new string[myClaimsSet.Count];
    int i = 0;
    foreach (string claim in myClaimsSet)
    {
      myClaims[i++] = claim;
    }
    photonView.RPC("SetClaimsRPC", PhotonTargets.AllViaServer, string.Join(",", myClaims));
  }

  public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
  {
    // Remove any claims held by that player.
    claimsByPlayerId.Remove(otherPlayer.ID);
  }

  private void LocalAddClaim(int playerId, string resourceId)
  {
    GetClaimSetFor(playerId).Add(resourceId);
  }

  private void LocalRemoveClaim(int playerId, string resourceId)
  {
    GetClaimSetFor(playerId).Remove(resourceId);
  }

  private HashSet<string> GetClaimSetFor(int playerId)
  {
    HashSet<string> claims;
    if (!claimsByPlayerId.TryGetValue(playerId, out claims))
    {
      claims = new HashSet<string>();
      claimsByPlayerId[playerId] = claims;
    }
    return claims;
  }

#if DEBUG
  [CommandTerminal.RegisterCommand(Help = "Claim a resource", MinArgCount = 1, MaxArgCount = 1)]
  public static void CommandCkClaim(CommandTerminal.CommandArg[] args)
  {
    GameObject.FindObjectOfType<ClaimKeeper>().DoClaim(args[0].ToString(), true);
  }

  [CommandTerminal.RegisterCommand(Help = "Unclaim a resource", MinArgCount = 1, MaxArgCount = 1)]
  public static void CommandCkUnclaim(CommandTerminal.CommandArg[] args)
  {
    GameObject.FindObjectOfType<ClaimKeeper>().DoUnclaim(args[0].ToString());
  }

  [CommandTerminal.RegisterCommand(Help = "Gets effective owner of a resource", MinArgCount = 1, MaxArgCount = 1)]
  public static void CommandCkGetOwner(CommandTerminal.CommandArg[] args)
  {
    int ownerId;
    if (GameObject.FindObjectOfType<ClaimKeeper>().GetEffectiveOwner(args[0].ToString(), out ownerId))
    {
      CommandTerminal.HeadlessTerminal.Log("Owner of " + args[0].ToString() + " is " + ownerId);
    }
    else
    {
      CommandTerminal.HeadlessTerminal.Log("Nobody owns " + args[0].ToString());
    }
  }

  [CommandTerminal.RegisterCommand(Help = "Lists current resource claims", MinArgCount = 0, MaxArgCount = 0)]
  public static void CommandCkList(CommandTerminal.CommandArg[] args)
  {
    ClaimKeeper keeper = GameObject.FindObjectOfType<ClaimKeeper>();
    foreach (KeyValuePair<int, HashSet<string>> pair in keeper.claimsByPlayerId)
    {
      foreach (string resourceId in pair.Value)
      {
        CommandTerminal.HeadlessTerminal.Log(pair.Key + " claims " + resourceId);
      }
    }
  }
#endif

#endif

}

public interface IClaimToken : System.IDisposable
{
  bool IsStillMine();
}
