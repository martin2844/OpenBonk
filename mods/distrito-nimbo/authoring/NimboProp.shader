Shader "OpenBonk/NimboProp"
{
    Properties { _Glaze ("Shell glaze", Range(0, 1)) = 0 }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back ZWrite On Blend Off
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
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            half _Glaze;
            struct Vertex
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Fragment
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                half3 normal : TEXCOORD0;
                float3 world : TEXCOORD1;
                SHADOW_COORDS(2)
                UNITY_FOG_COORDS(3)
            };
            Fragment vert(Vertex v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Fragment o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.color = v.color;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            fixed4 frag(Fragment i) : SV_Target
            {
                half3 normal = normalize(i.normal);
                half3 sun = normalize(_WorldSpaceLightPos0.xyz + half3(.0001,.0001,.0001));
                half shadow = SHADOW_ATTENUATION(i);
                half3 view = normalize(_WorldSpaceCameraPos - i.world);
                half diffuse = saturate(dot(normal,sun));
                half highlight = pow(saturate(dot(normal,normalize(sun+view))),20) * _Glaze * .38 * shadow;
                half3 ambient = max(half3(.48,.50,.54), ShadeSH9(half4(normal,1)) * .65);
                fixed4 color = fixed4(i.color.rgb * (ambient + _LightColor0.rgb * .48 * diffuse * shadow) + highlight,1);
                UNITY_APPLY_FOG(i.fogCoord,color);
                return color;
            }
            ENDCG
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            #pragma instancing_options forcemaxcount:16 nolightprobe nolightmap
            #include "UnityCG.cginc"
            struct ShadowFragment { V2F_SHADOW_CASTER; };
            ShadowFragment shadowVert(appdata_base v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                ShadowFragment o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }
            float4 shadowFrag(ShadowFragment i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
}
