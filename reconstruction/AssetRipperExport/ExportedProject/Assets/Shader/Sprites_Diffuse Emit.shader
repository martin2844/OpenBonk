Shader "Sprites/Diffuse Emit" {
    Properties {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _EmissionTex ("Emission Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Intensity ("Intensity", Float) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" "PreviewType"="Plane" }
        Cull Off ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex;
            sampler2D _EmissionTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Intensity;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; half3 normal : TEXCOORD1; half3 ambient : TEXCOORD2; float3 worldPosition : TEXCOORD3; UNITY_SHADOW_COORDS(4) };
            v2f vert(appdata v) {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                #ifdef PIXELSNAP_ON
                o.pos = UnityPixelSnap(o.pos);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color * _Color; o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_SHADOW(o, v.uv);
                #if UNITY_SHOULD_SAMPLE_SH
                o.ambient = max(0, ShadeSH9(half4(o.normal, 1)));
                #else
                o.ambient = 0;
                #endif
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * i.color;
                half occlusion = UNITY_SHADOW_ATTENUATION(i, i.worldPosition);
                half3 lighting = max(0, dot(i.normal, _WorldSpaceLightPos0.xyz)) * _LightColor0.rgb * occlusion + i.ambient;
                half3 emission = tex2D(_EmissionTex, i.uv).rgb * i.color.rgb * _Intensity;
                return fixed4(baseColor.rgb * baseColor.a * lighting + emission, baseColor.a);
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #pragma target 3.0
            #pragma multi_compile_fwdadd
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            struct appdataAdd { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2fAdd { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; half3 normal : TEXCOORD1; float3 worldPosition : TEXCOORD2; UNITY_LIGHTING_COORDS(3, 4) };
            v2fAdd vertAdd(appdataAdd v) {
                v2fAdd o; o.pos = UnityObjectToClipPos(v.vertex);
                #ifdef PIXELSNAP_ON
                o.pos = UnityPixelSnap(o.pos);
                #endif
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPosition = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_LIGHTING(o, v.uv);
                return o;
            }
            fixed4 fragAdd(v2fAdd i) : SV_Target {
                fixed4 baseColor = tex2D(_MainTex, i.uv) * i.color;
                half3 lightDirection = normalize(UnityWorldSpaceLightDir(i.worldPosition));
                UNITY_LIGHT_ATTENUATION(attenuation, i, i.worldPosition);
                return fixed4(baseColor.rgb * baseColor.a * _LightColor0.rgb * max(0, dot(i.normal, lightDirection)) * attenuation, baseColor.a);
            }
            ENDCG
        }
    }
    Fallback "Transparent/VertexLit"
}
