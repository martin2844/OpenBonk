Shader "Custom/MinimapHeightColorGradient" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MaxColor ("Color in Maxmal", Color) = (0,0,0,0)
        _MinDistance ("Min Distance", Float) = 100
        _MaxDistance ("Max Distance", Float) = 1000
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        ZWrite Off
        Pass {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float4 _Color, _MaxColor;
            float _MinDistance, _MaxDistance;
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float height : TEXCOORD0; float4 color : COLOR; float3 vertexLight : TEXCOORD1; UNITY_FOG_COORDS(2) };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.height = worldPos.y;
                o.color = v.color;
                o.vertexLight = 0;
                #ifdef VERTEXLIGHT_ON
                    o.vertexLight = Shade4PointLights(unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                        unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                        unity_4LightAtten0, worldPos, UnityObjectToWorldNormal(v.normal));
                #endif
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float amount = (i.height - _MinDistance) / (_MaxDistance - _MinDistance);
                float4 result = float4(lerp(_Color.rgb, _MaxColor.rgb, amount) * i.color.rgb * (1.0 + i.vertexLight), 1.0);
                UNITY_APPLY_FOG(i.fogCoord, result);
                return result;
            }
            ENDCG
        }
        Pass {
            Tags { "LightMode"="ForwardAdd" }
            Blend One One
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            float4 _Color, _MaxColor;
            float _MinDistance, _MaxDistance;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float height : TEXCOORD0; float4 color : COLOR; UNITY_FOG_COORDS(1) };
            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                UNITY_TRANSFER_FOG(o, o.pos);
                o.height = mul(unity_ObjectToWorld, v.vertex).y;
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target {
                float amount = (i.height - _MinDistance) / (_MaxDistance - _MinDistance);
                float4 result = float4(lerp(_Color.rgb, _MaxColor.rgb, amount) * i.color.rgb, 1.0);
                UNITY_APPLY_FOG_COLOR(i.fogCoord, result, fixed4(0, 0, 0, 0));
                return result;
            }
            ENDCG
        }
    }
}
