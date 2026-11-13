Shader "MK/Toon/Built-in/Particles/Simple" {
	Properties {
		[Enum(MK.Toon.Surface)] _Surface ("", Float) = 1
		_Blend ("", Float) = 0
		[Toggle] _AlphaClipping ("", Float) = 0
		[Enum(MK.Toon.RenderFace)] _RenderFace ("", Float) = 2
		[MainColor] _AlbedoColor ("", Color) = (1,1,1,1)
		_AlphaCutoff ("", Range(0, 1)) = 0.5
		[MainTexture] _AlbedoMap ("", 2D) = "white" {}
		_AlbedoMapIntensity ("", Range(0, 1)) = 1
		[MKToonColorRGB] _SpecularColor ("", Color) = (1,1,1,1)
		_SpecularMap ("", 2D) = "white" {}
		_Smoothness ("", Range(0, 1)) = 0.5
		_NormalMapIntensity ("", Float) = 1
		[Normal] _NormalMap ("", 2D) = "bump" {}
		_EmissionColor ("", Color) = (0,0,0,1)
		_EmissionMap ("", 2D) = "black" {}
		[Enum(MK.Toon.Light)] _Light ("", Float) = 0
		_DiffuseRamp ("", 2D) = "grey" {}
		_DiffuseSmoothness ("", Range(0, 1)) = 0
		_DiffuseThresholdOffset ("", Range(0, 1)) = 0.25
		_SpecularRamp ("", 2D) = "grey" {}
		_SpecularSmoothness ("", Range(0, 1)) = 0
		_SpecularThresholdOffset ("", Range(0, 1)) = 0.25
		_RimRamp ("", 2D) = "grey" {}
		_RimSmoothness ("", Range(0, 1)) = 0.5
		_RimThresholdOffset ("", Range(0, 1)) = 0.25
		[MKToonLightBands] _LightBands ("", Range(2, 12)) = 4
		_LightBandsScale ("", Range(0, 1)) = 0.5
		_LightThreshold ("", Range(0, 1)) = 0.5
		_ThresholdMap ("", 2D) = "gray" {}
		_ThresholdMapScale ("", Float) = 1
		_GoochRampIntensity ("", Range(0, 1)) = 0.5
		_GoochRamp ("", 2D) = "white" {}
		_GoochBrightColor ("", Color) = (1,1,1,1)
		_GoochDarkColor ("", Color) = (0,0,0,1)
		_Contrast ("", Float) = 1
		[MKToonSaturation] _Saturation ("", Float) = 1
		[MKToonBrightness] _Brightness ("", Float) = 1
		[Enum(MK.Toon.Iridescence)] _Iridescence ("", Float) = 0
		_IridescenceRamp ("", 2D) = "white" {}
		_IridescenceSize ("", Range(0, 5)) = 1
		_IridescenceColor ("", Color) = (1,1,1,0.5)
		_IridescenceSmoothness ("", Range(0, 1)) = 0.5
		_IridescenceThresholdOffset ("", Range(0, 1)) = 0
		[Enum(MK.Toon.Rim)] _Rim ("", Float) = 0
		_RimSize ("", Range(0, 5)) = 1
		_RimColor ("", Color) = (1,1,1,1)
		_RimBrightColor ("", Color) = (1,1,1,1)
		_RimDarkColor ("", Color) = (0,0,0,1)
		[Enum(MK.Toon.ColorGrading)] _ColorGrading ("", Float) = 0
		[Toggle] _VertexAnimationStutter ("", Float) = 0
		[Enum(MK.Toon.VertexAnimation)] _VertexAnimation ("", Float) = 0
		_VertexAnimationIntensity ("", Range(0, 1)) = 0.05
		_VertexAnimationMap ("", 2D) = "white" {}
		[MKToonVertexAnimationFrequency] _VertexAnimationFrequency ("", Vector) = (2.5,2.5,2.5,1)
		[Enum(MK.Toon.Dissolve)] _Dissolve ("", Float) = 0
		_DissolveMapScale ("", Float) = 1
		_DissolveMap ("", 2D) = "white" {}
		_DissolveAmount ("", Range(0, 1)) = 0.5
		_DissolveBorderSize ("", Range(0, 1)) = 0.25
		_DissolveBorderRamp ("", 2D) = "white" {}
		[HDR] _DissolveBorderColor ("", Color) = (1,1,1,1)
		[Enum(MK.Toon.Artistic)] _Artistic ("", Float) = 0
		[Enum(MK.Toon.ArtisticProjection)] _ArtisticProjection ("", Float) = 0
		_ArtisticFrequency ("", Range(1, 10)) = 1
		_DrawnMapScale ("", Float) = 1
		_DrawnMap ("", 2D) = "white" {}
		_DrawnClampMin ("", Range(0, 1)) = 0
		_DrawnClampMax ("", Range(0, 1)) = 1
		_HatchingMapScale ("", Float) = 1
		_HatchingBrightMap ("", 2D) = "white" {}
		_HatchingDarkMap ("", 2D) = "Black" {}
		_SketchMapScale ("", Float) = 1
		_SketchMap ("", 2D) = "black" {}
		[Enum(MK.Toon.BlendFactor)] [HideInInspector] _BlendSrc ("", Float) = 1
		[Enum(MK.Toon.BlendFactor)] [HideInInspector] _BlendDst ("", Float) = 0
		[Enum(MK.Toon.ZWrite)] _ZWrite ("", Float) = 1
		[Enum(MK.Toon.ZTest)] _ZTest ("", Float) = 4
		[Toggle] _WrappedLighting ("", Float) = 1
		_IndirectFade ("", Range(0, 1)) = 1
		[Toggle] _ReceiveShadows ("", Float) = 1
		[Enum(MK.Toon.SpecularSimple)] _Specular ("", Float) = 0
		[MKToonSpecularIntensity] _SpecularIntensity ("", Float) = 1
		[Enum(MK.Toon.EnvironmentReflectionSimple)] _EnvironmentReflections ("", Float) = 0
		[MKToonRenderPriority] _RenderPriority ("", Range(-50, 50)) = 0
		[Enum(MK.Toon.Stencil)] _Stencil ("", Float) = 1
		[MKToonStencilRef] _StencilRef ("", Range(0, 255)) = 0
		[MKToonStencilReadMask] _StencilReadMask ("", Range(0, 255)) = 255
		[MKToonStencilWriteMask] _StencilWriteMask ("", Range(0, 255)) = 255
		[Enum(MK.Toon.StencilComparison)] _StencilComp ("", Float) = 8
		[Enum(MK.Toon.StencilOperation)] _StencilPass ("", Float) = 0
		[Enum(MK.Toon.StencilOperation)] _StencilFail ("", Float) = 0
		[Enum(MK.Toon.StencilOperation)] _StencilZFail ("", Float) = 0
		[Toggle] _Flipbook ("", Float) = 0
		[Toggle] _SoftFade ("", Float) = 0
		[MKToonSoftFadeNearDistance] _SoftFadeNearDistance ("", Float) = 0
		[MKToonSoftFadeFarDistance] _SoftFadeFarDistance ("", Float) = 1
		[Enum(MK.Toon.ColorBlend)] _ColorBlend ("", Float) = 0
		[Toggle] _CameraFade ("", Float) = 0
		[MKToonCameraFadeNearDistance] _CameraFadeNearDistance ("", Float) = 1
		[MKToonCameraFadeFarDistance] _CameraFadeFarDistance ("", Float) = 2
		[HideInInspector] _Initialized ("", Float) = 0
		[HideInInspector] _OptionsTab ("", Float) = 1
		[HideInInspector] _InputTab ("", Float) = 1
		[HideInInspector] _StylizeTab ("", Float) = 0
		[HideInInspector] _AdvancedTab ("", Float) = 0
		[HideInInspector] _ParticlesTab ("", Float) = 0
		[HideInInspector] _Cutoff ("", Range(0, 1)) = 0.5
		[HideInInspector] _MainTex ("", 2D) = "white" {}
		[HideInInspector] _Color ("", Color) = (1,1,1,1)
	}
	// Primary transparent particle path reconstructed for the original Borgar material.
	// Additional-light, shadow, soft-particle and optional effect variants remain unimplemented.
	SubShader {
		Tags { "RenderType"="Opaque" "IgnoreProjector"="True" }
		Pass {
			Tags { "LightMode"="ForwardBase" }
			Blend [_BlendSrc] [_BlendDst]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Cull [_RenderFace]
			ColorMask RGB
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			sampler2D _AlbedoMap;
			float4 _AlbedoMap_ST, _AlbedoColor, _GoochDarkColor, _GoochBrightColor;
			struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 normal : TEXCOORD1; float4 color : COLOR; UNITY_FOG_COORDS(2) };
			v2f vert(appdata v) {
				v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
				o.normal = UnityObjectToWorldNormal(v.normal); o.color = v.color; UNITY_TRANSFER_FOG(o, o.pos); return o;
			}
			float4 frag(v2f i) : SV_Target {
				float4 color = tex2D(_AlbedoMap, i.uv) * _AlbedoColor * i.color * i.color;
				float light = saturate(dot(normalize(i.normal), normalize(_WorldSpaceLightPos0.xyz)));
				light = (light * 0.5 + 0.5); light *= light;
				color.rgb *= lerp(_GoochDarkColor.rgb, _GoochBrightColor.rgb, light) * _LightColor0.rgb;
				UNITY_APPLY_FOG(i.fogCoord, color);
				return color;
			}
			ENDCG
		}
	}
}
