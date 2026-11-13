Shader "Megabonk/Native Cubemap Skybox"
{
	Properties
	{
		[Gamma] _TintColor ("Cubemap Tint Color", Color) = (0.5, 0.5, 0.5, 1)
		_Exposure ("Exposure", Range(0, 8)) = 1
		_CubemapPosition ("Cubemap Position", Float) = 0
		[Toggle(_ENABLEROTATION_ON)] _EnableRotation ("Enable Rotation", Float) = 0
		_Rotation ("Rotation", Range(0, 360)) = 0
		_RotationSpeed ("Rotation Speed", Float) = 1
		[Toggle(_ENABLEFOG_ON)] _EnableFog ("Enable Fog", Float) = 0
		_FogIntensity ("Fog Intensity", Range(0, 1)) = 1
		_FogHeight ("Fog Height", Range(0, 1)) = 1
		_FogSmoothness ("Fog Smoothness", Range(0.01, 1)) = 0.01
		_FogFill ("Fog Fill", Range(0, 1)) = 0.5
		_FogPosition ("Fog Position", Float) = 0
		[NoScaleOffset] _Tex ("Cubemap", Cube) = "black" {}
		[HideInInspector] _Tex_HDR ("HDR Decode", Vector) = (1, 1, 0, 0)
	}
	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature _ENABLEROTATION_ON
			#pragma shader_feature _ENABLEFOG_ON
			#include "UnityCG.cginc"

			samplerCUBE _Tex;
			half4 _Tex_HDR;
			half4 _TintColor;
			half _Exposure;
			half _Rotation;
			half _RotationSpeed, _CubemapPosition;
			half _FogIntensity, _FogHeight, _FogSmoothness, _FogFill, _FogPosition;

			struct appdata { float4 vertex : POSITION; };
			struct v2f { float4 vertex : SV_POSITION; float3 direction : TEXCOORD0; float height : TEXCOORD1; };

			v2f vert(appdata input)
			{
				v2f output;
				output.vertex = UnityObjectToClipPos(input.vertex);
				float3 direction = input.vertex.xyz;
				float orthoScale = lerp(1.0, unity_OrthoParams.y / unity_OrthoParams.x, unity_OrthoParams.w);
				direction.y = direction.y * orthoScale - _CubemapPosition;
				#ifdef _ENABLEROTATION_ON
				float angle = 1.0 - radians(_Rotation + _Time.y * _RotationSpeed);
				float sine = sin(angle);
				float cosine = cos(angle);
				output.direction = float3(
					cosine * direction.x + sine * direction.z,
					direction.y,
					cosine * direction.z - sine * direction.x);
				#else
				output.direction = direction;
				#endif
				output.height = input.vertex.y;
				return output;
			}

			half4 frag(v2f input) : SV_Target
			{
				half4 sampleColor = texCUBE(_Tex, input.direction);
				float alpha = _Tex_HDR.w * (sampleColor.a - 1.0) + 1.0;
				float decode = _Tex_HDR.x * pow(max(alpha, 0.000001), _Tex_HDR.y);
				float4 sky = float4(sampleColor.rgb * decode * 4.5947938, 0.0) * _TintColor * _Exposure;
				#ifdef _ENABLEFOG_ON
				float height = abs(input.height - _FogPosition) / max(_FogHeight, 0.000001);
				float band = min(pow(height, 1.0 - _FogSmoothness), 1.0);
				float visibility = 1.0 + _FogIntensity * (band * (1.0 - _FogFill) - 1.0);
				return lerp(unity_FogColor, sky, visibility);
				#else
				return sky;
				#endif
			}
			ENDCG
		}
	}
}
