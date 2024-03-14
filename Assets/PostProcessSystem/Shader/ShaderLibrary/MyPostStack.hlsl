#ifndef MY_POST_STACK
#define MY_POST_STACK
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Random.hlsl"
//#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities//blit.hlsl"

TEXTURE2D (_BlitTexture);
SAMPLER (sampler_BlitTexture);
//SAMPLER(sampler_linear_clamp);
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
    UNITY_VERTEX_INPUT_INSTANCE_ID
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
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.x * _GaussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + float2(offset, 0.0),
                                      _GaussianBlurParams.w).rgb * weights[i];
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
        float offset = offests[i] * 2.0 * _BlitTexture_TexelSize.y * _GaussianBlurParams.x;
        color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + float2(0.0, offset),
                                      _GaussianBlurParams.w).rgb * weights[i];
    }
    return float4(color, 1.0);
}


//X: BoxBlurRadius Y:0 Z:0 W：mipmap
float4 _BoxBlurParams;

float4 BoxBlurFragment(Varyings input):SV_TARGET
{
    float4 d = _BlitTexture_TexelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * _BoxBlurParams.x;
    float3 color = 0;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + d.xy, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + d.zy, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + d.xw, _BoxBlurParams.w).rgb * 0.25;
    color += SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_BlitTexture, input.uv0 + d.zw, _BoxBlurParams.w).rgb * 0.25;
    return float4(color, 1.0);
}


float _KawasePixelOffset;
float4 KawaseBlurFragment(Varyings input):SV_TARGET
{
    float3 color = 0;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                              input.uv0 + float2(_KawasePixelOffset + 0.5, _KawasePixelOffset + 0.5) *
                              _BlitTexture_TexelSize.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                              input.uv0 + float2(-_KawasePixelOffset - 0.5, _KawasePixelOffset + 0.5) *
                              _BlitTexture_TexelSize.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                              input.uv0 + float2(-_KawasePixelOffset - 0.5, -_KawasePixelOffset - 0.5) *
                              _BlitTexture_TexelSize.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                              input.uv0 + float2(_KawasePixelOffset + 0.5, -_KawasePixelOffset - 0.5) *
                              _BlitTexture_TexelSize.xy).rgb;
    return float4(color * 0.25, 1.0);
}


float _DualBlurOffset;

float4 DualKawaseBlurDownSampleFragment(Varyings input): SV_Target
{
    float4 uv01 = 0;
    float4 uv23 = 0;
    _BlitTexture_TexelSize *= 0.5;
    uv01.xy = input.uv0 - _BlitTexture_TexelSize * float2(1 + _DualBlurOffset, 1 + _DualBlurOffset); //top right
    uv01.zw = input.uv0 + _BlitTexture_TexelSize * float2(1 + _DualBlurOffset, 1 + _DualBlurOffset); //bottom left
    uv23.xy = input.uv0 - float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * float2(
        1 + _DualBlurOffset, 1 + _DualBlurOffset); //top left
    uv23.zw = input.uv0 + float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * float2(
        1 + _DualBlurOffset, 1 + _DualBlurOffset); //bottom right

    float3 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0).rgb * 4;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv01.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv01.zw).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv23.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv23.zw).rgb;

    return float4(color * 0.125, 1.0);
}

float4 DualKawaseBlurUpSampleFragment(Varyings input): SV_Target
{
    _BlitTexture_TexelSize *= 0.5;
    _DualBlurOffset = float2(1 + _DualBlurOffset, 1 + _DualBlurOffset);
    float4 uv01 = 0;
    float4 uv23 = 0;
    float4 uv45 = 0;
    float4 uv67 = 0;
    uv01.xy = input.uv0 + float2(-_BlitTexture_TexelSize.x * 2, 0) * _DualBlurOffset;
    uv01.zw = input.uv0 + float2(-_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y) * _DualBlurOffset;
    uv23.xy = input.uv0 + float2(0, _BlitTexture_TexelSize.y * 2) * _DualBlurOffset;
    uv23.zw = input.uv0 + _BlitTexture_TexelSize * _DualBlurOffset;
    uv45.xy = input.uv0 + float2(_BlitTexture_TexelSize.x * 2, 0) * _DualBlurOffset;
    uv45.zw = input.uv0 + float2(_BlitTexture_TexelSize.x, -_BlitTexture_TexelSize.y) * _DualBlurOffset;
    uv67.xy = input.uv0 + float2(0, -_BlitTexture_TexelSize.y * 2) * _DualBlurOffset;
    uv67.zw = input.uv0 - _BlitTexture_TexelSize * _DualBlurOffset;

    float3 color = 0;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv01.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv01.zw).rgb * 2;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv23.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv23.zw).rgb * 2;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv45.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv45.zw).rgb * 2;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv67.xy).rgb;
    color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv67.zw).rgb * 2;

    return float4(color * 0.0833, 1.0);
}


