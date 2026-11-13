Shader "Hidden/OpenBonkWaterNormalProbe" {
 Properties { _MainTex ("Normal map", 2D) = "bump" {} }
 SubShader { Cull Off ZWrite Off ZTest Always Pass {
 CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 fixed4 frag(v2f_img i) : SV_Target { float3 n = UnpackNormal(tex2D(_MainTex, i.uv)); return float4(n * .5 + .5, 1); }
 ENDCG
 } }
}
