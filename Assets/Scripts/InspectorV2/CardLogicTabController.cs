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

public class CardLogicTabController : MonoBehaviour
{
  [SerializeField] ActorContentChecker contentChecker;
  [SerializeField] CardManager cardManagerPrefab;
  [SerializeField] BehaviorCards behaviorCards;
  [SerializeField] RectTransform referenceRect;
  private BehaviorSystem behaviorSystem;
  private VoosActor actor;

  CardManager cardManager;

  bool contentRefreshQueued = false;

  public System.Action<VoosActor> onActorChanged;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref behaviorCards);
    Util.FindIfNotSet(this, ref behaviorSystem);

    cardManager = Instantiate(cardManagerPrefab, null);
    cardManager.SetReferenceRect(referenceRect);
    cardManager.onCloseRequest += () =>
    {
      Open(null);
      onActorChanged(null);
    };
    cardManager.Close();

    contentChecker.Setup();

    behaviorSystem.onBehaviorPut += (p) =>
    {
      // If it's a new card, it shouldn't affect our panels.
      if (!p.isNewBehavior) OnBehaviorsChanged();
    };
    behaviorSystem.onBehaviorDelete += (d) => OnBehaviorsChanged();
  }

  private void OnBehaviorsChanged()
  {
    if (cardManager.IsOpen())
    {
      contentRefreshQueued = true;
    }
  }

  void SetActor(VoosActor newActor)
  {
    if (this.actor == newActor) return;
    if (this.actor != null)
    {
      this.actor.onBrainChanged -= OnActorBrainChanged;
    }
    this.actor = newActor;
    if (this.actor != null)
    {
      this.actor.onBrainChanged += OnActorBrainChanged;
    }
  }

  public void Open(VoosActor actor)
  {
    gameObject.SetActive(true);
    SetActor(actor);
    contentChecker.Open(actor, OpenContent, CloseContent);
    contentRefreshQueued = false;
  }

  public void Close()
  {
    contentChecker.Close();
    gameObject.SetActive(false);
    SetActor(null);
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void RequestDestroy()
  {
    cardManager.RequestDestroy();
    Destroy(gameObject);
  }

  public bool OnMenuRequest()
  {
    return cardManager.OnMenuRequest();
  }

  void OnRequestCodeCard(ICardModel model)
  {
    // GetInspector().SwitchTab("code", new Dictionary<string, object> {
    //   { "selectedCardId", model.GetId() }
    // });
  }

  public bool KeyLock()
  {
    GameObject selected = EventSystem.current?.currentSelectedGameObject;
    // The less hacky way would be for each tab to implement this function individually. But YAGNI.
    // Equals comparison is needed for when object is destroyed.
    return selected != null && !selected.Equals(null) && selected.GetComponent<TMPro.TMP_InputField>() != null;
  }

  public CardManager GetManager()
  {
    return cardManager;
  }

  void OpenContent(VoosActor actor)
  {
    bool actorChanged = actor != this.actor;
    SetActor(actor);
    if (actorChanged) onActorChanged?.Invoke(actor);
    ICardManagerModel model = behaviorCards.GetCardManager(actor);
    cardManager.Close();
    cardManager.Open(model);
  }

  void CloseContent(bool noActor)
  {
    cardManager.Close();
    if (noActor) SetActor(null);
  }

  private void OnActorBrainChanged(bool isUndo)
  {
    if (isUndo)
    {
      contentRefreshQueued = true;
    }
  }

  void Update()
  {
    if (contentRefreshQueued)
    {
      contentRefreshQueued = false;
      OpenContent(actor);
    }
  }
}
