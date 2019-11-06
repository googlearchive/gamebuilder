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

// Upgrade NOTE: upgraded instancing buffer 'SullyFoliageNoBillboard' to new syntax.

// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Sully/FoliageNoBillboard"
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
		_MainTint("MainTint", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
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

		UNITY_INSTANCING_BUFFER_START(SullyFoliageNoBillboard)
			UNITY_DEFINE_INSTANCED_PROP(float3, _Vector1)
#define _Vector1_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float2)
#define _Float2_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float6)
#define _Float6_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float4)
#define _Float4_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float7)
#define _Float7_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float9)
#define _Float9_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float8)
#define _Float8_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float3)
#define _Float3_arr SullyFoliageNoBillboard
			UNITY_DEFINE_INSTANCED_PROP(float, _Float5)
#define _Float5_arr SullyFoliageNoBillboard
		UNITY_INSTANCING_BUFFER_END(SullyFoliageNoBillboard)

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float4 temp_cast_0 = (v.texcoord.xy.y).xxxx;
			float2 uv_TextureSample2 = v.texcoord * _TextureSample2_ST.xy + _TextureSample2_ST.zw;
			float4 temp_cast_1 = 1;
			float _Float2_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float2_arr, _Float2);
			float4 lerpResult56 = lerp( (tex2Dlod( _TextureSample2, float4( uv_TextureSample2, 0, 0.0) )).rgba , temp_cast_1 , ( 1.0 - _Float2_Instance ));
			float4 blendOpSrc70 = temp_cast_0;
			float4 blendOpDest70 = lerpResult56;
			float4 temp_cast_2 = 1;
			float _Float6_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float6_arr, _Float6);
			float4 lerpResult75 = lerp( ( saturate( ( blendOpSrc70 * blendOpDest70 ) )) , temp_cast_2 , ( 1.0 - _Float6_Instance ));
			float4 myVarName86 = lerpResult75;
			float _Float4_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float4_arr, _Float4);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float3 break5 = _Vector0;
			float4 appendResult51 = (float4(break5.x , 0 , break5.z , 0.0));
			float _Float7_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float7_arr, _Float7);
			float clampResult81 = clamp( ( _Float4_Instance - distance( float4( ase_worldPos , 0.0 ) , appendResult51 ) ) , 0.0 , _Float7_Instance );
			float3 break52 = _Vector0;
			float4 appendResult57 = (float4(break52.x , ase_worldPos.y , break52.z , 0.0));
			float4 normalizeResult77 = normalize( ( float4( ase_worldPos , 0.0 ) - appendResult57 ) );
			float _Float9_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float9_arr, _Float9);
			float myVarName84 = _Float9_Instance;
			float3 _Vector1_Instance = UNITY_ACCESS_INSTANCED_PROP(_Vector1_arr, _Vector1);
			float3 normalizeResult68 = normalize( -_Vector1_Instance );
			float _Float8_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float8_arr, _Float8);
			float3 myVarName87 = ( normalizeResult68 * _Float8_Instance );
			float4 temp_cast_7 = (v.texcoord.xy.y).xxxx;
			float2 uv_TextureSample1 = v.texcoord * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
			float4 temp_cast_8 = 1;
			float _Float3_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float3_arr, _Float3);
			float4 lerpResult54 = lerp( (tex2Dlod( _TextureSample1, float4( uv_TextureSample1, 0, 0.0) )).rgba , temp_cast_8 , ( 1.0 - _Float3_Instance ));
			float4 blendOpSrc66 = temp_cast_7;
			float4 blendOpDest66 = lerpResult54;
			float4 temp_cast_9 = 1;
			float _Float5_Instance = UNITY_ACCESS_INSTANCED_PROP(_Float5_arr, _Float5);
			float4 lerpResult78 = lerp( ( saturate( ( blendOpSrc66 * blendOpDest66 ) )) , temp_cast_9 , ( 1.0 - _Float5_Instance ));
			float4 myVarName82 = lerpResult78;
			float4 GrassBendEffect91 = ( ( myVarName86 * ( clampResult81 * normalizeResult77 ) * myVarName84 ) + ( float4( myVarName87 , 0.0 ) * myVarName82 * -clampResult81 ) );
			float temp_output_110_0 = sin( ( ( ase_worldPos.x + _Time.y + ase_worldPos.z ) * 0.5 ) );
			float lerpResult108 = lerp( cos( ( ( v.texcoord.xy.y + _Time.y ) / ( 1.0 - _WindFrequency ) ) ) , 0.0 , ( 1.0 - v.texcoord.xy.y ));
			float temp_output_116_0 = ( ( temp_output_110_0 * lerpResult108 ) * (-0.5 + (( 1.0 - _WindSizeHorizontal ) - 0.0) * (0.0 - -0.5) / (1.0 - 0.0)) );
			float clampResult109 = clamp( ( 1.0 - v.texcoord.xy.y ) , 0.0 , 1.0 );
			float lerpResult114 = lerp( temp_output_110_0 , 0.0 , clampResult109);
			float4 appendResult119 = (float4(temp_output_116_0 , ( _WindSizeVertical * lerpResult114 ) , temp_output_116_0 , 0.0));
			float4 WindEffects121 = ( v.color.r * appendResult119 * v.color.b );
			v.vertex.xyz += ( GrassBendEffect91 + WindEffects121 ).rgb;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TextureSample0 = i.uv_texcoord * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
			float4 tex2DNode1 = tex2D( _TextureSample0, uv_TextureSample0 );
			o.Albedo = ( tex2DNode1 * _MainTint ).rgb;
			o.Alpha = 1;
			clip( tex2DNode1.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
1008;1097;1356;985;1370.489;522.485;1.603026;True;True
Node;AmplifyShaderEditor.CommentaryNode;2;-5904.213,-3323.226;Float;False;6436.089;2644.614;;11;91;90;86;76;67;39;28;27;6;4;3;Grass Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;27;-2831.781,-1740.994;Float;False;2585.829;913.2986;Makes grass bend DOWN;11;88;87;85;82;80;74;68;59;47;36;31;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;28;-3838.704,-3129.39;Float;False;2110.774;598.0815;Makes grass bend AWAY;2;34;30;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;92;-4560.855,239.5779;Float;False;3072.81;1248.075;;29;121;120;119;118;117;116;115;114;113;112;111;110;109;108;107;106;105;104;103;102;101;100;99;98;97;96;95;94;93;Wind Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;3;-4698.905,-2498.53;Float;False;1783.404;563.9719;;11;81;72;69;60;58;51;40;18;11;7;5;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;31;-2752.047,-1367.236;Float;False;911.1997;490;Control grass bend based off gradient;6;54;48;43;41;38;33;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;30;-3800.806,-3063.529;Float;False;911.1997;490;Control grass bend based off gradient;6;56;49;46;44;37;35;;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;95;-4487.653,901.8647;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;93;-4171.394,1340.525;Float;False;Property;_WindFrequency;Wind Frequency;6;0;Create;True;0;0;False;0;0.5;0.75;0;0.75;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector3Node;4;-5089.964,-1835.787;Float;False;Property;_Vector0;Vector 0;16;1;[HideInInspector];Create;False;0;0;False;0;0,0,0;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TextureCoordinatesNode;94;-4448.129,1129.767;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;33;-2711.448,-1299.72;Float;True;Property;_TextureSample1;Texture Sample 1;5;1;[HideInInspector];Create;True;0;0;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;97;-3996.023,972.5115;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;38;-2606.768,-1011.441;Float;False;InstancedProperty;_Float3;Float 3;14;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;5;-4644.51,-2168.89;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.OneMinusNode;96;-4036.428,1153.349;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;37;-3655.53,-2707.733;Float;False;InstancedProperty;_Float2;Float 2;10;1;[HideInInspector];Create;True;0;0;False;0;0.7;0.7;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;34;-2864.888,-3064.446;Float;False;1096.607;490.4404;Hold grass roots in place;7;75;71;70;64;62;55;50;;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;36;-1816.122,-1364.963;Float;False;1096.607;490.4404;Hold grass roots in place;7;78;73;66;65;61;53;42;;1,1,1,1;0;0
Node;AmplifyShaderEditor.WorldPosInputsNode;98;-4128.106,696.656;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;35;-3755.297,-2999.05;Float;True;Property;_TextureSample2;Texture Sample 2;3;1;[HideInInspector];Create;True;0;0;True;0;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;40;-4111.026,-2228.341;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;39;-3939.85,-1900.158;Float;False;1016.84;470.1794;;5;77;63;57;52;45;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleAddOpNode;100;-3860.963,719.4783;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.WorldPosInputsNode;45;-3832.705,-1818.536;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.IntNode;44;-3344.442,-2784.183;Float;False;Constant;_Int1;Int 1;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.OneMinusNode;49;-3357.529,-2702.733;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;101;-3791.812,973.7175;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;102;-3967.744,352.9156;Float;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;11;-4617.575,-2331.845;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.TexCoordVertexDataNode;42;-1778.484,-1290.47;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.OneMinusNode;43;-2308.761,-1006.441;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;48;-2295.675,-1087.891;Float;False;Constant;_Int2;Int 2;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.Vector3Node;47;-1522.422,-1634.956;Float;False;InstancedProperty;_Vector1;Vector 1;15;1;[HideInInspector];Create;True;0;0;False;0;0,-1,0;0,-1,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.ComponentMaskNode;41;-2401.5,-1299.991;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.TexCoordVertexDataNode;50;-2797.427,-2750.339;Float;False;0;2;0;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.BreakToComponentsNode;52;-3875.228,-1632.892;Float;False;FLOAT3;1;0;FLOAT3;0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.RangedFloatNode;99;-3918.473,861.1626;Float;False;Constant;_WorldPositionVariationSpeed;World Position Variation Speed;3;0;Create;True;0;0;False;0;0.5;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;46;-3450.263,-2996.284;Float;True;True;True;True;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;51;-3877.149,-2170.82;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ComponentMaskNode;55;-2601.706,-3001.628;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;106;-3524.024,1221.912;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ComponentMaskNode;53;-1552.938,-1302.144;Float;True;True;True;True;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;62;-2548.574,-2669.534;Float;False;InstancedProperty;_Float6;Float 6;11;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;105;-2939.525,1184.93;Float;False;Property;_WindSizeHorizontal;Wind Size Horizontal;4;0;Create;True;0;0;False;0;0.25;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;103;-3524.991,972.8674;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;54;-2103.513,-1128.013;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;56;-3152.278,-2824.304;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;104;-3510.271,686.9631;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;57;-3540.553,-1632.51;Float;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DistanceOpNode;58;-3708.858,-2326.989;Float;True;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;61;-1499.809,-970.0504;Float;False;InstancedProperty;_Float5;Float 5;13;1;[HideInInspector];Create;True;0;0;False;0;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;60;-3778.691,-2421.038;Float;False;InstancedProperty;_Float4;Float 4;2;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;107;-3595.715,388.5569;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NegateNode;59;-1320.192,-1629.367;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.ClampOpNode;109;-3354.994,400.9777;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;63;-3334.312,-1700.926;Float;False;2;0;FLOAT3;0,0,0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RangedFloatNode;69;-3431.942,-2174.401;Float;False;InstancedProperty;_Float7;Float 7;8;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;64;-2236.637,-2664.328;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;67;-1322.779,-2278.674;Float;False;810.2354;329.3519;Multiply offset with gradients;3;89;84;79;;1,1,1,1;0;0
Node;AmplifyShaderEditor.OneMinusNode;111;-2951.152,1021.634;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;108;-3289.551,973.1594;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;66;-1277.409,-1298.496;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.IntNode;73;-1171.255,-1058.279;Float;False;Constant;_Int6;Int 6;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.SinOpNode;110;-3287.45,687.1492;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;71;-2220.023,-2757.763;Float;False;Constant;_Int5;Int 5;10;0;Create;True;0;0;False;0;1;0;0;1;INT;0
Node;AmplifyShaderEditor.RangedFloatNode;74;-1287.163,-1542.982;Float;False;InstancedProperty;_Float8;Float 8;12;1;[HideInInspector];Create;True;0;0;False;0;2;2;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;72;-3436.438,-2288.883;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;65;-1187.869,-964.8446;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;70;-2326.175,-2997.98;Float;True;Multiply;True;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.NormalizeNode;68;-1169.577,-1628.868;Float;False;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;115;-2940.263,729.7711;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;80;-943.8041,-1562.07;Float;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.CommentaryNode;76;-2222.606,-2264.214;Float;False;250.1355;306.0909;Offset each vertex;1;83;;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;114;-2938.387,507.3759;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCRemapNode;113;-2759.842,990.1348;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;75;-2025.703,-2882.649;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;79;-1286.123,-2092.105;Float;False;InstancedProperty;_Float9;Float 9;7;0;Create;True;0;0;False;0;1;1;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.NormalizeNode;77;-3127.302,-1701.634;Float;False;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ClampOpNode;81;-3106.51,-2251.422;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;78;-976.936,-1183.164;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;112;-2945.802,383.1204;Float;False;Property;_WindSizeVertical;Wind Size Vertical;9;0;Create;True;0;0;False;0;0;0.01;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;84;-1002.658,-2091.443;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;117;-2575.065,485.2577;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;116;-2571.245,720.6692;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;-2194.15,-2201.436;Float;True;2;2;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;82;-659.2606,-1188.945;Float;False;myVarName;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;86;-1354.752,-2565.494;Float;False;myVarName;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;87;-745.2682,-1568.898;Float;False;myVarName;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.NegateNode;85;-1759.513,-1462.46;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;89;-727.9802,-2221.832;Float;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DynamicAppendNode;119;-2281.814,639.2933;Float;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.VertexColorNode;118;-2267.806,359.2087;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;-411.4601,-1511.616;Float;False;3;3;0;FLOAT3;0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;90;-80.6586,-2061.064;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;120;-1986.464,382.8787;Float;True;3;3;0;FLOAT;0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;121;-1729.12,444.574;Float;False;WindEffects;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.CommentaryNode;122;-971.2256,539.4099;Float;False;615;312;;3;125;124;123;Combine Wind + Bend Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;91;181.2766,-2065.915;Float;False;GrassBendEffect;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;6;-5804.962,-3208.762;Float;False;1928.392;685.9498;Set the height of Player Object;11;23;19;17;16;15;14;13;12;10;9;8;;1,1,1,1;0;0
Node;AmplifyShaderEditor.ColorNode;127;-1042.924,37.7206;Float;False;Property;_MainTint;MainTint;17;0;Create;True;0;0;False;0;0,0,0,0;1,1,1,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;16;-5055.369,-3068.823;Float;False;1146.685;514.6001;Recalculated Y;8;32;29;26;25;24;22;21;20;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;1;-1085.509,-186.733;Float;True;Property;_TextureSample0;Texture Sample 0;1;0;Create;True;0;0;False;0;None;fd68abcdaa6e05d49ade00c1386a41d1;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;123;-877.309,732.6962;Float;False;121;WindEffects;1;0;OBJECT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode;124;-878.715,618.3953;Float;False;91;GrassBendEffect;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;126;-687.4421,-26.44583;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;9;-5708.035,-3135.613;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;14;-5711.356,-2946.039;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;22;-4995.372,-2692.422;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;24;-4693.773,-2786.022;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;10;-5489.996,-3111.269;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;26;-4670.652,-2973.996;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;23;-5325.965,-2908.467;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;7;-4356.027,-2227.341;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;25;-4672.332,-2892.715;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TFHCCompareLower;29;-4435.771,-2969.307;Float;False;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;21;-4994.071,-2783.422;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;15;-5486.594,-2902.521;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;17;-5448.209,-2682.795;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;8;-5756.864,-3053.611;Float;False;Constant;_Float0;Float 0;10;1;[HideInInspector];Create;True;0;0;False;0;-1;-1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;32;-4164.308,-2975.341;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;18;-4358.425,-2383.761;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;125;-563.8831,655.0663;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.WorldPosInputsNode;13;-5723.179,-2722.619;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.GetLocalVarNode;20;-4992.777,-2870.521;Float;False;-1;;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;19;-5330.396,-3117.571;Float;False;myVarName;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;12;-5737.469,-2848.994;Float;False;Constant;_Float1;Float 1;8;1;[HideInInspector];Create;True;0;0;False;0;0;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/FoliageNoBillboard;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;97;0;94;2
WireConnection;97;1;95;2
WireConnection;5;0;4;0
WireConnection;96;0;93;0
WireConnection;100;0;98;1
WireConnection;100;1;95;2
WireConnection;100;2;98;3
WireConnection;49;0;37;0
WireConnection;101;0;97;0
WireConnection;101;1;96;0
WireConnection;43;0;38;0
WireConnection;41;0;33;0
WireConnection;52;0;4;0
WireConnection;46;0;35;0
WireConnection;51;0;5;0
WireConnection;51;1;40;0
WireConnection;51;2;5;2
WireConnection;55;0;50;2
WireConnection;106;0;94;2
WireConnection;53;0;42;2
WireConnection;103;0;101;0
WireConnection;54;0;41;0
WireConnection;54;1;48;0
WireConnection;54;2;43;0
WireConnection;56;0;46;0
WireConnection;56;1;44;0
WireConnection;56;2;49;0
WireConnection;104;0;100;0
WireConnection;104;1;99;0
WireConnection;57;0;52;0
WireConnection;57;1;45;2
WireConnection;57;2;52;2
WireConnection;58;0;11;0
WireConnection;58;1;51;0
WireConnection;107;0;102;2
WireConnection;59;0;47;0
WireConnection;109;0;107;0
WireConnection;63;0;45;0
WireConnection;63;1;57;0
WireConnection;64;0;62;0
WireConnection;111;0;105;0
WireConnection;108;0;103;0
WireConnection;108;2;106;0
WireConnection;66;0;53;0
WireConnection;66;1;54;0
WireConnection;110;0;104;0
WireConnection;72;0;60;0
WireConnection;72;1;58;0
WireConnection;65;0;61;0
WireConnection;70;0;55;0
WireConnection;70;1;56;0
WireConnection;68;0;59;0
WireConnection;115;0;110;0
WireConnection;115;1;108;0
WireConnection;80;0;68;0
WireConnection;80;1;74;0
WireConnection;114;0;110;0
WireConnection;114;2;109;0
WireConnection;113;0;111;0
WireConnection;75;0;70;0
WireConnection;75;1;71;0
WireConnection;75;2;64;0
WireConnection;77;0;63;0
WireConnection;81;0;72;0
WireConnection;81;2;69;0
WireConnection;78;0;66;0
WireConnection;78;1;73;0
WireConnection;78;2;65;0
WireConnection;84;0;79;0
WireConnection;117;0;112;0
WireConnection;117;1;114;0
WireConnection;116;0;115;0
WireConnection;116;1;113;0
WireConnection;83;0;81;0
WireConnection;83;1;77;0
WireConnection;82;0;78;0
WireConnection;86;0;75;0
WireConnection;87;0;80;0
WireConnection;85;0;81;0
WireConnection;89;0;86;0
WireConnection;89;1;83;0
WireConnection;89;2;84;0
WireConnection;119;0;116;0
WireConnection;119;1;117;0
WireConnection;119;2;116;0
WireConnection;88;0;87;0
WireConnection;88;1;82;0
WireConnection;88;2;85;0
WireConnection;90;0;89;0
WireConnection;90;1;88;0
WireConnection;120;0;118;1
WireConnection;120;1;119;0
WireConnection;120;2;118;3
WireConnection;121;0;120;0
WireConnection;91;0;90;0
WireConnection;126;0;1;0
WireConnection;126;1;127;0
WireConnection;24;0;20;0
WireConnection;24;1;21;0
WireConnection;24;2;21;0
WireConnection;24;3;22;0
WireConnection;10;0;9;0
WireConnection;10;1;8;0
WireConnection;23;0;15;0
WireConnection;7;0;5;1
WireConnection;29;0;26;0
WireConnection;29;1;25;0
WireConnection;29;2;25;0
WireConnection;29;3;24;0
WireConnection;15;0;14;0
WireConnection;15;1;12;0
WireConnection;17;0;13;2
WireConnection;32;0;29;0
WireConnection;18;0;11;2
WireConnection;125;0;124;0
WireConnection;125;1;123;0
WireConnection;19;0;10;0
WireConnection;0;0;126;0
WireConnection;0;10;1;4
WireConnection;0;11;125;0
ASEEND*/
//CHKSM=DA38EB77CB006D18C52768E537BD0EFF493A803A