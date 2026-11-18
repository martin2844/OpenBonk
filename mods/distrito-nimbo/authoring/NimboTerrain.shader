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
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TileWidth, _GridOffset, _Facade;

            struct Vertex
            {
                float4 position : POSITION;
                float3 normal : NORMAL;
            };
            struct Fragment
            {
                float4 position : SV_POSITION;
                float3 world : TEXCOORD0;
                half3 normal : TEXCOORD1;
                UNITY_FOG_COORDS(2)
            };

            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                output.world = mul(unity_ObjectToWorld, input.position).xyz;
                output.normal = UnityObjectToWorldNormal(input.normal);
                UNITY_TRANSFER_FOG(output, output.position);
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
                half3 pavement = tex2D(_MainTex, input.world.xz / 12).rgb;
                half blockShade = lerp(.88, 1, frac(dot(tile, float2(.39, .57))));
                half edge = max(abs(local.x), abs(local.y));
                half3 plaza = pavement * blockShade;
                plaza = lerp(plaza, palette(half3(.62, .72, .65)), smoothstep(.437, .445, edge) * (1 - smoothstep(.46, .47, edge)));

                // Align each avenue with the ascent of the underlying Forest tile.
                half east = step(abs(input.normal.z), abs(input.normal.x));
                float across = lerp(local.x, local.y, east);
                float along = lerp(local.y, local.x, east);
                half asphalt = 1 - smoothstep(.32, .34, abs(across));
                half3 street = lerp(pavement, palette(half3(.31, .45, .52)), asphalt);
                half lane = (1 - smoothstep(.007, .012, abs(across))) * step(.24, frac((along + .5) * 5));
                half crosswalk = smoothstep(.406, .412, abs(along)) * (1 - smoothstep(.454, .46, abs(along)));
                crosswalk *= asphalt * step(.42, frac((across + .5) * 12));
                street = lerp(street, palette(half3(.99, .91, .65)), saturate(lane * (1 - crosswalk) + crosswalk));
                half slope = 1 - smoothstep(.92, .965, input.normal.y);
                return lerp(plaza, street, slope);
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
                half3 windows = tex2D(_MainTex, float2(across / 21, input.world.y / 16)).rgb * tint;
                half corner = smoothstep(.455, .475, abs(frac(column + .5) - .5));
                half ledge = 1 - smoothstep(.01, .035, abs(frac(input.world.y / (width * .5) + .5) - .5));
                half3 pilaster = tint * palette(half3(.35, .40, .47));
                return lerp(windows, pilaster, max(corner, ledge));
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                input.normal = normalize(input.normal);
                half sun = saturate(dot(input.normal, normalize(half3(-.42, .86, .34))));
                half3 surface = _Facade > .5 ? facadeColor(input) : plazaOrStreet(input);
                fixed4 color = fixed4(surface * (.72 + .28 * sun), 1);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
