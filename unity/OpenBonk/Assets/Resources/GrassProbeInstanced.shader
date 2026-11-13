Shader "Hidden/GrassProbeInstanced" {
    Properties { _GrassDebug ("Debug", Float) = 0 }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:16
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float _GrassDebug;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float height : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata input) {
                UNITY_SETUP_INSTANCE_ID(input);
                v2f output; UNITY_INITIALIZE_OUTPUT(v2f, output);
                output.position = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv; output.height = input.vertex.y;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target { return fixed4(1, 0, 1, 1); }
            ENDCG
        }
    }
}