float4 _GoldenRot;
float4 _BokehBlurParams;
#define _BokehBlurIteration _BokehBlurParams.x
#define _BokehBlurRadius _BokehBlurParams.y

float4 BokehBlurFragment(Varyings input): SV_Target
{
    half2x2 rot = half2x2(_GoldenRot.xy,_GoldenRot.zw);
    half4 accumulator = 0.0;
    half4 divisor = 0.0;

    half r = 1.0;
    half2 angle = half2(0.0, _BokehBlurRadius);

    for (int j = 0; j < _BokehBlurIteration; j++)
    {
        r += 1.0 / r;
        angle = mul(rot, angle);
        half4 bokeh = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                       float2(input.uv0 + _BlitTexture_TexelSize.xy * (r - 1.0) * angle));
        accumulator += bokeh * bokeh;
        divisor += bokeh;
    }
    return accumulator / divisor;
}


float3 _TiltShiftBlurGradient;
float4 _TiltBokehBlurParams;
	
#define _TiltShiftBokehBlurOffset _TiltShiftBlurGradient.x
#define _TiltShiftBokehBlurArea _TiltShiftBlurGradient.y
#define _TiltShiftBokehBlurSpread _TiltShiftBlurGradient.z
#define _TiltShiftBokehBlurIteration _TiltBokehBlurParams.x
#define _TiltShiftBokehBlurRadius _TiltBokehBlurParams.y

float TiltShiftMask(float2 uv)
{
    float centerY = uv.y * 2.0 - 1.0 + _TiltShiftBokehBlurOffset; // [0,1] -> [-1,1]
    return pow(abs(centerY * _TiltShiftBokehBlurArea), _TiltShiftBokehBlurSpread);
}
float4 TiltShiftBokehBlurDebug(Varyings input): SV_Target
{
    return TiltShiftMask(input.uv0);
}

float4 TiltShiftBokehBlurFragment(Varyings input): SV_Target
{
    half2x2 rot = half2x2(_GoldenRot);
    half4 accumulator = 0.0;
    half4 divisor = 0.0;

    half r = 1.0;
    half2 angle = half2(0.0, _TiltShiftBokehBlurRadius * saturate(TiltShiftMask(input.uv0)));

    for (int j = 0; j < _TiltShiftBokehBlurIteration; j++)
    {
        r += 1.0 / r;
        angle = mul(rot, angle);
        half4 bokeh = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                       float2(input.uv0 + _BlitTexture_TexelSize.xy * (r - 1.0) * angle));
        accumulator += bokeh * bokeh;
        divisor += bokeh;
    }
    return accumulator / divisor;
}


float4 _IrisBokehBlurParams;
float3 	_IrisBokehBlurGradient;
#define _IrisBokehBlurOffset _IrisBokehBlurGradient.xy
#define _IrisBokehBlurAreaSize _IrisBokehBlurGradient.z
#define _IrisBokehBlurIteration _IrisBokehBlurParams.x
#define _IrisBokehBlurRadius _IrisBokehBlurParams.y

float IrisMask(float2 uv)
{
    float2 center = uv * 2.0 - 1.0 + _IrisBokehBlurOffset; // [0,1] -> [-1,1] 
    return dot(center, center) * _IrisBokehBlurAreaSize;
}

float4 IrisBokehBlurFragmentDebug(Varyings input): SV_Target
{
    return IrisMask(input.uv0);
}

