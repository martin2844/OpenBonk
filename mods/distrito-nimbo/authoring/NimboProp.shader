Shader "OpenBonk/NimboProp"
{
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

            struct Vertex { float4 position : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct Fragment
            {
                float4 position : SV_POSITION;
                fixed4 color : COLOR;
                UNITY_FOG_COORDS(0)
            };

            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                half illumination = saturate(dot(UnityObjectToWorldNormal(input.normal), normalize(half3(.45, 1, .3))));
                output.color = fixed4(input.color.rgb * (.56 + illumination * .44), 1);
                UNITY_TRANSFER_FOG(output, output.position);
                return output;
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                fixed4 color = input.color;
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
