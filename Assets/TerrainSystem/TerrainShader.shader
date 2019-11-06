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

Shader "Custom/TerrainShader"
{
  Properties
  {
    [HideInInspector]_MainTex("Albedo (RGB)", 2D) = "white" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
    _Cutoff("Alpha cutoff", Range(0,1)) = 0
  }

  SubShader
  {
    Tags
    {
      "RenderType" = "TransparentCutout"
      "Queue" = "AlphaTest"
    }
    LOD 200

    CGPROGRAM
    // Physically based Standard lighting model, and enable shadows on all light types
    // NOTE: addshadow make some diff in shadows. Like, less slivers..
    #pragma surface surf Standard fullforwardshadows vertex:vert alphatest:_Cutoff addshadow

    // Use shader model 3.0 target, to get nicer looking lighting
    #pragma target 3.0

    UNITY_DECLARE_TEX2DARRAY(_tex_array);
    UNITY_DECLARE_TEX2DARRAY(_border_tex_array);
    float _terrain_min_ambient;

    struct Input
    {
      float3 texcoord;
      float4 color : COLOR;
    };

    half _Glossiness;
    half _Metallic;

    // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
    // #pragma instancing_options assumeuniformscaling
    UNITY_INSTANCING_BUFFER_START(Props)
    // put more per-instance properties here
    UNITY_INSTANCING_BUFFER_END(Props)
    
    void vert(inout appdata_full v, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      o.texcoord.xy = v.texcoord;
      o.texcoord.z = v.color.b * 255.0 - 0.5;
    }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
      // int style = IN.color.b * 25;

      fixed4 tex = UNITY_SAMPLE_TEX2DARRAY(_tex_array, IN.texcoord.xyz);
      tex.a = 1.0;

      if (IN.color.g > 0.99)
      {
        tex = UNITY_SAMPLE_TEX2DARRAY(_border_tex_array, IN.texcoord.xyz);
      }

      o.Albedo = tex.rgb;
      // Metallic and smoothness come from slider variables
      o.Metallic = _Metallic;
      o.Smoothness = _Glossiness;
      o.Occlusion = lerp(IN.color[0], 1.0, _terrain_min_ambient);
      o.Alpha = tex.a;
      
      // bool color_only = false;
      // if (color_only)
      // {
        //   o.Albedo = half3(0,0,0);
        //   o.Metallic = 1;
        //   o.Occlusion = 0;
        //   o.Emission = IN.color[0];
      // }
    }
    ENDCG
  }

  // LOD for laptops. Skip the border masking stuff, so purely opaque and no dependent texture reads.
  SubShader
  {
    Tags
    {
      "RenderType" = "Opaque"
    }
    LOD 150

    CGPROGRAM
    // Physically based Standard lighting model, and enable shadows on all light types
    #pragma surface surf Standard fullforwardshadows vertex:vert

    // Use shader model 3.0 target, to get nicer looking lighting
    #pragma target 3.0

    UNITY_DECLARE_TEX2DARRAY(_tex_array);
    float _terrain_min_ambient;

    struct Input
    {
      float3 texcoord;
      float ao;
    };

    half _Glossiness;
    half _Metallic;

    // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
    // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
    // #pragma instancing_options assumeuniformscaling
    UNITY_INSTANCING_BUFFER_START(Props)
    // put more per-instance properties here
    UNITY_INSTANCING_BUFFER_END(Props)
    
    void vert(inout appdata_full v, out Input o)
    {
      UNITY_INITIALIZE_OUTPUT(Input, o);
      o.texcoord.xy = v.texcoord;
      o.texcoord.z = v.color.b * 255.0 - 0.5;
      o.ao = v.color.r;
    }

    void surf (Input IN, inout SurfaceOutputStandard o)
    {
      fixed3 tex = UNITY_SAMPLE_TEX2DARRAY(_tex_array, IN.texcoord.xyz);
      o.Albedo = tex.rgb;
      o.Metallic = _Metallic;
      o.Smoothness = _Glossiness;
      o.Occlusion = lerp(IN.ao, 1.0, _terrain_min_ambient);
    }
    ENDCG
  }

  FallBack "Diffuse"
}
