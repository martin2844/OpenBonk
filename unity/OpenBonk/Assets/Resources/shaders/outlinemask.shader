Shader "Custom/Outline Mask" {
    Properties { [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8 }
    SubShader {
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        Pass {
            Name "Mask"
            Cull Off ZTest [_ZTest] ZWrite Off ColorMask 0
            Stencil { Ref 1 Comp Always Pass Replace }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return 0; }
            ENDCG
        }
    }
}
