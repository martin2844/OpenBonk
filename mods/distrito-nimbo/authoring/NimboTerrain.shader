Shader "OpenBonk/NimboTerrain"
{
    Properties
    {
        _MainTex ("Masonry", 2D) = "white" {}
        _TileWidth ("Terrain tile width", Float) = 40
        _GridOffset ("Terrain grid offset", Float) = 0
        _Facade ("Project vertically", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back ZWrite On Blend Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma target 3.0
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            // Fit GLES3 devices with 256 vertex uniform vectors, including shadows.
            #pragma instancing_options forcemaxcount:16 nolightprobe nolightmap
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float _TileWidth, _GridOffset, _Facade;

            struct Vertex
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float3 normal : NORMAL;
            };
            struct Fragment
            {
                float4 pos : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                SHADOW_COORDS(3)
            };

            Fragment vert(Vertex v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                Fragment output;
                output.pos = UnityObjectToClipPos(v.vertex);
                output.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                output.normal = UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(output);
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            half3 palette(half3 srgb)
            {
                #ifndef UNITY_COLORSPACE_GAMMA
                return GammaToLinearSpace(srgb);
                #else
                return srgb;
                #endif
            }

            half3 plazaOrStreet(Fragment input)
            {
                float2 grid = (input.world.xz + _GridOffset) / max(1, _TileWidth);
                float2 tile = floor(grid + .5);
                float2 local = grid - tile;
                float width = max(1, _TileWidth);
                half3 pavement = tex2D(_MainTex, input.world.xz / 4).rgb;
                half blockShade = lerp(.92, 1, frac(dot(tile, float2(.39, .57))));
                // A continuous twelve-metre street cross on flat junctions.
                // Slopes keep only the avenue parallel to their ascent.
                float2 metres = local * width;
                half east = step(abs(input.normal.z), abs(input.normal.x));
                float across = lerp(metres.x, metres.y, east);
                float along = lerp(metres.y, metres.x, east);
                half slope = 1 - smoothstep(.92, .965, input.normal.y);
                float distanceToStreet = lerp(min(abs(metres.x), abs(metres.y)), abs(across), slope);
                half asphalt = 1 - smoothstep(5.35, 5.55, distanceToStreet);
                half curb = smoothstep(5.55, 5.65, distanceToStreet) * (1 - smoothstep(6.0, 6.12, distanceToStreet));
                half3 surface = lerp(pavement * blockShade, palette(half3(.22, .32, .36)), asphalt);
                surface = lerp(surface, palette(half3(.87, .84, .72)), curb);
                half lineX = (1 - smoothstep(.05, .09, abs(metres.x))) * step(.4, frac(input.world.z / 3));
                half lineZ = (1 - smoothstep(.05, .09, abs(metres.y))) * step(.4, frac(input.world.x / 3));
                half inCrossing = (1 - smoothstep(5.5, 5.7, abs(metres.x))) * (1 - smoothstep(5.5, 5.7, abs(metres.y)));
                half laneMarking = lerp(max(lineX,lineZ) * (1-inCrossing), lerp(lineX,lineZ,east), slope);
                float edge = width * .5 - 2.2;
                half crossX = (1-smoothstep(.6,.8,abs(abs(metres.x)-edge))) * (1-smoothstep(4.5,4.7,abs(metres.y))) * step(.45,frac(metres.y/1.15));
                half crossZ = (1-smoothstep(.6,.8,abs(abs(metres.y)-edge))) * (1-smoothstep(4.5,4.7,abs(metres.x))) * step(.45,frac(metres.x/1.15));
                half crosswalk = lerp(max(crossX,crossZ), lerp(crossZ,crossX,east), slope);
                return lerp(surface, palette(half3(.98,.94,.78)), saturate(laneMarking+crosswalk));
            }

            half3 facadeColor(Fragment input)
            {
                half east = step(abs(input.normal.z), abs(input.normal.x));
                float across = lerp(input.world.x, input.world.z, east);
                float depth = lerp(input.world.z, input.world.x, east);
                float width = max(1, _TileWidth);
                float column = (across + _GridOffset) / width;
                float2 block = floor(float2(column + .5, (depth + _GridOffset) / width));
                half section = frac(dot(block, float2(.43, .61)));
                half3 cream = palette(half3(1, .94, .81));
                half3 sage = palette(half3(.72, .94, .86));
                half3 lilac = palette(half3(.90, .80, 1));
                half3 tint = section < .36 ? cream : section < .68 ? sage : lilac;
                half3 windows = tex2D(_MainTex, float2(across / 12, input.world.y / 12)).rgb * tint;
                half corner = smoothstep(.455, .475, abs(frac(column + .5) - .5));
                half ledge = 1 - smoothstep(.01, .035, abs(frac(input.world.y / (width * .5) + .5) - .5));
                half3 pilaster = tint * palette(half3(.35, .40, .47));
                return lerp(windows, pilaster, max(corner, ledge));
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                input.normal = normalize(input.normal);
                half sun = saturate(dot(input.normal, normalize(_WorldSpaceLightPos0.xyz + half3(.0001,.0001,.0001))));
                half3 surface = _Facade > .5 ? facadeColor(input) : plazaOrStreet(input);
                fixed4 color = fixed4(surface * (max(half3(.48,.50,.54), ShadeSH9(half4(input.normal,1)) * .65) + _LightColor0.rgb * .48 * sun * SHADOW_ATTENUATION(input)), 1);
                UNITY_APPLY_FOG(input.fogCoord, color);
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
