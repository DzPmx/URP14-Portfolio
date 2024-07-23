#ifndef HAIR_INPUT_INCLUDED
#define HAIR_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _TipColor;
    half4 _RootColor;

    half _FuzzySpecualr;
    half _Roughness;
    half _Specular;
    half _Scatter;
    half _OcclusionStrength;
    half _Cutoff;
    half _Offset;

    half _SpecularWidth;
    half _SpecPower;
    half _SpecualrScale;
    half _PrimaryShift;
    half4 _PrimaryColor;
    half _SecondaryShift;
    half4 _SecondaryColor;
CBUFFER_END

TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
TEXTURE2D(_ID_AO_Root_Alpha);   SAMPLER(sampler_ID_AO_Root_Alpha);
TEXTURE2D(_Occlusion2UMap);     SAMPLER(sampler_Occlusion2UMap);
#endif
