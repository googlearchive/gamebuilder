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
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public class PhysicsTabController : AbstractTabController
{
  [SerializeField] PhysicsTabUI physicsTabUI;

  private VoosActor actor;

  public Preset ghostPreset;
  public Preset solidPreset;
  public Preset objectPreset;
  public Preset characterPreset;

  const float MIN_MASS = .01f;
  const float MAX_MASS = 20f;

  [Serializable]
  public struct Preset
  {
    public bool gravity;
    public bool solid;
    public bool pushable;
    public bool upright;
    public bool concave;

    public string description;

    public override string ToString()
    {
      return $"{gravity} {solid} {pushable} {upright} {concave}";
    }
  }

  enum PhysicsLock
  {
    Rotation,
    X,
    Y,
    Z
  };

  public override void Setup()
  {
    physicsTabUI.gravityToggle.onValueChanged.AddListener(SetGravity);
    physicsTabUI.solidToggle.onValueChanged.AddListener(SetIsSolid);
    physicsTabUI.staysUprightToggle.onValueChanged.AddListener(SetKeepUpright);
    physicsTabUI.pushableToggle.onValueChanged.AddListener(SetPhysics);
    physicsTabUI.concaveToggle.onValueChanged.AddListener(SetConcave);

    physicsTabUI.ghostPresetToggle.onValueChanged.AddListener((on) => { if (on) SetToPreset(ghostPreset); });
    physicsTabUI.solidPresetToggle.onValueChanged.AddListener((on) => { if (on) SetToPreset(solidPreset); });
    physicsTabUI.objectPresetToggle.onValueChanged.AddListener((on) => { if (on) SetToPreset(objectPreset); });
    physicsTabUI.characterPresetToggle.onValueChanged.AddListener((on) => { if (on) SetToPreset(characterPreset); });

    physicsTabUI.rotationLockToggle.onValueChanged.AddListener(SetRotationLock);
    physicsTabUI.positionXLockToggle.onValueChanged.AddListener(SetXLock);
    physicsTabUI.positionYLockToggle.onValueChanged.AddListener(SetYLock);
    physicsTabUI.positionZLockToggle.onValueChanged.AddListener(SetZLock);

    physicsTabUI.massSlider.onValueChanged.AddListener(SetMass);
    physicsTabUI.massTextInput.onEndEdit.AddListener(SetMass);

    physicsTabUI.massDragSlider.onValueChanged.AddListener(value => SetActorValueWithSlider(value, actor.SetDrag));
    physicsTabUI.massDragTextInput.onEndEdit.AddListener(value => SetActorValueWithTextInput(value, actor.SetDrag));

    physicsTabUI.angularDragSlider.onValueChanged.AddListener(value => SetActorValueWithSlider(value, actor.SetAngularDrag));
    physicsTabUI.angularDragTextInput.onEndEdit.AddListener(value => SetActorValueWithTextInput(value, actor.SetAngularDrag));

    physicsTabUI.bounceSlider.onValueChanged.AddListener(value => SetActorValueWithSlider(value, actor.SetBounciness));
    physicsTabUI.bounceTextInput.onEndEdit.AddListener(value => SetActorValueWithTextInput(value, actor.SetBounciness));
  }

  private void SetRotationLock(bool value)
  {
    actor.SetFreezeRotations(value);
    RefreshClones();
  }

  private void SetXLock(bool value)
  {
    actor.SetFreezeX(value);
    RefreshClones();
  }
  private void SetYLock(bool value)
  {
    actor.SetFreezeY(value);
    RefreshClones();
  }
  private void SetZLock(bool value)
  {
    actor.SetFreezeZ(value);
    RefreshClones();
  }

  private void SetMass(float value)
  {
    actor.SetMass(value);
    RefreshClones();
  }

  private void SetMass(string value)
  {
    float numValue;
    if (float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out numValue))
    {
      numValue = Mathf.Clamp(numValue, MIN_MASS, MAX_MASS);
      actor.SetMass(numValue);
      RefreshClones();
    }
  }

  private void SetActorValueWithSlider(float value, System.Action<float> action)
  {
    action(value);
    RefreshClones();
  }

  private void SetActorValueWithTextInput(string value, System.Action<float> action)
  {
    float numValue;
    if (float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out numValue))
    {
      numValue = Mathf.Clamp01(numValue);
      action(numValue);
      RefreshClones();
    }
  }
  /* 
    private void SetMassDrag(float value)
    {
      actor.SetDrag(value);
      RefreshClones();
    }

    private void SetMassDrag(string value)
    {
      float numValue;
      if (float.TryParse(value, out numValue))
      {
        numValue = Mathf.Clamp01(numValue);
        actor.SetDrag(numValue);
        RefreshClones();
      }
    }
   */
  public override void Open(VoosActor actor, Dictionary<string, object> props)
  {
    base.Open(actor, props);

    this.actor = actor;
    if (actor != null) UpdateActorValues();
  }

  protected override void Update()
  {
    base.Update();
    if (actor != null) UpdateActorValues();
  }

  private void UpdateActorValues()
  {
    Preset current = new Preset
    {
      gravity = actor.GetEnableGravity(),
      solid = actor.GetIsSolid(),
      pushable = actor.GetEnablePhysics(),
      upright = actor.GetKeepUpright(),
      concave = actor.GetUseConcaveCollider()
    };

    physicsTabUI.gravityToggle.isOn = current.gravity;
    physicsTabUI.solidToggle.isOn = current.solid;
    physicsTabUI.pushableToggle.isOn = current.pushable;
    physicsTabUI.staysUprightToggle.isOn = current.upright;
    physicsTabUI.concaveToggle.isOn = current.concave;
    physicsTabUI.concaveToggle.gameObject.SetActive(current.solid && !current.pushable);

    physicsTabUI.gravityToggle.gameObject.SetActive(current.pushable);
    physicsTabUI.staysUprightToggle.gameObject.SetActive(current.pushable);
    physicsTabUI.slidersSection.gameObject.SetActive(current.pushable);

    physicsTabUI.rotationLockToggle.isOn = actor.GetFreezeRotations();
    physicsTabUI.positionXLockToggle.isOn = actor.GetFreezeX();
    physicsTabUI.positionYLockToggle.isOn = actor.GetFreezeY();
    physicsTabUI.positionZLockToggle.isOn = actor.GetFreezeZ();

    physicsTabUI.ghostPresetToggle.isOn = BothSolidAndPushable(current, ghostPreset);
    physicsTabUI.solidPresetToggle.isOn = BothSolidAndPushable(current, solidPreset);
    physicsTabUI.objectPresetToggle.isOn = PresetsMatch(current, objectPreset);
    physicsTabUI.characterPresetToggle.isOn = PresetsMatch(current, characterPreset);

    if (AllPresetsOff())
    {
      physicsTabUI.presetDescription.text = "";
    }

    TrySettingSliderWithText(actor.GetMass(), physicsTabUI.massSlider, physicsTabUI.massTextInput);
    TrySettingSliderWithText(actor.GetDrag(), physicsTabUI.massDragSlider, physicsTabUI.massDragTextInput);
    TrySettingSliderWithText(actor.GetAngularDrag(), physicsTabUI.angularDragSlider, physicsTabUI.angularDragTextInput);
    TrySettingSliderWithText(actor.GetBounciness(), physicsTabUI.bounceSlider, physicsTabUI.bounceTextInput);
  }

  private bool AllPresetsOff()
  {
    return !physicsTabUI.ghostPresetToggle.isOn &&
   !physicsTabUI.solidPresetToggle.isOn &&
    !physicsTabUI.objectPresetToggle.isOn &&
    !physicsTabUI.characterPresetToggle.isOn;
  }

  void TrySettingSliderWithText(float value, UnityEngine.UI.Slider slider, TMPro.TMP_InputField textInput)
  {
    if (textInput.isFocused) return;

    slider.value = value;
    textInput.text = value.ToTwoDecimalPlaces();
  }

  bool BothSolidAndPushable(Preset a, Preset b)
  {
    return a.pushable == b.pushable &&
    a.solid == b.solid;

  }

  bool PresetsMatch(Preset a, Preset b)
  {
    return a.gravity == b.gravity &&
    a.pushable == b.pushable &&
    a.solid == b.solid &&
    a.upright == b.upright;
  }

  void SetGravity(bool value)
  {
    actor.SetEnableGravity(value);
    RefreshClones();
  }

  void SetIsSolid(bool value)
  {
    actor.SetIsSolid(value);
    RefreshClones();
  }

  void SetPhysics(bool value)
  {
    actor.SetEnablePhysics(value);
    RefreshClones();
  }

  void SetKeepUpright(bool value)
  {
    actor.SetKeepUpright(value);
    RefreshClones();
  }

  void SetConcave(bool value)
  {
    actor.SetUseConcaveCollider(value);
    RefreshClones();
  }

  void RefreshClones()
  {
    actor.ApplyPropertiesToClones();
  }

  void SetToPreset(Preset preset)
  {
    actor.SetEnableGravity(preset.gravity);
    actor.SetIsSolid(preset.solid);
    actor.SetEnablePhysics(preset.pushable);
    actor.SetKeepUpright(preset.upright);

    physicsTabUI.presetDescription.text = preset.description;
    RefreshClones();
  }
}
