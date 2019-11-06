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

public class DragMultiselect : MonoBehaviour
{
  [SerializeField] RectTransform canvasRect;
  [SerializeField] RectTransform feedbackRect;

  bool dragging = false;

  Vector2 startRectCorner = Vector2.zero;
  Vector2 endRectCorner = Vector2.zero;

  Vector2 startScreenCorner = Vector2.zero;
  Vector2 endScreenCorner = Vector2.zero;

  EditMain editMain;
  UserMain userMain;

  HashSet<VoosActor> selectedActors = new HashSet<VoosActor>();
  Dictionary<VoosActor, SelectionFeedback> selectedFeedback = new Dictionary<VoosActor, SelectionFeedback>();

  Color addColor = Color.yellow;
  Color removeColor = Color.red;

  float MAX_DIST = 1;

  public void Setup(EditMain editMain)
  {
    this.editMain = editMain;
    Util.FindIfNotSet(this, ref userMain);

    FindMaxDist();
  }

  private void FindMaxDist()
  {
    float xDim = 500;
    float zDim = 500;
    float yDim = 100;
    MAX_DIST = Mathf.Sqrt(Mathf.Pow(xDim, 2) + Mathf.Pow(yDim, 2) + Mathf.Pow(zDim, 2));
  }

  void Update()
  {
    if (!dragging) return;

    endScreenCorner = Input.mousePosition;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out endRectCorner);

    feedbackRect.offsetMin = Vector2.Min(startRectCorner, endRectCorner);
    feedbackRect.offsetMax = Vector2.Max(startRectCorner, endRectCorner);

    UpdateActorSelection();
    UpdateFeedbackColors();
  }

  public void StartDrag()
  {
    dragging = true;
    feedbackRect.gameObject.SetActive(true);

    startScreenCorner = Input.mousePosition;
    endScreenCorner = startScreenCorner;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, null, out startRectCorner);
    endRectCorner = startRectCorner;
    feedbackRect.offsetMin = startRectCorner;
    feedbackRect.offsetMax = endRectCorner;
  }

  void UpdateFeedbackColors()
  {
    bool toggling = Util.IsControlOrCommandHeld();

    foreach (KeyValuePair<VoosActor, SelectionFeedback> entry in selectedFeedback)
    {
      if (toggling)
      {
        entry.Value.SetColor(editMain.IsActorInTargetActors(entry.Key) ? removeColor : addColor);
      }
      else
      {
        entry.Value.SetColor(addColor);
      }

    }
  }

  public void StopDrag()
  {
    if (!dragging) return;

    if (Util.IsControlOrCommandHeld())
    {
      foreach (VoosActor actor in selectedActors)
      {
        editMain.ToggleTargetActor(actor);
      }
    }
    else
    {
      if (!Util.IsShiftHeld()) editMain.ClearTargetActors();
      foreach (VoosActor actor in selectedActors)
      {
        editMain.AddTargetActor(actor);
      }
    }


    dragging = false;
    selectedActors.Clear();

    foreach (KeyValuePair<VoosActor, SelectionFeedback> entry in selectedFeedback)
    {
      entry.Value.RequestDestroy();
    }
    selectedFeedback.Clear();
    feedbackRect.gameObject.SetActive(false);
  }


  void UpdateActorSelection()
  {
    Camera camera = editMain.GetCamera();

    Vector2 dragMinViewport = camera.ScreenToViewportPoint(Vector2.Min(startScreenCorner, endScreenCorner));
    Vector2 dragMaxViewport = camera.ScreenToViewportPoint(Vector2.Max(startScreenCorner, endScreenCorner));
    Rect dragViewportRect = new Rect(dragMinViewport, dragMaxViewport - dragMinViewport);


    Vector3[] dragFarWorldCorners = new Vector3[4];
    camera.CalculateFrustumCorners(dragViewportRect, MAX_DIST, Camera.MonoOrStereoscopicEye.Mono, dragFarWorldCorners);

    float dragBoxWidth = Mathf.Abs(dragFarWorldCorners[1].x - dragFarWorldCorners[2].x);
    float dragBoxHeigh = Mathf.Abs(dragFarWorldCorners[1].y - dragFarWorldCorners[0].y);

    Ray centerRay = camera.ScreenPointToRay(Vector2.Lerp(startScreenCorner, endScreenCorner, .5f));
    Vector3 dragBoxCenter = centerRay.GetPoint(MAX_DIST / 2f);
    Vector3 halfExtents = new Vector3(dragBoxWidth / 2f, dragBoxHeigh / 2f, MAX_DIST / 2f);
    Quaternion rotation = Quaternion.LookRotation(centerRay.direction, dragFarWorldCorners[1] - dragFarWorldCorners[0]);

    Collider[] colliders = Physics.OverlapBox(dragBoxCenter, halfExtents, rotation, userMain.GetLayerMask(), QueryTriggerInteraction.Collide);

    HashSet<VoosActor> actorsToAdd = new HashSet<VoosActor>();
    foreach (Collider collider in colliders)
    {
      VoosActor maybeActor = collider.GetComponentInParent<VoosActor>();
      if (maybeActor != null)
      {
        Vector3 viewportPosition = camera.WorldToViewportPoint(maybeActor.GetWorldRenderBoundsCenter());
        if (viewportPosition.z > 0 && dragViewportRect.Contains(viewportPosition))
        {
          actorsToAdd.Add(maybeActor);
        }
      }
    }

    // it feels like there should be a better way of finding these add/remove lists?
    HashSet<VoosActor> actorsToRemove = new HashSet<VoosActor>(selectedActors);
    actorsToRemove.ExceptWith(actorsToAdd);
    actorsToAdd.ExceptWith(selectedActors);

    foreach (VoosActor actor in actorsToRemove)
    {
      //editMain.RemoveTargetActor(actor);

      selectedActors.Remove(actor);
      selectedFeedback[actor].RequestDestroy();
      selectedFeedback.Remove(actor);
    }

    foreach (VoosActor actor in actorsToAdd)
    {
      //editMain.AddTargetActor(actor);
      selectedActors.Add(actor);
      SelectionFeedback feedback = Instantiate(editMain.targetFeedbackPrefab);
      feedback.scaleMod = 1.1f;
      feedback.SetActor(actor);
      selectedFeedback[actor] = feedback;
    }


  }


  /* void UpdateActorSelection()
  {


    
    //lets just do a raycheck first
    RaycastHit[] hits;
    Ray selectionRay = editMain.GetCamera().ScreenPointToRay(Input.mousePosition);
    hits = Physics.RaycastAll(selectionRay, 200f, userMain.GetLayerMask(), QueryTriggerInteraction.Collide);
    foreach (RaycastHit hit in hits)
    {
      VoosActor maybeActor = hit.collider.GetComponentInParent<VoosActor>();
      if (maybeActor != null)
      {
        if (selectedActors.Add(maybeActor))
        {
          editMain.AddTargetActor(maybeActor);
        }
      }
    }
  } */
}

