#ifndef HAIR_PASS_INCLUDED
#define HAIR_PASS_INCLUDED

#include "HairInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "HairShading.hlsl"

struct Attributes
{
    float4 posOS : POSITION;
    float3 nDirOS : NORMAL;
    float4 tDirOS : TANGENT;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0;
    float3 posWS : TEXCOORD1;
    float3 nDirWS : TEXCOORD2;
    half4 tDirWS : TEXCOORD3; // xyz: tangent, w: sign
    float4 shadowCoord : TEXCOORD4;
    float2 uv1: TEXCOORD5;
    float4 posCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.posOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.nDirOS, input.tDirOS);
    output.uv = input.uv0;
    output.uv1 = input.uv1;
    output.nDirWS = normalInput.normalWS;
    real sign = input.tDirOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
    output.tDirWS = tangentWS;
    output.posWS = vertexInput.positionWS;
    output.shadowCoord = GetShadowCoord(vertexInput);
    output.posCS = vertexInput.positionCS;

    return output;
}

void InitializeHairLitData(Varyings input, HairSurfaceData hairSurfaceData, out HairLitData customLitData, float face)
{
    customLitData = (HairLitData)0;

    customLitData.positionWS = input.posWS;
    customLitData.V = GetWorldSpaceNormalizeViewDir(input.posWS);
    customLitData.N = normalize(input.nDirWS);
    half fuzzySpecualr = lerp(_FuzzySpecualr, -_FuzzySpecualr, hairSurfaceData.id);
    half3 fuzzyB = normalize(float3(0, -1, fuzzySpecualr)) * face;
    customLitData.T = normalize(input.tDirWS) * face;
    customLitData.B = normalize(cross(customLitData.N - fuzzyB, customLitData.T) * input.tDirWS.w);
    customLitData.ScreenUV = GetNormalizedScreenSpaceUV(input.posCS);
}

void InitializeHairSurfaceData(Varyings input, out HairSurfaceData hairSurfaceData)
{
    hairSurfaceData = (HairSurfaceData)0;
    hairSurfaceData.baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 mixMap = SAMPLE_TEXTURE2D(_ID_AO_Root_Alpha, sampler_ID_AO_Root_Alpha, input.uv);
    half AO2U = SAMPLE_TEXTURE2D(_Occlusion2UMap, sampler_Occlusion2UMap, input.uv1);
    hairSurfaceData.id = mixMap.r;
    hairSurfaceData.ao = AO2U * mixMap.g;
    hairSurfaceData.root = mixMap.b;
    hairSurfaceData.alpha = mixMap.a;
    half3 baseColor = lerp(hairSurfaceData.baseColor * _TipColor, hairSurfaceData.baseColor * _RootColor,
                           hairSurfaceData.root);
    hairSurfaceData.baseColor = lerp(baseColor, baseColor * hairSurfaceData.ao, _OcclusionStrength);
    hairSurfaceData.roughness = _Roughness;
    hairSurfaceData.specular = _Specular;
}

half3 HairMarschnerDirectionalBRDF(Light light, HairSurfaceData hairSurafceData, HairLitData hairLitData)
{
    HairData hairData = (HairData)0;
    InitalHairData(hairData, hairLitData.B, hairLitData.V, light.direction, hairSurafceData.roughness);
    half3 diffuseTerm = KajiyaKayDiffuseAttenuation(hairSurafceData.baseColor,
                                                    _Scatter,
                                                    light.direction, hairLitData.V, hairLitData.B);
    half3 specualrTerm = MachsnerHairSpecularTerm(hairData, hairSurafceData.baseColor, _Specular);
    half3 radiance = light.color * light.distanceAttenuation * (light.shadowAttenuation * 0.5 + 0.5) * PI;
    return (diffuseTerm + specualrTerm) * radiance;
}

half3 HairKajiyaKayDirectionalBRDF(Light light, HairSurfaceData hairSurafceData, HairLitData hairLitData)
{
    HairData hairData = (HairData)0;
    InitalHairData(hairData, hairLitData.B, hairLitData.V, light.direction, hairSurafceData.roughness);
    half3 diffuseTerm = KajiyaKayDiffuseAttenuation(hairSurafceData.baseColor,
                                                    _Scatter,
                                                    light.direction, hairLitData.V, hairLitData.B);
    half3 specualrTerm = KajiyaKaySpecularTerm(hairLitData, light, _PrimaryColor, _PrimaryShift, _SecondaryColor,
                                               _SecondaryShift, _SpecPower, _SpecularWidth, _SpecualrScale);
    half3 radiance = light.color * light.distanceAttenuation * (light.shadowAttenuation * 0.5 + 0.5) * PI;
    return (diffuseTerm + specualrTerm) * radiance;
}

