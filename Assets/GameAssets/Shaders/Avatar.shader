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
Shader "Sully/Avatar"
{
	Properties
	{
		_Texture("Texture", 2D) = "white" {}
		_EmissionMask("Emission Mask", 2D) = "black" {}
		_BodyColorMask("Body Color Mask", 2D) = "white" {}
		_DecalColorMask("Decal Color Mask", 2D) = "white" {}
		_MainTint("MainTint", Color) = (1,1,1,0)
		_SecondaryTint("SecondaryTint", Color) = (1,1,1,0)
		_Transparency("Transparency", Range( 0 , 1)) = 1
		_Cutoff( "Mask Clip Value", Float ) = 0.5
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
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
			float4 screenPosition;
		};

		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _MainTint;
		uniform sampler2D _DecalColorMask;
		uniform float4 _DecalColorMask_ST;
		uniform float4 _SecondaryTint;
		uniform sampler2D _BodyColorMask;
		uniform float4 _BodyColorMask_ST;
		uniform sampler2D _EmissionMask;
		uniform float4 _EmissionMask_ST;
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
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode208 = tex2D( _Texture, uv_Texture );
			float2 uv_DecalColorMask = i.uv_texcoord * _DecalColorMask_ST.xy + _DecalColorMask_ST.zw;
			float4 tex2DNode209 = tex2D( _DecalColorMask, uv_DecalColorMask );
			float4 lerpResult225 = lerp( float4( 0,0,0,0 ) , ( tex2DNode208 * _MainTint ) , tex2DNode209);
			float4 tex2DNode207 = tex2D( _Texture, uv_Texture );
			float2 uv_BodyColorMask = i.uv_texcoord * _BodyColorMask_ST.xy + _BodyColorMask_ST.zw;
			float4 tex2DNode214 = tex2D( _BodyColorMask, uv_BodyColorMask );
			float4 lerpResult219 = lerp( float4( 0,0,0,0 ) , ( tex2DNode207 * _SecondaryTint ) , tex2DNode214);
			float4 lerpResult226 = lerp( tex2DNode207 , float4( 0,0,0,0 ) , ( tex2DNode209 + tex2DNode214 ));
			o.Albedo = ( lerpResult225 + ( lerpResult219 * 2.5 ) + lerpResult226 ).rgb;
			float2 uv_EmissionMask = i.uv_texcoord * _EmissionMask_ST.xy + _EmissionMask_ST.zw;
			o.Emission = ( tex2D( _EmissionMask, uv_EmissionMask ) * 1.0 ).rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen242 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither242 = Dither4x4Bayer( fmod(clipScreen242.x, 4), fmod(clipScreen242.y, 4) );
			float4 ditherCustomScreenPos235 = ase_screenPosNorm;
			float2 clipScreen235 = ditherCustomScreenPos235.xy * _ScreenParams.xy;
			float dither235 = Dither4x4Bayer( fmod(clipScreen235.x, 4), fmod(clipScreen235.y, 4) );
			dither235 = step( dither235, _Transparency );
			dither242 = step( dither242, ( tex2DNode208.a * dither235 ) );
			clip( dither242 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
7;7;2546;1524;3415.332;1511.807;3.009475;True;True
Node;AmplifyShaderEditor.CommentaryNode;203;-1180.677,61.9652;Float;False;1427.851;1297.914;;15;232;226;225;222;219;218;217;216;214;213;212;209;208;207;206;Main Texture + Colour Masking;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;207;-1062.607,716.3195;Float;True;Property;_TextureSample2;Texture Sample 2;0;0;Create;True;0;0;False;0;None;982999d043e9c384d9bfcd231523544f;True;0;False;white;Auto;False;Instance;208;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;212;-991.3824,920.9752;Float;False;Property;_SecondaryTint;SecondaryTint;5;0;Create;True;0;0;False;0;1,1,1,0;0.9490196,0.6745098,0.3411765,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;220;-542.8354,-515.5209;Float;False;675.0609;425.6818;;3;235;233;231;Dithered Transparency;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;209;-1077.539,496.9969;Float;True;Property;_DecalColorMask;Decal Color Mask;3;0;Create;True;0;0;False;0;a939ef97d5f57c34098590e2c82d5b70;a939ef97d5f57c34098590e2c82d5b70;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;213;-659.3093,867.1766;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;208;-1108.594,113.6108;Float;True;Property;_Texture;Texture;0;0;Create;True;0;0;False;0;982999d043e9c384d9bfcd231523544f;982999d043e9c384d9bfcd231523544f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;214;-1071.857,1115.327;Float;True;Property;_BodyColorMask;Body Color Mask;2;0;Create;True;0;0;False;0;87d47880663ea8f489a141958bab15f7;87d47880663ea8f489a141958bab15f7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;206;-994.8365,313.7528;Float;False;Property;_MainTint;MainTint;4;0;Create;True;0;0;False;0;1,1,1,0;0.7450981,0.1490196,0.2,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenPosInputsNode;233;-428.5203,-306.6033;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;231;-455.5433,-414.2236;Float;False;Property;_Transparency;Transparency;6;0;Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;228;304.14,-555.3389;Float;False;647.6017;430.7661;;3;240;237;234;Masked Emission;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;218;-696.1593,293.5586;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;219;-424.2838,906.1132;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;217;-298.5988,1165.363;Float;False;Constant;_Float0;Float 0;9;0;Create;True;0;0;False;0;2.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;216;-666.8694,594.5703;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;235;-126.7454,-414.1805;Float;False;0;True;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;237;422.0957,-243.7509;Float;False;Constant;_EmissionStrength;Emission Strength;4;0;Create;True;0;0;False;0;1;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;234;377.0776,-480.3672;Float;True;Property;_EmissionMask;Emission Mask;1;0;Create;True;0;0;False;0;354b26161e44c8f4299253ba17866649;92fc1503c4385dd4cb03559547d48cdc;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;241;448.2883,244.3047;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;226;-404.8278,600.7247;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;225;-417.0706,338.5394;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;222;-74.71448,1066.824;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;240;746.0734,-357.5673;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;232;-27.85358,601.8809;Float;True;3;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;242;819.2013,427.0408;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1224.869,306.1063;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/Avatar;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;5;False;-1;10;False;-1;2;5;False;-1;10;False;-1;1;False;-1;21;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;7;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;213;0;207;0
WireConnection;213;1;212;0
WireConnection;218;0;208;0
WireConnection;218;1;206;0
WireConnection;219;1;213;0
WireConnection;219;2;214;0
WireConnection;216;0;209;0
WireConnection;216;1;214;0
WireConnection;235;0;231;0
WireConnection;235;2;233;0
WireConnection;241;0;208;4
WireConnection;241;1;235;0
WireConnection;226;0;207;0
WireConnection;226;2;216;0
WireConnection;225;1;218;0
WireConnection;225;2;209;0
WireConnection;222;0;219;0
WireConnection;222;1;217;0
WireConnection;240;0;234;0
WireConnection;240;1;237;0
WireConnection;232;0;225;0
WireConnection;232;1;222;0
WireConnection;232;2;226;0
WireConnection;242;0;241;0
WireConnection;0;0;232;0
WireConnection;0;2;240;0
WireConnection;0;10;242;0
ASEEND*/
//CHKSM=D7535772001E1BDEF16E27A56B2B4D0D09A5A72C