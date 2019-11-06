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
Shader "Sully/Pixel Water"
{
	Properties
	{
		_Color("MainTint", Color) = (0,0,0,0)
		_WaterNormal("Water Normal", 2D) = "bump" {}
		_WaterDistortionIntensity("Water Distortion Intensity", Range( 0 , 1)) = -1
		_SecondaryTint("SecondaryTint", Color) = (1,1,1,0)
		_TextureWaveSpeed("Texture Wave Speed", Range( 0 , 1)) = 0
		_VertexWaveHeight("Vertex Wave Height", Range( 0 , 0.15)) = 0.1
		_VertexWaveSpeed("Vertex Wave Speed", Range( 1 , 10)) = 5
		_ReflectionCubemap("Reflection Cubemap", CUBE) = "white" {}
		_ReflectionIntensity("Reflection Intensity", Range( 0 , 1)) = 0.75
		_WaterCaustics("Water Caustics", 2D) = "white" {}
		_WaterCaustics2("Water Caustics 2", 2D) = "white" {}
		_CausticIntensity("Caustic Intensity", Range( 0 , 1)) = 0.5
		_CausticSpeed("Caustic Speed", Range( 0 , 0.75)) = 0
		_WaterSeamMask("Water Seam Mask", 2D) = "white" {}
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#pragma target 3.0
		#pragma surface surf StandardSpecular keepalpha exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			half2 uv_texcoord;
			float4 screenPos;
			float3 worldRefl;
			INTERNAL_DATA
		};

		uniform sampler2D _WaterSeamMask;
		uniform float4 _WaterSeamMask_ST;
		uniform half _VertexWaveSpeed;
		uniform half _VertexWaveHeight;
		uniform half _TextureWaveSpeed;
		uniform sampler2D _WaterNormal;
		uniform sampler2D _WaterCaustics;
		uniform half _CausticSpeed;
		uniform sampler2D _WaterCaustics2;
		uniform half _CausticIntensity;
		uniform sampler2D _CameraDepthTexture;
		uniform sampler2D _GrabTexture;
		uniform half _WaterDistortionIntensity;
		uniform half4 _Color;
		uniform samplerCUBE _ReflectionCubemap;
		uniform half _ReflectionIntensity;
		uniform half4 _SecondaryTint;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float2 uv_WaterSeamMask = v.texcoord * _WaterSeamMask_ST.xy + _WaterSeamMask_ST.zw;
			float simplePerlin2D202 = snoise( ( v.texcoord.xy + ( _Time.x * _VertexWaveSpeed ) ) );
			float4 lerpResult210 = lerp( float4( 0,0,0,0 ) , tex2Dlod( _WaterSeamMask, half4( uv_WaterSeamMask, 0, 0.0) ) , (( _VertexWaveHeight * 0.0 ) + (simplePerlin2D202 - 0.0) * (_VertexWaveHeight - ( _VertexWaveHeight * 0.0 )) / (1.0 - 0.0)));
			float4 VertexOffset286 = lerpResult210;
			v.vertex.xyz += VertexOffset286.rgb;
		}

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 panner22 = ( _Time.x * ( half2( 5,0 ) * _TextureWaveSpeed ) + i.uv_texcoord);
			half3 tex2DNode23 = UnpackScaleNormal( tex2D( _WaterNormal, panner22 ), 0.025 );
			float2 panner238 = ( _Time.x * ( half2( 0,5 ) * _TextureWaveSpeed ) + i.uv_texcoord);
			float3 Waves275 = BlendNormals( tex2DNode23 , UnpackScaleNormal( tex2D( _WaterNormal, panner238 ), 0.025 ) );
			o.Normal = Waves275;
			float2 panner389 = ( _Time.x * ( half2( 4,2 ) * _CausticSpeed ) + i.uv_texcoord);
			float2 panner397 = ( _Time.x * ( half2( -10,-20 ) * _CausticSpeed ) + i.uv_texcoord);
			float4 Caustics454 = ( ( tex2D( _WaterCaustics, panner389 ) * tex2D( _WaterCaustics2, panner397 ) ) * _CausticIntensity );
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float eyeDepth1 = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD( ase_screenPos ))));
			float temp_output_89_0 = abs( ( eyeDepth1 - ase_screenPos.w ) );
			float lerpResult13 = lerp( 0.0 , 0.0 , saturate( pow( ( -10.0 + temp_output_89_0 ) , -0.5 ) ));
			float WaterMainAlbedo335 = lerpResult13;
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float2 uv_WaterSeamMask = i.uv_texcoord * _WaterSeamMask_ST.xy + _WaterSeamMask_ST.zw;
			float4 lerpResult217 = lerp( float4( 0,0,0,0 ) , tex2D( _WaterSeamMask, uv_WaterSeamMask ) , half4( tex2DNode23 , 0.0 ));
			float4 screenColor65 = tex2Dproj( _GrabTexture, UNITY_PROJ_COORD( ( ase_grabScreenPosNorm + ( lerpResult217 * _WaterDistortionIntensity ) ) ) );
			float4 Refraction312 = screenColor65;
			o.Albedo = ( Caustics454 + WaterMainAlbedo335 + Refraction312 ).rgb;
			float4 SkyboxCubemapReflection278 = ( texCUBE( _ReflectionCubemap, normalize( WorldReflectionVector( i , Waves275 ) ) ) * _ReflectionIntensity );
			half4 blendOpSrc373 = _Color;
			half4 blendOpDest373 = SkyboxCubemapReflection278;
			float lerpResult429 = lerp( 0.8 , 1.0 , _SinTime.w);
			float clampResult431 = clamp( lerpResult429 , 0.8 , 1.0 );
			float4 lerpResult307 = lerp( float4( 0,0,0,0 ) , _SecondaryTint , saturate( pow( ( temp_output_89_0 + clampResult431 ) , -5.0 ) ));
			float4 EdgeFoam305 = lerpResult307;
			o.Emission = ( ( saturate( ( 1.0 - ( 1.0 - blendOpSrc373 ) * ( 1.0 - blendOpDest373 ) ) )) + EdgeFoam305 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=15800
238;167;1618;955;1969.892;906.7426;1;True;False
Node;AmplifyShaderEditor.CommentaryNode;271;-1978.996,-2219.633;Float;False;1541.642;484.9691;;13;275;24;407;408;212;236;238;48;22;23;465;466;467;Waves;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector2Node;407;-1825.204,-2162.936;Float;False;Constant;_Waves1Speed;Waves 1 Speed;15;0;Create;True;0;0;False;0;5,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;465;-1975.332,-1993.032;Float;False;Property;_TextureWaveSpeed;Texture Wave Speed;4;0;Create;True;0;0;False;0;0;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;408;-1812.439,-1884.606;Float;False;Constant;_Waves2Speed;Waves 2 Speed;15;0;Create;True;0;0;False;0;0,5;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;467;-1602.553,-1870.764;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;466;-1609.553,-2148.764;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;21;-2228.303,-2017.273;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TimeNode;212;-1701.53,-2031.365;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;238;-1444.879,-1916.286;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.03,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.PannerNode;22;-1452.653,-2141.348;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;-0.03,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;48;-1451.598,-2000.073;Float;False;Constant;_NormalScale;Normal Scale;14;1;[HideInInspector];Create;True;0;0;False;0;0.025;0.025;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;152;-2892.01,-800.2724;Float;False;778.4828;263.3816;;4;89;3;1;2;Edge Detection;1,1,1,1;0;0
Node;AmplifyShaderEditor.ScreenPosInputsNode;2;-2858.004,-727.763;Float;False;1;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;236;-1233.029,-1945.571;Float;True;Property;_WaterNormal;Water Normal;1;0;Create;True;0;0;False;0;None;a716154822a86274b992db837c8072bc;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;153;-1990.077,-628.1821;Float;False;1562.509;447.6141;;12;305;307;108;113;110;115;418;431;429;424;426;111;Edge Foam;1,1,1,1;0;0
Node;AmplifyShaderEditor.SamplerNode;23;-1237.752,-2168.335;Float;True;Property;_Normal2;Normal2;1;0;Create;True;0;0;False;0;None;None;True;0;True;bump;Auto;True;Instance;236;Auto;Texture2D;6;0;SAMPLER2D;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;1;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;111;-1971.224,-439.152;Float;False;Constant;_MinFoamDepth;Min Foam Depth;4;0;Create;True;0;0;False;0;1;0.85;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenDepthNode;1;-2649.004,-726.2631;Float;False;0;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendNormalsNode;24;-885.0517,-2052.608;Float;False;0;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SinTimeNode;426;-1911.935,-351.5037;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;457;-1980.928,-1645.101;Float;False;2096.244;568.3547;;16;454;405;406;393;390;389;397;392;446;444;396;398;445;460;461;469;Caustics;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;150;-1253.337,-2717.668;Float;False;1568.006;465.8677;;8;312;65;96;164;98;97;217;295;Refraction;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;424;-1975.522,-518.8718;Float;False;Constant;_MaxFoamDepth;Max Foam Depth;5;0;Create;True;0;0;False;0;0.8;0.75;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;295;-1197.009,-2646.589;Float;False;317;255;Edge Verts Mask;1;215;;1,1,1,1;0;0
Node;AmplifyShaderEditor.Vector2Node;398;-1493.397,-1229.867;Float;False;Constant;_Vector1;Vector 1;17;0;Create;True;0;0;False;0;-10,-20;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.CommentaryNode;284;-1993.89,-117.4345;Float;False;2022.057;608.9274;;12;286;197;210;203;204;205;196;206;199;202;200;285;Vertex Offset;1,1,1,1;0;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;3;-2412.607,-689.9627;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;396;-1496.189,-1582.191;Float;False;Constant;_Vector0;Vector 0;17;0;Create;True;0;0;False;0;4,2;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;445;-1677.663,-1393.655;Float;False;Property;_CausticSpeed;Caustic Speed;12;0;Create;True;0;0;False;0;0;0.15;0;0.75;0;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;277;-1994.576,571.5165;Float;False;1305.444;382.0563;;6;262;276;278;268;269;261;Skybox Cubemap Reflection;1,1,1,1;0;0
Node;AmplifyShaderEditor.CommentaryNode;159;-1987.098,-985.374;Float;False;1143.36;268.5185;;7;335;13;94;87;10;88;6;Depth;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;275;-647.8806,-2058.151;Float;False;Waves;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;429;-1750.934,-495.1677;Float;True;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TimeNode;392;-1383.413,-1418.536;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;276;-1958.88,660.0356;Float;False;275;Waves;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.TimeNode;196;-1861.592,71.24147;Float;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.AbsOpNode;89;-2251.409,-689.1466;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ClampOpNode;431;-1499.062,-497.7462;Float;False;3;0;FLOAT;0;False;1;FLOAT;0.85;False;2;FLOAT;0.95;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;215;-1180.347,-2595.104;Float;True;Property;_TextureSample2;Texture Sample 2;13;0;Create;True;0;0;False;0;2ef3ff2ea478e8e41bfb2903d24e68bf;2ef3ff2ea478e8e41bfb2903d24e68bf;True;0;False;white;Auto;False;Instance;209;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;197;-1925.794,230.1794;Float;False;Property;_VertexWaveSpeed;Vertex Wave Speed;6;0;Create;True;0;0;False;0;5;5;1;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;444;-1311.846,-1230.329;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;393;-1957.658,-1508.653;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;6;-1962.348,-920.688;Float;False;Constant;_WaterDepth;Water Depth;9;0;Create;True;0;0;False;0;-10;-10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;446;-1319.941,-1558.137;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;460;-1937.214,-1327.272;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TextureCoordinatesNode;206;-1615.331,-35.11543;Float;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;217;-814.1684,-2578.368;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;88;-1771.183,-914.261;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;418;-1505.738,-371.1044;Float;False;Constant;_FoamFalloff;Foam Falloff;8;0;Create;True;0;0;False;0;-5;-10;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;199;-1524.402,129.9255;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;97;-818.3845,-2344.597;Float;False;Property;_WaterDistortionIntensity;Water Distortion Intensity;2;0;Create;True;0;0;False;0;-1;0.5;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;397;-1151.983,-1297.163;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode;115;-1326.865,-558.2458;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.PannerNode;389;-1149.949,-1525.898;Float;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RangedFloatNode;10;-1819.847,-809.3609;Float;False;Constant;_WaterFalloff;Water Falloff;15;1;[HideInInspector];Create;True;0;0;False;0;-0.5;-0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.WorldReflectionVector;262;-1761.863,666.1047;Float;True;True;1;0;FLOAT3;0,0,0;False;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.SamplerNode;390;-968.5559,-1324.796;Float;True;Property;_WaterCaustics2;Water Caustics 2;10;0;Create;True;0;0;False;0;None;6acdb451bd222004f8e9d47a50fca586;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;261;-1520.196,637.3295;Float;True;Property;_ReflectionCubemap;Reflection Cubemap;7;0;Create;True;0;0;False;0;089f831a7a000214f8b206190c3b5c68;089f831a7a000214f8b206190c3b5c68;True;0;False;white;Auto;False;Object;-1;Auto;Cube;6;0;SAMPLER2D;;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;3;FLOAT3;0,0,0;False;4;FLOAT3;0,0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;200;-1335.253,61.92467;Float;False;2;2;0;FLOAT2;0,0;False;1;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;98;-535.6839,-2465.738;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;110;-1183.733,-479.2011;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GrabScreenPosition;164;-533.7501,-2643.235;Float;False;0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;461;-964.4445,-1543.142;Float;True;Property;_WaterCaustics;Water Caustics;9;0;Create;True;0;0;False;0;None;3ec64a043b8e1774abfb8125c314cb89;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;203;-1219.876,381.6037;Float;False;Property;_VertexWaveHeight;Vertex Wave Height;5;0;Create;True;0;0;False;0;0.1;0.15;0;0.15;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;269;-1488.694,839.3609;Float;False;Property;_ReflectionIntensity;Reflection Intensity;8;0;Create;True;0;0;False;0;0.75;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.PowerNode;87;-1599.916,-867.8812;Float;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;96;-257.2181,-2536.81;Float;False;2;2;0;FLOAT4;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;268;-1181.591,731.144;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;469;-607.0093,-1463.648;Float;True;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;204;-926.875,268.5056;Float;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;108;-1073.237,-376.6659;Float;False;Property;_SecondaryTint;SecondaryTint;3;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;285;-880.9848,-71.64643;Float;False;310.0474;248.2;Edge Verts Mask;1;209;;1,1,1,1;0;0
Node;AmplifyShaderEditor.RangedFloatNode;406;-638.6641,-1226.875;Float;False;Property;_CausticIntensity;Caustic Intensity;11;0;Create;True;0;0;False;0;0.5;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;94;-1429.36,-866.7128;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.NoiseGeneratorNode;202;-1181.155,56.27868;Float;True;Simplex2D;1;0;FLOAT2;0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;113;-1015.712,-478.8637;Float;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;405;-344.683,-1361.516;Float;True;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;209;-861.5408,-22.20562;Float;True;Property;_WaterSeamMask;Water Seam Mask;13;0;Create;True;0;0;False;0;2ef3ff2ea478e8e41bfb2903d24e68bf;2ef3ff2ea478e8e41bfb2903d24e68bf;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScreenColorNode;65;-96.44196,-2542.905;Float;False;Global;_WaterGrab;WaterGrab;-1;0;Create;True;0;0;False;0;Object;-1;False;True;1;0;FLOAT4;0,0,0,0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TFHCRemapNode;205;-733.3715,199.0923;Float;False;5;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;4;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;307;-801.8166,-475.7197;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.CommentaryNode;349;204.3308,-770.2141;Float;False;769.6882;328.9824;;5;386;308;372;373;279;Blend Water Colour, Skybox;1,1,1,1;0;0
Node;AmplifyShaderEditor.LerpOp;13;-1262.646,-912.5328;Float;False;3;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;278;-999.6375,650.7056;Float;True;SkyboxCubemapReflection;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;335;-1086.657,-916.8331;Float;False;WaterMainAlbedo;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.CommentaryNode;456;444.0949,-1182.079;Float;False;531.2454;295.3442;;4;336;402;453;455;Blend Caustics, Depth, Refraction;1,1,1,1;0;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;305;-640.2906,-474.5479;Float;False;EdgeFoam;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ColorNode;372;308.9712,-719.2831;Float;False;Property;_Color;MainTint;0;0;Create;False;0;0;False;0;0,0,0,0;0,0.4355549,0.745283,1;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode;279;232.7746,-540.2402;Float;False;278;SkyboxCubemapReflection;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;210;-497.9437,68.46269;Float;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;454;-110.4774,-1365.956;Float;False;Caustics;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;312;96.2574,-2542.216;Float;False;Refraction;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;453;465.7688,-964.1742;Float;False;312;Refraction;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;286;-208.1108,63.69369;Float;False;VertexOffset;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.BlendOpsNode;373;589.8635,-683.7946;Float;False;Screen;True;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;308;605.8023,-563.7516;Float;False;305;EdgeFoam;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;455;462.158,-1132.486;Float;False;454;Caustics;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;336;463.6465,-1048.908;Float;False;335;WaterMainAlbedo;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;287;792.4533,-413.3235;Float;False;286;VertexOffset;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleAddOpNode;402;768.4297,-1114.346;Float;True;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;283;818.5429,-860.4166;Float;False;275;Waves;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleAddOpNode;386;835.6855,-645.2414;Float;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1265.011,-877.4738;Half;False;True;2;Half;ASEMaterialInspector;0;0;StandardSpecular;Sully/Pixel Water;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;Off;0;False;-1;3;False;-1;False;0;False;-1;0;False;-1;False;0;Translucent;0.5;True;False;0;False;Opaque;;Transparent;ForwardOnly;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;0;4;10;25;False;0.5;True;0;1;False;-1;10;False;-1;0;0;False;-1;0;False;-1;1;False;-1;1;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT3;0,0,0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;467;0;408;0
WireConnection;467;1;465;0
WireConnection;466;0;407;0
WireConnection;466;1;465;0
WireConnection;238;0;21;0
WireConnection;238;2;467;0
WireConnection;238;1;212;1
WireConnection;22;0;21;0
WireConnection;22;2;466;0
WireConnection;22;1;212;1
WireConnection;236;1;238;0
WireConnection;236;5;48;0
WireConnection;23;1;22;0
WireConnection;23;5;48;0
WireConnection;1;0;2;0
WireConnection;24;0;23;0
WireConnection;24;1;236;0
WireConnection;3;0;1;0
WireConnection;3;1;2;4
WireConnection;275;0;24;0
WireConnection;429;0;424;0
WireConnection;429;1;111;0
WireConnection;429;2;426;4
WireConnection;89;0;3;0
WireConnection;431;0;429;0
WireConnection;431;1;424;0
WireConnection;431;2;111;0
WireConnection;444;0;398;0
WireConnection;444;1;445;0
WireConnection;446;0;396;0
WireConnection;446;1;445;0
WireConnection;217;1;215;0
WireConnection;217;2;23;0
WireConnection;88;0;6;0
WireConnection;88;1;89;0
WireConnection;199;0;196;1
WireConnection;199;1;197;0
WireConnection;397;0;460;0
WireConnection;397;2;444;0
WireConnection;397;1;392;1
WireConnection;115;0;89;0
WireConnection;115;1;431;0
WireConnection;389;0;393;0
WireConnection;389;2;446;0
WireConnection;389;1;392;1
WireConnection;262;0;276;0
WireConnection;390;1;397;0
WireConnection;261;1;262;0
WireConnection;200;0;206;0
WireConnection;200;1;199;0
WireConnection;98;0;217;0
WireConnection;98;1;97;0
WireConnection;110;0;115;0
WireConnection;110;1;418;0
WireConnection;461;1;389;0
WireConnection;87;0;88;0
WireConnection;87;1;10;0
WireConnection;96;0;164;0
WireConnection;96;1;98;0
WireConnection;268;0;261;0
WireConnection;268;1;269;0
WireConnection;469;0;461;0
WireConnection;469;1;390;0
WireConnection;204;0;203;0
WireConnection;94;0;87;0
WireConnection;202;0;200;0
WireConnection;113;0;110;0
WireConnection;405;0;469;0
WireConnection;405;1;406;0
WireConnection;65;0;96;0
WireConnection;205;0;202;0
WireConnection;205;3;204;0
WireConnection;205;4;203;0
WireConnection;307;1;108;0
WireConnection;307;2;113;0
WireConnection;13;2;94;0
WireConnection;278;0;268;0
WireConnection;335;0;13;0
WireConnection;305;0;307;0
WireConnection;210;1;209;0
WireConnection;210;2;205;0
WireConnection;454;0;405;0
WireConnection;312;0;65;0
WireConnection;286;0;210;0
WireConnection;373;0;372;0
WireConnection;373;1;279;0
WireConnection;402;0;455;0
WireConnection;402;1;336;0
WireConnection;402;2;453;0
WireConnection;386;0;373;0
WireConnection;386;1;308;0
WireConnection;0;0;402;0
WireConnection;0;1;283;0
WireConnection;0;2;386;0
WireConnection;0;11;287;0
ASEEND*/
//CHKSM=9B35F62A11E48C68DBEF095E0DDD09CAF30AD467