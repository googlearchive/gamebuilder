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
using System.IO;
using UnityEngine;

public class CreationLibrarySounds : MonoBehaviour
{
  [SerializeField] CreationLibraryUI ui;
  [SerializeField] ScrollingListItemUI soundItemTemplate;
  // [SerializeField] Button createNewButton;
  // [SerializeField] Button importButton;
  [SerializeField] SynthSoundEditor synthSoundEditorPrefab;
  [SerializeField] ImportedSoundEffectEditor importedSoundEditorPrefab;
  // [SerializeField] SoundEffectPleaseWaitWindow pleaseWaitWindow;
  // [SerializeField] SoundEffectPreImportDialog preImportDialog;
  // [SerializeField] GameObject modalScrim;

  DynamicPopup popups;
  SoundEffectSystem soundEffectSystem;
  WorkshopAssetSource workshopAssetSource;
  List<ScrollingListItemUI> listItems = new List<ScrollingListItemUI>();
  Dictionary<string, Util.Tuple<ProgressItemUI, WorkshopAssetSource.GetUploadProgress>> importingListItems =
  new Dictionary<string, Util.Tuple<ProgressItemUI, WorkshopAssetSource.GetUploadProgress>>();

  // WorkshopAssetSource workshopAssetSource;
  private SynthSoundEditor synthSoundEditor;
  private ImportedSoundEffectEditor importedSoundEditor;
  private ScrollingListItemUI selectedListItem;
  public System.Action<string> onSoundSelected;

  public enum UiResult
  {
    // User wants to save sound.
    SAVE,
    // User wants to save the sound as a copy.
    SAVE_COPY,
    // User wants to delete the sound.
    DELETE,
    // Editor aborted (lost claim to sfx, for instance)
    ABORTED
  }

  public void Setup()
  {
    Util.FindIfNotSet(this, ref soundEffectSystem);
    Util.FindIfNotSet(this, ref popups);
    Util.FindIfNotSet(this, ref workshopAssetSource);
    // pleaseWaitWindow.Setup();
    // importedSoundEditor.Setup();
    // preImportDialog.Setup();
    // importButton.onClick.AddListener(OnImportButtonClicked);
    soundItemTemplate.gameObject.SetActive(false);
    soundEffectSystem.onSoundEffectChanged += (id) => Refresh();
    soundEffectSystem.onSoundEffectRemoved += (id) => Refresh();
    synthSoundEditor = Instantiate(synthSoundEditorPrefab, ui.soundEditorContainer.transform);
    synthSoundEditor.Setup();
    importedSoundEditor = Instantiate(importedSoundEditorPrefab, ui.soundEditorContainer.transform);
    importedSoundEditor.Setup();
    ui.preImportSoundDialogUI.Setup();
  }

  public void Open()
  {
    Refresh();
    ui.soundLibrary.SetActive(true);
    ui.actionButtonsContainer.SetActive(true);
    ui.createButton.onClick.AddListener(OnCreateNewClicked);
    ui.importButton.onClick.AddListener(OnImportButtonClicked);
    ui.importButton.gameObject.SetActive(true);
    ui.copyButton.onClick.AddListener(CopySelectedSoundEffect);
    ui.trashButton.onClick.AddListener(DeleteSelectedSoundEffect);
  }

  public void Close()
  {
    CloseDialogs();

    ui.soundLibrary.SetActive(false);
    ui.actionButtonsContainer.SetActive(false);
    ui.copyButton.onClick.RemoveListener(CopySelectedSoundEffect);
    ui.trashButton.onClick.RemoveListener(DeleteSelectedSoundEffect);
    ui.createButton.onClick.RemoveListener(OnCreateNewClicked);
    ui.importButton.onClick.RemoveListener(OnImportButtonClicked);
    ui.importButton.gameObject.SetActive(false);
  }

  void OnDisable()
  {
    CloseDialogs();
  }

  private void CloseDialogs()
  {
    synthSoundEditor.Close();
    importedSoundEditor.Close();
    ui.preImportSoundDialogUI.Close();
  }

  public bool IsOpen()
  {
    return gameObject.activeSelf;
  }

