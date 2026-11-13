Shader "Unlit/MapWithFog" {
    Properties {
        _MainTex ("Map", 2D) = "white" {}
        _FogTex ("Fog Mask", 2D) = "white" {}
        _FogNoise ("Fog Noise", 2D) = "gray" {}
        _FogColor ("Fog Color", Color) = (0,0,0,1)
        _FogIntensity ("Fog Intensity", Range(0,1)) = 0.6
        _FogSpeed ("Fog Scroll Speed", Vector) = (0.02,0.01,0,0)
    }
    SubShader {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _FogTex, _FogNoise;
            float4 _MainTex_ST, _FogColor, _FogSpeed;
            float _FogIntensity;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float3 map = tex2D(_MainTex, i.uv).rgb;
                float fogMask = tex2D(_FogTex, i.uv).r;
                float noise = tex2D(_FogNoise, i.uv + _FogSpeed.xy * _Time.y).r;
                float3 fogBase = _FogColor.rgb + noise * .1;
                float3 mixed = lerp(fogBase, map, fogMask);
                return float4(lerp(mixed, _FogColor.rgb, (1.0 - fogMask) * _FogIntensity), 1.0);
            }
            ENDCG
        }
    }
}
