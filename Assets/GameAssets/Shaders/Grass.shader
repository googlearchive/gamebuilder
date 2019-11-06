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

// Upgrade NOTE: upgraded instancing buffer 'Foliage' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Foliage"
{
	Properties
	{
		[HideInInspector]_ToonMapTexture("Toon Map Texture", 2D) = "white" {}
		_Texture("Texture", 2D) = "white" {}
		_MainTint("MainTint", Color) = (1,1,1,0)
		_WindSizeHorizontal("Wind Size Horizontal", Range( 0 , 1)) = 0.25
		_WindFrequency("Wind Frequency", Range( 0 , 0.75)) = 0.5
		[HideInInspector]_EffectRadius("Effect Radius", Range( 0 , 10)) = 1
		_WindSizeVertical("Wind Size Vertical", Range( 0 , 1)) = 0
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HideInInspector]_ColorMask("Color Mask", 2D) = "white" {}
		[HideInInspector]_OffsetGradient("Offset Gradient", 2D) = "white" {}
		[HideInInspector]_GravityGradient("Gravity Gradient", 2D) = "white" {}
		[HideInInspector]_OffsetMultiplier("Offset Multiplier", Range( 0 , 10)) = 1
		[HideInInspector]_EffectClampMax("Effect Clamp Max", Range( 0 , 10)) = 1
		[HideInInspector]_OffsetGradientStrength("Offset Gradient Strength", Range( 0 , 1)) = 0.7
		[HideInInspector]_OffsetFixedRoots("Offset Fixed Roots", Range( 0 , 1)) = 1
		[HideInInspector]_GravityMultiplier("Gravity Multiplier", Range( 0 , 10)) = 2
		[HideInInspector]_GravityFixedRoots("Gravity Fixed Roots", Range( 0 , 1)) = 1
		[HideInInspector]_GravityGradientStrength("Gravity Gradient Strength", Range( 0 , 1)) = 0.7
		[HideInInspector]_Gravity("Gravity", Vector) = (0,-1,0,0)
		[HideInInspector]Player_Position("Player_Position", Vector) = (0,0,0,0)
		_Running("Running", Int) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" }
		Cull Off
		AlphaToMask On
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "UnityShaderVariables.cginc"
		#include "UnityCG.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldPos;
			float4 screenPosition;
			float2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
		};

		struct SurfaceOutputCustomLightingCustom
		{
			half3 Albedo;
			half3 Normal;
			half3 Emission;
			half Metallic;
			half Smoothness;
			half Occlusion;
			half Alpha;
			Input SurfInput;
			UnityGIInput GIData;
		};

		uniform sampler2D _OffsetGradient;
		uniform float4 _OffsetGradient_ST;
		uniform float3 Player_Position;
		uniform sampler2D _GravityGradient;
		uniform float4 _GravityGradient_ST;
		uniform int _Running;
		uniform float _WindFrequency;
		uniform float _WindSizeHorizontal;
		uniform float _WindSizeVertical;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _MainTint;
		uniform sampler2D _ColorMask;
		uniform float4 _ColorMask_ST;
		uniform sampler2D _ToonMapTexture;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(Foliage)
			UNITY_DEFINE_INSTANCED_PROP(float3, _Gravity)
#define _Gravity_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _OffsetGradientStrength)
#define _OffsetGradientStrength_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _OffsetFixedRoots)
#define _OffsetFixedRoots_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _EffectRadius)
#define _EffectRadius_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _EffectClampMax)
#define _EffectClampMax_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _OffsetMultiplier)
#define _OffsetMultiplier_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _GravityMultiplier)
#define _GravityMultiplier_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _GravityGradientStrength)
#define _GravityGradientStrength_arr Foliage
			UNITY_DEFINE_INSTANCED_PROP(float, _GravityFixedRoots)
