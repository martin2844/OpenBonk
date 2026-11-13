Shader "Archanor VFX/Retro Arsenal/Beam" {
    Properties {
        _Tint ("Tint", Color) = (0,0,0,0)
        _ExtraGlow ("Extra Glow", Range(0, 15)) = 0
        _TextureSample ("Texture Sample", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (-5,0,0,0)
        [HideInInspector] _texcoord ("", 2D) = "white" {}
        [HideInInspector] __dirty ("", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "IsEmissive"="True" }
        Pass {
            Tags { "LightMode"="ForwardBase" }
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            float4 _TextureSample_ST, _texcoord_ST, _ScrollSpeed, _Tint;
            float _ExtraGlow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _texcoord_ST.xy + _texcoord_ST.zw;
                o.color = v.color;
                // The original Beam computes fog from interpolated clip depth
                // in the fragment shader, including on mobile targets.
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                o.fogCoord = o.pos.z;
                #endif
                return o;
            }
            float4 frag(v2f i) : SV_Target {
                float2 uv = i.uv * _TextureSample_ST.xy + _TextureSample_ST.zw + _Time.y * _ScrollSpeed.xy;
                float4 texel = tex2D(_TextureSample, uv);
                float4 result = float4(i.color.rgb * _ExtraGlow * _Tint.rgb * texel.rgb, texel.a * i.color.a);
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                UNITY_CALC_FOG_FACTOR(i.fogCoord);
                UNITY_FOG_LERP_COLOR(result, unity_FogColor, unityFogFactor);
                #endif
                return result;
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Blend One Zero
            ColorMask RGB
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            sampler3D _DitherMaskLOD;
            float4 _TextureSample_ST, _ScrollSpeed;
            struct appdataShadow { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2fShadow { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; fixed4 color : COLOR; };
            v2fShadow vertShadow(appdataShadow v) {
                v2fShadow o;
                o.uv = v.uv;
                o.color = v.color;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 fragShadow(v2fShadow i) : SV_Target {
                float2 uv = TRANSFORM_TEX(i.uv, _TextureSample) + _Time.y * _ScrollSpeed.xy;
                float alpha = tex2D(_TextureSample, uv).a * i.color.a;
                clip(tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, alpha * 0.9375)).a - 0.01);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
