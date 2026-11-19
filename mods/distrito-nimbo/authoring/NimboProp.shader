Shader "OpenBonk/NimboProp"
{
    Properties
    {
        _Glaze ("Shell glaze", Range(0, 1)) = 0
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
            half _Glaze;

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
                half3 lightDirection = normalize(half3(.45, 1, .3));
                half3 normal = UnityObjectToWorldNormal(input.normal);
                half illumination = saturate(dot(normal, lightDirection));
                half3 viewDirection = normalize(WorldSpaceViewDir(input.position));
                half highlight = pow(saturate(dot(normal, normalize(lightDirection + viewDirection))), 20);
                // A narrow rim keeps pale creatures readable without bleaching
                // their colours at the steep chase camera.
                half rim = pow(1 - saturate(dot(normal, viewDirection)), 2.2) * .22;
                half3 shade = input.color.rgb * (.56 + illumination * .44) + highlight * _Glaze * .38;
                output.color = fixed4(shade + rim * lerp(input.color.rgb, half3(1, 1, 1), .3), 1);
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
