Shader "Custom/WaterShader"
{
    Properties
    {
        _Color1("Transmission Color", Color) = (0.1, 0.6, 1.0, 1)
        _Color2("Horizon Color", Color) = (0.2, 0.5, 1.0, 1)
        _Normal1("Normal 1", 2D) = "white"
        _Normal2("Normal 2", 2D) = "white"
        _Normal3("Normal 3", 2D) = "white"
        _Normal4("Normal 4", 2D) = "white"
        [ShowAsVector2] _Velocity1("Velocity 1", Vector) = ( 1,  1, 0, 0)
        [ShowAsVector2] _Velocity2("Velocity 2", Vector) = ( 1, -1, 0, 0)
        [ShowAsVector2] _Velocity3("Velocity 3", Vector) = (-1, -1, 0, 0)
        [ShowAsVector2] _Velocity4("Velocity 4", Vector) = (-1,  1, 0, 0)
        _Density1("Density 1", Float) = 1
        _Density2("Density 2", Float) = 0.5
        _Density3("Density 3", Float) = 0.25
        _Density4("Density 4", Float) = 0.125
        _Amplitude1("Amplitude 1", Float) = 1
        _Amplitude2("Amplitude 2", Float) = 0.5
        _Amplitude3("Amplitude 3", Float) = 0.25
        _Amplitude4("Amplitude 4", Float) = 0.125
        //_Refraction("Refraction", Range(0, 1)) = 0.2
        //_Shininess("Shininess", Range(0, 1)) = 0.5
    }
    SubShader{
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
        LOD 200

        Pass {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #include "CustomFunctions.hlsl"

            #define INTERNAL_DATA

            struct MeshData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                //float3 worldRefl : TEXCOORD2;
                //float3 worldRefr : TEXCOORD3;
            };

            float4 _Color1;
            float4 _Color2;
            sampler2D _Normal1;
            sampler2D _Normal2;
            sampler2D _Normal3;
            sampler2D _Normal4;
            float _Velocity1;
            float _Velocity2;
            float _Velocity3;
            float _Velocity4;
            float _Density1;
            float _Density2;
            float _Density3;
            float _Density4;
            float _Amplitude1;
            float _Amplitude2;
            float _Amplitude3;
            float _Amplitude4;
            //float _Refraction;
            //float _Shininess;

            Interpolators vert(MeshData v) {
                // Calculate wave height
                float height = 0;
                CompositePerlinNoise_float(v.vertex.xz, _Velocity1, _Velocity2, _Velocity3, _Velocity4, _Density1, _Density2, _Density3, _Density4, _Amplitude1, _Amplitude2, _Amplitude3, _Amplitude4, height);
                v.uv.y = height;

                // Pass data to the fragment shader
                Interpolators o;
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                //o.worldRefl = WorldReflectionVector(o, o.worldPos);
                //o.worldRefr = WorldRefractionVector(o, o.worldPos, _Refraction);
                return o;
            }
            fixed4 frag(Interpolators i) : SV_Target{
                // Calculate the derivatives of the vertex position with respect to the texture coordinates.
                float2 ddx2D = ddx(i.worldPos.xy);
                float2 ddy2D = ddy(i.worldPos.xy);
                float3 ddx3D = float3(ddx2D.x, 0, ddx2D.y);
                float3 ddy3D = float3(ddy2D.x, 0, ddy2D.y);

                // Calculate the surface normal using the cross product of the derivatives.
                float3 normal = normalize(cross(ddx3D, ddy3D));

                // Calculate the fresnel effect using the angle between the camera ray and normal vectors.
                float fresnel = dot(normalize(UnityWorldSpaceViewDir(i.worldPos)), normal);

                return float4(0.5, 0.5, 0.5, 1);

                // Sample the color based on the fresnel effect.
                float4 color = lerp(_Color1, _Color2, pow(1 - fresnel, 5));
                return color;

                //float fresnel = Fresnel(i.worldRefl, normal.xyz);
                //return waterColor = lerp(_Color1, _Color2, fresnel);
            }
            ENDCG
        }
    }
}
