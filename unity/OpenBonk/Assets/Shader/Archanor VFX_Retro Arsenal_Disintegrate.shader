Shader "Archanor VFX/Retro Arsenal/Disintegrate" {
    Properties {
        _Tint ("Tint", Color) = (0,0,0,0)
        _ExtraGlow ("Extra Glow", Range(0, 15)) = 0
        _TextureSample ("Texture Sample", 2D) = "white" {}
        _Ramp ("Ramp", 2D) = "white" {}
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
            sampler2D _TextureSample, _Ramp;
            float4 _TextureSample_ST, _Ramp_ST, _texcoord_ST, _Tint;
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
                float4 texel = tex2D(_TextureSample, i.uv * _TextureSample_ST.xy + _TextureSample_ST.zw);
                float ramp = tex2D(_Ramp, i.uv * _Ramp_ST.xy + _Ramp_ST.zw).r;
                float inverseRamp = 1.0 - ramp;
                float t = saturate((texel.a * i.color.a - inverseRamp * texel.a) / (1.0 - texel.a * inverseRamp));
                float alpha = min(t * t * (3.0 - 2.0 * t), 1.0);
                float4 result = float4(texel.rgb * i.color.rgb * _ExtraGlow * _Tint.rgb, alpha);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
    }
}
