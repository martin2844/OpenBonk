Shader "Archanor VFX/Retro Arsenal/BeamDouble" {
    Properties {
        _TextureSample ("Texture Sample", 2D) = "white" {}
        _TextureSample2 ("Texture Sample #2", 2D) = "white" {}
        _Tint ("Tint", Color) = (0,0,0,0)
        _ExtraGlow ("Extra Glow", Range(0, 15)) = 0
        _ScrollSpeed ("Scroll Speed", Vector) = (-5,0,0,0)
        _ScrollSpeed2 ("Scroll Speed #2", Vector) = (-25,0,0,0)
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
            sampler2D _TextureSample, _TextureSample2;
            float4 _TextureSample_ST, _TextureSample2_ST, _texcoord_ST, _ScrollSpeed, _ScrollSpeed2, _Tint;
            float _ExtraGlow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _texcoord_ST.xy + _texcoord_ST.zw;
                o.color = v.color;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            float4 frag(v2f i) : SV_Target {
                float2 uv1 = i.uv * _TextureSample_ST.xy + _TextureSample_ST.zw + _Time.y * _ScrollSpeed.xy;
                float2 uv2 = i.uv * _TextureSample2_ST.xy + _TextureSample2_ST.zw + _Time.y * 1.25 * _ScrollSpeed2.xy;
                float4 a = tex2D(_TextureSample, uv1);
                float4 b = tex2D(_TextureSample2, uv2);
                float4 result = float4(i.color.rgb * _ExtraGlow * _Tint.rgb * a.rgb * b.rgb, saturate(a.a * b.a * i.color.a));
                UNITY_APPLY_FOG(i.fogCoord, result);
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
            sampler2D _TextureSample, _TextureSample2;
            sampler3D _DitherMaskLOD;
            float4 _TextureSample_ST, _TextureSample2_ST, _ScrollSpeed, _ScrollSpeed2;
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
                float2 uv1 = TRANSFORM_TEX(i.uv, _TextureSample) + _Time.y * _ScrollSpeed.xy;
                float2 uv2 = TRANSFORM_TEX(i.uv, _TextureSample2) + _Time.y * 1.25 * _ScrollSpeed2.xy;
                float alpha = saturate(tex2D(_TextureSample, uv1).a * tex2D(_TextureSample2, uv2).a * i.color.a);
                clip(tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, alpha * 0.9375)).a - 0.01);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
