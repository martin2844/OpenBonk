Shader "OpenBonk/NimboDaySky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Fragment { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };

            Fragment vert(float4 vertex : POSITION)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(vertex);
                output.direction = vertex.xyz;
                return output;
            }

            float hash(float2 p)
            {
                float3 n = frac(p.xyx * .1031);
                n += dot(n, n.yzx + 33.33);
                return frac((n.x + n.y) * n.z);
            }

            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1, 0)), f.x),
                            lerp(hash(cell + float2(0, 1)), hash(cell + 1), f.x), f.y);
            }

            half4 frag(Fragment input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float up = saturate(dir.y);
                half3 sky = lerp(half3(.76, .88, .94), half3(.31, .66, .86), smoothstep(0, .67, up));
                float2 p = dir.xz / max(.16, up);
                float clouds = noise(p * 2 + 4) * .65 + noise(p * 5) * .28 + noise(p * 11 + 3) * .07;
                float mask = smoothstep(.62, .75, clouds) * smoothstep(.04, .21, up);
                half3 color = lerp(sky, half3(.97, .97, .94), mask * .83);
                #ifndef UNITY_COLORSPACE_GAMMA
                color = GammaToLinearSpace(color);
                #endif
                return half4(color, 1);
            }
            ENDCG
        }
    }
}
