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

public class CardDragLayer : MonoBehaviour
{
  [SerializeField] RectTransform rectTransform;
  private CardLibrary cardLibrary;
  private CardManager cardManager;

  public void Setup(CardLibrary library, CardManager manager)
  {
    cardLibrary = library;
    cardManager = manager;

    cardLibrary.onBeginDragCard += OnBeginDragLibraryCard;
    cardLibrary.onDragCard += OnDragLibraryCard;
    cardLibrary.onEndDragCard += OnEndDragLibraryCard;
    cardLibrary.onForceReleaseDragCard += OnForceReleaseLibraryCard;
  }

  public void OnBeginDragLibraryCard(Card card)
  {
    card.StartDrag(rectTransform);
    card.SetScale(cardManager.GetCardScale());
    cardManager.UpdateUIForBeginDragCard(card);
  }

  public void OnDragLibraryCard(Card card)
  {
    card.DragUpdate(rectTransform);
    cardManager.UpdateUIForDragCard(card);
  }

  public void OnEndDragLibraryCard(Card card)
  {
    card.EndDrag();
    card.SetScale(1);
    cardManager.MaybeAcceptDraggedCard(card);
  }

  public void OnForceReleaseLibraryCard(Card card)
  {
    card.EndDrag();
    card.SetScale(1);
    cardManager.CancelDraggedCard(card);
  }
}