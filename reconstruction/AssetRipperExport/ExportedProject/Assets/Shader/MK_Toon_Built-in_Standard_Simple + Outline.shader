Shader "MK/Toon/Built-in/Standard/Simple + Outline" {
	Properties {
		[Enum(MK.Toon.SurfaceOutline)] _Surface ("", Float) = 0
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
		[Enum(MK.Toon.Light)] _Light ("", Float) = 1
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
		[Enum(MK.Toon.EnvironmentReflectionSimple)] _EnvironmentReflections ("", Float) = 1
		[MKToonRenderPriority] _RenderPriority ("", Range(-50, 50)) = 0
		[Enum(MK.Toon.Stencil)] _Stencil ("", Float) = 1
		[MKToonStencilRef] _StencilRef ("", Range(0, 255)) = 0
		[MKToonStencilReadMask] _StencilReadMask ("", Range(0, 255)) = 255
		[MKToonStencilWriteMask] _StencilWriteMask ("", Range(0, 255)) = 255
		[Enum(MK.Toon.StencilComparison)] _StencilComp ("", Float) = 8
		[Enum(MK.Toon.StencilOperation)] _StencilPass ("", Float) = 0
		[Enum(MK.Toon.StencilOperation)] _StencilFail ("", Float) = 0
		[Enum(MK.Toon.StencilOperation)] _StencilZFail ("", Float) = 0
		[Enum(MK.Toon.Outline)] _Outline ("", Float) = 3
		[Enum(MK.Toon.OutlineData)] _OutlineData ("", Float) = 0
		_OutlineFadeMin ("", Float) = 0.25
		_OutlineFadeMax ("", Float) = 2
		_OutlineMap ("", 2D) = "white" {}
		_OutlineSize ("", Float) = 5
		_OutlineColor ("", Color) = (0,0,0,1)
		_OutlineNoise ("", Range(-1, 1)) = 0
		[HideInInspector] _Initialized ("", Float) = 0
		[HideInInspector] _OptionsTab ("", Float) = 1
		[HideInInspector] _InputTab ("", Float) = 1
		[HideInInspector] _StylizeTab ("", Float) = 0
		[HideInInspector] _AdvancedTab ("", Float) = 0
		[HideInInspector] _OutlineTab ("", Float) = 0
		[HideInInspector] _Cutoff ("", Range(0, 1)) = 0.5
		[HideInInspector] _MainTex ("", 2D) = "white" {}
		[HideInInspector] _Color ("", Color) = (1,1,1,1)
	}
	// The original desktop program samples _AlbedoMap * _AlbedoColor and, for
	// the default rim variant, blends _RimColor by an antialiased view rim.
	// Other outline and lighting variants remain to be recovered.
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Pass {
			Tags { "LightMode"="ForwardBase" }
			Cull [_RenderFace]
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile __ _MK_ALBEDO_MAP
			#pragma multi_compile __ _MK_IRIDESCENCE_DEFAULT
			#pragma multi_compile __ _MK_EMISSION
			#pragma multi_compile __ _MK_EMISSION_MAP
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_SINE
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_STUTTER
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"
			#include "MKToonVertexAnimation.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			sampler2D _AlbedoMap;
			sampler2D _EmissionMap;
			float4 _EmissionColor;
			sampler2D _IridescenceRamp;
			float4 _IridescenceColor;
			float _IridescenceSize, _IridescenceSmoothness;
			float4 _AlbedoMap_ST;
			float4 _AlbedoColor;
			float _AlphaClipping, _AlphaCutoff;
			float4 _RimColor;
			float _Rim;
			float _RimSize;
			float _RimSmoothness;
			float _LightThreshold, _DiffuseSmoothness, _SpecularSmoothness, _Smoothness, _SpecularIntensity, _IndirectFade;
			float _Specular, _WrappedLighting, _ReceiveShadows;
			float4 _GoochDarkColor, _GoochBrightColor, _SpecularColor;
			struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float3 normal : NORMAL; float4 color : COLOR; };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 worldNormal : TEXCOORD1; float3 worldPos : TEXCOORD2; SHADOW_COORDS(3) float4 color : COLOR; };
			v2f vert(appdata v) {
				v2f o;
				v.vertex = MKToonAnimateVertex(v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
				o.color = v.color;
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				TRANSFER_SHADOW(o);
				return o;
			}
			float4 frag(v2f i) : SV_Target {
				#if defined(_MK_ALBEDO_MAP)
					float4 albedo = tex2D(_AlbedoMap, i.uv) * _AlbedoColor;
				#else
					float4 albedo = i.color * _AlbedoColor;
				#endif
				if (_AlphaClipping > 0.5) clip(albedo.a - _AlphaCutoff + 0.000061);
				float3 normal = normalize(i.worldNormal);
				float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
				float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				float ndotl = saturate(dot(normal, lightDir));
				float wrapped = _WrappedLighting > 0.5 ? ndotl * 0.5 + 0.5 : ndotl;
				float diffuseInput = wrapped * wrapped;
				float diffuseWidth = fwidth(diffuseInput) + _DiffuseSmoothness * 0.5;
				float band = smoothstep(_LightThreshold - diffuseWidth, _LightThreshold + diffuseWidth, diffuseInput);
				UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPos);
				if (_ReceiveShadows > 0.5) band *= attenuation;
				float3 diffuseColor = lerp(_GoochDarkColor.rgb, _GoochBrightColor.rgb, band);
				float3 halfDir = normalize(lightDir + viewDir);
				float highlight = pow(saturate(dot(normal, halfDir)), exp2(_Smoothness * 10.0 + 1.0));
				float specWidth = fwidth(highlight) + _SpecularSmoothness * 0.5;
				float spec = _Specular > 0.5 ? smoothstep(0.5 - specWidth, 0.5 + specWidth, highlight) : 0.0;
				float3 direct = _LightColor0.rgb * diffuseColor * (albedo.rgb + 3.14159265 * _SpecularColor.rgb * _SpecularIntensity * spec);
				float luminance = dot(direct, float3(0.2126, 0.7152, 0.0722));
				float indirectFactor = lerp(luminance, 1.0, _IndirectFade);
				float3 ambient = max(ShadeSH9(float4(normal, 1.0)), 0.0) * albedo.rgb;
				float3 emission = 0;
				#if defined(_MK_EMISSION)
				emission = _EmissionColor.rgb;
				#if defined(_MK_EMISSION_MAP)
				emission *= tex2D(_EmissionMap, i.uv).rgb;
				#endif
				#endif
				albedo.rgb = direct + (ambient + emission) * indirectFactor;
				if (_Rim > 0.5 && _RimColor.a > 0) {
					float facing = saturate(dot(normal, viewDir));
					float rim = pow(1.0 - facing, _RimSize);
					float width = fwidth(rim) + _RimSmoothness * 0.5;
					float strength = smoothstep(0.5 - width, 0.5 + width, rim) * _RimColor.a;
					albedo.rgb = lerp(albedo.rgb, _RimColor.rgb, strength);
				}
				#if defined(_MK_IRIDESCENCE_DEFAULT)
				float iridescence = pow(saturate(1.0 - dot(normal, viewDir)), _IridescenceSize);
				float iridescenceWidth = fwidth(iridescence) + _IridescenceSmoothness * 0.5;
				iridescence = smoothstep(0.5 - iridescenceWidth, 0.5 + iridescenceWidth, iridescence);
				float3 iridescenceColor = tex2D(_IridescenceRamp, float2(iridescence, 0.5)).rgb * _IridescenceColor.rgb;
				albedo.rgb = lerp(albedo.rgb, iridescenceColor * iridescence, iridescence * _IridescenceColor.a);
				#endif
				return float4(albedo.rgb, 1.0);
			}
			ENDHLSL
		}
		Pass {
			Tags { "LightMode"="Always" }
			Cull Front
			ZTest [_ZTest]
			ZWrite [_ZWrite]
			HLSLPROGRAM
			#pragma vertex vertOutline
			#pragma fragment fragOutline
			#pragma multi_compile __ _MK_ALBEDO_MAP
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_SINE
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_STUTTER
			#pragma multi_compile __ _MK_OUTLINE_NOISE
			#include "UnityCG.cginc"
			#include "MKToonVertexAnimation.cginc"
			float _Outline, _OutlineSize, _OutlineNoise;
			sampler2D _AlbedoMap;
			float4 _OutlineColor, _AlbedoMap_ST, _AlbedoColor;
			float _AlphaClipping, _AlphaCutoff;
			struct outlineInput { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
			struct outlineOutput { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
			outlineOutput vertOutline(outlineInput v) {
				outlineOutput o;
				v.vertex = MKToonAnimateVertex(v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
				o.color = v.color;
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 projected = normalize(mul(UNITY_MATRIX_VP, float4(worldNormal, 0)).xyz);
				float aspectCorrection = 1.777778 / (_ScreenParams.x / _ScreenParams.y);
				float outlineSize = _OutlineSize;
				#if defined(_MK_OUTLINE_NOISE)
					precise float3 positionDirection = v.vertex.xyz * rsqrt(dot(v.vertex.xyz, v.vertex.xyz));
					precise float noise = dot(positionDirection, float3(26246.8613, 88296.1172, 8913.5625));
					noise = sin(noise);
					noise = frac(noise * 987654.562);
					precise float offset = _OutlineSize * noise - _OutlineSize;
					outlineSize = _OutlineNoise * offset + _OutlineSize;
				#endif
				o.pos.xy += (projected.xy / _ScreenParams.xy) * (2.0 * outlineSize) * aspectCorrection * (_ScreenParams.x / 3840.0);
				return o;
			}
			float4 fragOutline(outlineOutput i) : SV_Target {
				clip(_Outline - 2.5);
				#if defined(_MK_ALBEDO_MAP)
					if (_AlphaClipping > 0.5) clip(tex2D(_AlbedoMap, i.uv).a * _AlbedoColor.a - _AlphaCutoff + 0.000061);
					return float4(_OutlineColor.rgb, 1);
				#else
					if (_AlphaClipping > 0.5) clip(i.color.a * _AlbedoColor.a - _AlphaCutoff + 0.000061);
					return float4(i.color.rgb * _OutlineColor.rgb, 1);
				#endif
			}
			ENDHLSL
		}
		Pass {
			Tags { "LightMode"="ShadowCaster" }
			Cull [_RenderFace]
			ZWrite On ZTest LEqual
			HLSLPROGRAM
			#pragma vertex vertShadow
			#pragma fragment fragShadow
			#pragma multi_compile_shadowcaster
			#pragma multi_compile __ _MK_ALBEDO_MAP
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_SINE
			#pragma multi_compile __ _MK_VERTEX_ANIMATION_STUTTER
			#include "UnityCG.cginc"
			#include "MKToonVertexAnimation.cginc"
			sampler2D _AlbedoMap;
			float4 _AlbedoMap_ST, _AlbedoColor;
			float _AlphaClipping, _AlphaCutoff;
			struct shadowInput { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; };
			struct shadowOutput { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; float4 color : COLOR; };
			shadowOutput vertShadow(shadowInput v) {
				shadowOutput o;
				v.vertex = MKToonAnimateVertex(v.vertex);
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
				o.color = v.color;
				return o;
			}
			float4 fragShadow(shadowOutput i) : SV_Target {
				#if defined(_MK_ALBEDO_MAP)
					if (_AlphaClipping > 0.5) clip(tex2D(_AlbedoMap, i.uv).a * _AlbedoColor.a - _AlphaCutoff + 0.000061);
				#else
					if (_AlphaClipping > 0.5) clip(i.color.a * _AlbedoColor.a - _AlphaCutoff + 0.000061);
				#endif
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDHLSL
		}
	}

	Fallback "Legacy Shaders/Diffuse"
	//CustomEditor "MK.Toon.Editor.Legacy.StandardSimpleEditor"
}
