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
using System.Collections.Generic;
using UnityEngine;
using URP = UnityEngine.Rendering.PostProcessing;

public class GameBuilderStage : MonoBehaviour
{
  public enum SkyType
  {
    // DO NOT CHANGE THE NAMES! The numbers and order can change.
    Day = 0,
    Space,
    Overcast,
    SolidColor
  };

  public static Color GetFogBaseColor(SkyType sky)
  {
    Color rv = Color.white;
    switch (sky)
    {
      case SkyType.Day:
        ColorUtility.TryParseHtmlString("#65B6FB", out rv);
        return rv;
      case SkyType.Space:
        ColorUtility.TryParseHtmlString("#3A1A24", out rv);
        return rv;
      case SkyType.Overcast:
        ColorUtility.TryParseHtmlString("#D2DCF7", out rv);
        return rv;
      case SkyType.SolidColor:
      default:
        return Color.white;
    }
  }

  public enum GroundType
  {
    // DO NOT CHANGE THE NAMES! The numbers and order can change.
    Grass = 0,
    Space,
    Snow,
    SolidColor
  };

  public enum CameraMode
  {
    // DO NOT CHANGE THE NAMES! The numbers and order can change.
    ThirdPerson = 0,
    Isometric,  // Misnamed: it's now a non-isometric slanted top-down view.
                // Not changing the name in order not to break old files.
    FirstPerson
  };

  public enum SceneLightingMode
  {
    // DO NOT CHANGE THE NAMES! The numbers and order can change.
    Day = 0,
    Night = 1,
    Dark = 2
  };

  static float DefaultGroundSize = 500f;
  static SkyType DefaultSkyType = SkyType.Day;
  static Color DefaultSkyColor = Color.gray;
  static GroundType DefaultGroundType = GroundType.Grass;
  static Color DefaultGroundColor = Color.white;
  static CameraMode DefaultInitialCameraMode = CameraMode.ThirdPerson;
  static SceneLightingMode DefaultSceneLightingMode = SceneLightingMode.Day;

  [System.Serializable]
  public struct Persisted
  {
    public static int FirstVersionWithSeparateGroundSize = 3;
    public static int FirstVersionWithSceneLightingMode = 4;
    public static int CurrentVersion = 4;
    public int version;

    public float groundSize;    // LEGACY!
    public bool nightMode;      // LEGACY!

    // BEGIN_GAME_BUILDER_CODE_GEN STAGE_PERSISTED_STRUCT_MEMBERS
    public float groundSizeX;    // GENERATED
    public float groundSizeZ;    // GENERATED
    public string skyType;    // GENERATED
    public Color skyColor;    // GENERATED
    public string groundType;    // GENERATED
    public Color groundColor;    // GENERATED
    public string initialCameraMode;    // GENERATED
    public int isoCamRotationIndex;    // GENERATED
    public string sceneLightingMode;    // GENERATED
    // END_GAME_BUILDER_CODE_GEN

    public void DoUpgrades()
    {
      if (version < 1)
      {
        groundSize = DefaultGroundSize;
      }

      if (version < 2)
      {
        groundColor = DefaultGroundColor;
        skyColor = DefaultSkyColor;
      }

      if (version < FirstVersionWithSeparateGroundSize)
      {
        groundSizeX = groundSize;
        groundSizeZ = groundSize;
      }

      if (version < FirstVersionWithSceneLightingMode)
      {
        // Previous versions had a nightMode boolean to toggle between night and day.
        sceneLightingMode = nightMode ? "Night" : "Day";
      }

      version = CurrentVersion;
    }
  }

  [SerializeField] Transform collisionWallPositiveZ;
  [SerializeField] Transform collisionWallPositiveX;
  [SerializeField] Transform collisionWallNegativeZ;
  [SerializeField] Transform collisionWallNegativeX;
  [SerializeField] Material[] skyboxMaterialsBase;
  [SerializeField] Material[] groundMaterialsBase;

  [SerializeField] GameObject HighQualityLighting;
  [SerializeField] GameObject LowQualityLighting;

  [SerializeField] URP.PostProcessVolume post;