float4 IrisBokehBlurFragment(Varyings input): SV_Target
{
    half2x2 rot = half2x2(_GoldenRot);
    half4 accumulator = 0.0;
    half4 divisor = 0.0;
		
    half r = 1.0;
    half2 angle = half2(0.0, _IrisBokehBlurRadius * saturate(IrisMask(input.uv0)));
		
    for (int j = 0; j < _IrisBokehBlurIteration; j ++)
    {
        r += 1.0 / r;
        angle = mul(rot, angle);
        half4 bokeh = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(input.uv0 + _BlitTexture_TexelSize * (r - 1.0) * angle));
        accumulator += bokeh * bokeh;
        divisor += bokeh;
    }
    return accumulator / divisor;
}



float2 _GrainyBlurParams;	
#define _GrainyBlurRadius _GrainyBlurParams.x
#define _GrainyIteration _GrainyBlurParams.y



float4 GrainyBlurFragment(Varyings input): SV_Target
{
    half2 randomOffset = float2(0.0, 0.0);
    half4 finalColor = half4(0.0, 0.0, 0.0, 0.0);
    float random = Rand(input.uv0);
		
    for (int k = 0; k < int(_GrainyIteration); k ++)
    {
        random = frac(43758.5453 * random + 0.61432);;
        randomOffset.x = (random - 0.5) * 2.0;
        random = frac(43758.5453 * random + 0.61432);
        randomOffset.y = (random - 0.5) * 2.0;
			
        finalColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, half2(input.uv0 + randomOffset * _GrainyBlurRadius));
    }
    return finalColor / _GrainyIteration;
}

float4 _RadialBlurParams;
	
#define _RadialBlurRadius _RadialBlurParams.x
#define _RadialBlurIteration _RadialBlurParams.y
#define _RadialBlurCenter _RadialBlurParams.zw
float4 RadialBlurFragment(Varyings input): SV_Target
{
    float2 blurVector = (_RadialBlurCenter -input.uv0) * _RadialBlurRadius;
    half4 acumulateColor = half4(0, 0, 0, 0);
    
    for (int j = 0; j < _RadialBlurIteration; j ++)
    {
        acumulateColor += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
        input.uv0.xy += blurVector;
    }
		
    return acumulateColor / _RadialBlurIteration;
}



float3 _DirectionalBlurParams;	
#define _DirectionalBlurIteration _DirectionalBlurParams.x
#define _DirectionalBlurDir _DirectionalBlurParams.yz
float4 DirectionalBlurFragment(Varyings input): SV_Target
{
    half4 color = half4(0.0, 0.0, 0.0, 0.0);

    for (int k = -_DirectionalBlurIteration; k < _DirectionalBlurIteration; k++)
    {
        color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0 - _DirectionalBlurDir * k);
    }
    half4 finalColor = color / (_DirectionalBlurIteration * 2.0);
    return finalColor;
}


uniform float4 _RGBSplitParams;
uniform float3 _RGBSplitParams2;

#define _RGBSplitFading _RGBSplitParams.x
#define _RGBSplitAmount _RGBSplitParams.y
#define _RGBSplitSpeed _RGBSplitParams.z
#define _RGBSplitCenterFading _RGBSplitParams.w
#define _RGBSplitTimeX _RGBSplitParams2.x
#define _RGBSplitAmountR _RGBSplitParams2.y
#define _RGBSplitAmountB _RGBSplitParams2.z

float4 RGBSplitHorizontalFragment(Varyings input): SV_Target
{
		
    float2 uv = input.uv0;
    float time = _RGBSplitTimeX * 6 * _RGBSplitSpeed;
    float splitAmount = (1.0 + sin(time)) * 0.5;
    splitAmount *= 1.0 + sin(time * 2) * 0.5;
    splitAmount = pow(splitAmount, 3.0);
    splitAmount *= 0.05;
    float distance = length(uv - float2(0.5, 0.5));
    splitAmount *=  _RGBSplitFading * _RGBSplitAmount;
    splitAmount *= lerp(1, distance, _RGBSplitCenterFading);

    float3 colorR = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x + splitAmount * _RGBSplitAmountR, uv.y)).rgb;
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
    float3 colorB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x - splitAmount * _RGBSplitAmountB, uv.y)).rgb;

    float3 splitColor = half3(colorR.r, sceneColor.g, colorB.b);
    float3 finalColor = lerp(sceneColor.rgb, splitColor, _RGBSplitFading);

    return float4(finalColor, 1.0);
}

