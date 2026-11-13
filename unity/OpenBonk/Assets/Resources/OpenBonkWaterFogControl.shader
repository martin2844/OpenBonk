Shader "Hidden/OpenBonkWaterFogControl" {
 SubShader { Tags { "RenderType"="Opaque" } Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 struct appdata { float4 vertex : POSITION; };
 struct v2f { float4 vertex : SV_POSITION; float depth : TEXCOORD0; };
 v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.depth = o.vertex.z; return o; }
 fixed4 frag(v2f i) : SV_Target {
     fixed4 color = fixed4(0,1,0,1);
     #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
     UNITY_CALC_FOG_FACTOR(i.depth);
     UNITY_FOG_LERP_COLOR(color, unity_FogColor, unityFogFactor);
     #endif
     return color;
 }
 ENDCG
 } }
}
