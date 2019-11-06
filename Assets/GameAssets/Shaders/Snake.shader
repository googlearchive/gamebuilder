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
Shader "Sully/Snake"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_Texture("Texture", 2D) = "white" {}
		_TinkMask("Tink Mask", 2D) = "white" {}
		_MainTint("MainTint", Color) = (0,0,0,0)
		[HideInInspector]_Float0("Float 0", Range( 0 , 5)) = 5
		[HideInInspector]_Float2("Float 2", Range( 0 , 5)) = 5
		_Running("Running", Int) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float2 uv_texcoord;
		};

		uniform int _Running;
		uniform float _Float0;
		uniform float _Float2;
		uniform sampler2D _TinkMask;
		uniform float4 _TinkMask_ST;
		uniform float4 _MainTint;
		uniform sampler2D _Texture;
		uniform float4 _Texture_ST;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float temp_output_60_0 = ( _Time.y * _Running );
			float4 appendResult55 = (float4(( ( sin( ( ( ase_worldPos.x + temp_output_60_0 + ase_worldPos.z ) * 2.0 ) ) * cos( ( ( v.texcoord1.xy.y + temp_output_60_0 ) / ( 1.0 - _Float0 ) ) ) ) * (-0.5 + (( 1.0 - _Float2 ) - 0.0) * (0.0 - -0.5) / (1.0 - 0.0)) ) , 0.0 , 0.0 , 0.0));
			v.vertex.xyz += appendResult55.xyz;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TinkMask = i.uv_texcoord * _TinkMask_ST.xy + _TinkMask_ST.zw;
			float2 uv_Texture = i.uv_texcoord * _Texture_ST.xy + _Texture_ST.zw;
			float4 tex2DNode5 = tex2D( _Texture, uv_Texture );
			o.Albedo = ( ( tex2D( _TinkMask, uv_TinkMask ) * _MainTint ) + tex2DNode5 ).rgb;
			o.Alpha = 1;
			clip( tex2DNode5.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
2567;262;1844;1050;3637.481;435.7328;2.530365;True;True
Node;AmplifyShaderEditor.CommentaryNode;38;-2869.333,395.8375;Float;False;2746.393;812.8521;;20;39;40;60;58;59;42;43;48;47;49;53;54;51;41;45;44;46;50;52;55;Jiggle Effects;1,1,1,1;0;0
Node;AmplifyShaderEditor.TimeNode;58;-2833.082,833.1169;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.IntNode;59;-2778.164,993.1684;Float;False;Property;_Running;Running;6;0;Create;True;0;0;False;0;1;1;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;60;-2555.417,899.6283;Float;False;2;2;0;FLOAT;0;False;1;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;40;-2607.915,549.8784;Float;True;1;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.WorldPosInputsNode;41;-2210.604,524.2792;Float;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;39;-2566.365,1086.495;Float;False;Property;_Float0;Float 0;4;1;[HideInInspector];Create;True;0;0;False;0;5;0.75;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;44;-2117.9,700.9692;Float;False;Constant;_Float1;Float 1;3;1;[HideInInspector];Create;True;0;0;False;0;2;0.5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;45;-1983.609,547.1014;Float;False;3;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;42;-2261.981,826.9536;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;43;-2257.99,1090.797;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;46;-1752.078,602.6019;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleDivideOpNode;48;-2019.164,826.859;Float;True;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;47;-1412.006,902.3151;Float;False;Property;_Float2;Float 2;5;1;[HideInInspector];Create;True;0;0;False;0;5;0.15;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SinOpNode;50;-1397.669,464.5052;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CosOpNode;51;-1710.269,826.2275;Float;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.OneMinusNode;49;-1118.875,906.2785;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;2;-1452.254,-536.2444;Float;True;Property;_TinkMask;Tink Mask;2;0;Create;True;0;0;False;0;None;faf9572c1bbe4db429c8c8ac7e341ad8;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;1;-1365.954,-326.1446;Float;False;Property;_MainTint;MainTint;3;0;Create;True;0;0;False;0;0,0,0,0;0.5943396,0,0,0;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;53;-940.2181,907.6733;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;-0.5;False;4;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;52;-983.0592,496.3523;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;54;-684.6333,746.1027;Float;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;3;-1095.286,-421.4622;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;5;-1043.673,19.2194;Float;True;Property;_Texture;Texture;1;0;Create;True;0;0;False;0;None;79b907c88285bce45ae34870068570bc;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.DynamicAppendNode;55;-365.0981,491.9065;Float;True;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;7;-569.8163,-237.1555;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;0,0;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Sully/Snake;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;60;0;58;2
WireConnection;60;1;59;0
WireConnection;45;0;41;1
WireConnection;45;1;60;0
WireConnection;45;2;41;3
WireConnection;42;0;40;2
WireConnection;42;1;60;0
WireConnection;43;0;39;0
WireConnection;46;0;45;0
WireConnection;46;1;44;0
WireConnection;48;0;42;0
WireConnection;48;1;43;0
WireConnection;50;0;46;0
WireConnection;51;0;48;0
WireConnection;49;0;47;0
WireConnection;53;0;49;0
WireConnection;52;0;50;0
WireConnection;52;1;51;0
WireConnection;54;0;52;0
WireConnection;54;1;53;0
WireConnection;3;0;2;0
WireConnection;3;1;1;0
WireConnection;55;0;54;0
WireConnection;7;0;3;0
WireConnection;7;1;5;0
WireConnection;0;0;7;0
WireConnection;0;10;5;4
WireConnection;0;11;55;0
ASEEND*/
//CHKSM=87851AAB5D47A57B13FE5B5BE7610FE687727D66