  public void Refresh()
  {
    List<SoundEffectListing> soundEffectListings = soundEffectSystem.ListAll();
    string selectedId = selectedListItem?.name;
    bool hasSelectedItem = false;
    ui.soundsList.noneText.gameObject.SetActive(soundEffectListings.Count == 0);

    // Reuse as much of the list as possible to cut down on creating/destroying new objects:
    for (int i = 0; i < Mathf.Max(soundEffectListings.Count, listItems.Count); i++)
    {
      if (i < listItems.Count && i < soundEffectListings.Count)
      {
        // Reuse this list item.
        UpdateSoundListItem(listItems[i], soundEffectListings[i]);
      }
      else if (i < soundEffectListings.Count)
      {
        // Create a new list item.
        UpdateSoundListItem(CreateNewSoundListItem(), soundEffectListings[i]);
      }
      else
      {
        // Delete this list item.
        GameObject.Destroy(listItems[i].gameObject);
        listItems.RemoveAt(i);
      }
      if (i < soundEffectListings.Count && soundEffectListings[i].id == selectedId)
      {
        SetSelectedSound(listItems[i]);
        hasSelectedItem = true;
      }
    }
    if (!hasSelectedItem) SetSelectedSound(null);
  }

  private ScrollingListItemUI CreateNewSoundListItem()
  {
    ui.soundsList.noneText.gameObject.SetActive(false);
    ScrollingListItemUI listItem = Instantiate(soundItemTemplate, ui.soundsList.contentRect.transform);
    listItem.button.onClick.AddListener(() => SetSelectedSound(listItem));
    listItem.gameObject.SetActive(true);
    listItems.Add(listItem);
    return listItem;
  }

  private void UpdateSoundListItem(ScrollingListItemUI listItem, SoundEffectListing listing)
  {
    listItem.name = listing.id;
    listItem.textField.text = listing.name;
  }

  void OnCreateNewClicked()
  {
    // TODO: in the future, we will want to ask what kind of sound effect the user
    // wants to create. For now the only type is synthesized sound effects, so
    // that's what we create:
    SoundEffect effect = new SoundEffect(System.Guid.NewGuid().ToString(),
        GetRandomSoundName(), SoundEffectContent.NewWithSynthParams(SynthParams.GetDefaults()));
    soundEffectSystem.PutSoundEffect(effect);
    foreach (ScrollingListItemUI item in listItems)
    {
      if (item.name == effect.id)
      {
        SetSelectedSound(item);
        return;
      }
    }
  }

  void SetSelectedSound(ScrollingListItemUI item)
  {
    CloseDialogs();

    if (item != null)
    {
      string id = item.name;
      SoundEffect effect = soundEffectSystem.GetSoundEffect(id);
      if (effect == null)
      {
        // Someone remotely deleted this sound...
        Refresh();
        return;
      }
      else
      {
        ui.exportDropdownMenu.gameObject.SetActive(false);
        ui.copyButton.gameObject.SetActive(true);
        ui.trashButton.gameObject.SetActive(true);
        if (effect.content.effectType == SoundEffectType.Synthesized)
        {
          // Launch the synthesized sound editor.
          synthSoundEditor.Open(id);
        }
        else if (effect.content.effectType == SoundEffectType.SteamWorkshop)
        {
          // Launch the imported sound editor.
          importedSoundEditor.Open(id);
        }
        else
        {
          // TODO: launch editor appropriate for this type of sound.
        }
      }
    }
    else
    {
      ui.copyButton.gameObject.SetActive(false);
      ui.trashButton.gameObject.SetActive(false);
    }

    if (selectedListItem != null) selectedListItem.actorListItemSelected.SetActive(false);
    selectedListItem = item;
    if (selectedListItem != null) selectedListItem.actorListItemSelected.SetActive(true);

    onSoundSelected?.Invoke(selectedListItem?.name);
  }

  string GetRandomSoundName()
  {
    return "Sound " + UnityEngine.Random.Range(10000, 99999);
  }

  void CopySelectedSoundEffect()
  {
    SoundEffect sourceEffect = soundEffectSystem.GetSoundEffect(selectedListItem.name);
    SoundEffect copyEffect = new SoundEffect(System.Guid.NewGuid().ToString(),
        sourceEffect.name + " copy", sourceEffect.content);
    soundEffectSystem.PutSoundEffect(copyEffect);
    foreach (ScrollingListItemUI item in listItems)
    {
      if (item.name == copyEffect.id)
      {
        SetSelectedSound(item);
        return;
      }
    }
  }

