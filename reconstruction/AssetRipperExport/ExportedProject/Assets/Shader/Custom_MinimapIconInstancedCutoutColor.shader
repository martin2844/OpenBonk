Shader "Custom/MinimapIconInstancedCutoutColor" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _Color ("Color Tint", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Blend One OneMinusSrcAlpha
        ZWrite On
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:16
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Color;
            float _Cutoff;
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 texel = tex2D(_MainTex, i.uv);
                clip(texel.a - _Cutoff);
                return texel * _Color;
            }
            ENDCG
        }
    }
}
