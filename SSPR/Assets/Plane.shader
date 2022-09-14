Shader "SSPR/Plane"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("BaseMap", 2D) = "white" {}
        _Roughness("_Roughness", range(0,1)) = 0.25 
        _NoiseIntensity("NoiseIntensity", range(-0.2, 0.2)) = 0.0
        [NoScaleOffset] _NoiseMap("NoiseMap", 2D) = "gray" {}
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "SSPR_LightMode" }
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag

            #include "SSPRInclude.hlsl"
            
            struct Attributes
            {
                float4 vertex: POSITION;
                float2 uv: TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 screenPos    : TEXCOORD1;
                float3 posWS        : TEXCOORD2;
                float4 pos  : SV_POSITION;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NoiseMap);
            SAMPLER(sampler_NoiseMap);
            
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Roughness;
            half _NoiseIntensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vertexInput.positionCS;
                o.posWS = vertexInput.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(vertexInput.positionCS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                
                float2 noise = SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap, i.uv);
                noise = noise *2-1;
                noise.y = -abs(noise); //hide missing data, only allow offset to valid location
                noise.x *= 0.25;
                noise *= _NoiseIntensity;
                
                ReflectionInput reflectionData = (ReflectionInput)0;
                reflectionData.posWS = i.posWS;
                reflectionData.screenPos = i.screenPos;
                reflectionData.roughness = _Roughness;
                reflectionData.SSPR_Usage = baseColor.a;
                reflectionData.screenSpaceNoise = noise;

                half3 reflectionColor = GetReflectionColor(reflectionData);

                half a = SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap, i.uv);
                half3 finalColor = lerp(baseColor.rgb, reflectionColor, a);
                return half4(finalColor, 1);
            }

            ENDHLSL
        }
    }
}
