#ifndef MY_POST_STACK
#define MY_POST_STACK
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities//blit.hlsl"

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);
SAMPLER(sampler_linear_clamp);
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

float4 ColorTintFragment(Varyings input) : SV_Target
{
    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0, 0) * float4(
        _ColorTint.rgb, 1.0);
}

//X: GussianBlurRadius Y:0 Z:0 W：mipmap
float4 _GaussianBlurParams; 

float4 GaussianBlurHorizontalPassFragment(Varyings input):SV_TARGET
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
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.x*_GaussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+float2(offset, 0.0), _GaussianBlurParams.w).rgb * weights[i];
    }
    return float4(color, 1.0);
}

float4 GaussianBlurVerticalPassFragment(Varyings input):SV_TARGET
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
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.y*_GaussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+float2(0.0,offset), _GaussianBlurParams.w).rgb* weights[i];
    }
    return float4(color, 1.0);
}


//X: BoxBlurRadius Y:0 Z:0 W：mipmap
float4 _BoxBlurParams;

float4 BoxBlurFragment(Varyings input):SV_TARGET
{
    float4 d = _BlitTexture_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * _BoxBlurParams.x;
    float3 color = 0;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+d.xy, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+d.zy, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+d.xw, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_linear_clamp, input.uv0+d.zw, _BoxBlurParams.w).rgb * 0.25;
    return float4(color, 1.0);
}


 float _KawasePixelOffset;

float4 KawaseBlurFragment(Varyings input):SV_TARGET
{
    float3 color = 0;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_linear_clamp,
                              input.uv0 + float2(_KawasePixelOffset +0.5, _KawasePixelOffset +0.5)*
                              _BlitTexture_TexelSize).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_linear_clamp,
                              input.uv0 + float2(-_KawasePixelOffset -0.5, _KawasePixelOffset +0.5)*
                              _BlitTexture_TexelSize).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_linear_clamp,
                              input.uv0 + float2(-_KawasePixelOffset -0.5, -_KawasePixelOffset -0.5)*
                              _BlitTexture_TexelSize).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_linear_clamp,
                              input.uv0 + float2(_KawasePixelOffset +0.5, -_KawasePixelOffset -0.5)*
                              _BlitTexture_TexelSize).rgb;
    return float4(color * 0.25, 1.0);
}

#endif
