Shader "MK/Toon/Built-in/Standard/Physically Based + Outline" {
	Properties {
		[Enum(MK.Toon.Workflow)] _Workflow ("", Float) = 0
		[Enum(MK.Toon.SurfaceOutline)] _Surface ("", Float) = 0
		_Blend ("", Float) = 0
		[Toggle] _AlphaClipping ("", Float) = 0
		[Enum(MK.Toon.RenderFace)] _RenderFace ("", Float) = 2
		[MainColor] _AlbedoColor ("", Color) = (1,1,1,1)
		_AlphaCutoff ("", Range(0, 1)) = 0.5
		[MainTexture] _AlbedoMap ("", 2D) = "white" {}
		_AlbedoMapIntensity ("", Range(0, 1)) = 1
		[MKToonColorRGB] _SpecularColor ("", Color) = (0.203125,0.203125,0.203125,1)
		_SpecularMap ("", 2D) = "white" {}
		_Metallic ("", Range(0, 1)) = 0
		_MetallicMap ("", 2D) = "white" {}
		_RoughnessMap ("", 2D) = "white" {}
		_Smoothness ("", Range(0, 1)) = 0.5
		_Roughness ("", Range(0, 1)) = 0.5
		_NormalMapIntensity ("", Float) = 1
		[Normal] _NormalMap ("", 2D) = "bump" {}
		_Parallax ("", Range(0, 0.1)) = 0
		_HeightMap ("", 2D) = "white" {}
		[Enum(MK.Toon.LightTransmission)] _LightTransmission ("", Float) = 0
		_LightTransmissionDistortion ("", Range(0, 1)) = 0.25
		[MKToonColorRGB] _LightTransmissionColor ("", Color) = (1,0.65,0,1)
		_ThicknessMap ("", 2D) = "white" {}
		_OcclusionMapIntensity ("", Range(0, 1)) = 1
		_OcclusionMap ("", 2D) = "white" {}
		_EmissionColor ("", Color) = (0,0,0,1)
		_EmissionMap ("", 2D) = "black" {}
		[Enum(MK.Toon.DetailBlend)] _DetailBlend ("", Float) = 0
		[MKToonColorRGB] _DetailColor ("", Color) = (1,1,1,1)
		_DetailMix ("", Range(0, 1)) = 0.5
		_DetailMap ("", 2D) = "white" {}
		_DetailNormalMapIntensity ("", Float) = 1
		[Normal] _DetailNormalMap ("", 2D) = "bump" {}
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
		_LightTransmissionRamp ("", 2D) = "grey" {}
		_LightTransmissionSmoothness ("", Range(0, 1)) = 0.5
		_LightTransmissionThresholdOffset ("", Range(0, 1)) = 0.25
		[MKToonLightBands] _LightBands ("", Range(2, 12)) = 4
		_LightBandsScale ("", Range(0, 1)) = 0.5
		_LightThreshold ("", Range(0, 1)) = 0.5
		_ThresholdMap ("", 2D) = "gray" {}
		_ThresholdMapScale ("", Float) = 1
		_GoochRampIntensity ("", Range(0, 1)) = 0.5
		_GoochRamp ("", 2D) = "white" {}
		_GoochBrightColor ("", Color) = (1,1,1,1)
		_GoochBrightMap ("", 2D) = "white" {}
		_GoochDarkColor ("", Color) = (0,0,0,1)
		_GoochDarkMap ("", 2D) = "white" {}
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
		[Enum(MK.Toon.Diffuse)] _Diffuse ("", Float) = 0
		[Toggle] _WrappedLighting ("", Float) = 1
		_IndirectFade ("", Range(0, 1)) = 1
		[Toggle] _ReceiveShadows ("", Float) = 1
		[Enum(MK.Toon.Specular)] _Specular ("", Float) = 1
		[MKToonSpecularIntensity] _SpecularIntensity ("", Float) = 1
		_Anisotropy ("", Range(-1, 1)) = 0
		[MKToonTransmissionIntensity] _LightTransmissionIntensity ("", Float) = 1
		[Enum(MK.Toon.EnvironmentReflection)] _EnvironmentReflections ("", Float) = 2
		[Toggle] _FresnelHighlights ("", Float) = 1
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
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass {
			Name "ForwardBase"
			Tags { "LightMode"="ForwardBase" }
			Cull [_RenderFace]
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Blend [_BlendSrc] [_BlendDst]
			HLSLPROGRAM
			#pragma vertex pbrVert
			#pragma fragment pbrFrag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			sampler2D _AlbedoMap;
			float4 _AlbedoMap_ST, _AlbedoColor, _GoochDarkColor, _GoochBrightColor;
			float _Metallic, _Smoothness, _LightThreshold, _DiffuseSmoothness, _SpecularSmoothness, _SpecularIntensity, _IndirectFade;
			struct pbrInput { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
			struct pbrOutput { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; float3 normal : TEXCOORD2; float3 ambientL2 : TEXCOORD3; };
			pbrOutput pbrVert(pbrInput v) {
				pbrOutput o; o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_AlbedoMap);
				o.world=mul(unity_ObjectToWorld,v.vertex).xyz; o.normal=UnityObjectToWorldNormal(v.normal);
				o.ambientL2=SHEvalLinearL2(float4(o.normal,1)); return o;
			}
			float3 safeNormalize(float3 v) { return v*rsqrt(max(dot(v,v),.000061)); }
			float4 pbrFrag(pbrOutput i) : SV_Target {
				float3 view=safeNormalize(_WorldSpaceCameraPos.xyz-i.world), normal=safeNormalize(i.normal);
				float nv=saturate(dot(view,normal)); float fresnel=pow(1.0-nv,5.0);
				float3 albedo=tex2D(_AlbedoMap,i.uv).rgb*_AlbedoColor.rgb;
				float oneMinusReflectivity=(1.0-_Metallic)*.96;
				float3 specularColor=lerp(float3(.04,.04,.04),albedo,_Metallic);
				float3 diffuse=albedo*(1.0-specularColor)*oneMinusReflectivity;
				float roughness=(1.0-_Smoothness)*(1.0-_Smoothness), roughnessSq=roughness*roughness;
				float3 fresnelSpec=lerp(specularColor,max(specularColor,_Smoothness.xxx),fresnel);
				float3 ambientL0L1=float3(dot(unity_SHAr,float4(normal,1)),dot(unity_SHAg,float4(normal,1)),dot(unity_SHAb,float4(normal,1)));
				float3 indirect=max(ambientL0L1+i.ambientL2,0.0)*diffuse+fresnelSpec*unity_IndirectSpecColor.rgb/(roughnessSq+1.0);
				float3 light=safeNormalize(_WorldSpaceLightPos0.xyz), halfDirection=safeNormalize(light+view);
				float nl=saturate(dot(normal,light)), nh=saturate(dot(normal,halfDirection)), lh=dot(light,halfDirection);
				float diffuseInput=(nl*.5+.5)*(nl*.5+.5), diffuseWidth=fwidth(diffuseInput)+_DiffuseSmoothness*.5;
				float band=smoothstep(_LightThreshold-diffuseWidth,_LightThreshold+diffuseWidth,diffuseInput);
				float3 gooch=lerp(_GoochDarkColor.rgb,_GoochBrightColor.rgb,band);
				float d=(nh*nh*(roughnessSq-1.0)+1.0); float distribution=roughnessSq/(d*d*3.14159203);
				float k=(2.0-_Smoothness)*(2.0-_Smoothness)*.125;
				float visibility=(nv/(nv*(1.0-k)+k))*(nl/(nl*(1.0-k)+k));
				float specular=distribution*visibility/max(4.0*nv*nl,.000001);
				float specWidth=fwidth(specular)+_SpecularSmoothness*.5;
				float specBand=smoothstep(.5-specWidth,.5+specWidth,specular);
				float3 specTerm=saturate(specularColor/max(lh,.000001))*_SpecularIntensity;
				float3 direct=3.14159203*_LightColor0.rgb*(specTerm*specBand+diffuse*.318309009)*gooch;
				float luminance=dot(direct,float3(.2126,.7152,.0722));
				float reflectivity=1.0-oneMinusReflectivity;
				float fade=reflectivity*(reflectivity-_IndirectFade)+_IndirectFade;
				return float4(indirect*lerp(luminance,1.0,fade)+direct,1.0);
			}
			ENDHLSL
		}

		// Original noise-outline path used by the Poison Flask outline mesh.
		// The base pass above is still the exporter's approximate texture pass.
		Pass {
			Name "Outline"
			Tags { "LightMode"="Always" }
			Cull Front
			ZWrite [_ZWrite]
			ZTest [_ZTest]
			Blend [_BlendSrc] [_BlendDst]
			HLSLPROGRAM
			#pragma vertex outlineVert
			#pragma fragment outlineFrag
			#pragma multi_compile __ _MK_OUTLINE_NOISE
			#include "UnityCG.cginc"
			float _OutlineSize, _OutlineNoise;
			float4 _OutlineColor;
			struct outlineInput { float4 vertex : POSITION; float3 normal : NORMAL; };
			struct outlineOutput { float4 pos : SV_POSITION; };
			outlineOutput outlineVert(outlineInput v) {
				outlineOutput o;
				float size = _OutlineSize;
				#if defined(_MK_OUTLINE_NOISE)
					precise float3 pos = v.vertex.xyz * rsqrt(dot(v.vertex.xyz, v.vertex.xyz));
					precise float noise = dot(pos,float3(26246.8613,88296.1172,8913.5625));
					noise = sin(noise);
					noise = frac(noise * 987654.562);
					precise float offset = _OutlineSize * noise - _OutlineSize;
					size = _OutlineNoise * offset + _OutlineSize;
				#endif
				float3 normal = v.normal * rsqrt(max(dot(v.normal,v.normal),0.000061));
				v.vertex.xyz += normal * size * .0005;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
			float4 outlineFrag(outlineOutput i) : SV_Target { return float4(_OutlineColor.rgb,1.0); }
			ENDHLSL
		}
	}
	//CustomEditor "MK.Toon.Editor.Legacy.StandardPBSEditor"
}
