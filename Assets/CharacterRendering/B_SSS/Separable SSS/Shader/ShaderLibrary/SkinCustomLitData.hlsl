#ifndef SSS_LIT_DATA_INCLUDED
#define SSS_LIT_DATA_INCLUDED

struct HairLitData
{
    float3 positionWS;
    half3  V; //ViewDirWS
    half3  N; //NormalWS
    half3  B; //BinormalWS
    half3  T; //TangentWS
    float2 ScreenUV;
};

struct CustomSurfacedata
{
    half3 albedo;
    half3 specular;
    half3 normalTS;
    half  metallic;
    half  roughnessLobe1;
    half  roughnessLobe2;
    half  occlusion;
    half  alpha;
    half reflection;
};

struct CustomDualLobeSkinSurfacedata
{
    half3 albedo;
    half3 specular;
    half3 normalTS;
    half  metallic;
    half  roughnessLobe1;
    half  roughnessLobe2;
    half  occlusion;
    half  alpha;
};

struct CustomClearCoatData
{
    half3 clearCoatNormal;
    half  clearCoat;
    half  clearCoatRoughness;
};
#endif