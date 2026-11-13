Shader "Custom/DissolveInstanced_Cutout" {
    Properties {
        _NoiseTexture ("Noise Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        [PerRendererData] _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        [PerRendererData] _DisplacementStrength ("Displacement Strength", Range(0, 1)) = 0.1
    }
    SubShader {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            // Keep per-instance matrices and properties within mobile GLES uniform limits.
            #pragma instancing_options forcemaxcount:16
            #include "UnityCG.cginc"
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST;
            fixed4 _Color;
            UNITY_INSTANCING_BUFFER_START(DissolveProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _DissolveAmount)
                UNITY_DEFINE_INSTANCED_PROP(float, _DisplacementStrength)
            UNITY_INSTANCING_BUFFER_END(DissolveProps)
            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float amount = UNITY_ACCESS_INSTANCED_PROP(DissolveProps, _DissolveAmount);
                float strength = UNITY_ACCESS_INSTANCED_PROP(DissolveProps, _DisplacementStrength);
                v.vertex.xyz += v.normal * (sin(v.vertex.y * 10.0 + amount * 20.0) * strength);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(i);
                float amount = UNITY_ACCESS_INSTANCED_PROP(DissolveProps, _DissolveAmount);
                clip(tex2D(_NoiseTexture, i.uv).r - amount);
                return _Color;
            }
            ENDCG
        }
    }
}
