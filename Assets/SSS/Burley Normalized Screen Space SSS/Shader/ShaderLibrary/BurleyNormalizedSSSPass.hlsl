#ifndef BURLEY_NORMALIZED_SSS_PASS_INCLUDED
#define BURLEY_NORMALIZED_SSS_PASS_INCLUDED

TEXTURE2D(_BlitTexture);
SAMPLER(sampler_BlitTexture);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;

float4 _ShapeParamsAndMaxScatterDists;
float4 _WorldScalesAndFilterRadiiAndThicknessRemaps;
float4x4 _InvProjectMatrix;
float _FilterRadii;
float _WorldScale;

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


Varyings Vertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.posCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.uv0 = GetFullScreenTriangleTexCoord(input.vertexID);

    return output;
}

float4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 posSS = input.uv0 * _CameraDepthTexture_TexelSize.zw;
    float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv0).r;
    float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
    float2 cornerPosNDC = input.uv0 + 0.5 * _CameraDepthTexture_TexelSize.xy;
    #if UNITY_REVERSED_Z
    depth = 1 - depth;
    #endif
    depth = 2 * depth - 1;

    float3 centerPosVS = ComputeViewSpacePosition(input.uv0, depth, _InvProjectMatrix);
    float3 cornerPosVS = ComputeViewSpacePosition(cornerPosNDC, depth, _InvProjectMatrix);
    float mmPerUnit = 1000.0;
    float unitsPerMm = rcp(mmPerUnit);
    float worldScale = _WorldScale;
    float unitsPerPixel = max(0.0001f, 2.0 * abs(cornerPosVS.x - centerPosVS.x)) * worldScale; //一个像素覆盖多少米
    float pixelsPerMm = rcp(unitsPerPixel) * unitsPerMm; //1毫米覆盖多少个像素
    //SSS散射最大距离(单位毫米) 
    //已经预先在C#计算好
    float filterRadius = _FilterRadii;
    float filterArea = PI * Sq(filterRadius * pixelsPerMm); //圆盘上覆盖多少个像素

    uint sampleCount = (uint)(filterArea / (SSS_PIXELS_PER_SAMPLE)); //圆盘上有多少个采样点
    uint sampleBudget = (uint)32;
    uint n = min(sampleCount, sampleBudget);
    float3 S = _ShapeParamsAndMaxScatterDists.rgb;
    float d = _ShapeParamsAndMaxScatterDists.a;
    float2 pixelCoord = posSS;
    float3 totalIrradiance = 0;
    float3 totalWeight = 0;

    //根据屏幕坐标生成一个随机角度
    float phase = TWO_PI * GenerateHashedRandomFloat(uint3(posSS, (uint)(depth * 16777216)));
    for (uint i = 0; i < n; i++)
    {
        float scale = rcp(n);
        float offset = rcp(n) * 0.5;

        float sinPhase, cosPhase;
        sincos(phase, sinPhase, cosPhase);

        float r, rcpPdf;
        //通过i* scale + offset [0,1]的均匀递增数作为随机数计算出重要性采样的采样距离r和1/PDF
        SampleBurleyDiffusionProfile(i * scale + offset, d, r, rcpPdf);
        float phi = SampleDiskGolden(i, n).y;
        float sinPhi, cosPhi;
        sincos(phi, sinPhi, cosPhi);

        float sinPsi = cosPhase * sinPhi + sinPhase * cosPhi; // sin(phase + phi)
        float cosPsi = cosPhase * cosPhi - sinPhase * sinPhi; // cos(phase + phi)
        float2 vec = r * float2(cosPsi, sinPsi);
        //根据采样距离r,在圆盘上随机角度采样(可以在切空间中进行，这里简化一下)
        float2 position = pixelCoord + round((pixelsPerMm * r) * float2(cosPsi, sinPsi));

        float xy2 = r * r;
        float2 sampleUV = position * _CameraDepthTexture_TexelSize.xy;
        float3 irradiance = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture,
                                               sampleUV);
        if (irradiance.b > 0.0) //因为没有使用模板测试,在Diffuse计算时需要通过"diffuse.b = max(diffuse.b, HALF_MIN) 来表示"
        {
            float sampleDevZ = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, sampleUV).r;
            float sampleLinearZ = LinearEyeDepth(sampleDevZ, _ZBufferParams);
            float relZ = sampleLinearZ - linearDepth;
            //根据r计算DiffusionProfile和权重
            float3 weight = ComputeBilateralWeight(xy2, relZ, mmPerUnit, S, rcpPdf);
            totalIrradiance += weight * irradiance;
            totalWeight += weight;
        }
    }
    if (dot(totalIrradiance, float3(1, 1, 1)) == 0.0)
    {
        return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv0);
    }
    totalWeight = max(totalWeight, FLT_MIN);
    return float4(totalIrradiance / totalWeight, 1.0);
}
#endif
