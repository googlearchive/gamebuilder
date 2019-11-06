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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BehaviorProperties;
using UnityEngine;
using Behaviors;

// Stateful class that handles the logic of editing an actor's behaviors
public class ActorBehaviorsEditor : IEquatable<ActorBehaviorsEditor>
{
  readonly VoosEngine engine;

  readonly UndoStack undoStack;

  readonly BehaviorSystem behaviorSystem;

  // Store the ID instead of a ref so we can safely keep this around for undo
  // stack entries.
  readonly string actorId;

  VoosActor actor { get { return engine.GetActor(actorId); } }

  public static ActorBehaviorsEditor FromActor(VoosActor actor)
  {
    return new ActorBehaviorsEditor(actor.GetName(), actor.GetEngine(), null);
  }

  public ActorBehaviorsEditor(string actorId, VoosEngine engine, UndoStack undoStack)
  {
    this.actorId = actorId;
    this.engine = engine;
    this.undoStack = undoStack;
    this.behaviorSystem = actor.GetBehaviorSystem();
  }

  public bool IsValid()
  {
    return this.actor != null && behaviorSystem.HasBrain(this.actor.GetBrainName());
  }

  public bool CanWrite()
  {
    return IsValid() && this.actor.IsLocallyOwned();
  }

  public BehaviorSystem GetBehaviorSystem()
  {
    return behaviorSystem;
  }

  public UnassignedBehavior CreateNewBehavior(string initCode)
  {
    return CreateNewBehavior(initCode, null);
  }

  public UnassignedBehavior CreateNewBehavior(string initCode, string metadataJson)
  {
    string behaviorId = GetBehaviorSystem().GenerateUniqueId();
    GetBehaviorSystem().PutBehavior(behaviorId, new Behaviors.Behavior
    {
      label = "Custom",
      metadataJson = metadataJson,
      javascript = initCode
    });

    UnassignedBehavior newBehavior = new UnassignedBehavior(BehaviorSystem.IdToEmbeddedBehaviorUri(behaviorId), GetBehaviorSystem());

    // NOTE: We don't need to add it here, per se. The caller should call AddBehavior on us with this instance.
    return newBehavior;
  }

  public void CreateOwnCopyOfBrain()
  {
    Debug.Log($"{actor.GetDisplayName()} ({actor.GetName()}) is cloning its brain to have its own copy");
    string newBrainId = BehaviorSystem.CloneBrain(GetBehaviorSystem(), actor.GetBrainName());
    actor.SetBrainName(newBrainId);
    actor.ApplyBrainNameToClones();
  }

  void OnBeforeAnyChange()
  {
    if (!actor.IsLocallyOwned())
    {
      throw new System.Exception($"You may not edit the behaviors of an actor you do not own - sorry! Tried to edit actor {actor.GetDisplayName()} ({actor.GetName()})");
    }

    // Enforce copy-on-write. Or just create brain on write.
    // NOTE: This will never be undone - that's OK. Just slightly wasteful.

    Debug.AssertFormat(!actor.GetBrainName().IsNullOrEmpty(), "{0} had null/empty brain..?", actor.GetDebugName());

    if (actor.GetBrainName() == VoosEngine.DefaultBrainUid)
    {
      CreateOwnCopyOfBrain();
    }
  }

  public void SetProperties(AssignedBehavior assigned, Behaviors.PropertyAssignment[] properties)
  {
    var use = GetUse(assigned.useId);
    Debug.Assert(use.id == assigned.useId);
    Debug.Assert(use.behaviorUri == assigned.GetBehaviorUri());
    use.propertyAssignments = properties;
    PutUse(use);
  }

  public string GetMetadataJson()
  {
    return GetBehaviorSystem().GetBrain(actor.GetBrainName()).metadataJson;
  }

  public void SetMetadataJson(string json)
  {
    OnBeforeAnyChange();
    var brain = GetBehaviorSystem().GetBrain(actor.GetBrainName());
    brain.metadataJson = json;
    GetBehaviorSystem().PutBrain(actor.GetBrainName(), brain);
  }

  public AssignedBehavior AddBehavior(UnassignedBehavior behavior)
  {
    OnBeforeAnyChange();
    Debug.Assert(!behavior.behaviorUri.IsNullOrEmpty());

    string finalBehaviorUri = behavior.behaviorUri;

    // Alright, some special logic here. If it's a user-library behavior, do not
    // just use the URI directly. Import it, so turn it into embedded, then use
    // it. We want VOOS files to be stand-alone, so we can't have any local user
    // library dependencies.
    if (BehaviorSystem.IsUserLibraryBehaviorUri(behavior.behaviorUri))
    {
      Behaviors.Behavior importedBehavior = GetBehaviorSystem().GetBehaviorData(behavior.behaviorUri);
      Debug.Assert(!importedBehavior.userLibraryFile.IsNullOrEmpty());
      string importedId = GetBehaviorSystem().GenerateUniqueId();
      GetBehaviorSystem().PutBehavior(importedId, importedBehavior);
      string importedUri = BehaviorSystem.IdToEmbeddedBehaviorUri(importedId);
      finalBehaviorUri = importedUri;
    }

    // Create the use in the database
    string useId = actor.GetBehaviorSystem().GenerateUniqueId();
    var brain = GetBrain();
    brain.AddUse(new BehaviorUse
    {
      id = useId,
      behaviorUri = finalBehaviorUri,
      propertyAssignments = new Behaviors.PropertyAssignment[0]
    });
    GetBehaviorSystem().PutBrain(GetBrainId(), brain);

    return new AssignedBehavior(useId, this);
  }

