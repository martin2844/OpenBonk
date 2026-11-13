Shader "Custom/Outline Fill" {
    Properties {
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2
    }
    SubShader {
        Tags { "Queue"="Transparent+110" "RenderType"="Transparent" "DisableBatching"="true" }
        Pass {
            Name "Fill"
            Cull Off ZTest [_ZTest] ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            Stencil { Ref 1 Comp NotEqual }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float3 smoothNormal : TEXCOORD3; };
            struct v2f { float4 position : SV_POSITION; };
            float _OutlineWidth;
            fixed4 _OutlineColor;
            v2f vert(appdata input) {
                v2f output;
                float3 normal = any(input.smoothNormal) ? input.smoothNormal : input.normal;
                float3 viewPosition = UnityObjectToViewPos(input.vertex);
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, normal));
                viewPosition += viewNormal * (-viewPosition.z * _OutlineWidth * 0.001);
                output.position = UnityViewToClipPos(viewPosition);
                return output;
            }
            fixed4 frag(v2f input) : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}
