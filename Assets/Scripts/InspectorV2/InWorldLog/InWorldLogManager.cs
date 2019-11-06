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
using UnityEngine;

public class InWorldLogManager : MonoBehaviour
{
  [SerializeField] RectTransform canvasRect;
  [SerializeField] InWorldLogPanel logPanelPrefab;
  VoosEngine voosEngine;
  BehaviorSystem behaviorSystem;
  //need user main to get the camera
  UserMain userMain;
  Camera mainCamera;

  Dictionary<VoosActor, InWorldLogPanel> currentPanels = new Dictionary<VoosActor, InWorldLogPanel>();

  bool setupComplete = false;

  void Start()
  {
    StartCoroutine(SetupRoutine());

    //foreach VooseEngine.enumerateactors
    //five nights at freddies
  }

  IEnumerator SetupRoutine()
  {
    UserMain userMainCheck = null;
    while (userMainCheck == null)
    {
      userMainCheck = FindObjectOfType<UserMain>();
      yield return null;
    }
    Util.FindIfNotSet(this, ref voosEngine);
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref behaviorSystem);
    mainCamera = userMain.GetCamera();

    // Removing all this until we need it, since logging is complicated
    // enough.
    // behaviorSystem.onLogEvent += OnBehaviorLogEvent;

    setupComplete = true;
  }

  // Update is called once per frame
  void Update()
  {
    if (!setupComplete) return;

    foreach (VoosActor actor in voosEngine.EnumerateActors())
    {
      if (IsActorCenterOnScreen(actor))
      {
        InWorldLogPanel panel = GetOrAddPanel(actor);
        UpdatePanelPosition(panel);
      }
      else
      {
        AttemptRemovePanel(actor);
      }
    }
  }

  private void UpdatePanelPosition(InWorldLogPanel panel)
  {
    Vector2 localPoint;
    Vector3 topOfActor = panel.actor.GetWorldRenderBoundsCenter() + Vector3.up * panel.actor.GetWorldRenderBoundsSize().y / 2f;
    Vector2 screenPoint = mainCamera.WorldToScreenPoint(topOfActor);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);
    // panel.SetPosition(localPoint);
  }

  public Vector2 GetLerpMessagePosition(InWorldLogPanel panel, float t)
  {
    Vector2 localPoint;
    Vector3 topOfActor = panel.actor.GetWorldRenderBoundsCenter() + Vector3.up * t * panel.actor.GetWorldRenderBoundsSize().y / 2f;
    Vector2 screenPoint = mainCamera.WorldToScreenPoint(topOfActor);
    RectTransformUtility.ScreenPointToLocalPointInRectangle(panel.rectTransform, screenPoint, null, out localPoint);
    return localPoint;
  }

  private void AttemptRemovePanel(VoosActor actor)
  {
    if (currentPanels.ContainsKey(actor))
    {
      currentPanels[actor].RequestDestruct();
      currentPanels.Remove(actor);
    }
  }

  private InWorldLogPanel GetOrAddPanel(VoosActor actor)
  {
    if (!currentPanels.ContainsKey(actor))
    {
      InWorldLogPanel newPanel = Instantiate(logPanelPrefab, canvasRect);
      newPanel.Setup(actor, this);
      currentPanels[actor] = newPanel;
    }

    return currentPanels[actor];
  }

  bool IsActorCenterOnScreen(VoosActor actor)
  {
    Vector3 viewportPosition = mainCamera.WorldToViewportPoint(actor.GetWorldRenderBoundsCenter());
    // Debug.Log(actor.GetDisplayName() + ":" + viewportPosition);
    return viewportPosition.x >= 0 &&
      viewportPosition.x <= 1 &&
      viewportPosition.y >= 0 &&
      viewportPosition.y <= 1 &&
      viewportPosition.z > 0;
  }

  // removing all this until we actually need it.
  // void OnBehaviorLogEvent(BehaviorSystem.BehaviorLogEvent entry, bool isError)
  // {
  //   VoosActor actor = voosEngine.GetActor(entry.actorId);
  //   if (currentPanels.ContainsKey(actor))
  //   {
  //     currentPanels[actor].HandleLogEvent(entry, isError);
  //   }
  // }
}