#define _GravityFixedRoots_arr Foliage
		UNITY_INSTANCING_BUFFER_END(Foliage)


		inline float Dither4x4Bayer( int x, int y )
		{
			const float dither[ 16 ] = {
				 1,  9,  3, 11,
				13,  5, 15,  7,
				 4, 12,  2, 10,
				16,  8, 14,  6 };
			int r = y * 4 + x;
			return dither[r] / 16; // same # of instructions as pre-dividing due to compiler magic
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			//Calculate new billboard vertex position and normal;
			float3 upCamVec = normalize ( UNITY_MATRIX_V._m10_m11_m12 );
			float3 forwardCamVec = -normalize ( UNITY_MATRIX_V._m20_m21_m22 );
			float3 rightCamVec = normalize( UNITY_MATRIX_V._m00_m01_m02 );
			float4x4 rotationCamMatrix = float4x4( rightCamVec, 0, upCamVec, 0, forwardCamVec, 0, 0, 0, 0, 1 );
			v.normal = normalize( mul( float4( v.normal , 0 ), rotationCamMatrix ));
			v.vertex.x *= length( unity_ObjectToWorld._m00_m10_m20 );
			v.vertex.y *= length( unity_ObjectToWorld._m01_m11_m21 );
			v.vertex.z *= length( unity_ObjectToWorld._m02_m12_m22 );
			v.vertex = mul( v.vertex, rotationCamMatrix );
			v.vertex.xyz += unity_ObjectToWorld._m03_m13_m23;
			//Need to nullify rotation inserted by generated surface shader;
			v.vertex = mul( unity_WorldToObject, v.vertex );
			float4 temp_cast_0 = (v.texcoord.xy.y).xxxx;
			float2 uv_OffsetGradient = v.texcoord * _OffsetGradient_ST.xy + _OffsetGradient_ST.zw;
			float4 temp_cast_1 = 1;
			float _OffsetGradientStrength_Instance = UNITY_ACCESS_INSTANCED_PROP(_OffsetGradientStrength_arr, _OffsetGradientStrength);
			float4 lerpResult171 = lerp( (tex2Dlod( _OffsetGradient, float4( uv_OffsetGradient, 0, 0.0) )).rgba , temp_cast_1 , ( 1.0 - _OffsetGradientStrength_Instance ));
			float4 blendOpSrc177 = temp_cast_0;
			float4 blendOpDest177 = lerpResult171;
			float4 temp_cast_2 = 1;
			float _OffsetFixedRoots_Instance = UNITY_ACCESS_INSTANCED_PROP(_OffsetFixedRoots_arr, _OffsetFixedRoots);
			float4 lerpResult181 = lerp( ( saturate( ( blendOpSrc177 * blendOpDest177 ) )) , temp_cast_2 , ( 1.0 - _OffsetFixedRoots_Instance ));
			float4 XZOffset185 = lerpResult181;
			float _EffectRadius_Instance = UNITY_ACCESS_INSTANCED_PROP(_EffectRadius_arr, _EffectRadius);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 break125 = Player_Position;
			float WorldY140 = ase_worldPos.y;
			float PlayerY128 = break125.y;
			float PlayerTopY137 = ( PlayerY128 + 0.0 );
			float PlayerBottomY133 = ( PlayerY128 + -1.0 );
			float VertexY127 = ase_worldPos.y;
			float RecalculatedY149 = (( WorldY140 < PlayerTopY137 ) ? PlayerTopY137 :  (( WorldY140 < PlayerBottomY133 ) ? PlayerBottomY133 :  VertexY127 ) );
			float4 appendResult151 = (float4(break125.x , RecalculatedY149 , break125.z , 0.0));
			float _EffectClampMax_Instance = UNITY_ACCESS_INSTANCED_PROP(_EffectClampMax_arr, _EffectClampMax);
			float clampResult156 = clamp( ( _EffectRadius_Instance - distance( float4( ase_worldPos , 0.0 ) , appendResult151 ) ) , 0.0 , _EffectClampMax_Instance );
			float3 break159 = Player_Position;
			float4 appendResult161 = (float4(break159.x , ase_worldPos.y , break159.z , 0.0));
			float4 normalizeResult163 = normalize( ( float4( ase_worldPos , 0.0 ) - appendResult161 ) );
			float _OffsetMultiplier_Instance = UNITY_ACCESS_INSTANCED_PROP(_OffsetMultiplier_arr, _OffsetMultiplier);
			float XZMultiplier188 = _OffsetMultiplier_Instance;
			float3 _Gravity_Instance = UNITY_ACCESS_INSTANCED_PROP(_Gravity_arr, _Gravity);
			float3 normalizeResult214 = normalize( -_Gravity_Instance );
			float _GravityMultiplier_Instance = UNITY_ACCESS_INSTANCED_PROP(_GravityMultiplier_arr, _GravityMultiplier);
			float3 YMultiplier216 = ( normalizeResult214 * _GravityMultiplier_Instance );
			float4 temp_cast_7 = (v.texcoord.xy.y).xxxx;
			float2 uv_GravityGradient = v.texcoord * _GravityGradient_ST.xy + _GravityGradient_ST.zw;
			float4 temp_cast_8 = 1;
			float _GravityGradientStrength_Instance = UNITY_ACCESS_INSTANCED_PROP(_GravityGradientStrength_arr, _GravityGradientStrength);
			float4 lerpResult203 = lerp( (tex2Dlod( _GravityGradient, float4( uv_GravityGradient, 0, 0.0) )).rgba , temp_cast_8 , ( 1.0 - _GravityGradientStrength_Instance ));
			float4 blendOpSrc205 = temp_cast_7;
			float4 blendOpDest205 = lerpResult203;
			float4 temp_cast_9 = 1;
			float _GravityFixedRoots_Instance = UNITY_ACCESS_INSTANCED_PROP(_GravityFixedRoots_arr, _GravityFixedRoots);
			float4 lerpResult207 = lerp( ( saturate( ( blendOpSrc205 * blendOpDest205 ) )) , temp_cast_9 , ( 1.0 - _GravityFixedRoots_Instance ));
			float4 YOffset217 = lerpResult207;
			float4 GrassBendEffects191 = ( ( XZOffset185 * ( clampResult156 * normalizeResult163 ) * XZMultiplier188 ) + ( float4( YMultiplier216 , 0.0 ) * YOffset217 * -clampResult156 ) );
			float temp_output_266_0 = ( _Running * _Time.y );
			float temp_output_98_0 = sin( ( ( ase_worldPos.x + temp_output_266_0 + ase_worldPos.z ) * 0.5 ) );
			float lerpResult94 = lerp( cos( ( ( v.texcoord.xy.y + temp_output_266_0 ) / ( 1.0 - _WindFrequency ) ) ) , 0.0 , ( 1.0 - v.texcoord.xy.y ));
			float temp_output_107_0 = ( ( temp_output_98_0 * lerpResult94 ) * (-0.5 + (( 1.0 - _WindSizeHorizontal ) - 0.0) * (0.0 - -0.5) / (1.0 - 0.0)) );
			float clampResult102 = clamp( ( 1.0 - v.texcoord.xy.y ) , 0.0 , 1.0 );
			float lerpResult104 = lerp( temp_output_98_0 , 0.0 , clampResult102);
			float4 appendResult109 = (float4(temp_output_107_0 , ( _WindSizeVertical * lerpResult104 ) , temp_output_107_0 , 0.0));
			float4 WindEffects84 = ( v.color.r * appendResult109 * v.color.b );
			float3 ase_vertex3Pos = v.vertex.xyz;
			v.vertex.xyz += ( ( GrassBendEffects191 + WindEffects84 ) + float4( ( 0 + ( ase_vertex3Pos * 0 ) ) , 0.0 ) ).rgb;
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		inline half4 LightingStandardCustomLighting( inout SurfaceOutputCustomLightingCustom s, half3 viewDir, UnityGI gi )
		{
			UnityGIInput data = s.GIData;
			Input i = s.SurfInput;
			half4 c = 0;
			#ifdef UNITY_PASS_FORWARDBASE
			float ase_lightAtten = data.atten;
			if( _LightColor0.a == 0)
			ase_lightAtten = 0;
			#else
			float3 ase_lightAttenRGB = gi.light.color / ( ( _LightColor0.rgb ) + 0.000001 );
			float ase_lightAtten = max( max( ase_lightAttenRGB.r, ase_lightAttenRGB.g ), ase_lightAttenRGB.b );
			#endif
			#if defined(HANDLE_SHADOWS_BLENDING_IN_GI)
			half bakedAtten = UnitySampleBakedOcclusion(data.lightmapUV.xy, data.worldPos);
			float zDist = dot(_WorldSpaceCameraPos - data.worldPos, UNITY_MATRIX_V[2].xyz);
			float fadeDist = UnityComputeShadowFadeDistance(data.worldPos, zDist);
			ase_lightAtten = UnityMixRealtimeAndBakedShadows(data.atten, bakedAtten, UnityComputeShadowFade(fadeDist));
			#endif
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 ditherCustomScreenPos263 = ase_screenPosNorm;
			float2 clipScreen263 = ditherCustomScreenPos263.xy * _ScreenParams.xy;
			float dither263 = Dither4x4Bayer( fmod(clipScreen263.x, 4), fmod(clipScreen263.y, 4) );
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode247 = tex2D( _Texture, uv_Texture );
			dither263 = step( dither263, tex2DNode247.a );
			float2 uv_ColorMask = i.uv_texcoord * _ColorMask_ST.xy + _ColorMask_ST.zw;
			float4 lerpResult258 = lerp( tex2DNode247 , ( tex2DNode247 * _MainTint ) , ( tex2D( _ColorMask, uv_ColorMask ) * 1.0 ));
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldPos = i.worldPos;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aseld
			float3 ase_worldlightDir = 0;
			#else //aseld
			float3 ase_worldlightDir = normalize( UnityWorldSpaceLightDir( ase_worldPos ) );
			#endif //aseld
			float dotResult245 = dot( ase_worldNormal , ase_worldlightDir );
			float2 temp_cast_0 = (saturate( (dotResult245*0.5 + 0.5) )).xx;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			UnityGI gi257 = gi;
			float3 diffNorm257 = WorldNormalVector( i , ase_lightColor.rgb );
			gi257 = UnityGI_Base( data, 1, diffNorm257 );
			float3 indirectDiffuse257 = gi257.indirect.diffuse + diffNorm257 * 0.0001;
			c.rgb = ( ( lerpResult258 * tex2D( _ToonMapTexture, temp_cast_0 ) ) * float4( ( indirectDiffuse257 * ( ase_lightColor.rgb + ase_lightAtten ) ) , 0.0 ) ).rgb;
			c.a = 1;
			clip( dither263 - _Cutoff );
			return c;
		}

		inline void LightingStandardCustomLighting_GI( inout SurfaceOutputCustomLightingCustom s, UnityGIInput data, inout UnityGI gi )
		{
			s.GIData = data;
		}

		void surf( Input i , inout SurfaceOutputCustomLightingCustom o )
		{
			o.SurfInput = i;
			o.Normal = float3(0,0,1);
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			AlphaToMask Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float4 customPack1 : TEXCOORD1;
				float2 customPack2 : TEXCOORD2;
				float4 tSpace0 : TEXCOORD3;
				float4 tSpace1 : TEXCOORD4;
				float4 tSpace2 : TEXCOORD5;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xyzw = customInputData.screenPosition;
				o.customPack2.xy = customInputData.uv_texcoord;
				o.customPack2.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.screenPosition = IN.customPack1.xyzw;
				surfIN.uv_texcoord = IN.customPack2.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputCustomLightingCustom o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputCustomLightingCustom, o )
				surf( surfIN, o );
				UnityGI gi;
				UNITY_INITIALIZE_OUTPUT( UnityGI, gi );
				o.Alpha = LightingStandardCustomLighting( o, worldViewDir, gi ).a;
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
238;215;1618;907;1988.25;1135.533;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;232;-6233.055,-3780.54;Float;False;6436.089;2644.614;;11;191;190;185;157;189;164;219;210;138;150;225;Grass Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;150;-5027.747,-2955.844;Float;False;1783.404;563.9719;;11;155;156;125;154;152;129;153;128;127;151;226;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;225;-5418.806,-2293.102;Float;False;Property;Player_Position;Player_Position;19;1;[HideInInspector];Create;False;0;0;False;0;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;125;-4973.352,-2626.204;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode;138;-6133.804,-3666.076;Float;False;1928.392;685.9498;Set the height of Player Object;11;133;131;130;132;137;136;135;134;140;141;139;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;128;-4684.869,-2684.655;Float;False;PlayerY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;131;-6085.706,-3510.925;Float;False;Constant;_EffectBottomOffset;Effect Bottom Offset;10;1;[HideInInspector];Create;True;0;0;False;0;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;130;-6036.877,-3592.927;Float;False;128;PlayerY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;132;-5818.838,-3568.583;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;226;-4946.417,-2789.159;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;135;-6066.311,-3306.308;Float;False;Constant;_EffectTopOffset;Effect Top Offset;8;1;[HideInInspector];Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;-6040.198,-3403.353;Float;False;128;PlayerY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;139;-6052.021,-3179.933;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;141;-5384.211,-3526.137;Float;False;1146.685;514.6001;Recalculated Y;8;148;147;146;145;144;143;142;149;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;133;-5659.238,-3574.885;Float;False;PlayerBottomY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;127;-4687.267,-2841.075;Float;False;VertexY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;140;-5777.051,-3140.109;Float;False;WorldY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;136;-5815.436,-3359.835;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;144;-5324.214,-3149.736;Float;False;127;VertexY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;143;-5322.913,-3240.736;Float;False;133;PlayerBottomY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-5321.619,-3327.835;Float;False;140;WorldY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;137;-5654.807,-3365.781;Float;False;PlayerTopY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;146;-4999.494,-3431.31;Float;False;140;WorldY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;113;-4783.47,-941.5466;Float;False;3072.81;1248.075;;31;84;110;112;109;107;108;105;106;89;85;91;86;87;88;90;94;99;97;101;100;98;96;92;93;102;104;117;118;119;265;266;Wind Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.GetLocalVarNode;147;-5001.174,-3350.029;Float;False;137;PlayerTopY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;145;-5022.615,-3243.336;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;210;-4167.547,-3586.704;Float;False;2110.774;598.0815;Makes grass bend AWAY;2;173;172;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;219;-3160.623,-2198.309;Float;False;2585.829;913.2986;Makes grass bend DOWN;11;217;218;216;215;213;214;211;212;194;195;196;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;91;-4710.268,-279.2605;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;265;-4628.436,-405.619;Float;False;Property;_Running;Running;20;0;Create;True;0;0;False;0;0;1;0;1;INT;0
Node;AmplifyShaderEditor.TFHCCompareLower;148;-4764.613,-3426.621;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;195;-3080.889,-1824.552;Float;False;911.1997;490;Control grass bend based off gradient;6;209;203;200;199;198;197;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;89;-4394.009,159.4009;Float;False;Property;_WindFrequency;Wind Frequency;4;0;Create;True;0;0;False;0;0.5;0.75;0;0.75;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;172;-4129.649,-3520.843;Float;False;911.1997;490;Control grass bend based off gradient;6;165;171;167;168;166;184;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;266;-4442.919,-316.1348;Float;False;2;2;0;INT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;85;-4670.744,-51.35825;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-4493.151,-3432.655;Float;False;RecalculatedY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;164;-4268.693,-2357.473;Float;False;1016.84;470.1794;;5;163;162;161;159;227;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;196;-2144.964,-1822.278;Float;False;1096.607;490.4404;Hold grass roots in place;7;208;207;206;205;204;202;201;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;92;-4350.721,-484.4692;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;197;-2935.61,-1468.756;Float;False;InstancedProperty;_GravityGradientStrength;Gravity Gradient Strength;17;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;119;-4259.044,-27.77621;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;167;-3984.372,-3165.047;Float;False;InstancedProperty;_OffsetGradientStrength;Offset Gradient Strength;13;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;87;-4218.638,-208.6135;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;173;-3193.731,-3521.76;Float;False;1096.607;490.4404;Hold grass roots in place;7;178;179;181;177;174;176;183;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;209;-3040.291,-1757.035;Float;True;Property;_GravityGradient;Gravity Gradient;10;1;[HideInInspector];Create;True;0;0;True;0;59f147a4bcbaa27478ddf280be6fbf9c;59f147a4bcbaa27478ddf280be6fbf9c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;165;-4084.139,-3456.364;Float;True;Property;_OffsetGradient;Offset Gradient;9;1;[HideInInspector];Create;True;0;0;True;0;59f147a4bcbaa27478ddf280be6fbf9c;59f147a4bcbaa27478ddf280be6fbf9c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;129;-4439.868,-2685.655;Float;False;149;RecalculatedY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;211;-1851.264,-2092.271;Float;False;InstancedProperty;_Gravity;Gravity;18;1;[HideInInspector];Create;True;0;0;False;0;0,-1,0;0,-1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IntNode;200;-2624.517,-1545.206;Float;False;Constant;_Int3;Int 3;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.ComponentMaskNode;166;-3779.106,-3453.598;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldPosInputsNode;227;-4161.548,-2275.851;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;151;-4205.991,-2628.134;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;174;-3126.269,-3207.653;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode;199;-2730.342,-1757.306;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;198;-2637.604,-1463.756;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;88;-4014.428,-207.4074;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;168;-3686.371,-3160.047;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;93;-4083.579,-461.6472;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-4141.089,-319.9624;Float;False;Constant;_WorldPositionVariationSpeed;World Position Variation Speed;3;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;100;-4190.359,-828.2092;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;184;-3673.284,-3241.497;Float;False;Constant;_Int0;Int 0;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;159;-4204.071,-2090.207;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.TexCoordVertexDataNode;201;-2107.326,-1747.785;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NegateNode;212;-1649.034,-2086.682;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;208;-1881.78,-1759.46;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;86;-3746.645,40.78728;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;161;-3869.396,-2089.825;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;178;-2877.417,-3126.848;Float;False;InstancedProperty;_OffsetFixedRoots;Offset Fixed Roots;14;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;171;-3481.12,-3281.618;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;203;-2432.355,-1585.328;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;152;-4107.533,-2878.352;Float;False;InstancedProperty;_EffectRadius;Effect Radius;5;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;101;-3818.335,-792.5682;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;96;-3732.892,-494.1621;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;108;-3162.149,3.805252;Float;False;Property;_WindSizeHorizontal;Wind Size Horizontal;3;0;Create;True;0;0;False;0;0.25;0.25;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;153;-4037.7,-2784.303;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;90;-3747.612,-208.2575;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;176;-2930.548,-3458.942;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;202;-1828.651,-1427.366;Float;False;InstancedProperty;_GravityFixedRoots;Gravity Fixed Roots;16;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;179;-2565.479,-3121.642;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;205;-1606.251,-1755.812;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;189;-1651.621,-2735.988;Float;False;810.2354;329.3519;Multiply offset with gradients;3;186;187;188;;1,1,1,1;0;0
Node;AmplifyShaderEditor.BlendOpsNode;177;-2655.017,-3455.294;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode;183;-2548.865,-3215.077;Float;False;Constant;_One;One;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.OneMinusNode;204;-1516.711,-1422.16;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;155;-3760.784,-2631.715;Float;False;InstancedProperty;_EffectClampMax;Effect Clamp Max;12;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;206;-1500.097,-1515.595;Float;False;Constant;_Int4;Int 4;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.SinOpNode;98;-3510.073,-493.9763;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;214;-1498.419,-2086.183;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;118;-3173.776,-159.4913;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;102;-3577.617,-780.1473;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;94;-3512.174,-207.9655;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;213;-1616.005,-2000.297;Float;False;InstancedProperty;_GravityMultiplier;Gravity Multiplier;15;1;[HideInInspector];Create;True;0;0;False;0;2;2;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;154;-3765.28,-2746.197;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;162;-3663.154,-2158.241;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;-1272.646,-2019.386;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;181;-2354.545,-3339.963;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;156;-3435.353,-2708.736;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-3162.887,-451.3542;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;105;-3168.426,-798.0044;Float;False;Property;_WindSizeVertical;Wind Size Vertical;6;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-1614.965,-2549.419;Float;False;InstancedProperty;_OffsetMultiplier;Offset Multiplier;11;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;104;-3161.011,-673.7489;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;117;-2982.466,-190.9905;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;207;-1305.778,-1640.48;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;157;-2551.448,-2721.528;Float;False;250.1355;306.0909;Offset each vertex;1;158;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;241;-1562.965,-976.3568;Float;False;1773.812;1275.697;;23;264;263;262;261;260;259;258;257;256;255;254;253;252;251;250;249;248;247;246;245;244;243;242;Texture + Transparency;1,1,1,1;0;0
Node;AmplifyShaderEditor.NormalizeNode;163;-3456.144,-2158.949;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;217;-988.1025,-1646.261;Float;False;YOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;242;-1463.443,-472.7657;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;188;-1331.5,-2548.757;Float;False;XZMultiplier;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;194;-2088.355,-1919.775;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;107;-2793.87,-460.4563;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;-2522.992,-2658.75;Float;True;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;185;-1683.594,-3022.808;Float;False;XZOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;106;-2797.69,-695.8672;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;243;-1510.238,-321.2125;Float;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;216;-1074.11,-2026.213;Float;False;YMultiplier;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.VertexColorNode;110;-2490.431,-821.9163;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DotProductOpNode;245;-1241.804,-309.827;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;109;-2504.439,-541.8323;Float;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;186;-1056.822,-2679.146;Float;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;244;-1311.382,-137.8437;Float;False;Constant;_WrapperValue;Wrapper Value;0;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;-740.3022,-1968.931;Float;False;3;3;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;247;-834.5874,-912.3926;Float;True;Property;_Texture;Texture;1;0;Create;True;0;0;False;0;None;d30aaa9ddee34e140a5d518211cf0d50;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;250;-1053.292,-277.8493;Float;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;248;-1139.968,-671.0569;Float;True;Property;_ColorMask;Color Mask;8;1;[HideInInspector];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;249;-749.1332,-709.3977;Float;False;Property;_MainTint;MainTint;2;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;240;-531.6514,447.7197;Float;False;788;525;;5;233;234;235;237;236;Custom Billboarding;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;190;-409.5006,-2518.378;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;112;-2209.089,-798.2463;Float;True;3;3;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;246;-998.7945,-464.7763;Float;False;Constant;_Float3;Float 3;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LightAttenuation;251;-1347.945,161.3846;Float;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;252;-805.7816,-216.7476;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;255;-414.101,-708.0443;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;121;-1305.761,533.0539;Float;False;615;312;;3;77;120;78;Combine Wind + Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;254;-357.4094,-555.6599;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PosVertexDataNode;236;-466.3406,794.4448;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-147.5654,-2523.229;Float;False;GrassBendEffects;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-1951.745,-736.5511;Float;False;WindEffects;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LightColorNode;253;-1308.346,11.38481;Float;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.GetLocalVarNode;78;-1211.845,726.3403;Float;False;84;WindEffects;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ScaleNode;237;-257.7958,794.3266;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;120;-1213.251,612.0392;Float;False;191;GrassBendEffects;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;256;-868.7502,140.8846;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;258;-196.7928,-724.0465;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;257;-1111.848,28.78488;Float;False;Tangent;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;259;-584.3416,-220.3863;Float;True;Property;_ToonMapTexture;Toon Map Texture;0;1;[HideInInspector];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BillboardNode;235;-294.3408,676.4446;Float;False;Spherical;True;0;1;FLOAT3;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;260;-357.6615,83.34138;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;262;-167.3136,-373.316;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;234;-57.38946,681.6248;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;77;-898.4202,648.7105;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;261;-764.3502,11.38481;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;264;-4.108109,-165.5119;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;233;111.9557,568.1861;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;263;-83.97927,60.72985;Float;False;0;True;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;575.4599,-200.3809;Float;False;True;2;Float;ASEMaterialInspector;0;0;CustomLighting;Foliage;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;4;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;7;-1;-1;-1;0;True;0;0;False;-1;-1;0;False;-1;0;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;125;0;225;0
WireConnection;128;0;125;1
WireConnection;132;0;130;0
WireConnection;132;1;131;0
WireConnection;133;0;132;0
WireConnection;127;0;226;2
WireConnection;140;0;139;2
WireConnection;136;0;134;0
WireConnection;136;1;135;0
WireConnection;137;0;136;0
WireConnection;145;0;142;0
WireConnection;145;1;143;0
WireConnection;145;2;143;0
WireConnection;145;3;144;0
WireConnection;148;0;146;0
WireConnection;148;1;147;0
WireConnection;148;2;147;0
WireConnection;148;3;145;0
WireConnection;266;0;265;0
WireConnection;266;1;91;2
WireConnection;149;0;148;0
WireConnection;119;0;89;0
WireConnection;87;0;85;2
WireConnection;87;1;266;0
WireConnection;166;0;165;0
WireConnection;151;0;125;0
WireConnection;151;1;129;0
WireConnection;151;2;125;2
WireConnection;199;0;209;0
WireConnection;198;0;197;0
WireConnection;88;0;87;0
WireConnection;88;1;119;0
WireConnection;168;0;167;0
WireConnection;93;0;92;1
WireConnection;93;1;266;0
WireConnection;93;2;92;3
WireConnection;159;0;225;0
WireConnection;212;0;211;0
WireConnection;208;0;201;2
WireConnection;86;0;85;2
WireConnection;161;0;159;0
WireConnection;161;1;227;2
WireConnection;161;2;159;2
WireConnection;171;0;166;0
WireConnection;171;1;184;0
WireConnection;171;2;168;0
WireConnection;203;0;199;0
WireConnection;203;1;200;0
WireConnection;203;2;198;0
WireConnection;101;0;100;2
WireConnection;96;0;93;0
WireConnection;96;1;97;0
WireConnection;153;0;226;0
WireConnection;153;1;151;0
WireConnection;90;0;88;0
WireConnection;176;0;174;2
WireConnection;179;0;178;0
WireConnection;205;0;208;0
WireConnection;205;1;203;0
WireConnection;177;0;176;0
WireConnection;177;1;171;0
WireConnection;204;0;202;0
WireConnection;98;0;96;0
WireConnection;214;0;212;0
WireConnection;118;0;108;0
WireConnection;102;0;101;0
WireConnection;94;0;90;0
WireConnection;94;2;86;0
WireConnection;154;0;152;0
WireConnection;154;1;153;0
WireConnection;162;0;227;0
WireConnection;162;1;161;0
WireConnection;215;0;214;0
WireConnection;215;1;213;0
WireConnection;181;0;177;0
WireConnection;181;1;183;0
WireConnection;181;2;179;0
WireConnection;156;0;154;0
WireConnection;156;2;155;0
WireConnection;99;0;98;0
WireConnection;99;1;94;0
WireConnection;104;0;98;0
WireConnection;104;2;102;0
WireConnection;117;0;118;0
WireConnection;207;0;205;0
WireConnection;207;1;206;0
WireConnection;207;2;204;0
WireConnection;163;0;162;0
WireConnection;217;0;207;0
WireConnection;188;0;187;0
WireConnection;194;0;156;0
WireConnection;107;0;99;0
WireConnection;107;1;117;0
WireConnection;158;0;156;0
WireConnection;158;1;163;0
WireConnection;185;0;181;0
WireConnection;106;0;105;0
WireConnection;106;1;104;0
WireConnection;216;0;215;0
WireConnection;245;0;242;0
WireConnection;245;1;243;0
WireConnection;109;0;107;0
WireConnection;109;1;106;0
WireConnection;109;2;107;0
WireConnection;186;0;185;0
WireConnection;186;1;158;0
WireConnection;186;2;188;0
WireConnection;218;0;216;0
WireConnection;218;1;217;0
WireConnection;218;2;194;0
WireConnection;250;0;245;0
WireConnection;250;1;244;0
WireConnection;250;2;244;0
WireConnection;190;0;186;0
WireConnection;190;1;218;0
WireConnection;112;0;110;1
WireConnection;112;1;109;0
WireConnection;112;2;110;3
WireConnection;252;0;250;0
WireConnection;255;0;247;0
WireConnection;255;1;249;0
WireConnection;254;0;248;0
WireConnection;254;1;246;0
WireConnection;191;0;190;0
WireConnection;84;0;112;0
WireConnection;237;0;236;0
WireConnection;256;0;253;1
WireConnection;256;1;251;0
WireConnection;258;0;247;0
WireConnection;258;1;255;0
WireConnection;258;2;254;0
WireConnection;257;0;253;0
WireConnection;259;1;252;0
WireConnection;262;0;258;0
WireConnection;262;1;259;0
WireConnection;234;0;235;0
WireConnection;234;1;237;0
WireConnection;77;0;120;0
WireConnection;77;1;78;0
WireConnection;261;0;257;0
WireConnection;261;1;256;0
WireConnection;264;0;262;0
WireConnection;264;1;261;0
WireConnection;233;0;77;0
WireConnection;233;1;234;0
WireConnection;263;0;247;4
WireConnection;263;2;260;0
WireConnection;0;10;263;0
WireConnection;0;13;264;0
WireConnection;0;11;233;0
ASEEND*/
//CHKSM=1EF7E803E2C29628949718BD10A143954AEFF4A1