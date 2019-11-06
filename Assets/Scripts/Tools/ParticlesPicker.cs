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

public class ParticlesPicker : MonoBehaviour
{
  [SerializeField] VisualsTabUI ui;

  private Dictionary<string, ScrollingListItemUI> entries = new Dictionary<string, ScrollingListItemUI>();
  private ParticleEffectSystem particleEffectSystem;

  private System.Action<string> onSelected;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref particleEffectSystem);

    ScrollingListItemUI entry = Instantiate(ui.particlePickerItemTemplate, ui.particlePickerList.transform);
    entry.gameObject.SetActive(true);
    entry.textField.text = "<none>";
    entry.button.onClick.AddListener(() => OnParticleEffectClicked(null));
  }

  public void Open(System.Action<string> callback)
  {
    onSelected = callback;
    ui.particlePicker.SetActive(true);
    RepopulateList();
  }

  public void Close()
  {
    onSelected = null;
    ui.particlePicker.SetActive(false);
  }

  public bool IsOpen()
  {
    return ui.particlePicker.activeSelf;
  }

  public void OnParticleEffectClicked(string id)
  {
    onSelected?.Invoke(id);
    Close();
  }

  private void RepopulateList()
  {
    foreach (ScrollingListItemUI entry in entries.Values)
    {
      Destroy(entry.gameObject);
    }
    entries.Clear();

    List<ParticleEffectListing> list = particleEffectSystem.ListAll();
    foreach (ParticleEffectListing listing in list)
    {
      ScrollingListItemUI entry = Instantiate(ui.particlePickerItemTemplate, ui.particlePickerList.transform);
      entry.gameObject.SetActive(true);
      entry.textField.text = listing.name;
      // entry.Set(listing);
      entry.button.onClick.AddListener(() => OnParticleEffectClicked(listing.id));
      string name = listing.name;
      entries.Add(listing.id, entry);
    }
  }
}