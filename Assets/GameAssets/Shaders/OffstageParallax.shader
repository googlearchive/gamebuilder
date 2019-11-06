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
Shader "Sully/Offstage Parallax"
{
	Properties
	{
		_Tiling("Tiling", Float) = 4
		_Albedo("Albedo", 2D) = "white" {}
		_HeightMap("HeightMap", 2D) = "white" {}
		_Highlight("Highlight", 2D) = "white" {}
		_Parallax("Parallax", Float) = 0.250046
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			half2 uv_texcoord;
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
			half4 screenPosition;
		};

		uniform sampler2D _Albedo;
		uniform half _Tiling;
		uniform sampler2D _HeightMap;
		uniform half _Parallax;
		uniform sampler2D _Highlight;


		inline float Dither8x8Bayer( int x, int y )
		{
			const float dither[ 64 ] = {
				 1, 49, 13, 61,  4, 52, 16, 64,
				33, 17, 45, 29, 36, 20, 48, 32,
				 9, 57,  5, 53, 12, 60,  8, 56,
				41, 25, 37, 21, 44, 28, 40, 24,
				 3, 51, 15, 63,  2, 50, 14, 62,
				35, 19, 47, 31, 34, 18, 46, 30,
				11, 59,  7, 55, 10, 58,  6, 54,
				43, 27, 39, 23, 42, 26, 38, 22};
			int r = y * 8 + x;
			return dither[r] / 64; // same # of instructions as pre-dividing due to compiler magic
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			half2 temp_cast_0 = (_Tiling).xx;
			float2 uv_TexCoord94 = i.uv_texcoord * temp_cast_0;
			float3 ase_worldPos = i.worldPos;
			half3 ase_worldViewDir = Unity_SafeNormalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			half3 ase_worldNormal = WorldNormalVector( i, half3( 0, 0, 1 ) );
			half3 ase_worldTangent = WorldNormalVector( i, half3( 1, 0, 0 ) );
			half3 ase_worldBitangent = WorldNormalVector( i, half3( 0, 1, 0 ) );
			half3x3 ase_worldToTangent = float3x3( ase_worldTangent, ase_worldBitangent, ase_worldNormal );
			half3 ase_tanViewDir = mul( ase_worldToTangent, ase_worldViewDir );
			float2 Offset12 = ( ( tex2D( _HeightMap, uv_TexCoord94 ).r - 1 ) * ase_tanViewDir.xy * ( _Parallax * _SinTime.w ) ) + uv_TexCoord94;
			half4 tex2DNode61 = tex2D( _Albedo, Offset12 );
			float2 panner77 = ( 1.0 * _Time.y * float2( 0,-1 ) + ( uv_TexCoord94 * half2( 1,0.1 ) ));
			float cos86 = cos( radians( 90.0 ) );
			float sin86 = sin( radians( 90.0 ) );
			float2 rotator86 = mul( ( uv_TexCoord94 * half2( 0.1,1 ) ) - float2( 0.5,0.5 ) , float2x2( cos86 , -sin86 , sin86 , cos86 )) + float2( 0.5,0.5 );
			float2 panner84 = ( 1.0 * _Time.y * float2( 0,-0.5 ) + rotator86);
			o.Emission = ( tex2DNode61 + ( tex2D( _Highlight, panner77 ) + tex2D( _Highlight, panner84 ) ) ).rgb;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen101 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither101 = Dither8x8Bayer( fmod(clipScreen101.x, 8), fmod(clipScreen101.y, 8) );
			dither101 = step( dither101, tex2DNode61.a );
			o.Alpha = dither101;
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
-1797;-1919;1618;986;2633.212;528.1858;2.653496;True;True
Node;AmplifyShaderEditor.RangedFloatNode;1;-1542.674,226.3527;Float;False;Property;_Tiling;Tiling;1;0;Create;True;0;0;False;0;4;25;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;87;-1133.182,975.1633;Float;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;False;0;90;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;98;-1153.678,835.9096;Float;False;Constant;_Vector1;Vector 1;5;0;Create;True;0;0;False;0;0.1,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.TextureCoordinatesNode;94;-1362.241,206.3249;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RadiansOpNode;88;-976.6091,978.8611;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;99;-980.7526,788.8293;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector2Node;97;-988.629,645.4853;Float;False;Constant;_Vector0;Vector 0;5;0;Create;True;0;0;False;0;1,0.1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RotatorNode;86;-803.5867,850.7095;Float;False;3;0;FLOAT2;180,0;False;1;FLOAT2;0.5,0.5;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;3;-1050.778,-385.7317;Float;False;Property;_Parallax;Parallax;5;0;Create;True;0;0;False;0;0.250046;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinTimeNode;91;-1030.096,-296.6555;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;96;-772.2382,592.2715;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;84;-602.4069,851.9783;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-0.5;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;77;-604.2838,650.9714;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,-1;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;4;-608.4968,399.0481;Float;False;Tangent;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;100;-775.0329,-353.4551;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;9;-710.4042,16.4965;Float;True;Property;_HeightMap;HeightMap;3;0;Create;True;0;0;False;0;b758c1cba12b91747acb851f30aa5445;b758c1cba12b91747acb851f30aa5445;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;FLOAT2;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;75;-409.1843,624.9714;Float;True;Property;_Highlight;Highlight;4;0;Create;True;0;0;False;0;33681c357a44b5e48b1a89b9a07531dd;33681c357a44b5e48b1a89b9a07531dd;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;82;-405.3289,826.765;Float;True;Property;_TextureSample4;Texture Sample 4;4;0;Create;True;0;0;False;0;33681c357a44b5e48b1a89b9a07531dd;33681c357a44b5e48b1a89b9a07531dd;True;0;False;white;Auto;False;Instance;75;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ParallaxMappingNode;12;-356.3092,205.0121;Float;False;Normal;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;90;-3.922503,728.5352;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;61;-40.10659,181.7911;Float;True;Property;_Albedo;Albedo;2;0;Create;True;0;0;False;0;420521c2ad0ccc84c8d2d96a8300fd37;420521c2ad0ccc84c8d2d96a8300fd37;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;FLOAT2;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;81;354.3343,450.9591;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;101;371.6172,281.2156;Float;False;1;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;670.1281,77.16933;Half;False;True;2;Half;ASEMaterialInspector;0;0;Standard;Sully/Offstage Parallax;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;False;0;False;Transparent;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;94;0;1;0
WireConnection;88;0;87;0
WireConnection;99;0;94;0
WireConnection;99;1;98;0
WireConnection;86;0;99;0
WireConnection;86;2;88;0
WireConnection;96;0;94;0
WireConnection;96;1;97;0
WireConnection;84;0;86;0
WireConnection;77;0;96;0
WireConnection;100;0;3;0
WireConnection;100;1;91;4
WireConnection;9;1;94;0
WireConnection;75;1;77;0
WireConnection;82;1;84;0
WireConnection;12;0;94;0
WireConnection;12;1;9;1
WireConnection;12;2;100;0
WireConnection;12;3;4;0
WireConnection;90;0;75;0
WireConnection;90;1;82;0
WireConnection;61;1;12;0
WireConnection;81;0;61;0
WireConnection;81;1;90;0
WireConnection;101;0;61;4
WireConnection;0;2;81;0
WireConnection;0;9;101;0
ASEEND*/
//CHKSM=EBE514E56EB115BB653A5ABBF1B75C22D927EB7D