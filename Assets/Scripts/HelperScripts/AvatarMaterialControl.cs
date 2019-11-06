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
using UnityEngine;

public class AvatarMaterialControl : MonoBehaviour
{
  [SerializeField] Material avatarMaterial;
  [SerializeField] Material avatarEyesMaterial;
  [SerializeField] Material avatarHoloMaterial;
  [SerializeField] Material avatarEyesHoloMaterial;

  Material avatarMaterialInstance;
  Material avatarEyesMaterialInstance;
  Material avatarHoloMaterialInstance;
  Material avatarEyesHoloMaterialInstance;

  [SerializeField] MeshRenderer[] avatarPermanentRenderers;
  [SerializeField] MeshRenderer[] avatarRenderers;
  [SerializeField] MeshRenderer[] hologramEffects;
  [SerializeField] MeshRenderer avatarEyeRenderer;
  [SerializeField] MeshRenderer avatarPermanentEyeRenderer;

  float defaultHoloHue = .49f;
  float holoSaturation = .6f;
  float holoValue = .94f;

  NetworkingController networkingController;

  bool isHologram = false;

  void Awake()
  {
    Util.FindIfNotSet(this, ref networkingController);
    avatarMaterialInstance = Instantiate(avatarMaterial);
    avatarEyesMaterialInstance = Instantiate(avatarEyesMaterial);
    avatarHoloMaterialInstance = Instantiate(avatarHoloMaterial);
    avatarEyesHoloMaterialInstance = Instantiate(avatarEyesHoloMaterial);
  }

  public void SetToHologramMaterial()
  {
    if (this == null) return;

    isHologram = true;
    foreach (MeshRenderer renderer in avatarRenderers)
    {
      renderer.material = avatarHoloMaterialInstance;
    }

    avatarEyeRenderer.material = avatarEyesHoloMaterialInstance;

    foreach (MeshRenderer renderer in avatarPermanentRenderers)
    {
      renderer.material = avatarHoloMaterialInstance;
    }
    avatarPermanentEyeRenderer.material = avatarEyesHoloMaterialInstance;

  }

  public void SetToStandardMaterial()
  {
    isHologram = false;

    foreach (MeshRenderer renderer in avatarRenderers)
    {
      renderer.material = avatarMaterialInstance;
    }

    avatarEyeRenderer.material = avatarEyesMaterialInstance;
  }

  internal void SetTint(Color effectiveTint)
  {
    float h, s, v;
    Color.RGBToHSV(effectiveTint, out h, out s, out v);

    avatarMaterialInstance.SetColor("_MainTint", effectiveTint);

    Color holoColor = ArtUtil.GetHologramColor(effectiveTint);

    foreach (MeshRenderer renderer in hologramEffects)
    {
      renderer.material.SetColor("_MainTint", holoColor);
    }
    avatarHoloMaterialInstance.SetColor("_MainTint", holoColor);
    avatarEyesHoloMaterialInstance.SetColor("_MainTint", holoColor);

    if (isHologram)
    {
      SetToHologramMaterial();
    }
    else
    {
      SetToStandardMaterial();
    }
  }
}
