#ifndef MY_POST_STACK
#define MY_POST_STACK
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities//blit.hlsl"

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);
float4 _BlitTexture_TexelSize;
uniform float4 _BlitScaleBias;
uniform float4 _BlitScaleBiasRt;
uniform float _BlitMipLevel;
uniform float2 _BlitTextureSize;
uniform uint _BlitPaddingSize;
uniform int _BlitTexArraySlice;
uniform float4 _BlitDecodeInstructions;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float2 uv0 : TEXCOORD0;
};


Varyings triangleDrawVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    #if SHADER_API_GLES
    output.posCS = input.posOS;
    output.uv0 = input.uv0;
    #else
    output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);
    #endif

    return output;
}


float4 _ColorTint;

float4 colorTintFrag(Varyings input) : SV_Target
{
    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0, 0) * float4(
        _ColorTint.rgb, 1.0);
}

float4 _GussianBlurParams;

float4 GussianBlurHorizontalPassFragment(Varyings input):SV_TARGET
{
    float3 color = 0.0;
    float offests[] = {
        -4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
    };
    float weights[] = {
        0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
        0.19459459, 0.12162162, 0.05405405, 0.01621622
    };
    for (int i = 0; i < 9; i++)
    {
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.x*_GussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0+float2(offset, 0.0), _GussianBlurParams.w).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 GussianBlurVerticalPassFragment(Varyings input):SV_TARGET
{
    float3 color = 0.0;
    float offests[] = {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] = {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    for (int i = 0; i < 5; i++)
    {
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.y*_GussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0+float2(0.0,offset), _GussianBlurParams.w).rgb* weights[i];
    }
    return float4(color, 1.0);
}

#endif
