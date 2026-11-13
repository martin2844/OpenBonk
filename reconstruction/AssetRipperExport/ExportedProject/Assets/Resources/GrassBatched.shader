Shader "Custom/GrassBatched" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _BottomColor ("Bottom Color", Color) = (0.1,0.5,0.1,1)
        _TopColor ("Top Color", Color) = (0.5,1,0.5,1)
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.2
        _WindSpeed ("Wind Speed", Range(0, 10)) = 2
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 1.5
        _RenderDistance ("Render Distance", Float) = 100
        _PlayerPos ("Player Position", Vector) = (0,0,0,0)
        [HideInInspector] _GrassDebug ("Grass Debug", Float) = 0
    }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Cutoff, _WindStrength, _WindSpeed, _WindFrequency, _RenderDistance, _GrassDebug;
            float4 _BottomColor, _TopColor, _PlayerPos;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float2 localHeight : TEXCOORD1; };
            struct v2f { float4 position : SV_POSITION; float2 uv : TEXCOORD0; float height : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata input) {
                v2f output; UNITY_INITIALIZE_OUTPUT(v2f, output);
                // Batch vertices are already in world coordinates, with their source height retained in UV1.x.
                float4 world = input.vertex;
                if (distance(world.xz, _PlayerPos.xz) > _RenderDistance) return output;
                float wind = sin(world.x * _WindFrequency + _Time.y * _WindSpeed) * cos(world.z * _WindFrequency + _Time.y * _WindSpeed);
                world.xz += wind * _WindStrength * smoothstep(0.3, 1.0, input.localHeight.x);
                output.position = mul(UNITY_MATRIX_VP, world);
                UNITY_TRANSFER_FOG(output, output.position);
                output.uv = input.uv; output.height = input.localHeight.x;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target {
                fixed4 color = tex2D(_MainTex, input.uv);
                clip(color.a - _Cutoff);
                color.rgb *= lerp(_BottomColor.rgb, _TopColor.rgb, input.height);
                UNITY_APPLY_FOG(input.fogCoord, color);
                if (_GrassDebug > 0.5) color.rgb = float3(1, 0, 1);
                return color;
            }
            ENDCG
        }
    }
}
