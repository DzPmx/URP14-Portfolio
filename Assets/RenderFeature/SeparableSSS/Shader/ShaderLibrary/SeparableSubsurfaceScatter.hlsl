#ifndef SEPARABLE_SUBSURFACE_SCATTER
#define SEPARABLE_SUBSURFACE_SCATTER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define DistanceToProjectionWindow 5.671281819617709             //1.0 / tan(0.5 * radians(20));
#define DPTimes300 1701.384545885313                             //DistanceToProjectionWindow * 300
#define SamplerSteps 25
uniform float _SSSScale;
uniform float4 _Kernel[SamplerSteps], _Jitter, _NoiseSize, _screenSize, _CameraDepthTexture_TexelSize;
TEXTURE2D(_BlitTexture); SAMPLER (sampler_BlitTexture);
TEXTURE2D(_CameraDepthTexture);     SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_Noise);
SAMPLER(sampler_Noise);

float4 _RandomSeed;
inline int2 ihash(int2 n)
{
    n = (n << 13) ^ n;
    return (n*(n*n * 15731 + 789221) + 1376312589) & 2147483647;
}

inline int3 ihash(int3 n)
{
    n = (n << 13) ^ n;
    return (n*(n*n * 15731 + 789221) + 1376312589) & 2147483647;
}

inline float2 frand(int2 n)
{
    return ihash(n) / 2147483647.0;
}

inline float3 frand(int3 n)
{
    return ihash(n) / 2147483647.0;
}

inline float2 cellNoise(float2 p)
{
    int seed = dot(p, float2(641338.4168541, 963955.16871685));
    return sin(float2(frand(int2(seed, seed - 53))) * _RandomSeed.xy + _RandomSeed.zw);
}

inline float3 cellNoise(float3 p)
{
    int seed = dot(p, float3(641738.4168541, 9646285.16871685, 3186964.168734));
    return sin(float3(frand(int3(seed, seed - 12, seed - 57))) * _RandomSeed.xyz + _RandomSeed.w);
}

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 posCS : SV_POSITION;
    float2 uv0 : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings triangleDrawVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output)
    output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);
    return output;
}

float4 SeparableSubsurface(float4 SceneColor, float2 UV, float2 SSSIntencity)
{
    float SceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,sampler_CameraDepthTexture,UV),_ZBufferParams);
    float BlurLength = DistanceToProjectionWindow / SceneDepth;
    float2 UVOffset = SSSIntencity * BlurLength;
    
    float4 BlurSceneColor = SceneColor;
    BlurSceneColor.rgb *= _Kernel[0].rgb;
    
    for (int i = 1; i < SamplerSteps; i++)
    {
        float2 SSSUV = UV + _Kernel[i].a * UVOffset;
        float4 SSSSceneColor = SAMPLE_TEXTURE2D(_BlitTexture,sampler_BlitTexture, SSSUV);
        float SSSDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,sampler_CameraDepthTexture, SSSUV),_ZBufferParams).r;
        float SSSScale = saturate(DPTimes300 * SSSIntencity * abs(SceneDepth - SSSDepth));
        SSSSceneColor.rgb = lerp(SSSSceneColor.rgb, SceneColor.rgb, SSSScale);
        BlurSceneColor.rgb += _Kernel[i].rgb * SSSSceneColor.rgb;
    }
    return BlurSceneColor;
}



#endif
