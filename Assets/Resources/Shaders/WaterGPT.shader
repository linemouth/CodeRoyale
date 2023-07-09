Shader "Custom/WaterGPT"
{
    Properties{
        _Color("Water Color", Color) = (1, 1, 1, 1)
        _WaveSpeed("Wave Speed", Range(0, 5)) = 1
        _WaveHeight("Wave Height", Range(0, 2)) = 0.2
        _ReflectionSlider("Reflection Intensity", Range(0, 1)) = 0.5
        _FresnelColor1("Fresnel Color 1", Color) = (0, 0, 0, 1)
        _FresnelColor2("Fresnel Color 2", Color) = (1, 1, 1, 1)
    }

        SubShader{
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f {
                    float3 worldNormal : TEXCOORD0;
                    float3 worldViewDir : TEXCOORD1;
                    float4 vertex : SV_POSITION;
                };

                float4 _Color;
                float _WaveSpeed;
                float _WaveHeight;
                float _ReflectionSlider;
                float4 _FresnelColor1;
                float4 _FresnelColor2;

                v2f vert(appdata v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex + float4(0, sin(_Time.y * _WaveSpeed + v.vertex.x + v.vertex.z) * _WaveHeight, 0, 0));
                    o.worldNormal = mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz;
                    o.worldViewDir = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target {
                    float fresnel = 1 - abs(dot(normalize(i.worldNormal), normalize(i.worldViewDir)));
                    fresnel = saturate(fresnel);
                    fresnel = pow(fresnel, 3);

                    fixed4 color = lerp(_FresnelColor1, _FresnelColor2, fresnel);
                    color *= _Color;
                    color.a = 1;

                    color.rgb *= _ReflectionSlider;

                    return color;
                }
                ENDCG
            }
    }
}