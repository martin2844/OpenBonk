Shader "Toon/Lit Tri Planar Noise Blend" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Top Texture", 2D) = "white" {}
		_TopTex2 ("Top Texture 2", 2D) = "white" {}
		_MainTexSide ("Side/Bottom Texture", 2D) = "white" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
		_Noise ("Noise", 2D) = "white" {}
		_Scale ("Top Scale", Range(-2, 2)) = 1
		_SideScale ("Side Scale", Range(-2, 2)) = 1
		_NoiseScale ("Noise Scale", Range(-2, 2)) = 1
		_TopNoiseScale ("Top Noise Scale", Range(0.001, 1)) = 0.005
		_TopTex2Intensity ("Top Texture 2 Intensity", Range(0, 1)) = 0.5
		_TopTex2BlendSize ("Top Texture 2 Blend Size", Range(0, 1)) = 0
		_TopSpread ("TopSpread", Range(-2, 2)) = 1
		_EdgeWidth ("EdgeWidth", Range(0, 0.5)) = 1
		_EdgeColor ("Edge Color", Color) = (0.5,0.5,0.5,1)
		_BelowYTexture ("Below Y Texture", 2D) = "white" {}
		_BelowYBlend ("Below Y Blend Range", Range(0.01, 5)) = 1
		_BelowYThreshold ("Below Y Threshold", Float) = 0
		_OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.02
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Pass {
			Name "OUTLINE"
			Cull Front
			CGPROGRAM
			#pragma vertex outlineVert
			#pragma fragment outlineFrag
			#include "UnityCG.cginc"
			float _OutlineWidth; float4 _OutlineColor;
			struct outlineInput { float4 vertex : POSITION; float3 normal : NORMAL; };
			struct outlineOutput { float4 pos : SV_POSITION; };
			outlineOutput outlineVert(outlineInput v) { outlineOutput o; v.vertex.xyz += UnityObjectToWorldNormal(v.normal) * _OutlineWidth; o.pos = UnityObjectToClipPos(v.vertex); return o; }
			float4 outlineFrag(outlineOutput i) : SV_Target { return _OutlineColor; }
			ENDCG
		}
		Pass {
			Name "FORWARD"
			Tags { "LightMode"="ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			sampler2D _MainTex, _TopTex2, _MainTexSide, _BelowYTexture, _Noise, _Ramp;
			float4 _Color, _EdgeColor;
			float _Scale, _SideScale, _NoiseScale, _TopNoiseScale, _TopTex2Intensity, _TopTex2BlendSize, _TopSpread, _EdgeWidth, _BelowYThreshold, _BelowYBlend;
			struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 tangent : TANGENT; };
			struct v2f { float4 pos : SV_POSITION; float3 worldPos : TEXCOORD0; float3 normal : TEXCOORD1; float3 tangent : TEXCOORD2; float3 bitangent : TEXCOORD3; float fog : TEXCOORD4; UNITY_SHADOW_COORDS(5) float3 vertexLight : TEXCOORD6; };
			v2f vert(appdata v) {
				v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.worldPos=mul(unity_ObjectToWorld,v.vertex).xyz;
				o.normal=UnityObjectToWorldNormal(v.normal); o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
				o.bitangent=cross(o.normal,o.tangent)*(v.tangent.w*unity_WorldTransformParams.w);
				UNITY_TRANSFER_SHADOW(o, float2(0,0));
				#ifdef VERTEXLIGHT_ON
					o.vertexLight=Shade4PointLights(unity_4LightPosX0,unity_4LightPosY0,unity_4LightPosZ0,unity_LightColor[0].rgb,unity_LightColor[1].rgb,unity_LightColor[2].rgb,unity_LightColor[3].rgb,unity_4LightAtten0,o.worldPos,o.normal);
				#else
					o.vertexLight=0;
				#endif
				float depth=UNITY_Z_0_FAR_FROM_CLIPSPACE(o.pos.z);
				#if defined(FOG_LINEAR)
					o.fog=depth*unity_FogParams.z+unity_FogParams.w;
				#elif defined(FOG_EXP)
					o.fog=exp2(-depth*unity_FogParams.y);
				#elif defined(FOG_EXP2)
						float d=depth*unity_FogParams.x; o.fog=exp2(-d*d);
				#else
					o.fog=1.0;
				#endif
				return o;
			}
			float3 sampleTriplanar(sampler2D tex, float3 p, float scale, float3 weights) {
				return tex2D(tex,p.yz*scale).rgb*weights.x + tex2D(tex,p.xz*scale).rgb*weights.y + tex2D(tex,p.xy*scale).rgb*weights.z;
			}
			float4 frag(v2f i) : SV_Target {
				float3 n=i.normal;
				float3 weights=n*n; weights*=weights; weights/=max(dot(weights, float3(1,1,1)),1e-8);
				float3 top=sampleTriplanar(_MainTex,i.worldPos,_Scale,weights);
				float3 top2=sampleTriplanar(_TopTex2,i.worldPos,_Scale,weights);
				float noiseTop=tex2D(_Noise,i.worldPos.xz*_TopNoiseScale).r;
				float topBlend = _TopTex2BlendSize > 0.0 ? smoothstep(_TopTex2Intensity-_TopTex2BlendSize*.5,_TopTex2Intensity+_TopTex2BlendSize*.5,noiseTop) : step(_TopTex2Intensity,noiseTop);
				top=lerp(top,top2,topBlend);
				float3 side=sampleTriplanar(_MainTexSide,i.worldPos,_SideScale,weights);
				float noise=tex2D(_Noise,i.worldPos.xz*_NoiseScale).r;
				float blend=smoothstep(_TopSpread,_TopSpread+_EdgeWidth,n.y+noise*.5-.25);
				float3 albedo=lerp(side,top,blend)+_EdgeColor.rgb*blend*(1.0-blend);
				float below=1.0-smoothstep(_BelowYThreshold-_BelowYBlend*.5,_BelowYThreshold+_BelowYBlend*.5,i.worldPos.y);
				albedo=lerp(albedo,tex2D(_BelowYTexture,i.worldPos.xz*_Scale).rgb,below)*_Color.rgb;
				float ndl=dot(n,_WorldSpaceLightPos0.xyz)*.5+.5;
				float3 ramp=tex2D(_Ramp,ndl.xx).rgb;
				UNITY_LIGHT_ATTENUATION(attenuation,i,i.worldPos);
				float3 result=albedo*(ramp*_LightColor0.rgb*(2.0*attenuation)+i.vertexLight);
				return float4(lerp(unity_FogColor.rgb,result,saturate(i.fog)),1.0);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