  Material[] skyboxMaterials;
  Material[] groundMaterials;

  PhotonView photonView;
  SkyboxRotator skyboxRotator;

  TerrainManager terrain;

  bool hideAllGrass = false;
  Material tempSkyBox;
  Material tempGroundMaterial;

  public event System.Action OnUpdateGroundType;

  public URP.PostProcessVolume GetMainPostVolume() { return post; }

  public void SetHideAllGrass(bool hide)
  {
    hideAllGrass = hide;
  }

  // To diagnose NPEs in the wild..
  void CheckHQL()
  {
    Util.Log($"HQL active? {HighQualityLighting.activeInHierarchy}");
  }

  void CheckLQL()
  {
    Util.Log($"HQL active? {LowQualityLighting.activeInHierarchy}");
  }

  void CheckPost()
  {
    Util.Log($"Post enabled? {post.enabled}");
  }

  void CheckPostProfile()
  {
    Util.Log($"post profile has SSAO? {post.profile.HasSettings<URP.AmbientOcclusion>()}");
  }

  void OnQualityLevelChanged()
  {
    CheckHQL();
    CheckLQL();
    CheckPost();
    CheckPostProfile();

    var quality = GameBuilderApplication.GetQuality();
    if (quality == GameBuilderApplication.Quality.High)
    {
      HighQualityLighting.SetActive(true);
      LowQualityLighting.SetActive(false);
      post.enabled = true;
      post.profile.GetSetting<URP.MotionBlur>().enabled.value = true;
      post.profile.GetSetting<URP.AmbientOcclusion>().enabled.value = true;
    }
    else if (quality == GameBuilderApplication.Quality.Medium)
    {
      // Still want shadows for med, but their distance is reduced.
      HighQualityLighting.SetActive(true);
      LowQualityLighting.SetActive(false);
      post.enabled = true;
      post.profile.GetSetting<URP.MotionBlur>().enabled.value = false;
      post.profile.GetSetting<URP.AmbientOcclusion>().enabled.value = false;
    }
    else
    {
      Debug.Assert(quality == GameBuilderApplication.Quality.Low, $"Expected Low quality. Got: {quality.ToString()}");

      HighQualityLighting.SetActive(false);
      LowQualityLighting.SetActive(true);

      // NO POST!
      post.enabled = false;
    }
  }

  void Awake()
  {
    photonView = PhotonView.Get(this);
    Util.FindIfNotSet(this, ref skyboxRotator);
    Util.FindIfNotSet(this, ref terrain);

    //make instances of all these materials so we don't make permanent changes
    skyboxMaterials = new Material[skyboxMaterialsBase.Length];
    for (int i = 0; i < skyboxMaterialsBase.Length; i++)
    {
      skyboxMaterials[i] = Instantiate(skyboxMaterialsBase[i]);
    }

    groundMaterials = new Material[groundMaterialsBase.Length];
    for (int i = 0; i < groundMaterialsBase.Length; i++)
    {
      groundMaterials[i] = Instantiate(groundMaterialsBase[i]);
    }

    OnQualityLevelChanged();
  }

  void OnEnable()
  {
    GameBuilderApplication.onQualityLevelChanged += OnQualityLevelChanged;
  }

  void OnDisable()
  {
    GameBuilderApplication.onQualityLevelChanged -= OnQualityLevelChanged;
  }

  public Vector3 GetWorldMax()
  {
    return new Vector3(
      GetGroundSizeX() / 2,
      (TerrainManager.BlocksYStart + TerrainManager.BlocksYCount) * TerrainManager.BlockHeight,
      GetGroundSizeZ() / 2);
  }

  public Vector3 GetWorldMin()
  {
    return new Vector3(
      -GetGroundSizeX() / 2,
      TerrainManager.BlocksYStart * TerrainManager.BlockHeight,
      -GetGroundSizeZ() / 2);
  }

  void Start()
  {
    // BEGIN_GAME_BUILDER_CODE_GEN FORCE_UPDATE_ON_START
    UpdateGroundSizeX();    // GENERATED
    UpdateGroundSizeZ();    // GENERATED
    UpdateSkyType();    // GENERATED
    UpdateSkyColor();    // GENERATED
    UpdateGroundType();    // GENERATED
    UpdateGroundColor();    // GENERATED
    UpdateSceneLightingMode();    // GENERATED
                                  // END_GAME_BUILDER_CODE_GEN
  }

