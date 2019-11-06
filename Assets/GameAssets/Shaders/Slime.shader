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
Shader "Sully/Slime"
{
	Properties
	{
		_MainTint("MainTint", Color) = (1,1,1,0)
		_Texture("Texture", 2D) = "white" {}
		_EmissionEyes("Emission (Eyes)", 2D) = "white" {}
		[HideInInspector]_Float0("Float 0", Range( 0 , 0.75)) = 0.5
		[HideInInspector]_Float2("Float 2", Range( 0 , 1)) = 0.25
		_Running("Running", Int) = 1
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
		#pragma surface surf Standard keepalpha vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float4 screenPosition;
		};

		uniform int _Running;
		uniform float _Float0;
		uniform float _Float2;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float4 _MainTint;
		uniform sampler2D _EmissionEyes;
		uniform float4 _EmissionEyes_ST;
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
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float temp_output_222_0 = ( _Time.y * _Running );
			float temp_output_139_0 = ( ( sin( ( ( ase_worldPos.x + temp_output_222_0 + ase_worldPos.z ) * 1.0 ) ) * cos( ( ( v.texcoord1.xy.y + temp_output_222_0 ) / ( 1.0 - _Float0 ) ) ) ) * (-0.5 + (( 1.0 - _Float2 ) - 0.0) * (0.0 - -0.5) / (1.0 - 0.0)) );
			float4 appendResult141 = (float4(temp_output_139_0 , 0.0 , temp_output_139_0 , 0.0));
			v.vertex.xyz += ( v.color.r * appendResult141 * v.color.b ).xyz;
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode183 = tex2D( _Texture, uv_Texture );
			float4 blendOpSrc213 = tex2DNode183;
			float4 blendOpDest213 = _MainTint;
			float4 temp_output_213_0 = ( saturate(  (( blendOpSrc213 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpSrc213 - 0.5 ) ) * ( 1.0 - blendOpDest213 ) ) : ( 2.0 * blendOpSrc213 * blendOpDest213 ) ) ));
			o.Albedo = temp_output_213_0.rgb;
			float2 uv_EmissionEyes = i.uv_texcoord * _EmissionEyes_ST.xy + _EmissionEyes_ST.zw;
			o.Emission = ( tex2D( _EmissionEyes, uv_EmissionEyes ) + ( temp_output_213_0 * 0.5 ) ).rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen224 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither224 = Dither4x4Bayer( fmod(clipScreen224.x, 4), fmod(clipScreen224.y, 4) );
			dither224 = step( dither224, tex2DNode183.a );
			clip( dither224 - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
7;7;2546;1524;1972.25;757.7544;1.558166;True;True
Node;AmplifyShaderEditor.CommentaryNode;114;-2990.473,829.3363;Float;False;3072.81;1248.075;;22;142;141;140;139;136;134;133;132;129;127;126;123;122;119;118;117;116;115;143;144;222;221;Jiggle Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;116;-2943.073,1287.048;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;221;-2989.837,1460.224;Float;False;Property;_Running;Running;5;0;Create;True;0;0;False;0;1;1;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;222;-2690.122,1422.604;Float;False;2;2;0;FLOAT;0;False;1;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;115;-2572.502,1866.14;Float;False;Property;_Float0;Float 0;3;1;[HideInInspector];Create;True;0;0;False;0;0.5;0.75;0;0.75;0;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;117;-2877.748,1719.525;Float;True;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;143;-2588.006,1205.089;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;118;-2425.642,1562.269;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;119;-2466.048,1743.107;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;122;-2348.093,1450.92;Float;False;Constant;_Float1;Float 1;3;1;[HideInInspector];Create;True;0;0;False;0;1;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;144;-2320.864,1227.911;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;127;-1939.896,1276.721;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;126;-1369.153,1774.688;Float;False;Property;_Float2;Float 2;4;1;[HideInInspector];Create;True;0;0;False;0;0.25;0.15;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;123;-2221.432,1562.175;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;133;-1380.78,1611.391;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;169;-878.2669,9.714336;Float;False;708.0194;495.7169;;3;213;184;183;Main Texture + Colour Masking;1,1,1,1;0;0
Node;AmplifyShaderEditor.SinOpNode;132;-1717.077,1276.906;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;129;-1954.616,1562.625;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;184;-732.9698,292.646;Float;False;Property;_MainTint;MainTint;0;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;183;-819.0638,86.7783;Float;True;Property;_Texture;Texture;1;0;Create;True;0;0;False;0;None;2c8df84bfb2ae9c47b8dda8cda2545b7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;136;-1369.891,1319.529;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;134;-1189.47,1579.892;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;139;-1000.875,1310.427;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;220;-43.37523,22.50629;Float;False;Constant;_Float3;Float 3;7;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;213;-454.5414,173.801;Float;True;HardLight;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;215;-3.166138,-214.4332;Float;True;Property;_EmissionEyes;Emission (Eyes);2;0;Create;True;0;0;False;0;None;9a290158e680e8b40b00048e87754173;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;141;-711.4433,1229.051;Float;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.VertexColorNode;140;-697.4352,948.9666;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;219;142.1775,10.02765;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;142;-416.0934,972.6366;Float;True;3;3;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;218;359.1471,-101.3693;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;224;59.5979,413.9864;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;528.4552,195.1936;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/Slime;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0.1;0.4352942,0.6509804,0.2470588,0;VertexScale;True;False;Cylindrical;False;Relative;0;;6;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;222;0;116;2
WireConnection;222;1;221;0
WireConnection;118;0;117;2
WireConnection;118;1;222;0
WireConnection;119;0;115;0
WireConnection;144;0;143;1
WireConnection;144;1;222;0
WireConnection;144;2;143;3
WireConnection;127;0;144;0
WireConnection;127;1;122;0
WireConnection;123;0;118;0
WireConnection;123;1;119;0
WireConnection;133;0;126;0
WireConnection;132;0;127;0
WireConnection;129;0;123;0
WireConnection;136;0;132;0
WireConnection;136;1;129;0
WireConnection;134;0;133;0
WireConnection;139;0;136;0
WireConnection;139;1;134;0
WireConnection;213;0;183;0
WireConnection;213;1;184;0
WireConnection;141;0;139;0
WireConnection;141;2;139;0
WireConnection;219;0;213;0
WireConnection;219;1;220;0
WireConnection;142;0;140;1
WireConnection;142;1;141;0
WireConnection;142;2;140;3
WireConnection;218;0;215;0
WireConnection;218;1;219;0
WireConnection;224;0;183;4
WireConnection;0;0;213;0
WireConnection;0;2;218;0
WireConnection;0;10;224;0
WireConnection;0;11;142;0
ASEEND*/
//CHKSM=4210932E03C65E6BC8C8F36587C1BF2A9C9B9685