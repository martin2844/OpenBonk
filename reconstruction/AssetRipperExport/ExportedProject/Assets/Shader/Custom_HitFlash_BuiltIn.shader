Shader "Custom/HitFlash_BuiltIn" {
    Properties {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _FlashColor ("Flash Color", Color) = (1,1,1,1)
        _FlashAmount ("Flash Amount", Range(0,1)) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:16
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            sampler2D _MainTex;
            UNITY_INSTANCING_BUFFER_START(FlashProps)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _FlashColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlashAmount)
            UNITY_INSTANCING_BUFFER_END(FlashProps)
            v2f vert(appdata input) {
                v2f output; UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.vertex = UnityObjectToClipPos(input.vertex); output.uv = input.uv; return output;
            }
            fixed4 frag(v2f input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                fixed4 baseColor = tex2D(_MainTex, input.uv) * UNITY_ACCESS_INSTANCED_PROP(FlashProps, _Color);
                return lerp(baseColor, UNITY_ACCESS_INSTANCED_PROP(FlashProps, _FlashColor), UNITY_ACCESS_INSTANCED_PROP(FlashProps, _FlashAmount));
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
