Shader "OpenBonk/NimboArchitecture"
{
    Properties
    {
        _MainTex ("Surface", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            sampler2D _MainTex;

            struct Vertex { float4 position : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Fragment
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed3 shade : COLOR;
                UNITY_FOG_COORDS(1)
            };

            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                half sun = saturate(dot(UnityObjectToWorldNormal(input.normal), normalize(half3(-.42, .86, .34))));
                half3 palette = input.color.rgb;
                half luma = dot(palette, half3(.299, .587, .114));
                palette = saturate(lerp(half3(luma, luma, luma), palette, 1.35));
                #ifndef UNITY_COLORSPACE_GAMMA
                palette = GammaToLinearSpace(palette);
                #endif
                output.shade = palette * (.72 + .28 * sun);
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                fixed4 color = fixed4(tex2D(_MainTex, input.uv).rgb * input.shade, 1);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
