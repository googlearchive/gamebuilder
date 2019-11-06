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

// Upgrade NOTE: upgraded instancing buffer 'SullyFoliage' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Sully/Foliage"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Float4("Float 4", Range( 0 , 10)) = 1
		[HideInInspector]_TextureSample2("Texture Sample 2", 2D) = "white" {}
		_WindSizeHorizontal("Wind Size Horizontal", Range( 0 , 1)) = 0.25
		[HideInInspector]_TextureSample1("Texture Sample 1", 2D) = "white" {}
		_WindFrequency("Wind Frequency", Range( 0 , 0.75)) = 0.5
		_Float9("Float 9", Range( 0 , 10)) = 1
		_Float7("Float 7", Range( 0 , 10)) = 1
		_WindSizeVertical("Wind Size Vertical", Range( 0 , 1)) = 0
		[HideInInspector]_Float2("Float 2", Range( 0 , 1)) = 0.7
		[HideInInspector]_Float6("Float 6", Range( 0 , 1)) = 1
		[HideInInspector]_Float8("Float 8", Range( 0 , 10)) = 2
		[HideInInspector]_Float5("Float 5", Range( 0 , 1)) = 1
		[HideInInspector]_Float3("Float 3", Range( 0 , 1)) = 0.7
		[HideInInspector]_Vector1("Vector 1", Vector) = (0,-1,0,0)
		[HideInInspector]_Vector0("Vector 0", Vector) = (0,0,0,0)
		_MainTint("MainTint", Color) = (1,1,1,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "ForceNoShadowCasting" = "True" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
			float4 screenPosition;
		};

		uniform sampler2D _TextureSample2;
		uniform float4 _TextureSample2_ST;
		uniform float3 _Vector0;
		uniform sampler2D _TextureSample1;
		uniform float4 _TextureSample1_ST;
		uniform float _WindFrequency;
		uniform float _WindSizeHorizontal;
		uniform float _WindSizeVertical;
		uniform sampler2D _TextureSample0;
		uniform float4 _TextureSample0_ST;
		uniform float4 _MainTint;
		uniform float _Cutoff = 0.5;

		UNITY_INSTANCING_BUFFER_START(SullyFoliage)
			UNITY_DEFINE_INSTANCED_PROP(float3, _Vector1)
#define _Vector1_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float2)
#define _Float2_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float6)
#define _Float6_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float4)
#define _Float4_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float7)
#define _Float7_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float9)
#define _Float9_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float8)
#define _Float8_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float3)
#define _Float3_arr SullyFoliage
			UNITY_DEFINE_INSTANCED_PROP(float, _Float5)
