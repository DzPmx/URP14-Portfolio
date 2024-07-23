#ifndef SSSS_LIT_PASS_INCLUDED
#define SSSS_LIT_PASS_INCLUDED

#include "SkinCustomLitData.hlsl"
#include "SkinCustomLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 posOS : POSITION;
    float3 nDirOS : NORMAL;
    float4 tDirOS : TANGENT;
    float2 uv0 : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 posWS : TEXCOORD1;
    float3 nDirWS : TEXCOORD2;
    half4 tDirWS : TEXCOORD3; // xyz: tangent, w: sign
    float4 shadowCoord : TEXCOORD4;
    float4 posCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void InitializeCustomLitData(Varyings input, out HairLitData customLitData)
{
    customLitData = (HairLitData)0;

    customLitData.positionWS = input.posWS;
    customLitData.V = GetWorldSpaceNormalizeViewDir(input.posWS);
    customLitData.N = normalize(input.nDirWS);
    customLitData.T = normalize(input.tDirWS.xyz);
    customLitData.B = normalize(cross(customLitData.N, customLitData.T) * input.tDirWS.w);
    customLitData.ScreenUV = GetNormalizedScreenSpaceUV(input.posCS);
}


void InitializeCustomSurfaceData(Varyings input, out CustomSurfacedata customSurfaceData)
{
    customSurfaceData = (CustomSurfacedata)0;

    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

    //albedo & alpha & specular
    customSurfaceData.albedo = color.rgb;
    customSurfaceData.alpha = color.a;
#if defined(_ALPHATEST_ON)
    clip(customSurfaceData.alpha - _Cutoff);
#endif
    customSurfaceData.specular = (half3)0;
    customSurfaceData.reflection = SAMPLE_TEXTURE2D(_ReflectionMap, sampler_ReflectionMap, input.uv);

    //metallic & roughness
    half metallic = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv).r * _Metallic;
    customSurfaceData.metallic = saturate(metallic);

    half roughnessLobe1 = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r * _RoughnessLobe1;
    half roughnessLobe2 = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r * _RoughnessLobe2;
    customSurfaceData.roughnessLobe1 = max(roughnessLobe1, 0.001f);
    customSurfaceData.roughnessLobe2 = max(roughnessLobe2, 0.001f);
    //normalTS (tangent Space)
    float4 normalTS = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
    customSurfaceData.normalTS = UnpackNormalScale(normalTS, _Normal);

    //occlusion
    half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
    customSurfaceData.occlusion = lerp(1.0, occlusion, _OcclusionStrength);
}

Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.posOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.nDirOS, input.tDirOS);

    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
    output.nDirWS = normalInput.normalWS;
    real sign = input.tDirOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tDirWS = tangentWS;
    output.posWS = vertexInput.positionWS;
    output.posCS = vertexInput.positionCS;

    return output;
}

half4 SkinDiffusePassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    HairLitData customLitData;
    InitializeCustomLitData(input, customLitData);

    CustomSurfacedata customSurfaceData;
    InitializeCustomSurfaceData(input, customSurfaceData);
    half4 color = PBR.StandardLit(customLitData, customSurfaceData, input.posWS, input.shadowCoord, _EnvRotation);

    return color;
}

half4 SkinDualLobePassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    HairLitData customLitData;
    InitializeCustomLitData(input, customLitData);

    CustomSurfacedata customSurfaceData;
    InitializeCustomSurfaceData(input, customSurfaceData);
    float2 screenUV = input.posCS.xy / _ScaledScreenParams.xy;
    half4 color = PBR.StandardLit(customLitData, customSurfaceData, input.posWS, input.shadowCoord, _EnvRotation);
    color += SAMPLE_TEXTURE2D(_SeparableSSSTexture, sampler_SeparableSSSTexture, screenUV);
    return color;
}
#endif
