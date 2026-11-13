Shader "Projector/Multiply" {
	Properties {
		_ShadowTex ("Cookie", 2D) = "gray" {}
		_FalloffTex ("FallOff", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Transparent" }
		Pass {
			ZWrite Off
			ColorMask RGB
			Blend DstColor Zero
			Offset -1, -1
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			sampler2D _ShadowTex;
			sampler2D _FalloffTex;
			struct v2f {
				float4 pos : SV_POSITION;
				float4 shadow : TEXCOORD0;
				float4 falloff : TEXCOORD1;
			};
			v2f vert(float4 vertex : POSITION) {
				v2f output;
				output.pos = UnityObjectToClipPos(vertex);
				output.shadow = mul(unity_Projector, vertex);
				output.falloff = mul(unity_ProjectorClip, vertex);
				return output;
			}
			fixed4 frag(v2f input) : SV_Target {
				fixed4 shadow = tex2Dproj(_ShadowTex, UNITY_PROJ_COORD(input.shadow));
				fixed4 falloff = tex2Dproj(_FalloffTex, UNITY_PROJ_COORD(input.falloff));
				shadow.rgb = lerp(fixed3(1, 1, 1), shadow.rgb, shadow.a);
				return lerp(fixed4(1, 1, 1, 1), shadow, falloff.a);
			}
			ENDCG
		}
	}
}
