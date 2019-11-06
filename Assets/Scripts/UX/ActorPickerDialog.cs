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
using UnityEngine.UI;
using System.Linq;

public class ActorPickerDialog : MonoBehaviour
{
  private const string PREFAB_PATH = "ActorPicker";

  [SerializeField] HierarchyActorList onStageList;
  [SerializeField] HierarchyActorList offStageList;
  [SerializeField] GameObject offStageDisabledPanel;
  [SerializeField] GameObject importBar;
  [SerializeField] TMPro.TextMeshProUGUI pickerPromptField;
  [SerializeField] CreationLibrarySidebar creationLibrarySidebar;
  [SerializeField] Button closeButton;
  [SerializeField] Button importButton;
  [SerializeField] TMPro.TMP_InputField searchInput;
  [SerializeField] Button clearSearchButton;

  // Called when actor picker is closed.
  // success: True if user picked an actor, false if canceled.
  // pickedActorName: the picked actor's name (null for none).
  public delegate void OnActorPickerResult(bool success, string pickedActorName);

  VoosEngine engine;
  OnActorPickerResult callback;
  SidebarManager sidebarManager;
  AssetSearch assetSearch;

  private bool allowOffstageActors;

  public static ActorPickerDialog Launch(Canvas parentCanvas, string pickerPrompt, bool allowOffstageActors, OnActorPickerResult callback)
  {
    GameObject obj = GameObject.Instantiate(Resources.Load<GameObject>(PREFAB_PATH));
    if (parentCanvas != null) obj.transform.SetParent(parentCanvas.transform, false);
    ActorPickerDialog dialog = obj.GetComponent<ActorPickerDialog>();
    dialog.Setup(pickerPrompt, allowOffstageActors, callback);
    return dialog;
  }

  private void Setup(string pickerPrompt, bool allowOffstageActors, OnActorPickerResult callback)
  {
    this.callback = callback;
    this.allowOffstageActors = allowOffstageActors;
    creationLibrarySidebar.gameObject.SetActive(false);
    if (!string.IsNullOrEmpty(pickerPrompt))
    {
      pickerPromptField.text = pickerPrompt;
    }
    Util.FindIfNotSet(this, ref engine);
    Util.FindIfNotSet(this, ref sidebarManager);
    Util.FindIfNotSet(this, ref assetSearch);

    RefreshActorList();
    searchInput.onValueChanged.AddListener((s) =>
    {
      RefreshActorList();
      clearSearchButton.gameObject.SetActive(!s.IsNullOrEmpty());
    });
    clearSearchButton.onClick.AddListener(() =>
    {
      searchInput.text = "";
    });

    offStageDisabledPanel.SetActive(!allowOffstageActors);
    importBar.SetActive(allowOffstageActors);
    closeButton.onClick.AddListener(OnCloseButtonClicked);
    importButton.onClick.AddListener(OnImportButtonClicked);
  }

  private void RefreshActorList()
  {
    List<VoosActor> onList = new List<VoosActor>(engine.EnumerateActors().Where(actor => ShouldActorBeListed(false, actor)));
    // Include a "None" option so the user can choose to fill in a field with "no actor".
    // A null in the list means None.
    onList.Insert(0, null);
    onStageList.SetActors(onList);
    onStageList.AddClickListener(OnActorClicked);

    if (allowOffstageActors)
    {
      offStageList.SetActors(engine.EnumerateActors().Where(actor => ShouldActorBeListed(true, actor)));
      offStageList.AddClickListener(OnActorClicked);
    }
  }

  public void Close()
  {
    CloseAndReturn(false, null);
  }

  private bool ShouldActorBeListed(bool isOffstageList, VoosActor actor)
  {
    return actor.GetName() != "__GameRules__" &&
      (actor.GetIsOffstageEffective() == isOffstageList) &&
      !actor.GetWasClonedByScript() &&
      (actor.GetDisplayName().ToLower().Contains(searchInput.text.ToLower()));
  }

  private void OnCloseButtonClicked()
  {
    CloseAndReturn(false, null);
  }

  private void OnActorClicked(HierarchyActorEntry entry, HierarchyActorEntry.ActionType actionType)
  {
    // TODO: we should not need to have actionType passed in
    CloseAndReturn(true, entry.GetActorName());
  }

  private void OnImportButtonClicked()
  {
    creationLibrarySidebar.gameObject.SetActive(true);
    creationLibrarySidebar.Setup(sidebarManager);
    creationLibrarySidebar.SetToPicker(OnPickedFromCreationLibrary);
  }

  private void CloseAndReturn(bool success, string result)
  {
    GameObject.Destroy(gameObject);
    if (callback != null)
    {
      callback(success, result);
    }
  }

  private void OnPickedFromCreationLibrary(ActorableSearchResult searchResult)
  {
    VoosActor actor = assetSearch.RequestActor(searchResult, Vector3.zero, Quaternion.identity, Vector3.one);
    actor.SetPreferOffstage(true);
    CloseAndReturn(true, actor.GetName());
  }
}
