Shader "Unlit/SimpleBarShader"
{
    Properties
    {
        _Color("Color", Color) = (0, 1, 0, 1)
        _Background("Background", Color) = (0, 0, 0, 1)
        _Value("Value", Range(0, 1)) = 0.75
    }
    SubShader
    {
        // Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
        Tags {"RenderType" = "Opaque"}
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float4 _Background;
            float _Value;

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Interpolators i) : SV_TARGET
            {
                return float4(0.25, 0.5, 0.75, 1);
                return float4(i.uv, 0, 1);
                return i.uv.x < _Value ? _Color : _Background;
            }
            ENDCG
        }
    }
}