  void DeleteSelectedSoundEffect()
  {
    soundEffectSystem.DeleteSoundEffect(selectedListItem.name);
  }

  void OnImportButtonClicked()
  {
#if USE_FILEBROWSER
    var wavFilter = new Crosstales.FB.ExtensionFilter("WAV files", "wav");
    var oggFilter = new Crosstales.FB.ExtensionFilter("OGG files", "ogg");
    string[] paths = Crosstales.FB.FileBrowser.OpenFiles("Import sound", "", wavFilter, oggFilter);
    if (paths != null) OnImportFileSelected(paths);
#else
    popups.ShowTextInput("Enter the full path to a WAV or OGG file (such as C:\\my\\sounds\\foo.wav):", "", path =>
    {
      if (!path.IsNullOrEmpty() && File.Exists(path))
      {
        OnImportFileSelected(new string[] { path });
      }
    });
#endif
  }

  void OnImportFileSelected(string[] selections)
  {
    if (selections == null || selections.Length == 0)
    {
      // Canceled.
      return;
    }
    string fullPath = selections[0];
    string baseName = Path.GetFileNameWithoutExtension(fullPath);
    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
    if (ext != ".ogg" && ext != ".wav")
    {
      // Should not happen.
      Debug.LogError("Invalid extension for audio file: " + ext);
      return;
    }

    CloseDialogs();
#if USE_STEAMWORKS
    ui.preImportSoundDialogUI.Open(baseName, (proceed, soundName) => OnPreImportClosed(proceed, soundName, fullPath));
#else
    OnPreImportClosed(true, baseName, fullPath);
#endif
  }

  void OnPreImportClosed(bool proceed, string soundName, string fullPath)
  {
    if (!proceed)
    {
      return;
    }
    soundName = (soundName == null || soundName.Trim().Length == 0) ? "Untitled Sound" : soundName;
    string ext = Path.GetExtension(fullPath).ToLowerInvariant();
    string tempDir = Util.CreateTempDirectory();
    File.Copy(fullPath, Path.Combine(tempDir, "audio" + ext));

    string id = System.Guid.NewGuid().ToString();
    ui.soundsList.noneText.gameObject.SetActive(false);
    ProgressItemUI listItem = Instantiate(ui.progressSoundItemPrefab, ui.soundsList.contentRect.transform);
    listItem.name = id;
    listItem.label.text = soundName;
    listItem.gameObject.SetActive(true);
    importingListItems[id] = new Util.Tuple<ProgressItemUI, WorkshopAssetSource.GetUploadProgress>(
      listItem,
      () => { return 0; });
#if USE_STEAMWORKS
    workshopAssetSource.Put(tempDir, soundName, soundName, GameBuilder.SteamUtil.GameBuilderTags.WAV, null, null,
      result => OnSoundUploadComplete(soundName, result, id), (getProgress) =>
      {
        importingListItems[id].second = getProgress;
      });
#else
    workshopAssetSource.Put(tempDir, soundName, soundName, GameBuilder.SteamUtil.GameBuilderTags.WAV,
      result => OnSoundUploadComplete(soundName, result, id), (getProgress) =>
      {
        importingListItems[id].second = getProgress;
      });
#endif
  }

  void OnSoundUploadComplete(string name, Util.Maybe<ulong> result, string id)
  {
    Util.Tuple<ProgressItemUI, WorkshopAssetSource.GetUploadProgress> tuple = importingListItems[id];
    importingListItems.Remove(id);
    Destroy(tuple.first.gameObject);

    if (result.IsEmpty())
    {
      popups.Show("Error uploading sound to Steam Workshop:\n" + result.GetErrorMessage(), "OK", () => { });
      return;
    }
    SoundEffect soundEffect = new SoundEffect(
      id, name, SoundEffectContent.NewWithSteamWorkshopId(result.Value));
    soundEffectSystem.PutSoundEffect(soundEffect);
    // TODO: maybe immediately open the just-created sound effect?
  }

  void Update()
  {
    foreach (var entry in importingListItems)
    {
      float progress = entry.Value.second();
      entry.Value.first.SetProgress(progress);
    }
  }
}
