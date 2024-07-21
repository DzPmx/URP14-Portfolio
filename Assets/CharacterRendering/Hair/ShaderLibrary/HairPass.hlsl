#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "HairShading.hlsl"
#include "HairInput.hlsl"
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
    output.shadowCoord = GetShadowCoord(vertexInput);
    output.posCS = vertexInput.positionCS;

    return output;
}

half4 StandardLitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half4 color=(half4)0;
    
    //diffuse
    //Specualr

    return color;
}
#endif
