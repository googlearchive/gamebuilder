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

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Sully/Avatar Hologram"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Transparency("Transparency", Range( 0 , 1)) = 1
		_Texture("Texture", 2D) = "white" {}
		_MainTint("MainTint", Color) = (1,1,1,0)
		[HideInInspector]_ColorMask("Color Mask", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float2 uv_texcoord;
			float4 screenPosition;
		};

		uniform float4 _MainTint;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform sampler2D _ColorMask;
		uniform float4 _ColorMask_ST;
		uniform float _Transparency;
		uniform float _Cutoff = 0.5;


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
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float2 temp_output_1_0_g1 = float2( 0,70 );
			float temp_output_6_0_g1 = _Time.y;
			float temp_output_16_0_g1 = (temp_output_1_0_g1).y;
			float YVal31_g1 = ( ( 150.0 * cos( ( ( UNITY_PI * (temp_output_1_0_g1).x ) + ( UNITY_PI * temp_output_6_0_g1 ) ) ) * sin( ( ( temp_output_16_0_g1 * UNITY_PI ) + ( 40.0 / 3.0 ) + ( temp_output_6_0_g1 * UNITY_PI ) ) ) ) + temp_output_16_0_g1 );
			float fresnelNdotV28 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode28 = ( 0.0 + abs( ( 1.0 / ( ( YVal31_g1 * 1.0 ) / 75.0 ) ) ) * pow( 1.0 - fresnelNdotV28, 3.0 ) );
			float clampResult39 = clamp( fresnelNode28 , 0.0 , 1.0 );
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode14 = tex2D( _Texture, uv_Texture );
			float4 blendOpSrc20 = tex2DNode14;
			float4 blendOpDest20 = _MainTint;
			float2 uv_ColorMask = i.uv_texcoord * _ColorMask_ST.xy + _ColorMask_ST.zw;
			float4 lerpResult19 = lerp( tex2DNode14 , ( saturate( (( blendOpDest20 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpDest20 - 0.5 ) ) * ( 1.0 - blendOpSrc20 ) ) : ( 2.0 * blendOpDest20 * blendOpSrc20 ) ) )) , ( tex2D( _ColorMask, uv_ColorMask ) * 1.0 ));
			o.Emission = ( ( ( clampResult39 * _MainTint ) * 5.0 ) + lerpResult19 ).rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen40 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither40 = Dither4x4Bayer( fmod(clipScreen40.x, 4), fmod(clipScreen40.y, 4) );
			float4 ditherCustomScreenPos12 = ase_screenPosNorm;
			float2 clipScreen12 = ditherCustomScreenPos12.xy * _ScreenParams.xy;
			float dither12 = Dither4x4Bayer( fmod(clipScreen12.x, 4), fmod(clipScreen12.y, 4) );
			dither12 = step( dither12, _Transparency );
			dither40 = step( dither40, ( tex2DNode14.a * dither12 ) );
			clip( dither40 - _Cutoff );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
7;7;2546;1524;2376.581;1198.871;1.3;True;True
Node;AmplifyShaderEditor.TimeNode;38;-1951.172,-974.6375;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode;37;-1708.908,-976.1177;Float;False;CoolWave;-1;;1;a4ec317493edf3b439fcd463a40eca0d;0;6;35;FLOAT;150;False;4;FLOAT;1;False;6;FLOAT;0;False;3;FLOAT;75;False;1;FLOAT2;0,70;False;2;FLOAT;40;False;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;28;-1384.621,-988.3601;Float;False;Standard;WorldNormal;ViewDir;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0.5;False;3;FLOAT;3;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;16;-1304.266,-325.0817;Float;True;Property;_ColorMask;Color Mask;4;1;[HideInInspector];Create;True;0;0;False;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ClampOpNode;39;-1144.019,-966.1188;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;15;-1388.431,-792.0804;Float;False;Property;_MainTint;MainTint;3;0;Create;True;0;0;False;0;1,1,1,0;0.874,0.874,0.874,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;14;-1035.645,-533.9827;Float;True;Property;_Texture;Texture;2;0;Create;True;0;0;False;0;None;e781ba220fa5b214b84c235198765926;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;13;-1163.092,-118.8014;Float;False;Constant;_Float3;Float 3;4;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;11;-1219.757,218.9603;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;10;-1243.057,120.6459;Float;False;Property;_Transparency;Transparency;1;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.DitheringNode;12;-919.8644,107.6147;Float;False;0;True;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;20;-652.962,-426.7989;Float;False;Overlay;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-521.7071,-209.6849;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;29;-989.6,-901.0646;Float;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;36;-954.5336,-666.2142;Float;False;Constant;_Float0;Float 0;4;0;Create;True;0;0;False;0;5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;35;-688.1502,-777.0764;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;19;-361.0908,-378.0714;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;8;-609.5955,76.44096;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;22;-113.6761,-408.0446;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;40;-0.1796875,62.12732;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;3;546.1616,-246.8878;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/Avatar Hologram;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;37;6;38;2
WireConnection;28;2;37;0
WireConnection;39;0;28;0
WireConnection;12;0;10;0
WireConnection;12;2;11;0
WireConnection;20;0;14;0
WireConnection;20;1;15;0
WireConnection;17;0;16;0
WireConnection;17;1;13;0
WireConnection;29;0;39;0
WireConnection;29;1;15;0
WireConnection;35;0;29;0
WireConnection;35;1;36;0
WireConnection;19;0;14;0
WireConnection;19;1;20;0
WireConnection;19;2;17;0
WireConnection;8;0;14;4
WireConnection;8;1;12;0
WireConnection;22;0;35;0
WireConnection;22;1;19;0
WireConnection;40;0;8;0
WireConnection;3;2;22;0
WireConnection;3;10;40;0
ASEEND*/
//CHKSM=7AFE3D5F40AA1EC8800209FF69030F56CF925C42