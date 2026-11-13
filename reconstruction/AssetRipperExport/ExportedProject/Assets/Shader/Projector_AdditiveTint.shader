Shader "Projector/AdditiveTint" {
    Properties {
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Attenuation ("Falloff", Range(0,1)) = 1
        _ShadowTex ("Cookie", 2D) = "gray" {}
    }
    SubShader {
        Tags { "Queue"="Overlay" }
        Pass {
            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4x4 unity_Projector;
            sampler2D _ShadowTex;
            float4 _Color;
            float _Attenuation;
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float4 projected : TEXCOORD0; };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.projected = mul(unity_Projector, v.vertex); return o;
            }
            float4 frag(v2f i) : SV_Target {
                float alpha = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(i.projected)).a;
                return alpha * _Color * saturate(1.0 + _Attenuation - abs(i.projected.z));
            }
            ENDCG
        }
    }
}
