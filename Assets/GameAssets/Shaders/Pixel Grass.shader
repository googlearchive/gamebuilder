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

// Upgrade NOTE: upgraded instancing buffer 'SullyPixelGrass' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Sully/Pixel Grass"
{
	Properties
	{
		_Texture("Texture", 2D) = "white" {}
		_ColorMask("Color Mask", 2D) = "white" {}
		_MainTint("MainTint", Color) = (1,1,1,0)
		_GrassWindMask("Grass Wind Mask", 2D) = "white" {}
		_WindHighlightSize("Wind Highlight Size", Float) = 2000
		_WindHighlightIntensity("Wind Highlight Intensity", Float) = 0.5
		_WindHighlightTint("Wind Highlight Tint", Color) = (0.4470589,0.764706,0.2431373,1)
		_WindSpeed("Wind Speed", Float) = 0
		_WindOffsetIntensity("Wind Offset Intensity", Float) = 0
		_BendRadius("Bend Radius", Range( 0 , 10)) = 1
		[HideInInspector]_OffsetGradient("Offset Gradient", 2D) = "white" {}
		[HideInInspector]_GravityGradient("Gravity Gradient", 2D) = "white" {}
		[HideInInspector]_EffectClampMax("Effect Clamp Max", Range( 0 , 10)) = 1
		[HideInInspector]_OffsetFixedRoots("Offset Fixed Roots", Range( 0 , 1)) = 1
		[HideInInspector]_Gravity("Gravity", Vector) = (0,-1,0,0)
		[HideInInspector]Player_Position("Player_Position", Vector) = (0,0,0,0)
		[HideInInspector]_ToonMapTexture("Toon Map Texture", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IsEmissive" = "true"  }
		Cull Front
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
		uniform sampler2D _GrassWindMask;
		uniform float _WindSpeed;
		uniform float _WindHighlightSize;
		uniform float _WindOffsetIntensity;
		uniform float4 _WindHighlightTint;
		uniform float _WindHighlightIntensity;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _MainTint;
		uniform sampler2D _ColorMask;
		uniform float4 _ColorMask_ST;
		uniform sampler2D _ToonMapTexture;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(SullyPixelGrass)
			UNITY_DEFINE_INSTANCED_PROP(float3, _Gravity)
#define _Gravity_arr SullyPixelGrass
			UNITY_DEFINE_INSTANCED_PROP(float, _OffsetFixedRoots)
#define _OffsetFixedRoots_arr SullyPixelGrass
			UNITY_DEFINE_INSTANCED_PROP(float, _BendRadius)
#define _BendRadius_arr SullyPixelGrass
			UNITY_DEFINE_INSTANCED_PROP(float, _EffectClampMax)
#define _EffectClampMax_arr SullyPixelGrass
		UNITY_INSTANCING_BUFFER_END(SullyPixelGrass)


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
			float4 lerpResult171 = lerp( (tex2Dlod( _OffsetGradient, float4( uv_OffsetGradient, 0, 0.0) )).rgba , temp_cast_1 , ( 1.0 - 0.0 ));
			float4 blendOpSrc177 = temp_cast_0;
			float4 blendOpDest177 = lerpResult171;
			float4 temp_cast_2 = 1;
			float _OffsetFixedRoots_Instance = UNITY_ACCESS_INSTANCED_PROP(_OffsetFixedRoots_arr, _OffsetFixedRoots);
			float4 lerpResult181 = lerp( ( saturate( ( blendOpSrc177 * blendOpDest177 ) )) , temp_cast_2 , ( 1.0 - _OffsetFixedRoots_Instance ));
			float4 XZOffset185 = lerpResult181;
			float _BendRadius_Instance = UNITY_ACCESS_INSTANCED_PROP(_BendRadius_arr, _BendRadius);
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
			float clampResult156 = clamp( ( _BendRadius_Instance - distance( float4( ase_worldPos , 0.0 ) , appendResult151 ) ) , 0.0 , _EffectClampMax_Instance );
			float3 break159 = Player_Position;
			float4 appendResult161 = (float4(break159.x , ase_worldPos.y , break159.z , 0.0));
			float4 normalizeResult163 = normalize( ( float4( ase_worldPos , 0.0 ) - appendResult161 ) );
			float XZMultiplier188 = 1.0;
			float3 _Gravity_Instance = UNITY_ACCESS_INSTANCED_PROP(_Gravity_arr, _Gravity);
			float3 normalizeResult214 = normalize( -_Gravity_Instance );
			float3 YMultiplier216 = ( normalizeResult214 * 2.0 );
			float4 temp_cast_7 = (v.texcoord.xy.y).xxxx;
			float2 uv_GravityGradient = v.texcoord * _GravityGradient_ST.xy + _GravityGradient_ST.zw;
			float4 temp_cast_8 = 1;
			float4 lerpResult203 = lerp( (tex2Dlod( _GravityGradient, float4( uv_GravityGradient, 0, 0.0) )).rgba , temp_cast_8 , ( 1.0 - 0.5 ));
			float4 blendOpSrc205 = temp_cast_7;
			float4 blendOpDest205 = lerpResult203;
			float4 temp_cast_9 = 1;
			float4 lerpResult207 = lerp( ( saturate( ( blendOpSrc205 * blendOpDest205 ) )) , temp_cast_9 , ( 1.0 - 1.0 ));
			float4 YOffset217 = lerpResult207;
			float4 GrassBendEffects191 = ( ( XZOffset185 * ( clampResult156 * normalizeResult163 ) * XZMultiplier188 ) + ( float4( YMultiplier216 , 0.0 ) * YOffset217 * -clampResult156 ) );
			float2 panner406 = ( 1.0 * _Time.y * ( float2( -0.02,0.01 ) * _WindSpeed ) + (( ase_worldPos / _WindHighlightSize )).xy);
			float4 tex2DNode408 = tex2Dlod( _GrassWindMask, float4( panner406, 0, 0.0) );
			float4 lerpResult412 = lerp( float4( 0,0,0,0 ) , ( tex2DNode408 * float4( ( float3(1,0.25,0.75) * _WindOffsetIntensity ) , 0.0 ) ) , pow( v.texcoord.xy.y , 1.0 ));
			float4 WindOffset443 = lerpResult412;
			float3 ase_vertex3Pos = v.vertex.xyz;
			v.vertex.xyz += ( ( GrassBendEffects191 + WindOffset443 ) + float4( ( 0 + ( ase_vertex3Pos * 0 ) ) , 0.0 ) ).rgb;
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
			float2 temp_cast_1 = (saturate( (dotResult245*0.5 + 0.5) )).xx;
			#if defined(LIGHTMAP_ON) && UNITY_VERSION < 560 //aselc
			float4 ase_lightColor = 0;
			#else //aselc
			float4 ase_lightColor = _LightColor0;
			#endif //aselc
			UnityGI gi257 = gi;
			float3 diffNorm257 = WorldNormalVector( i , ase_lightColor.rgb );
			gi257 = UnityGI_Base( data, 1, diffNorm257 );
			float3 indirectDiffuse257 = gi257.indirect.diffuse + diffNorm257 * 0.0001;
			c.rgb = ( ( lerpResult258 * tex2D( _ToonMapTexture, temp_cast_1 ) ) * float4( ( indirectDiffuse257 * ( ase_lightColor.rgb + ase_lightAtten ) ) , 0.0 ) ).rgb;
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
			float3 ase_worldPos = i.worldPos;
			float2 panner406 = ( 1.0 * _Time.y * ( float2( -0.02,0.01 ) * _WindSpeed ) + (( ase_worldPos / _WindHighlightSize )).xy);
			float4 tex2DNode408 = tex2D( _GrassWindMask, panner406 );
			float4 GrassWindMask439 = tex2DNode408;
			float4 blendOpSrc436 = _WindHighlightTint;
			float4 blendOpDest436 = (GrassWindMask439).rgba;
			float4 WindHighlights441 = ( ( saturate( ( 1.0 - ( ( 1.0 - blendOpDest436) / blendOpSrc436) ) )) * _WindHighlightIntensity );
			o.Emission = WindHighlights441.rgb;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf StandardCustomLighting keepalpha fullforwardshadows exclude_path:deferred noambient novertexlights nolightmap  nodynlightmap nodirlightmap nofog nometa noforwardadd vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
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
238;173;1618;949;4974.646;2679.535;3.40268;True;False
Node;AmplifyShaderEditor.CommentaryNode;232;-6590.464,-3463.746;Float;False;6153.069;2484.791;;11;191;190;185;225;157;189;164;219;210;138;150;Grass Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;225;-5836.934,-2021.846;Float;False;Property;Player_Position;Player_Position;15;1;[HideInInspector];Create;False;0;0;False;0;0,0,0;0,0.75,4.75;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;150;-5445.875,-2684.588;Float;False;1783.404;563.9719;;11;155;156;125;154;152;129;153;128;127;151;226;;1,1,1,1;0;0
Node;AmplifyShaderEditor.BreakToComponentsNode;125;-5391.48,-2354.948;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.CommentaryNode;138;-6551.932,-3394.82;Float;False;1928.392;685.9498;Set the height of Player Object;11;133;131;130;132;137;136;135;134;140;141;139;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;128;-5102.997,-2413.399;Float;False;PlayerY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;131;-6503.834,-3239.669;Float;False;Constant;_EffectBottomOffset;Effect Bottom Offset;10;0;Create;True;0;0;False;0;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;130;-6455.005,-3321.671;Float;False;128;PlayerY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;139;-6470.149,-2908.677;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;135;-6484.439,-3035.052;Float;False;Constant;_EffectTopOffset;Effect Top Offset;8;1;[HideInInspector];Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;132;-6236.966,-3297.327;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;134;-6458.327,-3132.097;Float;False;128;PlayerY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;226;-5364.545,-2517.903;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RegisterLocalVarNode;127;-5105.395,-2569.819;Float;False;VertexY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;133;-6077.365,-3303.629;Float;False;PlayerBottomY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;140;-6195.179,-2868.853;Float;False;WorldY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;136;-6233.563,-3088.579;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;141;-5802.339,-3254.881;Float;False;1146.685;514.6001;Recalculated Y;8;148;147;146;145;144;143;142;149;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;137;-6072.935,-3094.525;Float;False;PlayerTopY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;144;-5742.342,-2878.48;Float;False;127;VertexY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;142;-5739.747,-3056.579;Float;False;140;WorldY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;143;-5741.041,-2969.48;Float;False;133;PlayerBottomY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;145;-5440.743,-2972.08;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;147;-5419.302,-3078.773;Float;False;137;PlayerTopY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;146;-5417.622,-3160.054;Float;False;140;WorldY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;210;-4592.034,-3311.209;Float;False;2110.774;598.0815;Makes grass bend AWAY;2;173;172;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TFHCCompareLower;148;-5182.741,-3155.365;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;219;-3578.751,-1927.053;Float;False;2585.829;913.2986;Makes grass bend DOWN;11;217;218;216;215;213;214;211;212;194;195;196;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;149;-4911.279,-3161.399;Float;False;RecalculatedY;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;451;-2750.155,1184.558;Float;False;2289.483;709.3481;;19;443;412;414;410;439;423;408;448;421;406;409;447;445;407;446;398;438;396;397;Wind Offset;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;172;-4554.136,-3245.348;Float;False;911.1997;490;Control grass bend based off gradient;6;165;171;167;168;166;184;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;195;-3499.017,-1553.295;Float;False;911.1997;490;Control grass bend based off gradient;6;209;203;200;199;198;197;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;196;-2563.093,-1551.021;Float;False;1096.607;490.4404;Hold grass roots in place;7;208;207;206;205;204;202;201;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;167;-4408.858,-2889.552;Float;False;Constant;_OffsetGradientStrength;Offset Gradient Strength;8;0;Create;True;0;0;False;0;0;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;173;-3618.217,-3246.265;Float;False;1096.607;490.4404;Hold grass roots in place;7;178;179;181;177;174;176;183;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;197;-3353.738,-1197.5;Float;False;Constant;_GravityGradientStrength;Gravity Gradient Strength;18;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;129;-4857.997,-2414.399;Float;False;149;RecalculatedY;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;164;-4686.821,-2086.217;Float;False;1016.84;470.1794;;5;163;162;161;159;227;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;165;-4508.625,-3180.869;Float;True;Property;_OffsetGradient;Offset Gradient;10;1;[HideInInspector];Create;True;0;0;True;0;59f147a4bcbaa27478ddf280be6fbf9c;59f147a4bcbaa27478ddf280be6fbf9c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;396;-2720.877,1248.932;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;397;-2720.462,1405.363;Float;False;Property;_WindHighlightSize;Wind Highlight Size;4;0;Create;True;0;0;False;0;2000;500;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;209;-3458.419,-1485.779;Float;True;Property;_GravityGradient;Gravity Gradient;11;1;[HideInInspector];Create;True;0;0;True;0;59f147a4bcbaa27478ddf280be6fbf9c;59f147a4bcbaa27478ddf280be6fbf9c;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;446;-2477.534,1613.04;Float;False;Property;_WindSpeed;Wind Speed;7;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;211;-2269.393,-1821.015;Float;False;InstancedProperty;_Gravity;Gravity;14;1;[HideInInspector];Create;True;0;0;False;0;0,-1,0;0,-1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;151;-4624.12,-2356.878;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ComponentMaskNode;166;-4203.592,-3178.103;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;199;-3148.47,-1486.05;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;398;-2491.461,1312.362;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.IntNode;184;-4097.77,-2966.002;Float;False;Constant;_Int0;Int 0;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.Vector2Node;438;-2488.459,1483.302;Float;False;Constant;_SpeedXY;Speed XY;22;0;Create;True;0;0;False;0;-0.02,0.01;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.IntNode;200;-3042.645,-1273.949;Float;False;Constant;_Int3;Int 3;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;174;-3550.755,-2932.158;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;198;-3055.732,-1192.5;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;168;-4110.857,-2884.552;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;227;-4579.676,-2004.595;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.BreakToComponentsNode;159;-4622.199,-1818.951;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.TexCoordVertexDataNode;201;-2525.454,-1476.529;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.NegateNode;212;-2067.162,-1815.426;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ComponentMaskNode;176;-3355.034,-3183.447;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;203;-2850.483,-1314.072;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;152;-4521.662,-2595.096;Float;False;InstancedProperty;_BendRadius;Bend Radius;9;0;Create;True;0;0;False;0;1;1.4;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;202;-2246.78,-1156.109;Float;False;Constant;_GravityFixedRoots;Gravity Fixed Roots;13;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DistanceOpNode;153;-4455.828,-2513.047;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;171;-3905.606,-3006.123;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;161;-4287.524,-1818.569;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ComponentMaskNode;407;-2353.594,1307.273;Float;False;True;True;False;False;1;0;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ComponentMaskNode;208;-2299.908,-1488.203;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;178;-3301.903,-2851.353;Float;False;InstancedProperty;_OffsetFixedRoots;Offset Fixed Roots;13;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;445;-2266.034,1459.04;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.OneMinusNode;179;-2989.965,-2846.147;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;154;-4183.408,-2474.941;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;189;-2069.749,-2464.732;Float;False;810.2354;329.3519;Multiply offset with gradients;3;186;187;188;;1,1,1,1;0;0
Node;AmplifyShaderEditor.NormalizeNode;214;-1916.547,-1814.927;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;155;-4178.912,-2360.459;Float;False;InstancedProperty;_EffectClampMax;Effect Clamp Max;12;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;406;-2114.739,1346.823;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.BlendOpsNode;205;-2024.379,-1484.555;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendOpsNode;177;-3079.503,-3179.799;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode;206;-1918.225,-1244.338;Float;False;Constant;_Int4;Int 4;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;162;-4081.282,-1886.985;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;213;-2034.133,-1729.041;Float;False;Constant;_GravityMultiplier;Gravity Multiplier;14;0;Create;True;0;0;False;0;2;2;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;204;-1934.839,-1150.904;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;183;-2973.351,-2939.582;Float;False;Constant;_One;One;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.ClampOpNode;156;-3853.481,-2437.48;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;157;-2969.576,-2450.272;Float;False;250.1355;306.0909;Offset each vertex;1;158;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;409;-1920.346,1510.261;Float;False;Constant;_OffsetXYZ;Offset XYZ;18;0;Create;True;0;0;False;0;1,0.25,0.75;1,0.25,0.75;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;215;-1690.774,-1748.13;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;241;-2150.952,-826.0389;Float;False;1702.152;1254.086;;23;264;261;256;257;253;251;260;263;262;259;258;252;255;254;249;247;246;248;250;244;245;242;243;Texture + Transparency;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;187;-2033.093,-2278.163;Float;False;Constant;_OffsetMultiplier;Offset Multiplier;9;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;207;-1723.906,-1369.223;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;447;-1916.586,1656.64;Float;False;Property;_WindOffsetIntensity;Wind Offset Intensity;8;0;Create;True;0;0;False;0;0;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;408;-1918.377,1318.047;Float;True;Property;_GrassWindMask;Grass Wind Mask;3;0;Create;True;0;0;False;0;4f098fe2ad6d105408059bf4da0df12b;4f098fe2ad6d105408059bf4da0df12b;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;181;-2779.031,-3064.468;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;163;-3874.272,-1887.693;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;448;-1660.784,1562.339;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;421;-1377.173,1788.984;Float;False;Constant;_GradientPower;Gradient Power;21;0;Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;158;-2941.12,-2387.494;Float;True;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;423;-1411.308,1518.833;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldSpaceLightDirHlpNode;243;-2095.213,-22.15414;Float;False;False;1;0;FLOAT;0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.NegateNode;194;-2506.483,-1648.518;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;216;-1492.238,-1754.957;Float;False;YMultiplier;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;439;-1567.984,1320.48;Float;False;GrassWindMask;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;188;-1749.628,-2277.501;Float;False;XZMultiplier;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;217;-1406.231,-1375.004;Float;False;YOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldNormalVector;242;-2046.419,-174.7072;Float;False;False;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;452;-1797.527,2037.178;Float;False;1331.348;466.1685;;7;369;441;437;436;382;395;440;Wind Highlights;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;185;-2101.722,-2751.552;Float;False;XZOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;186;-1474.95,-2407.89;Float;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;440;-1772.474,2271.907;Float;False;439;GrassWindMask;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;-1158.43,-1697.675;Float;False;3;3;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DotProductOpNode;245;-1803.296,-102.6933;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;244;-1854.356,45.2149;Float;False;Constant;_WrapperValue;Wrapper Value;0;1;[HideInInspector];Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;414;-1116.454,1649.545;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;410;-1482.96,1408.325;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;412;-858.6261,1396.068;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;247;-1853.244,-756.5923;Float;True;Property;_Texture;Texture;0;0;Create;True;0;0;False;0;None;d30aaa9ddee34e140a5d518211cf0d50;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;246;-1700.607,-184.8486;Float;False;Constant;_Float3;Float 3;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;369;-1529.444,2096.846;Float;False;Property;_WindHighlightTint;Wind Highlight Tint;6;0;Create;True;0;0;False;0;0.4470589,0.764706,0.2431373,1;0.8905229,0.9333333,0.0470588,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;248;-1848.127,-381.8559;Float;True;Property;_ColorMask;Color Mask;1;0;Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;249;-1762.374,-553.478;Float;False;Property;_MainTint;MainTint;2;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;250;-1643.265,-50.79105;Float;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;240;-1175.817,699.0048;Float;False;712.0085;386.527;;5;233;234;235;236;237;Custom Billboarding;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;190;-860.9254,-2243.792;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ComponentMaskNode;395;-1535.801,2271.076;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;382;-1248.102,2391.714;Float;False;Property;_WindHighlightIntensity;Wind Highlight Intensity;5;0;Create;True;0;0;False;0;0.5;0.25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;255;-1494.901,-570.3718;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;121;-1706.303,574.5612;Float;False;467.0629;249.4112;;3;77;120;444;Combine Wind + Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;254;-1488.974,-374.7335;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;191;-678.9012,-2245.314;Float;False;GrassBendEffects;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SaturateNode;252;-1430.754,-50.68916;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;236;-1146.956,925.537;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;443;-680.2699,1391.511;Float;False;WindOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LightColorNode;253;-1602.52,174.3944;Float;False;0;3;COLOR;0;FLOAT3;1;FLOAT;2
Node;AmplifyShaderEditor.LightAttenuation;251;-1649.899,307.5399;Float;False;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;436;-1241.479,2170.617;Float;True;ColorBurn;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;444;-1641.087,715.8651;Float;False;443;WindOffset;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;437;-942.4242,2264.26;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.BillboardNode;235;-980.957,844.537;Float;False;Spherical;True;0;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;256;-1348.318,239.0701;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleNode;237;-933.4122,925.419;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;258;-1280.277,-491.1129;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;120;-1680.175,636.4781;Float;False;191;GrassBendEffects;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.IndirectDiffuseLighting;257;-1400.833,136.046;Float;False;Tangent;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;259;-1268.872,-79.31203;Float;True;Property;_ToonMapTexture;Toon Map Texture;16;1;[HideInInspector];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;261;-1119.458,183.6909;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;77;-1384.953,663.3443;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;441;-699.0602,2258.969;Float;False;WindHighlights;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;262;-937.8867,-296.0576;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;234;-755.0059,865.7167;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;260;-1046.02,-648.7954;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;442;-181.8569,-555.8363;Float;False;441;WindHighlights;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;263;-798.4742,-748.4741;Float;False;0;True;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;233;-596.158,752.4315;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;264;-703.6806,-44.63567;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;155.8395,-350.4269;Float;False;True;2;Float;ASEMaterialInspector;0;0;CustomLighting;Sully/Pixel Grass;False;False;False;False;True;True;True;True;True;True;True;True;False;False;False;False;False;False;False;False;Front;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;4;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;False;False;Cylindrical;False;Relative;0;;17;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;15;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;125;0;225;0
WireConnection;128;0;125;1
WireConnection;132;0;130;0
WireConnection;132;1;131;0
WireConnection;127;0;226;2
WireConnection;133;0;132;0
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
WireConnection;149;0;148;0
WireConnection;151;0;125;0
WireConnection;151;1;129;0
WireConnection;151;2;125;2
WireConnection;166;0;165;0
WireConnection;199;0;209;0
WireConnection;398;0;396;0
WireConnection;398;1;397;0
WireConnection;198;0;197;0
WireConnection;168;0;167;0
WireConnection;159;0;225;0
WireConnection;212;0;211;0
WireConnection;176;0;174;2
WireConnection;203;0;199;0
WireConnection;203;1;200;0
WireConnection;203;2;198;0
WireConnection;153;0;226;0
WireConnection;153;1;151;0
WireConnection;171;0;166;0
WireConnection;171;1;184;0
WireConnection;171;2;168;0
WireConnection;161;0;159;0
WireConnection;161;1;227;2
WireConnection;161;2;159;2
WireConnection;407;0;398;0
WireConnection;208;0;201;2
WireConnection;445;0;438;0
WireConnection;445;1;446;0
WireConnection;179;0;178;0
WireConnection;154;0;152;0
WireConnection;154;1;153;0
WireConnection;214;0;212;0
WireConnection;406;0;407;0
WireConnection;406;2;445;0
WireConnection;205;0;208;0
WireConnection;205;1;203;0
WireConnection;177;0;176;0
WireConnection;177;1;171;0
WireConnection;162;0;227;0
WireConnection;162;1;161;0
WireConnection;204;0;202;0
WireConnection;156;0;154;0
WireConnection;156;2;155;0
WireConnection;215;0;214;0
WireConnection;215;1;213;0
WireConnection;207;0;205;0
WireConnection;207;1;206;0
WireConnection;207;2;204;0
WireConnection;408;1;406;0
WireConnection;181;0;177;0
WireConnection;181;1;183;0
WireConnection;181;2;179;0
WireConnection;163;0;162;0
WireConnection;448;0;409;0
WireConnection;448;1;447;0
WireConnection;158;0;156;0
WireConnection;158;1;163;0
WireConnection;194;0;156;0
WireConnection;216;0;215;0
WireConnection;439;0;408;0
WireConnection;188;0;187;0
WireConnection;217;0;207;0
WireConnection;185;0;181;0
WireConnection;186;0;185;0
WireConnection;186;1;158;0
WireConnection;186;2;188;0
WireConnection;218;0;216;0
WireConnection;218;1;217;0
WireConnection;218;2;194;0
WireConnection;245;0;242;0
WireConnection;245;1;243;0
WireConnection;414;0;423;2
WireConnection;414;1;421;0
WireConnection;410;0;408;0
WireConnection;410;1;448;0
WireConnection;412;1;410;0
WireConnection;412;2;414;0
WireConnection;250;0;245;0
WireConnection;250;1;244;0
WireConnection;250;2;244;0
WireConnection;190;0;186;0
WireConnection;190;1;218;0
WireConnection;395;0;440;0
WireConnection;255;0;247;0
WireConnection;255;1;249;0
WireConnection;254;0;248;0
WireConnection;254;1;246;0
WireConnection;191;0;190;0
WireConnection;252;0;250;0
WireConnection;443;0;412;0
WireConnection;436;0;369;0
WireConnection;436;1;395;0
WireConnection;437;0;436;0
WireConnection;437;1;382;0
WireConnection;256;0;253;1
WireConnection;256;1;251;0
WireConnection;237;0;236;0
WireConnection;258;0;247;0
WireConnection;258;1;255;0
WireConnection;258;2;254;0
WireConnection;257;0;253;0
WireConnection;259;1;252;0
WireConnection;261;0;257;0
WireConnection;261;1;256;0
WireConnection;77;0;120;0
WireConnection;77;1;444;0
WireConnection;441;0;437;0
WireConnection;262;0;258;0
WireConnection;262;1;259;0
WireConnection;234;0;235;0
WireConnection;234;1;237;0
WireConnection;263;0;247;4
WireConnection;263;2;260;0
WireConnection;233;0;77;0
WireConnection;233;1;234;0
WireConnection;264;0;262;0
WireConnection;264;1;261;0
WireConnection;0;2;442;0
WireConnection;0;10;263;0
WireConnection;0;13;264;0
WireConnection;0;11;233;0
ASEEND*/
//CHKSM=ADCB026011721A07D89A7F6CE5F95A1C1B32CF16