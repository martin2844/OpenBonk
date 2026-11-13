Shader "BadDog/BGForceField" {
    Properties {
        [HDR] _EmissionColor ("Emission Color", Color) = (0.5,0.5,0.5,0)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _DepthOffset ("Depth Offset", Float) = 0.5
        _FresnelPower ("Fresnel Power", Float) = 1
        _ScrollSpeed ("Scroll Speed", Float) = 1
        _PatternTex ("Pattern Tex", 2D) = "white" {}
        _PatternOffsetU ("Pattern Offset U", Float) = 0.1
        _PatternOffsetV ("Pattern Offset V", Float) = 0.1
        _AlphaStrength ("Alpha Strength", Range(0, 10)) = 1
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard alpha:fade noforwardadd noshadow
        #include "UnityCG.cginc"
        sampler2D _PatternTex;
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
        float4 _EmissionColor;
        float _Metallic, _Smoothness, _DepthOffset, _FresnelPower;
        float _ScrollSpeed, _PatternOffsetU, _PatternOffsetV, _AlphaStrength;
        struct Input { float2 uv_PatternTex; float4 screenPos; float3 worldPos; float3 worldNormal; };
        void surf(Input i, inout SurfaceOutputStandard o) {
            float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
            float intersection = saturate(1.0 - abs(sceneDepth - (i.screenPos.w - _DepthOffset)));
            intersection = intersection * intersection * (3.0 - 2.0 * intersection);
            float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
            float facing = dot(viewDirection, i.worldNormal);
            float rim = facing < 0.0 ? 0.0 : pow(1.0 - saturate(facing), _FresnelPower);
            float2 uv = i.uv_PatternTex + _Time.y * _ScrollSpeed * float2(_PatternOffsetU, _PatternOffsetV);
            float pattern = tex2D(_PatternTex, uv).r;
            o.Albedo = 0;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = pattern * (intersection + rim) * _AlphaStrength;
        }
        ENDCG
    }
    Fallback Off
}
