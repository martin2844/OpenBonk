Shader "Custom/UIRainbowShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Range(0.1, 10)) = 1
    }
    SubShader {
        Tags { "Queue"="Overlay+100" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _MainTex_ST;
            float _Speed;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            fixed4 frag(v2f i) : SV_Target {
                float hue = frac(_Time.y * _Speed + i.uv.x);
                if (hue < 0.16667) return fixed4(1,0,0,1);
                if (hue < 0.33333) return fixed4(1,.5,0,1);
                if (hue < 0.5) return fixed4(1,1,0,1);
                if (hue < 0.66667) return fixed4(0,1,0,1);
                if (hue < 0.83333) return fixed4(0,1,1,1);
                return fixed4(1,0,1,1);
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
