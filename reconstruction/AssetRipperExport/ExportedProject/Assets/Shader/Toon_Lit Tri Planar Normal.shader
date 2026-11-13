Shader "Toon/Lit Tri Planar Normal" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_MainTex ("Top Texture", 2D) = "white" {}
		_NormalT ("Top Normal", 2D) = "bump" {}
		_MainTexSide ("Side/Bottom Texture", 2D) = "white" {}
		_Normal ("Side/Bottom Normal", 2D) = "bump" {}
		_Ramp ("Toon Ramp (RGB)", 2D) = "gray" {}
		_Noise ("Noise", 2D) = "white" {}
		_Scale ("Top Scale", Range(-2, 2)) = 1
		_SideScale ("Side Scale", Range(-2, 2)) = 1
		_NoiseScale ("Noise Scale", Range(-2, 2)) = 1
		_TopSpread ("TopSpread", Range(-2, 2)) = 1
		_EdgeWidth ("EdgeWidth", Range(0, 0.5)) = 1
		_EdgeColor ("Edge Color", Color) = (0.5,0.5,0.5,1)
		_BelowYTexture ("Below Y Texture", 2D) = "white" {}
		_BelowYBlend ("Below Y Blend Range", Range(0.01, 5)) = 1
		_BelowYThreshold ("Below Y Threshold", Float) = 0
		_OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.02
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_SuppressOutline ("Suppress Outline", Float) = 0
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		Pass {
			Name "SHADOWCASTER"
			Tags { "LightMode"="ShadowCaster" }
			ZWrite On ZTest LEqual
			CGPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma instancing_options forcemaxcount:16
			#include "UnityCG.cginc"
			struct shadowInput { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
			struct shadowOutput { V2F_SHADOW_CASTER; };
			shadowOutput vertShadow(shadowInput v) { shadowOutput o; UNITY_SETUP_INSTANCE_ID(v); TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) return o; }
			float4 fragShadow(shadowOutput i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
			ENDCG
		}
		Pass {
			Name "OUTLINE"
			Cull Front
			CGPROGRAM
			#pragma vertex outlineVert
			#pragma fragment outlineFrag
			#include "UnityCG.cginc"
			float _OutlineWidth; float _SuppressOutline; float4 _OutlineColor;
			struct outlineInput { float4 vertex : POSITION; float3 normal : NORMAL; };
			struct outlineOutput { float4 pos : SV_POSITION; };
			outlineOutput outlineVert(outlineInput v) { outlineOutput o; v.vertex.xyz += UnityObjectToWorldNormal(v.normal) * _OutlineWidth; o.pos = UnityObjectToClipPos(v.vertex); return o; }
			float4 outlineFrag(outlineOutput i) : SV_Target { clip(0.5 - _SuppressOutline); return _OutlineColor; }
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
			sampler2D _MainTex, _MainTexSide, _BelowYTexture, _Noise, _NormalT, _Normal, _Ramp;
			float4 _Color, _EdgeColor;
			float _Scale, _SideScale, _NoiseScale, _TopSpread, _EdgeWidth, _BelowYThreshold, _BelowYBlend;
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
			float3 triplanar(sampler2D tex,float3 world,float scale,float2 w) {
				float3 z=tex2D(tex,world.xy*scale).rgb;
				float3 x=tex2D(tex,world.zy*scale).rgb;
				float3 y=tex2D(tex,world.zx*scale).rgb;
				return lerp(lerp(z,x,w.x),y,w.y);
			}
			float3 triplanarNormal(sampler2D tex,float3 world,float scale,float2 w) {
				float3 z=UnpackNormal(tex2D(tex,world.xy*scale));
				float3 x=UnpackNormal(tex2D(tex,world.zy*scale));
				float3 y=UnpackNormal(tex2D(tex,world.zx*scale));
				return lerp(lerp(z,x,w.x),y,w.y);
			}
			float4 frag(v2f i) : SV_Target {
				float3 n=i.normal; float2 w=n.xy*1.4; w*=w; w=saturate(w*w);
				float3 noise=triplanar(_Noise,i.worldPos,_NoiseScale,w);
				float3 top=triplanar(_MainTex,i.worldPos,_Scale,w);
				float3 side=triplanar(_MainTexSide,i.worldPos,_SideScale,w);
				float height=dot(noise*.5+noise.y+float3(0,0,1),n.yyy);
				float blend=smoothstep(_TopSpread,_TopSpread+_EdgeWidth,height);
				float3 albedo=lerp(side,top,blend)+_EdgeColor.rgb*blend*(1.0-blend);
				float below=1.0-smoothstep(_BelowYThreshold-_BelowYBlend*.5,_BelowYThreshold+_BelowYBlend*.5,i.worldPos.y);
				albedo=lerp(albedo,tex2D(_BelowYTexture,i.worldPos.xz*_Scale).rgb,below)*_Color.rgb;
				float3 topN=triplanarNormal(_NormalT,i.worldPos,_Scale,w), sideN=triplanarNormal(_Normal,i.worldPos,_SideScale,w);
				float3 tangentN=topN*step(_TopSpread,height)+sideN*step(height,_TopSpread);
				float3 worldN=normalize(i.tangent*tangentN.x+i.bitangent*tangentN.y+n*tangentN.z);
				float ndl=dot(worldN,_WorldSpaceLightPos0.xyz)*.5+.5;
				float3 ramp=tex2D(_Ramp,ndl.xx).rgb;
				UNITY_LIGHT_ATTENUATION(attenuation,i,i.worldPos);
				float3 result=albedo*(ramp*_LightColor0.rgb*(2.0*attenuation)+i.vertexLight);
				return float4(lerp(unity_FogColor.rgb,result,saturate(i.fog)),1.0);
			}
			ENDCG
		}
		Pass {
			Name "FORWARD_ADD"
			Tags { "LightMode"="ForwardAdd" }
			Blend One One
			ZWrite Off
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			sampler2D _MainTex, _MainTexSide, _BelowYTexture, _Noise, _NormalT, _Normal, _Ramp;
			float4 _Color, _EdgeColor;
			float _Scale, _SideScale, _NoiseScale, _TopSpread, _EdgeWidth, _BelowYThreshold, _BelowYBlend;
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
			float3 triplanar(sampler2D tex,float3 world,float scale,float2 w) {
				float3 z=tex2D(tex,world.xy*scale).rgb;
				float3 x=tex2D(tex,world.zy*scale).rgb;
				float3 y=tex2D(tex,world.zx*scale).rgb;
				return lerp(lerp(z,x,w.x),y,w.y);
			}
			float3 triplanarNormal(sampler2D tex,float3 world,float scale,float2 w) {
				float3 z=UnpackNormal(tex2D(tex,world.xy*scale));
				float3 x=UnpackNormal(tex2D(tex,world.zy*scale));
				float3 y=UnpackNormal(tex2D(tex,world.zx*scale));
				return lerp(lerp(z,x,w.x),y,w.y);
			}
			float4 frag(v2f i) : SV_Target {
				float3 n=i.normal; float2 w=n.xy*1.4; w*=w; w=saturate(w*w);
				float3 noise=triplanar(_Noise,i.worldPos,_NoiseScale,w);
				float3 top=triplanar(_MainTex,i.worldPos,_Scale,w);
				float3 side=triplanar(_MainTexSide,i.worldPos,_SideScale,w);
				float height=dot(noise*.5+noise.y+float3(0,0,1),n.yyy);
				float blend=smoothstep(_TopSpread,_TopSpread+_EdgeWidth,height);
				float3 albedo=lerp(side,top,blend)+_EdgeColor.rgb*blend*(1.0-blend);
				float below=1.0-smoothstep(_BelowYThreshold-_BelowYBlend*.5,_BelowYThreshold+_BelowYBlend*.5,i.worldPos.y);
				albedo=lerp(albedo,tex2D(_BelowYTexture,i.worldPos.xz*_Scale).rgb,below)*_Color.rgb;
				float3 topN=triplanarNormal(_NormalT,i.worldPos,_Scale,w), sideN=triplanarNormal(_Normal,i.worldPos,_SideScale,w);
				float3 tangentN=topN*step(_TopSpread,height)+sideN*step(height,_TopSpread);
				float3 worldN=normalize(i.tangent*tangentN.x+i.bitangent*tangentN.y+n*tangentN.z);
				float ndl=dot(worldN,normalize(UnityWorldSpaceLightDir(i.worldPos)))*.5+.5;
				float3 ramp=tex2D(_Ramp,ndl.xx).rgb;
				UNITY_LIGHT_ATTENUATION(attenuation,i,i.worldPos);
				float3 result=albedo*ramp*_LightColor0.rgb*(2.0*attenuation);
				return float4(result*saturate(i.fog),1.0);
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
}