half4 HairMachsnerOpaqueFragment(Varyings input, float face:VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half3 color = (half3)0;
    HairSurfaceData hairSurafceData;
    InitializeHairSurfaceData(input, hairSurafceData);
    HairLitData hairLitData;
    InitializeHairLitData(input, hairSurafceData, hairLitData, face);

    //Lighting
    Light mainLight = GetMainLight(input.shadowCoord, normalize(input.posWS), half4(1.0, 1.0, 1.0, 1.0));
    color += HairMarschnerDirectionalBRDF(mainLight, hairSurafceData, hairLitData);
    int additionalLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightCount; ++i)
    {
        Light additionalLight = GetAdditionalLight(i, input.posWS, half4(1.0, 1.0, 1.0, 1.0));
        color += HairMarschnerDirectionalBRDF(additionalLight, hairSurafceData, hairLitData);
    }
    clip(hairSurafceData.alpha - _Cutoff);
    return half4(color, 1.0);
}

half4 HairMachsnerTransparentFragment(Varyings input, float face:VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half3 color = (half3)0;
    HairSurfaceData hairSurafceData;
    InitializeHairSurfaceData(input, hairSurafceData);
    HairLitData hairLitData;
    InitializeHairLitData(input, hairSurafceData, hairLitData, face);

    //Lighting
    Light mainLight = GetMainLight(input.shadowCoord, normalize(input.posWS), half4(1.0, 1.0, 1.0, 1.0));
    color += HairMarschnerDirectionalBRDF(mainLight, hairSurafceData, hairLitData);
    return half4(color, hairSurafceData.alpha);
}
half4 HairKajiyaKayOpaqueFragment(Varyings input, float face:VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half3 color = (half3)0;
    HairSurfaceData hairSurafceData;
    InitializeHairSurfaceData(input, hairSurafceData);
    HairLitData hairLitData;
    InitializeHairLitData(input, hairSurafceData, hairLitData, face);

    //Lighting
    Light mainLight = GetMainLight(input.shadowCoord, normalize(input.posWS), half4(1.0, 1.0, 1.0, 1.0));
    color += HairKajiyaKayDirectionalBRDF(mainLight, hairSurafceData, hairLitData);
    int additionalLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightCount; ++i)
    {
        Light additionalLight = GetAdditionalLight(i, input.posWS, half4(1.0, 1.0, 1.0, 1.0));
        color += HairKajiyaKayDirectionalBRDF(additionalLight, hairSurafceData, hairLitData);
    }
    clip(hairSurafceData.alpha - _Cutoff);
    return half4(color, 1.0);
}

half4 HairKajiyaKayTransparentFragment(Varyings input, float face:VFACE) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half3 color = (half3)0;
    HairSurfaceData hairSurafceData;
    InitializeHairSurfaceData(input, hairSurafceData);
    HairLitData hairLitData;
    InitializeHairLitData(input, hairSurafceData, hairLitData, face);

    //Lighting
    Light mainLight = GetMainLight(input.shadowCoord, normalize(input.posWS), half4(1.0, 1.0, 1.0, 1.0));
    color += HairKajiyaKayDirectionalBRDF(mainLight, hairSurafceData, hairLitData);
    int additionalLightCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightCount; ++i)
    {
        Light additionalLight = GetAdditionalLight(i, input.posWS, half4(1.0, 1.0, 1.0, 1.0));
        color += HairKajiyaKayDirectionalBRDF(additionalLight, hairSurafceData, hairLitData);
    }
    return half4(color, hairSurafceData.alpha);
}


half4 HairShadowFragment(Varyings input, float face:VFACE) : SV_Target
{
    HairSurfaceData hairSurafceData;
    InitializeHairSurfaceData(input, hairSurafceData);
    clip(hairSurafceData.alpha - _Cutoff);
    return 0;
}



#endif
