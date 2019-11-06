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
using UnityEngine;

public interface ICardManagerModel
{
  IEnumerable<CardPanel.IAssignedPanel> GetAssignedPanels();

  // TODO move this out, into the same location as GetCards (wherever that
  // should be)
  IEnumerable<PanelLibraryItem.IModel> GetPanelLibrary();

  ICardAssignmentModel FindAssignedCard(string id);
  string GetId();

  PanelManager.ViewState GetViewState();
  void SetViewState(PanelManager.ViewState viewState);

  PanelNote.Data[] GetNotes();
  void SetNotes(PanelNote.Data[] notesData);

  bool IsValid();
  bool CanWrite();
  void Dispose();
}


// Additional data that only assigned cards have.
public interface ICardAssignmentModel
{
  ICardModel GetCard();

  // Leverage the old interface for modifying code, property values, etc.
  AssignedBehavior GetAssignedBehavior();

  void Unassign();

  // Will be false if no longer assigned
  bool IsValid();

  string GetId();

  PropEditor[] GetProperties();

  void SetProperties(PropEditor[] props);
}

// An *unassigned* card in the library.
public interface ICardModel
{
  string GetImagePath();
  Sprite GetImage();
  string GetTitle();
  string GetDescription();
  string GetUri();
  ICollection<string> GetCategories();
  // TODO REMOVE?
  UnassignedBehavior GetUnassignedBehaviorItem();
  string GetId();
  void SetTitle(string title);
  void SetDescription(string description);
  void SetCategories(ICollection<string> categories);
  void SetImagePath(string imagePath);
  PropEditor[] GetDefaultProperties();
  bool IsBuiltin();
  ICardModel MakeCopy();
  bool IsValid();
}

// A slot, as part of an assigned panel.
public interface IDeckModel
{
  string GetPrompt();
  string GetCardCategory();
  IEnumerable<ICardAssignmentModel> GetAssignedCards();
  ICardAssignmentModel OnAssignCard(ICardModel card, int index = -1);
  int GetIndexOf(ICardAssignmentModel assignedModel);
  ICardAssignmentModel CreateAndAssignNewCard();
  Sprite GetIcon();
  int GetNumAssignedCards();
  IEnumerable<ICardModel> GetDefaultCards();
  string GetPropertyName();  // Can be null if slot doesn't correspond to a property.
}