half4 RGBSplitVerticalFragment(Varyings input) : SV_Target
{

    float2 uv = input.uv0;
    float time = _RGBSplitTimeX * 6 * _RGBSplitSpeed;
    float splitAmount = (1.0 + sin(time)) * 0.5;
    splitAmount *= 1.0 + sin(time * 2) * 0.5;
    splitAmount = pow(splitAmount, 3.0);
    splitAmount *= 0.05;
    splitAmount *= _RGBSplitFading * _RGBSplitAmount;
    splitAmount *= _RGBSplitFading * _RGBSplitAmount;

    float3 colorR = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x , uv.y + splitAmount * _RGBSplitAmountR)).rgb;
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
    float3 colorB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x, uv.y - splitAmount * _RGBSplitAmountB)).rgb;

    float3 splitColor = half3(colorR.r, sceneColor.g, colorB.b);
    float3 finalColor = lerp(sceneColor.rgb, splitColor, _RGBSplitFading);

    return float4(finalColor, 1.0);
}


float4 RGBSplitCombineFragment(Varyings input) : SV_Target
{

    float2 uv =input.uv0;
    float time = _RGBSplitTimeX * 6 * _RGBSplitSpeed;
    float splitAmount = (1.0 + sin(time)) * 0.5;
    splitAmount *= 1.0 + sin(time * 2) * 0.5;
    splitAmount = pow(splitAmount, 3.0);
    splitAmount *= 0.05;
    splitAmount *= _RGBSplitFading * _RGBSplitAmount;
    splitAmount *= _RGBSplitFading * _RGBSplitAmount;

    float splitAmountR = splitAmount * _RGBSplitAmountR;
    float splitAmountB = splitAmount * _RGBSplitAmountB;

    float3 colorR = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x + splitAmountR, uv.y + splitAmountR)).rgb;
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
    float3 colorB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv.x - splitAmountB, uv.y - splitAmountB)).rgb;

    float3 splitColor = half3(colorR.r, sceneColor.g, colorB.b);
    float3 finalColor = lerp(sceneColor.rgb, splitColor, _RGBSplitFading);

    return float4(finalColor, 1.0);

}

float3 _ImageBlockParams;

#define _ImageBlockSpeed _ImageBlockParams.x
#define _ImageBlockBlockSize _ImageBlockParams.y


float4 ImageBlockFragment(Varyings input) : SV_Target
{
    float2 block = randomNoise(floor(input.uv0 * _ImageBlockBlockSize),_ImageBlockSpeed);
    float displaceNoise = pow(block.x, 8.0) * pow(block.x, 3.0);

    float ColorR = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0).r;
    float ColorG = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                    input.uv0 + float2(displaceNoise * 0.05 * randomNoise(7.0), 0.0)).g;
    float ColorB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,
                                    input.uv0 - float2(displaceNoise * 0.05 * randomNoise(13.0), 0.0)).b;

    return float4(ColorR, ColorG, ColorB, 1.0);
}

uniform float4 _LineBlockParams;
uniform float4 _LineBlockParams2;
	
#define _LineBlockFrequency _LineBlockParams.x
#define _LineBlockTimeX _LineBlockParams.y
#define _LineBlockAmount _LineBlockParams.z
#define _LineBlockOffset _LineBlockParams2.x
#define _LineBlockLinesWidth _LineBlockParams2.y
#define _LineBlockAlpha _LineBlockParams2.z

