Shader "Archanor VFX/Retro Arsenal/BasicGlowUI" {
    Properties {
        _Tint ("Tint", Color) = (0,0,0,0)
        _ExtraGlow ("Extra Glow", Range(0, 15)) = 0
        _TextureSample ("Texture Sample", 2D) = "white" {}
        [HideInInspector] _texcoord ("", 2D) = "white" {}
        [HideInInspector] __dirty ("", Float) = 1
    }
    SubShader {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" "IsEmissive"="true" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _TextureSample;
            float4 _TextureSample_ST, _texcoord_ST;
            float4 _Tint;
            float _ExtraGlow;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            v2f vert(appdata v) {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _texcoord); o.color = v.color; return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 texel = tex2D(_TextureSample, TRANSFORM_TEX(i.uv, _TextureSample));
                return fixed4(i.color.rgb * _ExtraGlow * _Tint.rgb * texel.rgb, texel.a * i.color.a);
            }
            ENDCG
        }
    }
}
