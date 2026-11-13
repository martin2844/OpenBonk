Shader "UI/AnimatedOutline" {
	Properties {
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Outline Color", Color) = (1,1,1,1)
		_BackgroundColor ("Background Color", Color) = (0,0,0,0)
		_Thickness ("Outline Thickness (normalized 0–1)", Range(0.001, 5)) = 0.02
		_TrailLength ("Trail Length", Range(0.01, 1)) = 0.1
		_Speed ("Trail Speed", Float) = 1
		_RectSize ("UI Element Size (pixels)", Vector) = (100,100,0,0)
	}
	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
		Cull Off
		ZWrite Off
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			float4 _MainTex_ST, _Color, _RectSize;
			float _Thickness, _TrailLength, _Speed;
			struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
			struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
			v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
			float4 frag(v2f i) : SV_Target {
				float2 t = _Thickness / _RectSize.xy;
				float2 edge = min(i.uv, 1.0 - i.uv) / t;
				if (min(edge.x, edge.y) > 1.0) discard;
				float2 farEdge = 1.0 - t;
				bool top = i.uv.y >= farEdge.y, bottom = i.uv.y <= t.y;
				bool left = i.uv.x <= t.x, right = i.uv.x >= farEdge.x;
				float along = -1.0;
				if (top && !left && !right) along = .25 * (i.uv.x - t.x) / (1.0 - 2.0 * t.x);
				else if (top && right) along = .25;
				else if (right && !top && !bottom) along = .25 + .25 * (1.0 - i.uv.y - t.y) / (1.0 - 2.0 * t.y);
				else if (right && bottom) along = .5;
				else if (bottom && !left && !right) along = .5 + .25 * (1.0 - t.x - i.uv.x) / (1.0 - 2.0 * t.x);
				else if (bottom && left) along = .75;
				else if (left && !top && !bottom) along = .75 + .25 * (i.uv.y - t.y) / (1.0 - 2.0 * t.y);
				else if (top && left) along = .999;
				if (along < 0.0) discard;
				float end = frac(_Time.y * _Speed), start = frac(end - _TrailLength);
				float delta = along - start;
				float t0 = saturate(delta / _TrailLength);
				float beforeEnd = end >= along ? 1.0 : 0.0;
				float alpha = start < end ? beforeEnd * t0 : max(beforeEnd * saturate((delta + 1.0) / _TrailLength), t0);
				return (alpha * .8 + .2) * _Color;
			}
			ENDCG
		}
	}
}
