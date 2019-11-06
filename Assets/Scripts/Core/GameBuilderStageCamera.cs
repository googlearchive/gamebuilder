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

[RequireComponent(typeof(Camera))]
public class GameBuilderStageCamera : MonoBehaviour
{
  [SerializeField] GameBuilderStage stage;
  [SerializeField] GameObject snowFallEffect;

  GameObject snowFallEffectInstance;

  void OnQualityLevelChanged()
  {
    GetComponent<Camera>().renderingPath =
      GameBuilderApplication.GetQuality() == GameBuilderApplication.Quality.Low
      ? RenderingPath.Forward
      : RenderingPath.DeferredShading;
  }

  void Awake()
  {
    Util.FindIfNotSet(this, ref stage);
    UpdateGroundType();

    OnQualityLevelChanged();
  }

  void UpdateGroundType()
  {
    if (stage.GetGroundType() == GameBuilderStage.GroundType.Snow)
    {
      if (snowFallEffectInstance == null)
      {
        snowFallEffectInstance = GameObject.Instantiate(snowFallEffect, transform.position, transform.rotation, transform);
      }
    }
    else
    {
      if (snowFallEffectInstance != null)
      {
        GameObject.Destroy(snowFallEffectInstance);
      }
    }
  }

  void OnEnable()
  {
    GameBuilderApplication.onQualityLevelChanged += OnQualityLevelChanged;
    stage.OnUpdateGroundType += UpdateGroundType;
  }

  void OnDisable()
  {
    GameBuilderApplication.onQualityLevelChanged -= OnQualityLevelChanged;
    stage.OnUpdateGroundType -= UpdateGroundType;
  }

  // Use this for initialization
  void Start()
  {

  }

  // Update is called once per frame
  void Update()
  {

  }
}