#define _Float5_arr SullyFoliage
		UNITY_INSTANCING_BUFFER_END(SullyFoliage)


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
			float2 uv_TextureSample2 = v.texcoord * _TextureSample2_ST.xy + _TextureSample2_ST.zw;
			float4 temp_cast_1 = 1;
			float _Float2_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float2_arr, _Float2);
			float4 lerpResult52 = lerp( (tex2Dlod( _TextureSample2, float4( uv_TextureSample2, 0, 0.0) )).rgba , temp_cast_1 , ( 1.0 - _Float2_Instance ));
			float4 blendOpSrc59 = temp_cast_0;
			float4 blendOpDest59 = lerpResult52;
			float4 temp_cast_2 = 1;
			float _Float6_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float6_arr, _Float6);
			float4 lerpResult72 = lerp( ( saturate( ( blendOpSrc59 * blendOpDest59 ) )) , temp_cast_2 , ( 1.0 - _Float6_Instance ));
			float4 myVarName84 = lerpResult72;
			float _Float4_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float4_arr, _Float4);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 break23 = _Vector0;
			float4 appendResult33 = (float4(break23.x , 0 , break23.z , 0.0));
			float _Float7_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float7_arr, _Float7);
			float clampResult77 = clamp( ( _Float4_Instance - distance( float4( ase_worldPos , 0.0 ) , appendResult33 ) ) , 0.0 , _Float7_Instance );
			float3 break24 = _Vector0;
			float4 appendResult44 = (float4(break24.x , ase_worldPos.y , break24.z , 0.0));
			float4 normalizeResult73 = normalize( ( float4( ase_worldPos , 0.0 ) - appendResult44 ) );
			float _Float9_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float9_arr, _Float9);
			float myVarName89 = _Float9_Instance;
			float3 _Vector1_Instance = UNITY_ACCESS_INSTANCED_PROP(_Vector1_arr, _Vector1);
			float3 normalizeResult62 = normalize( -_Vector1_Instance );
			float _Float8_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float8_arr, _Float8);
			float3 myVarName86 = ( normalizeResult62 * _Float8_Instance );
			float4 temp_cast_7 = (v.texcoord.xy.y).xxxx;
			float2 uv_TextureSample1 = v.texcoord * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
			float4 temp_cast_8 = 1;
			float _Float3_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float3_arr, _Float3);
			float4 lerpResult51 = lerp( (tex2Dlod( _TextureSample1, float4( uv_TextureSample1, 0, 0.0) )).rgba , temp_cast_8 , ( 1.0 - _Float3_Instance ));
			float4 blendOpSrc65 = temp_cast_7;
			float4 blendOpDest65 = lerpResult51;
			float4 temp_cast_9 = 1;
			float _Float5_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float5_arr, _Float5);
			float4 lerpResult75 = lerp( ( saturate( ( blendOpSrc65 * blendOpDest65 ) )) , temp_cast_9 , ( 1.0 - _Float5_Instance ));
			float4 myVarName85 = lerpResult75;
			float4 GrassBendEffect99 = ( ( myVarName84 * ( clampResult77 * normalizeResult73 ) * myVarName89 ) + ( float4( myVarName86 , 0.0 ) * myVarName85 * -clampResult77 ) );
			float temp_output_57_0 = sin( ( ( ase_worldPos.x + _Time.y + ase_worldPos.z ) * 0.5 ) );
			float lerpResult69 = lerp( cos( ( ( v.texcoord.xy.y + _Time.y ) / ( 1.0 - _WindFrequency ) ) ) , 0.0 , ( 1.0 - v.texcoord.xy.y ));
			float temp_output_87_0 = ( ( temp_output_57_0 * lerpResult69 ) * (-0.5 + (( 1.0 - _WindSizeHorizontal ) - 0.0) * (0.0 - -0.5) / (1.0 - 0.0)) );
			float clampResult70 = clamp( ( 1.0 - v.texcoord.xy.y ) , 0.0 , 1.0 );
			float lerpResult82 = lerp( temp_output_57_0 , 0.0 , clampResult70);
			float4 appendResult92 = (float4(temp_output_87_0 , ( _WindSizeVertical * lerpResult82 ) , temp_output_87_0 , 0.0));
			float4 WindEffects97 = ( v.color.r * appendResult92 * v.color.b );
			float3 ase_vertex3Pos = v.vertex.xyz;
			v.vertex.xyz += ( ( GrassBendEffect99 + WindEffects97 ) + float4( ( 0 + ( ase_vertex3Pos * 0 ) ) , 0.0 ) ).rgb;
			float4 ase_screenPos = ComputeScreenPos( UnityObjectToClipPos( v.vertex ) );
			o.screenPosition = ase_screenPos;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 temp_cast_0 = (0.0).xxx;
			o.Normal = temp_cast_0;
			float2 uv_TextureSample0 = i.uv_texcoord * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
			float4 tex2DNode105 = tex2D( _TextureSample0, uv_TextureSample0 );
			o.Albedo = ( tex2DNode105 * _MainTint ).rgb;
			o.Alpha = 1;
			float4 ase_screenPos = i.screenPosition;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float2 clipScreen128 = ase_screenPosNorm.xy * _ScreenParams.xy;
			float dither128 = Dither4x4Bayer( fmod(clipScreen128.x, 4), fmod(clipScreen128.y, 4) );
			dither128 = step( dither128, tex2DNode105.a );
			clip( dither128 - _Cutoff );
		}

		ENDCG
	}
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
7;7;2546;1524;1675.919;-681.9901;1;True;True
Node;AmplifyShaderEditor.CommentaryNode;1;-5848.595,-2120.809;Float;False;6436.089;2644.614;;11;100;99;96;84;79;64;16;8;7;3;2;Grass Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;2;-2776.163,-538.5771;Float;False;2585.829;913.2986;Makes grass bend DOWN;11;94;86;85;83;74;62;56;45;27;13;11;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;3;-3783.086,-1926.973;Float;False;2110.774;598.0815;Makes grass bend AWAY;2;14;6;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;4;-4533.079,808.2704;Float;False;3072.81;1248.075;;29;97;95;93;92;88;87;82;81;80;78;70;69;68;57;54;53;50;49;43;39;38;36;32;22;18;12;10;9;5;Wind Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;9;-4420.353,1698.46;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;8;-4643.287,-1296.113;Float;False;1783.404;563.9719;;11;122;114;77;71;60;48;42;37;33;23;15;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector3Node;7;-5034.346,-633.3701;Float;False;Property;_Vector0;Vector 0;16;1;[HideInInspector];Create;False;0;0;False;0;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;11;-2696.429,-164.8191;Float;False;911.1997;490;Control grass bend based off gradient;6;51;30;28;26;21;19;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;6;-3745.188,-1861.112;Float;False;911.1997;490;Control grass bend based off gradient;6;52;40;34;29;20;17;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;10;-4459.876,1470.557;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;5;-4143.618,1909.219;Float;False;Property;_WindFrequency;Wind Frequency;6;0;Create;True;0;0;False;0;0.5;0.75;0;0.75;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;17;-3699.679,-1796.633;Float;True;Property;_TextureSample2;Texture Sample 2;3;1;[HideInInspector];Create;True;0;0;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;12;-4100.33,1265.348;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.CommentaryNode;13;-1760.504,-162.5461;Float;False;1096.607;490.4404;Hold grass roots in place;7;75;65;61;58;47;41;35;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;14;-2809.27,-1862.029;Float;False;1096.607;490.4404;Hold grass roots in place;7;72;67;63;59;55;46;25;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;20;-3599.912,-1505.316;Float;False;InstancedProperty;_Float2;Float 2;10;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;21;-2655.83,-97.30309;Float;True;Property;_TextureSample1;Texture Sample 1;5;1;[HideInInspector];Create;True;0;0;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;15;-4055.408,-1025.924;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;23;-4588.892,-966.473;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;19;-2551.15,190.9759;Float;False;InstancedProperty;_Float3;Float 3;14;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;22;-4008.651,1722.042;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;16;-3884.232,-697.7411;Float;False;1016.84;470.1794;;5;73;66;44;31;24;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;18;-3968.246,1541.204;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;24;-3819.61,-430.4751;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.SimpleDivideOpNode;39;-3764.035,1542.41;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;35;-1722.866,-88.05309;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;28;-2240.057,114.5259;Float;False;Constant;_Int2;Int 2;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.OneMinusNode;30;-2253.143,195.9759;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;36;-3833.186,1288.171;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;34;-3288.824,-1581.766;Float;False;Constant;_Int1;Int 1;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.DynamicAppendNode;33;-3821.531,-968.4032;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;25;-2741.809,-1547.922;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;40;-3301.911,-1500.316;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;31;-3777.087,-616.1191;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector3Node;27;-1466.804,-432.5392;Float;False;InstancedProperty;_Vector1;Vector 1;15;1;[HideInInspector];Create;True;0;0;False;0;0,-1,0;0,-1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;29;-3394.645,-1793.867;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;32;-3890.696,1429.855;Float;False;Constant;_WorldPositionVariationSpeed;World Position Variation Speed;3;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;37;-4561.958,-1129.428;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;26;-2345.882,-97.57408;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;38;-3939.967,921.6083;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;44;-3484.935,-430.0931;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ComponentMaskNode;46;-2546.088,-1799.211;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;55;-2492.956,-1467.117;Float;False;InstancedProperty;_Float6;Float 6;11;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;52;-3096.66,-1621.887;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.OneMinusNode;50;-3567.938,957.2495;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;54;-2911.748,1753.623;Float;False;Property;_WindSizeHorizontal;Wind Size Horizontal;4;0;Create;True;0;0;False;0;0.25;0.15;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;43;-3482.494,1255.655;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;45;-1264.574,-426.9501;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;41;-1444.191,232.3665;Float;False;InstancedProperty;_Float5;Float 5;13;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;51;-2047.895,74.40395;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DistanceOpNode;42;-3653.24,-1124.572;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;49;-3496.247,1790.605;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-3723.073,-1218.621;Float;False;InstancedProperty;_Float4;Float 4;2;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;47;-1497.32,-99.72716;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;53;-3497.214,1541.56;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;58;-1132.251,237.5723;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;57;-3259.673,1255.841;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;63;-2164.405,-1555.346;Float;False;Constant;_Int5;Int 5;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;66;-3278.694,-498.5092;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.OneMinusNode;68;-2923.375,1590.327;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;61;-1115.637,144.1378;Float;False;Constant;_Int6;Int 6;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;60;-3380.82,-1086.466;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;56;-1231.545,-340.5652;Float;False;InstancedProperty;_Float8;Float 8;12;1;[HideInInspector];Create;True;0;0;False;0;2;2;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;59;-2270.557,-1795.563;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;69;-3261.774,1541.852;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;64;-1267.161,-1076.257;Float;False;810.2354;329.3519;Multiply offset with gradients;3;91;89;76;;1,1,1,1;0;0
Node;AmplifyShaderEditor.NormalizeNode;62;-1113.959,-426.4512;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;67;-2181.019,-1461.911;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;65;-1221.791,-96.07909;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ClampOpNode;70;-3327.217,969.6703;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;71;-3376.324,-971.984;Float;False;InstancedProperty;_Float7;Float 7;8;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;78;-2732.065,1558.828;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;81;-2918.025,951.8129;Float;False;Property;_WindSizeVertical;Wind Size Vertical;9;0;Create;True;0;0;False;0;0;0.15;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-2912.486,1298.463;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;74;-888.1865,-359.6531;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;76;-1230.505,-889.6881;Float;False;InstancedProperty;_Float9;Float 9;7;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;73;-3071.684,-499.2172;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp;75;-921.3184,19.25295;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;82;-2910.61,1076.068;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;72;-1970.085,-1680.232;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;79;-2166.988,-1061.797;Float;False;250.1355;306.0909;Offset each vertex;1;90;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ClampOpNode;77;-3050.892,-1049.005;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;89;-947.0404,-889.0262;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;90;-2138.532,-999.0192;Float;True;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;87;-2543.468,1289.361;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;86;-689.6506,-366.4811;Float;False;myVarName;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-1299.134,-1363.077;Float;False;myVarName;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NegateNode;83;-1703.895,-260.0431;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;85;-603.643,13.47194;Float;False;myVarName;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-2547.288,1053.95;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;93;-2240.029,927.9014;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;91;-672.3626,-1019.415;Float;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;92;-2254.037,1207.985;Float;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;94;-355.8424,-309.1991;Float;False;3;3;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;-1958.687,951.5712;Float;True;3;3;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-25.04096,-858.6471;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;141;-983.3008,2053.675;Float;False;788;525;;5;146;145;144;143;142;Custom Billboarding;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;98;-1764.864,2170.114;Float;False;615;312;;3;110;102;101;Combine Wind + Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;97;-1701.343,1013.266;Float;False;WindEffects;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.PosVertexDataNode;142;-917.9899,2400.401;Float;False;0;0;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;99;236.8942,-863.4982;Float;False;GrassBendEffect;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;101;-1670.946,2363.401;Float;False;97;WindEffects;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;100;-5749.344,-2006.345;Float;False;1928.392;685.9498;Set the height of Player Object;11;125;124;121;119;116;113;111;109;107;106;104;;1,1,1,1;0;0
Node;AmplifyShaderEditor.BillboardNode;144;-744.5244,2282.401;Float;False;Spherical;True;0;1;FLOAT3;0
Node;AmplifyShaderEditor.ScaleNode;143;-709.4448,2400.283;Float;False;0;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;102;-1672.352,2249.1;Float;False;99;GrassBendEffect;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;145;-509.0385,2287.581;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;110;-1357.521,2285.771;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;103;-987.3063,1240.137;Float;False;Property;_MainTint;MainTint;17;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;104;-4999.751,-1866.406;Float;False;1146.685;514.6001;Recalculated Y;8;127;123;120;118;117;115;112;108;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;105;-1029.891,1015.684;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;9bc2b204a5a43894e8700e703b046de7;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;108;-4108.69,-1772.924;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;121;-5434.378,-1908.852;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;120;-4615.034,-1771.579;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;112;-4937.159,-1668.104;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;114;-4302.807,-1181.344;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;106;-5392.591,-1480.378;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-5655.738,-1743.622;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;122;-4300.409,-1024.924;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;119;-5270.347,-1706.05;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;107;-5701.246,-1851.194;Float;False;Constant;_Float0;Float 0;10;1;[HideInInspector];Create;True;0;0;False;0;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-631.8245,1175.971;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;140;-342.1425,1315.947;Float;False;Constant;_Float10;Float 10;18;0;Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;117;-4616.714,-1690.298;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;111;-5667.562,-1520.202;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SimpleAddOpNode;146;-339.6934,2174.141;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;128;-494.6504,1421.725;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;113;-5274.778,-1915.154;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;116;-5681.852,-1646.577;Float;False;Constant;_Float1;Float 1;8;1;[HideInInspector];Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;115;-4938.453,-1581.005;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;123;-4638.155,-1583.605;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;109;-5430.977,-1700.104;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;125;-5652.417,-1933.196;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;127;-4380.153,-1766.89;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;118;-4939.754,-1490.005;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;-109.3176,1240.194;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/Foliage;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;False;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;False;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;True;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;23;0;7;0
WireConnection;22;0;5;0
WireConnection;18;0;9;2
WireConnection;18;1;10;2
WireConnection;24;0;7;0
WireConnection;39;0;18;0
WireConnection;39;1;22;0
WireConnection;30;0;19;0
WireConnection;36;0;12;1
WireConnection;36;1;10;2
WireConnection;36;2;12;3
WireConnection;33;0;23;0
WireConnection;33;1;15;0
WireConnection;33;2;23;2
WireConnection;40;0;20;0
WireConnection;29;0;17;0
WireConnection;26;0;21;0
WireConnection;44;0;24;0
WireConnection;44;1;31;2
WireConnection;44;2;24;2
WireConnection;46;0;25;2
WireConnection;52;0;29;0
WireConnection;52;1;34;0
WireConnection;52;2;40;0
WireConnection;50;0;38;2
WireConnection;43;0;36;0
WireConnection;43;1;32;0
WireConnection;45;0;27;0
WireConnection;51;0;26;0
WireConnection;51;1;28;0
WireConnection;51;2;30;0
WireConnection;42;0;37;0
WireConnection;42;1;33;0
WireConnection;49;0;9;2
WireConnection;47;0;35;2
WireConnection;53;0;39;0
WireConnection;58;0;41;0
WireConnection;57;0;43;0
WireConnection;66;0;31;0
WireConnection;66;1;44;0
WireConnection;68;0;54;0
WireConnection;60;0;48;0
WireConnection;60;1;42;0
WireConnection;59;0;46;0
WireConnection;59;1;52;0
WireConnection;69;0;53;0
WireConnection;69;2;49;0
WireConnection;62;0;45;0
WireConnection;67;0;55;0
WireConnection;65;0;47;0
WireConnection;65;1;51;0
WireConnection;70;0;50;0
WireConnection;78;0;68;0
WireConnection;80;0;57;0
WireConnection;80;1;69;0
WireConnection;74;0;62;0
WireConnection;74;1;56;0
WireConnection;73;0;66;0
WireConnection;75;0;65;0
WireConnection;75;1;61;0
WireConnection;75;2;58;0
WireConnection;82;0;57;0
WireConnection;82;2;70;0
WireConnection;72;0;59;0
WireConnection;72;1;63;0
WireConnection;72;2;67;0
WireConnection;77;0;60;0
WireConnection;77;2;71;0
WireConnection;89;0;76;0
WireConnection;90;0;77;0
WireConnection;90;1;73;0
WireConnection;87;0;80;0
WireConnection;87;1;78;0
WireConnection;86;0;74;0
WireConnection;84;0;72;0
WireConnection;83;0;77;0
WireConnection;85;0;75;0
WireConnection;88;0;81;0
WireConnection;88;1;82;0
WireConnection;91;0;84;0
WireConnection;91;1;90;0
WireConnection;91;2;89;0
WireConnection;92;0;87;0
WireConnection;92;1;88;0
WireConnection;92;2;87;0
WireConnection;94;0;86;0
WireConnection;94;1;85;0
WireConnection;94;2;83;0
WireConnection;95;0;93;1
WireConnection;95;1;92;0
WireConnection;95;2;93;3
WireConnection;96;0;91;0
WireConnection;96;1;94;0
WireConnection;97;0;95;0
WireConnection;99;0;96;0
WireConnection;143;0;142;0
WireConnection;145;0;144;0
WireConnection;145;1;143;0
WireConnection;110;0;102;0
WireConnection;110;1;101;0
WireConnection;108;0;127;0
WireConnection;121;0;125;0
WireConnection;121;1;107;0
WireConnection;114;0;37;2
WireConnection;106;0;111;2
WireConnection;122;0;23;1
WireConnection;119;0;109;0
WireConnection;126;0;105;0
WireConnection;126;1;103;0
WireConnection;146;0;110;0
WireConnection;146;1;145;0
WireConnection;128;0;105;4
WireConnection;113;0;121;0
WireConnection;123;0;112;0
WireConnection;123;1;115;0
WireConnection;123;2;115;0
WireConnection;123;3;118;0
WireConnection;109;0;124;0
WireConnection;109;1;116;0
WireConnection;127;0;120;0
WireConnection;127;1;117;0
WireConnection;127;2;117;0
WireConnection;127;3;123;0
WireConnection;0;0;126;0
WireConnection;0;1;140;0
WireConnection;0;10;128;0
WireConnection;0;11;146;0
ASEEND*/
//CHKSM=FD1B75FC4370BE7E5DFC8FC380D11FA5D5AADA54