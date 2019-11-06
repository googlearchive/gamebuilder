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

public class VisualsTabController : AbstractTabController
{
  [SerializeField] RenderableLibrary creationLibrary;
  [SerializeField] VisualsTabUI visualsTabUI;
  [SerializeField] ParticlesPicker particlesPicker;
  [SerializeField] SoundsPicker soundsPicker;

  private VoosActor actor;
  private UndoStack undoStack;
  private SoundEffectSystem sfxSystem;
  private ParticleEffectSystem pfxSystem;

  public override void Setup()
  {
    Util.FindIfNotSet(this, ref undoStack);
    Util.FindIfNotSet(this, ref pfxSystem);
    Util.FindIfNotSet(this, ref sfxSystem);
    visualsTabUI.changeAssetButton.onClick.AddListener(() =>
    {
      creationLibrary.gameObject.SetActive(!creationLibrary.gameObject.activeInHierarchy);
    });
    creationLibrary.Setup();
    creationLibrary.AddResultClickedListener(OnCreationLibraryResult);

    visualsTabUI.colorField.Setup();
    visualsTabUI.colorField.DisableAlpha();

    visualsTabUI.colorField.OnColorChange = OnColorWheelChanged;
    visualsTabUI.emitLightToggle.onValueChanged.AddListener(OnEmitLightToggleChanged);

    soundsPicker.Setup();
    particlesPicker.Setup();

    visualsTabUI.changeSoundEffect.onClick.AddListener(() =>
    {

      if (!soundsPicker.IsOpen())
      {
        particlesPicker.Close();
        soundsPicker.Open(OnSoundPicked);
      }
      else soundsPicker.Close();
    });

    visualsTabUI.changeParticleEffect.onClick.AddListener(() =>
    {
      if (!particlesPicker.IsOpen())
      {
        soundsPicker.Close();
        particlesPicker.Open(OnParticlePicked);
      }
      else particlesPicker.Close();
    });
  }

  void OnSoundPicked(string sfxId)
  {
    string lastSfxId = actor.GetSfxId();
    undoStack.PushUndoForActor(
      actor,
      $"Set sound effect for {actor.GetDisplayName()}",
      actor =>
      {
        actor.SetSfxId(sfxId);
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        actor.SetSfxId(lastSfxId);
        actor.ApplyPropertiesToClones();
      });
  }

  void OnParticlePicked(string pfxId)
  {
    string lastPfxId = actor.GetPfxId();
    undoStack.PushUndoForActor(
      actor,
      $"Set particle effect for {actor.GetDisplayName()}",
      actor =>
      {
        actor.SetPfxId(pfxId);
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        actor.SetPfxId(lastPfxId);
        actor.ApplyPropertiesToClones();
      });
  }


  void OnCreationLibraryResult(ActorableSearchResult _result)
  {
    string prevRenderableUri = actor.GetRenderableUri();
    string newRenderableUri = _result.renderableReference.uri;
    undoStack.PushUndoForActor(
      actor,
      $"Set asset for {actor.GetDisplayName()}",
      actor =>
      {
        actor.SetRenderableUri(newRenderableUri);
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        actor.SetRenderableUri(prevRenderableUri);
        actor.ApplyPropertiesToClones();
      });
    creationLibrary.gameObject.SetActive(false);
  }

  void OnEmitLightToggleChanged(bool on)
  {
    float range = actor.GetLightSettings().range;
    undoStack.PushUndoForActor(
      actor,
      $"Set asset for {actor.GetDisplayName()}",
      actor =>
      {
        VoosActor.LightSettings settings = actor.GetLightSettings();
        // In future, we may want user to be able to adjust range
        settings.range = range > 0 ? 0 : VoosActor.LightSettings.DEFAULT_RANGE;
        actor.SetLightSettingsJson(JsonUtility.ToJson(settings));
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        VoosActor.LightSettings settings = actor.GetLightSettings();
        settings.range = range;
        actor.SetLightSettingsJson(JsonUtility.ToJson(settings));
        actor.ApplyPropertiesToClones();
      });
  }

  void OnColorWheelChanged(Color color)
  {
    // Set tint, but preserve alpha.
    color.a = actor.GetTint().a;
    Color prevColor = actor.GetTint();
    undoStack.PushUndoForActor(
      actor,
      $"Set tint for {actor.GetDisplayName()}",
      actor =>
      {
        actor.SetTint(color);
        actor.ApplyPropertiesToClones();
      },
      actor =>
      {
        actor.SetTint(prevColor);
        actor.ApplyPropertiesToClones();
      });
  }

  public override void Open(VoosActor actor, Dictionary<string, object> props)
  {
    base.Open(actor, props);
    this.actor = actor;
    UpdateFields();
  }

  protected override void Update()
  {
    if (actor != null)
    {
      UpdateFields();
    }
    base.Update();
  }

  private void UpdateFields()
  {
    if (!visualsTabUI.colorField.IsBeingEdited())
    {
      visualsTabUI.colorField.SetColor(actor.GetTint());
    }
    visualsTabUI.emitLightToggle.onValueChanged.RemoveListener(OnEmitLightToggleChanged);
    visualsTabUI.emitLightToggle.isOn = actor.GetLightSettings().range > 0;
    visualsTabUI.emitLightToggle.onValueChanged.AddListener(OnEmitLightToggleChanged);
    visualsTabUI.particleEffectName.text = actor.GetPfxId() != null ?
      pfxSystem.GetParticleEffect(actor.GetPfxId())?.name : null;
    visualsTabUI.soundEffectName.text = actor.GetSfxId() != null ?
      sfxSystem.GetSoundEffect(actor.GetSfxId())?.name : null;
  }

  public override void Close()
  {
    base.Close();
    creationLibrary.gameObject.SetActive(false);
    soundsPicker.Close();
    particlesPicker.Close();
  }
}