  public void Load(Persisted state)
  {
    state.DoUpgrades();
    // BEGIN_GAME_BUILDER_CODE_GEN STAGE_LOAD_PERSISTED
    SetGroundSizeXLocal(state.groundSizeX);    // GENERATED
    SetGroundSizeZLocal(state.groundSizeZ);    // GENERATED
    SetSkyTypeLocal(state.skyType.IsNullOrEmpty() ? DefaultSkyType : Util.ParseEnum<SkyType>(state.skyType));    // GENERATED
    SetSkyColorLocal(state.skyColor);    // GENERATED
    SetGroundTypeLocal(state.groundType.IsNullOrEmpty() ? DefaultGroundType : Util.ParseEnum<GroundType>(state.groundType));    // GENERATED
    SetGroundColorLocal(state.groundColor);    // GENERATED
    SetInitialCameraModeLocal(state.initialCameraMode.IsNullOrEmpty() ? DefaultInitialCameraMode : Util.ParseEnum<CameraMode>(state.initialCameraMode));    // GENERATED
    SetIsoCamRotationIndexLocal(state.isoCamRotationIndex);    // GENERATED
    SetSceneLightingModeLocal(state.sceneLightingMode.IsNullOrEmpty() ? DefaultSceneLightingMode : Util.ParseEnum<SceneLightingMode>(state.sceneLightingMode));    // GENERATED
                                                                                                                                                                   // END_GAME_BUILDER_CODE_GEN
  }

  public Persisted Save()
  {
    return new Persisted
    {
      // BEGIN_GAME_BUILDER_CODE_GEN STAGE_SAVE_ASSIGNMENTS
      groundSizeX = GetGroundSizeX(),    // GENERATED
      groundSizeZ = GetGroundSizeZ(),    // GENERATED
      skyType = GetSkyType().ToString(),    // GENERATED
      skyColor = GetSkyColor(),    // GENERATED
      groundType = GetGroundType().ToString(),    // GENERATED
      groundColor = GetGroundColor(),    // GENERATED
      initialCameraMode = GetInitialCameraMode().ToString(),    // GENERATED
      isoCamRotationIndex = GetIsoCamRotationIndex(),    // GENERATED
      sceneLightingMode = GetSceneLightingMode().ToString(),    // GENERATED
      // END_GAME_BUILDER_CODE_GEN
      version = Persisted.CurrentVersion
    };
  }

  public void SetIsRunning(bool running)
  {
    skyboxRotator.SetIsRunning(running);
  }

  void UpdateGroundTextureScaleOffset(Material ground)
  {
    float texScaleX = groundSizeX / 5f;
    float negOffsetX = groundSizeX / 2f / 5f - 0.25f;
    float texScaleZ = groundSizeZ / 5f;
    float negOffsetZ = groundSizeZ / 2f / 5f - 0.25f;
    ground.SetTextureScale("_MainTex", new Vector2(texScaleX, texScaleZ));
    ground.SetTextureOffset("_MainTex", new Vector2(-negOffsetX, -negOffsetZ));
  }

  void UpdateGroundSizeX()
  {
    UpdateGroundSize();
  }

  void UpdateGroundSizeZ()
  {
    UpdateGroundSize();
  }

  void UpdateGroundSize()
  {
    float baseWallPositionX = groundSizeX / 2f;
    float baseWallPositionZ = groundSizeZ / 2f;

    //update walls
    collisionWallPositiveX.localPosition = new Vector3(baseWallPositionX, 2, 0);
    collisionWallNegativeX.localPosition = new Vector3(-baseWallPositionX, 2, 0);
    collisionWallPositiveZ.localPosition = new Vector3(0, 2, baseWallPositionZ);
    collisionWallNegativeZ.localPosition = new Vector3(0, 2, -baseWallPositionZ);

    foreach (Material ground in groundMaterials)
    {
      UpdateGroundTextureScaleOffset(ground);
    }

    if (tempGroundMaterial != null)
    {
      UpdateGroundTextureScaleOffset(tempGroundMaterial);
    }

    // grassSpawn.UpdateGrassDistance(baseWallPosition);
    // snowyGrassSpawn.UpdateGrassDistance(baseWallPosition);
  }

