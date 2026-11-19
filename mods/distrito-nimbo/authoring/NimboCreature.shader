Shader "OpenBonk/NimboCreature"
{
    Properties
    {
        _MainTex ("Species atlas", 2D) = "white" {}
        _AlbedoColor ("Status tint", Color) = (1,1,1,1)
        _RimColor ("Invulnerability rim", Color) = (0,1,1,0)
        [Toggle(_NIMBO_ALPHA_CLIP)] _AlphaClip ("Masked iris overlays", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Face culling", Float) = 2
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull [_Cull] ZWrite On Blend Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Fit GLES3 devices with 256 vertex uniform vectors, including shadows.
            #pragma instancing_options forcemaxcount:16 nolightprobe nolightmap
            #pragma shader_feature_local _NIMBO_ALPHA_CLIP
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex;
            fixed4 _AlbedoColor, _RimColor;
            struct Vertex
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float4 atlas : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Fragment
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normal : TEXCOORD1;
                float3 world : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4)
                half grounding : TEXCOORD5;
                float4 atlas : TEXCOORD6;
            };
            Fragment vert(Vertex v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Fragment o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                o.atlas = v.atlas;
                o.grounding = lerp(.83h, 1.0h, saturate(v.vertex.y * 1.8h));
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(Fragment i, fixed facing : VFACE) : SV_Target
            {
                half3 normal = normalize(i.normal) * (facing >= 0 ? 1 : -1);
                half3 sun = normalize(_WorldSpaceLightPos0.xyz + half3(.0001h, .0001h, .0001h));
                half diffuse = saturate(dot(normal, sun));
                half shadow = SHADOW_ATTENUATION(i);
                half3 ambient = max(half3(.36h,.38h,.42h), ShadeSH9(half4(normal,1)) * .65h);
                half3 lighting = ambient + _LightColor0.rgb * (.54h * diffuse * shadow);
                half3 view = normalize(_WorldSpaceCameraPos - i.world);
                half rim = pow(1 - saturate(dot(normal, view)), 3);
                fixed4 texel = tex2Dgrad(_MainTex, i.atlas.xy + frac(i.uv) * i.atlas.zw, ddx(i.uv) * i.atlas.zw, ddy(i.uv) * i.atlas.zw);
                #if defined(_NIMBO_ALPHA_CLIP)
                    clip(texel.a - .5h);
                #endif
                fixed3 albedo = texel.rgb * _AlbedoColor.rgb;
                fixed4 color = fixed4(albedo * lighting * i.grounding + albedo * rim * .05h + _RimColor.rgb * (_RimColor.a * rim), 1);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
        Pass
        {
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Fit GLES3 devices with 256 vertex uniform vectors, including shadows.
            #pragma instancing_options forcemaxcount:16 nolightprobe nolightmap
            #pragma shader_feature_local _NIMBO_ALPHA_CLIP
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct v2f { V2F_SHADOW_CASTER; float2 uv : TEXCOORD1; float4 atlas : TEXCOORD2; };
            v2f vert(appdata_full v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                o.uv = v.texcoord.xy;
                o.atlas = v.texcoord1;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 frag(v2f i) : SV_Target
            {
                #if defined(_NIMBO_ALPHA_CLIP)
                    clip(tex2Dgrad(_MainTex, i.atlas.xy + frac(i.uv) * i.atlas.zw, ddx(i.uv) * i.atlas.zw, ddy(i.uv) * i.atlas.zw).a - .5h);
                #endif
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
