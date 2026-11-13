Shader "Kaima/Depth/ForceField" {
    Properties {
        _MainColor ("Main Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0,1)) = 1
        _IntersectionPower ("Intersect Power", Range(0,1)) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:16
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float3 viewDir : TEXCOORD1; float4 screenPos : TEXCOORD2; float eyeDepth : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
            UNITY_INSTANCING_BUFFER_END(Props)
            float _RimPower; float _IntersectionPower;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            v2f vert(appdata input) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                o.position = UnityObjectToClipPos(input.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;
                o.viewDir = _WorldSpaceCameraPos.xyz - worldPos;
                o.normal = normalize(mul((float3x3)unity_ObjectToWorld, input.normal));
                o.screenPos = ComputeScreenPos(o.position);
                o.eyeDepth = -UnityObjectToViewPos(input.vertex).z;
                return o;
            }
            float4 frag(v2f input) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(input);
                float rim = 1.0 - abs(dot(normalize(input.normal), normalize(input.viewDir))) * _RimPower;
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(input.screenPos)));
                float intersection = (1.0 - (depth - input.eyeDepth)) * _IntersectionPower;
                return UNITY_ACCESS_INSTANCED_PROP(Props, _MainColor) * max(intersection, rim);
            }
            ENDCG
        }
    }
}
