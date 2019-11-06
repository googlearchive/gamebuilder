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
Shader "Sully/Dissolve"
{
	Properties
	{
		_DissolveProgress("Dissolve Progress", Range( 0 , 0.5)) = 0
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Color0("Color 0", Color) = (0.3764706,0.9372549,0.9137255,1)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 4.6
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float3 worldNormal;
			float4 screenPosition;
		};

		uniform float _DissolveProgress;
		uniform float4 _Color0;
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
			float3 ase_vertex3Pos = v.vertex.xyz;
			v.vertex.xyz += ( ( (0.0 + (( 1.0 - (0.0 + (_DissolveProgress - 0.0) * (2.0 - 0.0) / (1.0 - 0.0)) ) - 0.0) * (0.25 - 0.0) / (1.0 - 0.0)) * ase_vertex3Pos ) + float3( 0,0,0 ) );
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float fresnelNdotV322 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode322 = ( 0.5 + 0.5 * pow( 1.0 - fresnelNdotV322, -0.15 ) );
			o.Emission = ( fresnelNode322 * _Color0 ).rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen209 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither209 = Dither4x4Bayer( fmod(clipScreen209.x, 4), fmod(clipScreen209.y, 4) );
			float lerpResult313 = lerp( 0.0 , sin( ( 10.0 * ( ase_worldPos.y + ( _Time.x * 20.0 ) ) ) ) , 1.0);
			dither209 = step( dither209, ( lerpResult313 * (0.0 + (_DissolveProgress - 0.0) * (2.0 - 0.0) / (1.0 - 0.0)) * float4(1,1,1,1) ).r );
			clip( dither209 - _Cutoff );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
2567;268;1844;1044;-572.3821;-233.4527;1.104538;True;True
Node;AmplifyShaderEditor.RangedFloatNode;304;-71.20542,205.3966;Float;False;Constant;_AnimationStrength;Animation Strength;4;1;[HideInInspector];Create;True;0;0;False;0;20;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;303;-74.96337,33.74547;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;306;233.9545,150.2726;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;305;183.0958,-8.851954;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;160;295.5281,840.1994;Float;False;Property;_DissolveProgress;Dissolve Progress;0;0;Create;True;0;0;False;0;0;0.5;0;0.5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;307;387.8922,8.88987;Float;False;Constant;_ScanlineScale;Scanline Scale;3;1;[HideInInspector];Create;True;0;0;False;0;10;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;308;451.1418,112.7104;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;302;701.9642,1113.67;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;309;590.5515,87.39467;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;213;907.5426,1110.616;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;310;744.3731,97.27589;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;312;473.1599,254.249;Float;False;Constant;_ScanlineStrength;Scanline Strength;5;1;[HideInInspector];Create;True;0;0;False;0;1;0.4;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PosVertexDataNode;224;1063.52,1288.539;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;234;1072.206,1113.138;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;0.25;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;313;924.5677,76.55702;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;286;1107.198,720.7821;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;2;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;166;1095.475,896.3337;Float;False;Constant;_MainTint;MainTint;3;0;Create;True;0;0;False;0;1,1,1,1;0,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;285;1446.289,792.9247;Float;True;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;323;1198.055,424.741;Float;False;Property;_Color0;Color 0;2;0;Create;True;0;0;False;0;0.3764706,0.9372549,0.9137255,1;0.3764705,0.9372549,0.9137255,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FresnelNode;322;1135.954,197.8943;Float;True;Standard;WorldNormal;ViewDir;True;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0.5;False;2;FLOAT;0.5;False;3;FLOAT;-0.15;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;212;1321.233,1193.714;Float;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;241;1507.081,1192.525;Float;True;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DitheringNode;209;1777.377,791.7959;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;324;1510.087,310.0356;Float;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;2010.876,566.6758;Float;False;True;6;Float;ASEMaterialInspector;0;0;Standard;Sully/Dissolve;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;False;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;306;0;303;1
WireConnection;306;1;304;0
WireConnection;308;0;305;2
WireConnection;308;1;306;0
WireConnection;302;0;160;0
WireConnection;309;0;307;0
WireConnection;309;1;308;0
WireConnection;213;0;302;0
WireConnection;310;0;309;0
WireConnection;234;0;213;0
WireConnection;313;1;310;0
WireConnection;313;2;312;0
WireConnection;286;0;160;0
WireConnection;285;0;313;0
WireConnection;285;1;286;0
WireConnection;285;2;166;0
WireConnection;212;0;234;0
WireConnection;212;1;224;0
WireConnection;241;0;212;0
WireConnection;209;0;285;0
WireConnection;324;0;322;0
WireConnection;324;1;323;0
WireConnection;0;2;324;0
WireConnection;0;10;209;0
WireConnection;0;11;241;0
ASEEND*/
//CHKSM=BD4174E23D8D262ADB784460F3C1385791EF6988