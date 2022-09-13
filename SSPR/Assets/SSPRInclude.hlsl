#ifndef SSPR_INCLUDE
#define SSPR_INCLUDE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_ReflectionRT);
sampler LinearClampSampler;

struct ReflectionInput
{
    float3 posWS;
    float4 screenPos;
    float2 screenSpaceNoise;
    float roughness;
    float SSPR_Usage;
};

half3 GetReflectionColor(ReflectionInput input)
{
    return 0;
}
#endif
