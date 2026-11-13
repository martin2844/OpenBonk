Shader "Archanor VFX/Retro Arsenal/BasicGlow" {
    Properties {
        _Tint ("Tint", Color) = (1,1,1,1)
        _ExtraGlow ("Extra Glow", Range(0, 15)) = 0
        _TextureSample ("Texture Sample", 2D) = "white" {}
        [HideInInspector] _texcoord ("", 2D) = "white" {}
        [HideInInspector] __dirty ("", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_particles
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            float4 _TextureSample_ST;
            float4 _texcoord_ST;
            float4 _Tint;
            float _ExtraGlow;
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _texcoord);
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            float4 frag(v2f i) : SV_Target {
                float4 color = tex2D(_TextureSample, TRANSFORM_TEX(i.uv, _TextureSample));
                float4 result = float4(color.rgb * i.color.rgb * _Tint.rgb * _ExtraGlow, color.a * i.color.a);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            Blend One Zero
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            float4 _TextureSample_ST;
            sampler3D _DitherMaskLOD;
            struct appdataShadow {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2fShadow {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2fShadow vertShadow(appdataShadow v) {
                v2fShadow o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.uv = v.uv;
                o.color = v.color;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 fragShadow(v2fShadow i) : SV_Target {
                float alpha = tex2D(_TextureSample, TRANSFORM_TEX(i.uv, _TextureSample)).a * i.color.a;
                clip(tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, alpha * 0.9375)).a - 0.01);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
