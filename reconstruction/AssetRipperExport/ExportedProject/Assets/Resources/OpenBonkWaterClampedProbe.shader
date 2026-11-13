Shader "Hidden/OpenBonkWaterClampedProbe" {
    Properties {
        [NoScaleOffset] [Normal] _Normal ("Normal", 2D) = "bump" {}
        _NormalStrenght ("Normal Strenght", Range(0,1)) = 0.5
        _Size ("Size", Range(0,2)) = 0
        _Speed ("Speed", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,10)) = 1
        _FoamColor ("Foam Color", Color) = (0,0,0,0)
        _TopColor ("Top Color", Color) = (0,0,0,0)
        _MidColor ("Mid Color", Color) = (1,1,1,0)
        _BottomColor ("Bottom Color", Color) = (0,0,0,0)
        _WaterFalloff ("Water Falloff", Range(-1,0)) = 0
        _EdgeReach ("Edge Reach", Range(0,1)) = 0
        _FoamReach ("Foam Reach", Range(0,1)) = 0
        _FoamScale ("Foam Scale", Range(0,1)) = 0
        [HideInInspector] _texcoord ("", 2D) = "white" {}
        [HideInInspector] __dirty ("", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        #include "UnityCG.cginc"
        sampler2D _Normal;
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
        half _NormalStrenght, _Size, _Speed, _Smoothness;
        half _WaterFalloff, _EdgeReach, _FoamReach, _FoamScale;
        fixed4 _FoamColor, _TopColor, _MidColor, _BottomColor;
        struct Input { float2 uv_texcoord; float3 worldPos; float4 screenPos; };

        // The original fragment program uses two animated 2D simplex-noise fields.
        float3 mod289(float3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float2 mod289(float2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }
        float3 permute(float3 x) { return mod289(((x * 34.0) + 1.0) * x); }
        float simplexNoise(float2 v) {
            const float4 C = float4(0.2113248654, 0.3660254038, -0.5773502692, 0.0243902439);
            float2 i = floor(v + dot(v, C.yy));
            float2 x0 = v - i + dot(i, C.xx);
            float2 i1 = (x0.x > x0.y) ? float2(1,0) : float2(0,1);
            float4 x12 = x0.xyxy + C.xxzz;
            x12.xy -= i1;
            i = mod289(i);
            float3 p = permute(permute(i.y + float3(0, i1.y, 1)) + i.x + float3(0, i1.x, 1));
            float3 m = max(0.5 - float3(dot(x0,x0), dot(x12.xy,x12.xy), dot(x12.zw,x12.zw)), 0.0);
            m = m * m; m = m * m;
            float3 x = 2.0 * frac(p * C.www) - 1.0;
            float3 h = abs(x) - 0.5;
            float3 ox = floor(x + 0.5);
            float3 a0 = x - ox;
            m *= 1.792842914 - 0.853734721 * (a0*a0 + h*h);
            float3 g;
            g.x = a0.x*x0.x + h.x*x0.y;
            g.yz = a0.yz*x12.xz + h.yz*x12.yw;
            return 130.0 * dot(m,g);
        }
        void surf(Input IN, inout SurfaceOutputStandard o) {
            float2 normalUV = IN.uv_texcoord * (_Size * 100.0);
            float3 n1 = UnpackNormal(tex2D(_Normal, normalUV + _Time.y * _Speed));
            float3 n2 = UnpackNormal(tex2D(_Normal, normalUV - _Time.y * _Speed + 1.0));
            n1.xy *= _NormalStrenght; n2.xy *= _NormalStrenght;
            n1.z = sqrt(1.0 - saturate(dot(n1.xy,n1.xy)));
            n2.z = sqrt(1.0 - saturate(dot(n2.xy,n2.xy)));
            o.Normal = normalize(float3(n1.xy+n2.xy, n1.z*n2.z));
            float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float difference = abs(depth - IN.screenPos.w);
            float scale = 1.0 - _FoamScale;
            float noise = simplexNoise((IN.worldPos.xz + float2(_Time.y * 0.05, 0)) * scale)
                        + simplexNoise((IN.worldPos.xz + float2(-_Time.y * 0.05, 0)) * scale * 2.0);
            float foam = ((1.0 - _FoamReach) * difference < noise ? 1.0 : 0.0)
                       + ((1.0 - _EdgeReach) * difference < (sin(_Time.y) + 1.0) * 0.1 + 0.1 ? 1.0 : 0.0);
            float depthBlend = saturate(pow(max(difference, 0.00001), abs(_WaterFalloff)));
            o.Albedo = saturate(lerp(_BottomColor.rgb, _MidColor.rgb, depthBlend)
                     + max(1.0 - difference, 0.0) * _TopColor.rgb + foam * _FoamColor.rgb);
            o.Metallic = 0.0;
            // Until the original full lighting path is recovered, prevent the
            // Forest material's value 10 from saturating this reconstruction.
            o.Smoothness = saturate(_Smoothness);
            o.Alpha = 1.0;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
