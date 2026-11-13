Shader "Custom/GridSpiralTransitionWithTransparency" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BlockSize ("Block Size", Float) = 0.1
        _Progress ("Transition Progress", Range(0, 1)) = 0
    }
    SubShader {
        Tags { "Queue"="Overlay+101" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Back
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _BlockSize, _Progress;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target {
                float n = ceil(1.0 / _BlockSize);
                float2 p = floor(i.uv * n);
                float index = -1.0;
                if (p.x >= 0.0 && p.y >= 0.0 && p.x < n && p.y < n) {
                    float ring = min(min(p.x, p.y), min(n - 1.0 - p.x, n - 1.0 - p.y));
                    float width = n - 2.0 * ring;
                    float edge = n - 1.0 - ring;
                    float start = n * n - width * width;
                    if (p.y == ring) index = start + p.x - ring;
                    else if (p.x == edge) index = start + width + p.y - ring - 1.0;
                    else if (p.y == edge) index = start + 2.0 * width - 1.0 + edge - p.x - 1.0;
                    else index = start + 3.0 * width - 2.0 + edge - p.y - 1.0;
                }
                return float4(0, 0, 0, index < n * n * _Progress ? 1 : 0);
            }
            ENDCG
        }
    }
}
