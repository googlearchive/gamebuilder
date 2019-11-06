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
Shader "Sully/Hologram"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_MainTint("MainTint", Color) = (0.3764706,0.937255,0.9137256,1)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 4.6
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float4 screenPosition;
			float3 worldPos;
			float3 worldNormal;
		};

		uniform float4 _MainTint;
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
			o.Emission = _MainTint.rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float4 ditherCustomScreenPos13 = ase_screenPosNorm;
			float2 clipScreen13 = ditherCustomScreenPos13.xy * _ScreenParams.xy;
			float dither13 = Dither4x4Bayer( fmod(clipScreen13.x, 4), fmod(clipScreen13.y, 4) );
			float3 ase_worldPos = i.worldPos;
			float lerpResult71 = lerp( 1.0 , sin( ( 2.0 * ( ase_worldPos.y + ( _Time.x * 50.0 ) ) ) ) , 0.35);
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = i.worldNormal;
			float clampResult8 = clamp( 1.0 , 0.0 , 4.0 );
			float fresnelNdotV9 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode9 = ( 0.0 + 1.0 * pow( 1.0 - fresnelNdotV9, clampResult8 ) );
			dither13 = step( dither13, ( lerpResult71 * ( 1.0 - fresnelNode9 ) ) );
			clip( dither13 - _Cutoff );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
7;1;2546;1530;2950.794;1323.088;2.235482;True;True
Node;AmplifyShaderEditor.CommentaryNode;102;-1403.828,247.2743;Float;False;1407.193;403.8862;Scanlines;12;75;77;72;73;74;68;69;71;12;78;70;76;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-1331.504,535.6252;Float;False;Constant;_AnimationStrength;Animation Strength;4;1;[HideInInspector];Create;True;0;0;False;0;50;100;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;75;-1335.262,363.9738;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;103;-1157.813,725.5767;Float;False;934.9331;305.7513;Fresnel;4;7;8;9;89;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;-1026.343,480.5012;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;72;-1077.202,321.3765;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;73;-809.1559,442.939;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-872.4055,339.1182;Float;False;Constant;_ScanlineScale;Scanline Scale;3;1;[HideInInspector];Create;True;0;0;False;0;2;2;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;7;-1106.214,840.3124;Float;False;Constant;_FresnelPower;Fresnel Power;0;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;68;-669.7466,417.6232;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;8;-876.2605,838.4659;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;4;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;78;-696.4055,563.1183;Float;False;Constant;_ScanlineStrength;Scanline Strength;5;1;[HideInInspector];Create;True;0;0;False;0;0.35;0.4;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;70;-521.9119,323.1182;Float;False;Constant;_constant1;constant1;11;0;Create;True;0;0;False;0;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FresnelNode;9;-701.224,796.7948;Float;True;Standard;TangentNormal;ViewDir;False;5;0;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;69;-515.9252,427.5044;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;71;-335.7307,406.7855;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;126;56.12038,258.2719;Float;False;517.7682;361.3132;Dither;2;124;13;;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;89;-410.4645,817.0881;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;124;92.10688,424.4569;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;12;-139.9882,418.7449;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DitheringNode;13;338.0843,322.8038;Float;False;0;True;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;97;339.1454,-32.44496;Float;False;Property;_MainTint;MainTint;1;0;Create;True;0;0;False;0;0.3764706,0.937255,0.9137256,1;0.05098038,0.8167245,0.9803922,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;717.0723,-27.005;Float;False;True;6;Float;ASEMaterialInspector;0;0;Standard;Sully/Hologram;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0.1;0.3764706,0.937255,0.9137256,0;VertexScale;False;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;77;0;75;1
WireConnection;77;1;76;0
WireConnection;73;0;72;2
WireConnection;73;1;77;0
WireConnection;68;0;74;0
WireConnection;68;1;73;0
WireConnection;8;0;7;0
WireConnection;9;3;8;0
WireConnection;69;0;68;0
WireConnection;71;0;70;0
WireConnection;71;1;69;0
WireConnection;71;2;78;0
WireConnection;89;0;9;0
WireConnection;12;0;71;0
WireConnection;12;1;89;0
WireConnection;13;0;12;0
WireConnection;13;2;124;0
WireConnection;0;2;97;0
WireConnection;0;10;13;0
ASEEND*/
//CHKSM=C105FA76ED385E5EE8E8AFEC5521304CC3D68559