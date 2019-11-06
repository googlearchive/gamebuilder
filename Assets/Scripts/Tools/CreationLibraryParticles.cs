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

public class CreationLibraryParticles : MonoBehaviour
{
  [SerializeField] CreationLibraryUI ui;
  [SerializeField] ParticleFields particleFieldsPrefab;
  [SerializeField] ScrollingListItemUI particleEntryPrefab;

  private List<ParticleEffectListing> listing;
  private string selectedParticleEffectId;
  private ParticleFields particleFields;
  private EditMain editMain;

  private ParticleEffectSystem particleEffectSystem;
  private bool isSetup = false;

  private Dictionary<string, ScrollingListItemUI> entries = new Dictionary<string, ScrollingListItemUI>();
  private ClaimKeeper claimKeeper;
  private DynamicPopup popups;

  public System.Action<string> onParticleEffectSelected;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref particleEffectSystem);
    Util.FindIfNotSet(this, ref claimKeeper);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref editMain);
    particleFields = Instantiate(particleFieldsPrefab, ui.particleEditorContainer.transform);
    particleFields.Setup();
  }

  public void SelectParticleEffect(string id)
  {
    if (selectedParticleEffectId != null && entries.ContainsKey(selectedParticleEffectId))
    {
      entries[selectedParticleEffectId].actorListItemSelected.SetActive(false);
    }
    selectedParticleEffectId = id;
    UpdateSelectedParticleUI();
    onParticleEffectSelected?.Invoke(id);
  }

  private void UpdateSelectedParticleUI()
  {
    ui.trashButton.gameObject.SetActive(selectedParticleEffectId != null);
    ui.copyButton.gameObject.SetActive(selectedParticleEffectId != null);
    if (selectedParticleEffectId != null)
    {
      ui.exportDropdownMenu.gameObject.SetActive(false);
      particleFields.Open(selectedParticleEffectId);
      entries[selectedParticleEffectId].actorListItemSelected.SetActive(true);
    }
    else
    {
      particleFields.Close();
    }
  }

  public void SetShowing(bool showing)
  {
    if (showing) Show();
    else Hide();
  }

  public void Show()
  {
    if (!isSetup)
    {
      Setup();
      isSetup = true;
    }
    listing = particleEffectSystem.ListAll();
    RepopulateList();
    ui.particleLibrary.SetActive(true);
    particleEffectSystem.onParticleEffectChanged += OnParticleEffectChanged;
    particleEffectSystem.onParticleEffectRemoved += OnParticleEffectRemoved;
    ui.createButton.onClick.AddListener(AddNewParticleEffect);
    ui.trashButton.onClick.AddListener(RemoveSelectedParticleEffect);
    ui.copyButton.onClick.AddListener(CopySelectedParticleEffect);
    ui.actionButtonsContainer.SetActive(true);
  }

  public void Hide()
  {
    particleFields.Close();
    if (particleEffectSystem != null)
    {
      particleEffectSystem.onParticleEffectChanged -= OnParticleEffectChanged;
      particleEffectSystem.onParticleEffectRemoved -= OnParticleEffectRemoved;
    }
    ui.particleLibrary.SetActive(false);
    ui.createButton.onClick.RemoveListener(AddNewParticleEffect);
    ui.actionButtonsContainer.SetActive(false);
    ui.trashButton.gameObject.SetActive(false);
    ui.trashButton.onClick.RemoveListener(RemoveSelectedParticleEffect);
    ui.copyButton.gameObject.SetActive(false);
    ui.copyButton.onClick.RemoveListener(CopySelectedParticleEffect);
  }

  public void AddNewParticleEffect()
  {
    string id = System.Guid.NewGuid().ToString();
    string name = "My particle effect";

    ParticleEffectContent content = new ParticleEffectContent();
    content.burstCount = 10;
    content.duration = 1;
    content.shapeType = ParticleEffectContent.ShapeType.Sphere;
    content.speed = 1;
    content.rotationOverLifetime.Add(new ParticleEffectContent.FloatStop(2, 0));
    content.rotationOverLifetime.Add(new ParticleEffectContent.FloatStop(2, 1));
    content.startSize = 1;
    content.endSize = 1;
    content.colorOverLifetime.Add(new ParticleEffectContent.ColorStop(Color.cyan, 0));
    content.colorOverLifetime.Add(new ParticleEffectContent.ColorStop(Color.cyan, 1));

    ParticleEffect particleEffect = new ParticleEffect(id, name, content);
    particleEffectSystem.PutParticleEffect(particleEffect);

    RepopulateList();
    SelectParticleEffect(particleEffect.id);
  }

  public void CopySelectedParticleEffect()
  {
    ParticleEffect sourceEffect = particleEffectSystem.GetParticleEffect(selectedParticleEffectId);
    string id = System.Guid.NewGuid().ToString();
    string name = sourceEffect.name + " copy";
    ParticleEffect copiedEffect = new ParticleEffect(id, name, sourceEffect.content);
    particleEffectSystem.PutParticleEffect(copiedEffect);
    RepopulateList();
    SelectParticleEffect(copiedEffect.id);
  }

  private void RemoveSelectedParticleEffect()
  {
    string claimId = ParticleEffectSystem.PFX_CLAIM_PREFIX + selectedParticleEffectId;
    string owner = claimKeeper.GetEffectiveOwnerNickname(claimId);
    if (owner == null || claimKeeper.IsMine(claimId))
    {
      particleEffectSystem.DeleteParticleEffect(selectedParticleEffectId);
    }
    else
    {
      popups.Show(
        $"Sorry, can't delete {name} right now. {owner} is editing it.",
        "Ok");
    }
  }

  private void OnParticleEffectChanged(string pfxId)
  {
    RepopulateList();
  }

  private void OnParticleEffectRemoved(string pfxId)
  {
    RepopulateList();
  }

  private void RepopulateList()
  {
    bool hasSelectedParticle = false;
    foreach (ScrollingListItemUI entry in entries.Values)
    {
      Destroy(entry.gameObject);
    }
    entries.Clear();
    List<ParticleEffectListing> list = particleEffectSystem.ListAll();
    ui.particlesList.noneText.gameObject.SetActive(list.Count == 0);
    foreach (ParticleEffectListing listing in list)
    {
      ScrollingListItemUI entry = Instantiate(particleEntryPrefab, ui.particlesList.contentRect.transform);
      entry.gameObject.SetActive(true);
      entry.textField.text = listing.name;
      // entry.Set(listing);
      entry.button.onClick.AddListener(() => SelectParticleEffect(listing.id));
      string name = listing.name;
      entries.Add(listing.id, entry);
      if (listing.id == selectedParticleEffectId) hasSelectedParticle = true;
    }
    if (!hasSelectedParticle)
    {
      SelectParticleEffect(null);
    }
    else
    {
      UpdateSelectedParticleUI();
    }
  }
}