  public void RemoveBehavior(AssignedBehavior behavior)
  {
    OnBeforeAnyChange();
    var brain = GetBrain();
    brain.DeleteUse(behavior.useId);
    // Important for triggering "onCardRemoved" in behaviors.
    GetBehaviorSystem().NotifyCardRemoved(GetBrainId(), behavior.useId);
    GetBehaviorSystem().PutBrain(GetBrainId(), brain);
  }

  public IEnumerable<AssignedBehavior> GetAssignedBehaviors()
  {
    return GetBrain().behaviorUses.Select(use => new AssignedBehavior(use.id, this));
  }

  public string GetBehaviorMetadataJson(UnassignedBehavior b)
  {
    return GetBehaviorSystem().GetBehaviorData(b.behaviorUri).metadataJson;
  }

  public string GetUseBehaviorUri(string useId)
  {
    return GetUse(useId).behaviorUri;
  }

  internal string GetUseMetaJson(string useId)
  {
    return GetUse(useId).metadataJson;
  }

  void PutUse(BehaviorUse use)
  {
    OnBeforeAnyChange();
    var brain = GetBrain();
    brain.SetUse(use);
    GetBehaviorSystem().PutBrain(GetBrainId(), brain);
  }

  BehaviorUse GetUse(string useId)
  {
    return GetBrain().GetUse(useId);
  }

  internal void SetUseMetaJson(string useId, string json)
  {
    var use = GetUse(useId);
    if (json == use.metadataJson)
    {
      return;
    }
    use.metadataJson = json;
    PutUse(use);
  }

  public Brain GetBrain()
  {
    return GetBehaviorSystem().GetBrain(GetBrainId());
  }

  int undoScopeCount = 0;

  struct UndoScope : System.IDisposable
  {
    readonly ActorBehaviorsEditor editor;
    readonly bool outerMostLevel;
    readonly Brain beforeBrain;
    readonly string label;

    // TODO: We could also use this to be smarter about RPCs. Ie. don't actually
    // call BehaviorSystem.SetBrain until the outermost scope is disposed!

    public UndoScope(string label, ActorBehaviorsEditor editor)
    {
      this.label = label;
      this.editor = editor;
      Debug.Assert(editor.undoScopeCount >= 0);
      editor.undoScopeCount++;

      this.outerMostLevel = editor.undoScopeCount == 1;

      if (outerMostLevel)
      {
        beforeBrain = editor.GetBrain();
      }
      else
      {
        // Doesn't matter.
        beforeBrain = new Brain();
      }
    }

    public void Dispose()
    {
      editor.undoScopeCount--;
      Debug.Assert(editor.undoScopeCount >= 0);

      if (!this.outerMostLevel)
      {
        return;
      }

      // We're outer-most. Push the undo item.
      var undoEditor = this.editor;
      Brain undoBrain = this.beforeBrain;
      Brain currBrain = editor.GetBrain();
      bool isRedo = false;

      System.Action<VoosActor> doFunc = redoActor =>
      {
        Debug.Assert(redoActor.GetName() == undoEditor.actor.GetName());
        undoEditor.SetBrain(currBrain);
        if (isRedo)
        {
          redoActor.NotifyBrainUndoRedo();
        }
        else
        {
          // No need to notify now - but future calls wil be redos.
          isRedo = true;
        }
      };

      if (editor.undoStack == null)
      {
        doFunc.Invoke(editor.actor);
      }
      else
      {
        editor.undoStack.PushUndoForActor(editor.actor, label,
        doFunc,
        undoActor =>
        {
          Debug.Assert(undoActor.GetName() == undoEditor.actor.GetName());
          undoEditor.SetBrain(undoBrain);
          undoActor.NotifyBrainUndoRedo();
        });
      }
    }
  }

  public System.IDisposable StartUndo(string label)
  {
    return new UndoScope(label, this);
  }

  public void SetBrain(Brain brain)
  {
    OnBeforeAnyChange();
    GetBehaviorSystem().PutBrain(GetBrainId(), brain);
  }

  public string GetActorName()
  {
    return actor.GetName();
  }

  public string GetActorDisplayName()
  {
    return actor.GetDisplayName();
  }

  public string GetBrainId()
  {
    return actor.GetBrainName();
  }

  public override bool Equals(object obj)
  {
    return Equals(obj as ActorBehaviorsEditor);
  }

  public bool Equals(ActorBehaviorsEditor other)
  {
    return other != null &&
           EqualityComparer<VoosEngine>.Default.Equals(engine, other.engine) &&
           actorId == other.actorId;
  }

  public override int GetHashCode()
  {
    var hashCode = 518469258;
    hashCode = hashCode * -1521134295 + EqualityComparer<VoosEngine>.Default.GetHashCode(engine);
    hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(actorId);
    return hashCode;
  }
}