float4 LineBlockHorizontalFragment(Varyings input): SV_Target
{
    float2 uv = input.uv0;
		
    half strength = 0;
    #if USING_FREQUENCY_INFINITE
    strength = 10;
    #else
    strength = 0.5 + 0.5 * cos(_LineBlockTimeX * _LineBlockFrequency);
    #endif
		
    _LineBlockTimeX *= strength;
		
    //	[1] 生成随机强度梯度线条
    float truncTime = trunc(_LineBlockTimeX, 4.0);
    float uv_trunc = randomNoise(trunc(uv.yy, float2(8, 8)) + 100.0 * truncTime);
    float uv_randomTrunc = 6.0 * trunc(_LineBlockTimeX, 24.0 * uv_trunc);
		
    // [2] 生成随机非均匀宽度线条
    float blockLine_random = 0.5 * randomNoise(trunc(uv.yy + uv_randomTrunc, float2(8 * _LineBlockLinesWidth, 8 * _LineBlockLinesWidth)));
    blockLine_random += 0.5 * randomNoise(trunc(uv.yy + uv_randomTrunc, float2(7, 7)));
    blockLine_random = blockLine_random * 2.0 - 1.0;
    blockLine_random = sign(blockLine_random) * saturate((abs(blockLine_random) - _LineBlockAmount) / (0.4));
    blockLine_random = lerp(0, blockLine_random, _LineBlockOffset);
		
		
    // [3] 生成源色调的blockLine Glitch
    float2 uv_blockLine = uv;
    uv_blockLine = saturate(uv_blockLine + float2(0.1 * blockLine_random, 0));
    float4 blockLineColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, abs(uv_blockLine));
		
    // [4] 将RGB转到YUV空间，并做色调偏移
    // RGB -> YUV
    float3 blockLineColor_yuv = rgb2yuv(blockLineColor.rgb);
    // adjust Chrominance | 色度
    blockLineColor_yuv.y /= 1.0 - 3.0 * abs(blockLine_random) * saturate(0.5 - blockLine_random);
    // adjust Chroma | 浓度
    blockLineColor_yuv.z += 0.125 * blockLine_random * saturate(blockLine_random - 0.5);
    float3 blockLineColor_rgb = yuv2rgb(blockLineColor_yuv);
		
		
    // [5] 与源场景图进行混合
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
    return lerp(sceneColor, float4(blockLineColor_rgb, blockLineColor.a), _LineBlockAlpha);
}

float4 LineBlockVerticalFragment(Varyings input): SV_Target
{
    float2 uv = input.uv0;
		
    half strength = 0;
    #if USING_FREQUENCY_INFINITE
    strength = 10;
    #else
    strength = 0.5 + 0.5 * cos(_LineBlockTimeX * _LineBlockFrequency);
    #endif
		
    _LineBlockTimeX *= strength;
		
    // [1] 生成随机均匀宽度线条
    float truncTime = trunc(_LineBlockTimeX, 4.0);
    float uv_trunc = randomNoise(trunc(uv.xx, float2(8, 8)) + 100.0 * truncTime);
    float uv_randomTrunc = 6.0 * trunc(_LineBlockTimeX, 24.0 * uv_trunc);
		
    // [2] 生成随机非均匀宽度线条 | Generate Random inhomogeneous Block Line
    float blockLine_random = 0.5 * randomNoise(trunc(uv.xx + uv_randomTrunc, float2(8 * _LineBlockLinesWidth, 8 * _LineBlockLinesWidth)));
    blockLine_random += 0.5 * randomNoise(trunc(uv.xx + uv_randomTrunc, float2(7, 7)));
    blockLine_random = blockLine_random * 2.0 - 1.0;
    blockLine_random = sign(blockLine_random) * saturate((abs(blockLine_random) - _LineBlockAmount) / (0.4));
    blockLine_random = lerp(0, blockLine_random, _LineBlockOffset);
		
    // [3] 生成源色调的blockLine Glitch
    float2 uv_blockLine = uv;
    uv_blockLine = saturate(uv_blockLine + float2(0, 0.1 * blockLine_random));
    float4 blockLineColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, abs(uv_blockLine));
		
    // [4] 将RGB转到YUV空间，并做色调偏移
    // RGB -> YUV
    float3 blockLineColor_yuv = rgb2yuv(blockLineColor.rgb);
    // adjust Chrominance | 色度
    blockLineColor_yuv.y /= 1.0 - 3.0 * abs(blockLine_random) * saturate(0.5 - blockLine_random);
    // adjust Chroma | 浓度
    blockLineColor_yuv.z += 0.125 * blockLine_random * saturate(blockLine_random - 0.5);
    float3 blockLineColor_rgb = yuv2rgb(blockLineColor_yuv);
		
    // [5] 与源场景图进行混合
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture,input.uv0);
    return lerp(sceneColor, float4(blockLineColor_rgb, blockLineColor.a), _LineBlockAlpha);
}

uniform float4 _TileJitterParams;

#define _TileJitterSplittingNumber _TileJitterParams.x
#define _TileJitterAmount _TileJitterParams.y
#define _TileJitterSpeed _TileJitterParams.z
#define _TileJitterFrequency _TileJitterParams.w

