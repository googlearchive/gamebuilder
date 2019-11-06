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

// Panel that lets the user "edit" an imported sound effect.
// Actually they can just change the name and listen to it, that's
// how far as "editing' goes but hey, it's better than nothing.
public class ImportedSoundEffectEditor : MonoBehaviour
{
  [SerializeField] ImportedSoundEditorUI ui;
  SoundEffectSystem soundEffectSystem;

  private string sfxId;
  private SoundEffect soundEffect;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref soundEffectSystem);
    gameObject.SetActive(false);
    ui.closeButton.onClick.AddListener(OnCloseClicked);
    ui.playButton.onClick.AddListener(OnPlayClicked);
    ui.nameField.onEndEdit.AddListener(OnNameChanged);
    ui.spatializedToggle.onValueChanged.AddListener(OnSpatializedChanged);
  }

  public void Open(string sfxId)
  {
    this.sfxId = sfxId;

    UpdateSound();
    soundEffectSystem.onSoundEffectChanged += OnSoundEffectChanged;

    gameObject.SetActive(true);
  }

  void OnSoundEffectChanged(string sfxId)
  {
    if (this.sfxId == sfxId) UpdateSound();
  }

  void UpdateSound()
  {
    this.soundEffect = soundEffectSystem.GetSoundEffect(sfxId);
    ui.nameField.text = soundEffect.name;
    ui.spatializedToggle.onValueChanged.RemoveListener(OnSpatializedChanged);
    ui.spatializedToggle.isOn = soundEffect.content.spatialized;
    ui.spatializedToggle.onValueChanged.AddListener(OnSpatializedChanged);
  }

  void OnNameChanged(string value)
  {
    soundEffect.name = value;
    soundEffectSystem.PutSoundEffect(soundEffect);
  }

  void OnSpatializedChanged(bool value)
  {
    soundEffect.content.spatialized = value;
    soundEffectSystem.PutSoundEffect(soundEffect);
  }

  public void Close()
  {
    soundEffectSystem.onSoundEffectChanged -= OnSoundEffectChanged;
    gameObject.SetActive(false);
  }

  void OnCloseClicked()
  {
    Close();
  }

  void OnPlayClicked()
  {
    AudioListener audioListener = GameObject.FindObjectOfType<AudioListener>();
    if (audioListener != null)
    {
      soundEffectSystem.PlaySoundEffectLocal(soundEffect, null, audioListener.transform.position, 0.8f);
    }
  }
}
