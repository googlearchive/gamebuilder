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
using UnityEngine.EventSystems;

// Sound editor UI.
// This is the full sound editor with the curve editors, text box for name,
// speed setting, wave shape setting, etc, etc.
//
// This interface edits ONE PARTICULAR sound.
public class SynthSoundEditor : MonoBehaviour
{
  // Volume used when user presses Play to preview sound.
  private const float PREVIEW_PLAY_VOLUME = 0.8f;

  [SerializeField] SoundEditorUI ui;

  private string sfxId;
  private SoundEffect soundEffect;

  AudioClip previewClip;
  AudioListener audioListener;
  UserMain userMain;
  SoundEffectSystem soundEffectSystem;

  // Explicit init. Must be called before anything else.
  public void Setup()
  {
    Util.FindIfNotSet(this, ref userMain);
    Util.FindIfNotSet(this, ref soundEffectSystem);

    ui.playButton.onClick.AddListener(OnPlayClicked);
    ui.pitchEditor.Setup();
    ui.pitchEditor.onFinishDrag += OnPitchChanged;
    ui.volumeEditor.Setup();
    ui.volumeEditor.onFinishDrag += OnVolumeChanged;
    ui.playbackSpeedSlider.onValueChanged += OnPlaybackSpeedChanged;
    ui.nameField.onEndEdit.AddListener(OnNameChanged);
    ui.spatializedToggle.onValueChanged.AddListener(OnSpatializedChanged);
    ui.sineToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.triangleToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.squareToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.noiseToggle.onValueChanged.AddListener(OnWaveShapeSelected);

    gameObject.SetActive(false);
    ui.closeButton.onClick.AddListener(Close);
  }

  // Shows this sound editor.
  // soundEffect: the sound effect that is to be edited.
  // callback: callback to call when the user does something interesting.
  public void Open(string sfxId)
  {
    this.sfxId = sfxId;
    UpdateFromModel();
    gameObject.SetActive(true);
  }

  void OnSoundEffectChanged(string sfxId)
  {
    if (sfxId == this.sfxId)
    {
      UpdateFromModel();
    }
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  void UpdateFromModel()
  {
    soundEffect = soundEffectSystem.GetSoundEffect(sfxId);

    // Be forgiving with data errors because this data can come from serialized
    // data, over the network, etc, so just fail instead of crashing if there
    // is something wrong:
    if (soundEffect.content.effectType != SoundEffectType.Synthesized)
    {
      Debug.LogWarning("SynthSoundEditor can only edit synth based sounds.");
      Close();
      return;
    }
    if (soundEffect.content.synthParams == null)
    {
      Debug.LogWarning("SynthSoundEditor: missing synth params. Can't edit.");
      Close();
      return;
    }

    UpdateWidgetsFromModel();

    ClipSynthesizer synth = new ClipSynthesizer(soundEffect.content.synthParams);
    previewClip = synth.GetAudioClip();
  }

  private void UpdateWidgetsFromModel()
  {
    SynthParams synthParams = soundEffect.content.synthParams;
    ui.nameField.text = soundEffect.name;

    // Remove listeners before setting - we don't want to receive an event from programmatic set
    ui.sineToggle.onValueChanged.RemoveListener(OnWaveShapeSelected);
    ui.triangleToggle.onValueChanged.RemoveListener(OnWaveShapeSelected);
    ui.noiseToggle.onValueChanged.RemoveListener(OnWaveShapeSelected);
    ui.squareToggle.onValueChanged.RemoveListener(OnWaveShapeSelected);

    ui.sineToggle.isOn = synthParams.waveShape == SynthWaveShape.SINE;
    ui.triangleToggle.isOn = synthParams.waveShape == SynthWaveShape.TRIANGLE;
    ui.noiseToggle.isOn = synthParams.waveShape == SynthWaveShape.NOISE;
    ui.squareToggle.isOn = synthParams.waveShape == SynthWaveShape.SQUARE;

    ui.sineToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.triangleToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.squareToggle.onValueChanged.AddListener(OnWaveShapeSelected);
    ui.noiseToggle.onValueChanged.AddListener(OnWaveShapeSelected);

    ui.pitchEditor.SetSampleValues(synthParams.pitch);
    ui.volumeEditor.SetSampleValues(synthParams.volume);
    ui.playbackSpeedSlider.SetValue(synthParams.speed);

    ui.spatializedToggle.onValueChanged.RemoveListener(OnSpatializedChanged);
    ui.spatializedToggle.isOn = soundEffect.content.spatialized;
    ui.spatializedToggle.onValueChanged.AddListener(OnSpatializedChanged);
  }

  void OnNameChanged(string value)
  {
    soundEffect.name = value;
    soundEffectSystem.PutSoundEffect(soundEffect);
  }

  void OnPitchChanged()
  {
    soundEffect.content.synthParams.pitch = ui.pitchEditor.GetSampleValues();
    PutAndPreviewSoundEffect();
  }

  void OnVolumeChanged()
  {
    soundEffect.content.synthParams.volume = ui.volumeEditor.GetSampleValues();
    PutAndPreviewSoundEffect();
  }

  void OnPlaybackSpeedChanged(float value)
  {
    soundEffect.content.synthParams.speed = Mathf.RoundToInt(value);
    PutAndPreviewSoundEffect();
  }

  void OnSpatializedChanged(bool value)
  {
    soundEffect.content.spatialized = value;
    PutAndPreviewSoundEffect();
  }

  void OnWaveShapeSelected(bool value)
  {
    // Ignore if this is a negative event (eg. not the selected toggle).
    // Otherwise, we will handle duplicate events.
    if (!value) return;
    SynthParams synthParams = soundEffect.content.synthParams;
    if (ui.sineToggle.isOn)
    {
      synthParams.waveShape = SynthWaveShape.SINE;
    }
    else if (ui.squareToggle.isOn)
    {
      synthParams.waveShape = SynthWaveShape.SQUARE;
    }
    else if (ui.triangleToggle.isOn)
    {
      synthParams.waveShape = SynthWaveShape.TRIANGLE;
    }
    else if (ui.noiseToggle.isOn)
    {
      synthParams.waveShape = SynthWaveShape.NOISE;
    }
    PutAndPreviewSoundEffect();
  }

  void PutAndPreviewSoundEffect()
  {
    soundEffectSystem.PutSoundEffect(soundEffect);
    PreviewSound();
  }

  void OnPlayClicked()
  {
    PreviewSound();
  }

  void PreviewSound()
  {
    audioListener = audioListener ?? GameObject.FindObjectOfType<AudioListener>();
    if (audioListener != null)
    {
      AudioSource.PlayClipAtPoint(previewClip, audioListener.transform.position, PREVIEW_PLAY_VOLUME * userMain.playerOptions.sfxVolume);
    }
    else
    {
      Debug.LogError("No AudioListener on scene. Can't preview sound.");
    }
  }
}
