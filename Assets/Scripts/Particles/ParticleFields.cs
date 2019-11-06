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

public class ParticleFields : MonoBehaviour
{
  [SerializeField] ParticlesEditorUI ui;
  [SerializeField] RenderTexture particlesPreviewTexture;
  [SerializeField] ParticlesPreview particlesPreview;

  private ParticleEffectSystem system;
  private string pfxId;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref system);
    ui.nameField.onEndEdit.AddListener((name) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      ParticleEffect newParticleEffect = particleEffect;
      newParticleEffect.name = name;
      system.PutParticleEffect(particleEffect);
    });
    ui.speedSlider.onValueChanged += (speed) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.speed = speed;
      system.PutParticleEffect(particleEffect);
    };
    ui.lifetimeSlider.onValueChanged += (duration) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.duration = duration;
      system.PutParticleEffect(particleEffect);
    };
    ui.densitySlider.onValueChanged += (burstCount) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.burstCount = Mathf.RoundToInt(burstCount);
      system.PutParticleEffect(particleEffect);
    };
    ui.shapeSizeSlider.onValueChanged += (shapeSize) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.shapeSize = shapeSize;
      system.PutParticleEffect(particleEffect);
    };
    ui.startSizeSlider.onValueChanged += (startSize) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.startSize = startSize;
      system.PutParticleEffect(particleEffect);
    };
    ui.endSizeSlider.onValueChanged += (endSize) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.endSize = endSize;
      system.PutParticleEffect(particleEffect);
    };
    ui.gravitySlider.onValueChanged += (gravity) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particleEffect.content.gravityModifier = gravity;
      system.PutParticleEffect(particleEffect);
    };
    ui.colorGradientField.Setup();
    ui.colorGradientField.addStopRequested += OnAddColorStopRequested;
    ui.colorGradientField.changeStopValueRequested += OnChangeColorStopValueRequested;
    ui.colorGradientField.changeStopPositionRequested += OnChangeColorStopPositionRequested;
    ui.colorGradientField.removeStopRequested += OnRemoveColorStopRequested;
    ui.colorField.Setup();

    ui.closeButton.onClick.AddListener(Close);
    ui.previewRawImage.texture = particlesPreviewTexture;

    ui.burstPreviewToggle.onValueChanged.AddListener((v) =>
    {
      ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
      particlesPreview.Open(particleEffect, !v);
    });

    AddEmissionToggleListeners();

    particlesPreview.Setup();
  }

  private void AddEmissionToggleListeners()
  {
    ui.coneEmissionToggle.toggle.onValueChanged.AddListener(
      (v) => OnEmissionShapeSelected(ParticleEffectContent.ShapeType.Cone));
    ui.sphereEmissionToggle.toggle.onValueChanged.AddListener(
      (v) => OnEmissionShapeSelected(ParticleEffectContent.ShapeType.Sphere));
    ui.circleEmissionToggle.toggle.onValueChanged.AddListener(
      (v) => OnEmissionShapeSelected(ParticleEffectContent.ShapeType.Circle));
    ui.rectangleEmissionToggle.toggle.onValueChanged.AddListener(
      (v) => OnEmissionShapeSelected(ParticleEffectContent.ShapeType.Rectangle));
  }

  private void RemoveEmissionToggleListeners()
  {
    ui.coneEmissionToggle.toggle.onValueChanged.RemoveAllListeners();
    ui.sphereEmissionToggle.toggle.onValueChanged.RemoveAllListeners();
    ui.circleEmissionToggle.toggle.onValueChanged.RemoveAllListeners();
    ui.rectangleEmissionToggle.toggle.onValueChanged.RemoveAllListeners();
  }

  private void OnEmissionShapeSelected(ParticleEffectContent.ShapeType shapeType)
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    particleEffect.content.shapeType = shapeType;
    system.PutParticleEffect(particleEffect);
  }

  public void OnEnable()
  {
    system.onParticleEffectChanged += OnParticleEffectChanged;
    system.onParticleEffectRemoved += OnParticleEffectRemoved;
    if (system.GetParticleEffect(pfxId) == null)
    {
      Close();
    }
    else
    {
      // HACK: Some stuff relies on UI being sized correctly but sizes may be 0 at start
      StartCoroutine(DelayedRefresh());
    }
  }

  public void OnDisable()
  {
    system.onParticleEffectChanged -= OnParticleEffectChanged;
    system.onParticleEffectRemoved -= OnParticleEffectRemoved;
  }

  public void Open(string pfxId)
  {
    Close();
    this.pfxId = pfxId;
    gameObject.SetActive(true);
    Refresh();
  }

  private IEnumerator DelayedRefresh()
  {
    yield return 0.1f;
    Refresh();
  }

  private void Refresh()
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    ui.nameField.text = particleEffect.name;
    ui.lifetimeSlider.SetValue(particleEffect.content.duration);
    ui.densitySlider.SetValue(particleEffect.content.burstCount);
    ui.shapeSizeSlider.SetValue(particleEffect.content.shapeSize);
    // rotationField.text = effect.content.rotationOverLifetime[0].value.ToString();
    ui.startSizeSlider.SetValue(particleEffect.content.startSize);
    ui.endSizeSlider.SetValue(particleEffect.content.endSize);
    ui.gravitySlider.SetValue(particleEffect.content.gravityModifier);
    ui.speedSlider.SetValue(particleEffect.content.speed);
    ui.colorGradientField.SetData(new ColorModel(particleEffect.content.colorOverLifetime));

    RemoveEmissionToggleListeners();
    ui.coneEmissionToggle.toggle.isOn = false;
    ui.sphereEmissionToggle.toggle.isOn = false;
    ui.circleEmissionToggle.toggle.isOn = false;
    ui.rectangleEmissionToggle.toggle.isOn = false;

    switch (particleEffect.content.shapeType)
    {
      case ParticleEffectContent.ShapeType.Cone:
        ui.coneEmissionToggle.toggle.isOn = true;
        break;
      case ParticleEffectContent.ShapeType.Sphere:
        ui.sphereEmissionToggle.toggle.isOn = true;
        break;
      case ParticleEffectContent.ShapeType.Circle:
        ui.circleEmissionToggle.toggle.isOn = true;
        break;
      case ParticleEffectContent.ShapeType.Rectangle:
        ui.rectangleEmissionToggle.toggle.isOn = true;
        break;
    }
    AddEmissionToggleListeners();
    particlesPreview.Open(particleEffect, !ui.burstPreviewToggle.isOn);
  }

  public void Close()
  {
    particlesPreview.Close();
    this.gameObject.SetActive(false);
  }

  private void OnParticleEffectChanged(string pfxId)
  {
    if (this.pfxId == pfxId)
    {
      Refresh();
    }
  }

  private void OnParticleEffectRemoved(string pfxId)
  {
    if (this.pfxId == pfxId)
    {
      Close();
    }
  }

  private void OnAddColorStopRequested(float position, Color value)
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    List<ParticleEffectContent.ColorStop> stops = particleEffect.content.colorOverLifetime;
    stops.Add(new ParticleEffectContent.ColorStop(value, position));
    stops.Sort((ParticleEffectContent.ColorStop cs1, ParticleEffectContent.ColorStop cs2) =>
    {
      return cs1.position.CompareTo(cs2.position);
    });
    system.PutParticleEffect(particleEffect);
  }

  private void OnChangeColorStopPositionRequested(string id, float position)
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    List<ParticleEffectContent.ColorStop> stops = particleEffect.content.colorOverLifetime;
    ParticleEffectContent.ColorStop stop = stops.Find((cs) => cs.id == id);
    stop.position = position;
    stops.Sort((ParticleEffectContent.ColorStop cs1, ParticleEffectContent.ColorStop cs2) =>
    {
      return cs1.position.CompareTo(cs2.position);
    });
    system.PutParticleEffect(particleEffect);
  }

  private void OnChangeColorStopValueRequested(string id, Color value)
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    List<ParticleEffectContent.ColorStop> stops = particleEffect.content.colorOverLifetime;
    ParticleEffectContent.ColorStop stop = stops.Find((cs) => cs.id == id);
    stop.value = value;
    system.PutParticleEffect(particleEffect);
  }

  private void OnRemoveColorStopRequested(string id)
  {
    ParticleEffect particleEffect = system.GetParticleEffect(pfxId);
    List<ParticleEffectContent.ColorStop> stops = particleEffect.content.colorOverLifetime;
    if (stops.Count < 2) return;
    stops.RemoveAll((cs) => cs.id == id);
    system.PutParticleEffect(particleEffect);
  }

  protected class ColorModel : GradientFieldUI<Color>.IModel
  {
    private List<ParticleEffectContent.ColorStop> stops;
    public ColorModel(List<ParticleEffectContent.ColorStop> stops)
    {
      this.stops = stops;
    }
    public int GetCount()
    {
      return stops.Count;
    }

    public string GetId(int index)
    {
      return stops[index].id;
    }

    public float GetPosition(int index)
    {
      return stops[index].position;
    }

    public Color GetValue(int index)
    {
      return stops[index].value;
    }
  }
}
