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

public class ParticlesPreview : MonoBehaviour
{

  [SerializeField] ParticleSimulationController simulationControllerPrefab;
  private ParticleSimulationController simulationController;
  private ParticleEffect particleEffect;
  private bool stream;
  private ParticleEffectSystem pfxSystem;

  public void Setup()
  {
    Util.FindIfNotSet(this, ref pfxSystem);
  }

  public void Open(ParticleEffect particleEffect, bool stream)
  {
    this.particleEffect = particleEffect;
    this.stream = stream;
    if (simulationController == null)
    {
      simulationController = Instantiate(simulationControllerPrefab);
      simulationController.Setup();
    }
    simulationController.SetParticleEffect(particleEffect, stream);
    gameObject.SetActive(true);
  }

  public void Close()
  {
    gameObject.SetActive(false);
  }

  public void OnEnable()
  {
    if (particleEffect == null)
    {
      Close();
    }
    else
    {
      simulationController?.SetParticleEffect(particleEffect, stream);
    }
  }

  public void OnDestroy()
  {
    if (simulationController != null)
    {
      Destroy(simulationController.gameObject);
    }
  }

}