float4 TileJitterHorizontalFragment(Varyings input): SV_Target
{
    float2 uv = input.uv0;
    half strength = 1.0;
    half pixelSizeX = 1.0 / _ScreenParams.x;

    // --------------------------------Prepare Jitter UV--------------------------------
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 * cos(frac(_Time.y)  * _TileJitterFrequency);
    #endif
    if(fmod(uv.y * _TileJitterSplittingNumber, 2) < 1.0)
    {
        #if JITTER_DIRECTION_HORIZONTAL
        uv.x += pixelSizeX * cos(frac(_Time.y)  * _TileJitterSpeed) * _TileJitterAmount * strength;
        #else
        uv.y += pixelSizeX * cos(frac(_Time.y)  * _TileJitterSpeed) * _TileJitterAmount * strength;
    #endif
    }

    // -------------------------------Final Sample------------------------------
    half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
    return sceneColor;
}

float4 TileJitterVerticalFragment(Varyings input): SV_Target
{
    float2 uv = input.uv0;
    half strength = 1.0;
    half pixelSizeX = 1.0 / _ScreenParams.x;
		
    // --------------------------------Prepare Jitter UV--------------------------------
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 *cos(frac(_Time.y) * _TileJitterFrequency);
    #endif

    if (fmod(uv.x * _TileJitterSplittingNumber, 2) < 1.0)
    {
        #if JITTER_DIRECTION_HORIZONTAL
        uv.x += pixelSizeX * cos(frac(_Time.y)  * _TileJitterSpeed) * _TileJitterAmount * strength;
        #else
        uv.y += pixelSizeX * cos(frac(_Time.y)  * _TileJitterSpeed) * _TileJitterAmount * strength;
    #endif
    }

    // -------------------------------Final Sample------------------------------
    half4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);
    return sceneColor;
}

uniform float3 _ScanLineJitterParams;
#define _ScanLineJitterAmount _ScanLineJitterParams.x
#define _ScanLineJitterThreshold _ScanLineJitterParams.y
#define _ScanLineJitterFrequency _ScanLineJitterParams.z

float4 ScanLineJitterHorizontalFragment(Varyings input): SV_Target
{
    float strength = 0;
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 * cos(_Time.y * _ScanLineJitterFrequency);
    #endif
		
		
    float jitter = randomNoise(input.uv0.y, _Time.x) * 2 - 1;
    jitter *= step(_ScanLineJitterThreshold, abs(jitter)) * _ScanLineJitterAmount * strength;
		
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, frac(input.uv0 + float2(jitter, 0)));
		
    return sceneColor;
}
	
float4 ScanLineJitterVerticalFragment(Varyings input): SV_Target
{
    float strength = 0;
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 * cos(_Time.y * _ScanLineJitterFrequency);
    #endif
		
    float jitter = randomNoise(input.uv0.x, _Time.x) * 2 - 1;
    jitter *= step(_ScanLineJitterThreshold, abs(jitter)) * _ScanLineJitterAmount * strength;
		
    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, frac(input.uv0  + float2(0, jitter)));
		
    return sceneColor;
}

uniform half _DigitalStripeIntensity;
uniform half4 _DigitalStripeColorAdjustColor;
uniform half _DigitalStripeColorAdjustIntensity;
TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

half4 DigitalStripeFragment(Varyings input): SV_Target
{
    // 基础数据准备
    half4 stripNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, input.uv0);
    half threshold = 1.001 - _DigitalStripeIntensity * 1.001;

    // uv偏移
    half uvShift = step(threshold, pow(abs(stripNoise.x), 3));
    float2 uv = frac(input.uv0 + stripNoise.yz * uvShift);
    half4 source = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);

    #ifndef NEED_TRASH_FRAME
    return source;
    #endif 	

    // 基于废弃帧插值
    half stripIndensity = step(threshold, pow(abs(stripNoise.w), 3)) * _DigitalStripeColorAdjustIntensity;
    half3 color = lerp(source, _DigitalStripeColorAdjustColor, stripIndensity).rgb;
    return float4(color, source.a);
}