  // Pass in null to clear it, using the scene's skybox.
  public void SetTempSkyBox(Material skybox)
  {
    tempSkyBox = skybox;
    UpdateSkyBox();
  }

  void UpdateSkyBox()
  {
    if (tempSkyBox != null)
    {
      RenderSettings.skybox = tempSkyBox;
    }
    else
    {
      RenderSettings.skybox = skyboxMaterials[(int)GetSkyType()];
    }
  }

  // Depends: skyType, skyColor
  void UpdateFogColor()
  {
    // TEMP HACK adding grey to skyColor cuz for some reason default is grey?
    float hackyScale = skyType == SkyType.SolidColor ? 1f : 2f;
    RenderSettings.fogColor = GetFogBaseColor(skyType) * (skyColor * hackyScale);
  }

  void UpdateSkyType()
  {
    UpdateSkyBox();
    UpdateFogColor();
  }

  const float SKY_GRADIENT_DARK_SAT_MAX = 1;
  const float SKY_GRADIENT_DARK_SAT_MOD = .3f;

  const float SKY_GRADIENT_DARK_VAL_MIN = 0;
  const float SKY_GRADIENT_DARK_VAL_MOD = .7f;

  void UpdateSkyColor()
  {
    // This is bad.
    skyboxMaterials[(int)SkyType.Day].SetColor("_Tint", skyColor);
    skyboxMaterials[(int)SkyType.Space].SetColor("_Tint", skyColor);
    skyboxMaterials[(int)SkyType.Overcast].SetColor("_Tint", skyColor);
    skyboxMaterials[(int)SkyType.SolidColor].SetColor("_Color2", skyColor);

    // To make the second darker color in the gradient
    float darkHue, darkSat, darkVal;
    Color.RGBToHSV(skyColor, out darkHue, out darkSat, out darkVal);
    darkSat = darkSat == 0 ? 0 : Mathf.Min(SKY_GRADIENT_DARK_SAT_MAX, darkSat + SKY_GRADIENT_DARK_SAT_MOD);
    darkVal = Mathf.Max(SKY_GRADIENT_DARK_VAL_MIN, darkVal - SKY_GRADIENT_DARK_VAL_MOD);
    skyboxMaterials[(int)SkyType.SolidColor].SetColor("_Color1", Color.HSVToRGB(darkHue, darkSat, darkVal));

    UpdateFogColor();
  }

  // TODO Should rename these generated functions to "OnGroundTypeChanged"
  void UpdateGroundType()
  {
    OnUpdateGroundType?.Invoke();
  }

  static int[] PossibleGroundTintShaderProperties = new int[] {
    Shader.PropertyToID("_ColorTint"),
    Shader.PropertyToID("_Color"),
    Shader.PropertyToID("_MainTint")
  };

  void UpdateGroundMaterialTint(Material ground)
  {
    foreach (int prop in PossibleGroundTintShaderProperties)
    {
      if (ground.HasProperty(prop))
      {
        ground.SetColor(prop, groundColor);
        return;
      }
    }
    throw new System.Exception($"Ground material {ground.name} does not have a tint shader property that we know about.");
  }

  // TODO Should rename these generated functions to "OnGroundColorChanged"
  void UpdateGroundColor()
  {
    foreach (Material ground in groundMaterials)
    {
      UpdateGroundMaterialTint(ground);
    }

    // NOTE: We are purposefully not updating tempGroundMaterial tint.
  }

  void UpdateSceneLightingMode()
  {

  }

  public Vector2 GetGroundSize()
  {
    return new Vector2(GetGroundSizeX(), GetGroundSizeZ());
  }

  // BEGIN_GAME_BUILDER_CODE_GEN STAGE_CSHARP
  private float groundSizeX;    // GENERATED

  public float GetGroundSizeX()    // GENERATED
  {
    return groundSizeX;    // GENERATED
  }

