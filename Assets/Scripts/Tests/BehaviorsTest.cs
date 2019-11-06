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

using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Behaviors;
using static BehaviorSystem;

public class BehaviorsTest
{
  private BehaviorSystem behaviorSystem;
  private VoosEngine voosEngine;

  public IEnumerator Setup()
  {
    TestUtil.TestScene scene = new TestUtil.TestScene("main");
    yield return scene.LoadAndWait();

    behaviorSystem = scene.FindRootComponent<BehaviorSystem>("ScriptingSystems");
    voosEngine = scene.FindRootComponent<VoosEngine>("ScriptingSystems");

    // Wait for loading done..
    while (true)
    {
      if (voosEngine.GetIsRunning())
      {
        yield break;
      }
      yield return null;
    }
  }

  struct TestUse
  {
    public string brainId;
    public string behaviorUri;
  }

  void PutTestUse(TestUse test)
  {
    var use = new BehaviorUse { behaviorUri = test.behaviorUri, id = NewGUID() };
    var brain = new Brain { behaviorUses = new BehaviorUse[] { use } };
    behaviorSystem.PutBrain(test.brainId, brain);
  }

  string NewGUID()
  {
    return behaviorSystem.GenerateUniqueId();
  }

  [UnityTest]
  public IEnumerator TestBasic()
  {
    yield return Setup();

    VoosActor actor = voosEngine.CreateActor(new Vector3(1, 2, 3), Quaternion.identity, _ => { });
    Assert.NotNull(actor);
    Assert.AreEqual(1.0, actor.GetTint().r, 1e-4);

    string brainId = actor.GetBrainName();
    string behaviorId = NewGUID();
    string js = @"
        export function onTick(api) {
          setTint(0.12, 0.34, 0.56);
        }
    ";
    behaviorSystem.PutBehavior(behaviorId, new Behavior { javascript = js });
    var use = new BehaviorUse
    {
      id = NewGUID(),
      behaviorUri = IdToEmbeddedBehaviorUri(behaviorId)
    };
    var brain = new Brain { behaviorUses = new BehaviorUse[] { use } };
    behaviorSystem.PutBrain(brainId, brain);

    // Let it run at least one voos update
    yield return new WaitForEndOfFrame();
    yield return new WaitForEndOfFrame();

    Assert.AreEqual(0.12, actor.GetTint().r, 1e-4);
  }


  [UnityTest]
  public IEnumerator TestHandleMultipleInOneFrame()
  {
    yield return Setup();

    VoosActor actor = voosEngine.CreateActor(new Vector3(1, 2, 3), Quaternion.identity, newActor => { });
    actor.SetTint(new Color(0.1f, 0.1f, 0.1f));
    Assert.NotNull(actor);
    Assert.AreEqual(1.0, actor.transform.position.x, 1e-4);

    string brainId = actor.GetBrainName();
    string behaviorId = behaviorSystem.GenerateUniqueId();
    string js = @"
    export function onMoveRight() {
      const c = getTint();
      c.r += 0.1;
      setTint(c.r, c.g, c.b);
    }
    ";
    behaviorSystem.PutBehavior(behaviorId, new Behaviors.Behavior { javascript = js });
    PutTestUse(new TestUse
    {
      behaviorUri = IdToEmbeddedBehaviorUri(behaviorId),
      brainId = brainId
    });

    // We expect x to increment by 2..
    voosEngine.EnqueueMessage(new VoosEngine.ActorMessage { name = "MoveRight", targetActor = actor.GetName() });
    voosEngine.EnqueueMessage(new VoosEngine.ActorMessage { name = "MoveRight", targetActor = actor.GetName() });

    // Let it run at least one voos update
    yield return new WaitForEndOfFrame();
    yield return new WaitForEndOfFrame();

    Assert.AreEqual(0.3, actor.GetTint().r, 1e-4);
  }

  [Test]
  public void TestBehaviorDbGarbageCollect()
  {
    var db = new Behaviors.Database();

    db.behaviors.Set("A", new Behavior { javascript = "test();" });
    db.behaviors.Set("B", new Behavior { javascript = "test();" });
    db.behaviors.Set("C", new Behavior { javascript = "test();" });
    db.behaviors.Set("D", new Behavior { javascript = "test();" });

    db.brains.Set("b0", new Brain
    {
      behaviorUses = new BehaviorUse[]{
        new BehaviorUse{id="u00", behaviorUri="embedded:B"},
        new BehaviorUse{id="u01", behaviorUri="embedded:C"}
      }
    });

    db.brains.Set("b1", new Brain
    {
      behaviorUses = new BehaviorUse[]{
        new BehaviorUse{id="u10", behaviorUri="embedded:C"},
        new BehaviorUse{id="u11", behaviorUri="embedded:D"}
      }
    });

    var usedBrainIds = new HashSet<string> { "b0" };

    db.GarbageCollect(false, usedBrainIds);

    Assert.IsTrue(db.brains.Exists("b0"), "Used brain not deleted");
    Assert.IsFalse(db.brains.Exists("b1"), "Unused brain is deleted");

    // All behaviors left alone
    Assert.IsTrue(db.behaviors.Exists("A"), "Unused behavior not deleted");
    Assert.IsTrue(db.behaviors.Exists("B"), "Unused behavior not deleted");
    Assert.IsTrue(db.behaviors.Exists("C"), "Unused behavior not deleted");
    Assert.IsTrue(db.behaviors.Exists("D"), "Unused behavior not deleted");

  }

  [Test]
  public void TestBehaviorDbGarbageCollectDeleteUnusedBehaviors()
  {
    var db = new Behaviors.Database();

    db.behaviors.Set("A", new Behaviors.Behavior { javascript = "test();" });
    db.behaviors.Set("B", new Behaviors.Behavior { javascript = "test();" });
    db.behaviors.Set("C", new Behaviors.Behavior { javascript = "test();" });
    db.behaviors.Set("D", new Behaviors.Behavior { javascript = "test();" });

    db.brains.Set("b0", new Brain
    {
      behaviorUses = new BehaviorUse[]{
        new BehaviorUse{id="u00", behaviorUri="embedded:B"},
        new BehaviorUse{id="u01", behaviorUri="embedded:C"}
      }
    });

    db.brains.Set("b1", new Brain
    {
      behaviorUses = new BehaviorUse[]{
        new BehaviorUse{id="u10", behaviorUri="embedded:C"},
        new BehaviorUse{id="u11", behaviorUri="embedded:D"}
      }
    });

    var usedBrainIds = new HashSet<string> { "b0" };

    db.GarbageCollect(true, usedBrainIds);

    Assert.IsTrue(db.brains.Exists("b0"), "Used brain not deleted");

    Assert.IsFalse(db.brains.Exists("b1"), "Unused brain is deleted");

    // Only used behaviors left..
    Assert.IsFalse(db.behaviors.Exists("A"));
    Assert.IsTrue(db.behaviors.Exists("B"));
    Assert.IsTrue(db.behaviors.Exists("C"));
    Assert.IsFalse(db.behaviors.Exists("D"));

  }

}
