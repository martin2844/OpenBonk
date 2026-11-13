Shader "Hidden/GrassProbeFog" {
    SubShader { Cull Off Pass { CGPROGRAM
        #pragma target 3.0
        #pragma vertex vert
        #pragma fragment frag
        #pragma multi_compile_fog
        #include "UnityCG.cginc"
        float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
        fixed4 frag() : SV_Target { return fixed4(1,0,1,1); }
    ENDCG } }
}
