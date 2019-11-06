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

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ActorUndoUtil
{
  // Best-effort semantics. This will silently fail and just not call thenFunc
  // is the actor can't be found or can't be owned before timeout.
  public static void GetValidActorThen(
    VoosEngine engineRef, string actorName,
    System.Action<VoosActor> thenFunc)
  {
    VoosActor currentActor = engineRef.GetActor(actorName);
    if (currentActor == null) return;
    currentActor.RequestOwnershipThen(() => thenFunc(currentActor));
  }

  public static string GetUnableToEditActorReason(VoosEngine engineRef, string actorName)
  {
    VoosActor currentActor = engineRef.GetActor(actorName);
    if (currentActor == null)
    {
      return $"The actor does not exist anymore.";
    }
    if (currentActor.IsLockedByAnother())
    {
      return $"{currentActor.GetLockingOwnerNickName()} is editing '{currentActor.GetDisplayName()}'.";
    }
    return null;
  }

  public static void PushUndoForActor(this UndoStack stack, VoosActor theActor, string label, System.Action<VoosActor> doIt, System.Action<VoosActor> undo)
  {
    // IMPORTANT: Do *NOT* use a reference to the actor! It may be deleted,
    // un-deleted, etc. So use its name, which is stable.
    string actorName = theActor.GetName();

    // Assume the VoosEngine instance is stable.
    VoosEngine engineRef = theActor.GetEngine();

    stack.Push(new UndoStack.Item
    {
      actionLabel = label,
      getUnableToDoReason = () => GetUnableToEditActorReason(engineRef, actorName),
      getUnableToUndoReason = () => GetUnableToEditActorReason(engineRef, actorName),
      doIt = () => GetValidActorThen(engineRef, actorName, doIt),
      undo = () => GetValidActorThen(engineRef, actorName, undo)
    });
  }

  public static void PushUndoForCreatingActor(this UndoStack stack, VoosActor theActor, string label)
  {
    var theActors = new List<VoosActor>();
    theActors.Add(theActor);
    stack.PushUndoForCreatingActors(theActors, label);
  }

  // Use this if you *already* created the actor, and you want undo (to destroy
  // the actor) and redo (to re-create it)
  public static void PushUndoForCreatingActors(this UndoStack stack, List<VoosActor> theActors, string label)
  {
    Debug.Assert(theActors.Count > 0);
    var engineRef = theActors[0].GetEngine();
    var actorDataArray = (from actor in theActors select engineRef.SaveActor(actor)).ToArray();

    stack.Push(new UndoStack.Item
    {
      actionLabel = label,

      // No reason we can't create...I suppose maybe the name is
      // used..somehow..like some multiplayer race-condition..
      getUnableToDoReason = () => null,

      getUnableToUndoReason = () =>
      {
        foreach (var actorData in actorDataArray)
        {
          // Return the first one if any
          string reason = GetUnableToEditActorReason(engineRef, actorData.name);
          if (reason != null)
          {
            return reason;
          }
        }
        return null;
      },

      // Only called for re-do
      doIt = () =>
      {
        foreach (var actorData in actorDataArray)
        {
          engineRef.RestoreActor(actorData);
        }
      },

      undo = () =>
      {
        foreach (var actorData in actorDataArray)
        {
          VoosActor currentActor = engineRef.GetActor(actorData.name);
          if (currentActor != null)
          {
            engineRef.DestroyActor(currentActor);
          }
        }
      }
    },
    // Important that we don't invoke "doIt" immediately, since we already created it.
    false
    );
  }

  public static void PushUndoForMany(this UndoStack stack, VoosEngine engine, IEnumerable<VoosActor> actors, string verb, System.Action<VoosActor> doIt, System.Action<VoosActor> undo)
  {
    List<string> actorNames = (from actor in actors select actor.GetName()).ToList();

    if (actorNames.Count == 1)
    {
      // Special case this to be the fancier.
      string actorName = actorNames[0];
      if (GetUnableToEditActorReason(engine, actorName) == null)
      {
        VoosActor actor = engine.GetActor(actorName);
        stack.PushUndoForActor(actor, $"{verb} {actor.GetDisplayName()}", doIt, undo);
      }
      return;
    }

    stack.Push(new UndoStack.Item
    {
      actionLabel = $"{verb} {actorNames.Count} actors",
      // We'll just do best effort for all this - so never block it.
      getUnableToDoReason = () => null,
      getUnableToUndoReason = () => null,
      doIt = () =>
      {
        foreach (string name in actorNames)
        {
          GetValidActorThen(engine, name, doIt);
        }
      },
      undo = () =>
      {
        foreach (string name in actorNames)
        {
          GetValidActorThen(engine, name, undo);
        }
      }
    });
  }
}