uniform float4 _AnalogNoiseParams;
#define _AnalogNoiseSpeed _AnalogNoiseParams.x
#define _AnalogNoiseFading _AnalogNoiseParams.y
#define _AnalogNoiseLuminanceJitterThreshold _AnalogNoiseParams.z
#define _AnalogNoiseTimeX _AnalogNoiseParams.w

float4 AnalogNoiseFragment(Varyings input): SV_Target
{

    float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, input.uv0);
    float4 noiseColor = sceneColor;

    half luminance = dot(noiseColor.rgb, float3(0.22, 0.707, 0.071));
    if (randomNoise(float2(_AnalogNoiseTimeX * _AnalogNoiseSpeed, _AnalogNoiseTimeX * _AnalogNoiseSpeed)) > _AnalogNoiseLuminanceJitterThreshold)
    {
        noiseColor = float4(luminance, luminance, luminance, luminance);
    }

    float noiseX = randomNoise(_AnalogNoiseTimeX * _AnalogNoiseSpeed + input.uv0 / float2(-213, 5.53));
    float noiseY = randomNoise(_AnalogNoiseTimeX * _AnalogNoiseSpeed - input.uv0 / float2(213, -5.53));
    float noiseZ = randomNoise(_AnalogNoiseTimeX * _AnalogNoiseSpeed + input.uv0 / float2(213, 5.53));

    noiseColor.rgb += 0.25 * float3(noiseX,noiseY,noiseZ) - 0.125;

    noiseColor = lerp(sceneColor, noiseColor, _AnalogNoiseFading);
		
    return noiseColor;
}

uniform float4 _WaveJitterParams;
float2 _WaveJitterResolution;

#define _Frequency _WaveJitterParams.x
#define _RGBSplit _WaveJitterParams.y
#define _Speed _WaveJitterParams.z
#define _Amount _WaveJitterParams.w

float4 WaveJitterHorizontalFragment(Varyings input): SV_Target
{
    half strength = 0.0;
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 *cos(_Time.y * _Frequency);
    #endif
		
    // Prepare UV
    float uv_y = input.uv0.y * _WaveJitterResolution.y;
    float noise_wave_1 = snoise(float2(uv_y * 0.01, _Time.y * _Speed * 20)) * (strength * _Amount * 32.0);
    float noise_wave_2 = snoise(float2(uv_y * 0.02, _Time.y * _Speed * 10)) * (strength * _Amount * 4.0);
    float noise_wave_x = noise_wave_1 * noise_wave_2 / _WaveJitterResolution.x;
    float uv_x = input.uv0.x + noise_wave_x;

    float rgbSplit_uv_x = (_RGBSplit * 50 + (20.0 * strength + 1.0)) * noise_wave_x / _WaveJitterResolution.x;

    // Sample RGB Color-
    half4 colorG = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv_x, input.uv0.y));
    half4 colorRB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(uv_x + rgbSplit_uv_x, input.uv0.y));
		
    return  half4(colorRB.r, colorG.g, colorRB.b, colorRB.a + colorG.a);
}

float4 WaveJitterVerticalFragment(Varyings input) : SV_Target
{
    half strength = 0.0;
    #if USING_FREQUENCY_INFINITE
    strength = 1;
    #else
    strength = 0.5 + 0.5 * cos(_Time.y * _Frequency);
    #endif

    // Prepare UV
    float uv_x = input.uv0.x * _WaveJitterResolution.x;
    float noise_wave_1 = snoise(float2(uv_x * 0.01, _Time.y * _Speed * 20)) * (strength * _Amount * 32.0);
    float noise_wave_2 = snoise(float2(uv_x * 0.02, _Time.y * _Speed * 10)) * (strength * _Amount * 4.0);
    float noise_wave_y = noise_wave_1 * noise_wave_2 / _WaveJitterResolution.x;
    float uv_y = input.uv0.y + noise_wave_y;

    float rgbSplit_uv_y = (_RGBSplit * 50 + (20.0 * strength + 1.0)) * noise_wave_y / _WaveJitterResolution.y;

    // Sample RGB Color
    half4 colorG = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(input.uv0.x, uv_y));
    half4 colorRB = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, float2(input.uv0.x, uv_y + rgbSplit_uv_y));

    return half4(colorRB.r, colorG.g, colorRB.b, colorRB.a + colorG.a);
}

#endif