  bool SetGroundSizeXLocal(float newGroundSizeX)    // GENERATED
  {
    if (Mathf.Abs(groundSizeX - newGroundSizeX) < 1e-4f)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.groundSizeX = newGroundSizeX;    // GENERATED
    UpdateGroundSizeX();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetGroundSizeXRPC(float newGroundSizeX)    // GENERATED
  {
    SetGroundSizeXLocal(newGroundSizeX);    // GENERATED
  }

  public void SetGroundSizeX(float newGroundSizeX)    // GENERATED
  {
    if (SetGroundSizeXLocal(newGroundSizeX))    // GENERATED
    {
      photonView.RPC("SetGroundSizeXRPC", PhotonTargets.AllViaServer, newGroundSizeX);    // GENERATED
    }
  }
  private float groundSizeZ;    // GENERATED

  public float GetGroundSizeZ()    // GENERATED
  {
    return groundSizeZ;    // GENERATED
  }

  bool SetGroundSizeZLocal(float newGroundSizeZ)    // GENERATED
  {
    if (Mathf.Abs(groundSizeZ - newGroundSizeZ) < 1e-4f)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.groundSizeZ = newGroundSizeZ;    // GENERATED
    UpdateGroundSizeZ();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetGroundSizeZRPC(float newGroundSizeZ)    // GENERATED
  {
    SetGroundSizeZLocal(newGroundSizeZ);    // GENERATED
  }

  public void SetGroundSizeZ(float newGroundSizeZ)    // GENERATED
  {
    if (SetGroundSizeZLocal(newGroundSizeZ))    // GENERATED
    {
      photonView.RPC("SetGroundSizeZRPC", PhotonTargets.AllViaServer, newGroundSizeZ);    // GENERATED
    }
  }
  private SkyType skyType;    // GENERATED

  public SkyType GetSkyType()    // GENERATED
  {
    return skyType;    // GENERATED
  }

  bool SetSkyTypeLocal(SkyType newSkyType)    // GENERATED
  {
    if (skyType == newSkyType)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.skyType = newSkyType;    // GENERATED
    UpdateSkyType();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetSkyTypeRPC(SkyType newSkyType)    // GENERATED
  {
    SetSkyTypeLocal(newSkyType);    // GENERATED
  }

  public void SetSkyType(SkyType newSkyType)    // GENERATED
  {
    if (SetSkyTypeLocal(newSkyType))    // GENERATED
    {
      photonView.RPC("SetSkyTypeRPC", PhotonTargets.AllViaServer, newSkyType);    // GENERATED
    }
  }
  private Color skyColor;    // GENERATED

  public Color GetSkyColor()    // GENERATED
  {
    return skyColor;    // GENERATED
  }

  bool SetSkyColorLocal(Color newSkyColor)    // GENERATED
  {
    if (skyColor == newSkyColor)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.skyColor = newSkyColor;    // GENERATED
    UpdateSkyColor();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetSkyColorRPC(Color newSkyColor)    // GENERATED
  {
    SetSkyColorLocal(newSkyColor);    // GENERATED
  }

  public void SetSkyColor(Color newSkyColor)    // GENERATED
  {
    if (SetSkyColorLocal(newSkyColor))    // GENERATED
    {
      photonView.RPC("SetSkyColorRPC", PhotonTargets.AllViaServer, newSkyColor);    // GENERATED
    }
  }
  private GroundType groundType;    // GENERATED

  public GroundType GetGroundType()    // GENERATED
  {
    return groundType;    // GENERATED
  }

  bool SetGroundTypeLocal(GroundType newGroundType)    // GENERATED
  {
    if (groundType == newGroundType)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.groundType = newGroundType;    // GENERATED
    UpdateGroundType();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetGroundTypeRPC(GroundType newGroundType)    // GENERATED
  {
    SetGroundTypeLocal(newGroundType);    // GENERATED
  }

  public void SetGroundType(GroundType newGroundType)    // GENERATED
  {
    if (SetGroundTypeLocal(newGroundType))    // GENERATED
    {
      photonView.RPC("SetGroundTypeRPC", PhotonTargets.AllViaServer, newGroundType);    // GENERATED
    }
  }
  private Color groundColor;    // GENERATED

  public Color GetGroundColor()    // GENERATED
  {
    return groundColor;    // GENERATED
  }

  bool SetGroundColorLocal(Color newGroundColor)    // GENERATED
  {
    if (groundColor == newGroundColor)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.groundColor = newGroundColor;    // GENERATED
    UpdateGroundColor();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetGroundColorRPC(Color newGroundColor)    // GENERATED
  {
    SetGroundColorLocal(newGroundColor);    // GENERATED
  }

  public void SetGroundColor(Color newGroundColor)    // GENERATED
  {
    if (SetGroundColorLocal(newGroundColor))    // GENERATED
    {
      photonView.RPC("SetGroundColorRPC", PhotonTargets.AllViaServer, newGroundColor);    // GENERATED
    }
  }
  private CameraMode initialCameraMode;    // GENERATED

  public CameraMode GetInitialCameraMode()    // GENERATED
  {
    return initialCameraMode;    // GENERATED
  }

  bool SetInitialCameraModeLocal(CameraMode newInitialCameraMode)    // GENERATED
  {
    if (initialCameraMode == newInitialCameraMode)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.initialCameraMode = newInitialCameraMode;    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetInitialCameraModeRPC(CameraMode newInitialCameraMode)    // GENERATED
  {
    SetInitialCameraModeLocal(newInitialCameraMode);    // GENERATED
  }

  public void SetInitialCameraMode(CameraMode newInitialCameraMode)    // GENERATED
  {
    if (SetInitialCameraModeLocal(newInitialCameraMode))    // GENERATED
    {
      photonView.RPC("SetInitialCameraModeRPC", PhotonTargets.AllViaServer, newInitialCameraMode);    // GENERATED
    }
  }
  private int isoCamRotationIndex;    // GENERATED

  public int GetIsoCamRotationIndex()    // GENERATED
  {
    return isoCamRotationIndex;    // GENERATED
  }

  bool SetIsoCamRotationIndexLocal(int newIsoCamRotationIndex)    // GENERATED
  {
    if (isoCamRotationIndex == newIsoCamRotationIndex)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.isoCamRotationIndex = newIsoCamRotationIndex;    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetIsoCamRotationIndexRPC(int newIsoCamRotationIndex)    // GENERATED
  {
    SetIsoCamRotationIndexLocal(newIsoCamRotationIndex);    // GENERATED
  }

  public void SetIsoCamRotationIndex(int newIsoCamRotationIndex)    // GENERATED
  {
    if (SetIsoCamRotationIndexLocal(newIsoCamRotationIndex))    // GENERATED
    {
      photonView.RPC("SetIsoCamRotationIndexRPC", PhotonTargets.AllViaServer, newIsoCamRotationIndex);    // GENERATED
    }
  }
  private SceneLightingMode sceneLightingMode;    // GENERATED

  public SceneLightingMode GetSceneLightingMode()    // GENERATED
  {
    return sceneLightingMode;    // GENERATED
  }

  bool SetSceneLightingModeLocal(SceneLightingMode newSceneLightingMode)    // GENERATED
  {
    if (sceneLightingMode == newSceneLightingMode)    // GENERATED
    {
      return false;    // GENERATED
    }
    this.sceneLightingMode = newSceneLightingMode;    // GENERATED
    UpdateSceneLightingMode();    // GENERATED
    return true;    // GENERATED
  }

  [PunRPC]
  void SetSceneLightingModeRPC(SceneLightingMode newSceneLightingMode)    // GENERATED
  {
    SetSceneLightingModeLocal(newSceneLightingMode);    // GENERATED
  }

  public void SetSceneLightingMode(SceneLightingMode newSceneLightingMode)    // GENERATED
  {
    if (SetSceneLightingModeLocal(newSceneLightingMode))    // GENERATED
    {
      photonView.RPC("SetSceneLightingModeRPC", PhotonTargets.AllViaServer, newSceneLightingMode);    // GENERATED
    }
  }
  // END_GAME_BUILDER_CODE_GEN
}
