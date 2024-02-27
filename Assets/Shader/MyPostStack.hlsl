#ifndef MY_POST_STACK
#define MY_POST_STACK
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
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

float Rand(float2 n)
{
    return sin(dot(n, half2(1233.224, 1743.335)));
}

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
float4 RaidalBlurFragment(Varyings input): SV_Target
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

#endif
