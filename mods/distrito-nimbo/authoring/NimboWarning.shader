Shader "OpenBonk/NimboWarning"
{
    SubShader
    {
        Tags { "Queue"="Transparent+4" "RenderType"="Transparent" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Vertex { float4 position : POSITION; fixed4 color : COLOR; };
            struct Fragment { float4 position : SV_POSITION; fixed4 color : COLOR; };
            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                output.color = input.color;
                return output;
            }
            fixed4 frag(Fragment input) : SV_Target { return input.color; }
            ENDCG
        }
    }
}
