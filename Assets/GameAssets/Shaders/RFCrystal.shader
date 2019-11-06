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

Shader "RobFichman/Crystal"
{
	Properties
	{
		_NoiseTex("NoiseTex", 2D) = "white" {}
		_ColorTex("ColorTex", 2D) = "white" {}
		_SparkleTex("SparkleTex", 2D) = "white" {}
		_RimTex("RimTex",2D) = "white" {}
		_RimColor("RimColor", Color) = (0.425,1,1,1)
		_ChangeColor("ChangeColor", Color) = (0.23,0,0.64,1)
		_RimPower("RimPower", float) = 0.55
		_Scale("Scale", Float) = 1.07
		_Intensity("Intensity", Float) = 54.23
		_Depth1("Depth1", Float) = 0.36
		_Depth2("Depth2", Float) = 0.2

	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "DisableBatching" = "True"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 tangent : TANGENT;
				float3 normal : NORMAL;

			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 tangentViewDir : TEXCOORD1;
				float3 wPos : TEXCOORD2;
				float3 wNormal : TEXCOORD3;
				UNITY_FOG_COORDS(4)


			};

			sampler2D _NoiseTex;
			sampler2D _ColorTex;
			sampler2D _SparkleTex;
			float4 _NoiseTex_ST;
			float4 _RimColor;
			float4 _ChangeColor;
			float _RimPower;
			float _Scale;
			float _Intensity;
			float _Depth1;
			float _Depth2;
			sampler2D _RimTex;
			v2f vert(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);

				float3x3 objectToTangent = float3x3(
					v.tangent.xyz,
					cross(v.normal, v.tangent.xyz) * v.tangent.w,
					v.normal
					);
				o.tangentViewDir = mul(objectToTangent, ObjSpaceViewDir(v.vertex));
				o.tangentViewDir = normalize(o.tangentViewDir);
				o.tangentViewDir.xy /= (o.tangentViewDir.z + 0.42);

				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.wNormal = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);

				UNITY_TRANSFER_FOG(o, o.vertex);

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
			float3 viewDirection = normalize(i.wPos - _WorldSpaceCameraPos);
			//get parallax values
			float heightval = tex2D(_NoiseTex, i.uv);
			float2 offset = i.tangentViewDir.xy*(1 - heightval)*-1 * _Depth1;
			float2 offset2 = i.tangentViewDir.xy*(heightval)*-1 * _Depth2;
			//setup color through parallax
			float4 color2 = tex2D(_ColorTex, i.uv);
			float val = tex2D(_NoiseTex, i.uv + (offset));
			float4 color = tex2D(_ColorTex, i.uv + (offset)+half2(_Time.x*3, 0));
			color = lerp(color, _ChangeColor, 1 - val);
			//sparkles
			fixed3 sparklemap = tex2D(_SparkleTex, (i.uv + offset2)*_Scale);
			sparklemap -= half3(0.5, 0.5, 0.5);
			sparklemap = normalize(sparklemap);
			half sparkle = pow(saturate((dot(-viewDirection, normalize(sparklemap + i.wNormal)))), _Intensity);
			color += sparkle * half4(2, 2, 2, 0)*color2;
			//rim lighting
			float rim = saturate((pow(1.0 - saturate(dot(-viewDirection, i.wNormal)), _RimPower)));
			rim = tex2D(_RimTex, half2(rim, rim));
			color += _RimColor * rim;
			UNITY_APPLY_FOG(i.fogCoord, color);

			return color;

			}
			ENDCG
		}
			UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"

	}
}
