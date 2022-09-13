Shader "SSPR/Plane"
{
    Properties
    {
        [MainColor] _BaseColor("BaseColor", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("BaseMap", 2D) = "black" {}
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
            
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex.xyz);
                o.pos = vertexInput.positionCS;
                o.posWS = vertexInput.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.screenPos = GetScreenPos(vertexInput.positionCS);
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            { 
                return _BaseColor;
            }

            ENDHLSL
        }
    }
}
