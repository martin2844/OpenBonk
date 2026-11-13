Shader "Archanor VFX/Retro Arsenal/TextGlow" {
    Properties {
        _TextureSample ("Texture Sample", 2D) = "white" {}
        _PanningTexture ("Panning Texture", 2D) = "white" {}
        _CenterGlow ("Center Glow", Range(1, 5)) = 1
        _Speed ("Speed", Vector) = (0,4,0,0)
        [HideInInspector] _texcoord ("", 2D) = "white" {}
        [HideInInspector] __dirty ("", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "IsEmissive"="true" }
        Pass {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _TextureSample, _PanningTexture;
            float4 _TextureSample_ST, _texcoord_ST, _Speed;
            float _CenterGlow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=TRANSFORM_TEX(v.uv, _texcoord); o.color=v.color;
                // Match the original fragment's fog calculation on mobile.
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                o.fogCoord = o.vertex.z;
                #endif
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float2 panUv=i.uv * 16.0 + _Time.y * _Speed.xy;
                float3 glow=tex2D(_PanningTexture, panUv).rgb * _CenterGlow;
                fixed4 sample=tex2D(_TextureSample, TRANSFORM_TEX(i.uv, _TextureSample));
                fixed4 result=fixed4((glow * i.color.rgb + 2.0) * sample.rgb * i.color.rgb, sample.a);
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                UNITY_CALC_FOG_FACTOR(i.fogCoord);
                UNITY_FOG_LERP_COLOR(result, unity_FogColor, unityFogFactor);
                #endif
                return result;
            }
            ENDCG
        }
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Off ZWrite On
            ColorMask RGB
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            float4 _TextureSample_ST;
            sampler3D _DitherMaskLOD;
            struct appdataShadow { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2fShadow { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; };
            v2fShadow vertShadow(appdataShadow v) {
                v2fShadow o;
                o.uv = v.uv;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 fragShadow(v2fShadow i) : SV_Target {
                float alpha = tex2D(_TextureSample, TRANSFORM_TEX(i.uv, _TextureSample)).a;
                clip(tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, alpha * 0.9375)).a - 0.01);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
